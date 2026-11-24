using UnityEngine;

namespace SilkenGrotto
{
    public enum KarakArchetype
    {
        Driznit,
        Conchfly,
        Pharlid,
        CoralFurm
    }

    /// <summary>
    /// Refactored 60 FPS Finite State Machine AI for the 4 Sands of Karak & Bellhart enemy profiles,
    /// driven by centralized ENEMY_COMBAT_CONFIG (preserving all existing reference sprites & 12-24 FPS sheets):
    /// 1. Driznit: medium spacing hoverer firing code-drawn BouncingProjectile (maxBounces=3, deflectable).
    /// 2. Conchfly: figure-8 aerial harasser with AimTelegraph (0.35s) + aim line -> FastDiagonalDart -> BrakeAndRecover (0.55s).
    /// 3. Pharlid (and Bellhart PharlidDiver): Ground SkitterPatrol/QuickLunge/SkitterAway OR CeilingDrop ambush
    ///    (CeilingCling upside-down -> 0.18s DropWarning + falling dust -> AmbushDrop -> ground FSM).
    /// 4. Coral Furm: grounded CrawlPatrol -> AlertHop/RollNudge -> ShellBrace, pogo-friendly, with signature
    ///    gravity coral shard bursts, layered coral crunch SFX, and 36ms hitstop on every hit & death.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class KarakEnemyAI : MonoBehaviour
    {
        [Header("Archetype & Spawn Mode")]
        [SerializeField] private KarakArchetype archetype = KarakArchetype.Pharlid;
        [SerializeField] private PharlidAmbushMode ambushMode = PharlidAmbushMode.Ground;

        [Header("Legacy Sprite Hook (Kept For Serialized Compatibility)")]
        [SerializeField] private Sprite dartProjectileSprite;

        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hoverAmplitude = 0.34f;
        [SerializeField] private float hoverFrequency = 3.2f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private HitStopController _hitStopController;
        private ENEMY_COMBAT_CONFIG.KarakProfile _profile;

        private LayerMask _solidTileMask;
        private LayerMask _groundAndPlatformMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private Vector3 _anchorPosition;
        private float _defaultGravityScale = 3.0f;

        private int _patrolDir = 1;
        private float _attackCooldownTimer;
        private float _animPhase;
        private float _pogoRecoilTimer;
        private float _throwRecoilSnapTimer;

        // Conchfly locked dart state
        private Vector2 _lockedAirDir = Vector2.right;
        private bool _isDarting;
        private bool _isBraking;
        private float _brakeProgress;

        // Pharlid ceiling ambush & skitter state
        private bool _isInvertedOnCeiling;
        private bool _isCeilingWarning;
        private bool _isAmbushDropping;
        private float _skitterPhaseTimer;
        private bool _skitterBurstActive = true;

        // Coral Furm shell tuck state
        private bool _isShellBracing;

        private CeilingClingState _ceilingClingState;
        private DropWarningState _dropWarningState;
        private AmbushDropState _ambushDropState;
        private PatrolState _patrolState;
        private ChaseState _chaseState;
        private WindupState _windupState;
        private AttackState _attackState;
        private RecoverState _recoverState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        public KarakArchetype Archetype { get { return archetype; } }
        public PharlidAmbushMode AmbushMode { get { return ambushMode; } }

        public void ConfigureArchetype(KarakArchetype kind, Sprite dartSprite)
        {
            archetype = kind;
            if (dartSprite != null) dartProjectileSprite = dartSprite;
            ENEMY_COMBAT_CONFIG.KarakProfile p = ENEMY_COMBAT_CONFIG.GetKarak(kind);
            if (p.HoverAmplitude > 0.01f) hoverAmplitude = p.HoverAmplitude;
            if (p.HoverFrequency > 0.01f) hoverFrequency = p.HoverFrequency;
        }

        public void ConfigureAmbushMode(PharlidAmbushMode mode)
        {
            ambushMode = mode;
        }

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _fsm = new EnemyStateMachine();
            _profile = ENEMY_COMBAT_CONFIG.GetKarak(archetype);

            _solidTileMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _groundAndPlatformMask = _solidTileMask | (1 << GameConstants.LayerOneWayPlatform);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.72f) % 6.28f;
            _patrolDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;
            _anchorPosition = transform.position;

            if (_rb != null)
            {
                _defaultGravityScale = _rb.gravityScale > 0.1f ? _rb.gravityScale : 3.0f;
            }

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualScale.y = Mathf.Abs(_baseVisualScale.y);
            _baseVisualLocalPos = visualTransform.localPosition;

