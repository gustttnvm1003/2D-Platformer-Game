using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Spinning bronze bell projectile thrown along a curved parabolic arc by Pilgrim Bellbearer.
    /// - 100% code-drawn bronze bell silhouette & spinning arc trail (zero image files required; also supports optional SpriteRenderer).
    /// - Runs 60 FPS ballistic physics, deals 1 damage to player, chimes & shatters on walls/ground.
    /// - Supports canDeflect: true so player melee slashes swat/deflect the bell back with metallic clang!
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class BellProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float gravity = 14.5f;
        [SerializeField] private float spinSpeed = 580f;
        [SerializeField] private float maxLifetime = 3.6f;

        private Vector2 _velocity;
        private int _damage = 1;
        private float _knockback = 6.2f;
        private bool _canDeflect = true;
        private bool _deflectedByPlayer;
        private GameObject _source;
        private float _lifeTimer;
        private bool _consumed;

        private LineRenderer _bellDome;
        private LineRenderer _bellClapper;
        private SpriteSheetVfxPlayer _bellVfx;
        private bool _usingSpriteSheet;

        public void Launch(Vector2 initialVelocity, int damage, float knockback, GameObject source, Sprite sprite)
        {
            LaunchParabolic(transform.position, initialVelocity, gravity, damage, knockback, true, source, sprite);
        }

        public void LaunchParabolic(
            Vector3 origin,
            Vector2 initialVelocity,
            float customGravity,
            int damage,
            float knockback,
            bool canDeflect,
            GameObject source,
            Sprite optionalSprite = null)
        {
            transform.position = origin;
            _velocity = initialVelocity;
            gravity = customGravity > 0.1f ? customGravity : 14.5f;
            _damage = Mathf.Max(1, damage);
            _knockback = knockback;
            _canDeflect = canDeflect;
            _deflectedByPlayer = false;
            _source = source;
            _lifeTimer = maxLifetime;
            _consumed = false;

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            Sprite[] bellSpinFrames = VFXManager.Instance != null ? VFXManager.Instance.GetBellSpinFrames() : null;
            if (spriteRenderer != null && optionalSprite != null)
            {
                _usingSpriteSheet = false;
                spriteRenderer.sprite = optionalSprite;
            }
            else if (bellSpinFrames != null && bellSpinFrames.Length > 0)
            {
                // Shared 2D hand-drawn Spinning Citadel Bell Projectile (Row 4 F1-4 @ 22 FPS)
                _usingSpriteSheet = true;
                GameObject bellGo = new GameObject("HandDrawnVFX_BellProjectile");
                bellGo.transform.SetParent(transform, false);
                bellGo.transform.localPosition = Vector3.zero;
                float flipX = initialVelocity.x < 0f ? -1f : 1f;
                float s = ENEMY_VFX_CONFIG.BellbearerBellSpin.DefaultScale;
                bellGo.transform.localScale = new Vector3(flipX * s, s, 1f);
                _bellVfx = bellGo.AddComponent<SpriteSheetVfxPlayer>();
                _bellVfx.ConfigureSorting(
                    ENEMY_VFX_CONFIG.BellbearerBellSpin.SortingOrder,
                    VFXManager.Instance.DefaultSpriteMaterial);
                _bellVfx.Play(bellSpinFrames, ENEMY_VFX_CONFIG.BellbearerBellSpin.Fps, true, false);
            }
            else
            {
                _usingSpriteSheet = false;
                BuildCodeBellVisual();
            }

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.28f;
            }
            gameObject.layer = GameConstants.LayerEnemyAttack;
        }

        private void BuildCodeBellVisual()
        {
            if (_bellDome != null) return;
            Material mat = ChoralCodeVFX.GetLineMaterial();

            // Crisp golden-bronze hand-drawn bell dome outline
            _bellDome = gameObject.AddComponent<LineRenderer>();
            _bellDome.useWorldSpace = false;
            _bellDome.sortingLayerName = GameConstants.SortVFX;
            _bellDome.sortingOrder = 17;
            if (mat != null) _bellDome.sharedMaterial = mat;
            _bellDome.positionCount = 9;
            _bellDome.startWidth = 0.075f;
            _bellDome.endWidth = 0.075f;
            Color bronzeGold = new Color(0.96f, 0.80f, 0.40f, 1f);
            Color warmRim = new Color(1.0f, 0.94f, 0.68f, 1f);
            _bellDome.startColor = warmRim;
            _bellDome.endColor = bronzeGold;
            _bellDome.SetPosition(0, new Vector3(-0.24f, -0.16f, 0f));
            _bellDome.SetPosition(1, new Vector3(-0.18f, 0.04f, 0f));
            _bellDome.SetPosition(2, new Vector3(-0.11f, 0.19f, 0f));
            _bellDome.SetPosition(3, new Vector3(0.00f, 0.25f, 0f));
            _bellDome.SetPosition(4, new Vector3(0.11f, 0.19f, 0f));
            _bellDome.SetPosition(5, new Vector3(0.18f, 0.04f, 0f));
            _bellDome.SetPosition(6, new Vector3(0.24f, -0.16f, 0f));
            _bellDome.SetPosition(7, new Vector3(0.00f, -0.19f, 0f));
            _bellDome.SetPosition(8, new Vector3(-0.24f, -0.16f, 0f));

            // Glowing inner clapper
            GameObject clapperGo = new GameObject("BellClapper");
            clapperGo.transform.SetParent(transform, false);
            _bellClapper = clapperGo.AddComponent<LineRenderer>();
            _bellClapper.useWorldSpace = false;
            _bellClapper.sortingLayerName = GameConstants.SortVFX;
            _bellClapper.sortingOrder = 18;
            if (mat != null) _bellClapper.sharedMaterial = mat;
            _bellClapper.positionCount = 2;
            _bellClapper.startWidth = 0.09f;
            _bellClapper.endWidth = 0.09f;
            _bellClapper.startColor = Color.white;
            _bellClapper.endColor = bronzeGold;
            _bellClapper.SetPosition(0, new Vector3(0f, -0.16f, 0f));
            _bellClapper.SetPosition(1, new Vector3(0f, -0.25f, 0f));
        }

        private void FixedUpdate()
        {
            if (_consumed) return;
            float dt = Time.fixedDeltaTime;
            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _velocity.y -= gravity * dt;
            transform.position += (Vector3)(_velocity * dt);
            if (!_usingSpriteSheet)
            {
                transform.Rotate(0f, 0f, -Mathf.Sign(_velocity.x == 0f ? 1f : _velocity.x) * spinSpeed * dt);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed || other == null) return;

            int layer = other.gameObject.layer;

            // Deflected bell can damage enemies on its return arc!
            if (_deflectedByPlayer && layer == GameConstants.LayerEnemy)
            {
                EnemyController enemy = other.GetComponentInParent<EnemyController>();
                if (enemy != null && enemy.IsAlive)
                {
                    DamageInfo reflectInfo = new DamageInfo(
                        12,
                        _velocity.sqrMagnitude > 0.01f ? _velocity.normalized : Vector2.right,
                        7.0f,
                        0.18f,
                        gameObject,
                        transform.position,
                        false);
                    enemy.TakeDamage(reflectInfo);
                    TriggerImpactAndDestroy();
                }
                return;
            }

            if (!_deflectedByPlayer && _source != null && (other.gameObject == _source || other.transform.IsChildOf(_source.transform)))
            {
                return;
            }

            // Player melee deflection when canDeflect == true!
            if (_canDeflect && !_deflectedByPlayer && layer == GameConstants.LayerPlayerAttack)
            {
                _deflectedByPlayer = true;
                gameObject.layer = GameConstants.LayerPlayerAttack;
                _velocity = new Vector2(-_velocity.x * 1.25f, Mathf.Max(5.8f, Mathf.Abs(_velocity.y) * 0.75f + 4.0f));
                if (_bellVfx != null)
                {
                    _bellVfx.SetTint(new Color(0.86f, 0.96f, 1.0f, 1f));
                }
                if (_bellDome != null)
                {
                    _bellDome.startColor = Color.white;
                    _bellDome.endColor = new Color(0.86f, 0.96f, 1.0f, 1f);
                }
                if (VFXManager.Instance == null || !VFXManager.Instance.SpawnBellImpactSonic(transform.position))
                {
                    ChoralCodeVFX.SpawnBlockSparks(transform.position, _velocity.x >= 0f ? 1 : -1);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.85f);
                    AudioManager.Instance.PlayArmorClink(0.92f);
                }
                GameEvents.RaiseCameraShake(0.025f, 0.045f);
                return;
            }

            if (!_deflectedByPlayer && (layer == GameConstants.LayerPlayer || other.CompareTag(GameConstants.TagPlayer)))
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
                    TriggerImpactAndDestroy();
                }
                return;
            }

            if (layer == GameConstants.LayerGround || layer == GameConstants.LayerBreakableWall)
            {
                TriggerImpactAndDestroy();
            }
        }

        private void TriggerImpactAndDestroy()
        {
            if (_consumed) return;
            _consumed = true;
            if (VFXManager.Instance == null || !VFXManager.Instance.SpawnBellImpactSonic(transform.position))
            {
                ChoralCodeVFX.SpawnBounceSparks(transform.position, Vector2.up);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.82f);
            }
            Destroy(gameObject);
        }
    }
}
