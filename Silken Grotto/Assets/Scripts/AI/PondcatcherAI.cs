using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Refactored 60 FPS Finite State Machine AI for Pondcatcher (Batch 2 — Spear Hunter),
    /// preserving pondcatcher.png & pondcatcher_Sheet.png untouched:
    /// - HuntIdle: stands vigilant at platform/water edges, periodically performing downward hunting spear pokes.
    /// - SpearWindup (0.35s): draws spear back while aiming toward player (supports BOTH horizontal AND diagonal downward thrust!).
    /// - SpearThrust: launches medium-long reach spear hitbox + crisp code-drawn streak -> PullBackRecover (0.50s)
    ///   -> ShortReposition backstep if player crowds too close.
    /// Stats: Medium HP (48 HP, ~4 hits), 1 damage, pogo-friendly.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class PondcatcherAI : MonoBehaviour
    {
        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.Batch2Profile _profile;

        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private float _animPhase;
        private float _attackCooldownTimer;
        private float _idlePokeTimer;
        private float _idlePokeAnimTimer;
        private float _pogoRecoilTimer;

        private Vector2 _lockedSpearDir = Vector2.right;
        private bool _spearHitPlayerThisThrust;

        private HuntIdleState _huntIdleState;
        private StalkEdgeState _stalkEdgeState;
        private SpearWindupState _spearWindupState;
        private SpearThrustState _spearThrustState;
        private PullBackRecoverState _pullBackRecoverState;
        private ShortRepositionState _shortRepositionState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _fsm = new EnemyStateMachine();
            _profile = ENEMY_COMBAT_CONFIG.Pondcatcher;
            _animPhase = (Mathf.Abs(transform.position.x) * 0.71f) % 6.28f;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualScale.y = Mathf.Abs(_baseVisualScale.y);
            _baseVisualLocalPos = visualTransform.localPosition;

            _huntIdleState = new HuntIdleState(this);
            _stalkEdgeState = new StalkEdgeState(this);
            _spearWindupState = new SpearWindupState(this);
            _spearThrustState = new SpearThrustState(this);
            _pullBackRecoverState = new PullBackRecoverState(this);
            _shortRepositionState = new ShortRepositionState(this);
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
            _profile = ENEMY_COMBAT_CONFIG.Pondcatcher;
            ResolvePlayer();
            int startDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;
            _enemy.SetFacing(startDir);
            _idlePokeTimer = _profile.IdlePokeInterval * 0.65f;
            TransitionTo(_huntIdleState);
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= dt;
            if (_idlePokeAnimTimer > 0f) _idlePokeAnimTimer -= dt;
            if (_pogoRecoilTimer > 0f) _pogoRecoilTimer -= dt;
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
                _pogoRecoilTimer = 0.16f;
                ChoralCodeVFX.SpawnMossPogoBurst(transform.position + new Vector3(0f, 1.35f, 0f), new Color(0.68f, 0.94f, 0.78f, 0.94f));
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

        private Vector3 SpearOriginPos()
        {
            int dir = _enemy != null ? _enemy.FacingDirection : 1;
            return transform.position + new Vector3(dir * 0.32f, 0.96f, 0f);
        }

        private float GetDistToPlayer()
        {
            return _playerTransform != null ? Vector2.Distance(transform.position, _playerTransform.position) : float.MaxValue;
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
            float dy = _playerTransform.position.y - transform.position.y;
            // Detects players level with Pondcatcher OR down below the platform edge!
            return GetDistToPlayer() <= _profile.DetectRange && dy <= 3.6f && dy >= -6.8f;
        }

        private bool ShouldLosePlayer()
        {
            if (!IsPlayerAlive()) return true;
            float dy = Mathf.Abs(_playerTransform.position.y - transform.position.y);
            return GetDistToPlayer() > _profile.LoseTargetRange || dy > 9.0f;
        }

        private bool InSpearStrikeRange()
        {
            if (!IsPlayerAlive()) return false;
            float dist = Vector2.Distance((Vector2)SpearOriginPos(), PlayerChestPos());
            float dy = _playerTransform.position.y - transform.position.y;
            return dist <= _profile.AttackRange && dy <= 2.4f && dy >= -4.8f;
        }

        private Vector2 ComputeSpearAimDirection()
        {
            int dir = _enemy.FacingDirection;
            if (!IsPlayerAlive()) return new Vector2(dir, 0f);
            Vector2 delta = PlayerChestPos() - (Vector2)SpearOriginPos();
            if (delta.sqrMagnitude < 0.01f) return new Vector2(dir, 0f);

            // Support BOTH crisp horizontal thrusts AND diagonal-downward thrusts when player is below!
            float dy = delta.y;
            if (dy < -0.55f)
            {
                float clampAngle = Mathf.Clamp(Mathf.Atan2(dy, Mathf.Max(0.45f, Mathf.Abs(delta.x))), -1.12f, -0.18f);
                return new Vector2(Mathf.Cos(clampAngle) * dir, Mathf.Sin(clampAngle)).normalized;
            }
            float slightTilt = Mathf.Clamp(dy * 0.14f, -0.22f, 0.24f);
            return new Vector2(dir, slightTilt).normalized;
        }

        private void CheckDirectionalSpearHit()
        {
            if (_spearHitPlayerThisThrust || !IsPlayerAlive()) return;
            Vector3 origin = SpearOriginPos();
            RaycastHit2D hit = Physics2D.CircleCast(origin, 0.34f, _lockedSpearDir, _profile.SpearReach, 1 << GameConstants.LayerPlayer);
            if (hit.collider != null)
            {
                PlayerController pc = hit.collider.GetComponentInParent<PlayerController>();
                if (pc != null && pc.IsAlive && !pc.IsInvulnerable)
                {
                    _spearHitPlayerThisThrust = true;
                    Vector2 kbDir = new Vector2(_lockedSpearDir.x >= 0f ? 1f : -1f, 0.32f).normalized;
                    DamageInfo info = new DamageInfo(
                        1,
                        kbDir,
                        _profile.ContactKnockback,
                        0.20f,
                        gameObject,
                        hit.point,
                        false);
                    pc.TakeDamage(info);
                }
            }
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
                if (_idlePokeAnimTimer > 0f)
                {
                    // Charming downward water-hunting spear poke lean
                    float poke = Mathf.Sin((_idlePokeAnimTimer / 0.28f) * Mathf.PI);
                    rotZ = -dir * (22f * poke);
                    offset.x = dir * (0.20f * poke);
                    offset.y = -0.10f * poke;
                }
                else if (st == EnemyAIStateType.Windup)
                {
                    // Pull spear back coiled stance
                    rotZ = dir * 14f;
                    offset.x = -dir * 0.16f;
                    sx *= 0.92f;
                    sy *= 1.06f;
                }
                else if (st == EnemyAIStateType.Attack)
                {
                    // Crisp forward/downward thrust lunge along _lockedSpearDir
                    float aimDeg = Mathf.Atan2(_lockedSpearDir.y, Mathf.Max(0.1f, Mathf.Abs(_lockedSpearDir.x))) * Mathf.Rad2Deg;
                    rotZ = dir * (aimDeg * 0.55f - 10f);
                    offset.x = _lockedSpearDir.x * 0.26f;
                    offset.y = _lockedSpearDir.y * 0.16f;
                    sx *= 1.14f;
                    sy *= 0.92f;
                }
                else if (st == EnemyAIStateType.Recover)
                {
                    rotZ = dir * 5f;
                }

                if (_pogoRecoilTimer > 0f)
                {
                    float u = _pogoRecoilTimer / 0.16f;
                    float dip = Mathf.Sin(u * Mathf.PI);
                    sx *= 1f + 0.14f * dip;
                    sy *= 1f - 0.14f * dip;
                }
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // FSM STATES
        // =========================================================================

        private sealed class HuntIdleState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }
            public HuntIdleState(PondcatcherAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.FacePlayer();
                    if (_ai.InSpearStrikeRange() && _ai._attackCooldownTimer <= 0f)
                        _ai.TransitionTo(_ai._spearWindupState);
                    else
                        _ai.TransitionTo(_ai._stalkEdgeState);
                    return;
                }

                // Periodic downward pond-hunting poke animation (drives Row 3 DownwardSpearPoke via IsCharging!)
                _ai._idlePokeTimer -= dt;
                if (_ai._idlePokeTimer <= 0f)
                {
                    _ai._idlePokeTimer = _ai._profile.IdlePokeInterval + Random.Range(-0.35f, 0.45f);
                    _ai._idlePokeAnimTimer = 0.38f;
                    _ai._enemy.SetChargeState(true);
                    int dir = _ai._enemy.FacingDirection;
                    Vector2 downPokeDir = new Vector2(dir * 0.58f, -0.82f).normalized;
                    ChoralCodeVFX.SpawnSpearThrustLine(
                        _ai.SpearOriginPos(),
                        downPokeDir,
                        2.1f,
                        new Color(0.74f, 0.94f, 0.86f, 0.72f),
                        0.18f);
                }
                else if (_ai._idlePokeAnimTimer <= 0f && _ai._enemy.IsCharging)
                {
                    _ai._enemy.SetChargeState(false);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { _ai._enemy.SetChargeState(false); }
        }

        private sealed class StalkEdgeState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }
            public StalkEdgeState(PondcatcherAI ai) { _ai = ai; }

            public void Enter()
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            }

            public void Tick(float dt)
            {
                if (_ai.ShouldLosePlayer())
                {
                    _ai.TransitionTo(_ai._huntIdleState);
                    return;
                }

                _ai.FacePlayer();

                if (_ai.InSpearStrikeRange() && _ai._attackCooldownTimer <= 0f)
                {
                    _ai.TransitionTo(_ai._spearWindupState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                int dir = _ai.DirToPlayer();
                float dist = _ai.GetDistToPlayer();

                if (dist < _ai._profile.TooCloseRepositionRange && _ai._attackCooldownTimer > 0f)
                {
                    int away = -dir;
                    if (_ai._enemy.HasGroundAhead(away) && !_ai._enemy.HasWallAhead(away))
                    {
                        _ai._enemy.SetHorizontalVelocity(away * (_ai._profile.PatrolSpeed * 1.25f));
                        return;
                    }
                }

                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir) || _ai.InSpearStrikeRange())
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }

                _ai._enemy.SetHorizontalVelocity(dir * _ai._profile.AggroSpeed);
            }

            public void Exit() { }
        }

        private sealed class SpearWindupState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public SpearWindupState(PondcatcherAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                _ai._enemy.StopHorizontal();
                _timer = _ai._profile.SpearWindupDuration; // 0.35s
                _ai._lockedSpearDir = _ai.ComputeSpearAimDirection();
                // IsCharging selects Row 3 (DownwardSpearPoke) when aiming below, Row 2 (SpearThrust) when horizontal!
                _ai._enemy.SetChargeState(_ai._lockedSpearDir.y < -0.35f);
                ChoralCodeVFX.SpawnTelegraphGlint(
                    _ai.SpearOriginPos() - (Vector3)(_ai._lockedSpearDir * 0.35f),
                    new Color(0.84f, 0.98f, 0.90f, 0.95f),
                    0.46f,
                    0.30f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer > 0.12f)
                {
                    _ai.FacePlayer();
                    _ai._lockedSpearDir = _ai.ComputeSpearAimDirection();
                    _ai._enemy.SetChargeState(_ai._lockedSpearDir.y < -0.35f);
                }
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._spearThrustState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { }
        }

        private sealed class SpearThrustState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public SpearThrustState(PondcatcherAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._profile.SpearThrustDuration; // 0.24s
                _ai._spearHitPlayerThisThrust = false;
                _ai._enemy.SetChargeState(_ai._lockedSpearDir.y < -0.35f);

                ChoralCodeVFX.SpawnSpearThrustLine(
                    _ai.SpearOriginPos(),
                    _ai._lockedSpearDir,
                    _ai._profile.SpearReach,
                    new Color(0.88f, 1.0f, 0.92f, 0.98f),
                    0.18f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayAttackSlash(2);

                // Small forward commitment step if safe on platform
                int dir = _ai._enemy.FacingDirection;
                if (_ai._enemy.HasGroundAhead(dir) && !_ai._enemy.HasWallAhead(dir))
                {
                    _ai._enemy.SetHorizontalVelocity(dir * 3.2f);
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._pullBackRecoverState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai.CheckDirectionalSpearHit();
                if (_ai._rb != null)
                {
                    _ai._rb.velocity = new Vector2(Mathf.MoveTowards(_ai._rb.velocity.x, 0f, 22f * fixedDt), _ai._rb.velocity.y);
                }
            }

            public void Exit() { _ai._enemy.StopHorizontal(); }
        }

        private sealed class PullBackRecoverState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public PullBackRecoverState(PondcatcherAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _timer = _ai._profile.PullBackRecoverDuration; // 0.50s
                _ai._attackCooldownTimer = _ai._profile.AttackCooldown;
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    if (_ai.IsPlayerAlive() && _ai.GetDistToPlayer() <= _ai._profile.TooCloseRepositionRange)
                    {
                        _ai.TransitionTo(_ai._shortRepositionState);
                    }
                    else if (_ai.CanDetectPlayer())
                    {
                        _ai.TransitionTo(_ai._stalkEdgeState);
                    }
                    else
                    {
                        _ai.TransitionTo(_ai._huntIdleState);
                    }
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { _ai._enemy.SetChargeState(false); }
        }

        private sealed class ShortRepositionState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            private float _timer;
            private int _retreatDir;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public ShortRepositionState(PondcatcherAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._profile.RepositionDuration; // 0.28s
                _retreatDir = -_ai.DirToPlayer();
                _ai.FacePlayer();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._stalkEdgeState);
                    else _ai.TransitionTo(_ai._huntIdleState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (!_ai._enemy.HasGroundAhead(_retreatDir) || _ai._enemy.HasWallAhead(_retreatDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(_retreatDir * _ai._profile.RepositionBackSpeed);
            }

            public void Exit() { _ai._enemy.StopHorizontal(); }
        }

        private sealed class StaggerState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(PondcatcherAI ai) { _ai = ai; }
            public void Enter() { _ai._enemy.DeactivateAttackHitbox(); }
            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._stalkEdgeState);
                    else _ai.TransitionTo(_ai._huntIdleState);
                }
            }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly PondcatcherAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(PondcatcherAI ai) { _ai = ai; }
            public void Enter() { _ai._enemy.StopHorizontal(); }
            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
