using UnityEngine;

namespace SilkenGrotto
{
    public enum ShellwoodArchetype
    {
        Aknid,
        WingedPilgrim,
        PilgrimGroveller,
        PilgrimHulk,
        PilgrimBellbearer,
        CovetousPilgrim,
        Pondcatcher
    }

    /// <summary>
    /// Refactored 60 FPS Finite State Machine AI for Batch 3 — All 5 Pilgrim Variants
    /// (preserving all existing reference sprites & 12-24 FPS sprite sheets completely untouched):
    /// 1. Pilgrim Groveller (Basic Floor Jumper): SlowWalkPatrol -> JumpTelegraph (0.20s) -> LungingHop -> LandPause (0.45s).
    /// 2. Winged Pilgrim (Diving Aerial Pilgrim): AirCirclePatrol -> DiveTelegraph (0.30s lock) -> BodyThrowDive -> BrakeAndRise (0.52s).
    /// 3. Pilgrim Bellbearer (Ranged Bell Thrower): SpacingWalk -> BellThrowWindup (0.35s) -> ThrowBell (deflectable parabolic BellProjectile) -> Cooldown (1.30s).
    /// 4. Covetous Pilgrim (High-Reward Multi-Move Brawler): MarchPatrol -> patterned 3-move kit (ChainSwing gold arc, HeavyStepSlam coin burst, BackstepGuard) + hit-drop & 5x rosary jackpot!
    /// 5. Pilgrim Hulk (Rolling Charger): HeavyWalk -> RollCurlTelegraph (0.35s) -> RollingCharge (continuous 360-deg spin + dust) -> WallBonk / UncurlRecover (0.60s).
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class ShellwoodEnemyAI : MonoBehaviour
    {
        private enum CovetousMove
        {
            ChainSwing,
            HeavyStepSlam,
            BackstepGuard
        }

        [Header("Archetype")]
        [SerializeField] private ShellwoodArchetype archetype = ShellwoodArchetype.PilgrimGroveller;

        [Header("Legacy Sprite Hook (Kept For Serialized Compatibility)")]
        [SerializeField] private Sprite bellProjectileSprite;

        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hoverAmplitude = 0.48f;
        [SerializeField] private float hoverFrequency = 2.4f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.PilgrimProfile _profile;

        private LayerMask _solidTileMask;
        private LayerMask _groundMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private Vector3 _anchorPosition;

        private int _patrolDir = 1;
        private float _attackCooldownTimer;
        private float _animPhase;
        private float _pogoRecoilTimer;
        private float _landSquashTimer;

        // Winged Pilgrim dive lock
        private Vector2 _lockedAirDir = Vector2.right;
        private bool _isDiving;

        // Covetous Pilgrim multi-move state
        private CovetousMove _selectedCovetousMove = CovetousMove.ChainSwing;
        private CovetousMove _lastCovetousMove = CovetousMove.BackstepGuard;
        private int _covetousAttackCounter;

        // Pilgrim Hulk rolling charge state
        private float _rollSpinAngle;
        private bool _isRollingCharge;
        private bool _didWallBonk;
        private float _rollDustTimer;

        // Reusable FSM States
        private PatrolState _patrolState;
        private ChaseState _chaseState;
        private WindupState _windupState;
        private AttackState _attackState;
        private RecoverState _recoverState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        public ShellwoodArchetype Archetype { get { return archetype; } }

        public void ConfigureArchetype(ShellwoodArchetype kind, Sprite bellSprite)
        {
            archetype = kind;
            if (bellSprite != null) bellProjectileSprite = bellSprite;
            ENEMY_COMBAT_CONFIG.PilgrimProfile p = ENEMY_COMBAT_CONFIG.GetPilgrim(kind);
            if (p.HoverAmplitude > 0.01f) hoverAmplitude = p.HoverAmplitude;
            if (p.HoverFrequency > 0.01f) hoverFrequency = p.HoverFrequency;
        }

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _fsm = new EnemyStateMachine();
            _profile = ENEMY_COMBAT_CONFIG.GetPilgrim(archetype);

            _solidTileMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _groundMask = _solidTileMask | (1 << GameConstants.LayerOneWayPlatform);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.65f) % 6.28f;
            _patrolDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;
            _anchorPosition = transform.position;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualScale.y = Mathf.Abs(_baseVisualScale.y);
            _baseVisualLocalPos = visualTransform.localPosition;

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
            _profile = ENEMY_COMBAT_CONFIG.GetPilgrim(archetype);
            _anchorPosition = transform.position;
            ResolvePlayer();

