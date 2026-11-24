using UnityEngine;

namespace SilkenGrotto
{
    public enum CourtyardDrapeArchetype
    {
        Drapemite,
        Drapefly
    }

    /// <summary>
    /// Refactored 60 FPS Finite State Machine AI for Batch 2 Courtyard enemies
    /// (preserving drapemite.png, drapefly.png, and their 12-24 FPS sprite sheets untouched):
    /// 1. Drapemite (Aggressive Leaping Slasher):
    ///    GroundPatrol -> CrouchTelegraph (0.28s vertical squash) -> SlashLeap (fast parabolic leap + code-drawn front slash arc)
    ///    -> LandRecover (0.45s). HP 32 (2-3 hits), 1 damage.
    /// 2. Drapefly (Side-to-Side Sweeper):
    ///    AirHover -> SweepWindup (0.30s pull-back when aligned horizontally) -> SideToSideSweep (fast straight horizontal
    ///    corridor sweep to max distance or wall, then turn) -> TurnRecover (0.50s). HP 22 (2 hits), 1 damage, easy to air-pogo!
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class CourtyardDrapeAI : MonoBehaviour
    {
        [Header("Archetype")]
        [SerializeField] private CourtyardDrapeArchetype archetype = CourtyardDrapeArchetype.Drapemite;

        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hoverAmplitude = 0.34f;
        [SerializeField] private float hoverFrequency = 3.0f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.Batch2Profile _profile;

        private LayerMask _solidTileMask;
        private LayerMask _groundMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private Vector3 _anchorPosition;

        private int _patrolDir = 1;
        private float _attackCooldownTimer;
        private float _animPhase;
        private int _sweepDirX = 1;
        private float _sweepStartX;
        private float _pogoRecoilTimer;
        private float _landSquashTimer;

        private PatrolState _patrolState;
        private WindupState _windupState;
        private AttackState _attackState;
        private RecoverState _recoverState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        public CourtyardDrapeArchetype Archetype { get { return archetype; } }

        public void ConfigureArchetype(CourtyardDrapeArchetype kind)
        {
            archetype = kind;
            ENEMY_COMBAT_CONFIG.Batch2Profile p = kind == CourtyardDrapeArchetype.Drapefly
                ? ENEMY_COMBAT_CONFIG.Drapefly
                : ENEMY_COMBAT_CONFIG.Drapemite;
            if (p.HoverAmplitude > 0.01f) hoverAmplitude = p.HoverAmplitude;
            if (p.HoverFrequency > 0.01f) hoverFrequency = p.HoverFrequency;
        }

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _fsm = new EnemyStateMachine();
            _profile = archetype == CourtyardDrapeArchetype.Drapefly
                ? ENEMY_COMBAT_CONFIG.Drapefly
                : ENEMY_COMBAT_CONFIG.Drapemite;

            _solidTileMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _groundMask = _solidTileMask | (1 << GameConstants.LayerOneWayPlatform);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.77f) % 6.28f;
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
            _profile = archetype == CourtyardDrapeArchetype.Drapefly
                ? ENEMY_COMBAT_CONFIG.Drapefly
                : ENEMY_COMBAT_CONFIG.Drapemite;
            _anchorPosition = transform.position;
            ResolvePlayer();

            if (IsAirborne() && _rb != null)
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
            if (l != GameConstants.LayerGround && l != GameConstants.LayerBreakableWall && l != GameConstants.LayerOneWayPlatform) return;

