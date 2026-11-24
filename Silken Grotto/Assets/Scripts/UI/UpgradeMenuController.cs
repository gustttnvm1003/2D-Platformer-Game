using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Interactive Attribute Upgrade & Skill Status screen.
    /// Allows players to spend Silk Essence on ScriptableObject-defined upgrades
    /// and inspect unlocked vs locked Metroidvania skills.
    /// </summary>
    public class UpgradeMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text essenceHeaderText;
        [SerializeField] private Text statsSummaryText;
        [SerializeField] private Transform upgradeListContainer;
        [SerializeField] private Text skillStatusSummaryText;
        [SerializeField] private Button closeButton;

        private PlayerController _player;
        private Font _font;

        private void Awake()
        {
            _font = HUDController.LoadSafeFont();
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleStateChanged;
            GameEvents.OnUpgradePurchased += HandleUpgradeChanged;
            GameEvents.OnSilkEssenceChanged += HandleEssenceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleStateChanged;
            GameEvents.OnUpgradePurchased -= HandleUpgradeChanged;
            GameEvents.OnSilkEssenceChanged -= HandleEssenceChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            bool show = state == GameState.UpgradeMenu;
            if (panelRoot != null) panelRoot.SetActive(show);
            if (show) RefreshDisplay();
        }

        private void HandleUpgradeChanged(UpgradeType type, int level)
        {
            if (panelRoot != null && panelRoot.activeSelf) RefreshDisplay();
        }

        private void HandleEssenceChanged(int amount)
        {
            if (panelRoot != null && panelRoot.activeSelf) RefreshDisplay();
        }

        private void OnCloseClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        public void RefreshDisplay()
        {
            if (_player == null) _player = FindObjectOfType<PlayerController>();
            if (_player == null) return;

            UpgradeManager upg = _player.Upgrades;
            PlayerStats st = _player.Stats;
            SkillManager sk = _player.Skills;

            if (essenceHeaderText != null && upg != null)
            {
                essenceHeaderText.text = string.Format("SILK ESSENCE: {0}", upg.SilkEssence);
            }

            if (statsSummaryText != null && st != null)
            {
                statsSummaryText.text = string.Format(
                    "ATTRIBUTES\nMax Health: {0} Masks\nAttack Damage: {1}\nWalk / Run Speed: {2:F1} / {3:F1}\nJump Force: {4:F1}  |  Air Control: {5:F2}x\nMax Silk: {6:F0}\nDefense Reduction: {7:F0}%",
                    st.MaxHealth,
                    st.AttackDamage,
                    st.WalkSpeed,
                    st.RunSpeed,
                    st.JumpForce,
                    st.AirControlMultiplier,
                    st.MaxSilk,
                    st.DamageReduction * 100f
                );
            }

            if (skillStatusSummaryText != null && sk != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("AWAKENED ABILITIES");
                IList<SkillData> list = sk.AllSkills;
                for (int i = 0; i < list.Count; i++)
                {
                    SkillData s = list[i];
                    if (s == null) continue;
                    bool unlocked = sk.IsUnlocked(s.skillType);
                    sb.AppendFormat(
                        "{0} {1} [{2}] — {3}\n",
                        unlocked ? "[UNLOCKED]" : "[LOCKED]  ",
                        s.displayName,
                        s.inputHint,
                        unlocked ? s.description : "Explore Moss Grotto to awaken."
                    );
                }
                skillStatusSummaryText.text = sb.ToString();
            }

            RebuildUpgradeRows(upg);
        }

        private void RebuildUpgradeRows(UpgradeManager upg)
        {
            if (upgradeListContainer == null || upg == null) return;

            for (int i = upgradeListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(upgradeListContainer.GetChild(i).gameObject);
            }

            IList<UpgradeData> list = upg.Upgrades;
            for (int i = 0; i < list.Count; i++)
            {
                UpgradeData data = list[i];
                if (data == null) continue;

                int level = upg.GetLevel(data.upgradeType);
                int nextCost = upg.GetNextCost(data.upgradeType);
                bool isMax = level >= data.maxLevel;
                bool canBuy = upg.CanPurchaseUpgrade(data.upgradeType);

                GameObject row = new GameObject("UpgradeRow_" + data.upgradeType);
                row.transform.SetParent(upgradeListContainer, false);
                RectTransform rrt = row.AddComponent<RectTransform>();
                rrt.sizeDelta = new Vector2(540f, 54f);
                Image rbg = row.AddComponent<Image>();
                rbg.color = new Color(0.09f, 0.14f, 0.16f, 0.92f);

                // Label text
                GameObject txtGo = new GameObject("Label");
                txtGo.transform.SetParent(row.transform, false);
                RectTransform trt = txtGo.AddComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.03f, 0.05f);
                trt.anchorMax = new Vector2(0.66f, 0.95f);
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
                Text txt = txtGo.AddComponent<Text>();
                txt.font = _font;
                txt.fontSize = 15;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = new Color(0.92f, 0.94f, 0.90f, 1f);
                txt.text = string.Format("{0}  (Lv {1}/{2})\n+{3} {4} per tier", data.displayName, level, data.maxLevel, data.bonusPerLevel, data.unitLabel);

                // Purchase Button
                GameObject btnGo = new GameObject("BuyBtn");
                btnGo.transform.SetParent(row.transform, false);
                RectTransform brt = btnGo.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.68f, 0.14f);
                brt.anchorMax = new Vector2(0.97f, 0.86f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Image bImg = btnGo.AddComponent<Image>();
                bImg.color = isMax
                    ? new Color(0.20f, 0.34f, 0.28f, 0.85f)
                    : (canBuy ? new Color(0.76f, 0.24f, 0.32f, 0.95f) : new Color(0.20f, 0.24f, 0.26f, 0.80f));

                Button btn = btnGo.AddComponent<Button>();
                btn.interactable = canBuy;
                UpgradeType capturedType = data.upgradeType;
                btn.onClick.AddListener(delegate
                {
                    if (_player != null && _player.Upgrades != null && _player.Upgrades.TryPurchaseUpgrade(capturedType))
                    {
                        if (GameManager.Instance != null) GameManager.Instance.SaveCurrentProgress();
                        RefreshDisplay();
                    }
                });

                GameObject bTxtGo = new GameObject("BtnText");
                bTxtGo.transform.SetParent(btnGo.transform, false);
                RectTransform btrt = bTxtGo.AddComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;
                Text bTxt = bTxtGo.AddComponent<Text>();
                bTxt.font = _font;
                bTxt.fontSize = 14;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.color = Color.white;
                bTxt.text = isMax ? "MAXED" : string.Format("UPGRADE ({0})", nextCost);
            }
        }
    }
}
