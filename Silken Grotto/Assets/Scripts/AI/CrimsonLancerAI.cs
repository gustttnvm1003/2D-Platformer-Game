using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Concrete Finite State Machine AI for the Crimson Lancer (and Elite Captain variant).
    /// Handles Idle, Patrol (with strict platform edge/wall safety), Player Detection,
    /// Chase, Telegraphed Windup -> Lunge Attack -> Recovery with enforced cooldown,
    /// Stagger reaction, and Death.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class CrimsonLancerAI : MonoBehaviour
    {
        private EnemyController _enemy;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;

        private float _attackCooldownTimer;
        private int _patrolDirection = 1;

        private IdleState _idleState;
        private PatrolState _patrolState;
        private ChaseState _chaseState;
        private WindupState _windupState;
        private AttackState _attackState;
        private RecoverState _recoverState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _fsm = new EnemyStateMachine();

            _idleState = new IdleState(this);
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
            ResolvePlayer();
            TransitionTo(_patrolState);
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;

            _fsm.Tick(Time.deltaTime);
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
            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            TransitionTo(_deathState);
        }

        private bool IsPlayerAlive()
        {
            return _playerTransform != null && (_playerController == null || _playerController.IsAlive);
        }

        private float GetDistanceToPlayer()
        {
            if (_playerTransform == null) return float.MaxValue;
            return Vector2.Distance(transform.position, _playerTransform.position);
        }

        private float GetVerticalDeltaToPlayer()
        {
            if (_playerTransform == null) return float.MaxValue;
            return Mathf.Abs(_playerTransform.position.y - transform.position.y);
        }

        private bool CanDetectPlayer()
        {
            if (!IsPlayerAlive()) return false;
            float range = _enemy.Stats != null ? _enemy.Stats.detectRange : 7.5f;
            return GetDistanceToPlayer() <= range && GetVerticalDeltaToPlayer() <= 3.2f;
        }

        private bool ShouldLosePlayer()
        {
            if (!IsPlayerAlive()) return true;
            float loseRange = _enemy.Stats != null ? _enemy.Stats.loseTargetRange : 11.5f;
            return GetDistanceToPlayer() > loseRange || GetVerticalDeltaToPlayer() > 5.0f;
        }

        private bool IsPlayerInAttackRange()
        {
            if (!IsPlayerAlive()) return false;
            float atkRange = _enemy.Stats != null ? _enemy.Stats.attackRange : 2.05f;
            return GetDistanceToPlayer() <= atkRange && GetVerticalDeltaToPlayer() <= 1.8f;
        }

        private int DirectionTowardPlayer()
        {
            if (_playerTransform == null) return _enemy.FacingDirection;
            return _playerTransform.position.x >= transform.position.x ? 1 : -1;
        }

        // ---------------- CONCRETE FSM STATES ----------------

        private class IdleState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }

            public IdleState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.idlePauseDuration : 1.15f;
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.TransitionTo(_ai._chaseState);
                    return;
                }
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { }
        }

        private class PatrolState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }

            public PatrolState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetFacing(_ai._patrolDirection);
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.TransitionTo(_ai._chaseState);
                    return;
                }

                float maxDist = _ai._enemy.Stats != null ? _ai._enemy.Stats.patrolDistance : 4.5f;
                float offsetFromSpawn = _ai.transform.position.x - _ai._enemy.SpawnPosition.x;

                bool hitEdge = !_ai._enemy.HasGroundAhead(_ai._patrolDirection);
                bool hitWall = _ai._enemy.HasWallAhead(_ai._patrolDirection);
                bool passedBound = (_ai._patrolDirection > 0 && offsetFromSpawn >= maxDist) ||
                                   (_ai._patrolDirection < 0 && offsetFromSpawn <= -maxDist);

                if (hitEdge || hitWall || passedBound)
                {
                    _ai._patrolDirection = -_ai._patrolDirection;
                    _ai._enemy.SetFacing(_ai._patrolDirection);
                    _ai.TransitionTo(_ai._idleState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (!_ai._enemy.HasGroundAhead(_ai._patrolDirection) || _ai._enemy.HasWallAhead(_ai._patrolDirection))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                float speed = _ai._enemy.Stats != null ? _ai._enemy.Stats.patrolSpeed : 2.4f;
                _ai._enemy.SetHorizontalVelocity(_ai._patrolDirection * speed);
            }

            public void Exit() { }
        }

        private class ChaseState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }

            public ChaseState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyChirp();
            }

            public void Tick(float dt)
            {
                if (_ai.ShouldLosePlayer())
                {
                    _ai.TransitionTo(_ai._idleState);
                    return;
                }

                int dir = _ai.DirectionTowardPlayer();
                _ai._enemy.SetFacing(dir);

                if (_ai.IsPlayerInAttackRange() && _ai._attackCooldownTimer <= 0f)
                {
                    _ai.TransitionTo(_ai._windupState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                int dir = _ai.DirectionTowardPlayer();
                // Strict platform safety: never walk off a platform edge while chasing
                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir) || _ai.IsPlayerInAttackRange())
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                float speed = _ai._enemy.Stats != null ? _ai._enemy.Stats.chaseSpeed : 4.4f;
                _ai._enemy.SetHorizontalVelocity(dir * speed);
            }

            public void Exit() { }
        }

        private class WindupState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }

            public WindupState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetFacing(_ai.DirectionTowardPlayer());
                _ai._enemy.StopHorizontal();
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.windupDuration : 0.42f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._attackState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { }
        }

        private class AttackState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }

            public AttackState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackActiveDuration : 0.24f;
                _ai._enemy.ActivateAttackHitbox();
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
                int dir = _ai._enemy.FacingDirection;
                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                float lunge = _ai._enemy.Stats != null ? _ai._enemy.Stats.lungeSpeed : 7.2f;
                _ai._enemy.SetHorizontalVelocity(dir * lunge);
            }

            public void Exit()
            {
                _ai._enemy.DeactivateAttackHitbox();
                _ai._enemy.StopHorizontal();
            }
        }

        private class RecoverState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }

            public RecoverState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.recoveryDuration : 0.45f;
                _ai._attackCooldownTimer = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackCooldown : 1.25f;
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    if (_ai.CanDetectPlayer()) _ai.TransitionTo(_ai._chaseState);
                    else _ai.TransitionTo(_ai._idleState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { }
        }

        private class StaggerState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }

            public StaggerState(CrimsonLancerAI ai) { _ai = ai; }

            public void Enter()
            {
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

            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }

        private class DeathState : IEnemyState
        {
            private readonly CrimsonLancerAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }

            public DeathState(CrimsonLancerAI ai) { _ai = ai; }
            public void Enter() { _ai._enemy.DeactivateAttackHitbox(); _ai._enemy.StopHorizontal(); }
            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
