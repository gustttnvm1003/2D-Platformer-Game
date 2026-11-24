using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Fast linear coral-stinger dart fired by Driznit along an aimed trajectory toward the player.
    /// Oriented automatically along its velocity vector, deflectable by needle slashes, and shatters on walls.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class StingerDartProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float gravity = 1.6f;
        [SerializeField] private float maxLifetime = 2.6f;
        [SerializeField] private int maxSurfaceBounces = 1;

        private Vector2 _velocity;
        private int _damage = 1;
        private float _knockback = 6.2f;
        private GameObject _source;
        private float _lifeTimer;
        private int _bouncesRemaining;
        private float _bounceCooldown;
        private bool _consumed;

        public void Launch(Vector2 initialVelocity, int damage, float knockback, GameObject source, Sprite sprite)
        {
            LaunchWithGravity(initialVelocity, gravity, maxSurfaceBounces, damage, knockback, source, sprite);
        }

        public void LaunchWithGravity(
            Vector2 initialVelocity,
            float customGravity,
            int bounces,
            int damage,
            float knockback,
            GameObject source,
            Sprite sprite)
        {
            _velocity = initialVelocity;
            gravity = customGravity;
            _bouncesRemaining = Mathf.Max(0, bounces);
            _bounceCooldown = 0f;
            _damage = Mathf.Max(1, damage);
            _knockback = knockback;
            _source = source;
            _lifeTimer = maxLifetime;
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
                col.radius = 0.20f;
            }
            gameObject.layer = GameConstants.LayerEnemyAttack;
            OrientAlongVelocity();
        }

        private void Update()
        {
            if (_consumed) return;
            float dt = Time.deltaTime;
            if (_bounceCooldown > 0f) _bounceCooldown -= dt;
            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _velocity.y -= gravity * dt;
            transform.position += (Vector3)(_velocity * dt);
            OrientAlongVelocity();
        }

        private void OrientAlongVelocity()
        {
            if (_velocity.sqrMagnitude < 0.001f) return;
            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
                        new Vector2(dirX, 0.30f).normalized,
                        _knockback,
                        0.16f,
                        _source != null ? _source : gameObject,
                        transform.position,
                        false
                    );
                    player.TakeDamage(info);
                    TriggerImpactAndDestroy(false);
                }
                return;
            }

            if (layer == GameConstants.LayerPlayerAttack)
            {
                TriggerImpactAndDestroy(true);
                return;
            }

            if (layer == GameConstants.LayerGround || layer == GameConstants.LayerBreakableWall)
            {
                if (_bounceCooldown <= 0f && _bouncesRemaining > 0 && _velocity.sqrMagnitude > 0.25f)
                {
                    _bouncesRemaining--;
                    _bounceCooldown = 0.05f;
                    int solidMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
                    Vector2 dirNorm = _velocity.normalized;
                    RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position - dirNorm * 0.45f, dirNorm, 1.25f, solidMask);
                    Vector2 normal = hit.collider != null
                        ? hit.normal
                        : (Mathf.Abs(_velocity.x) > Mathf.Abs(_velocity.y)
                            ? new Vector2(-Mathf.Sign(_velocity.x), 0.18f).normalized
                            : new Vector2(0f, -Mathf.Sign(_velocity.y)));

                    float dot = _velocity.x * normal.x + _velocity.y * normal.y;
                    _velocity = (_velocity - normal * (2f * dot)) * 0.90f;
                    transform.position += (Vector3)(normal * 0.14f);
                    OrientAlongVelocity();
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnHitImpact(transform.position, new Color(0.95f, 0.56f, 0.58f, 1f), false, true);
                    }
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.50f);
                    return;
                }
                TriggerImpactAndDestroy(true);
            }
        }

        private void TriggerImpactAndDestroy(bool metallicClink)
        {
            _consumed = true;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnHitImpact(transform.position, new Color(0.95f, 0.56f, 0.58f, 1f), false, true);
            }
            if (AudioManager.Instance != null)
            {
                if (metallicClink) AudioManager.Instance.PlayArmorClink(0.62f);
                else AudioManager.Instance.PlayEnemyHit();
            }
            Destroy(gameObject);
        }
    }
}
