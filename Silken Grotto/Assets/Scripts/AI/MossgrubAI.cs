using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Refactored Silksong-style Moss Grub AI (Early Passive-Until-Provoked Ground Enemy):
    /// - PassivePatrol (default): crawls slowly back and forth along platforms, automatically turning at ledges & walls.
    ///   NEVER chases or attacks the player unless provoked by taking damage first!
    /// - ProvokedAlert (0.25s): triggered ONLY on OnDamaged; brief angry tremor, exclamation pop & dust burst.
    /// - ProvokedCharge: turns toward player and rushes rapidly along the floor (respecting walls/edges or short hop per config),
    ///   returning calmly to PassivePatrol after calmDownDuration without further disturbance.
    /// - Adaptive rendering: maps cleanly to existing sprite sheets (12-24 FPS) AND applies rhythmic crawl squash-stretch,
    ///   bristle shake, and aggressive provoked lean on single-image setups.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class MossgrubAI : MonoBehaviour
    {
        [Header("Visual & Turn Tuning")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float turnDuration = 0.15f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private Animator _animator;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.MossGrottoProfile _cfg;
        private bool _hasSpriteSheetAnimator;

        private int _moveDir = 1;
        private float _animPhase;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private float _calmDownTimer;
        private float _pogoSquishTimer;

        private PassivePatrolState _passivePatrolState;
        private TurnState _turnState;
        private ProvokedAlertState _provokedAlertState;
        private ProvokedChargeState _provokedChargeState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        private void Awake()
        {
            _cfg = ENEMY_COMBAT_CONFIG.MossGrub;
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _hasSpriteSheetAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            _fsm = new EnemyStateMachine();
            _moveDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;
            _animPhase = (Mathf.Abs(transform.position.x) * 0.53f) % 6.28f;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualLocalPos = visualTransform.localPosition;

            _passivePatrolState = new PassivePatrolState(this);
            _turnState = new TurnState(this);
            _provokedAlertState = new ProvokedAlertState(this);
            _provokedChargeState = new ProvokedChargeState(this);
            _staggerState = new StaggerState(this);
            _deathState = new DeathState(this);
        }

        private void OnEnable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged += HandleDamaged;
                _enemy.OnTaunted += HandleTaunted;
                _enemy.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_enemy != null)
            {
                _enemy.OnDamaged -= HandleDamaged;
                _enemy.OnTaunted -= HandleTaunted;
                _enemy.OnDied -= HandleDied;
            }
        }

        private void Start()
        {
            ResolvePlayer();
            _enemy.SetFacing(_moveDir);
            TransitionTo(_passivePatrolState);
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            _animPhase += dt;
            if (_pogoSquishTimer > 0f) _pogoSquishTimer -= dt;

            _fsm.Tick(dt);
            ApplyAdaptiveCrawlVisuals();
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
                _pogoSquishTimer = 0.16f;
                ChoralCodeVFX.SpawnMossPogoBurst(transform.position + Vector3.up * 0.55f, new Color(0.56f, 0.90f, 0.30f, 0.95f));
            }

            // Mossgrub ONLY provokes when struck by the player!
            _calmDownTimer = _cfg.CalmDownDuration;
            if (IsPlayerAlive())
            {
                _moveDir = _playerTransform.position.x >= transform.position.x ? 1 : -1;
            }
            TransitionTo(_staggerState);
        }

        private void HandleTaunted(Vector3 taunterPos)
        {
            if (!_enemy.IsAlive) return;
            _calmDownTimer = _cfg.CalmDownDuration;
            _moveDir = taunterPos.x >= transform.position.x ? 1 : -1;
            _enemy.SetFacing(_moveDir);
            TransitionTo(_provokedAlertState);
        }

        private void HandleDied()
        {
            _enemy.SetTurnState(false);
            _enemy.SetChargeState(false);
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

        private void ApplyAdaptiveCrawlVisuals()
        {
            if (visualTransform == null) return;
            int dir = _enemy != null ? _enemy.FacingDirection : _moveDir;
            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            float rotZ = 0f;
            Vector3 offset = Vector3.zero;
            float procWeight = _hasSpriteSheetAnimator ? 0.40f : 1.0f;

            if (_fsm.CurrentState == _passivePatrolState)
            {
                // Rhythmic caterpillar horizontal squash & stretch crawling wave
                float wave = Mathf.Sin(_animPhase * 7.2f);
                sx *= (1f + wave * 0.085f * procWeight);
                sy *= (1f - wave * 0.065f * procWeight);
            }
            else if (_fsm.CurrentState == _provokedAlertState)
            {
                // Fast bristle shake & puffed spines when startled/provoked!
                float shake = Mathf.Sin(_animPhase * 62f) * 0.045f;
                offset.x += shake;
                sx *= (1f - 0.06f * procWeight);
                sy *= (1f + 0.10f * procWeight);
            }
            else if (_fsm.CurrentState == _provokedChargeState)
            {
                // Rapid bristled crawl + aggressive forward tilt
                float rapid = Mathf.Sin(_animPhase * 18f);
                sx *= (1f + 0.10f * procWeight + rapid * 0.07f * procWeight);
                sy *= (1f - 0.06f * procWeight - rapid * 0.05f * procWeight);
                rotZ = -dir * 6.5f * procWeight;
            }

            if (_pogoSquishTimer > 0f)
            {
                float k = _pogoSquishTimer / 0.16f;
                sx *= (1f + 0.18f * k);
                sy *= (1f - 0.18f * k);
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // 1. PASSIVE PATROL (Default: never attacks unless provoked!)
        // =========================================================================
        private sealed class PassivePatrolState : IEnemyState
        {
            private readonly MossgrubAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public PassivePatrolState(MossgrubAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
                _ai._enemy.SetFacing(_ai._moveDir);
            }

            public void Tick(float dt)
            {
                if (!_ai._enemy.HasGroundAhead(_ai._moveDir) || _ai._enemy.HasWallAhead(_ai._moveDir))
                {
                    _ai.TransitionTo(_ai._turnState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (!_ai._enemy.HasGroundAhead(_ai._moveDir) || _ai._enemy.HasWallAhead(_ai._moveDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(_ai._moveDir * _ai._cfg.PatrolSpeed);
            }

            public void Exit() { }
        }

        // =========================================================================
        // 2. TURN STATE (Clean ledge/wall turnaround)
        // =========================================================================
        private sealed class TurnState : IEnemyState
        {
            private readonly MossgrubAI _ai;
            private float _elapsed;
            private bool _flipped;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }
            public TurnState(MossgrubAI ai) { _ai = ai; }

            public void Enter()
            {
                _elapsed = 0f;
                _flipped = false;
                _ai._enemy.StopHorizontal();
                _ai._enemy.SetChargeState(false);
                _ai._enemy.SetTurnState(true);
            }

            public void Tick(float dt)
            {
                _elapsed += dt;
                float dur = Mathf.Max(0.08f, _ai.turnDuration);
                if (!_flipped && _elapsed >= dur * 0.5f)
                {
                    _flipped = true;
                    _ai._moveDir = -_ai._moveDir;
                    _ai._enemy.SetFacing(_ai._moveDir);
                }
                if (_elapsed >= dur)
                {
                    _ai._enemy.SetTurnState(false);
                    _ai.TransitionTo(_ai._calmDownTimer > 0f ? (IEnemyState)_ai._provokedChargeState : _ai._passivePatrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
            }

            public void Exit()
            {
                _ai._enemy.SetTurnState(false);
            }
        }

        // =========================================================================
        // 3. PROVOKED ALERT (0.25s angry bristle-shake telegraph upon taking damage)
        // =========================================================================
        private sealed class ProvokedAlertState : IEnemyState
        {
            private readonly MossgrubAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public ProvokedAlertState(MossgrubAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._cfg.ProvokedAlertDuration;
                _ai._enemy.StopHorizontal();
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
                if (_ai.IsPlayerAlive())
                {
                    _ai._moveDir = _ai._playerTransform.position.x >= _ai.transform.position.x ? 1 : -1;
                    _ai._enemy.SetFacing(_ai._moveDir);
                }

                ChoralCodeVFX.SpawnProvokedAlertPop(_ai.transform.position + Vector3.up * 0.88f);
                ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._moveDir, false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._provokedChargeState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                _ai._enemy.StopHorizontal();
            }

            public void Exit() { }
        }

        // =========================================================================
        // 4. PROVOKED CHARGE (Fast floor rush toward player -> calms back to PassivePatrol)
        // =========================================================================
        private sealed class ProvokedChargeState : IEnemyState
        {
            private readonly MossgrubAI _ai;
            private float _dustTimer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }
            public ProvokedChargeState(MossgrubAI ai) { _ai = ai; }

            public void Enter()
            {
                _dustTimer = 0.16f;
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(true);
                _ai._enemy.SetFacing(_ai._moveDir);
            }

            public void Tick(float dt)
            {
                _ai._calmDownTimer -= dt;
                if (_ai._calmDownTimer <= 0f)
                {
                    _ai._calmDownTimer = 0f;
                    _ai.TransitionTo(_ai._passivePatrolState);
                    return;
                }

                _dustTimer -= dt;
                if (_dustTimer <= 0f)
                {
                    _dustTimer = 0.24f;
                    ChoralCodeVFX.SpawnChargeDust(_ai.transform.position, _ai._moveDir, false);
                }

                bool noGround = !_ai._enemy.HasGroundAhead(_ai._moveDir);
                bool wall = _ai._enemy.HasWallAhead(_ai._moveDir);

                if (noGround && _ai._cfg.AllowEdgeHopOnCharge && _ai._rb != null)
                {
                    _ai._rb.velocity = new Vector2(_ai._moveDir * _ai._cfg.AggroSpeed, 5.2f);
                    return;
                }

                if (noGround || wall)
                {
                    _ai.TransitionTo(_ai._turnState);
                    return;
                }

                // Face toward player smoothly if player crosses behind during provoked state
                if (_ai.IsPlayerAlive())
                {
                    float dx = _ai._playerTransform.position.x - _ai.transform.position.x;
                    if (Mathf.Abs(dx) > 1.25f && (dx * _ai._moveDir < 0f))
                    {
                        _ai.TransitionTo(_ai._turnState);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (!_ai._enemy.HasGroundAhead(_ai._moveDir) || _ai._enemy.HasWallAhead(_ai._moveDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(_ai._moveDir * _ai._cfg.AggroSpeed);
            }

            public void Exit()
            {
                _ai._enemy.SetChargeState(false);
            }
        }

        // =========================================================================
        // 5. STAGGER -> PROVOKED ALERT & DEATH
        // =========================================================================
        private sealed class StaggerState : IEnemyState
        {
            private readonly MossgrubAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(MossgrubAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    // After recovering from hit knockback, enter 0.25s ProvokedAlert -> ProvokedCharge!
                    _ai.TransitionTo(_ai._provokedAlertState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                int velDir = _ai._enemy.HorizontalVelocity >= 0f ? 1 : -1;
                if (!_ai._enemy.HasGroundAhead(velDir))
                {
                    _ai._enemy.StopHorizontal();
                }
            }

            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly MossgrubAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(MossgrubAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
                _ai._enemy.StopHorizontal();
            }

            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
