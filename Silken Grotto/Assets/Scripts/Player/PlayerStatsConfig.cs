using UnityEngine;

namespace SilkenGrotto
{
    [CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "Silken Grotto/Characters/Player Stats Config")]
    public class PlayerStatsConfig : ScriptableObject
    {
        [Header("Base Vitals")]
        [Min(1)] public int baseMaxHealth = 5;
        [Min(10f)] public float baseMaxSilk = 100f;
        [Min(0f)] public float passiveSilkRegenPerSec = 3.5f;
        [Min(1f)] public float bindHealSilkCost = 35f;
        [Min(1)] public int bindHealMaskAmount = 2;

        [Header("Base Combat")]
        [Min(1)] public int baseAttackDamage = 12;
        [Range(0f, 0.6f)] public float baseDamageReduction = 0f;
        [Min(0.2f)] public float hurtInvulnerabilityDuration = 1.05f;

        [Header("Base Locomotion")]
        [Min(1f)] public float baseWalkSpeed = 6.2f;
        [Min(1f)] public float baseRunSpeed = 9.4f;
        [Min(1f)] public float baseJumpForce = 15.2f;
        [Min(0.1f)] public float baseAirControlMultiplier = 0.85f;

        [Header("Safety Attribute Caps")]
        public int maxHealthCap = 10;
        public int maxAttackDamageCap = 48;
        public float maxSpeedBonusCap = 4.0f;
        public float maxJumpBonusCap = 3.2f;
        public float maxSilkCap = 220f;
        [Range(0f, 0.55f)] public float maxDamageReductionCap = 0.45f;
    }
}
