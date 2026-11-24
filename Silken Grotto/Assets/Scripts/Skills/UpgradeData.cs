using UnityEngine;

namespace SilkenGrotto
{
    [CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Silken Grotto/Upgrades/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Identity")]
        public UpgradeType upgradeType = UpgradeType.MaxHealth;
        public string displayName = "Shell Vitality";
        [TextArea(2, 3)]
        public string description = "Increases Maximum Health Masks.";

        [Header("Progression & Caps")]
        [Min(1)] public int maxLevel = 5;
        [Min(1)] public int baseCost = 30;
        [Min(0)] public int costPerLevel = 25;
        public float bonusPerLevel = 1f;
        public string unitLabel = "HP";

        public int GetCostForNextLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return -1;
            return baseCost + (currentLevel * costPerLevel);
        }

        public float GetTotalBonus(int currentLevel)
        {
            int clamped = Mathf.Clamp(currentLevel, 0, maxLevel);
            return clamped * bonusPerLevel;
        }
    }
}
