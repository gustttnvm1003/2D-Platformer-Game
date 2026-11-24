using UnityEngine;

namespace SilkenGrotto
{
    [CreateAssetMenu(fileName = "EnemyStatsData", menuName = "Silken Grotto/Enemies/Enemy Stats Data")]
    public class EnemyStatsData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName = "Crimson Lancer";
        public bool isElite = false;

        [Header("Vitals & Rewards")]
        [Min(1)] public int maxHealth = 45;
        [Min(1)] public int silkEssenceReward = 20;
        [Min(1)] public int orbDropCount = 3;

        [Header("Locomotion & Patrol")]
        [Min(0.5f)] public float patrolSpeed = 2.4f;
        [Min(0.5f)] public float chaseSpeed = 4.4f;
        [Min(0.5f)] public float patrolDistance = 4.5f;
        [Min(0.2f)] public float idlePauseDuration = 1.15f;

        [Header("Detection & Attack")]
        [Min(1f)] public float detectRange = 7.5f;
        [Min(1f)] public float loseTargetRange = 11.5f;
        [Min(0.5f)] public float attackRange = 2.05f;
        [Min(1)] public int attackDamage = 1;
        public float attackKnockbackForce = 7.5f;

        [Header("Attack Telegraph & Cadence")]
        [Min(0.1f)] public float windupDuration = 0.42f;
        [Min(0.1f)] public float attackActiveDuration = 0.24f;
        [Min(0.1f)] public float recoveryDuration = 0.45f;
        [Min(0.2f)] public float attackCooldown = 1.25f;
        public float lungeSpeed = 7.2f;

        [Header("Reaction")]
        [Range(0f, 0.9f)] public float knockbackResistance = 0.15f;
        [Min(0.05f)] public float staggerDuration = 0.28f;
    }
}
