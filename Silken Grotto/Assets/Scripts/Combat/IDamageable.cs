using UnityEngine;

namespace SilkenGrotto
{
    public struct DamageInfo
    {
        public int Amount;
        public Vector2 KnockbackDirection;
        public float KnockbackForce;
        public float StaggerDuration;
        public GameObject Source;
        public Vector3 HitPoint;
        public bool IsDownwardPogo;
        public bool BreaksGuard;

        public DamageInfo(
            int amount,
            Vector2 knockbackDirection,
            float knockbackForce,
            float staggerDuration,
            GameObject source,
            Vector3 hitPoint,
            bool isDownwardPogo = false,
            bool breaksGuard = false)
        {
            Amount = amount;
            KnockbackDirection = knockbackDirection.sqrMagnitude > 0.001f ? knockbackDirection.normalized : Vector2.right;
            KnockbackForce = knockbackForce;
            StaggerDuration = staggerDuration;
            Source = source;
            HitPoint = hitPoint;
            IsDownwardPogo = isDownwardPogo;
            BreaksGuard = breaksGuard;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        bool IsInvulnerable { get; }
        bool TakeDamage(DamageInfo info);
    }
}