            if (IsAirborne() && _fsm.CurrentState == _attackState)
            {
                // Drapefly hits corridor wall -> stop sweep cleanly & turn around in TurnRecover
                if (_rb != null) _rb.velocity = new Vector2(-_sweepDirX * 1.8f, 0f);
                _sweepDirX = -_sweepDirX;
                _enemy.SetFacing(_sweepDirX);
                TransitionTo(_recoverState);
            }
        }

        private bool IsAirborne()
        {
            return archetype == CourtyardDrapeArchetype.Drapefly;
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
            if (info.IsDownwardPogo)
            {
                _pogoRecoilTimer = 0.18f;
                ChoralCodeVFX.SpawnMossPogoBurst(
                    transform.position + new Vector3(0f, IsAirborne() ? 0.40f : 0.85f, 0f),
                    new Color(0.84f, 0.82f, 0.92f, 0.95f));
                if (IsAirborne() && _rb != null)
                {
                    // Clean elastic dip when player air-pogos Drapefly mid-sweep
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.4f, -3.2f);
                }
            }
            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            TransitionTo(_deathState);
            if (visualTransform != null) visualTransform.localRotation = Quaternion.identity;
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
            _enemy.SetFacing(DirToPlayer());
        }

        private bool CanTriggerDrapemiteLeap()
        {
            if (!IsPlayerAlive()) return false;
            return GetDistToPlayer() <= _profile.DetectRange && GetVertDeltaToPlayer() <= 3.0f;
        }

        private bool CanTriggerDrapeflySweep()
        {
            if (!IsPlayerAlive()) return false;
            float dx = Mathf.Abs(_playerTransform.position.x - transform.position.x);
            float dy = Mathf.Abs(PlayerChestPos().y - transform.position.y);
            return dx <= _profile.AttackRange && dx >= 1.5f && dy <= _profile.SweepVertTolerance;
        }

        private bool IsGroundedNow()
        {
            return Physics2D.Raycast(transform.position, Vector2.down, 0.32f, _groundMask).collider != null;
        }

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
                if (!IsAirborne())
                {
                    // Drapemite: dramatic vertical squash on CrouchTelegraph, leap stretch on SlashLeap, landing squash
                    if (st == EnemyAIStateType.Windup)
                    {
                        sx *= 1.24f;
                        sy *= 0.72f;
                        offset.y = -0.08f;
                        offset.x = Mathf.Sin(_animPhase * 62f) * 0.03f;
                    }
                    else if (st == EnemyAIStateType.Attack)
                    {
                        float vy = _rb != null ? _rb.velocity.y : 0f;
                        sx *= 1.15f;
                        sy *= 0.88f;
                        rotZ = -dir * Mathf.Clamp(12f - vy * 1.4f, -14f, 18f);
                    }
                    else if (_landSquashTimer > 0f)
                    {
                        float sq = Mathf.Sin((_landSquashTimer / 0.16f) * Mathf.PI);
                        sx *= 1f + 0.18f * sq;
                        sy *= 1f - 0.16f * sq;
                    }
                }
                else
                {
                    // Drapefly: hover bob, pull-back tilt on SweepWindup, aggressive horizontal flying lean on SideToSideSweep
                    if (st == EnemyAIStateType.Windup)
                    {
                        rotZ = dir * 16f;
                        offset.x = -dir * 0.14f;
                        sx *= 0.90f;
                        sy *= 1.10f;
                    }
                    else if (st == EnemyAIStateType.Attack)
                    {
                        rotZ = -dir * 18f;
                        sx *= 1.20f;
                        sy *= 0.84f;
                    }
                    else
                    {
                        offset.y = Mathf.Sin(_animPhase * 4.4f) * 0.055f;
                        float vx = _rb != null ? _rb.velocity.x : 0f;
                        rotZ = Mathf.Clamp(-vx * 1.6f, -10f, 10f);
                    }
                }

                if (_pogoRecoilTimer > 0f)
                {
                    float dip = Mathf.Sin((_pogoRecoilTimer / 0.18f) * Mathf.PI);
                    sx *= 1f + 0.18f * dip;
                    sy *= 1f - 0.18f * dip;
                    offset.y -= 0.12f * dip;
                }
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // FSM STATES
        // =========================================================================

        private sealed class PatrolState : IEnemyState
        {
            private readonly CourtyardDrapeAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public PatrolState(CourtyardDrapeAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt)
            {
                if (_ai._attackCooldownTimer <= 0f)
                {
                    if (!_ai.IsAirborne() && _ai.CanTriggerDrapemiteLeap())
                    {
                        _ai.TransitionTo(_ai._windupState);
                        return;
                    }
                    if (_ai.IsAirborne() && _ai.CanTriggerDrapeflySweep())
                    {
                        _ai.TransitionTo(_ai._windupState);
                        return;
                    }
                }

                if (!_ai.IsAirborne())
                {
                    float maxDist = _ai._profile.PatrolSpanX > 0.5f ? _ai._profile.PatrolSpanX : 4.0f;
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
                if (_ai.IsAirborne())
                {
                    if (_ai._rb == null) return;
                    // AirHover: gently align altitude toward player corridor when detected, else calm anchor hover
                    float targetX = _ai._anchorPosition.x + Mathf.Sin(_ai._animPhase * 1.15f) * _ai._profile.PatrolSpanX;
                    float targetY = _ai._anchorPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude;
                    if (_ai.IsPlayerAlive() && _ai.GetDistToPlayer() <= _ai._profile.DetectRange)
                    {
                        _ai.FacePlayer();
                        float desiredAlt = Mathf.Clamp(_ai.PlayerChestPos().y + 0.35f, _ai._anchorPosition.y - 3.6f, _ai._anchorPosition.y + 2.0f);
                        targetY = desiredAlt + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * (_ai.hoverAmplitude * 0.65f);
                        targetX = _ai._rb.position.x;
                    }
                    Vector2 vel = (new Vector2(targetX, targetY) - _ai._rb.position) * 2.4f;
                    if (Mathf.Abs(vel.x) > 0.25f) _ai._enemy.SetFacing(vel.x >= 0f ? 1 : -1);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 4.6f));
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

        private sealed class WindupState : IEnemyState
        {
            private readonly CourtyardDrapeAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public WindupState(CourtyardDrapeAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                _ai._sweepDirX = _ai.DirToPlayer();
                if (!_ai.IsAirborne())
                {
                    _ai._enemy.StopHorizontal();
                    _timer = _ai._profile.CrouchTelegraphDuration; // 0.28s CrouchTelegraph
                    ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._sweepDirX, false);
                }
                else
                {
                    _timer = _ai._profile.SweepWindupDuration; // 0.30s SweepWindup
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer > 0.08f)
                {
                    _ai.FacePlayer();
                    _ai._sweepDirX = _ai.DirToPlayer();
                }
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._attackState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborne() && _ai._rb != null)
                {
                    // Slight rear pull-back before high-speed horizontal sweep
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, new Vector2(-_ai._sweepDirX * 1.8f, 0f), Mathf.Clamp01(fixedDt * 10f));
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
            private readonly CourtyardDrapeAI _ai;
            private float _timer;
            private float _airTime;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public AttackState(CourtyardDrapeAI ai) { _ai = ai; }

            public void Enter()
            {
                _airTime = 0f;
                _ai._enemy.SetFacing(_ai._sweepDirX);
                _ai._enemy.SetChargeState(true);

                if (!_ai.IsAirborne())
                {
                    // Drapemite SlashLeap: parabolic leap toward player + front slash hitbox & Canvas arc
                    _timer = 0.72f;
                    if (_ai._rb != null)
                    {
                        float distX = _ai.IsPlayerAlive() ? Mathf.Abs(_ai._playerTransform.position.x - _ai.transform.position.x) : 4.2f;
                        float scaledVx = Mathf.Clamp(distX * 2.1f, _ai._profile.LeapSpeedX * 0.70f, _ai._profile.LeapSpeedX * 1.15f);
                        _ai._rb.velocity = new Vector2(_ai._sweepDirX * scaledVx, _ai._profile.LeapVelY);
                    }

                    _ai._enemy.ActivateCustomAttackHitbox(
                        new Vector2(0.85f, 0.56f),
                        new Vector2(1.80f, 1.18f),
                        1,
                        _ai._profile.ContactKnockback,
                        new Color(0.95f, 0.90f, 0.96f, 0.90f),
                        0.0f); // Drawn via pure code SlashArc below!

                    ChoralCodeVFX.SpawnSlashArc(
                        _ai.transform.position + new Vector3(_ai._sweepDirX * 0.85f, 0.65f, 0f),
                        _ai._sweepDirX,
                        1.15f,
                        0.92f,
                        128f,
                        0.15f,
                        new Color(0.96f, 0.92f, 1.0f, 0.96f),
                        0.18f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                }
                else
                {
                    // Drapefly SideToSideSweep: fast straight horizontal corridor sweep!
                    _ai._sweepStartX = _ai.transform.position.x;
                    _timer = (_ai._profile.SweepMaxDistance / Mathf.Max(4f, _ai._profile.SweepSpeed)) + 0.12f;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                _airTime += dt;

                if (!_ai.IsAirborne())
                {
                    // Deactivate active slash hitbox after the sharp front swipe window
                    if (_airTime >= _ai._profile.LeapSlashDuration)
                    {
                        _ai._enemy.DeactivateAttackHitbox();
                    }

                    if ((_airTime > 0.14f && _ai.IsGroundedNow() && (_ai._rb == null || _ai._rb.velocity.y <= 0.1f)) || _timer <= 0f)
                    {
                        _ai._landSquashTimer = 0.16f;
                        ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._sweepDirX, true);
                        _ai.TransitionTo(_ai._recoverState);
                    }
                    return;
                }

                // Drapefly sweeps straight horizontally until max distance or wall, then reverses!
                float covered = Mathf.Abs(_ai.transform.position.x - _ai._sweepStartX);
                if (covered >= _ai._profile.SweepMaxDistance || _timer <= 0f)
                {
                    _ai._sweepDirX = -_ai._sweepDirX;
                    _ai._enemy.SetFacing(_ai._sweepDirX);
                    _ai.TransitionTo(_ai._recoverState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborne())
                {
                    if (_ai._rb == null) return;
                    float spd = _ai._profile.SweepSpeed;
                    RaycastHit2D wallHit = Physics2D.CircleCast(_ai._rb.position, 0.44f, new Vector2(_ai._sweepDirX, 0f), spd * fixedDt + 0.12f, _ai._solidTileMask);
                    if (wallHit.collider != null)
                    {
                        _ai._rb.velocity = new Vector2(-_ai._sweepDirX * 1.8f, 0f);
                        _ai._sweepDirX = -_ai._sweepDirX;
                        _ai._enemy.SetFacing(_ai._sweepDirX);
                        _ai.TransitionTo(_ai._recoverState);
                        return;
                    }
                    _ai._rb.velocity = new Vector2(_ai._sweepDirX * spd, 0f);
                    return;
                }

                if (_ai._enemy.HasWallAhead(_ai._sweepDirX) && _ai._rb != null)
                {
                    _ai._rb.velocity = new Vector2(0f, _ai._rb.velocity.y);
                }
            }

            public void Exit()
            {
                _ai._enemy.SetChargeState(false);
                _ai._enemy.DeactivateAttackHitbox();
            }
        }

        private sealed class RecoverState : IEnemyState
        {
            private readonly CourtyardDrapeAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public RecoverState(CourtyardDrapeAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.DeactivateAttackHitbox();
                _ai._attackCooldownTimer = _ai._profile.AttackCooldown;
                if (!_ai.IsAirborne())
                {
                    _ai._enemy.StopHorizontal();
                    _timer = _ai._profile.LandRecoverDuration; // 0.45s LandRecover
                }
                else
                {
                    _timer = _ai._profile.TurnRecoverDuration; // 0.50s TurnRecover
                    _ai._anchorPosition = new Vector3(_ai.transform.position.x, _ai._anchorPosition.y, _ai.transform.position.z);
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborne() && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 22f * fixedDt);
                }
                else
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit() { }
        }

        private sealed class StaggerState : IEnemyState
        {
            private readonly CourtyardDrapeAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(CourtyardDrapeAI ai) { _ai = ai; }
            public void Enter() { _ai._enemy.DeactivateAttackHitbox(); }
            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    _ai.TransitionTo(_ai._patrolState);
                }
            }
            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborne() && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 16f * fixedDt);
                }
            }
            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly CourtyardDrapeAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(CourtyardDrapeAI ai) { _ai = ai; }
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
