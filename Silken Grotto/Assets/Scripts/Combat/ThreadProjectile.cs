using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Choristor's mid-long range CastThread projectile:
    /// 100% code-drawn luminous white-gold silk thread strand/whip using LineRenderer (zero external image files needed!).
    /// Travels straight or slightly aimed toward player's last position, deflectable by player melee slashes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ThreadProjectile : MonoBehaviour
    {
        private Vector2 _velocity;
        private Vector3 _originPos;
        private int _damage = 1;
        private float _knockback = 6.6f;
        private float _lifeTimer = 1.4f;
        private float _maxRange = 12.2f;
        private float _strandLength = 1.95f;
        private float _age;
        private GameObject _source;
        private bool _consumed;

        private LineRenderer _coreStrand;
        private LineRenderer _glowStrand;
        private BoxCollider2D _col;

        public void Launch(
            Vector3 origin,
            Vector2 direction,
            float speed,
            int damage,
            float knockback,
            float maxRange,
            GameObject source)
        {
            _originPos = origin;
            transform.position = origin;
            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _velocity = dir * Mathf.Max(4f, speed);
            _damage = Mathf.Max(1, damage);
            _knockback = knockback;
            _maxRange = Mathf.Max(4f, maxRange);
            _lifeTimer = (_maxRange / Mathf.Max(4f, speed)) + 0.15f;
            _source = source;
            _consumed = false;
            _age = 0f;

            gameObject.layer = GameConstants.LayerEnemyAttack;

            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            _col = GetComponent<BoxCollider2D>();
            if (_col != null)
            {
                _col.isTrigger = true;
                _col.size = new Vector2(_strandLength * 0.85f, 0.34f);
                _col.offset = new Vector2(-_strandLength * 0.28f, 0f);
            }

            BuildCodeThreadVisuals();
        }

        private void BuildCodeThreadVisuals()
        {
            Material mat = ChoralCodeVFX.GetLineMaterial();

            _coreStrand = gameObject.AddComponent<LineRenderer>();
            _coreStrand.useWorldSpace = false;
            _coreStrand.sortingLayerName = GameConstants.SortVFX;
            _coreStrand.sortingOrder = 16;
            if (mat != null) _coreStrand.sharedMaterial = mat;
            _coreStrand.positionCount = 10;
            _coreStrand.startWidth = 0.025f;
            _coreStrand.endWidth = 0.095f;
            _coreStrand.startColor = new Color(1f, 0.98f, 0.92f, 0.30f);
            _coreStrand.endColor = Color.white;

            GameObject glowGo = new GameObject("SilkThreadGlow");
            glowGo.transform.SetParent(transform, false);
            _glowStrand = glowGo.AddComponent<LineRenderer>();
            _glowStrand.useWorldSpace = false;
            _glowStrand.sortingLayerName = GameConstants.SortVFX;
            _glowStrand.sortingOrder = 15;
            if (mat != null) _glowStrand.sharedMaterial = mat;
            _glowStrand.positionCount = 10;
            _glowStrand.startWidth = 0.07f;
            _glowStrand.endWidth = 0.22f;
            _glowStrand.startColor = new Color(1f, 0.93f, 0.68f, 0.12f);
            _glowStrand.endColor = new Color(1f, 0.95f, 0.78f, 0.68f);

            UpdateSilkWaveGeometry();
        }

        private void UpdateSilkWaveGeometry()
        {
            if (_coreStrand == null || _glowStrand == null) return;
            // Stretch smoothly out from launch tip into elongated taut silk whip
            float curLen = Mathf.Lerp(0.45f, _strandLength, Mathf.Clamp01(_age / 0.09f));
            int pts = 10;
            for (int i = 0; i < pts; i++)
            {
                float u = (float)i / (pts - 1); // 0 = trailing tail, 1 = sharp forward tip
                float x = Mathf.Lerp(-curLen, 0.16f, u);
                float env = Mathf.Sin(u * Mathf.PI);
                float waveY = Mathf.Sin(u * 10.5f - _age * 38f) * 0.075f * env;
                Vector3 p = new Vector3(x, waveY, 0f);
                _coreStrand.SetPosition(i, p);
                _glowStrand.SetPosition(i, p);
            }
        }

        private void Update()
        {
            if (_consumed) return;
            float dt = Time.deltaTime;
            _age += dt;
            _lifeTimer -= dt;

            transform.position += (Vector3)(_velocity * dt);
            UpdateSilkWaveGeometry();

            if (_lifeTimer <= 0f || Vector2.Distance(_originPos, transform.position) >= _maxRange)
            {
                DissipateAndDestroy();
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
                        new Vector2(dirX, 0.32f).normalized,
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

            // Deflectable by player needle attacks!
            if (layer == GameConstants.LayerPlayerAttack)
            {
                ChoralCodeVFX.SpawnBlockSparks(transform.position, _velocity.x >= 0f ? -1 : 1);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.75f);
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
            if (_consumed) return;
            _consumed = true;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnGoldenSilkHitBurst(transform.position, false);
            }
            Destroy(gameObject);
        }
    }
}
