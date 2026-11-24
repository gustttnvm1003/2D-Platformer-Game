using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Shared unscaled-time fade and subtle scale transition helper for modal pause/menu overlays.
    /// </summary>
    internal static class MenuOverlayTransitionHelper
    {
        public static void EnsureComponents(GameObject panelRoot, ref CanvasGroup canvasGroup, ref Transform cardTransform)
        {
            if (panelRoot == null) return;

            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = panelRoot.AddComponent<CanvasGroup>();
                }
            }

            if (cardTransform == null && panelRoot.transform.childCount > 0)
            {
                cardTransform = panelRoot.transform.GetChild(0);
            }
        }

        public static void SetImmediate(
            GameObject panelRoot,
            CanvasGroup canvasGroup,
            Transform cardTransform,
            bool showing)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = showing ? 1f : 0f;
                canvasGroup.interactable = showing;
                canvasGroup.blocksRaycasts = showing;
            }
            if (cardTransform != null)
            {
                cardTransform.localScale = Vector3.one;
            }
            if (panelRoot != null)
            {
                panelRoot.SetActive(showing);
            }
        }

        public static IEnumerator AnimateFadeScaleRoutine(
            CanvasGroup canvasGroup,
            Transform cardTransform,
            bool showing,
            float duration,
            float hiddenScale,
            bool blockRaycastsWhileFadingOut,
            Action onComplete)
        {
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : (showing ? 0f : 1f);
            float endAlpha = showing ? 1f : 0f;
            float startScale = showing ? hiddenScale : 1.0f;
            float endScale = showing ? 1.0f : hiddenScale;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = showing;
                canvasGroup.blocksRaycasts = showing || blockRaycastsWhileFadingOut;
            }
            if (cardTransform != null && showing && startAlpha < 0.05f)
            {
                cardTransform.localScale = Vector3.one * startScale;
            }

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                }
                if (cardTransform != null)
                {
                    float sc = Mathf.Lerp(startScale, endScale, eased);
                    cardTransform.localScale = Vector3.one * sc;
                }
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = endAlpha;
                canvasGroup.interactable = showing;
                canvasGroup.blocksRaycasts = showing;
            }
            if (cardTransform != null)
            {
                cardTransform.localScale = Vector3.one * endScale;
            }

            if (onComplete != null) onComplete();
        }
    }

    /// <summary>
    /// Interactive Controls & Keybinding overlay opened from the Main Menu or Pause Menu.
    /// Lists all core, Tier 1, and Tier 2 movesets (Glide, Charge Slash, all 6 Silk Skills, Taunt,
    /// and menus) and lets the player manually rebind any Keyboard or Mouse button at runtime.
    /// </summary>
    public class ControlsMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform leftColumnContainer;
        [SerializeField] private Transform rightColumnContainer;
        [SerializeField] private Text comboTipsText;
        [SerializeField] private Text statusFeedbackText;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button closeButton;

        private const float TransitionDuration = 0.18f;
        private static readonly Color NormalBtnColor = new Color(0.14f, 0.24f, 0.28f, 0.96f);
        private static readonly Color ListenBtnColor = new Color(0.76f, 0.52f, 0.14f, 0.98f);
        private static readonly Color RowBgEvenColor = new Color(0.05f, 0.10f, 0.12f, 0.68f);
        private static readonly Color RowBgOddColor = new Color(0.08f, 0.14f, 0.16f, 0.68f);
        private static readonly Color StatusIdleColor = new Color(0.74f, 0.88f, 0.84f, 0.94f);
        private static readonly Color StatusListenColor = new Color(1.0f, 0.88f, 0.44f, 1f);

        private struct RowEntry
        {
            public ControlAction Action;
            public Button BindButton;
            public Image BindButtonImage;
            public Text BindButtonText;
        }

        private readonly System.Collections.Generic.List<RowEntry> _rows = new System.Collections.Generic.List<RowEntry>();
        private CanvasGroup _canvasGroup;
        private Transform _cardTransform;
        private Coroutine _transitionCoroutine;
        private Action _onClosedCallback;
        private bool _isOpen;
        private bool _rowsBuilt;

        private bool _isWaitingForRebind;
        private ControlAction _listeningAction;
        private float _listenStartUnscaledTime;

        public bool IsOpen { get { return _isOpen; } }
        public bool IsWaitingForRebind { get { return _isWaitingForRebind; } }

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);

            BuildInteractiveBindingRows();
            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
            _isOpen = false;
        }

        private void OnEnable()
        {
            ControlBindingsManager.OnBindingsChanged += RefreshAllBindingLabels;
        }

        private void OnDisable()
        {
            ControlBindingsManager.OnBindingsChanged -= RefreshAllBindingLabels;
            _isWaitingForRebind = false;
        }

        private void Update()
        {
            if (!_isOpen || !_isWaitingForRebind) return;

            // Small unscaled delay prevents capturing the Mouse0 click that activated this button
            if (Time.unscaledTime - _listenStartUnscaledTime < 0.14f) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelListening("Rebind cancelled.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                return;
            }

            if (!Input.anyKeyDown) return;

            KeyCode[] supported = ControlBindingsManager.SupportedRebindKeys;
            for (int i = 0; i < supported.Length; i++)
            {
                KeyCode candidate = supported[i];
                if (Input.GetKeyDown(candidate))
                {
                    ControlAction targetAction = _listeningAction;
                    _isWaitingForRebind = false;
                    string feedback = ControlBindingsManager.RebindPrimary(targetAction, candidate);
                    RefreshAllBindingLabels();
                    SetStatus(feedback, StatusIdleColor);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                    return;
                }
            }
        }

        private void BuildInteractiveBindingRows()
        {
            if (_rowsBuilt || leftColumnContainer == null || rightColumnContainer == null) return;
            _rowsBuilt = true;

            Font font = HUDController.LoadSafeFont();
            ControlAction[] all = ControlBindingsManager.AllActions;
            int splitIndex = 10; // 10 Movement & Mobility on left, 9 Combat, Silk Skills & Menus on right

            for (int i = 0; i < all.Length; i++)
            {
                bool isLeft = i < splitIndex;
                Transform parentCol = isLeft ? leftColumnContainer : rightColumnContainer;
                int localIdx = isLeft ? i : (i - splitIndex);
                int colCount = isLeft ? splitIndex : (all.Length - splitIndex);
                ControlAction act = all[i];

                float rowSpan = 1f / Mathf.Max(1, colCount);
                float yMax = 1f - localIdx * rowSpan - 0.006f;
                float yMin = 1f - (localIdx + 1) * rowSpan + 0.006f;

                GameObject rowGo = new GameObject("Row_" + act.ToString());
                rowGo.transform.SetParent(parentCol, false);
                Image rowBg = rowGo.AddComponent<Image>();
                rowBg.color = (localIdx % 2 == 0) ? RowBgEvenColor : RowBgOddColor;
                RectTransform rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, yMin);
                rowRt.anchorMax = new Vector2(1f, yMax);
                rowRt.offsetMin = Vector2.zero;
                rowRt.offsetMax = Vector2.zero;

                GameObject labelGo = new GameObject("ActionLabel");
                labelGo.transform.SetParent(rowGo.transform, false);
                Text lbl = labelGo.AddComponent<Text>();
                lbl.text = ControlBindingsManager.GetActionDisplayName(act);
                lbl.font = font;
                lbl.fontSize = 14;
                lbl.alignment = TextAnchor.MiddleLeft;
                lbl.color = new Color(0.91f, 0.94f, 0.90f, 0.98f);
                RectTransform lblRt = labelGo.GetComponent<RectTransform>();
                lblRt.anchorMin = new Vector2(0.04f, 0.04f);
                lblRt.anchorMax = new Vector2(0.58f, 0.96f);
                lblRt.offsetMin = Vector2.zero;
                lblRt.offsetMax = Vector2.zero;

                GameObject btnGo = new GameObject("Btn_Rebind_" + act.ToString());
                btnGo.transform.SetParent(rowGo.transform, false);
                Image btnImg = btnGo.AddComponent<Image>();
                btnImg.color = NormalBtnColor;
                Button btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                btnGo.AddComponent<UIButtonAudioFeedback>();
                RectTransform btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.60f, 0.10f);
                btnRt.anchorMax = new Vector2(0.97f, 0.90f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;

                GameObject btnTxtGo = new GameObject("KeyLabel");
                btnTxtGo.transform.SetParent(btnGo.transform, false);
                Text btnTxt = btnTxtGo.AddComponent<Text>();
                btnTxt.text = ControlBindingsManager.GetDisplayLabelForButton(act);
                btnTxt.font = font;
                btnTxt.fontSize = 13;
                btnTxt.alignment = TextAnchor.MiddleCenter;
                btnTxt.color = new Color(0.97f, 0.96f, 0.90f, 1f);
                RectTransform btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
                btnTxtRt.anchorMin = Vector2.zero;
                btnTxtRt.anchorMax = Vector2.one;
                btnTxtRt.offsetMin = Vector2.zero;
                btnTxtRt.offsetMax = Vector2.zero;

                ControlAction capturedAction = act;
                btn.onClick.AddListener(delegate { BeginListeningForAction(capturedAction); });

                _rows.Add(new RowEntry
                {
                    Action = act,
                    BindButton = btn,
                    BindButtonImage = btnImg,
                    BindButtonText = btnTxt
                });
            }
        }

        private void BeginListeningForAction(ControlAction action)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            _isWaitingForRebind = true;
            _listeningAction = action;
            _listenStartUnscaledTime = Time.unscaledTime;
            RefreshAllBindingLabels();
            SetStatus(
                string.Format("Listening for {0}... Press any Keyboard key or Mouse button (ESC to cancel)",
                    ControlBindingsManager.GetActionDisplayName(action)),
                StatusListenColor);
        }

        private void CancelListening(string message)
        {
            _isWaitingForRebind = false;
            RefreshAllBindingLabels();
            SetStatus(message, StatusIdleColor);
        }

        private void OnResetDefaultsClicked()
        {
            _isWaitingForRebind = false;
            ControlBindingsManager.ResetAllToDefaults();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            RefreshAllBindingLabels();
            SetStatus("All controls restored to default bindings.", StatusIdleColor);
        }

        private void RefreshAllBindingLabels()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                RowEntry row = _rows[i];
                bool listeningThis = _isWaitingForRebind && row.Action == _listeningAction;
                if (row.BindButtonImage != null)
                {
                    row.BindButtonImage.color = listeningThis ? ListenBtnColor : NormalBtnColor;
                }
                if (row.BindButtonText != null)
                {
                    row.BindButtonText.text = listeningThis
                        ? "PRESS KEY..."
                        : ControlBindingsManager.GetDisplayLabelForButton(row.Action);
                }
            }

            if (comboTipsText != null)
            {
                comboTipsText.text = ControlBindingsManager.BuildDynamicComboTips();
            }
        }

        private void SetStatus(string text, Color col)
        {
            if (statusFeedbackText == null) return;
            statusFeedbackText.text = text;
            statusFeedbackText.color = col;
        }

        public void Show(Action onClosed)
        {
            BuildInteractiveBindingRows();
            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);
            _onClosedCallback = onClosed;
            _isOpen = true;
            _isWaitingForRebind = false;

            RefreshAllBindingLabels();
            SetStatus("Click any key button to rebind  |  Press ESC while listening to cancel", StatusIdleColor);

            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
                panelRoot.SetActive(true);
            }

            StartTransition(true, null);
        }

        public void Hide()
        {
            if (!_isOpen && (panelRoot == null || !panelRoot.activeSelf)) return;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            _isWaitingForRebind = false;
            _isOpen = false;

            Action closedCb = _onClosedCallback;
            _onClosedCallback = null;

            StartTransition(false, delegate
            {
                if (panelRoot != null) panelRoot.SetActive(false);
                if (closedCb != null) closedCb();
            });
        }

        public void HideImmediate()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            _isWaitingForRebind = false;
            _isOpen = false;
            _onClosedCallback = null;
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
        }

        private void StartTransition(bool showing, Action onComplete)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (!gameObject.activeInHierarchy || _canvasGroup == null)
            {
                MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, showing);
                if (onComplete != null) onComplete();
                return;
            }

            _transitionCoroutine = StartCoroutine(MenuOverlayTransitionHelper.AnimateFadeScaleRoutine(
                _canvasGroup, _cardTransform, showing, TransitionDuration, 0.93f, true, delegate
                {
                    _transitionCoroutine = null;
                    if (onComplete != null) onComplete();
                }));
        }
    }
}
