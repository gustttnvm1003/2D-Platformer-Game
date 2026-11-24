using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Full Inventory overlay displaying carried items, descriptions, direct Consumable/Relic
    /// activation, and the centralized Silk Essence vessel removed from the minimal HUD.
    /// Accessible via hotkey [I]/[B] during gameplay or via the Pause Menu.
    /// </summary>
    public class InventoryMenuController : MonoBehaviour
    {
        [Header("Root & Transition")]
        [SerializeField] private GameObject panelRoot;

        [Header("Essence Vessel")]
        [SerializeField] private Text essenceAmountText;
        [SerializeField] private Text pouchCountText;

        [Header("Items Grid & Inspector")]
        [SerializeField] private Transform itemListContainer;
        [SerializeField] private Text emptyListHintText;
        [SerializeField] private Image detailIconImage;
        [SerializeField] private Text detailTitleText;
        [SerializeField] private Text detailCategoryText;
        [SerializeField] private Text detailDescriptionText;
        [SerializeField] private Text detailEffectText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button useItemButton;
        [SerializeField] private Text useButtonText;
        [SerializeField] private Button closeButton;

        private const float TransitionDuration = 0.16f;

        private CanvasGroup _canvasGroup;
        private Transform _cardTransform;
        private Coroutine _transitionCoroutine;
        private Action _onPauseSubClosed;
        private bool _openedFromPause;
        private PlayerController _player;
        private Font _font;
        private string _selectedItemId;

        public bool IsOpenFromPause { get { return _openedFromPause && panelRoot != null && panelRoot.activeSelf; } }

        private void Awake()
        {
            _font = HUDController.LoadSafeFont();
            if (useItemButton != null) useItemButton.onClick.AddListener(OnUseItemClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnInventoryChanged += HandleInventoryOrEssenceChanged;
            GameEvents.OnSilkEssenceChanged += HandleEssenceEvent;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnInventoryChanged -= HandleInventoryOrEssenceChanged;
            GameEvents.OnSilkEssenceChanged -= HandleEssenceEvent;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.InventoryMenu)
            {
                _openedFromPause = false;
                _onPauseSubClosed = null;
                if (feedbackText != null) feedbackText.text = string.Empty;
                RefreshDisplay();
                TransitionVisibility(true, false, null);
            }
            else if (state != GameState.Paused || !_openedFromPause)
            {
                HideImmediate();
            }
        }

        public void ShowFromPause(Action onClosed)
        {
            _openedFromPause = true;
            _onPauseSubClosed = onClosed;
            if (feedbackText != null) feedbackText.text = string.Empty;
            RefreshDisplay();

            if (panelRoot != null) panelRoot.transform.SetAsLastSibling();
            TransitionVisibility(true, true, null);
        }

        public void HideFromPause()
        {
            if (!IsOpenFromPause) return;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            _openedFromPause = false;
            Action cb = _onPauseSubClosed;
            _onPauseSubClosed = null;

            TransitionVisibility(false, true, delegate
            {
                if (panelRoot != null) panelRoot.SetActive(false);
                if (cb != null) cb();
            });
        }

        public void HideImmediate()
        {
            _openedFromPause = false;
            _onPauseSubClosed = null;
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
        }

        private void HandleInventoryOrEssenceChanged()
        {
            if (panelRoot != null && panelRoot.activeSelf) RefreshDisplay();
        }

        private void HandleEssenceEvent(int amount)
        {
            if (panelRoot != null && panelRoot.activeSelf) RefreshDisplay();
        }

        private void OnCloseClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (_openedFromPause)
            {
                HideFromPause();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        private void OnUseItemClicked()
        {
            if (string.IsNullOrEmpty(_selectedItemId) || _player == null || _player.Inventory == null) return;

            string msg;
            if (_player.Inventory.TryUseItem(_selectedItemId, out msg))
            {
                if (feedbackText != null)
                {
                    feedbackText.color = new Color(0.62f, 0.96f, 0.80f, 1f);
                    feedbackText.text = msg;
                }
            }
            else if (feedbackText != null)
            {
                feedbackText.color = new Color(0.96f, 0.68f, 0.45f, 1f);
                feedbackText.text = msg;
            }

            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (_player == null) _player = FindObjectOfType<PlayerController>();
            if (_player == null) return;

            InventoryManager inv = _player.Inventory;
            int essence = inv != null ? inv.SilkEssence : (_player.Upgrades != null ? _player.Upgrades.SilkEssence : 0);
            if (essenceAmountText != null)
            {
                essenceAmountText.text = string.Format("SILK ESSENCE: {0}", essence);
            }

            if (itemListContainer == null) return;

            for (int i = itemListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(itemListContainer.GetChild(i).gameObject);
            }

            IList<InventoryItemStack> stacks = inv != null ? inv.Items : null;
            int totalSlots = stacks != null ? stacks.Count : 0;
            if (pouchCountText != null)
            {
                pouchCountText.text = string.Format("CARRIED ITEMS: {0}", totalSlots);
            }

            if (emptyListHintText != null)
            {
                emptyListHintText.gameObject.SetActive(totalSlots == 0);
            }

            if (totalSlots > 0)
            {
                bool selectionExists = false;
                for (int i = 0; i < stacks.Count; i++)
                {
                    if (stacks[i].itemId == _selectedItemId)
                    {
                        selectionExists = true;
                        break;
                    }
                }
                if (!selectionExists)
                {
                    _selectedItemId = stacks[0].itemId;
                }

                for (int i = 0; i < stacks.Count; i++)
                {
                    InventoryItemStack stack = stacks[i];
                    InventoryItemData data = inv.GetItemData(stack.itemId);
                    CreateItemRowButton(stack, data, stack.itemId == _selectedItemId);
                }
            }
            else
            {
                _selectedItemId = string.Empty;
            }

            UpdateInspectorPane(inv);
        }

        private void CreateItemRowButton(InventoryItemStack stack, InventoryItemData data, bool isSelected)
        {
            string itemId = stack.itemId;
            string itemName = data != null ? data.displayName : itemId;
            string catLabel = data != null ? FormatCategory(data.category) : "Item";

            GameObject row = new GameObject("InvRow_" + itemId);
            row.transform.SetParent(itemListContainer, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 68f);

            Image bg = row.AddComponent<Image>();
            bg.color = isSelected
                ? new Color(0.18f, 0.34f, 0.30f, 0.96f)
                : new Color(0.08f, 0.14f, 0.16f, 0.90f);

            Button btn = row.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(delegate
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
                _selectedItemId = itemId;
                if (feedbackText != null) feedbackText.text = string.Empty;
                RefreshDisplay();
            });

            if (data != null && data.icon != null)
            {
                GameObject iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(row.transform, false);
                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = data.icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                RectTransform irt = iconGo.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.02f, 0.12f);
                irt.anchorMax = new Vector2(0.14f, 0.88f);
                irt.offsetMin = Vector2.zero;
                irt.offsetMax = Vector2.zero;
            }

            Text title = CreateRowText("Name", row.transform, itemName, 17, TextAnchor.MiddleLeft, new Color(0.95f, 0.94f, 0.88f, 1f));
            RectTransform trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.16f, 0.42f);
            trt.anchorMax = new Vector2(0.76f, 0.92f);

            Text sub = CreateRowText("Category", row.transform, catLabel, 13, TextAnchor.MiddleLeft, new Color(0.64f, 0.88f, 0.80f, 0.92f));
            RectTransform srt = sub.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.16f, 0.08f);
            srt.anchorMax = new Vector2(0.76f, 0.44f);

            Text qty = CreateRowText("Qty", row.transform, "x" + stack.quantity, 20, TextAnchor.MiddleRight, new Color(0.96f, 0.86f, 0.56f, 1f));
            RectTransform qrt = qty.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(0.78f, 0.12f);
            qrt.anchorMax = new Vector2(0.96f, 0.88f);
        }

        private void UpdateInspectorPane(InventoryManager inv)
        {
            InventoryItemData data = (inv != null && !string.IsNullOrEmpty(_selectedItemId))
                ? inv.GetItemData(_selectedItemId)
                : null;

            if (data == null)
            {
                if (detailIconImage != null) detailIconImage.gameObject.SetActive(false);
                if (detailTitleText != null) detailTitleText.text = "NO ITEM SELECTED";
                if (detailCategoryText != null) detailCategoryText.text = string.Empty;
                if (detailDescriptionText != null) detailDescriptionText.text = "Collect relics, essence shards, and mossberry vials throughout Silken Grotto to fill your traveler's pouch.";
                if (detailEffectText != null) detailEffectText.text = string.Empty;
                if (useItemButton != null) useItemButton.gameObject.SetActive(false);
                return;
            }

            if (detailIconImage != null)
            {
                detailIconImage.gameObject.SetActive(data.icon != null);
                detailIconImage.sprite = data.icon;
            }
            if (detailTitleText != null) detailTitleText.text = data.displayName.ToUpper();
            if (detailCategoryText != null) detailCategoryText.text = FormatCategory(data.category);
            if (detailDescriptionText != null) detailDescriptionText.text = data.description;
            if (detailEffectText != null) detailEffectText.text = data.effectSummary;

            if (useItemButton != null)
            {
                useItemButton.gameObject.SetActive(data.usableFromInventory);
                if (data.usableFromInventory)
                {
                    string reason;
                    bool canUse = inv != null && inv.CanUseItem(data.itemId, out reason);
                    useItemButton.interactable = canUse;
                    if (useButtonText != null)
                    {
                        if (data.essenceYield > 0) useButtonText.text = "CHANNEL ESSENCE";
                        else useButtonText.text = canUse ? "USE ITEM" : "CANNOT USE NOW";
                    }
                }
            }
        }

        private static string FormatCategory(InventoryItemCategory cat)
        {
            switch (cat)
            {
                case InventoryItemCategory.EssenceRelic: return "Essence Shard / Relic";
                case InventoryItemCategory.KeyArtifact: return "Ancient Artifact";
                default: return "Consumable Pouch Item";
            }
        }

        private Text CreateRowText(string objName, Transform parent, string text, int fontSize, TextAnchor align, Color col)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = _font != null ? _font : HUDController.LoadSafeFont();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = col;
            t.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return t;
        }

        private void TransitionVisibility(bool showing, bool blockWhileFadingOut, Action onComplete)
        {
            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (showing && panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (!gameObject.activeInHierarchy || _canvasGroup == null)
            {
                MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, showing);
                if (onComplete != null) onComplete();
                return;
            }

            _transitionCoroutine = StartCoroutine(MenuOverlayTransitionHelper.AnimateFadeScaleRoutine(
                _canvasGroup, _cardTransform, showing, TransitionDuration, 0.94f, blockWhileFadingOut, delegate
                {
                    _transitionCoroutine = null;
                    if (!showing && panelRoot != null) panelRoot.SetActive(false);
                    if (onComplete != null) onComplete();
                }));
        }
    }
}