            if (IsAirborneArchetype() && _rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.drag = 0f;
            }

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
            if (_pogoRecoilTimer > 0f) _pogoRecoilTimer -= dt;
            if (_landSquashTimer > 0f) _landSquashTimer -= dt;
            _animPhase += dt;

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
            bool isSolid = l == GameConstants.LayerGround || l == GameConstants.LayerBreakableWall || l == GameConstants.LayerOneWayPlatform;
            if (!isSolid) return;

            if (IsAirborneArchetype() && _fsm.CurrentState == _attackState)
            {
                if (_rb != null) _rb.velocity = -_lockedAirDir * 2.2f;
                TransitionTo(_recoverState);
            }
        }

        private bool IsAirborneArchetype()
        {
            return archetype == ShellwoodArchetype.WingedPilgrim || archetype == ShellwoodArchetype.Aknid;
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

            // Covetous Pilgrim hit-drop rosaries: gleaming coins scatter + chime on every hit!
            if (archetype == ShellwoodArchetype.CovetousPilgrim && _profile.DropRosariesOnHit)
            {
                ChoralCodeVFX.SpawnRosaryCoinShower(transform.position + new Vector3(0f, 1.0f, 0f), 5, false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.56f);
                if (_profile.HitDropRosaryCount > 0)
                {
                    _enemy.SpawnBonusLootOrbs(_profile.HitDropRosaryCount, _profile.HitDropRosaryCount * 4);
                }
            }

            if (info.IsDownwardPogo)
            {
                _pogoRecoilTimer = 0.18f;
                ChoralCodeVFX.SpawnMossPogoBurst(
                    transform.position + new Vector3(0f, IsAirborneArchetype() ? 0.45f : 1.05f, 0f),
                    new Color(0.96f, 0.88f, 0.58f, 0.95f));
                if (IsAirborneArchetype() && _rb != null)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.35f, -3.2f);
                }
            }

            // Pilgrim Hulk resists hit-stagger while actively rolling (heavyweight boulder charger — player can jump or pogo over!)
            if (archetype == ShellwoodArchetype.PilgrimHulk && _isRollingCharge)
            {
                return;
            }
            if (archetype == ShellwoodArchetype.CovetousPilgrim && _fsm.CurrentState == _attackState && _selectedCovetousMove == CovetousMove.HeavyStepSlam)
            {
                return;
            }

            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            _isDiving = false;
            _isRollingCharge = false;
            if (archetype == ShellwoodArchetype.CovetousPilgrim)
            {
                ChoralCodeVFX.SpawnRosaryCoinShower(transform.position + new Vector3(0f, 1.0f, 0f), 16, true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.95f);
            }
            TransitionTo(_deathState);
            if (visualTransform != null)
            {
                visualTransform.localRotation = Quaternion.identity;
            }
        }

        private bool IsPlayerAlive()
        {
            return _playerTransform != null && (_playerController == null || _playerController.IsAlive);
        }

        private Vector2 PlayerChestPos()
        {
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 0.95f) : (Vector2)transform.position;
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
            float dx = _playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.10f)
            {
                _enemy.SetFacing(dx >= 0f ? 1 : -1);
            }
        }

        private bool IsGroundedNow()
        {
            return Physics2D.Raycast(transform.position, Vector2.down, 0.34f, _groundMask).collider != null;
        }

        private bool CanDetectPlayer()
        {
            if (!IsPlayerAlive()) return false;
            float maxDy = IsAirborneArchetype() ? 7.6f : (archetype == ShellwoodArchetype.PilgrimBellbearer ? 5.8f : 3.0f);
            if (GetDistToPlayer() > _profile.DetectRange || GetVertDeltaToPlayer() > maxDy) return false;

            if (IsAirborneArchetype() || archetype == ShellwoodArchetype.PilgrimBellbearer)
            {
                Vector2 myPos = (Vector2)transform.position + Vector2.up * 0.7f;
                RaycastHit2D hit = Physics2D.Linecast(myPos, PlayerChestPos(), _solidTileMask);
                if (hit.collider != null && Vector2.Distance(myPos, PlayerChestPos()) > 3.2f) return false;
            }
            return true;
        }

        private bool ShouldLosePlayer()
        {
            if (!IsPlayerAlive()) return true;
            return GetDistToPlayer() > _profile.LoseTargetRange || GetVertDeltaToPlayer() > 9.0f;
        }

        private bool InTriggerAttackRange()
        {
            if (!IsPlayerAlive()) return false;
            if (archetype == ShellwoodArchetype.PilgrimHulk)
            {
                // Hulk starts rolling charge when horizontally aligned inside range
                return GetDistToPlayer() <= _profile.AttackRange && GetVertDeltaToPlayer() <= 2.3f;
            }
            if (archetype == ShellwoodArchetype.PilgrimBellbearer)
            {
                float d = GetDistToPlayer();
                return d <= _profile.AttackRange && d >= 1.8f && GetVertDeltaToPlayer() <= 5.4f;
            }
            if (IsAirborneArchetype())
            {
                return GetDistToPlayer() <= _profile.AttackRange && GetVertDeltaToPlayer() <= 6.8f;
            }
            return GetDistToPlayer() <= _profile.AttackRange && GetVertDeltaToPlayer() <= 2.4f;
        }

        private CovetousMove PickNextCovetousMove()
        {
            _covetousAttackCounter++;
            float d = GetDistToPlayer();
            if (d < 2.15f && _lastCovetousMove != CovetousMove.BackstepGuard)
            {
                return CovetousMove.BackstepGuard;
            }

            int pick = (_covetousAttackCounter + Random.Range(0, 2)) % 3;
            CovetousMove candidate = pick == 0
                ? CovetousMove.ChainSwing
                : (pick == 1 ? CovetousMove.HeavyStepSlam : CovetousMove.BackstepGuard);

            if (candidate == _lastCovetousMove)
            {
                candidate = candidate == CovetousMove.ChainSwing ? CovetousMove.HeavyStepSlam : CovetousMove.ChainSwing;
            }
            return candidate;
        }

        private void LaunchBellAtPlayer()
        {
            if (!IsPlayerAlive()) return;
            Vector3 spawnPos = transform.position + new Vector3(_enemy.FacingDirection * 0.55f, 1.05f, 0f);
            Vector2 toTarget = PlayerChestPos() - (Vector2)spawnPos;
            float distX = Mathf.Abs(toTarget.x);
            float flightT = Mathf.Clamp(distX / Mathf.Max(4f, _profile.BellThrowSpeed), 0.35f, 0.92f);
            float vx = Mathf.Clamp(toTarget.x / flightT, -11.8f, 11.8f);
            float g = _profile.BellGravity > 1f ? _profile.BellGravity : 14.5f;
            float vy = Mathf.Clamp((toTarget.y + 0.5f * g * flightT * flightT) / flightT, 3.2f, 12.8f);

            GameObject go = new GameObject("PilgrimBell_Projectile");
            go.transform.position = spawnPos;
            go.AddComponent<CircleCollider2D>();
            BellProjectile proj = go.AddComponent<BellProjectile>();
            // Pass null sprite so BellProjectile renders its pure code-drawn rotating golden-bronze bell!
            proj.LaunchParabolic(spawnPos, new Vector2(vx, vy), g, _profile.AttackDamage, _profile.ContactKnockback, _profile.CanDeflectBell, gameObject, null);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.72f);
        }

        private void ApplyProceduralVisualJuice()
        {
            if (visualTransform == null) return;
            float dir = _enemy.FacingDirection >= 0 ? 1f : -1f;
            EnemyAIStateType st = _enemy.CurrentAIState;

            float rotZ = 0f;
            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            Vector3 offset = Vector3.zero;

            if (st != EnemyAIStateType.Death)
            {
                if (archetype == ShellwoodArchetype.PilgrimHulk)
                {
                    if (_isRollingCharge)
                    {
                        // Hand-drawn pilgrim_hulk_sheet.png Row 2 (Frames 3-6) natively animates full 360-deg roll + floor dust!
                        _rollSpinAngle = 0f;
                        rotZ = 0f;
                        offset.y = Mathf.Abs(Mathf.Sin(_animPhase * 18f)) * 0.04f;
                    }
                    else if (st == EnemyAIStateType.Windup)
                    {
                        _rollSpinAngle = 0f;
                        offset.x = Mathf.Sin(_animPhase * 62f) * 0.03f;
                    }
                    else
                    {
                        _rollSpinAngle = 0f;
                        if (_didWallBonk && st == EnemyAIStateType.Recover)
                        {
                            rotZ = dir * Mathf.Sin(_animPhase * 20f) * 5f;
                        }
                    }
                }
                else if (archetype == ShellwoodArchetype.PilgrimGroveller)
                {
                    if (st == EnemyAIStateType.Windup)
                    {
                        // JumpTelegraph crouch squash
                        sx *= 1.22f;
                        sy *= 0.74f;
                        offset.y = -0.07f;
                    }
                    else if (st == EnemyAIStateType.Attack)
                    {
                        sx *= 0.90f;
                        sy *= 1.14f;
                        rotZ = -dir * 10f;
                    }
                    else if (_landSquashTimer > 0f)
                    {
                        float sq = Mathf.Sin((_landSquashTimer / 0.16f) * Mathf.PI);
                        sx *= 1f + 0.18f * sq;
                        sy *= 1f - 0.16f * sq;
                    }
                }
                else if (archetype == ShellwoodArchetype.WingedPilgrim)
                {
                    if (st == EnemyAIStateType.Windup)
                    {
                        // DiveTelegraph pull-back & jitter
                        rotZ = dir * 16f;
                        offset = new Vector3(-_lockedAirDir.x * 0.12f + Mathf.Sin(_animPhase * 60f) * 0.03f, -_lockedAirDir.y * 0.12f, 0f);
                    }
                    else if (_isDiving && st == EnemyAIStateType.Attack)
                    {
                        // Sleek forward dive tilt aligned along locked vector
                        float diveDeg = Mathf.Atan2(_lockedAirDir.y, Mathf.Max(0.1f, Mathf.Abs(_lockedAirDir.x))) * Mathf.Rad2Deg;
                        rotZ = dir * (diveDeg * 0.7f - 10f);
                        sx *= 1.14f;
                        sy *= 0.88f;
                    }
                    else
                    {
                        offset.y = Mathf.Sin(_animPhase * 4.2f) * 0.06f;
                        float vx = _rb != null ? _rb.velocity.x : 0f;
                        rotZ = Mathf.Clamp(-vx * 1.6f, -9f, 9f);
                    }
                }
                else if (archetype == ShellwoodArchetype.PilgrimBellbearer)
                {
                    if (st == EnemyAIStateType.Windup)
                    {
                        rotZ = dir * 13f;
                        offset.x = -dir * 0.10f;
                    }
                    else if (st == EnemyAIStateType.Attack)
                    {
                        rotZ = -dir * 14f;
                        offset.x = dir * 0.12f;
                    }
                }
                else if (archetype == ShellwoodArchetype.CovetousPilgrim)
                {
                    if (st == EnemyAIStateType.Windup)
                    {
                        rotZ = dir * (_selectedCovetousMove == CovetousMove.HeavyStepSlam ? 16f : 11f);
                        sx *= 0.94f;
                        sy *= 1.08f;
                    }
                    else if (st == EnemyAIStateType.Attack)
                    {
                        rotZ = -dir * 12f;
                        sx *= 1.10f;
                        sy *= 0.92f;
                    }
                }

                if (_pogoRecoilTimer > 0f)
                {
                    float dip = Mathf.Sin((_pogoRecoilTimer / 0.18f) * Mathf.PI);
                    sx *= 1f + 0.16f * dip;
                    sy *= 1f - 0.16f * dip;
                    offset.y -= 0.12f * dip;
                }
            }

            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
        }

        // =========================================================================
        // FINITE STATE MACHINE STATES
        // =========================================================================

        private sealed class PatrolState : IEnemyState
        {
            private readonly ShellwoodEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public PatrolState(ShellwoodEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDiving = false;
                _ai._isRollingCharge = false;
                _ai._enemy.SetChargeState(false);
                _ai._enemy.SetFacing(_ai._patrolDir);
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.FacePlayer();
                    // Groveller jumps directly toward player on sight!
                    if (_ai.archetype == ShellwoodArchetype.PilgrimGroveller && _ai._attackCooldownTimer <= 0f)
                    {
                        _ai.TransitionTo(_ai._windupState);
                        return;
                    }
                    // Winged Pilgrim dives right after spotting player!
                    if (_ai.archetype == ShellwoodArchetype.WingedPilgrim && _ai._attackCooldownTimer <= 0f && _ai.InTriggerAttackRange())
                    {
                        _ai.TransitionTo(_ai._windupState);
                        return;
                    }
                    _ai.TransitionTo(_ai._chaseState);
                    return;
                }

                if (!_ai.IsAirborneArchetype())
                {
                    float maxDist = _ai._profile.PatrolSpanX > 0.5f ? _ai._profile.PatrolSpanX : 3.8f;
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
                    // Winged Pilgrim: calm circular aerial patrol (AirCirclePatrol)
                    Vector2 anchor = _ai._anchorPosition;
                    float xOff = Mathf.Sin(_ai._animPhase * 1.25f) * _ai._profile.AirCircleRadiusX;
                    float yOff = Mathf.Cos(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude;
                    Vector2 desiredPos = anchor + new Vector2(xOff, yOff);
                    Vector2 vel = (desiredPos - _ai._rb.position) * 2.8f;
                    if (Mathf.Abs(vel.x) > 0.22f) _ai._enemy.SetFacing(vel.x >= 0f ? 1 : -1);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 5.2f));
                    return;
                }

                if (!_ai._enemy.HasGroundAhead(_ai._patrolDir) || _ai._enemy.HasWallAhead(_ai._patrolDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(_ai._patrolDir * _ai._profile.PatrolSpeed);
            }

            public void Exit() { }
        }

        private sealed class ChaseState : IEnemyState
        {
            private readonly ShellwoodEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }
            public ChaseState(ShellwoodEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
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

                if (_ai.archetype == ShellwoodArchetype.WingedPilgrim)
                {
                    if (_ai._rb == null) return;
                    Vector2 target = _ai.PlayerChestPos() + new Vector2(-dir * 4.4f, 2.4f);
                    Vector2 toTarget = target - _ai._rb.position;
                    Vector2 desired = toTarget.sqrMagnitude > 0.20f ? toTarget.normalized * _ai._profile.AggroSpeed : Vector2.zero;
                    desired.y += Mathf.Sin(_ai._animPhase * 3.2f) * 0.45f;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, desired, Mathf.Clamp01(fixedDt * 5.5f));
                    return;
                }

                if (_ai.archetype == ShellwoodArchetype.PilgrimBellbearer)
                {
                    // SpacingWalk: keep comfortable medium bell-throwing distance, backing away cautiously (Row 3 RetreatStep via IsCharging)
                    float dist = _ai.GetDistToPlayer();
                    if (dist < _ai._profile.IdealMinRange && _ai._enemy.HasGroundAhead(-dir) && !_ai._enemy.HasWallAhead(-dir))
                    {
                        _ai._enemy.SetChargeState(true);
                        _ai._enemy.SetHorizontalVelocity(-dir * _ai._profile.PatrolSpeed);
                    }
                    else if (dist > _ai._profile.IdealMaxRange && _ai._enemy.HasGroundAhead(dir) && !_ai._enemy.HasWallAhead(dir))
                    {
                        _ai._enemy.SetChargeState(false);
                        _ai._enemy.SetHorizontalVelocity(dir * _ai._profile.AggroSpeed);
                    }
                    else
                    {
                        _ai._enemy.SetChargeState(false);
                        _ai._enemy.StopHorizontal();
                    }
                    return;
                }

                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir) || _ai.InTriggerAttackRange())
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(dir * _ai._profile.AggroSpeed);
            }

            public void Exit() { _ai._enemy.SetChargeState(false); }
        }

        private sealed class WindupState : IEnemyState
        {
            private readonly ShellwoodEnemyAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public WindupState(ShellwoodEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                if (!_ai.IsAirborneArchetype()) _ai._enemy.StopHorizontal();

                switch (_ai.archetype)
                {
                    case ShellwoodArchetype.PilgrimGroveller:
                        _timer = _ai._profile.JumpTelegraphDuration; // 0.20s
                        break;

                    case ShellwoodArchetype.WingedPilgrim:
                        _timer = _ai._profile.DiveTelegraphDuration; // 0.30s lock on player coordinate!
                        LockWingedDiveDir();
                        ChoralCodeVFX.SpawnTelegraphGlint(_ai.transform.position, new Color(1.0f, 0.94f, 0.72f, 0.92f), 0.42f, 0.26f);
                        break;

                    case ShellwoodArchetype.PilgrimBellbearer:
                        _timer = _ai._profile.BellWindupDuration; // 0.35s
                        ChoralCodeVFX.SpawnTelegraphGlint(_ai.transform.position + new Vector3(_ai._enemy.FacingDirection * 0.45f, 1.05f, 0f), new Color(1.0f, 0.84f, 0.42f, 0.95f), 0.44f, 0.30f);
                        break;

                    case ShellwoodArchetype.CovetousPilgrim:
                        _ai._selectedCovetousMove = _ai.PickNextCovetousMove();
                        _ai._lastCovetousMove = _ai._selectedCovetousMove;
                        if (_ai._selectedCovetousMove == CovetousMove.BackstepGuard)
                        {
                            _timer = 0.08f; // Immediate nimble backstep -> Row 4 BackstepGuard (via IsTurning)
                            _ai._enemy.SetTurnState(true);
                            _ai._enemy.SetChargeState(false);
                        }
                        else if (_ai._selectedCovetousMove == CovetousMove.HeavyStepSlam)
                        {
                            _timer = _ai._profile.HeavySlamWindup; // 0.42s -> Row 3 HeavyStepSlam (via IsCharging)
                            _ai._enemy.SetTurnState(false);
                            _ai._enemy.SetChargeState(true);
                            ChoralCodeVFX.SpawnTelegraphGlint(_ai.transform.position + new Vector3(_ai._enemy.FacingDirection * 0.4f, 1.55f, 0f), new Color(1.0f, 0.86f, 0.36f, 0.98f), 0.58f, 0.36f);
                        }
                        else
                        {
                            _timer = _ai._profile.ChainSwingWindup; // 0.32s -> Row 2 ChainSwing
                            _ai._enemy.SetTurnState(false);
                            _ai._enemy.SetChargeState(false);
                            ChoralCodeVFX.SpawnTelegraphGlint(_ai.transform.position + new Vector3(_ai._enemy.FacingDirection * 0.5f, 1.05f, 0f), new Color(1.0f, 0.88f, 0.42f, 0.95f), 0.48f, 0.28f);
                        }
                        break;

                    case ShellwoodArchetype.PilgrimHulk:
                        _timer = _ai._profile.RollCurlTelegraphDuration; // 0.35s RollCurlTelegraph
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._enemy.FacingDirection, false);
                        break;

                    default:
                        _timer = 0.25f;
                        break;
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            private void LockWingedDiveDir()
            {
                if (!_ai.IsPlayerAlive()) return;
                Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.transform.position;
                _ai._lockedAirDir = delta.sqrMagnitude > 0.01f ? delta.normalized : new Vector2(_ai._enemy.FacingDirection, -0.35f).normalized;
                if (Mathf.Abs(_ai._lockedAirDir.x) > 0.05f) _ai._enemy.SetFacing(_ai._lockedAirDir.x >= 0f ? 1 : -1);
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                // Winged Pilgrim locks target strictly early so player can dodge cleanly
                if (_ai.archetype != ShellwoodArchetype.WingedPilgrim && _timer > 0.08f)
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
                if (_ai.IsAirborneArchetype() && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, -_ai._lockedAirDir * 1.2f, Mathf.Clamp01(fixedDt * 10f));
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
            private readonly ShellwoodEnemyAI _ai;
            private float _timer;
            private float _elapsed;
            private int _backstepDir;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public AttackState(ShellwoodEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _elapsed = 0f;
                _ai._isDiving = false;
                _ai._isRollingCharge = false;
                _ai._didWallBonk = false;

                switch (_ai.archetype)
                {
                    case ShellwoodArchetype.PilgrimGroveller:
                    {
                        _ai.FacePlayer();
                        _timer = 0.70f;
                        int dir = _ai._enemy.FacingDirection;
                        if (_ai._rb != null)
                        {
                            float vx = _ai._enemy.HasGroundAhead(dir) ? dir * _ai._profile.HopSpeedX : dir * (_ai._profile.HopSpeedX * 0.45f);
                            _ai._rb.velocity = new Vector2(vx, _ai._profile.HopVelY);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
                        break;
                    }

                    case ShellwoodArchetype.WingedPilgrim:
                    {
                        _ai._isDiving = true;
                        _timer = _ai._profile.DiveDuration; // 0.48s straight diagonal body throw!
                        if (Mathf.Abs(_ai._lockedAirDir.x) > 0.05f) _ai._enemy.SetFacing(_ai._lockedAirDir.x >= 0f ? 1 : -1);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        break;
                    }

                    case ShellwoodArchetype.PilgrimBellbearer:
                    {
                        _ai.FacePlayer();
                        _ai.LaunchBellAtPlayer();
                        _timer = 0.22f;
                        break;
                    }

                    case ShellwoodArchetype.CovetousPilgrim:
                    {
                        _ai.FacePlayer();
                        int dir = _ai._enemy.FacingDirection;
                        if (_ai._selectedCovetousMove == CovetousMove.BackstepGuard)
                        {
                            _timer = _ai._profile.BackstepDuration; // 0.24s short backstep
                            _backstepDir = -dir;
                            _ai._enemy.SetTurnState(true);
                            _ai._enemy.SetChargeState(false);
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        }
                        else if (_ai._selectedCovetousMove == CovetousMove.HeavyStepSlam)
                        {
                            _timer = 0.30f;
                            _ai._enemy.SetTurnState(false);
                            _ai._enemy.SetChargeState(true);
                            if (_ai._enemy.HasGroundAhead(dir) && !_ai._enemy.HasWallAhead(dir))
                            {
                                _ai._enemy.SetHorizontalVelocity(dir * 5.2f);
                            }
                            _ai._enemy.ActivateCustomAttackHitbox(
                                new Vector2(1.25f, 0.72f),
                                new Vector2(2.85f, 1.65f),
                                _ai._profile.AttackDamage,
                                9.4f,
                                new Color(1.0f, 0.84f, 0.32f, 0.90f),
                                0.0f);
                            ChoralCodeVFX.SpawnRosaryCoinShower(_ai.transform.position + new Vector3(dir * 1.15f, 0.18f, 0f), 9, true);
                            ChoralCodeVFX.SpawnChargeDust(_ai.transform.position + new Vector3(dir * 1.1f, 0f, 0f), dir, true);
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.88f);
                            GameEvents.RaiseCameraShake(0.058f, 0.085f);
                        }
                        else // ChainSwing
                        {
                            _timer = 0.26f;
                            _ai._enemy.SetTurnState(false);
                            _ai._enemy.SetChargeState(false);
                            _ai._enemy.ActivateCustomAttackHitbox(
                                new Vector2(1.42f, 0.98f),
                                new Vector2(3.35f, 1.90f),
                                _ai._profile.AttackDamage,
                                8.2f,
                                new Color(1.0f, 0.86f, 0.34f, 0.94f),
                                0.0f);
                            ChoralCodeVFX.SpawnGoldenChainArc(_ai.transform.position + new Vector3(dir * 0.65f, 1.02f, 0f), dir, 2.25f, 1.15f, 0.19f);
                            ChoralCodeVFX.SpawnRosaryCoinShower(_ai.transform.position + new Vector3(dir * 1.25f, 1.0f, 0f), 4, false);
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(2);
                        }
                        break;
                    }

                    case ShellwoodArchetype.PilgrimHulk:
                    {
                        _ai.FacePlayer();
                        _ai._isRollingCharge = true;
                        _ai._rollSpinAngle = 0f;
                        _ai._rollDustTimer = 0f;
                        _timer = _ai._profile.RollMaxDuration; // Up to 1.35s roll until wall or ledge!
                        _ai._enemy.SetChargeState(true);
                        _ai._enemy.ActivateCustomAttackHitbox(
                            new Vector2(0.45f, 0.72f),
                            new Vector2(1.85f, 1.42f),
                            _ai._profile.RollingChargeDamage, // 2 damage during RollingCharge!
                            11.5f,
                            new Color(0.92f, 0.76f, 0.56f, 0.68f),
                            0.0f);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                        GameEvents.RaiseCameraShake(0.045f, 0.075f);
                        break;
                    }

                    default:
                        _timer = 0.25f;
                        break;
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                _elapsed += dt;

                if (_ai.archetype == ShellwoodArchetype.PilgrimGroveller)
                {
                    if ((_elapsed > 0.14f && _ai.IsGroundedNow() && (_ai._rb == null || _ai._rb.velocity.y <= 0.15f)) || _timer <= 0f)
                    {
                        _ai._landSquashTimer = 0.16f;
                        _ai.TransitionTo(_ai._recoverState);
                    }
                    return;
                }

                if (_ai.archetype == ShellwoodArchetype.PilgrimHulk && _ai._isRollingCharge)
                {
                    _ai._rollDustTimer -= dt;
                    if (_ai._rollDustTimer <= 0f)
                    {
                        _ai._rollDustTimer = 0.11f;
                        ChoralCodeVFX.SpawnRollingDustTrail(_ai.transform.position, _ai._enemy.FacingDirection);
                    }
                }

                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._recoverState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == ShellwoodArchetype.WingedPilgrim)
                {
                    if (_ai._rb == null) return;
                    float spd = _ai._profile.DiveSpeed;
                    RaycastHit2D hit = Physics2D.CircleCast(_ai._rb.position, 0.42f, _ai._lockedAirDir, spd * fixedDt + 0.10f, _ai._solidTileMask);
                    if (hit.collider != null)
                    {
                        _ai._rb.velocity = -_ai._lockedAirDir * 2.4f;
                        _ai.TransitionTo(_ai._recoverState);
                        return;
                    }
                    _ai._rb.velocity = _ai._lockedAirDir * spd;
                    return;
                }

                if (_ai.archetype == ShellwoodArchetype.PilgrimGroveller)
                {
                    if (_ai._enemy.HasWallAhead(_ai._enemy.FacingDirection) && _ai._rb != null)
                    {
                        _ai._rb.velocity = new Vector2(0f, _ai._rb.velocity.y);
                    }
                    return;
                }

                if (_ai.archetype == ShellwoodArchetype.PilgrimBellbearer)
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                if (_ai.archetype == ShellwoodArchetype.CovetousPilgrim)
                {
                    if (_ai._selectedCovetousMove == CovetousMove.BackstepGuard)
                    {
                        if (_ai._enemy.HasGroundAhead(_backstepDir) && !_ai._enemy.HasWallAhead(_backstepDir))
                            _ai._enemy.SetHorizontalVelocity(_backstepDir * _ai._profile.BackstepSpeed);
                        else
                            _ai._enemy.StopHorizontal();
                    }
                    else if (_ai._rb != null)
                    {
                        _ai._rb.velocity = new Vector2(Mathf.MoveTowards(_ai._rb.velocity.x, 0f, 18f * fixedDt), _ai._rb.velocity.y);
                    }
                    return;
                }

                // Pilgrim Hulk RollingCharge: rolls rapidly until hitting a solid wall or platform edge!
                int hDir = _ai._enemy.FacingDirection;
                if (_ai._enemy.HasWallAhead(hDir))
                {
                    // WallBonk! Rebound backward with impact sparks & heavy thud
                    _ai._didWallBonk = true;
                    _ai._isRollingCharge = false;
                    if (_ai._rb != null) _ai._rb.velocity = new Vector2(-hDir * 4.2f, 3.6f);
                    ChoralCodeVFX.SpawnBounceSparks(_ai.transform.position + new Vector3(hDir * 0.72f, 0.72f, 0f), new Vector2(-hDir, 0.25f));
                    ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, hDir, true);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.HollowBugShell, 0.92f);
                    GameEvents.RaiseCameraShake(0.085f, 0.12f);
                    _ai.TransitionTo(_ai._recoverState);
                    return;
                }
                if (!_ai._enemy.HasGroundAhead(hDir))
                {
                    _ai._isRollingCharge = false;
                    _ai._enemy.StopHorizontal();
                    _ai.TransitionTo(_ai._recoverState);
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(hDir * _ai._profile.RollSpeed);
            }

            public void Exit()
            {
                _ai._isDiving = false;
                _ai._isRollingCharge = false;
                _ai._enemy.DeactivateAttackHitbox();
            }
        }

        private sealed class RecoverState : IEnemyState
        {
            private readonly ShellwoodEnemyAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public RecoverState(ShellwoodEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.DeactivateAttackHitbox();
                _ai._attackCooldownTimer = _ai._profile.AttackCooldown;

                if (_ai.archetype == ShellwoodArchetype.PilgrimGroveller)
                {
                    _timer = _ai._profile.LandPauseDuration; // 0.45s LandPause
                    _ai._enemy.StopHorizontal();
                }
                else if (_ai.archetype == ShellwoodArchetype.WingedPilgrim)
                {
                    _timer = _ai._profile.BrakeRiseDuration; // 0.52s BrakeAndRise
                }
                else if (_ai.archetype == ShellwoodArchetype.PilgrimHulk)
                {
                    _timer = _ai._profile.WallBonkRecoverDuration; // 0.60s WallBonk / UncurlRecover
                    // IsTurning routes WallBonk (Row 3 F1-2 -> F3-6) vs smooth UncurlRecover (Row 3 F3-6)!
                    _ai._enemy.SetTurnState(_ai._didWallBonk);
                    _ai._enemy.SetChargeState(false);
                    if (!_ai._didWallBonk) _ai._enemy.StopHorizontal();
                }
                else if (_ai.archetype == ShellwoodArchetype.CovetousPilgrim && _ai._selectedCovetousMove == CovetousMove.BackstepGuard)
                {
                    _timer = 0.24f; // Holds Row 4 Frames 4-6 GuardStance cleanly!
                    _ai._attackCooldownTimer = 0.34f;
                    _ai._enemy.SetTurnState(true);
                    _ai._enemy.SetChargeState(false);
                    _ai._enemy.StopHorizontal();
                }
                else
                {
                    _timer = _ai._profile.RecoverDuration;
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._chaseState);
                    else _ai.TransitionTo(_ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == ShellwoodArchetype.WingedPilgrim && _ai._rb != null)
                {
                    // BrakeAndRise: decelerate smoothly and climb back toward patrol altitude
                    float climbY = Mathf.Clamp((_ai._anchorPosition.y - _ai._rb.position.y) * 1.8f, -1.5f, 3.8f);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, new Vector2(0f, climbY), Mathf.Clamp01(fixedDt * 5.2f));
                    return;
                }

                if (_ai.archetype == ShellwoodArchetype.PilgrimHulk && _ai._didWallBonk && _ai._rb != null)
                {
                    _ai._rb.velocity = new Vector2(Mathf.MoveTowards(_ai._rb.velocity.x, 0f, 16f * fixedDt), _ai._rb.velocity.y);
                    return;
                }

                _ai._enemy.StopHorizontal();
            }

            public void Exit()
            {
                _ai._didWallBonk = false;
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
            }
        }

        private sealed class StaggerState : IEnemyState
        {
            private readonly ShellwoodEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(ShellwoodEnemyAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDiving = false;
                _ai._isRollingCharge = false;
                _ai._enemy.DeactivateAttackHitbox();
            }

            public void Tick(float dt)
            {
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
            }

            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly ShellwoodEnemyAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(ShellwoodEnemyAI ai) { _ai = ai; }
            public void Enter()
            {
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.StopHorizontal();
            }
            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
