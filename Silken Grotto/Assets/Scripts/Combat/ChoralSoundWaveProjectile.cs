using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Golden sonic bell resonance projectile launched either radially along marble floors by Choir Elder
    /// or diagonally from mid-air by Choir Flyer. Deflectable by player slashes and dissolves on walls.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class ChoralSoundWaveProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector2 _velocity;
        private int _damage = 1;
        private float _knockback = 7.0f;
        private float _lifeTimer = 2.4f;
        private GameObject _source;
        private bool _consumed;

        public void Launch(
            Vector2 velocity,
            int damage,
            float knockback,
            float lifetime,
            float colliderRadius,
            GameObject source,
            Sprite sprite)
        {
            _velocity = velocity;
            _damage = Mathf.Max(1, damage);
            _knockback = knockback;
            _lifeTimer = Mathf.Max(0.4f, lifetime);
            _source = source;
            _consumed = false;

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = Mathf.Max(0.22f, colliderRadius);
            }
            gameObject.layer = GameConstants.LayerEnemyAttack;
            OrientToTravel();
        }

        private void Update()
        {
            if (_consumed) return;
            float dt = Time.deltaTime;
            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (Vector3)(_velocity * dt);
        }

        private void OrientToTravel()
        {
            if (_velocity.sqrMagnitude < 0.001f) return;
            if (Mathf.Abs(_velocity.y) < 0.4f)
            {
                float sx = Mathf.Abs(transform.localScale.x);
                transform.localScale = new Vector3(_velocity.x >= 0f ? sx : -sx, transform.localScale.y, transform.localScale.z);
                transform.rotation = Quaternion.identity;
            }
            else
            {
                float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed || other == null) return;
            if (_source != null && (other.gameObject == _source || other.transform.IsChildOf(_source.transform))) return;

            int layer = other.gameObject.layer;

            if (layer == GameConstants.LayerPlayer || other.CompareTag(GameConstants.TagPlayer))
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                if (player != null && player.IsAlive && !player.IsInvulnerable)
                {
                    float dirX = _velocity.x >= 0f ? 1f : -1f;
                    DamageInfo info = new DamageInfo(
                        _damage,
                        new Vector2(dirX, 0.35f).normalized,
                        _knockback,
                        0.18f,
                        _source != null ? _source : gameObject,
                        transform.position,
                        false
                    );
                    player.TakeDamage(info);
                    DissipateAndDestroy();
                }
                return;
            }

            if (layer == GameConstants.LayerPlayerAttack)
            {
                DissipateAndDestroy();
                return;
            }

            if (layer == GameConstants.LayerGround || layer == GameConstants.LayerBreakableWall)
            {
                DissipateAndDestroy();
            }
        }

        private void DissipateAndDestroy()
        {
            _consumed = true;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnGoldenSilkHitBurst(transform.position, false);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell);
            }
            Destroy(gameObject);
        }
    }
}
