using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Minimalist gothic HUD displaying Mask Health indicators, Silk Energy bar,
    /// Silk Essence counter, unlocked/locked Skill slots with cooldown overlays,
    /// proximity interaction prompts, area title banners, and checkpoint/skill notifications.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health Masks")]
        [SerializeField] private CanvasGroup hudDimGroup;
        [SerializeField] private Transform healthMaskContainer;
        [SerializeField] private Sprite maskFullSprite;
        [SerializeField] private Sprite maskEmptySprite;
        [SerializeField] private Image crestReadyGlowImage;

        [Header("Silk Energy & Active Crest")]
        [SerializeField] private Image silkFillImage;
        [SerializeField] private Image crestBadgeBackground;
        [SerializeField] private Text crestBadgeText;

        [Header("Skills Bar")]
        [SerializeField] private Transform skillSlotContainer;

        [Header("Prompts & Banners")]
        [SerializeField] private GameObject interactPromptRoot;
        [SerializeField] private Text interactPromptText;
        [SerializeField] private GameObject notificationRoot;
        [SerializeField] private Text notificationTitleText;
        [SerializeField] private Text notificationSubText;
        [SerializeField] private GameObject areaBannerRoot;
        [SerializeField] private Text areaBannerTitleText;
        [SerializeField] private Text areaBannerSubText;

        private readonly List<Image> _maskImages = new List<Image>();
        private readonly Dictionary<SkillType, SkillSlotVisual> _skillSlots = new Dictionary<SkillType, SkillSlotVisual>();
        private PlayerController _player;
        private Coroutine _notifyCoroutine;
        private Coroutine _areaCoroutine;
        private Coroutine _healPulseCoroutine;
        private int _lastKnownHealth = -1;
        private int _lastKnownMaxHealth = 5;
        private float _lastKnownSilk = 0f;
        private Font _uiFont;

        private class SkillSlotVisual
        {
            public Image Background;
            public Image IconImage;
            public Image CooldownOverlay;
            public Text LabelText;
        }

        private void Awake()
        {
            _uiFont = LoadSafeFont();
            if (interactPromptRoot != null) interactPromptRoot.SetActive(false);
            if (notificationRoot != null) notificationRoot.SetActive(false);
            if (areaBannerRoot != null) areaBannerRoot.SetActive(false);
            if (crestReadyGlowImage != null)
            {
                Color c = crestReadyGlowImage.color;
                c.a = 0f;
                crestReadyGlowImage.color = c;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandleHealthChanged;
            GameEvents.OnPlayerSilkChanged += HandleSilkChanged;
            GameEvents.OnSkillUnlocked += HandleSkillUnlocked;
            GameEvents.OnSkillCooldownUpdated += HandleSkillCooldown;
            GameEvents.OnNotificationRequested += HandleNotification;
            GameEvents.OnAreaEntered += HandleAreaEntered;
            GameEvents.OnCrestChanged += HandleCrestChanged;
            ControlBindingsManager.OnBindingsChanged += RefreshCrestBadgeVisual;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandleHealthChanged;
            GameEvents.OnPlayerSilkChanged -= HandleSilkChanged;
            GameEvents.OnSkillUnlocked -= HandleSkillUnlocked;
            GameEvents.OnSkillCooldownUpdated -= HandleSkillCooldown;
            GameEvents.OnNotificationRequested -= HandleNotification;
            GameEvents.OnAreaEntered -= HandleAreaEntered;
            GameEvents.OnCrestChanged -= HandleCrestChanged;
            ControlBindingsManager.OnBindingsChanged -= RefreshCrestBadgeVisual;
        }

        private void Start()
        {
            _player = FindObjectOfType<PlayerController>();
            if (_player != null)
            {
                if (_player.Stats != null)
                {
                    HandleHealthChanged(_player.Stats.CurrentHealth, _player.Stats.MaxHealth);
                    HandleSilkChanged(_player.Stats.CurrentSilk, _player.Stats.MaxSilk);
                }
                BuildSkillSlots(_player.Skills);
            }
            RefreshCrestBadgeVisual();
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindObjectOfType<PlayerController>();
                if (_player == null)
                {
                    UpdateCrestReadyGlow();
                    return;
                }
            }

            UpdateInteractPrompt();
            UpdateCrestReadyGlow();
            if (PlayerCrestSystem.IsReaperHarvestActive)
            {
                RefreshCrestBadgeVisual();
            }
        }

        private void HandleCrestChanged(CrestType crest)
        {
            RefreshCrestBadgeVisual();
        }

        private void RefreshCrestBadgeVisual()
        {
            if (crestBadgeText == null && crestBadgeBackground == null) return;
            CrestProfile prof = PlayerCrestSystem.ActiveProfile;
            string keyHint = ControlBindingsManager.FormatKeyName(ControlBindingsManager.GetPrimaryKey(ControlAction.CycleCrest));
            if (crestBadgeText != null)
            {
                if (PlayerCrestSystem.IsReaperHarvestActive)
                {
                    crestBadgeText.text = string.Format("REAPER · HARVEST {0:0}s [{1}]", Mathf.CeilToInt(PlayerCrestSystem.ReaperHarvestRemaining), keyHint);
                }
                else
                {
                    crestBadgeText.text = string.Format("CREST: {0} [{1}]", prof.ShortBadge, keyHint);
                }
                crestBadgeText.color = prof.AccentColor;
            }
            if (crestBadgeBackground != null)
            {
                float pulse = PlayerCrestSystem.IsReaperHarvestActive ? (0.14f + 0.08f * Mathf.Sin(Time.unscaledTime * 8f)) : 0f;
                crestBadgeBackground.color = new Color(
                    0.05f + prof.AccentColor.r * (0.10f + pulse),
                    0.08f + prof.AccentColor.g * (0.10f + pulse),
                    0.10f + prof.AccentColor.b * (0.10f + pulse),
                    0.86f);
            }
        }

        private void UpdateCrestReadyGlow()
        {
            if (crestReadyGlowImage == null) return;

            bool canBind = false;
            if (_player != null && _player.Stats != null)
            {
                canBind = PlayerCrestSystem.ActiveCrest == CrestType.Witch
                    ? (_player.Stats.CurrentHealth > 0 && _player.Stats.CurrentSilk >= _player.Stats.BindHealSilkCost)
                    : _player.Stats.CanBindHeal();
            }
            else
            {
                canBind = _lastKnownHealth > 0 && _lastKnownHealth < _lastKnownMaxHealth && _lastKnownSilk >= 35f;
            }

            CrestProfile prof = PlayerCrestSystem.ActiveProfile;
            Color c = new Color(prof.AccentColor.r, prof.AccentColor.g, prof.AccentColor.b, crestReadyGlowImage.color.a);
            float targetAlpha = canBind ? (0.42f + 0.22f * Mathf.Sin(Time.unscaledTime * 4.2f)) : 0f;
            c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.unscaledDeltaTime * 3.5f);
            crestReadyGlowImage.color = c;
        }

        private void UpdateInteractPrompt()
        {
            if (interactPromptRoot == null || interactPromptText == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                interactPromptRoot.SetActive(false);
                return;
            }

            int mask = 1 << GameConstants.LayerInteractable;
            Collider2D[] cols = Physics2D.OverlapCircleAll(_player.transform.position + Vector3.up * 0.9f, 2.0f, mask);
            string prompt = string.Empty;
            for (int i = 0; i < cols.Length; i++)
            {
                IInteractable inter = cols[i].GetComponentInParent<IInteractable>();
                if (inter != null)
                {
                    string p = inter.GetInteractPrompt();
                    if (!string.IsNullOrEmpty(p))
                    {
                        prompt = p;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(prompt))
            {
                interactPromptRoot.SetActive(true);
                interactPromptText.text = prompt;
            }
            else
            {
                interactPromptRoot.SetActive(false);
            }
        }

        public void SetTransitionHudAlpha(float alpha)
        {
            if (hudDimGroup != null)
            {
                hudDimGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (healthMaskContainer == null) return;
            _lastKnownMaxHealth = Mathf.Max(1, max);
            EnsureMaskCount(_lastKnownMaxHealth);
            bool gainedMask = _lastKnownHealth >= 0 && current > _lastKnownHealth;
            int prev = _lastKnownHealth;
            _lastKnownHealth = current;

            for (int i = 0; i < _maskImages.Count; i++)
            {
                bool activeSlot = i < max;
                _maskImages[i].gameObject.SetActive(activeSlot);
                if (!activeSlot) continue;

                bool filled = i < current;
                if (maskFullSprite != null && maskEmptySprite != null)
                {
                    _maskImages[i].sprite = filled ? maskFullSprite : maskEmptySprite;
                    _maskImages[i].color = filled ? Color.white : new Color(0.92f, 0.92f, 0.95f, 0.90f);
                }
                else
                {
                    _maskImages[i].color = filled ? new Color(0.95f, 0.92f, 0.85f, 1f) : new Color(0.22f, 0.25f, 0.28f, 0.8f);
                }
            }

            if (gainedMask && prev < _maskImages.Count && gameObject.activeInHierarchy)
            {
                if (_healPulseCoroutine != null) StopCoroutine(_healPulseCoroutine);
                _healPulseCoroutine = StartCoroutine(PulseRestoredMasksRoutine(prev, Mathf.Min(current, _maskImages.Count)));
            }
        }

        private IEnumerator PulseRestoredMasksRoutine(int fromIndex, int toExclusive)
        {
            if (fromIndex >= toExclusive) yield break;
            Color origSilkColor = silkFillImage != null ? silkFillImage.color : Color.white;
            if (silkFillImage != null) silkFillImage.color = Color.white;

            float dur = 0.36f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                float wave = Mathf.Sin(u * Mathf.PI);
                for (int i = fromIndex; i < toExclusive; i++)
                {
                    Image targetMask = _maskImages[i];
                    if (targetMask != null)
                    {
                        targetMask.transform.localScale = Vector3.one * (1f + 0.36f * wave);
                        targetMask.color = Color.Lerp(Color.white, new Color(0.84f, 1f, 0.92f, 1f), wave);
                    }
                }
                if (silkFillImage != null)
                {
                    silkFillImage.color = Color.Lerp(Color.white, origSilkColor, u);
                }
                yield return null;
            }

            for (int i = fromIndex; i < toExclusive; i++)
            {
                Image targetMask = _maskImages[i];
                if (targetMask != null)
                {
                    targetMask.transform.localScale = Vector3.one;
                    targetMask.color = Color.white;
                }
            }
            if (silkFillImage != null) silkFillImage.color = origSilkColor;
            _healPulseCoroutine = null;
        }

        private void EnsureMaskCount(int needed)
        {
            while (_maskImages.Count < needed)
            {
                GameObject slot = new GameObject("Mask_" + _maskImages.Count);
                slot.transform.SetParent(healthMaskContainer, false);
                RectTransform rt = slot.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(62f, 70f);
                Image img = slot.AddComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                _maskImages.Add(img);
            }
        }

        private void HandleSilkChanged(float current, float max)
        {
            _lastKnownSilk = current;
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (silkFillImage != null) silkFillImage.fillAmount = ratio;
        }

        public void BuildSkillSlots(SkillManager skillManager)
        {
            if (skillSlotContainer == null || skillManager == null) return;

            for (int i = skillSlotContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(skillSlotContainer.GetChild(i).gameObject);
            }
            _skillSlots.Clear();

            IList<SkillData> list = skillManager.AllSkills;
            for (int i = 0; i < list.Count; i++)
            {
                SkillData s = list[i];
                if (s == null) continue;

                GameObject go = new GameObject("SkillSlot_" + s.skillType);
                go.transform.SetParent(skillSlotContainer, false);
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(62f, 76f);

                Image bg = go.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.13f, 0.15f, 0.88f);

                // Icon child
                GameObject iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                RectTransform irt = iconGo.AddComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.08f, 0.25f);
                irt.anchorMax = new Vector2(0.92f, 0.94f);
                irt.offsetMin = Vector2.zero;
                irt.offsetMax = Vector2.zero;
                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = s.icon;
                iconImg.preserveAspect = true;

                // Cooldown overlay
                GameObject cdGo = new GameObject("Cooldown");
                cdGo.transform.SetParent(iconGo.transform, false);
                RectTransform crt = cdGo.AddComponent<RectTransform>();
                crt.anchorMin = Vector2.zero;
                crt.anchorMax = Vector2.one;
                crt.offsetMin = Vector2.zero;
                crt.offsetMax = Vector2.zero;
                Image cdImg = cdGo.AddComponent<Image>();
                cdImg.color = new Color(0.02f, 0.04f, 0.06f, 0.72f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Vertical;
                cdImg.fillOrigin = 0;
                cdImg.fillAmount = 0f;

                // Input hint label
                GameObject lblGo = new GameObject("Hint");
                lblGo.transform.SetParent(go.transform, false);
                RectTransform lrt = lblGo.AddComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 0.25f);
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                Text lbl = lblGo.AddComponent<Text>();
                lbl.font = _uiFont;
                lbl.fontSize = 11;
                lbl.alignment = TextAnchor.MiddleCenter;
                lbl.color = new Color(0.84f, 0.92f, 0.88f, 0.95f);

                SkillSlotVisual visual = new SkillSlotVisual
                {
                    Background = bg,
                    IconImage = iconImg,
                    CooldownOverlay = cdImg,
                    LabelText = lbl
                };
                _skillSlots[s.skillType] = visual;
                RefreshSkillVisual(s, skillManager.IsUnlocked(s.skillType));
            }
        }

        private void RefreshSkillVisual(SkillData s, bool unlocked)
        {
            SkillSlotVisual v;
            if (!_skillSlots.TryGetValue(s.skillType, out v)) return;

            if (unlocked)
            {
                v.IconImage.color = Color.white;
                v.Background.color = new Color(0.10f, 0.20f, 0.20f, 0.92f);
                v.LabelText.text = s.inputHint;
                v.LabelText.color = new Color(0.86f, 0.96f, 0.90f, 1f);
            }
            else
            {
                v.IconImage.color = new Color(0.25f, 0.28f, 0.30f, 0.48f);
                v.Background.color = new Color(0.05f, 0.07f, 0.08f, 0.72f);
                v.LabelText.text = "LOCKED";
                v.LabelText.color = new Color(0.55f, 0.58f, 0.60f, 0.75f);
            }
        }

        private void HandleSkillUnlocked(SkillData skill)
        {
            if (skill == null) return;
            if (_player != null && _player.Skills != null && !_skillSlots.ContainsKey(skill.skillType))
            {
                BuildSkillSlots(_player.Skills);
            }
            RefreshSkillVisual(skill, true);
        }

        private void HandleSkillCooldown(SkillType type, float remaining, float total)
        {
            SkillSlotVisual v;
            if (!_skillSlots.TryGetValue(type, out v) || v.CooldownOverlay == null) return;
            v.CooldownOverlay.fillAmount = total > 0.01f ? Mathf.Clamp01(remaining / total) : 0f;
        }

        private void HandleNotification(string title, string subtitle)
        {
            if (notificationRoot == null) return;
            if (_notifyCoroutine != null) StopCoroutine(_notifyCoroutine);
            _notifyCoroutine = StartCoroutine(ShowTemporaryBanner(notificationRoot, notificationTitleText, notificationSubText, title, subtitle, 3.6f));
        }

        private void HandleAreaEntered(string areaName, string subtitle)
        {
            if (areaBannerRoot == null) return;
            if (_areaCoroutine != null) StopCoroutine(_areaCoroutine);
            _areaCoroutine = StartCoroutine(ShowTemporaryBanner(areaBannerRoot, areaBannerTitleText, areaBannerSubText, areaName.ToUpper(), subtitle, 3.2f));
        }

        private IEnumerator ShowTemporaryBanner(GameObject root, Text titleText, Text subText, string title, string subtitle, float duration)
        {
            root.SetActive(true);
            if (titleText != null) titleText.text = title;
            if (subText != null) subText.text = subtitle;
            yield return new WaitForSecondsRealtime(duration);
            if (root != null) root.SetActive(false);
        }

        public static Font LoadSafeFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
