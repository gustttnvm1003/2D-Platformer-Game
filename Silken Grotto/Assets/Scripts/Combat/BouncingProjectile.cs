using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Driznit's 100% code-drawn BouncingProjectile (zero image files required!):
    /// - Travels along an aimed diagonal/slight-gravity trajectory toward the player's upper body/face
    /// - Reflects cleanly off floors, walls, and ceilings (vx = -vx / vy = -vy) with bounce spark VFX
    /// - Shatters after reaching maxBounces (default 3) or dealing 1 damage to the player
    /// - Supports canDeflect: true so player needle slashes parry/reverse the projectile!
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class BouncingProjectile : MonoBehaviour
    {
        private Vector2 _velocity;
        private float _gravity = 4.2f;
        private int _maxBounces = 3;
        private int _bounceCount;
        private float _restitution = 0.94f;
        private int _damage = 1;
        private float _knockback = 6.0f;
        private bool _canDeflect = true;
        private bool _deflectedByPlayer;
        private float _lifeTimer = 4.2f;
        private float _bounceCooldown;
        private float _age;
        private GameObject _source;
        private bool _consumed;
        private LayerMask _surfaceMask;

        private LineRenderer _coreBody;
        private LineRenderer _shellRing;
        private LineRenderer _trailLine;
        private Vector3[] _trailHistory;
        private SpriteSheetVfxPlayer _spinVfx;
        private bool _usingSpriteSheet;

        public void Launch(
            Vector3 origin,
            Vector2 initialVelocity,
            float gravity,
            int maxBounces,
            float restitution,
            int damage,
            float knockback,
            bool canDeflect,
            GameObject source)
        {
            transform.position = origin;
            _velocity = initialVelocity;
            _gravity = gravity;
            _maxBounces = Mathf.Max(0, maxBounces);
            _bounceCount = 0;
            _restitution = Mathf.Clamp(restitution, 0.5f, 1.05f);
            _damage = Mathf.Max(1, damage);
            _knockback = knockback;
            _canDeflect = canDeflect;
            _deflectedByPlayer = false;
            _lifeTimer = 4.5f;
            _bounceCooldown = 0f;
            _age = 0f;
            _source = source;
            _consumed = false;

            _surfaceMask = (1 << GameConstants.LayerGround)
                | (1 << GameConstants.LayerBreakableWall)
                | (1 << GameConstants.LayerOneWayPlatform);

            gameObject.layer = GameConstants.LayerEnemyAttack;

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.24f;
            }

            _trailHistory = new Vector3[6];
            for (int i = 0; i < _trailHistory.Length; i++)
            {
                _trailHistory[i] = origin;
            }

            BuildCodeProjectileVisuals();
            OrientAlongVelocity();
        }

        private void BuildCodeProjectileVisuals()
        {
            Material mat = ChoralCodeVFX.GetLineMaterial();

            // Prefer shared 2D hand-drawn Driznit Spinning Projectile (Row 3 F1-4 @ 24 FPS)!
            Sprite[] spinFrames = VFXManager.Instance != null ? VFXManager.Instance.GetDriznitSpinFrames() : null;
            if (spinFrames != null && spinFrames.Length > 0)
            {
                _usingSpriteSheet = true;
                GameObject spGo = new GameObject("HandDrawnVFX_DriznitProjectile");
                spGo.transform.SetParent(transform, false);
                spGo.transform.localPosition = Vector3.zero;
                spGo.transform.localScale = Vector3.one * ENEMY_VFX_CONFIG.DriznitProjectileSpin.DefaultScale;
                _spinVfx = spGo.AddComponent<SpriteSheetVfxPlayer>();
                _spinVfx.ConfigureSorting(
                    ENEMY_VFX_CONFIG.DriznitProjectileSpin.SortingOrder,
                    VFXManager.Instance.DefaultSpriteMaterial);
                _spinVfx.Play(spinFrames, ENEMY_VFX_CONFIG.DriznitProjectileSpin.Fps, true, false);
            }
            else
            {
                _usingSpriteSheet = false;
                // Fallback inner sharp coral-amber diamond dart
                _coreBody = gameObject.AddComponent<LineRenderer>();
                _coreBody.useWorldSpace = false;
                _coreBody.sortingLayerName = GameConstants.SortVFX;
                _coreBody.sortingOrder = 17;
                if (mat != null) _coreBody.sharedMaterial = mat;
                _coreBody.positionCount = 5;
                _coreBody.startWidth = 0.085f;
                _coreBody.endWidth = 0.085f;
                _coreBody.startColor = Color.white;
                _coreBody.endColor = new Color(1.0f, 0.86f, 0.56f, 1f);
                _coreBody.SetPosition(0, new Vector3(0.24f, 0f, 0f));
                _coreBody.SetPosition(1, new Vector3(-0.04f, 0.14f, 0f));
                _coreBody.SetPosition(2, new Vector3(-0.22f, 0f, 0f));
                _coreBody.SetPosition(3, new Vector3(-0.04f, -0.14f, 0f));
                _coreBody.SetPosition(4, new Vector3(0.24f, 0f, 0f));

                // Fallback outer glowing crimson-coral ring
                GameObject ringGo = new GameObject("CoralShellRing");
                ringGo.transform.SetParent(transform, false);
                _shellRing = ringGo.AddComponent<LineRenderer>();
                _shellRing.useWorldSpace = false;
                _shellRing.sortingLayerName = GameConstants.SortVFX;
                _shellRing.sortingOrder = 16;
                if (mat != null) _shellRing.sharedMaterial = mat;
                int segs = 13;
                _shellRing.positionCount = segs;
                _shellRing.startWidth = 0.055f;
                _shellRing.endWidth = 0.055f;
                Color coralRim = new Color(0.98f, 0.44f, 0.34f, 0.95f);
                _shellRing.startColor = coralRim;
                _shellRing.endColor = coralRim;
                for (int i = 0; i < segs; i++)
                {
                    float a = (float)i / (segs - 1) * Mathf.PI * 2f;
                    _shellRing.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.21f, Mathf.Sin(a) * 0.19f, 0f));
                }
            }

            // World-space velocity trail ribbon
            GameObject trailGo = new GameObject("BounceTrail");
            trailGo.transform.SetParent(transform, false);
            _trailLine = trailGo.AddComponent<LineRenderer>();
            _trailLine.useWorldSpace = true;
            _trailLine.sortingLayerName = GameConstants.SortVFX;
            _trailLine.sortingOrder = 15;
            if (mat != null) _trailLine.sharedMaterial = mat;
            _trailLine.positionCount = _trailHistory.Length;
            _trailLine.startWidth = 0.095f;
            _trailLine.endWidth = 0.012f;
            _trailLine.startColor = new Color(1.0f, 0.62f, 0.40f, 0.56f);
            _trailLine.endColor = new Color(1.0f, 0.45f, 0.32f, 0.03f);
            UpdateTrailVisual();
        }

        private void FixedUpdate()
        {
            if (_consumed) return;
            float dt = Time.fixedDeltaTime;
            _age += dt;
            if (_bounceCooldown > 0f) _bounceCooldown -= dt;
            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                ShatterAndDestroy(false);
                return;
            }

            // 60 FPS ballistic physics
            _velocity.y -= _gravity * dt;
            Vector2 step = _velocity * dt;
            float stepDist = step.magnitude;

            if (stepDist > 0.0001f)
            {
                Vector2 dir = step * (1f / stepDist);
                RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.20f, dir, stepDist + 0.03f, _surfaceMask);
                if (hit.collider != null && _bounceCooldown <= 0f)
                {
                    HandleSurfaceBounce(hit.point, hit.normal);
                }
                else
                {
                    transform.position += (Vector3)step;
                }
            }

            OrientAlongVelocity();
            UpdateTrailHistory();
        }

        private void HandleSurfaceBounce(Vector2 hitPoint, Vector2 normal)
        {
            if (_bounceCount >= _maxBounces)
            {
                transform.position = new Vector3(hitPoint.x, hitPoint.y, 0f);
                ShatterAndDestroy(true);
                return;
            }

            _bounceCount++;
            _bounceCooldown = 0.035f;

            // Exact tile/wall/floor/ceiling reflection (vx = -vx on walls, vy = -vy on floor/ceiling, smooth on slopes)
            if (Mathf.Abs(normal.x) >= 0.85f)
            {
                _velocity.x = -_velocity.x * _restitution;
            }
            else if (Mathf.Abs(normal.y) >= 0.85f)
            {
                _velocity.y = -_velocity.y * _restitution;
                // Keep a lively minimum rebound off floors so bounces stay readable & threatening
                if (normal.y > 0.5f && _velocity.y < 5.6f)
                {
                    _velocity.y = 5.6f;
                }
            }
            else
            {
                float dot = _velocity.x * normal.x + _velocity.y * normal.y;
                _velocity = (_velocity - normal * (2f * dot)) * _restitution;
            }

            transform.position = new Vector3(hitPoint.x + normal.x * 0.23f, hitPoint.y + normal.y * 0.23f, 0f);
            if (VFXManager.Instance == null || !VFXManager.Instance.SpawnDriznitRicochetSpark(hitPoint, normal))
            {
                ChoralCodeVFX.SpawnBounceSparks(hitPoint, normal);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayArmorClink(0.56f);
            }
        }

        private void UpdateTrailHistory()
        {
            if (_trailHistory == null) return;
            for (int i = _trailHistory.Length - 1; i > 0; i--)
            {
                _trailHistory[i] = _trailHistory[i - 1];
            }
            _trailHistory[0] = transform.position;
            UpdateTrailVisual();
        }

        private void UpdateTrailVisual()
        {
            if (_trailLine == null || _trailHistory == null) return;
            for (int i = 0; i < _trailHistory.Length; i++)
            {
                _trailLine.SetPosition(i, _trailHistory[i]);
            }
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

            int layer = other.gameObject.layer;

            // Deflected projectile can hit enemies!
            if (_deflectedByPlayer && layer == GameConstants.LayerEnemy)
            {
                EnemyController enemy = other.GetComponentInParent<EnemyController>();
                if (enemy != null && enemy.IsAlive)
                {
                    DamageInfo reflectInfo = new DamageInfo(
                        12,
                        _velocity.sqrMagnitude > 0.01f ? _velocity.normalized : Vector2.right,
                        6.8f,
                        0.18f,
                        gameObject,
                        transform.position,
                        false
                    );
                    enemy.TakeDamage(reflectInfo);
                    ShatterAndDestroy(true);
                }
                return;
            }

            if (!_deflectedByPlayer && _source != null && (other.gameObject == _source || other.transform.IsChildOf(_source.transform)))
            {
                return;
            }

            // Deflection by player's melee weapon slash!
            if (_canDeflect && !_deflectedByPlayer && layer == GameConstants.LayerPlayerAttack)
            {
                _deflectedByPlayer = true;
                _bounceCount = Mathf.Max(0, _maxBounces - 2);
                Vector2 away = new Vector2(-_velocity.x, Mathf.Abs(_velocity.y) * 0.45f + 2.2f);
                if (away.sqrMagnitude < 0.01f) away = Vector2.right;
                _velocity = away.normalized * Mathf.Max(13.5f, _velocity.magnitude * 1.25f);
                gameObject.layer = GameConstants.LayerPlayerAttack;

                if (_spinVfx != null)
                {
                    _spinVfx.SetTint(new Color(0.86f, 0.96f, 1.0f, 1f));
                }
                if (_shellRing != null)
                {
                    Color parryCol = new Color(0.95f, 0.98f, 1.0f, 1f);
                    _shellRing.startColor = parryCol;
                    _shellRing.endColor = parryCol;
                }
                if (_trailLine != null)
                {
                    _trailLine.startColor = new Color(0.88f, 0.96f, 1.0f, 0.85f);
                    _trailLine.endColor = new Color(0.70f, 0.88f, 1.0f, 0.06f);
                }

                if (VFXManager.Instance == null || !VFXManager.Instance.SpawnDriznitRicochetSpark(transform.position, _velocity.normalized))
                {
                    ChoralCodeVFX.SpawnBlockSparks(transform.position, _velocity.x >= 0f ? 1 : -1);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.92f);
                GameEvents.RaiseCameraShake(0.025f, 0.045f);
                return;
            }

            // Hit player (1 damage)
            if (!_deflectedByPlayer && (layer == GameConstants.LayerPlayer || other.CompareTag(GameConstants.TagPlayer)))
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
                    ShatterAndDestroy(false);
                }
                return;
            }

            // Fallback trigger bounce for overlapping thin colliders
            if ((layer == GameConstants.LayerGround || layer == GameConstants.LayerBreakableWall || layer == GameConstants.LayerOneWayPlatform)
                && _bounceCooldown <= 0f)
            {
                Vector2 fallbackNormal = Mathf.Abs(_velocity.x) > Mathf.Abs(_velocity.y)
                    ? new Vector2(-Mathf.Sign(_velocity.x), 0f)
                    : new Vector2(0f, -Mathf.Sign(_velocity.y));
                HandleSurfaceBounce(transform.position, fallbackNormal);
            }
        }

        private void ShatterAndDestroy(bool metallicClink)
        {
            if (_consumed) return;
            _consumed = true;

            if (VFXManager.Instance == null || !VFXManager.Instance.SpawnDriznitRicochetSpark(transform.position, Vector2.up))
            {
                ChoralCodeVFX.SpawnBounceSparks(transform.position, Vector2.up);
            }
            if (AudioManager.Instance != null)
            {
                if (metallicClink) AudioManager.Instance.PlayArmorClink(0.65f);
                else AudioManager.Instance.PlayEnemyHit();
            }
            Destroy(gameObject);
        }
    }
}
