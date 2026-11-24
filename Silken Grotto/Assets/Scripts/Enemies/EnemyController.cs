using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Core Enemy physical body, health, hurtbox/hitbox wiring, platform ledge/wall sensors,
    /// knockback/stagger reaction, Silk Essence drops, and death handling.
    /// AI decision logic lives separately in CrimsonLancerAI / EnemyStateMachine.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] private string enemyId = "lancer_01";
        [SerializeField] private EnemyStatsData statsData;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Hurtbox hurtbox;
        [SerializeField] private Hitbox attackHitbox;

        [Header("Body & Behavior Flags")]
        [SerializeField] private bool isFlyingEnemy;
        [SerializeField] private bool isMossyCreature;
        [SerializeField] private bool isFrostCrystalCreature;
        [SerializeField] private bool isGoldenSilkCreature;
        [SerializeField] private bool isDrapeClothCreature;
        [SerializeField] private bool isBellBronzeCreature;
        [SerializeField] private bool isBoneCarapaceCreature;
        [SerializeField] private bool hasTopSpikyArmor;
        [SerializeField] private bool topArmorReflectsPogoBounce;
        [SerializeField] private float topArmorHeightOffset = 0.72f;
        [SerializeField] private float bodyCenterHeight = 0.8f;
        [SerializeField] private EnemyContactHazard contactHazard;

        [Header("Environment Sensors")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float ledgeProbeForward = 0.72f;
        [SerializeField] private float ledgeProbeDepth = 1.15f;
        [SerializeField] private float wallProbeDistance = 0.75f;
        [SerializeField] private float wallProbeHeight = 0.8f;

        [Header("Loot & Respawn")]
        [SerializeField] private GameObject silkOrbPrefab;
        [SerializeField] private Sprite silkOrbSprite;
        [SerializeField] private float respawnAfterSeconds = 0f;

        private Rigidbody2D _rb;
        private Collider2D _bodyCollider;
        private int _currentHealth;
        private bool _isAlive = true;
        private int _facingDirection = 1;
        private float _staggerTimer;
        private Vector3 _spawnPosition;
        private EnemyAIStateType _currentAIState = EnemyAIStateType.Idle;
        private bool _isTurning;
        private bool _isCharging;
        private bool _frontalGuardEnabled;
        private bool _allSideSpikeShieldActive;
        private float _nextNeedolinVulnerableTime;
        private Color _cachedSpriteBaseColor = Color.white;
        private bool _hasCachedSpriteColor;

        public string EnemyId { get { return enemyId; } }
        public EnemyStatsData Stats { get { return statsData; } }
        public bool IsAlive { get { return _isAlive; } }
        public bool IsInvulnerable { get { return false; } }
        public bool IsStaggered { get { return _staggerTimer > 0f; } }
        public int FacingDirection { get { return _facingDirection; } }
        public Vector3 SpawnPosition { get { return _spawnPosition; } }
        public float HorizontalVelocity { get { return _rb != null ? _rb.velocity.x : 0f; } }
        public float MovementSpeed
        {
            get
            {
                if (_rb == null) return 0f;
                return isFlyingEnemy ? _rb.velocity.magnitude : Mathf.Abs(_rb.velocity.x);
            }
        }
        public EnemyAIStateType CurrentAIState { get { return _currentAIState; } }
        public bool IsTurning { get { return _isTurning; } }
        public bool IsCharging { get { return _isCharging; } }
        public bool IsFrontalGuardActive { get { return _frontalGuardEnabled; } }
        public bool IsAllSideSpikeShieldActive { get { return _allSideSpikeShieldActive; } }
        public float RespawnAfterSeconds { get { return respawnAfterSeconds; } }

        public event System.Action<DamageInfo> OnDamaged;
        public event System.Action<DamageInfo> OnFrontalBlocked;
        public event System.Action<Vector3> OnTaunted;
        public event System.Action OnDied;
        public event System.Action OnRespawned;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _bodyCollider = GetComponent<Collider2D>();
            if (hurtbox == null) hurtbox = GetComponentInChildren<Hurtbox>();
            if (attackHitbox == null) attackHitbox = GetComponentInChildren<Hitbox>();
            if (contactHazard == null) contactHazard = GetComponentInChildren<EnemyContactHazard>();

            _rb.freezeRotation = true;
            if (isFlyingEnemy) _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (groundLayers.value == 0)
            {
                groundLayers = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform) | (1 << GameConstants.LayerBreakableWall);
            }

            if (attackHitbox != null)
            {
                attackHitbox.SetTargetLayers(1 << GameConstants.LayerPlayer);
                attackHitbox.Deactivate();
            }

            _spawnPosition = transform.position;
            _currentHealth = statsData != null ? statsData.maxHealth : 45;
            _isAlive = true;
        }

        public void Configure(string id, EnemyStatsData data, Sprite orbSprite)
        {
            enemyId = id;
            statsData = data;
            silkOrbSprite = orbSprite;
            _currentHealth = statsData != null ? statsData.maxHealth : 45;
        }

        public void SetAIState(EnemyAIStateType state)
        {
            _currentAIState = state;
        }

        public void SetTurnState(bool turning)
        {
            _isTurning = turning;
        }

        public void SetChargeState(bool charging)
        {
            _isCharging = charging;
        }

        public void SetFrontalGuard(bool guardEnabled)
        {
            _frontalGuardEnabled = guardEnabled;
        }

        public void SetAllSideSpikeShield(bool spikeShieldActive)
        {
            _allSideSpikeShieldActive = spikeShieldActive;
        }

        public void SetRespawnDelay(float seconds)
        {
            respawnAfterSeconds = Mathf.Max(0f, seconds);
        }

        private void Update()
        {
            if (!_isAlive) return;
            if (_staggerTimer > 0f)
            {
                _staggerTimer -= Time.deltaTime;
            }
        }

        public void SetHorizontalVelocity(float vx)
        {
            if (_rb == null || !_isAlive) return;
            _rb.velocity = new Vector2(vx, _rb.velocity.y);
        }

        public void StopHorizontal()
        {
            if (_rb == null) return;
            _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }

        public void SetFacing(int dir)
        {
            if (dir == 0) return;
            _facingDirection = dir > 0 ? 1 : -1;
            Transform target = visualRoot != null ? visualRoot : transform;
            Vector3 s = target.localScale;
            s.x = Mathf.Abs(s.x) * _facingDirection;
            target.localScale = s;
        }

        public bool HasGroundAhead(int direction)
        {
            int d = direction >= 0 ? 1 : -1;
            Vector2 origin = (Vector2)transform.position + new Vector2(d * ledgeProbeForward, 0.35f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, ledgeProbeDepth, groundLayers);
            return hit.collider != null;
        }

        public bool HasWallAhead(int direction)
        {
            int d = direction >= 0 ? 1 : -1;
            int solidMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, wallProbeHeight);
            RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(d, 0f), wallProbeDistance, solidMask);
            return hit.collider != null;
        }

        public void ActivateAttackHitbox()
        {
            float rangeX = statsData != null ? statsData.attackRange * 0.6f : 1.15f;
            ActivateCustomAttackHitbox(
                new Vector2(rangeX, 0.85f),
                new Vector2(1.75f, 1.1f),
                statsData != null ? statsData.attackDamage : 1,
                statsData != null ? statsData.attackKnockbackForce : 7.5f,
                new Color(0.92f, 0.32f, 0.36f, 0.88f),
                0.78f
            );
        }

        public void ActivateCustomAttackHitbox(
            Vector2 localForwardOffset,
            Vector2 hitboxSize,
            int customDamage,
            float customKnockback,
            Color slashColor,
            float slashScale)
        {
            if (attackHitbox == null || !_isAlive) return;

            bool visualFlipped = visualRoot != null && visualRoot.localScale.x < 0f;
            Vector2 localOffset = visualFlipped
                ? new Vector2(-localForwardOffset.x, localForwardOffset.y)
                : new Vector2(localForwardOffset.x * _facingDirection, localForwardOffset.y);

            DamageInfo info = new DamageInfo(
                Mathf.Max(1, customDamage),
                new Vector2(_facingDirection, 0.35f),
                customKnockback,
                0.24f,
                gameObject,
                transform.position
            );

            attackHitbox.Activate(localOffset, hitboxSize, info, null);

            if (VFXManager.Instance != null && slashScale > 0.05f)
            {
                Vector3 slashPos = transform.position + new Vector3(_facingDirection * localForwardOffset.x, localForwardOffset.y, 0f);
                VFXManager.Instance.SpawnSlashEffect(slashPos, _facingDirection, 0f, slashColor, slashScale);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(2);
        }

        public void DeactivateAttackHitbox()
        {
            if (attackHitbox != null)
            {
                attackHitbox.Deactivate();
            }
        }

        private float _lastArmorClinkTime = -10f;

        private bool IsBlockedByTopArmor(DamageInfo info)
        {
            if (!hasTopSpikyArmor) return false;
            if (info.IsDownwardPogo) return true;
            if (info.Source != null)
            {
                float srcY = info.Source.transform.position.y;
                float hitY = info.HitPoint.sqrMagnitude > 0.0001f ? info.HitPoint.y : transform.position.y;
                if (srcY >= transform.position.y + topArmorHeightOffset &&
                    hitY >= transform.position.y + topArmorHeightOffset * 0.65f)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsBlockedByFrontalGuard(DamageInfo info)
        {
            if (!_frontalGuardEnabled || info.IsDownwardPogo || info.BreaksGuard || info.Source == null) return false;
            float dx = info.Source.transform.position.x - transform.position.x;
            float dy = info.Source.transform.position.y - transform.position.y;
            // Strikes from behind OR aerial attacks from above head bypass frontal guard cleanly
            return dx * _facingDirection > 0.05f && dy < bodyCenterHeight + 0.72f;
        }

        /// <summary>
        /// Invoked by the player's Taunt (Garama!) sonic challenge ring to aggro and draw nearby enemies.
        /// </summary>
        public void ReceiveTauntProvoke(Vector3 taunterPos, GameObject taunter)
        {
            if (!_isAlive) return;
            int dir = taunterPos.x >= transform.position.x ? 1 : -1;
            SetFacing(dir);
            ChoralCodeVFX.SpawnTelegraphGlint(
                transform.position + Vector3.up * (Mathf.Max(0.75f, bodyCenterHeight) + 0.58f),
                new Color(1.0f, 0.90f, 0.45f, 0.95f),
                0.48f,
                0.24f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            if (_rb != null)
            {
                if (isFlyingEnemy)
                {
                    Vector2 pull = ((Vector2)(taunterPos - transform.position)).normalized;
                    _rb.velocity = pull * 3.8f;
                }
                else
                {
                    _rb.velocity = new Vector2(dir * 2.8f, Mathf.Max(_rb.velocity.y, 1.9f));
                }
            }
            if (OnTaunted != null) OnTaunted(taunterPos);
        }

        /// <summary>
        /// Invoked by the player's Needolin silk-violin resonance wave (00:06:29).
        /// Briefly entrances/dazes nearby enemies with floating musical glints (with immunity cooldown).
        /// </summary>
        public bool ReceiveNeedolinResonance(float dazeDuration)
        {
            if (!_isAlive || Time.time < _nextNeedolinVulnerableTime) return false;
            bool isElite = statsData != null && statsData.isElite;
            float appliedDur = isElite ? Mathf.Min(0.32f, dazeDuration * 0.45f) : dazeDuration;
            _nextNeedolinVulnerableTime = Time.time + (isElite ? 6.0f : 4.2f);
            _staggerTimer = Mathf.Max(_staggerTimer, appliedDur);
            if (_rb != null)
            {
                _rb.velocity = _rb.velocity * 0.18f;
            }
            ChoralCodeVFX.SpawnTelegraphGlint(
                transform.position + Vector3.up * (Mathf.Max(0.75f, bodyCenterHeight) + 0.62f),
                new Color(0.68f, 0.98f, 0.88f, 0.96f),
                0.52f,
                0.34f);
            return true;
        }

        /// <summary>
        /// Unified metallic block response invoked by EnemyFrontBlockBox or EnemyController frontal guard:
        /// 0 damage, metallic clang SFX, code-drawn spark burst, light knockback recoil on player, and AI notify.
        /// Rate-limited so overlapping FrontBlockBox + BodyHurtbox never double-trigger on the same swing.
        /// </summary>
        public void TriggerFrontalBlockFeedback(DamageInfo info, Vector3 sparkPosition)
        {
            if (Time.time - _lastArmorClinkTime <= 0.14f) return;
            _lastArmorClinkTime = Time.time;

            Vector3 sparkPos = sparkPosition.sqrMagnitude > 0.0001f
                ? sparkPosition
                : transform.position + new Vector3(_facingDirection * 0.72f, Mathf.Max(0.85f, bodyCenterHeight), 0f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayArmorClink(0.96f);
                AudioManager.Instance.PlayAttackSlash(1);
            }
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnHitImpact(sparkPos, new Color(1.0f, 0.90f, 0.45f, 1f), false, true);
            }
            ChoralCodeVFX.SpawnBlockSparks(sparkPos, _facingDirection);
            GameEvents.RaiseCameraShake(0.032f, 0.052f);

            // Light recoil knockback pushing the player back from the blocked blade
            if (info.Source != null)
            {
                PlayerMovement pm = info.Source.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    float pushDir = info.Source.transform.position.x >= transform.position.x ? 1f : -1f;
                    pm.ApplyKnockback(new Vector2(pushDir, 0.14f).normalized, 5.4f, 0.08f);
                }
            }

            if (_rb != null)
            {
                float resist = statsData != null ? statsData.knockbackResistance : 0.2f;
                _rb.velocity = new Vector2(-_facingDirection * (2.2f * (1f - resist * 0.5f)), _rb.velocity.y);
            }

            if (OnFrontalBlocked != null) OnFrontalBlocked(info);
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (!_isAlive) return false;

            if (IsBlockedByTopArmor(info))
            {
                if (Time.time - _lastArmorClinkTime > 0.14f)
                {
                    _lastArmorClinkTime = Time.time;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayArmorClink(0.88f);
                        if (topArmorReflectsPogoBounce) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell);
                    }
                    if (VFXManager.Instance != null)
                    {
                        Vector3 sparkPos = info.HitPoint.sqrMagnitude > 0.0001f
                            ? info.HitPoint
                            : transform.position + Vector3.up * Mathf.Max(0.8f, topArmorHeightOffset * 1.15f);
                        Color sparkTint = topArmorReflectsPogoBounce
                            ? new Color(0.98f, 0.80f, 0.42f, 1f)
                            : new Color(0.95f, 0.80f, 0.68f, 1f);
                        VFXManager.Instance.SpawnHitImpact(sparkPos, sparkTint, false, true);
                    }
                    GameEvents.RaiseCameraShake(0.025f, 0.045f);
                }
                if (topArmorReflectsPogoBounce && info.IsDownwardPogo)
                {
                    if (contactHazard != null) contactHazard.SuppressContactDamage(0.24f);
                    if (info.Source != null)
                    {
                        PlayerMovement pm = info.Source.GetComponent<PlayerMovement>();
                        if (pm != null) pm.ApplyPogoBounce();
                    }
                }
                return false;
            }

            // All-side crystal/metallic retaliation spike shield (Mnemonid / Mnemonord SpikeActive)
            if (_allSideSpikeShieldActive)
            {
                if (Time.time - _lastArmorClinkTime > 0.12f)
                {
                    _lastArmorClinkTime = Time.time;
                    Vector3 sparkPos = info.HitPoint.sqrMagnitude > 0.0001f
                        ? info.HitPoint
                        : transform.position + Vector3.up * bodyCenterHeight;
                    int normalX = info.Source != null && info.Source.transform.position.x < transform.position.x ? -1 : 1;
                    ChoralCodeVFX.SpawnBlockSparks(sparkPos, normalX);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.94f);
                    GameEvents.RaiseCameraShake(0.028f, 0.048f);
                }
                if (info.Source != null)
                {
                    PlayerMovement pm = info.Source.GetComponent<PlayerMovement>();
                    if (pm != null)
                    {
                        if (info.IsDownwardPogo)
                        {
                            if (contactHazard != null) contactHazard.SuppressContactDamage(0.24f);
                            pm.ApplyPogoBounce();
                        }
                        else
                        {
                            float pushDir = info.Source.transform.position.x >= transform.position.x ? 1f : -1f;
                            pm.ApplyKnockback(new Vector2(pushDir, 0.18f).normalized, 5.6f, 0.08f);
                        }
                    }
                }
                return false;
            }

            if (IsBlockedByFrontalGuard(info))
            {
                TriggerFrontalBlockFeedback(info, info.HitPoint);
                return false;
            }

            if (info.IsDownwardPogo && contactHazard != null)
            {
                // Suppress body/spike contact damage briefly so downward pogo bounces cleanly
                contactHazard.SuppressContactDamage(0.22f);
            }

            _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(1, info.Amount));
            DeactivateAttackHitbox();

            float resist = statsData != null ? statsData.knockbackResistance : 0.15f;
            float appliedKnock = info.KnockbackForce * (1f - resist);

            if (_currentHealth <= 0)
            {
                Die();
                return true;
            }

            if (isMossyCreature && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnMossEnemyHitBurst(transform.position + Vector3.up * bodyCenterHeight, false);
            }
            if (isFrostCrystalCreature && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnCrystalIceHitBurst(transform.position + Vector3.up * bodyCenterHeight, false);
            }
            if (isGoldenSilkCreature && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnGoldenSilkHitBurst(transform.position + Vector3.up * bodyCenterHeight, false);
            }
            if (isDrapeClothCreature && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnClothDustHitBurst(transform.position + Vector3.up * bodyCenterHeight, false);
            }
            if (isBellBronzeCreature && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnBellBronzeHitBurst(transform.position + Vector3.up * bodyCenterHeight, false);
            }
            if (isBoneCarapaceCreature && VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnBoneCarapaceHitBurst(transform.position + Vector3.up * bodyCenterHeight, false);
            }

            float stag = statsData != null ? Mathf.Max(info.StaggerDuration, statsData.staggerDuration) : info.StaggerDuration;
            _staggerTimer = stag * (1f - resist * 0.5f);

            if (_rb != null)
            {
                float kx = info.KnockbackDirection.x * appliedKnock;
                float ky = isFlyingEnemy
                    ? info.KnockbackDirection.y * appliedKnock
                    : Mathf.Clamp(info.KnockbackDirection.y * appliedKnock * 0.45f, 0f, 4.5f);
                _rb.velocity = new Vector2(kx, ky);
            }

            if (OnDamaged != null) OnDamaged(info);
            return true;
        }

        private void Die()
        {
            _isAlive = false;
            _isTurning = false;
            _isCharging = false;
            _frontalGuardEnabled = false;
            _allSideSpikeShieldActive = false;
            _currentAIState = EnemyAIStateType.Death;
            DeactivateAttackHitbox();

            if (contactHazard != null)
            {
                contactHazard.SuppressContactDamage(5f);
                contactHazard.enabled = false;
            }

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.simulated = false;
            }
            if (_bodyCollider != null) _bodyCollider.enabled = false;
            if (hurtbox != null) hurtbox.gameObject.SetActive(false);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyDeath();
            Vector3 centerPos = transform.position + Vector3.up * bodyCenterHeight;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnEnemyDeath(centerPos);
                if (isMossyCreature) VFXManager.Instance.SpawnMossEnemyHitBurst(centerPos, true);
                if (isFrostCrystalCreature) VFXManager.Instance.SpawnCrystalIceHitBurst(centerPos, true);
                if (isGoldenSilkCreature) VFXManager.Instance.SpawnGoldenSilkHitBurst(centerPos, true);
                if (isDrapeClothCreature) VFXManager.Instance.SpawnClothDustHitBurst(centerPos, true);
                if (isBellBronzeCreature) VFXManager.Instance.SpawnBellBronzeHitBurst(centerPos, true);
                if (isBoneCarapaceCreature) VFXManager.Instance.SpawnBoneCarapaceHitBurst(centerPos, true);
            }
            GameEvents.RaiseCameraShake(0.072f, 0.095f);
            GameEvents.RaiseEnemyDefeated(enemyId);

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null && statsData != null && statsData.isElite)
            {
                player.RecordDefeatedEnemy(enemyId);
                GameEvents.RaiseNotification("ELITE VANQUISHED", statsData.enemyName + " has fallen!");
            }

            SpawnLootOrbs();

            if (OnDied != null) OnDied();
            StartCoroutine(FadeAndRemoveAfterDeath());
        }

        public void ForceSetDefeatedFromSave()
        {
            _isAlive = false;
            gameObject.SetActive(false);
        }

        public void SpawnBonusLootOrbs(int count, int totalEssence)
        {
            int safeCount = Mathf.Clamp(count, 1, 16);
            int safeTotal = Mathf.Max(safeCount, totalEssence);
            int perOrb = Mathf.Max(1, safeTotal / safeCount);
            int remainder = safeTotal - (perOrb * safeCount);

            for (int i = 0; i < safeCount; i++)
            {
                int val = perOrb + (i == 0 ? remainder : 0);
                Vector3 pos = transform.position + new Vector3(Random.Range(-0.45f, 0.45f), 0.85f, 0f);
                GameObject orbObj;
                if (silkOrbPrefab != null)
                {
                    orbObj = Instantiate(silkOrbPrefab, pos, Quaternion.identity);
                }
                else
                {
                    orbObj = new GameObject("RosaryOrbDrop");
                    orbObj.transform.position = pos;
                    orbObj.transform.localScale = Vector3.one * 0.40f;
                    SpriteRenderer sr = orbObj.AddComponent<SpriteRenderer>();
                    sr.sprite = silkOrbSprite;
                    sr.sortingLayerName = GameConstants.SortInteractables;
                    sr.sortingOrder = 5;
                    CircleCollider2D col = orbObj.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.65f;
                    orbObj.AddComponent<SilkOrbPickup>();
                }

                SilkOrbPickup pickup = orbObj.GetComponent<SilkOrbPickup>();
                if (pickup != null)
                {
                    pickup.InitializeDrop(val, new Vector2(Random.Range(-3.4f, 3.4f), Random.Range(3.6f, 6.2f)));
                }
            }
        }

        private void SpawnLootOrbs()
        {
            int totalEssence = statsData != null ? statsData.silkEssenceReward : 20;
            int count = statsData != null ? Mathf.Max(1, statsData.orbDropCount) : 3;
            int perOrb = Mathf.Max(1, totalEssence / count);
            int remainder = totalEssence - (perOrb * count);

            for (int i = 0; i < count; i++)
            {
                int val = perOrb + (i == 0 ? remainder : 0);
                Vector3 pos = transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.7f, 0f);
                GameObject orbObj;
                if (silkOrbPrefab != null)
                {
                    orbObj = Instantiate(silkOrbPrefab, pos, Quaternion.identity);
                }
                else
                {
                    orbObj = new GameObject("SilkOrbDrop");
                    orbObj.transform.position = pos;
                    orbObj.transform.localScale = Vector3.one * 0.42f;
                    SpriteRenderer sr = orbObj.AddComponent<SpriteRenderer>();
                    sr.sprite = silkOrbSprite;
                    sr.sortingLayerName = GameConstants.SortInteractables;
                    sr.sortingOrder = 5;
                    CircleCollider2D col = orbObj.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.65f;
                    orbObj.AddComponent<SilkOrbPickup>();
                }

                SilkOrbPickup pickup = orbObj.GetComponent<SilkOrbPickup>();
                if (pickup != null)
                {
                    pickup.InitializeDrop(val, new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(3.2f, 5.2f)));
                }
            }
        }

        private IEnumerator FadeAndRemoveAfterDeath()
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && !_hasCachedSpriteColor)
            {
                _cachedSpriteBaseColor = sr.color;
                _hasCachedSpriteColor = true;
            }

            yield return new WaitForSeconds(respawnAfterSeconds > 0f ? 0.35f : 1.1f);
            if (sr != null)
            {
                Color c = _hasCachedSpriteColor ? _cachedSpriteBaseColor : sr.color;
                float fadeDur = respawnAfterSeconds > 0f ? 0.25f : 0.6f;
                float t = 0f;
                while (t < fadeDur)
                {
                    t += Time.deltaTime;
                    sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t / fadeDur));
                    yield return null;
                }
            }

            if (respawnAfterSeconds > 0f)
            {
                if (visualRoot != null && visualRoot != transform) visualRoot.gameObject.SetActive(false);
                float waitRem = Mathf.Max(0.2f, respawnAfterSeconds - 0.60f);
                yield return new WaitForSeconds(waitRem);
                RespawnAtAnchor(sr);
                yield break;
            }

            gameObject.SetActive(false);
        }

        private void RespawnAtAnchor(SpriteRenderer sr)
        {
            transform.position = _spawnPosition;
            _currentHealth = statsData != null ? statsData.maxHealth : 45;
            _isAlive = true;
            _isTurning = false;
            _isCharging = false;
            _frontalGuardEnabled = false;
            _allSideSpikeShieldActive = false;
            _staggerTimer = 0f;
            _currentAIState = EnemyAIStateType.Patrol;

            if (_rb != null)
            {
                _rb.simulated = true;
                _rb.velocity = Vector2.zero;
                if (isFlyingEnemy) _rb.gravityScale = 0f;
            }
            if (_bodyCollider != null) _bodyCollider.enabled = true;
            if (hurtbox != null)
            {
                hurtbox.gameObject.SetActive(true);
                hurtbox.ClearInvulnerability();
            }
            if (contactHazard != null)
            {
                contactHazard.enabled = true;
                contactHazard.SuppressContactDamage(0.25f);
            }
            if (visualRoot != null && visualRoot != transform)
            {
                visualRoot.gameObject.SetActive(true);
            }
            if (sr != null)
            {
                sr.color = _hasCachedSpriteColor ? _cachedSpriteBaseColor : Color.white;
            }

            ChoralCodeVFX.SpawnJoyfulFleaSparkles(transform.position);
            if (OnRespawned != null) OnRespawned();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            float det = statsData != null ? statsData.detectRange : 7.5f;
            float atk = statsData != null ? statsData.attackRange : 2.05f;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, det);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, atk);

            Gizmos.color = Color.green;
            Vector3 probeStart = transform.position + new Vector3(_facingDirection * ledgeProbeForward, 0.35f, 0f);
            Gizmos.DrawLine(probeStart, probeStart + Vector3.down * ledgeProbeDepth);
        }
    }
}
