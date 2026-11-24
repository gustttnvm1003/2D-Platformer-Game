using UnityEngine;

namespace SilkenGrotto
{
    public enum ChoralArchetype
    {
        GrandReed,
        Reed,
        Choristor,
        ChoirHornhead,
        ChoirElder,
        ChoirFlyer
    }

    /// <summary>
    /// Modular 60-FPS-physics Silksong-style FSM implementing all 6 Choral Chambers Citadel enemies
    /// tuned centrally via ENEMY_COMBAT_CONFIG (preserving all existing art paths & automatic rendering adaptation):
    ///
    /// 1. GrandReed     : Idle -> March -> Telegraph (0.45s) -> WideHeavySlash (2 dmg) / DoubleThrustCombo (2 dmg) -> Recovery;
    ///                    reactive GuardStance on sustained frontal pressure (head remains pogo-able), 3x-5x orb reward.
    /// 2. Reed          : Patrol -> agile duel cycle of BlockStance (separated FrontBlockBox), QuickSlash, ForwardLunge & Backstep.
    ///                    Downward pogo & rear attacks pierce block & cancel BlockStance.
    /// 3. Choristor     : Mid-range Thread Caster — RetreatHop if player closes in, else ThreadWindup (0.35s) -> CastThread
    ///                    (code-drawn white ThreadProjectile) -> Cooldown.
    /// 4. ChoirHornhead : Patrol -> ChargeWindup (lower horn + dust) -> ChargeDash -> SkidStop;
    ///                    PlayerOverhead rule freezes horizontal velocity (0.62s-0.88s) for diagonal pogo chains!
    /// 5. ChoirElder    : Slow high-poise bruiser rotating HeavySwing (close) & StepStrike (mid-range lunge) with 2 damage.
    /// 6. ChoirFlyer    : HoverPatrol (sine wave) -> LockOn (0.25s) -> SwoopDive (straight charge) -> RecoverClimb; low HP, pogo-friendly.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class ChoralEnemyAI : MonoBehaviour
    {
        private enum ActiveMoveKind
        {
            None,
            // Grand Reed
            WideHeavySlash,
            DoubleThrustCombo,
            // Reed
            QuickSlash,
            ForwardLunge,
            // Choristor
            CastThread,
            // Choir Hornhead
            ChargeDash,
            // Choir Elder
            HeavySwing,
            StepStrike,
            // Choir Flyer
            SwoopDive
        }

        [Header("Archetype & Optional Legacy Sprite")]
        [SerializeField] private ChoralArchetype archetype = ChoralArchetype.Choristor;
        [SerializeField] private Sprite soundWaveSprite;

        [Header("Visual & Block References")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private EnemyFrontBlockBox frontBlockBox;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private Animator _animator;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.Profile _cfg;
        private bool _hasSpriteSheetAnimator;

        private LayerMask _solidTileMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private int _patrolDir = 1;
        private float _attackCooldownTimer;
        private float _animPhase;

        // Tactical state memory
        private ActiveMoveKind _chosenMove = ActiveMoveKind.None;
        private int _patternCycleIndex;
        private int _reedDuelStep;
        private float _pressureTimer;
        private int _pressureHits;
        private bool _counterQueuedFromBlock;
        private float _overheadPauseTimer;
        private float _overheadCooldownTimer;
        private Vector2 _lockedAirTarget;
        private Vector2 _lockedAirDir = Vector2.right;
        private float _safeHoverAltitudeY;
        private float _chargeStartX;
        private float _hopTimer;
        private float _hopVx;
        private float _squishPulseTimer;

        // Concrete FSM States
        private PatrolState _patrolState;
        private EngageState _engageState;
        private GuardState _guardState;
        private RetreatHopState _retreatHopState;
        private TelegraphState _telegraphState;
        private AttackState _attackState;
        private RecoverState _recoverState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        public ChoralArchetype Archetype { get { return archetype; } }

        public void ConfigureArchetype(ChoralArchetype kind, Sprite waveSprite)
        {
            archetype = kind;
            if (waveSprite != null) soundWaveSprite = waveSprite;
            _cfg = ENEMY_COMBAT_CONFIG.Get(archetype);
        }

        private void Awake()
        {
            _cfg = ENEMY_COMBAT_CONFIG.Get(archetype);
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _hasSpriteSheetAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            _fsm = new EnemyStateMachine();
            _solidTileMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.71f) % 6.28f;
            _patrolDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualLocalPos = visualTransform.localPosition;

            if (frontBlockBox == null)
            {
                Transform fbChild = transform.Find("FrontBlockBox");
                if (fbChild != null) frontBlockBox = fbChild.GetComponent<EnemyFrontBlockBox>();
                if (frontBlockBox == null) frontBlockBox = GetComponentInChildren<EnemyFrontBlockBox>();
            }
            if (frontBlockBox == null && _cfg.HasFrontBlockBox)
            {
                GameObject fb = new GameObject("FrontBlockBox");
                fb.transform.SetParent(transform, false);
                frontBlockBox = fb.AddComponent<EnemyFrontBlockBox>();
                frontBlockBox.Configure(_enemy, _cfg.FrontBlockBoxOffset, _cfg.FrontBlockBoxSize, new Color(1f, 0.92f, 0.56f, 0.95f));
            }

            _patrolState = new PatrolState(this);
            _engageState = new EngageState(this);
            _guardState = new GuardState(this);
            _retreatHopState = new RetreatHopState(this);
            _telegraphState = new TelegraphState(this);
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
                _enemy.OnFrontalBlocked += HandleFrontalBlocked;
                _enemy.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged -= HandleDamaged;
                _enemy.OnFrontalBlocked -= HandleFrontalBlocked;
                _enemy.OnDied -= HandleDied;
            }
        }