            _ceilingClingState = new CeilingClingState(this);
            _dropWarningState = new DropWarningState(this);
            _ambushDropState = new AmbushDropState(this);
            _patrolState = new PatrolState(this);
            _chaseState = new ChaseState(this);
            _windupState = new WindupState(this);
            _attackState = new AttackState(this);
            _recoverState = new RecoverState(this);
            _staggerState = new StaggerState(this);
            _deathState = new DeathState(this);
        }

        private void OnEnable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged += HandleDamaged;
                _enemy.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged -= HandleDamaged;
                _enemy.OnDied -= HandleDied;
            }
        }

        private void Start()
        {
            _profile = ENEMY_COMBAT_CONFIG.GetKarak(archetype);
            _anchorPosition = transform.position;
            ResolvePlayer();
            _hitStopController = FindObjectOfType<HitStopController>();

            if (IsAirborneArchetype() && _rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.drag = 0f;
            }

            _enemy.SetFacing(_patrolDir);

            if (archetype == KarakArchetype.Pharlid && ambushMode == PharlidAmbushMode.CeilingDrop)
            {
                TransitionTo(_ceilingClingState);
            }
            else
            {
                TransitionTo(_patrolState);
            }
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_pogoRecoilTimer > 0f) _pogoRecoilTimer -= dt;
            if (_throwRecoilSnapTimer > 0f) _throwRecoilSnapTimer -= dt;
            _animPhase += dt;

            if (_enemy != null)
            {
                bool driznitRetreat = archetype == KarakArchetype.Driznit
                    && _fsm.CurrentState == _chaseState
                    && _rb != null
                    && (_rb.velocity.x * _enemy.FacingDirection < -0.45f);
                _enemy.SetChargeState(_isDarting || _isAmbushDropping || driznitRetreat || (archetype == KarakArchetype.CoralFurm && _fsm.CurrentState == _attackState));
                if (archetype == KarakArchetype.Pharlid)
                {
                    // Drives Row 3 (CeilingCling & AmbushDrop) cleanly via IsTurning!
                    _enemy.SetTurnState(_isInvertedOnCeiling || _isCeilingWarning || _isAmbushDropping);
                }
            }

            _fsm.Tick(dt);
            ApplyProceduralVisualJuice();
        }

        private void FixedUpdate()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            _fsm.FixedTick(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_enemy == null || !_enemy.IsAlive || collision == null) return;
            int l = collision.gameObject.layer;
            bool isSurface = l == GameConstants.LayerGround
                || l == GameConstants.LayerBreakableWall
                || l == GameConstants.LayerOneWayPlatform;
            if (!isSurface) return;

            if (archetype == KarakArchetype.Conchfly && _fsm.CurrentState == _attackState)
            {
                TriggerConchflyWallBrake();
            }
            else if (archetype == KarakArchetype.Pharlid && _fsm.CurrentState == _ambushDropState)
            {
                CompletePharlidAmbushLanding();
            }
        }

        private void TriggerConchflyWallBrake()
        {
            if (_rb != null) _rb.velocity = -_lockedAirDir * 3.2f;
            ChoralCodeVFX.SpawnBounceSparks(transform.position + (Vector3)(_lockedAirDir * 0.48f), -_lockedAirDir);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.70f);
            GameEvents.RaiseCameraShake(0.038f, 0.065f);
            TransitionTo(_recoverState);
        }

        private void CompletePharlidAmbushLanding()
        {
            _isAmbushDropping = false;
            _isInvertedOnCeiling = false;
            _isCeilingWarning = false;
            _anchorPosition = transform.position;
            if (_rb != null)
            {
                _rb.gravityScale = _defaultGravityScale;
                _rb.velocity = new Vector2(0f, 0f);
            }
            ChoralCodeVFX.SpawnChargeDust(transform.position, _enemy.FacingDirection, true);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
            _attackCooldownTimer = 0.25f;

            if (CanDetectPlayer()) TransitionTo(_chaseState);
            else TransitionTo(_patrolState);
        }

        private bool IsAirborneArchetype()
        {
            return archetype == KarakArchetype.Driznit || archetype == KarakArchetype.Conchfly;
        }

        private void ResolvePlayer()
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (pObj != null)
            {
                _playerTransform = pObj.transform;
                _playerController = pObj.GetComponent<PlayerController>();
            }
        }

        private void TransitionTo(IEnemyState next)
        {
            _fsm.ChangeState(next);
            if (_enemy != null && next != null)
            {
                _enemy.SetAIState(next.StateType);
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (!_enemy.IsAlive) return;

            // Signature Coral Furm "Coral Crunch" feedback on EVERY hit!
            if (archetype == KarakArchetype.CoralFurm)
            {
                Vector3 center = transform.position + new Vector3(0f, 0.65f, 0f);
                ChoralCodeVFX.SpawnCoralShardBurst(center, _profile.CoralShardCountHit, false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCoralCrunch(false);
                if (_hitStopController == null) _hitStopController = FindObjectOfType<HitStopController>();
                if (_hitStopController != null) _hitStopController.TriggerHitStop(_profile.CoralCrunchHitstop);
                GameEvents.RaiseCameraShake(0.044f, 0.072f);

                if (info.IsDownwardPogo)
                {
                    ChoralCodeVFX.SpawnMossPogoBurst(transform.position + new Vector3(0f, 1.1f, 0f), new Color(0.98f, 0.56f, 0.36f, 0.95f));
                }

                TransitionTo(_staggerState); // Enters brief ShellBrace recoil
                return;
            }

            // Pharlid knocked off ceiling immediately drops into active ground combat
            if (archetype == KarakArchetype.Pharlid && (_isInvertedOnCeiling || _isCeilingWarning))
            {
                _isInvertedOnCeiling = false;
                _isCeilingWarning = false;
                if (_rb != null) _rb.gravityScale = _defaultGravityScale;
            }

            // Smooth downward pogo bounce feedback for Driznit, Conchfly, and Pharlid
            if (info.IsDownwardPogo)
            {
                _pogoRecoilTimer = 0.18f;
                ChoralCodeVFX.SpawnMossPogoBurst(
                    transform.position + new Vector3(0f, IsAirborneArchetype() ? 0.45f : 0.95f, 0f),
                    new Color(0.98f, 0.78f, 0.52f, 0.95f));

                if (IsAirborneArchetype() && _rb != null)
                {
                    // Gentle controlled elastic dip so aerial pogos feel super responsive (esp. Conchfly BrakeAndRecover)
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.35f, -3.4f);
                }
            }

            // Conchfly maintains momentum mid-dart unless pogoed cleanly; easy to interrupt during BrakeAndRecover
            if (archetype == KarakArchetype.Conchfly && _fsm.CurrentState == _attackState && !info.IsDownwardPogo)
            {
                return;
            }

            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            _isDarting = false;
            _isBraking = false;
            _isInvertedOnCeiling = false;
            _isCeilingWarning = false;
            _isAmbushDropping = false;
            _isShellBracing = false;

            if (archetype == KarakArchetype.CoralFurm)
            {
                Vector3 center = transform.position + new Vector3(0f, 0.65f, 0f);
                ChoralCodeVFX.SpawnCoralShardBurst(center, _profile.CoralShardCountDeath, true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCoralCrunch(true);
                if (_hitStopController == null) _hitStopController = FindObjectOfType<HitStopController>();
                if (_hitStopController != null) _hitStopController.TriggerHitStop(0.040f);
            }

            TransitionTo(_deathState);
            if (visualTransform != null)
            {
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = new Vector3(_enemy.FacingDirection * _baseVisualScale.x, _baseVisualScale.y, _baseVisualScale.z);
            }
        }

        private bool IsPlayerAlive()
        {
            return _playerTransform != null && (_playerController == null || _playerController.IsAlive);
        }

        private Vector2 PlayerChestPos()
        {
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 0.98f) : (Vector2)transform.position;
        }

        private Vector2 PlayerFacePos()
        {
            // Driznit's iconic Silksong tendency to aim bouncing shots right at the player's face/upper torso
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 1.42f) : (Vector2)transform.position;
        }

        private float GetDistToPlayer()
        {
            return _playerTransform != null ? Vector2.Distance(transform.position, _playerTransform.position) : float.MaxValue;
        }

        private float GetVertDeltaToPlayer()
        {
            return _playerTransform != null ? Mathf.Abs(_playerTransform.position.y - transform.position.y) : float.MaxValue;
        }

        private int DirToPlayer()
        {
            if (_playerTransform == null) return _enemy.FacingDirection;
            return _playerTransform.position.x >= transform.position.x ? 1 : -1;
        }

        private void FacePlayer()
        {
            if (!IsPlayerAlive()) return;
            _enemy.SetFacing(DirToPlayer());
        }

        private bool CanDetectPlayer()
        {
            if (!IsPlayerAlive()) return false;
            float range = _profile.DetectRange > 0.1f ? _profile.DetectRange : 8.5f;
            float maxDy = IsAirborneArchetype() ? 8.5f : 3.6f;
            return GetDistToPlayer() <= range && GetVertDeltaToPlayer() <= maxDy;
        }

        private bool ShouldLosePlayer()
        {
            if (!IsPlayerAlive()) return true;
            float lose = _profile.LoseTargetRange > 0.1f ? _profile.LoseTargetRange : 13.0f;
            float maxDy = IsAirborneArchetype() ? 11.0f : 5.6f;
            return GetDistToPlayer() > lose || GetVertDeltaToPlayer() > maxDy;
        }

        private bool InTriggerAttackRange()
        {
            if (!IsPlayerAlive()) return false;
            float atkRng = _profile.AttackRange > 0.1f ? _profile.AttackRange : 4.2f;

            if (archetype == KarakArchetype.Conchfly)
            {
                Vector2 targetPos = PlayerChestPos();
                float dist = Vector2.Distance((Vector2)transform.position, targetPos);
                if (dist > atkRng || dist < 1.8f) return false;
                return true;
            }

            if (archetype == KarakArchetype.Driznit)
            {
                float d = GetDistToPlayer();
                return d <= atkRng && d >= 1.6f && GetVertDeltaToPlayer() <= 6.5f;
            }

            return GetDistToPlayer() <= atkRng && GetVertDeltaToPlayer() <= 2.4f;
        }

        private void FireBouncingProjectile()
        {
            if (!IsPlayerAlive()) return;
            Vector3 spawnPos = transform.position + new Vector3(_enemy.FacingDirection * 0.44f, 0.04f, 0f);
            Vector2 faceTarget = PlayerFacePos();
            Vector2 toFace = faceTarget - (Vector2)spawnPos;
            float dist = Mathf.Max(1.0f, toFace.magnitude);
            float spd = _profile.ProjectileSpeed > 0.5f ? _profile.ProjectileSpeed : 12.6f;
            float grav = _profile.ProjectileGravity;
            float flightTime = Mathf.Clamp(dist / spd, 0.12f, 0.85f);

            // Subtle gravity lead angled directly toward player's face so shots threaten jumps and corridor bounces
            Vector2 aimVec = toFace + new Vector2(0f, 0.5f * grav * flightTime * flightTime);
            Vector2 initialVel = (aimVec.sqrMagnitude > 0.001f ? aimVec.normalized : new Vector2(_enemy.FacingDirection, -0.15f).normalized) * spd;

            GameObject projGo = new GameObject("Driznit_BouncingProjectile");
            projGo.transform.position = spawnPos;
            projGo.AddComponent<CircleCollider2D>();
            BouncingProjectile proj = projGo.AddComponent<BouncingProjectile>();
            proj.Launch(
                spawnPos,
                initialVel,
                grav,
                _profile.MaxBounces,
                _profile.BounceRestitution,
                1,
                _profile.ContactKnockback,
                _profile.CanDeflectProjectile,
                gameObject);

            _throwRecoilSnapTimer = 0.16f;
            ChoralCodeVFX.SpawnBounceSparks(spawnPos, initialVel.normalized);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(1);
        }

        /// <summary>
        /// Adaptive Canvas/Transform procedural animation layer working seamlessly over both
        /// existing 12-24 FPS sprite sheets AND single-image sprites:
        /// - Driznit: pull-back tilt on ThrowWindup, forward snap recoil on ThrowBouncer
        /// - Conchfly: fast buzz tremor on AimTelegraph, velocity-aligned rotation on FastDiagonalDart, squash on BrakeAndRecover
        /// - Pharlid: agile horizontal skitter squash & stretch, inverted scaleY = -1 when clinging to ceiling
        /// - Coral Furm: dense crawl compression, ShellTuck flattening during ShellBrace / windup
        /// </summary>
        private void ApplyProceduralVisualJuice()
        {
            if (visualTransform == null) return;
            EnemyAIStateType st = _fsm.CurrentState != null ? _fsm.CurrentState.StateType : EnemyAIStateType.Idle;
            int dir = _enemy != null ? _enemy.FacingDirection : 1;

            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            float rotZ = 0f;
            Vector3 offset = Vector3.zero;

            if (st != EnemyAIStateType.Death)
            {
                switch (archetype)
                {
                    case KarakArchetype.Driznit:
                    {
                        float hoverY = Mathf.Sin(_animPhase * 4.4f) * 0.055f;
                        offset = new Vector3(0f, hoverY, 0f);
                        if (st == EnemyAIStateType.Windup)
                        {
                            // Pull-back tilt before throwing BouncingProjectile
                            rotZ = dir * 16f;
                            sx *= 0.92f;
                            sy *= 1.07f;
                            offset.x = -dir * 0.10f;
                        }
                        else if (_throwRecoilSnapTimer > 0f)
                        {
                            // Crisp forward snap thrust on projectile release
                            float snap = _throwRecoilSnapTimer / 0.16f;
                            rotZ = -dir * (18f * snap);
                            sx *= 1f + 0.14f * snap;
                            sy *= 1f - 0.10f * snap;
                            offset.x = dir * (0.14f * snap);
                        }
                        else
                        {
                            float vx = _rb != null ? _rb.velocity.x : 0f;
                            rotZ = Mathf.Clamp(-vx * 1.6f, -10f, 10f);
                        }
                        break;
                    }

                    case KarakArchetype.Conchfly:
                    {
                        if (st == EnemyAIStateType.Windup)
                        {
                            // Rapid jitter vibration during AimTelegraph + aligned nose angle
                            float jitterX = Mathf.Sin(_animPhase * 68f) * 0.055f;
                            float jitterY = Mathf.Cos(_animPhase * 76f) * 0.045f;
                            offset = new Vector3(jitterX, jitterY, 0f) - (Vector3)(_lockedAirDir * 0.10f);
                            float aimAngle = Mathf.Atan2(_lockedAirDir.y, Mathf.Max(0.05f, Mathf.Abs(_lockedAirDir.x))) * Mathf.Rad2Deg;
                            rotZ = dir * aimAngle;
                            sx *= 0.90f;
                            sy *= 1.10f;
                        }
                        else if (_isDarting && st == EnemyAIStateType.Attack)
                        {
                            // Sleek stretched rotation strictly aligned with locked dart vector
                            float dartAngle = Mathf.Atan2(_lockedAirDir.y, Mathf.Max(0.05f, Mathf.Abs(_lockedAirDir.x))) * Mathf.Rad2Deg;
                            rotZ = dir * dartAngle;
                            sx *= 1.18f;
                            sy *= 0.86f;
                        }
                        else if (_isBraking && st == EnemyAIStateType.Recover)
                        {
                            // Elastic mid-air braking squash during BrakeAndRecover (clear pogo cue!)
                            float sq = Mathf.Sin(_brakeProgress * Mathf.PI);
                            sx *= 1f - 0.16f * sq;
                            sy *= 1f + 0.18f * sq;
                            rotZ = dir * (10f * (1f - _brakeProgress));
                        }
                        else
                        {
                            offset = new Vector3(0f, Mathf.Sin(_animPhase * 3.8f) * 0.055f, 0f);
                            float vx = _rb != null ? _rb.velocity.x : 0f;
                            rotZ = Mathf.Clamp(-vx * 1.8f, -11f, 11f);
                        }
                        break;
                    }

                    case KarakArchetype.Pharlid:
                    {
                        if (_isInvertedOnCeiling)
                        {
                            // Hand-drawn pharlid_sheet.png Row 3 Frames 1-2 already render Pharlid upside-down on ceiling!
                            offset = new Vector3(0f, 0.25f, 0f);
                            if (_isCeilingWarning)
                            {
                                offset.x += Mathf.Sin(_animPhase * 78f) * 0.06f;
                                offset.y += Mathf.Cos(_animPhase * 64f) * 0.03f;
                            }
                        }
                        else if (_isAmbushDropping)
                        {
                            // Stretched vertical ambush plunge
                            sx *= 0.86f;
                            sy *= 1.20f;
                            rotZ = -dir * 12f;
                        }
                        else if (st == EnemyAIStateType.Windup)
                        {
                            // Coiled low crouch before QuickLunge
                            sx *= 1.14f;
                            sy *= 0.82f;
                            offset.x = Mathf.Sin(_animPhase * 58f) * 0.035f;
                        }
                        else
                        {
                            // Agile insectoid horizontal skitter squash & stretch
                            float spd = _rb != null ? Mathf.Abs(_rb.velocity.x) : 0f;
                            if (spd > 0.6f)
                            {
                                float pulse = Mathf.Sin(_animPhase * 22f);
                                sx *= 1f + 0.11f * pulse;
                                sy *= 1f - 0.09f * pulse;
                                rotZ = -dir * Mathf.Clamp(spd * 0.9f, 0f, 7f);
                            }
                        }
                        break;
                    }

                    case KarakArchetype.CoralFurm:
                    {
                        if (_isShellBracing || st == EnemyAIStateType.Stagger)
                        {
                            // ShellTuck: compressed wide & low when absorbing crunchy impact
                            sx *= 1.15f;
                            sy *= 0.78f;
                            offset = new Vector3(0f, -0.05f, 0f);
                        }
                        else if (st == EnemyAIStateType.Windup)
                        {
                            // Dense shell crouch before AlertHop / RollNudge
                            sx *= 1.12f;
                            sy *= 0.84f;
                            offset = new Vector3(Mathf.Sin(_animPhase * 46f) * 0.025f, -0.04f, 0f);
                        }
                        else if (st == EnemyAIStateType.Attack)
                        {
                            // Compact forward roll/nudge lean
                            sx *= 1.08f;
                            sy *= 0.92f;
                            rotZ = -dir * 7f;
                        }
                        else
                        {
                            // Dense rhythmic grounded crawl compression
                            float crawl = Mathf.Sin(_animPhase * 7.5f);
                            float moveAmt = (_rb != null && Mathf.Abs(_rb.velocity.x) > 0.3f) ? 1f : 0.35f;
                            sx *= 1f + 0.055f * crawl * moveAmt;
                            sy *= 1f - 0.05f * crawl * moveAmt;
                        }
                        break;
                    }
                }

                // Subtle elastic pogo squash bounce on any downward pogo hit
                if (_pogoRecoilTimer > 0f)
                {
                    float u = _pogoRecoilTimer / 0.18f;
                    float dip = Mathf.Sin(u * Mathf.PI);
                    sx *= 1f + 0.16f * dip;
                    sy *= 1f - 0.16f * dip;
                    offset.y -= 0.14f * dip;
                }
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // PHARLID CEILING AMBUSH STATES (CeilingCling -> DropWarning -> AmbushDrop)
        // =========================================================================

        private sealed class CeilingClingState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }
            public CeilingClingState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isInvertedOnCeiling = true;
                _ai._isCeilingWarning = false;
                _ai._isAmbushDropping = false;
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);

                if (_ai._rb != null)
                {
                    _ai._rb.gravityScale = 0f;
                    _ai._rb.velocity = Vector2.zero;
                }

                // Snugly align under nearby overhead ceiling/platform if present
                RaycastHit2D ceilHit = Physics2D.Raycast(_ai.transform.position, Vector2.up, 2.2f, _ai._groundAndPlatformMask);
                if (ceilHit.collider != null)
                {
                    _ai.transform.position = new Vector3(_ai.transform.position.x, ceilHit.point.y - 0.56f, _ai.transform.position.z);
                }
            }

            public void Tick(float dt)
            {
                if (!_ai.IsPlayerAlive()) return;
                float dx = Mathf.Abs(_ai._playerTransform.position.x - _ai.transform.position.x);
                float dyBelow = _ai.transform.position.y - _ai._playerTransform.position.y;
                float triggerW = _ai._profile.DropTriggerWidth > 0.2f ? _ai._profile.DropTriggerWidth : 2.65f;

                if (dx <= triggerW && dyBelow >= 0.45f && dyBelow <= 12.0f)
                {
                    _ai.FacePlayer();
                    _ai.TransitionTo(_ai._dropWarningState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb != null) _ai._rb.velocity = Vector2.zero;
            }

            public void Exit() { }
        }

        private sealed class DropWarningState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public DropWarningState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isInvertedOnCeiling = true;
                _ai._isCeilingWarning = true;
                _timer = _ai._profile.DropWarningDuration > 0.05f ? _ai._profile.DropWarningDuration : 0.18f;
                if (_ai._rb != null) _ai._rb.velocity = Vector2.zero;

                ChoralCodeVFX.SpawnCeilingDropDust(_ai.transform.position, 0.9f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._ambushDropState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb != null) _ai._rb.velocity = Vector2.zero;
            }

            public void Exit()
            {
                _ai._isCeilingWarning = false;
            }
        }

        private sealed class AmbushDropState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            private float _dropTimer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public AmbushDropState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isInvertedOnCeiling = false;
                _ai._isAmbushDropping = true;
                _dropTimer = 0f;
                _ai.FacePlayer();

                if (_ai._rb != null)
                {
                    _ai._rb.gravityScale = _ai._defaultGravityScale;
                    float dropSpd = _ai._profile.AmbushDropSpeed > 2f ? _ai._profile.AmbushDropSpeed : 16.5f;
                    float leadX = _ai.IsPlayerAlive()
                        ? Mathf.Clamp((_ai._playerTransform.position.x - _ai.transform.position.x) * 1.4f, -3.2f, 3.2f)
                        : 0f;
                    _ai._rb.velocity = new Vector2(leadX, -dropSpd);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
            }

            public void Tick(float dt)
            {
                _dropTimer += dt;
                if (_dropTimer >= 1.35f)
                {
                    _ai.CompletePharlidAmbushLanding();
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                float dropSpd = _ai._profile.AmbushDropSpeed > 2f ? _ai._profile.AmbushDropSpeed : 16.5f;
                if (_ai._rb.velocity.y > -dropSpd * 0.85f)
                {
                    _ai._rb.velocity = new Vector2(_ai._rb.velocity.x, -dropSpd);
                }

                // Raycast downward so Pharlid lands cleanly on both solid floors AND one-way platforms
                if (_dropTimer > 0.06f)
                {
                    RaycastHit2D floorHit = Physics2D.Raycast(_ai.transform.position, Vector2.down, 0.52f, _ai._groundAndPlatformMask);
                    if (floorHit.collider != null)
                    {
                        _ai.CompletePharlidAmbushLanding();
                    }
                }
            }

            public void Exit()
            {
                _ai._isAmbushDropping = false;
            }
        }

        // =========================================================================
        // SHARED / SPECIALIZED GROUND & AERIAL FSM STATES
        // =========================================================================

        private sealed class PatrolState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public PatrolState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDarting = false;
                _ai._isBraking = false;
                _ai._isShellBracing = false;
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
                _ai._skitterBurstActive = true;
                _ai._skitterPhaseTimer = 0.42f;
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.TransitionTo(_ai._chaseState);
                    return;
                }

                if (_ai.archetype == KarakArchetype.Pharlid)
                {
                    // Stop-and-dart insectoid skitter rhythm
                    _ai._skitterPhaseTimer -= dt;
                    if (_ai._skitterPhaseTimer <= 0f)
                    {
                        _ai._skitterBurstActive = !_ai._skitterBurstActive;
                        _ai._skitterPhaseTimer = _ai._skitterBurstActive
                            ? Random.Range(0.32f, 0.54f)
                            : Random.Range(0.20f, 0.36f);
                    }
                }

                if (!_ai.IsAirborneArchetype())
                {
                    float maxDist = _ai._profile.PatrolSpanX > 0.5f ? _ai._profile.PatrolSpanX : 4.2f;
                    float dx = _ai.transform.position.x - _ai._anchorPosition.x;
                    bool edge = !_ai._enemy.HasGroundAhead(_ai._patrolDir);
                    bool wall = _ai._enemy.HasWallAhead(_ai._patrolDir);
                    bool limit = (_ai._patrolDir > 0 && dx >= maxDist) || (_ai._patrolDir < 0 && dx <= -maxDist);
                    if (edge || wall || limit)
                    {
                        _ai._patrolDir = -_ai._patrolDir;
                        _ai._enemy.SetFacing(_ai._patrolDir);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborneArchetype())
                {
                    if (_ai._rb == null) return;
                    Vector2 anchor = _ai._anchorPosition;
                    Vector2 desiredPos;
                    if (_ai.archetype == KarakArchetype.Conchfly)
                    {
                        // Light aerial figure-8 patrol (x = sin(t), y = sin(2t))
                        float t = _ai._animPhase * 1.15f;
                        desiredPos = anchor + new Vector2(
                            Mathf.Sin(t) * _ai._profile.PatrolSpanX,
                            Mathf.Sin(t * 2.0f) * _ai._profile.HoverAmplitude);
                    }
                    else
                    {
                        // Driznit light hovering horizontal sway
                        desiredPos = anchor + new Vector2(
                            Mathf.Sin(_ai._animPhase * 1.65f) * _ai._profile.PatrolSpanX,
                            Mathf.Cos(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude);
                    }

                    Vector2 vel = (desiredPos - _ai._rb.position) * 3.0f;
                    if (Mathf.Abs(vel.x) > 0.22f) _ai._enemy.SetFacing(vel.x >= 0f ? 1 : -1);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 5.6f));
                    return;
                }

                if (!_ai._enemy.HasGroundAhead(_ai._patrolDir) || _ai._enemy.HasWallAhead(_ai._patrolDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                if (_ai.archetype == KarakArchetype.Pharlid && !_ai._skitterBurstActive)
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                float spd = _ai._profile.PatrolSpeed > 0.1f ? _ai._profile.PatrolSpeed : 2.2f;
                _ai._enemy.SetHorizontalVelocity(_ai._patrolDir * spd);
            }

            public void Exit() { }
        }

        private sealed class ChaseState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }
            public ChaseState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDarting = false;
                _ai._isBraking = false;
                _ai._isShellBracing = false;
                _ai._enemy.SetChargeState(_ai.archetype == KarakArchetype.Pharlid);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            }

            public void Tick(float dt)
            {
                if (_ai.ShouldLosePlayer())
                {
                    _ai.TransitionTo(_ai._patrolState);
                    return;
                }

                _ai.FacePlayer();

                if (_ai.InTriggerAttackRange() && _ai._attackCooldownTimer <= 0f)
                {
                    _ai.TransitionTo(_ai._windupState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                int dir = _ai.DirToPlayer();

                if (_ai.archetype == KarakArchetype.Driznit)
                {
                    if (_ai._rb == null || !_ai.IsPlayerAlive()) return;
                    // Hover/SkitterSpacing: hold medium range, smoothly back off if player pushes too close!
                    Vector2 facePos = _ai.PlayerFacePos();
                    float distX = Mathf.Abs(facePos.x - _ai._rb.position.x);
                    float idealX = Mathf.Clamp(distX, _ai._profile.IdealMinRange, _ai._profile.IdealMaxRange);
                    if (distX < _ai._profile.IdealMinRange) idealX = _ai._profile.IdealMinRange + 1.1f;

                    float targetX = facePos.x - dir * idealX;
                    float targetY = facePos.y + 1.05f + Mathf.Sin(_ai._animPhase * 2.9f) * 0.48f;
                    Vector2 toTarget = new Vector2(targetX - _ai._rb.position.x, targetY - _ai._rb.position.y);
                    Vector2 desired = toTarget.sqrMagnitude > 0.08f
                        ? toTarget.normalized * Mathf.Min(_ai._profile.AggroSpeed, toTarget.magnitude * 2.8f)
                        : Vector2.zero;

                    // Prevent backing into solid walls or ceilings
                    if (Mathf.Abs(desired.x) > 0.2f && Physics2D.Raycast(_ai._rb.position, new Vector2(Mathf.Sign(desired.x), 0f), 0.9f, _ai._solidTileMask).collider != null)
                    {
                        desired.x = 0f;
                    }
                    if (desired.y > 0.2f && Physics2D.Raycast(_ai._rb.position, Vector2.up, 0.9f, _ai._solidTileMask).collider != null)
                    {
                        desired.y = 0f;
                    }

                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, desired, Mathf.Clamp01(fixedDt * 6.5f));
                    return;
                }

                if (_ai.archetype == KarakArchetype.Conchfly)
                {
                    if (_ai._rb == null || !_ai.IsPlayerAlive()) return;
                    Vector2 p = _ai.PlayerChestPos();
                    Vector2 target = p + new Vector2(-dir * 5.8f, 1.95f + Mathf.Sin(_ai._animPhase * 2.6f) * 0.42f);
                    Vector2 toTarget = target - _ai._rb.position;
                    Vector2 desired = toTarget.sqrMagnitude > 0.10f ? toTarget.normalized * _ai._profile.AggroSpeed : Vector2.zero;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, desired, Mathf.Clamp01(fixedDt * 5.2f));
                    return;
                }

                // Grounded: Pharlid & CoralFurm
                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir) || _ai.InTriggerAttackRange())
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                float groundSpd = _ai._profile.AggroSpeed > 0.1f ? _ai._profile.AggroSpeed : 4.0f;
                _ai._enemy.SetHorizontalVelocity(dir * groundSpd);
            }

            public void Exit() { _ai._enemy.SetChargeState(false); }
        }

        private sealed class WindupState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            private float _timer;
            private bool _lockedOnce;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public WindupState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                _lockedOnce = false;
                if (!_ai.IsAirborneArchetype()) _ai._enemy.StopHorizontal();

                if (_ai.archetype == KarakArchetype.Driznit)
                {
                    _timer = _ai._profile.ThrowWindupDuration; // 0.35s ThrowWindup
                    ChoralCodeVFX.SpawnTelegraphGlint(
                        _ai.transform.position + new Vector3(_ai._enemy.FacingDirection * 0.42f, 0.08f, 0f),
                        new Color(1.0f, 0.62f, 0.38f, 0.96f),
                        0.48f,
                        0.32f);
                }
                else if (_ai.archetype == KarakArchetype.Conchfly)
                {
                    _timer = _ai._profile.AimTelegraphDuration; // 0.35s AimTelegraph
                    LockConchflyTargetOnce();
                    if (_ai._rb != null) _ai._rb.velocity = Vector2.zero;
                }
                else if (_ai.archetype == KarakArchetype.Pharlid)
                {
                    _timer = _ai._profile.LungeWindupDuration; // 0.25s LungeWindup
                }
                else
                {
                    _timer = _ai._profile.LungeWindupDuration; // 0.24s CoralFurm AlertHop windup
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            private void LockConchflyTargetOnce()
            {
                if (_lockedOnce || !_ai.IsPlayerAlive()) return;
                _lockedOnce = true;
                Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.transform.position;
                _ai._lockedAirDir = delta.sqrMagnitude > 0.01f ? delta.normalized : new Vector2(_ai._enemy.FacingDirection, -0.25f).normalized;
                if (Mathf.Abs(_ai._lockedAirDir.x) > 0.05f)
                {
                    _ai._enemy.SetFacing(_ai._lockedAirDir.x >= 0f ? 1 : -1);
                }
                // Fair thin telegraph aim line pointing through locked player coordinate
                ChoralCodeVFX.SpawnConchflyAimLine(_ai.transform.position, _ai._lockedAirDir, 10.5f, _timer);
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                // Conchfly NEVER homes after locking so the player can cleanly dodge with jump/dash!
                if (_ai.archetype != KarakArchetype.Conchfly && _timer > 0.08f)
                {
                    _ai.FacePlayer();
                }

                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._attackState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == KarakArchetype.Conchfly && _ai._rb != null)
                {
                    // Stop abruptly mid-air with tiny coiled pull-back
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, -_ai._lockedAirDir * 0.9f, Mathf.Clamp01(fixedDt * 14f));
                }
                else if (_ai.archetype == KarakArchetype.Driznit && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, Vector2.zero, Mathf.Clamp01(fixedDt * 12f));
                }
                else
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit() { }
        }

        private sealed class AttackState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public AttackState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDarting = false;

                switch (_ai.archetype)
                {
                    case KarakArchetype.Driznit:
                        _ai.FacePlayer();
                        _ai.FireBouncingProjectile();
                        _timer = 0.20f;
                        if (_ai._rb != null)
                        {
                            // Crisp recoil hop backward when releasing the bouncing shot
                            _ai._rb.velocity = new Vector2(-_ai._enemy.FacingDirection * 2.8f, 1.1f);
                        }
                        break;

                    case KarakArchetype.Conchfly:
                        _ai._isDarting = true;
                        _timer = _ai._profile.DartDuration; // 0.42s straight non-homing high-speed dart
                        if (Mathf.Abs(_ai._lockedAirDir.x) > 0.05f)
                        {
                            _ai._enemy.SetFacing(_ai._lockedAirDir.x >= 0f ? 1 : -1);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        break;

                    case KarakArchetype.Pharlid:
                        _ai.FacePlayer();
                        _timer = 0.30f;
                        int pDir = _ai._enemy.FacingDirection;
                        if (_ai._rb != null)
                        {
                            _ai._rb.velocity = new Vector2(pDir * _ai._profile.QuickLungeSpeed, _ai._profile.QuickLungeHopY);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, pDir, false);
                        break;

                    case KarakArchetype.CoralFurm:
                        _ai.FacePlayer();
                        _timer = _ai._profile.AlertHopDuration; // 0.36s forward AlertHop / RollNudge
                        int cfDir = _ai._enemy.FacingDirection;
                        if (_ai._rb != null && _ai._enemy.HasGroundAhead(cfDir) && !_ai._enemy.HasWallAhead(cfDir))
                        {
                            _ai._rb.velocity = new Vector2(cfDir * _ai._profile.AlertHopSpeed, _ai._profile.AlertHopY);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, cfDir, false);
                        break;
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._recoverState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == KarakArchetype.Conchfly)
                {
                    if (_ai._rb == null) return;
                    float dartSpd = _ai._profile.DartSpeed > 1f ? _ai._profile.DartSpeed : 18.2f;
                    RaycastHit2D hit = Physics2D.CircleCast(_ai._rb.position, 0.44f, _ai._lockedAirDir, dartSpd * fixedDt + 0.08f, _ai._solidTileMask);
                    if (hit.collider != null)
                    {
                        _ai.TriggerConchflyWallBrake();
                        return;
                    }
                    _ai._rb.velocity = _ai._lockedAirDir * dartSpd;
                    return;
                }

                if (_ai.archetype == KarakArchetype.Driznit)
                {
                    if (_ai._rb != null)
                    {
                        _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, Vector2.zero, Mathf.Clamp01(fixedDt * 6.0f));
                    }
                    return;
                }

                if (_ai.archetype == KarakArchetype.Pharlid)
                {
                    int dir = _ai._enemy.FacingDirection;
                    if (_ai._enemy.HasWallAhead(dir) && _ai._rb != null)
                    {
                        _ai._rb.velocity = new Vector2(0f, _ai._rb.velocity.y);
                    }
                    return;
                }

                // CoralFurm AlertHop / RollNudge respects edges & walls cleanly
                int cDir = _ai._enemy.FacingDirection;
                if (!_ai._enemy.HasGroundAhead(cDir) || _ai._enemy.HasWallAhead(cDir))
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit()
            {
                _ai._isDarting = false;
                _ai._enemy.DeactivateAttackHitbox();
            }
        }

        private sealed class RecoverState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            private float _timer;
            private float _totalDuration;
            private int _skitterAwayDir;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public RecoverState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDarting = false;
                _ai._enemy.DeactivateAttackHitbox();
                _ai._attackCooldownTimer = _ai._profile.AttackCooldown > 0.1f ? _ai._profile.AttackCooldown : 1.0f;

                if (_ai.archetype == KarakArchetype.Conchfly)
                {
                    // 0.55s BrakeAndRecover: air-friction deceleration + rising back to patrol height
                    _totalDuration = _ai._profile.BrakeRecoverDuration;
                    _timer = _totalDuration;
                    _ai._isBraking = true;
                    _ai._brakeProgress = 0f;
                }
                else if (_ai.archetype == KarakArchetype.Pharlid)
                {
                    // Short nimble SkitterAway retreat before turning back
                    _totalDuration = _ai._profile.SkitterAwayDuration;
                    _timer = _totalDuration;
                    _skitterAwayDir = -_ai.DirToPlayer();
                    _ai._enemy.SetFacing(_skitterAwayDir);
                }
                else if (_ai.archetype == KarakArchetype.Driznit)
                {
                    _totalDuration = 0.24f;
                    _timer = _totalDuration;
                }
                else
                {
                    _totalDuration = _ai._profile.BrakeRecoverDuration;
                    _timer = _totalDuration;
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_ai._isBraking && _totalDuration > 0.01f)
                {
                    _ai._brakeProgress = Mathf.Clamp01(1f - (_timer / _totalDuration));
                }

                if (_timer <= 0f)
                {
                    if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._chaseState);
                    else _ai.TransitionTo(_ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == KarakArchetype.Conchfly && _ai._rb != null)
                {
                    // Smooth air-friction braking, then gently ascend back toward anchor patrol height
                    float desiredY = Mathf.Clamp((_ai._anchorPosition.y - _ai._rb.position.y) * 1.45f, -2.4f, 3.6f);
                    Vector2 settleVel = new Vector2(0f, desiredY);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, settleVel, Mathf.Clamp01(fixedDt * 5.2f));
                    return;
                }

                if (_ai.archetype == KarakArchetype.Pharlid)
                {
                    // SkitterAway safely without running off ledges or into walls
                    if (!_ai._enemy.HasGroundAhead(_skitterAwayDir) || _ai._enemy.HasWallAhead(_skitterAwayDir))
                    {
                        _ai._enemy.StopHorizontal();
                    }
                    else
                    {
                        _ai._enemy.SetHorizontalVelocity(_skitterAwayDir * _ai._profile.SkitterAwaySpeed);
                    }
                    return;
                }

                if (_ai.archetype == KarakArchetype.Driznit && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, Vector2.zero, Mathf.Clamp01(fixedDt * 6.5f));
                    return;
                }

                _ai._enemy.StopHorizontal();
            }

            public void Exit()
            {
                _ai._isBraking = false;
                _ai._brakeProgress = 0f;
            }
        }

        private sealed class StaggerState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            private float _shellBraceTimer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(KarakEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDarting = false;
                _ai._isBraking = false;
                _ai._enemy.DeactivateAttackHitbox();
                if (_ai.archetype == KarakArchetype.CoralFurm)
                {
                    _ai._isShellBracing = true;
                    _shellBraceTimer = _ai._profile.ShellBraceDuration > 0.05f ? _ai._profile.ShellBraceDuration : 0.28f;
                }
            }

            public void Tick(float dt)
            {
                if (_ai.archetype == KarakArchetype.CoralFurm)
                {
                    _shellBraceTimer -= dt;
                    if (_shellBraceTimer <= 0f && !_ai._enemy.IsStaggered)
                    {
                        if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._chaseState);
                        else _ai.TransitionTo(_ai._patrolState);
                    }
                    return;
                }

                if (!_ai._enemy.IsStaggered)
                {
                    if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._chaseState);
                    else _ai.TransitionTo(_ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborneArchetype() && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 15.0f * fixedDt);
                }
                else if (_ai.archetype == KarakArchetype.CoralFurm && _ai._rb != null)
                {
                    // Measured ShellBrace slide deceleration
                    _ai._rb.velocity = new Vector2(Mathf.MoveTowards(_ai._rb.velocity.x, 0f, 18.0f * fixedDt), _ai._rb.velocity.y);
                }
            }

            public void Exit()
            {
                _ai._isShellBracing = false;
            }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly KarakEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(KarakEnemyAI ai) { _ai = ai; }
            public void Enter()
            {
                _ai._isDarting = false;
                _ai._isBraking = false;
                _ai._isShellBracing = false;
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.StopHorizontal();
            }
            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
