using UnityEngine;

namespace SilkenGrotto
{
    public enum BellhartExtendedArchetype
    {
        Skullwing,
        PharlidDiver,
        Mawling,
        Marrowmaw,
        Mossmir,
        Mossgrub
    }

    /// <summary>
    /// Modular Finite State Machine AI for the 6 additional Map 4 (Bellhart) cavern & town species:
    /// - Skullwing: horizontal flying cruiser executing level-aligned air dashes
    /// - PharlidDiver: high-ceiling spider hunter lining up overhead for rapid downward dive strikes
    /// - Mawling: nimble ground/bridge grub curling into a rolling floor rush toward nearby players
    /// - Marrowmaw: heavy bone-armored tank with hyper-armor and crushing frontal ram charges
    /// - Mossmir: damp-corner spiky moss flyer bobbing gently before swooping in on close targets
    /// - Mossgrub: spiky dorsal moss crawler patrolling streets/roofs with top contact & spike defense
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class BellhartExtendedAI : MonoBehaviour
    {
        [Header("Archetype")]
        [SerializeField] private BellhartExtendedArchetype archetype = BellhartExtendedArchetype.Skullwing;

        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hoverAmplitude = 0.30f;
        [SerializeField] private float hoverFrequency = 3.0f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;

        private LayerMask _solidAndPlatMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private int _patrolDir = 1;
        private float _attackCooldownTimer;
        private float _animPhase;
        private float _rollSpinAngle;
        private Vector2 _lockedVector = Vector2.right;

        private PatrolState _patrolState;
        private ChaseState _chaseState;
        private WindupState _windupState;
        private AttackState _attackState;
        private RecoverState _recoverState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        public BellhartExtendedArchetype Archetype { get { return archetype; } }

        public void ConfigureArchetype(BellhartExtendedArchetype kind)
        {
            archetype = kind;
            if (kind == BellhartExtendedArchetype.Skullwing)
            {
                hoverAmplitude = 0.24f;
                hoverFrequency = 4.6f;
            }
            else if (kind == BellhartExtendedArchetype.PharlidDiver)
            {
                hoverAmplitude = 0.16f;
                hoverFrequency = 2.4f;
            }
            else if (kind == BellhartExtendedArchetype.Mossmir)
            {
                hoverAmplitude = 0.44f;
                hoverFrequency = 2.2f;
            }
        }

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _fsm = new EnemyStateMachine();
            _solidAndPlatMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform) | (1 << GameConstants.LayerBreakableWall);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.77f) % 6.28f;
            _patrolDir = (Mathf.Abs(transform.position.x) % 2f) < 1f ? 1 : -1;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
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
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
            _animPhase += Time.deltaTime;

            _fsm.Tick(Time.deltaTime);
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
            if (_enemy == null || !_enemy.IsAlive) return;
            if (archetype == BellhartExtendedArchetype.PharlidDiver && _fsm.CurrentState == _attackState)
            {
                int l = collision.gameObject.layer;
                if (l == GameConstants.LayerGround || l == GameConstants.LayerOneWayPlatform || l == GameConstants.LayerBreakableWall)
                {
                    TriggerDiverSurfaceRebound();
                }
            }
        }

        private void TriggerDiverSurfaceRebound()
        {
            if (_rb != null) _rb.velocity = new Vector2(_rb.velocity.x * 0.25f, 4.6f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(1.12f);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnBoneCarapaceHitBurst(transform.position + Vector3.down * 0.35f, false);
            }
            TransitionTo(_recoverState);
        }

        private bool IsAirborne()
        {
            return archetype == BellhartExtendedArchetype.Skullwing
                || archetype == BellhartExtendedArchetype.PharlidDiver
                || archetype == BellhartExtendedArchetype.Mossmir;
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
            // Heavy Marrowmaw and PharlidDiver mid-strike preserve momentum against light interruptions
            if (archetype == BellhartExtendedArchetype.Marrowmaw && (_fsm.CurrentState == _windupState || _fsm.CurrentState == _attackState))
            {
                return;
            }
            if (archetype == BellhartExtendedArchetype.PharlidDiver && _fsm.CurrentState == _attackState)
            {
                return;
            }
            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
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
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 0.92f) : (Vector2)transform.position;
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
            float range = _enemy.Stats != null ? _enemy.Stats.detectRange : 8.5f;
            float maxDy = IsAirborne() ? (archetype == BellhartExtendedArchetype.PharlidDiver ? 10.5f : 7.5f) : 2.8f;
            return GetDistToPlayer() <= range && GetVertDeltaToPlayer() <= maxDy;
        }

        private bool ShouldLosePlayer()
        {
            if (!IsPlayerAlive()) return true;
            float lose = _enemy.Stats != null ? _enemy.Stats.loseTargetRange : 13.0f;
            float maxDy = IsAirborne() ? 12.5f : 4.8f;
            return GetDistToPlayer() > lose || GetVertDeltaToPlayer() > maxDy;
        }

        private bool InTriggerAttackRange()
        {
            if (!IsPlayerAlive()) return false;
            float atkRange = _enemy.Stats != null ? _enemy.Stats.attackRange : 4.0f;
            if (archetype == BellhartExtendedArchetype.Skullwing)
            {
                float dy = Mathf.Abs(PlayerChestPos().y - transform.position.y);
                return GetDistToPlayer() <= atkRange && dy <= 1.35f;
            }
            if (archetype == BellhartExtendedArchetype.PharlidDiver)
            {
                float dx = Mathf.Abs(_playerTransform.position.x - transform.position.x);
                bool below = _playerTransform.position.y < transform.position.y - 1.2f;
                return below && dx <= 2.35f && GetDistToPlayer() <= atkRange;
            }
            if (archetype == BellhartExtendedArchetype.Mossmir)
            {
                return GetDistToPlayer() <= atkRange;
            }
            float maxGroundDy = archetype == BellhartExtendedArchetype.Marrowmaw ? 2.6f : 2.1f;
            return GetDistToPlayer() <= atkRange && GetVertDeltaToPlayer() <= maxGroundDy;
        }

        private void ApplyProceduralVisualJuice()
        {
            if (visualTransform == null) return;

            float dir = _enemy.FacingDirection >= 0 ? 1f : -1f;
            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            Vector3 offset = Vector3.zero;
            float rotZ = 0f;

            EnemyAIStateType st = _enemy.CurrentAIState;

            if (st != EnemyAIStateType.Stagger && st != EnemyAIStateType.Death)
            {
                if (archetype == BellhartExtendedArchetype.Mawling && st == EnemyAIStateType.Attack)
                {
                    _rollSpinAngle += Time.deltaTime * 680f;
                    offset = new Vector3(0f, 0.34f, 0f);
                    rotZ = -dir * _rollSpinAngle;
                }
                else
                {
                    _rollSpinAngle = 0f;
                    if (archetype == BellhartExtendedArchetype.Skullwing ||
                        archetype == BellhartExtendedArchetype.PharlidDiver ||
                        archetype == BellhartExtendedArchetype.Mossmir)
                    {
                        offset = new Vector3(0f, Mathf.Sin(_animPhase * 4.2f) * 0.05f, 0f);
                        float vx = _rb != null ? _rb.velocity.x : 0f;
                        rotZ = Mathf.Clamp(-vx * 1.4f, -7f, 7f);
                    }
                }
            }
            else
            {
                _rollSpinAngle = 0f;
            }

            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // FSM STATES
        // =========================================================================

        private sealed class PatrolState : IEnemyState
        {
            private readonly BellhartExtendedAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public PatrolState(BellhartExtendedAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetTurnState(false);
                _ai._enemy.SetChargeState(false);
            }

            public void Tick(float dt)
            {
                if (_ai.CanDetectPlayer())
                {
                    _ai.TransitionTo(_ai._chaseState);
                    return;
                }

                float maxDist = _ai._enemy.Stats != null ? _ai._enemy.Stats.patrolDistance : 4.2f;
                float dx = _ai.transform.position.x - _ai._enemy.SpawnPosition.x;

                if (_ai.IsAirborne())
                {
                    bool hitWall = Physics2D.Raycast(_ai.transform.position, new Vector2(_ai._patrolDir, 0f), 1.05f, _ai._solidAndPlatMask).collider != null;
                    bool limit = (_ai._patrolDir > 0 && dx >= maxDist) || (_ai._patrolDir < 0 && dx <= -maxDist);
                    if (hitWall || limit)
                    {
                        _ai._patrolDir = -_ai._patrolDir;
                        _ai._enemy.SetFacing(_ai._patrolDir);
                    }
                }
                else
                {
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
                    if (_ai.archetype == BellhartExtendedArchetype.Skullwing)
                    {
                        float spd = _ai._enemy.Stats != null ? _ai._enemy.Stats.patrolSpeed : 3.4f;
                        float targetY = _ai._enemy.SpawnPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude;
                        float vy = (targetY - _ai._rb.position.y) * 3.5f;
                        _ai._rb.velocity = new Vector2(_ai._patrolDir * spd, vy);
                    }
                    else if (_ai.archetype == BellhartExtendedArchetype.PharlidDiver)
                    {
                        float spd = _ai._enemy.Stats != null ? _ai._enemy.Stats.patrolSpeed : 2.0f;
                        float targetY = _ai._enemy.SpawnPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude;
                        float vy = (targetY - _ai._rb.position.y) * 4.2f;
                        _ai._rb.velocity = new Vector2(_ai._patrolDir * spd, vy);
                    }
                    else // Mossmir
                    {
                        Vector2 anchor = _ai._enemy.SpawnPosition;
                        Vector2 desired = anchor + new Vector2(
                            Mathf.Sin(_ai._animPhase * 1.05f) * 2.1f,
                            Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude);
                        Vector2 vel = (desired - _ai._rb.position) * 2.2f;
                        if (Mathf.Abs(vel.x) > 0.2f) _ai._enemy.SetFacing(vel.x >= 0f ? 1 : -1);
                        _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 4.2f));
                    }
                    return;
                }

                if (!_ai._enemy.HasGroundAhead(_ai._patrolDir) || _ai._enemy.HasWallAhead(_ai._patrolDir))
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                float gSpd = _ai._enemy.Stats != null ? _ai._enemy.Stats.patrolSpeed : 2.8f;
                _ai._enemy.SetHorizontalVelocity(_ai._patrolDir * gSpd);
            }

            public void Exit() { }
        }

        private sealed class ChaseState : IEnemyState
        {
            private readonly BellhartExtendedAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Chase; } }
            public ChaseState(BellhartExtendedAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.SetChargeState(!_ai.IsAirborne());
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
                if (_ai._rb == null || !_ai.IsPlayerAlive()) return;
                int dir = _ai.DirToPlayer();
                float flySpd = _ai._enemy.Stats != null ? _ai._enemy.Stats.chaseSpeed : 4.2f;

                if (_ai.archetype == BellhartExtendedArchetype.Skullwing)
                {
                    Vector2 chest = _ai.PlayerChestPos();
                    float holdOffsetX = -dir * 4.8f;
                    Vector2 target = new Vector2(chest.x + holdOffsetX, chest.y + 0.15f);
                    Vector2 toDest = target - _ai._rb.position;
                    Vector2 vel = toDest.sqrMagnitude > 0.25f ? toDest.normalized * flySpd : new Vector2(dir * 1.4f, 0f);
                    vel.y += Mathf.Sin(_ai._animPhase * 4.4f) * 0.25f;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 5.6f));
                    return;
                }

                if (_ai.archetype == BellhartExtendedArchetype.PharlidDiver)
                {
                    float ceilY = Mathf.Max(_ai._enemy.SpawnPosition.y - 0.6f, _ai._playerTransform.position.y + 4.8f);
                    Vector2 dest = new Vector2(_ai._playerTransform.position.x, ceilY);
                    Vector2 toDest = dest - _ai._rb.position;
                    Vector2 vel = toDest.sqrMagnitude > 0.20f ? toDest.normalized * flySpd : Vector2.zero;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 5.2f));
                    return;
                }

                if (_ai.archetype == BellhartExtendedArchetype.Mossmir)
                {
                    Vector2 chest = _ai.PlayerChestPos();
                    Vector2 dest = new Vector2(chest.x - dir * 2.8f, chest.y + 1.15f);
                    Vector2 toDest = dest - _ai._rb.position;
                    Vector2 vel = toDest.sqrMagnitude > 0.16f ? toDest.normalized * flySpd : Vector2.zero;
                    vel.y += Mathf.Sin(_ai._animPhase * 2.6f) * 0.42f;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 4.2f));
                    return;
                }

                if (!_ai._enemy.HasGroundAhead(dir) || _ai._enemy.HasWallAhead(dir) || _ai.InTriggerAttackRange())
                {
                    _ai._enemy.StopHorizontal();
                    return;
                }
                _ai._enemy.SetHorizontalVelocity(dir * flySpd);
            }

            public void Exit() { _ai._enemy.SetChargeState(false); }
        }

        private sealed class WindupState : IEnemyState
        {
            private readonly BellhartExtendedAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public WindupState(BellhartExtendedAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                if (!_ai.IsAirborne()) _ai._enemy.StopHorizontal();
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.windupDuration : 0.24f;
                LockAttackVector();

                if (AudioManager.Instance != null)
                {
                    if (_ai.archetype == BellhartExtendedArchetype.PharlidDiver) AudioManager.Instance.PlayEnemyChirp();
                    else AudioManager.Instance.PlayEnemyWindup();
                }
            }

            private void LockAttackVector()
            {
                Vector2 dirVec = (_ai.PlayerChestPos() - (Vector2)_ai.transform.position);
                if (_ai.archetype == BellhartExtendedArchetype.Skullwing)
                {
                    dirVec.y *= 0.22f;
                }
                if (dirVec.sqrMagnitude < 0.01f) dirVec = new Vector2(_ai._enemy.FacingDirection, 0f);
                _ai._lockedVector = dirVec.normalized;
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer > 0.08f)
                {
                    _ai.FacePlayer();
                    LockAttackVector();
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
                    Vector2 pullBack = _ai.archetype == BellhartExtendedArchetype.PharlidDiver
                        ? new Vector2(0f, 1.8f)
                        : new Vector2(-_ai._enemy.FacingDirection * 2.4f, 0.35f);
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, pullBack, Mathf.Clamp01(fixedDt * 8.5f));
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
            private readonly BellhartExtendedAI _ai;
            private float _timer;
            private int _chargeDir = 1;
            private bool _chompComboDone;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public AttackState(BellhartExtendedAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
                _chargeDir = _ai._enemy.FacingDirection >= 0 ? 1 : -1;
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackActiveDuration : 0.36f;
                _chompComboDone = false;
                int dmg = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackDamage : 1;
                float kb = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackKnockbackForce : 7.0f;

                if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();

                if (_ai.archetype == BellhartExtendedArchetype.Skullwing)
                {
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(0.50f, 0f), new Vector2(1.45f, 0.88f), dmg, kb, new Color(0.90f, 0.90f, 0.95f, 0.82f), 0.72f);
                }
                else if (_ai.archetype == BellhartExtendedArchetype.PharlidDiver)
                {
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(0f, -0.30f), new Vector2(1.55f, 1.32f), dmg, kb, new Color(0.96f, 0.96f, 1.0f, 0.86f), 0.82f);
                }
                else if (_ai.archetype == BellhartExtendedArchetype.Mossmir)
                {
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(0.48f, 0f), new Vector2(1.38f, 0.92f), dmg, kb, new Color(0.68f, 0.92f, 0.56f, 0.82f), 0.72f);
                }
                else if (_ai.archetype == BellhartExtendedArchetype.Mawling)
                {
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(0.40f, 0.44f), new Vector2(1.35f, 0.90f), dmg, kb, new Color(0.92f, 0.92f, 0.94f, 0.78f), 0.65f);
                }
                else if (_ai.archetype == BellhartExtendedArchetype.Marrowmaw)
                {
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(0.95f, 0.84f), new Vector2(1.90f, 1.48f), dmg, kb, new Color(0.95f, 0.90f, 0.82f, 0.86f), 0.98f);
                }
                else // Mossgrub
                {
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(0.60f, 0.42f), new Vector2(1.42f, 0.85f), dmg, kb, new Color(0.62f, 0.90f, 0.52f, 0.80f), 0.65f);
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                // Marrowmaw twin jaw chomp follow-up halfway through attack
                if (_ai.archetype == BellhartExtendedArchetype.Marrowmaw && !_chompComboDone && _timer <= 0.24f)
                {
                    _chompComboDone = true;
                    _ai.FacePlayer();
                    _chargeDir = _ai._enemy.FacingDirection >= 0 ? 1 : -1;
                    int dmg = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackDamage : 2;
                    float kb = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackKnockbackForce : 10.5f;
                    _ai._enemy.ActivateCustomAttackHitbox(new Vector2(1.10f, 0.86f), new Vector2(2.15f, 1.56f), dmg, kb, new Color(0.98f, 0.92f, 0.84f, 0.92f), 1.08f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPropBreak(BreakablePropKind.AncientBonePile);
                }
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._recoverState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                float speed = _ai._enemy.Stats != null ? _ai._enemy.Stats.lungeSpeed : 12.0f;

                if (_ai.archetype == BellhartExtendedArchetype.PharlidDiver)
                {
                    if (_ai._rb == null) return;
                    RaycastHit2D hit = Physics2D.CircleCast(_ai._rb.position, 0.45f, _ai._lockedVector, speed * fixedDt + 0.08f, _ai._solidAndPlatMask);
                    if (hit.collider != null)
                    {
                        _ai.TriggerDiverSurfaceRebound();
                        return;
                    }
                    _ai._rb.velocity = _ai._lockedVector * speed;
                    return;
                }

                if (_ai.archetype == BellhartExtendedArchetype.Skullwing || _ai.archetype == BellhartExtendedArchetype.Mossmir)
                {
                    if (_ai._rb == null) return;
                    RaycastHit2D wallHit = Physics2D.CircleCast(_ai._rb.position, 0.42f, _ai._lockedVector, speed * fixedDt + 0.06f, _ai._solidAndPlatMask);
                    if (wallHit.collider != null)
                    {
                        _ai._rb.velocity = -_ai._lockedVector * 1.8f;
                        _ai.TransitionTo(_ai._recoverState);
                        return;
                    }
                    _ai._rb.velocity = _ai._lockedVector * speed;
                    return;
                }

                // Ground Mawling roll, Marrowmaw heavy ram, and Mossgrub bristle lunge
                if (!_ai._enemy.HasGroundAhead(_chargeDir) || _ai._enemy.HasWallAhead(_chargeDir))
                {
                    if (_ai.archetype == BellhartExtendedArchetype.Marrowmaw)
                    {
                        GameEvents.RaiseCameraShake(0.055f, 0.075f);
                    }
                    _ai._enemy.StopHorizontal();
                    _ai.TransitionTo(_ai._recoverState);
                    return;
                }

                _ai._enemy.SetHorizontalVelocity(_chargeDir * speed);
            }

            public void Exit()
            {
                _ai._enemy.DeactivateAttackHitbox();
            }
        }

        private sealed class RecoverState : IEnemyState
        {
            private readonly BellhartExtendedAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public RecoverState(BellhartExtendedAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.recoveryDuration : 0.45f;
                _ai._attackCooldownTimer = _ai._enemy.Stats != null ? _ai._enemy.Stats.attackCooldown : 0.75f;
                if (!_ai.IsAirborne()) _ai._enemy.StopHorizontal();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai.CanDetectPlayer() ? (IEnemyState)_ai._chaseState : _ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == BellhartExtendedArchetype.PharlidDiver && _ai._rb != null)
                {
                    // Rapidly climb back toward ceiling rafters after a dive strike
                    Vector2 ceilingTarget = new Vector2(_ai._rb.position.x, _ai._enemy.SpawnPosition.y);
                    Vector2 toCeil = (ceilingTarget - _ai._rb.position);
                    Vector2 ascend = toCeil.sqrMagnitude > 0.25f ? toCeil.normalized * 6.5f : Vector2.zero;
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, ascend, Mathf.Clamp01(fixedDt * 6.5f));
                }
                else if (_ai.IsAirborne() && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, Vector2.zero, Mathf.Clamp01(fixedDt * 6.0f));
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
            private readonly BellhartExtendedAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(BellhartExtendedAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._enemy.Stats != null ? _ai._enemy.Stats.staggerDuration : 0.16f;
                _ai._enemy.DeactivateAttackHitbox();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai.CanDetectPlayer() ? (IEnemyState)_ai._chaseState : _ai._patrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.IsAirborne() && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, Vector2.zero, Mathf.Clamp01(fixedDt * 5.0f));
                }
            }

            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly BellhartExtendedAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(BellhartExtendedAI ai) { _ai = ai; }
            public void Enter() { }
            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
