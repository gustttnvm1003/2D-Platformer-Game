using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Tracks Silk Essence currency and attribute upgrade tiers using ScriptableObject definitions.
    /// Applies safety bounds and immediately refreshes live PlayerStats values.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [Header("Upgrade Definitions (ScriptableObjects)")]
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();
        [SerializeField] private PlayerStats playerStats;

        private readonly Dictionary<UpgradeType, UpgradeData> _upgradeMap = new Dictionary<UpgradeType, UpgradeData>();
        private readonly Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();
        private int _silkEssence;

        public IList<UpgradeData> Upgrades { get { return upgrades; } }
        public int SilkEssence { get { return _silkEssence; } }

        private void Awake()
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            RebuildMap();
            ResetUpgrades();
        }

        public void SetUpgradeCatalog(List<UpgradeData> catalog)
        {
            upgrades = catalog != null ? catalog : new List<UpgradeData>();
            RebuildMap();
            ResetUpgrades();
        }

        private void RebuildMap()
        {
            _upgradeMap.Clear();
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeData u = upgrades[i];
                if (u == null) continue;
                _upgradeMap[u.upgradeType] = u;
                if (!_levels.ContainsKey(u.upgradeType))
                {
                    _levels[u.upgradeType] = 0;
                }
            }
        }

        public void ResetUpgrades()
        {
            _silkEssence = 0;
            _levels.Clear();
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null) _levels[upgrades[i].upgradeType] = 0;
            }
            ApplyAllToStats(true);
            GameEvents.RaiseSilkEssenceChanged(_silkEssence);
        }

        public void AddSilkEssence(int amount)
        {
            if (amount <= 0) return;
            _silkEssence = Mathf.Min(999999, _silkEssence + amount);
            GameEvents.RaiseSilkEssenceChanged(_silkEssence);
        }

        public void SetSilkEssence(int amount)
        {
            _silkEssence = Mathf.Clamp(amount, 0, 999999);
            GameEvents.RaiseSilkEssenceChanged(_silkEssence);
        }

        public int GetLevel(UpgradeType type)
        {
            int lvl;
            return _levels.TryGetValue(type, out lvl) ? lvl : 0;
        }

        public UpgradeData GetUpgradeData(UpgradeType type)
        {
            UpgradeData data;
            return _upgradeMap.TryGetValue(type, out data) ? data : null;
        }

        public int GetNextCost(UpgradeType type)
        {
            UpgradeData data = GetUpgradeData(type);
            if (data == null) return -1;
            return data.GetCostForNextLevel(GetLevel(type));
        }

        public float GetTotalBonus(UpgradeType type)
        {
            UpgradeData data = GetUpgradeData(type);
            if (data == null) return 0f;
            return data.GetTotalBonus(GetLevel(type));
        }

        public bool CanPurchaseUpgrade(UpgradeType type)
        {
            UpgradeData data = GetUpgradeData(type);
            if (data == null) return false;
            int current = GetLevel(type);
            if (current >= data.maxLevel) return false;
            int cost = data.GetCostForNextLevel(current);
            return cost >= 0 && _silkEssence >= cost;
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            if (!CanPurchaseUpgrade(type)) return false;

            UpgradeData data = GetUpgradeData(type);
            int current = GetLevel(type);
            int cost = data.GetCostForNextLevel(current);

            _silkEssence -= cost;
            int nextLevel = Mathf.Clamp(current + 1, 0, data.maxLevel);
            _levels[type] = nextLevel;

            ApplyAllToStats(type == UpgradeType.MaxHealth || type == UpgradeType.MaxSilkEnergy);

            GameEvents.RaiseSilkEssenceChanged(_silkEssence);
            GameEvents.RaiseUpgradePurchased(type, nextLevel);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
            return true;
        }

        public void ApplyAllToStats(bool refillVitals)
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (playerStats == null) return;

            playerStats.ApplyUpgradeBonuses(
                Mathf.RoundToInt(GetTotalBonus(UpgradeType.MaxHealth)),
                Mathf.RoundToInt(GetTotalBonus(UpgradeType.AttackDamage)),
                GetTotalBonus(UpgradeType.MovementSpeed),
                GetTotalBonus(UpgradeType.JumpControl),
                GetTotalBonus(UpgradeType.MaxSilkEnergy),
                GetTotalBonus(UpgradeType.Defense),
                refillVitals
            );
        }

        public List<UpgradeSaveEntry> ExportUpgradeLevels()
        {
            List<UpgradeSaveEntry> list = new List<UpgradeSaveEntry>();
            foreach (KeyValuePair<UpgradeType, int> kv in _levels)
            {
                list.Add(new UpgradeSaveEntry { upgradeType = kv.Key.ToString(), level = kv.Value });
            }
            return list;
        }

        public void ImportUpgradeLevels(List<UpgradeSaveEntry> entries)
        {
            _levels.Clear();
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null) _levels[upgrades[i].upgradeType] = 0;
            }

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    try
                    {
                        UpgradeType t = (UpgradeType)System.Enum.Parse(typeof(UpgradeType), entries[i].upgradeType, true);
                        UpgradeData d = GetUpgradeData(t);
                        int maxL = d != null ? d.maxLevel : 5;
                        _levels[t] = Mathf.Clamp(entries[i].level, 0, maxL);
                    }
                    catch
                    {
                        // Ignore invalid entries cleanly
                    }
                }
            }

            ApplyAllToStats(false);
        }
    }
}
