using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Deals contact damage when overlapping the player's hurtbox/body,
    /// with temporary suppression on downward pogo strikes so pogo bounces cleanly.
    /// </summary>
    public class EnemyContactHazard : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float knockbackForce = 6.2f;
        [SerializeField] private float staggerDuration = 0.18f;
        [SerializeField] private float contactCooldown = 0.45f;

        private float _suppressedUntil;
        private float _nextContactTime;

        private void Awake()
        {
            if (enemyController == null) enemyController = GetComponentInParent<EnemyController>();
        }

        public void Configure(EnemyController owner, int damage, float knockForce)
        {
            enemyController = owner;
            contactDamage = Mathf.Max(0, damage);
            knockbackForce = knockForce;
        }

        public void SetContactDamage(int damage)
        {
            contactDamage = Mathf.Max(0, damage);
        }

        public void SuppressContactDamage(float duration)
        {
            float until = Time.time + Mathf.Max(0f, duration);
            if (until > _suppressedUntil) _suppressedUntil = until;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryInflictContact(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryInflictContact(other);
        }

        private void TryInflictContact(Collider2D other)
        {
            if (other == null || contactDamage <= 0) return;
            if (enemyController != null && !enemyController.IsAlive) return;
            if (Time.time < _suppressedUntil || Time.time < _nextContactTime) return;
            if (other.gameObject.layer != GameConstants.LayerPlayer && !other.CompareTag(GameConstants.TagPlayer)) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive || player.IsInvulnerable) return;

            float dirX = player.transform.position.x >= transform.position.x ? 1f : -1f;
            Vector2 knockDir = new Vector2(dirX, 0.45f).normalized;
            DamageInfo info = new DamageInfo(
                contactDamage,
                knockDir,
                knockbackForce,
                staggerDuration,
                gameObject,
                transform.position,
                false
            );

            if (player.TakeDamage(info))
            {
                _nextContactTime = Time.time + contactCooldown;
            }
        }
    }
}