        private void Start()
        {
            ResolvePlayer();
            _safeHoverAltitudeY = transform.position.y;
            if (_cfg.IsFlying && _rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.drag = 0f;
            }
            SetBlockStance(false);
            _enemy.SetFacing(_patrolDir);
            TransitionTo(_patrolState);
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_hopTimer > 0f) _hopTimer -= dt;
            if (_squishPulseTimer > 0f) _squishPulseTimer -= dt;
            if (_overheadCooldownTimer > 0f) _overheadCooldownTimer -= dt;
            if (_overheadPauseTimer > 0f) _overheadPauseTimer -= dt;
            if (_pressureTimer > 0f)
            {
                _pressureTimer -= dt;
                if (_pressureTimer <= 0f) _pressureHits = 0;
            }
            _animPhase += dt;

            // Choir Hornhead PlayerOverhead pogo-friendly rule (never interrupts mid-ChargeDash!)
            UpdateHornheadOverheadRule();

            _fsm.Tick(dt);
            ApplyAdaptiveVisualAnimation();
        }

        private void FixedUpdate()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            _fsm.FixedTick(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (_cfg.IsFlying && _fsm.CurrentState == _attackState)
            {
                int l = collision.gameObject.layer;
                if (l == GameConstants.LayerGround || l == GameConstants.LayerBreakableWall)
                {
                    if (_rb != null) _rb.velocity = Vector2.zero;
                    TransitionTo(_recoverState);
                }
            }
        }

        private void SetBlockStance(bool active)
        {
            if (_enemy != null) _enemy.SetFrontalGuard(active);
            if (frontBlockBox != null) frontBlockBox.SetBlockActive(active);
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

        private void HandleFrontalBlocked(DamageInfo info)
        {
            if (!_enemy.IsAlive) return;
            _squishPulseTimer = 0.12f;
            if (archetype == ChoralArchetype.Reed && _fsm.CurrentState == _guardState)
            {
                // Successful frontal parry empowers Reed to answer swiftly with QuickSlash!
                _counterQueuedFromBlock = true;
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (!_enemy.IsAlive) return;
            _squishPulseTimer = 0.14f;

            // Downward pogo or rear hit cleanly breaks BlockStance / GuardStance!
            if (_fsm.CurrentState == _guardState)
            {
                SetBlockStance(false);
                _counterQueuedFromBlock = false;
                TransitionTo(_staggerState);
                return;
            }

            // Grand Reed tracks consecutive hits while pressed -> enters brief reactive GuardStance after recovery/stagger
            if (archetype == ChoralArchetype.GrandReed)
            {
                _pressureHits++;
                _pressureTimer = 2.6f;
                if (_fsm.CurrentState == _telegraphState || _fsm.CurrentState == _attackState) return;
                if (_pressureHits >= 2 && !info.IsDownwardPogo)
                {
                    _pressureHits = 0;
                    TransitionTo(_guardState);
                    return;
                }
            }

            // High-poise bruiser & mid-charge Hornhead resist stun interruption
            if (archetype == ChoralArchetype.ChoirElder) return;
            if (archetype == ChoralArchetype.ChoirHornhead && _fsm.CurrentState == _attackState) return;

            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            SetBlockStance(false);
            TransitionTo(_deathState);
            if (visualTransform != null)
            {
                visualTransform.localRotation = Quaternion.identity;
                visualTransform.localScale = new Vector3(_enemy.FacingDirection * _baseVisualScale.x, _baseVisualScale.y, _baseVisualScale.z);
            }
        }

        private void UpdateHornheadOverheadRule()
        {
            if (archetype != ChoralArchetype.ChoirHornhead || !IsPlayerAlive()) return;
            // Never interrupt an active ChargeDash!
            if (_fsm.CurrentState == _attackState || _fsm.CurrentState == _staggerState || _fsm.CurrentState == _deathState) return;
            if (_overheadPauseTimer > 0f || _overheadCooldownTimer > 0f) return;

            if (IsPlayerOverheadHornhead())
            {
                _overheadPauseTimer = Random.Range(_cfg.OverheadPauseMin, _cfg.OverheadPauseMax);
                _overheadCooldownTimer = _overheadPauseTimer + 0.55f;
                _enemy.StopHorizontal();
            }
        }

        private bool IsPlayerOverheadHornhead()
        {
            if (!IsPlayerAlive()) return false;
            float dx = Mathf.Abs(_playerTransform.position.x - transform.position.x);
            float dy = _playerTransform.position.y - transform.position.y;
            return dy >= 1.10f && dy <= 4.6f && dx <= 2.40f;
        }

        private bool IsPlayerAlive()
        {
            return _playerTransform != null && (_playerController == null || _playerController.IsAlive);
        }

        private Vector2 PlayerChestPos()
        {
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 0.92f) : (Vector2)transform.position;
        }

        private float GetDistToPlayer()
        {
            return _playerTransform != null ? Vector2.Distance(transform.position, _playerTransform.position) : float.MaxValue;
        }

        private float GetHorizDistToPlayer()
        {
            return _playerTransform != null ? Mathf.Abs(_playerTransform.position.x - transform.position.x) : float.MaxValue;
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
            float maxDy = _cfg.IsFlying ? 8.5f : 3.6f;
            return GetDistToPlayer() <= _cfg.DetectRange && GetVertDeltaToPlayer() <= maxDy;
        }

        private bool ShouldLosePlayer()
        {
            if (!IsPlayerAlive()) return true;
            float maxDy = _cfg.IsFlying ? 11.0f : 5.8f;
            return GetDistToPlayer() > _cfg.LoseTargetRange || GetVertDeltaToPlayer() > maxDy;
        }

        private bool CanTriggerAttack()
        {
            if (!IsPlayerAlive() || _attackCooldownTimer > 0f) return false;
            if (archetype == ChoralArchetype.ChoirHornhead)
            {
                if (_overheadPauseTimer > 0f || IsPlayerOverheadHornhead()) return false;
                return GetHorizDistToPlayer() <= _cfg.AttackRange && GetVertDeltaToPlayer() <= 1.85f;
            }
            if (archetype == ChoralArchetype.Choristor)
            {
                float d = GetHorizDistToPlayer();
                return d >= _cfg.IdealMinRange && d <= _cfg.IdealMaxRange && GetVertDeltaToPlayer() <= 3.2f;
            }
            if (_cfg.IsFlying)
            {
                return GetDistToPlayer() <= _cfg.AttackRange;
            }
            return GetDistToPlayer() <= _cfg.AttackRange && GetVertDeltaToPlayer() <= 2.4f;
        }

        private void LaunchChoristorThreadProjectile()
        {
            int face = _enemy.FacingDirection;
            Vector3 origin = transform.position + new Vector3(face * 0.78f, 0.94f, 0f);
            Vector2 dir = new Vector2(face, 0f);

            // Straight or lightly aimed (clamped within +/-18 degrees) toward player's last chest position
            if (IsPlayerAlive())
            {
                Vector2 toPlayer = PlayerChestPos() - (Vector2)origin;
                if (toPlayer.sqrMagnitude > 0.04f)
                {
                    float rawAngle = Mathf.Atan2(toPlayer.y, Mathf.Abs(toPlayer.x)) * Mathf.Rad2Deg;
                    float clampedRad = Mathf.Clamp(rawAngle, -18f, 18f) * Mathf.Deg2Rad;
                    dir = new Vector2(Mathf.Cos(clampedRad) * face, Mathf.Sin(clampedRad)).normalized;
                }
            }

            GameObject go = new GameObject("Choristor_ThreadProjectile");
            go.AddComponent<BoxCollider2D>();
            ThreadProjectile thread = go.AddComponent<ThreadProjectile>();
            thread.Launch(origin, dir, _cfg.ProjectileSpeed, _cfg.PrimaryAttackDamage, _cfg.PrimaryKnockback, 12.2f, gameObject);

            ChoralCodeVFX.SpawnTelegraphGlint(origin, new Color(1f, 0.97f, 0.85f, 0.95f), 0.36f, 0.14f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySilkSpear();
        }

        /// <summary>
        /// Adaptive Rendering:
        /// - With existing 12x4 sprite sheets: enhances Animator states with crisp subtle telegraph tremors,
        ///   guard poise, charge/swoop pitch, and hit squash.
        /// - With single-image fallback (no Animator): drives full procedural breathing, walk bob, lean,
        ///   telegraph tremble, and attack squash & stretch so every pattern stays readable!
        /// </summary>
        private void ApplyAdaptiveVisualAnimation()
        {
            if (visualTransform == null) return;
            EnemyAIStateType st = _fsm.CurrentState != null ? _fsm.CurrentState.StateType : EnemyAIStateType.Idle;
            int dir = _enemy != null ? _enemy.FacingDirection : 1;

            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            float rotZ = 0f;
            Vector3 offset = Vector3.zero;
            float proceduralStrength = _hasSpriteSheetAnimator ? 0.38f : 1.0f;

            if (st == EnemyAIStateType.Death)
            {
                visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
                visualTransform.localPosition = _baseVisualLocalPos;
                visualTransform.localRotation = Quaternion.identity;
                return;
            }

            // Idle / Patrol subtle breathing & flying hover bob
            if (st == EnemyAIStateType.Idle || st == EnemyAIStateType.Patrol || st == EnemyAIStateType.Chase)
            {
                if (_cfg.IsFlying)
                {
                    offset.y += Mathf.Sin(_animPhase * 4.2f) * 0.055f;
                    float vx = _rb != null ? _rb.velocity.x : 0f;
                    rotZ = Mathf.Clamp(-vx * 1.4f, -8f, 8f);
                }
                else
                {
                    float speed = _rb != null ? Mathf.Abs(_rb.velocity.x) : 0f;
                    if (speed < 0.15f)
                    {
                        float breath = Mathf.Sin(_animPhase * 2.8f) * 0.022f * proceduralStrength;
                        sy *= (1f + breath);
                        sx *= (1f - breath * 0.5f);
                    }
                    else
                    {
                        float strideBob = Mathf.Abs(Mathf.Sin(_animPhase * (speed * 2.2f))) * 0.045f * proceduralStrength;
                        offset.y += strideBob;
                        rotZ = -dir * Mathf.Clamp(speed * 0.75f, 0f, 5.5f) * proceduralStrength;
                    }
                }
            }
            else if (st == EnemyAIStateType.Windup)
            {
                if (_fsm.CurrentState == _guardState)
                {
                    // GuardStance / BlockStance braced defensive poise
                    sx *= (1f + 0.04f * proceduralStrength);
                    sy *= (1f - 0.04f * proceduralStrength);
                    rotZ = dir * 3.5f * proceduralStrength;
                }
                else
                {
                    // Telegraph anticipation: coil back + micro tremor
                    float tremor = Mathf.Sin(_animPhase * 52f) * 0.032f;
                    offset.x += tremor - dir * 0.06f * proceduralStrength;
                    sx *= (1f - 0.06f * proceduralStrength);
                    sy *= (1f + 0.05f * proceduralStrength);
                    if (archetype == ChoralArchetype.ChoirHornhead)
                    {
                        // Lower horn aggressively forward during ChargeWindup
                        rotZ = -dir * 9.5f * proceduralStrength;
                        sy *= (1f - 0.08f * proceduralStrength);
                    }
                    else if (archetype == ChoralArchetype.ChoirFlyer)
                    {
                        float ang = Mathf.Atan2(_lockedAirDir.y, Mathf.Abs(_lockedAirDir.x)) * Mathf.Rad2Deg;
                        rotZ = Mathf.Clamp(ang * dir * 0.35f, -16f, 16f);
                    }
                }
            }
            else if (st == EnemyAIStateType.Attack)
            {
                sx *= (1f + 0.08f * proceduralStrength);
                sy *= (1f - 0.05f * proceduralStrength);
                if (archetype == ChoralArchetype.ChoirHornhead)
                {
                    rotZ = -dir * 12f * proceduralStrength;
                }
                else if (archetype == ChoralArchetype.ChoirFlyer)
                {
                    float ang = Mathf.Atan2(_lockedAirDir.y, Mathf.Abs(_lockedAirDir.x)) * Mathf.Rad2Deg;
                    rotZ = Mathf.Clamp(ang * dir * 0.55f, -28f, 28f);
                }
                else
                {
                    rotZ = -dir * 6f * proceduralStrength;
                }
            }
            else if (st == EnemyAIStateType.Recover && archetype == ChoralArchetype.ChoirHornhead)
            {
                // Braking lean back during SkidStop
                rotZ = dir * 7.5f * proceduralStrength;
            }

            if (_squishPulseTimer > 0f)
            {
                float k = _squishPulseTimer / 0.14f;
                sx *= (1f + 0.07f * k);
                sy *= (1f - 0.06f * k);
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // 1. PATROL / IDLE / HOVER PATROL STATE
        // =========================================================================
        private sealed class PatrolState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            private float _idlePauseTimer;
            public EnemyAIStateType StateType { get { return _idlePauseTimer > 0f ? EnemyAIStateType.Idle : EnemyAIStateType.Patrol; } }
            public PatrolState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
                _idlePauseTimer = _ai.archetype == ChoralArchetype.GrandReed ? _ai._cfg.IdlePauseDuration * 0.65f : 0f;
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.TransitionTo(_ai._engageState);
                    return;
                }

                if (_ai._overheadPauseTimer > 0f) return;

                if (_idlePauseTimer > 0f)
                {
                    _idlePauseTimer -= dt;
                    return;
                }

                if (!_ai._cfg.IsFlying)
                {
                    float dx = _ai.transform.position.x - _ai._enemy.SpawnPosition.x;
                    bool edge = !_ai._enemy.HasGroundAhead(_ai._patrolDir);
                    bool wall = _ai._enemy.HasWallAhead(_ai._patrolDir);
                    bool limit = (_ai._patrolDir > 0 && dx >= _ai._cfg.PatrolDistance) || (_ai._patrolDir < 0 && dx <= -_ai._cfg.PatrolDistance);
                    if (edge || wall || limit)
                    {
                        _ai._patrolDir = -_ai._patrolDir;
                        _ai._enemy.SetFacing(_ai._patrolDir);
                        _idlePauseTimer = _ai._cfg.IdlePauseDuration;
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._cfg.IsFlying)
                {
                    if (_ai._rb == null) return;
                    Vector2 anchor = new Vector2(_ai._enemy.SpawnPosition.x, _ai._safeHoverAltitudeY);
                    Vector2 desired = anchor + new Vector2(
                        Mathf.Sin(_ai._animPhase * 1.25f) * _ai._cfg.PatrolDistance * 0.68f,
                        Mathf.Sin(_ai._animPhase * _ai._cfg.HoverFrequency) * _ai._cfg.HoverAmplitude);
                    Vector2 vel = (desired - _ai._rb.position) * 2.9f;
                    if (Mathf.Abs(vel.x) > 0.25f) _ai._enemy.SetFacing(vel.x >= 0f ? 1 : -1);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 5.2f));
                    return;
                }

                if (_idlePauseTimer > 0f || _ai._overheadPauseTimer > 0f)
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                if (!_ai._enemy.HasGroundAhead(_ai._patrolDir) || _ai._enemy.HasWallAhead(_ai._patrolDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(_ai._patrolDir * _ai._cfg.PatrolSpeed);
            }

            public void Exit() { }
        }

        // =========================================================================
        // 2. ENGAGE / MARCH / SPACING / APPROACH STATE
        // =========================================================================
        private sealed class EngageState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }
            public EngageState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai._enemy.SetChargeState(false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            }

            public void Tick(float dt)
            {
                if (_ai.ShouldLosePlayer())
                {
                    _ai.TransitionTo(_ai._patrolState);
                    return;
                }

                // Choir Hornhead freezes cleanly when player is overhead so player can pogo!
                if (_ai.archetype == ChoralArchetype.ChoirHornhead && _ai._overheadPauseTimer > 0f)
                {
                    return;
                }

                _ai.FacePlayer();

                // Choristor spacing AI: RetreatHop if player gets too close!
                if (_ai.archetype == ChoralArchetype.Choristor && _ai.GetHorizDistToPlayer() < _ai._cfg.IdealMinRange && _ai.GetVertDeltaToPlayer() <= 2.6f)
                {
                    _ai.TransitionTo(_ai._retreatHopState);
                    return;
                }

                // Reed agile duelist rotation: opens / interleaves clear BlockStance in duel rhythm!
                if (_ai.archetype == ChoralArchetype.Reed && _ai._attackCooldownTimer <= 0f && _ai.GetDistToPlayer() <= _ai._cfg.AttackRange + 0.5f && _ai.GetVertDeltaToPlayer() <= 2.4f)
                {
                    int step = _ai._reedDuelStep % 3;
                    _ai._reedDuelStep++;
                    if (step == 0)
                    {
                        _ai.TransitionTo(_ai._guardState);
                        return;
                    }
                    _ai._chosenMove = (step == 1) ? ActiveMoveKind.QuickSlash : ActiveMoveKind.ForwardLunge;
                    _ai.TransitionTo(_ai._telegraphState);
                    return;
                }

                if (_ai.CanTriggerAttack())
                {
                    _ai.SelectNextMove();
                    _ai.TransitionTo(_ai._telegraphState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == ChoralArchetype.ChoirHornhead && _ai._overheadPauseTimer > 0f)
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                int dir = _ai.DirToPlayer();

                if (_ai._cfg.IsFlying)
                {
                    // Choir Flyer glides smoothly above player with gentle sine hover until LockOn triggers
                    if (_ai._rb == null || !_ai.IsPlayerAlive()) return;
                    Vector2 target = _ai.PlayerChestPos() + new Vector2(-dir * 3.8f, 2.8f);
                    target.y = Mathf.Max(target.y, _ai._safeHoverAltitudeY - 0.5f);
                    Vector2 toT = target - _ai._rb.position;
                    Vector2 desired = toT.sqrMagnitude > 0.25f ? toT.normalized * _ai._cfg.ChaseSpeed : Vector2.zero;
                    desired.y += Mathf.Sin(_ai._animPhase * _ai._cfg.HoverFrequency) * 0.65f;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, desired, Mathf.Clamp01(fixedDt * 5.6f));
                    return;
                }

                if (_ai.archetype == ChoralArchetype.Choristor)
                {
                    float hDist = _ai.GetHorizDistToPlayer();
                    if (hDist <= _ai._cfg.IdealMaxRange && hDist >= _ai._cfg.IdealMinRange)
                    {
                        _ai._enemy.StopHorizontal();
                        return;
                    }
                }

                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                float stopDist = (_ai.archetype == ChoralArchetype.ChoirElder) ? 2.15f : 1.45f;
                if (_ai.GetHorizDistToPlayer() <= stopDist)
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                _ai._enemy.SetHorizontalVelocity(dir * _ai._cfg.ChaseSpeed);
            }

            public void Exit() { }
        }

        private void SelectNextMove()
        {
            _patternCycleIndex = (_patternCycleIndex + 1) % 2;
            switch (archetype)
            {
                case ChoralArchetype.GrandReed:
                    _chosenMove = (_patternCycleIndex == 0) ? ActiveMoveKind.WideHeavySlash : ActiveMoveKind.DoubleThrustCombo;
                    break;
                case ChoralArchetype.Reed:
                    _chosenMove = (GetHorizDistToPlayer() <= 2.3f) ? ActiveMoveKind.QuickSlash : ActiveMoveKind.ForwardLunge;
                    break;
                case ChoralArchetype.Choristor:
                    _chosenMove = ActiveMoveKind.CastThread;
                    break;
                case ChoralArchetype.ChoirHornhead:
                    _chosenMove = ActiveMoveKind.ChargeDash;
                    break;
                case ChoralArchetype.ChoirElder:
                    // Clean 2-pattern rotation: HeavySwing for close range, StepStrike to close mid-range (and alternating!)
                    float dist = GetHorizDistToPlayer();
                    if (dist <= _cfg.IdealMinRange)
                        _chosenMove = (_patternCycleIndex == 0) ? ActiveMoveKind.HeavySwing : ActiveMoveKind.StepStrike;
                    else
                        _chosenMove = (_patternCycleIndex == 0) ? ActiveMoveKind.StepStrike : ActiveMoveKind.HeavySwing;
                    break;
                case ChoralArchetype.ChoirFlyer:
                    _chosenMove = ActiveMoveKind.SwoopDive;
                    break;
            }
        }

        // =========================================================================
        // 3. BLOCK STANCE (Reed) & GUARD STANCE (Grand Reed)
        // =========================================================================
        private sealed class GuardState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public GuardState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                _ai._enemy.StopHorizontal();
                _ai._counterQueuedFromBlock = false;
                _ai.SetBlockStance(true);
                _timer = _ai._cfg.GuardDuration;

                Vector3 glintPos = _ai.transform.position + new Vector3(_ai._enemy.FacingDirection * 0.72f, _ai._cfg.FrontBlockBoxOffset.y, 0f);
                ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1f, 0.94f, 0.62f, 0.95f), 0.45f, 0.22f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.68f);
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                _ai.FacePlayer();

                // Reed answers immediately out of BlockStance when player's frontal strike clangs off the guard!
                if (_ai._counterQueuedFromBlock && _timer <= _ai._cfg.GuardDuration - 0.16f)
                {
                    _ai._counterQueuedFromBlock = false;
                    _ai._chosenMove = ActiveMoveKind.QuickSlash;
                    _ai.TransitionTo(_ai._telegraphState);
                    return;
                }

                if (_timer <= 0f)
                {
                    if (_ai.archetype == ChoralArchetype.Reed && _ai.IsPlayerAlive() && _ai.GetDistToPlayer() <= _ai._cfg.AttackRange + 1.0f)
                    {
                        _ai._chosenMove = (_ai.GetHorizDistToPlayer() <= 2.4f) ? ActiveMoveKind.QuickSlash : ActiveMoveKind.ForwardLunge;
                        _ai.TransitionTo(_ai._telegraphState);
                    }
                    else
                    {
                        _ai._attackCooldownTimer = 0.25f;
                        _ai.TransitionTo(_ai._engageState);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
            }

            public void Exit()
            {
                _ai.SetBlockStance(false);
            }
        }

        // =========================================================================
        // 4. RETREAT HOP (Choristor spacing) & BACKSTEP (Reed agile reset)
        // =========================================================================
        private sealed class RetreatHopState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            private float _timer;
            private int _hopDir;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public RetreatHopState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai.FacePlayer();
                _hopDir = -_ai._enemy.FacingDirection;
                _timer = (_ai.archetype == ChoralArchetype.Choristor) ? 0.30f : 0.24f;

                if (_ai._rb != null)
                {
                    float vx = (_ai._enemy.HasGroundAhead(_hopDir) && !_ai._enemy.HasWallAhead(_hopDir))
                        ? (_hopDir * _ai._cfg.RetreatHopSpeed)
                        : (_hopDir * _ai._cfg.RetreatHopSpeed * 0.32f);
                    float vy = (_ai.archetype == ChoralArchetype.Choristor) ? 5.8f : 4.6f;
                    _ai._rb.velocity = new Vector2(vx, vy);
                }

                ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, -_hopDir, false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    if (_ai.archetype == ChoralArchetype.Choristor && _ai.IsPlayerAlive() && _ai._attackCooldownTimer <= 0f)
                    {
                        // Immediately follow RetreatHop with ThreadWindup -> CastThread!
                        _ai._chosenMove = ActiveMoveKind.CastThread;
                        _ai.TransitionTo(_ai._telegraphState);
                        return;
                    }
                    _ai.TransitionTo(_ai.CanDetectPlayer() ? (IEnemyState)_ai._engageState : _ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._enemy.HasWallAhead(_hopDir))
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit()
            {
                _ai._enemy.StopHorizontal();
            }
        }

        // =========================================================================
        // 5. TELEGRAPH / WINDUP / LOCK-ON STATE
        // =========================================================================
        private sealed class TelegraphState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            private float _timer;
            private float _dustTimer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public TelegraphState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai.FacePlayer();
                if (!_ai._cfg.IsFlying) _ai._enemy.StopHorizontal();

                if (_ai._chosenMove == ActiveMoveKind.StepStrike || _ai._chosenMove == ActiveMoveKind.ForwardLunge)
                    _timer = _ai._cfg.SecondaryTelegraphDuration;
                else if (_ai._chosenMove == ActiveMoveKind.SwoopDive)
                    _timer = _ai._cfg.LockOnDuration;
                else
                    _timer = _ai._cfg.TelegraphDuration;

                _dustTimer = 0.08f;
                LockAirCoordinates();

                // Unmistakable Silksong audio + code-drawn visual telegraph cue!
                Vector3 glintPos = _ai.transform.position + new Vector3(
                    _ai._enemy.FacingDirection * (_ai._cfg.IsFlying ? 0.25f : 0.65f),
                    _ai._cfg.IsFlying ? 0f : (_ai._cfg.BodyColliderSize.y * 0.65f),
                    0f);

                if (_ai.archetype == ChoralArchetype.GrandReed)
                {
                    ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1.0f, 0.85f, 0.32f, 1f), 0.82f, 0.36f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell);
                }
                else if (_ai.archetype == ChoralArchetype.Choristor)
                {
                    ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1.0f, 0.98f, 0.88f, 1f), 0.56f, 0.32f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
                }
                else if (_ai.archetype == ChoralArchetype.ChoirHornhead)
                {
                    ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._enemy.FacingDirection, false);
                    ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1.0f, 0.92f, 0.68f, 0.95f), 0.50f, 0.26f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
                }
                else if (_ai.archetype == ChoralArchetype.ChoirElder)
                {
                    ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1.0f, 0.78f, 0.28f, 1f), 0.76f, 0.38f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell);
                }
                else if (_ai.archetype == ChoralArchetype.ChoirFlyer)
                {
                    ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1.0f, 0.92f, 0.52f, 1f), 0.52f, 0.24f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
                }
                else
                {
                    ChoralCodeVFX.SpawnTelegraphGlint(glintPos, new Color(1.0f, 0.94f, 0.65f, 0.92f), 0.44f, 0.20f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
                }
            }

            private void LockAirCoordinates()
            {
                if (!_ai.IsPlayerAlive()) return;
                _ai._lockedAirTarget = _ai.PlayerChestPos();
                Vector2 delta = _ai._lockedAirTarget - (Vector2)_ai.transform.position;
                if (delta.sqrMagnitude > 0.01f) _ai._lockedAirDir = delta.normalized;
            }

            public void Tick(float dt)
            {
                // Choir Hornhead pauses cleanly if player jumped overhead during ChargeWindup!
                if (_ai.archetype == ChoralArchetype.ChoirHornhead && _ai._overheadPauseTimer > 0f)
                {
                    _ai.TransitionTo(_ai._engageState);
                    return;
                }

                _timer -= dt;
                if (_timer > 0.09f)
                {
                    _ai.FacePlayer();
                    if (_ai._cfg.IsFlying) LockAirCoordinates();
                }

                // Hornhead foot-scraping charge dust during ChargeWindup
                if (_ai.archetype == ChoralArchetype.ChoirHornhead)
                {
                    _dustTimer -= dt;
                    if (_dustTimer <= 0f)
                    {
                        _dustTimer = 0.16f;
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._enemy.FacingDirection, false);
                    }
                }

                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._attackState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._cfg.IsFlying && _ai._rb != null)
                {
                    // Brief backward coil before SwoopDive
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, -_ai._lockedAirDir * 1.6f, Mathf.Clamp01(fixedDt * 10f));
                }
                else
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit() { }
        }

        // =========================================================================
        // 6. ATTACK EXECUTION STATE (ALL 6 CHORAL PATTERNS)
        // =========================================================================
        private sealed class AttackState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            private float _timer;
            private bool _secondStrikeFired;
            private float _chargeDustTimer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public AttackState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai.FacePlayer();
                _timer = _ai._cfg.AttackActiveDuration;
                _secondStrikeFired = false;
                _chargeDustTimer = 0.10f;
                _ai._chargeStartX = _ai.transform.position.x;

                int face = _ai._enemy.FacingDirection;
                ENEMY_COMBAT_CONFIG.Profile c = _ai._cfg;

                switch (_ai._chosenMove)
                {
                    case ActiveMoveKind.WideHeavySlash:
                        // Grand Reed: Wide long-reach golden lance arc (2 DAMAGE!)
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.PrimaryAttackOffset, c.PrimaryAttackSize,
                            c.PrimaryAttackDamage, c.PrimaryKnockback,
                            new Color(1f, 0.88f, 0.44f, 0.95f), 1.35f);
                        ChoralCodeVFX.SpawnSlashArc(
                            _ai.transform.position + new Vector3(face * 0.95f, 1.15f, 0f),
                            face, 2.25f, 1.25f, 138f, 0.24f, new Color(1f, 0.90f, 0.46f, 0.96f), 0.16f);
                        GameEvents.RaiseCameraShake(0.055f, 0.085f);
                        break;

                    case ActiveMoveKind.DoubleThrustCombo:
                        // Grand Reed: Thrust 1 of 2 rapid piercing thrusts (2 DAMAGE each!)
                        _timer = 0.44f;
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.SecondaryAttackOffset, c.SecondaryAttackSize,
                            c.SecondaryAttackDamage, c.SecondaryKnockback,
                            new Color(1f, 0.95f, 0.72f, 0.96f), 1.20f);
                        ChoralCodeVFX.SpawnThrustStreak(
                            _ai.transform.position + new Vector3(face * 0.45f, 1.02f, 0f),
                            face, 3.25f, 0.20f, new Color(1f, 0.92f, 0.56f, 0.96f), 0.14f);
                        break;

                    case ActiveMoveKind.QuickSlash:
                        // Reed: Fast agile close needle slash (1 damage)
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.PrimaryAttackOffset, c.PrimaryAttackSize,
                            c.PrimaryAttackDamage, c.PrimaryKnockback,
                            new Color(1f, 0.95f, 0.75f, 0.94f), 0.95f);
                        ChoralCodeVFX.SpawnSlashArc(
                            _ai.transform.position + new Vector3(face * 0.72f, 0.92f, 0f),
                            face, 1.45f, 0.82f, 118f, 0.14f, new Color(1f, 0.96f, 0.80f, 0.96f), 0.12f);
                        break;

                    case ActiveMoveKind.ForwardLunge:
                        // Reed: Agile forward needle gap-closer (1 damage)
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.SecondaryAttackOffset, c.SecondaryAttackSize,
                            c.SecondaryAttackDamage, c.SecondaryKnockback,
                            new Color(1f, 0.96f, 0.82f, 0.96f), 1.02f);
                        ChoralCodeVFX.SpawnThrustStreak(
                            _ai.transform.position + new Vector3(face * 0.35f, 0.92f, 0f),
                            face, 2.45f, 0.15f, new Color(1f, 0.96f, 0.78f, 0.96f), 0.13f);
                        break;

                    case ActiveMoveKind.CastThread:
                        // Choristor: Launches elongated white-gold ThreadProjectile across mid-long range!
                        _ai.LaunchChoristorThreadProjectile();
                        break;

                    case ActiveMoveKind.ChargeDash:
                        // Choir Hornhead: High-speed ground bull charge!
                        _ai._enemy.SetChargeState(true);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.PrimaryAttackOffset, c.PrimaryAttackSize,
                            c.PrimaryAttackDamage, c.PrimaryKnockback,
                            new Color(0.98f, 0.92f, 0.76f, 0.80f), 0.72f);
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, face, false);
                        break;

                    case ActiveMoveKind.HeavySwing:
                        // Choir Elder Pattern 1: Slow heavy close-range swing (2 DAMAGE!)
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.PrimaryAttackOffset, c.PrimaryAttackSize,
                            c.PrimaryAttackDamage, c.PrimaryKnockback,
                            new Color(1f, 0.84f, 0.38f, 0.95f), 1.25f);
                        ChoralCodeVFX.SpawnSlashArc(
                            _ai.transform.position + new Vector3(face * 0.85f, 0.96f, 0f),
                            face, 1.95f, 1.15f, 145f, 0.26f, new Color(1f, 0.84f, 0.38f, 0.96f), 0.17f);
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, face, true);
                        GameEvents.RaiseCameraShake(0.068f, 0.095f);
                        break;

                    case ActiveMoveKind.StepStrike:
                        // Choir Elder Pattern 2: Fast forward step-lunge slash closing mid-range (2 DAMAGE!)
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.SecondaryAttackOffset, c.SecondaryAttackSize,
                            c.SecondaryAttackDamage, c.SecondaryKnockback,
                            new Color(1f, 0.90f, 0.52f, 0.95f), 1.18f);
                        ChoralCodeVFX.SpawnSlashArc(
                            _ai.transform.position + new Vector3(face * 0.95f, 0.92f, 0f),
                            face, 1.80f, 0.92f, 115f, 0.22f, new Color(1f, 0.90f, 0.52f, 0.96f), 0.15f);
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, face, false);
                        break;

                    case ActiveMoveKind.SwoopDive:
                        // Choir Flyer: Fast direct aerial swoop toward locked player coordinates!
                        _ai._enemy.SetChargeState(true);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        _ai._enemy.ActivateCustomAttackHitbox(
                            c.PrimaryAttackOffset, c.PrimaryAttackSize,
                            c.PrimaryAttackDamage, c.PrimaryKnockback,
                            new Color(1f, 0.92f, 0.60f, 0.86f), 0.78f);
                        if (VFXManager.Instance != null)
                        {
                            VFXManager.Instance.SpawnMovementStreaks(_ai.transform.position, _ai._lockedAirDir, 4, 0.42f);
                        }
                        break;
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;

                // Grand Reed DoubleThrustCombo: Trigger 2nd piercing thrust mid-combo (2 DAMAGE!)
                if (_ai._chosenMove == ActiveMoveKind.DoubleThrustCombo && !_secondStrikeFired && _timer <= 0.21f)
                {
                    _secondStrikeFired = true;
                    _ai.FacePlayer();
                    int face = _ai._enemy.FacingDirection;
                    ENEMY_COMBAT_CONFIG.Profile c = _ai._cfg;
                    _ai._enemy.DeactivateAttackHitbox();
                    _ai._enemy.ActivateCustomAttackHitbox(
                        c.SecondaryAttackOffset + new Vector2(0.20f, 0.06f), c.SecondaryAttackSize,
                        c.SecondaryAttackDamage, c.SecondaryKnockback * 1.05f,
                        new Color(1f, 0.98f, 0.82f, 0.98f), 1.32f);
                    ChoralCodeVFX.SpawnThrustStreak(
                        _ai.transform.position + new Vector3(face * 0.55f, 1.08f, 0f),
                        face, 3.55f, 0.22f, Color.white, 0.15f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                }

                // Hornhead dust trail while charging
                if (_ai._chosenMove == ActiveMoveKind.ChargeDash)
                {
                    _chargeDustTimer -= dt;
                    if (_chargeDustTimer <= 0f)
                    {
                        _chargeDustTimer = 0.14f;
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._enemy.FacingDirection, false);
                    }
                    if (Mathf.Abs(_ai.transform.position.x - _ai._chargeStartX) >= _ai._cfg.MaxChargeDistance)
                    {
                        _ai.TransitionTo(_ai._recoverState);
                        return;
                    }
                }

                if (_timer <= 0f)
                {
                    // Reed follows up QuickSlash / ForwardLunge with nimble Backstep!
                    if (_ai.archetype == ChoralArchetype.Reed && _ai.GetHorizDistToPlayer() <= 3.8f)
                    {
                        _ai._attackCooldownTimer = _ai._cfg.AttackCooldown;
                        _ai.TransitionTo(_ai._retreatHopState);
                        return;
                    }
                    _ai.TransitionTo(_ai._recoverState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._chosenMove == ActiveMoveKind.SwoopDive)
                {
                    if (_ai._rb == null) return;
                    float spd = _ai._cfg.LungeSpeed;
                    RaycastHit2D hit = Physics2D.CircleCast(_ai._rb.position, 0.44f, _ai._lockedAirDir, spd * fixedDt + 0.08f, _ai._solidTileMask);
                    if (hit.collider != null)
                    {
                        _ai._rb.velocity = Vector2.zero;
                        _ai.TransitionTo(_ai._recoverState);
                        return;
                    }
                    _ai._rb.velocity = _ai._lockedAirDir * spd;
                    return;
                }

                if (_ai._chosenMove == ActiveMoveKind.CastThread || _ai._chosenMove == ActiveMoveKind.HeavySwing)
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                int dir = _ai._enemy.FacingDirection;
                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir))
                {
                    _ai._enemy.StopHorizontal();
                    if (_ai._chosenMove == ActiveMoveKind.ChargeDash)
                    {
                        _ai.TransitionTo(_ai._recoverState);
                    }
                    return;
                }

                float moveSpeed = _ai._cfg.LungeSpeed;
                if (_ai._chosenMove == ActiveMoveKind.WideHeavySlash) moveSpeed *= 0.35f;
                else if (_ai._chosenMove == ActiveMoveKind.QuickSlash) moveSpeed *= 0.42f;
                else if (_ai._chosenMove == ActiveMoveKind.DoubleThrustCombo) moveSpeed *= 0.72f;

                _ai._enemy.SetHorizontalVelocity(dir * moveSpeed);
            }

            public void Exit()
            {
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.SetChargeState(false);
            }
        }

        // =========================================================================
        // 7. RECOVERY / SKID STOP (Hornhead) / RECOVER CLIMB (Flyer) / COOLDOWN
        // =========================================================================
        private sealed class RecoverState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            private float _timer;
            private float _skidVx;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public RecoverState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai._enemy.DeactivateAttackHitbox();
                _ai._attackCooldownTimer = _ai._cfg.AttackCooldown;

                if (_ai.archetype == ChoralArchetype.ChoirHornhead && _ai._chosenMove == ActiveMoveKind.ChargeDash)
                {
                    // SkidStop deceleration slide with skid dust!
                    _timer = _ai._cfg.SkidDuration;
                    _skidVx = _ai._rb != null ? _ai._rb.velocity.x * 0.65f : 0f;
                    ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._enemy.FacingDirection, true);
                }
                else
                {
                    _timer = _ai._cfg.RecoveryDuration;
                    _skidVx = 0f;
                    if (!_ai._cfg.IsFlying) _ai._enemy.StopHorizontal();
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai.CanDetectPlayer() ? (IEnemyState)_ai._engageState : _ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._cfg.IsFlying && _ai._rb != null)
                {
                    // Choir Flyer RecoverClimb: ascend cleanly back to safe hover height!
                    float targetY = _ai._safeHoverAltitudeY;
                    if (_ai.IsPlayerAlive()) targetY = Mathf.Max(targetY, _ai.PlayerChestPos().y + 2.6f);
                    float dy = targetY - _ai._rb.position.y;
                    Vector2 climbVel = new Vector2(-_ai._enemy.FacingDirection * 1.4f, Mathf.Clamp(dy * 2.4f, -2.5f, 4.8f));
                    _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, climbVel, 14f * fixedDt);
                    return;
                }

                if (_ai.archetype == ChoralArchetype.ChoirHornhead && Mathf.Abs(_skidVx) > 0.1f)
                {
                    int sDir = _skidVx >= 0f ? 1 : -1;
                    if (!_ai._enemy.HasGroundAhead(sDir) || _ai._enemy.HasWallAhead(sDir) || _ai._overheadPauseTimer > 0f)
                    {
                        _skidVx = 0f;
                        _ai._enemy.StopHorizontal();
                    }
                    else
                    {
                        _skidVx = Mathf.MoveTowards(_skidVx, 0f, 18.5f * fixedDt);
                        _ai._enemy.SetHorizontalVelocity(_skidVx);
                    }
                }
                else
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit()
            {
                _skidVx = 0f;
            }
        }

        // =========================================================================
        // 8. STAGGER & DEATH STATES
        // =========================================================================
        private sealed class StaggerState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    _ai.TransitionTo(_ai.CanDetectPlayer() ? (IEnemyState)_ai._engageState : _ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._cfg.IsFlying && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 15.0f * fixedDt);
                }
            }

            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly ChoralEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(ChoralEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.SetBlockStance(false);
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.StopHorizontal();
            }

            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
