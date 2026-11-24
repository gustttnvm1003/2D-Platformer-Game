using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Frost crystal shard & white light energy wave projectile for Mount Fay enemies
    /// (Servitor Boran white upper energy wave & Mnemonord aimed ice crystal shard).
    /// Deflectable by player attacks and shatters into icy crystal particles on collision.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class FrostCrystalProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector2 _velocity;
        private float _gravity;
        private int _damage = 1;
        private float _knockback = 6.8f;
        private float _radius = 0.28f;
        private GameObject _source;
        private float _lifeTimer = 2.8f;
        private bool _orientToVelocity = true;
        private bool _isLightWave;
        private bool _consumed;

        public void Launch(
            Vector2 initialVelocity,
            float gravity,
            int damage,
            float knockback,
            float colliderRadius,
            bool orientToVelocity,
            bool isLightWave,
            GameObject source,
            Sprite sprite)
        {
            _velocity = initialVelocity;
            _gravity = gravity;
            _damage = Mathf.Max(1, damage);
            _knockback = knockback;
            _radius = Mathf.Max(0.16f, colliderRadius);
            _orientToVelocity = orientToVelocity;
            _isLightWave = isLightWave;
            _source = source;
            _lifeTimer = 3.0f;
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
                col.radius = _radius;
            }
            gameObject.layer = GameConstants.LayerEnemyAttack;
            UpdateOrientation();
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

            if (_gravity > 0.01f)
            {
                _velocity.y -= _gravity * dt;
            }
            transform.position += (Vector3)(_velocity * dt);
            UpdateOrientation();
        }

        private void UpdateOrientation()
        {
            if (!_orientToVelocity || _velocity.sqrMagnitude < 0.001f) return;
            if (_isLightWave)
            {
                // Keep horizontal wave crescent upright while flipping scale along X
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
                    ShatterAndDestroy();
                }
                return;
            }

            if (layer == GameConstants.LayerPlayerAttack)
            {
                ShatterAndDestroy();
                return;
            }

            if (layer == GameConstants.LayerGround || layer == GameConstants.LayerBreakableWall)
            {
                ShatterAndDestroy();
            }
        }

        private void ShatterAndDestroy()
        {
            _consumed = true;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnCrystalIceHitBurst(transform.position, false);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPropBreak(BreakablePropKind.IceCrystalCluster);
            }
            Destroy(gameObject);
        }
    }
}
