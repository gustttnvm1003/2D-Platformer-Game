using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Refactored Silksong-style Aknid AI (Aerial Carrier & Secondary Pogo Target):
    /// - Carry Payload System (`carryType`: None | Berry | Flea, configurable per instance):
    ///   renders a suspended code-drawn indicator directly beneath Aknid; releases on first hit or death
    ///   (Berry drops to floor as healing/essence pickup; Flea escapes joyfully with celebratory sparkles!).
    /// - CarryFlight / Patrol: wider, more dynamic patrol than Mossmir; subtly weaves/evades height when player approaches
    ///   so aerial pogo timing feels slightly livelier while remaining 100% responsive with elastic PogoRecoil!
    /// - DiveNudge: when carryType == None, performs a crisp light close-range DiveNudge before climbing back safely.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class AknidAI : MonoBehaviour
    {
        [Header("Carry Payload (Configurable Per Spawn Instance)")]
        [SerializeField] private AknidCarryType carryType = AknidCarryType.None;

        [Header("Visual Reference")]
        [SerializeField] private Transform visualTransform;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private Animator _animator;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.MossGrottoProfile _cfg;
        private bool _hasSpriteSheetAnimator;

        private Vector2 _anchorPos;
        private float _flightTime;
        private float _diveCooldown;
        private float _pogoSquishTimer;
        private bool _payloadDropped;
        private GameObject _carryVisualObj;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private LayerMask _solidTileMask;

        private CarryPatrolState _carryPatrolState;
        private PogoRecoilState _pogoRecoilState;
        private DiveNudgeState _diveNudgeState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        public AknidCarryType CarryType { get { return carryType; } }

        public void ConfigureCarry(AknidCarryType payload)
        {
            carryType = payload;
            _payloadDropped = false;
            RefreshCarryVisual();
        }

        private void Awake()
        {
            _cfg = ENEMY_COMBAT_CONFIG.Aknid;
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _hasSpriteSheetAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            _fsm = new EnemyStateMachine();
            _solidTileMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _flightTime = (Mathf.Abs(transform.position.x) * 0.77f) % 6.28f;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualLocalPos = visualTransform.localPosition;

            _carryPatrolState = new CarryPatrolState(this);
            _pogoRecoilState = new PogoRecoilState(this);
            _diveNudgeState = new DiveNudgeState(this);
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
            _anchorPos = transform.position;
            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.drag = 0f;
            }
            RefreshCarryVisual();
            TransitionTo(_carryPatrolState);
        }

        private void RefreshCarryVisual()
        {
            if (_carryVisualObj != null)
            {
                Destroy(_carryVisualObj);
                _carryVisualObj = null;
            }
            if (!_payloadDropped && carryType != AknidCarryType.None)
            {
                _carryVisualObj = ChoralCodeVFX.CreateAknidCarryVisual(transform, carryType);
            }
        }

        private void ReleaseCarryPayload(Vector2 releaseImpulse)
        {
            if (_payloadDropped || carryType == AknidCarryType.None) return;
            _payloadDropped = true;

            Vector3 dropPos = _carryVisualObj != null
                ? _carryVisualObj.transform.position
                : transform.position + new Vector3(0f, -0.52f, 0f);

            if (_carryVisualObj != null)
            {
                Destroy(_carryVisualObj);
                _carryVisualObj = null;
            }

            GameObject dropGo = new GameObject("AknidDropped_" + carryType);
            dropGo.AddComponent<CircleCollider2D>();
            AknidPayloadDrop drop = dropGo.AddComponent<AknidPayloadDrop>();
            drop.Initialize(carryType, dropPos, releaseImpulse);
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            _flightTime += dt;
            if (_diveCooldown > 0f) _diveCooldown -= dt;
            if (_pogoSquishTimer > 0f) _pogoSquishTimer -= dt;

            _fsm.Tick(dt);
            ApplyAdaptiveFlightVisuals();
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

            if (_cfg.DropPayloadOnFirstHit)
            {
                Vector2 burst = new Vector2(Random.Range(-1.8f, 1.8f), carryType == AknidCarryType.Flea ? 4.2f : 1.6f);
                ReleaseCarryPayload(burst);
            }

            if (info.IsDownwardPogo)
            {
                _pogoSquishTimer = _cfg.PogoRecoilDuration;
                ChoralCodeVFX.SpawnMossPogoBurst(transform.position + Vector3.up * 0.28f, new Color(0.68f, 0.96f, 0.44f, 0.96f));
                TransitionTo(_pogoRecoilState);
                return;
            }

            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            _enemy.SetTurnState(false);
            if (_cfg.DropPayloadOnDeath)
            {
                Vector2 burst = new Vector2(Random.Range(-1.6f, 1.6f), carryType == AknidCarryType.Flea ? 4.4f : 2.2f);
                ReleaseCarryPayload(burst);
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
            return _playerTransform != null ? (Vector2)_playerTransform.position + new Vector2(0f, 0.92f) : (Vector2)transform.position;
        }

        private bool ShouldDiveNudge()
        {
            // DiveNudge only executes when NOT carrying an item/flea!
            if (carryType != AknidCarryType.None && !_payloadDropped) return false;
            if (!IsPlayerAlive() || _diveCooldown > 0f) return false;
            Vector2 pPos = PlayerChestPos();
            Vector2 myPos = transform.position;
            float dist = Vector2.Distance(myPos, pPos);
            if (dist > _cfg.DiveNudgeRange || dist < 1.0f) return false;
            // Avoid interrupting players actively pogoing directly above
            if (pPos.y > myPos.y + 0.9f) return false;
            RaycastHit2D wallHit = Physics2D.Linecast(myPos, pPos, _solidTileMask);
            return wallHit.collider == null;
        }

        private void ApplyAdaptiveFlightVisuals()
        {
            if (visualTransform == null) return;
            int dir = _enemy != null ? _enemy.FacingDirection : 1;
            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            float rotZ = 0f;
            Vector3 offset = Vector3.zero;
            float procWeight = _hasSpriteSheetAnimator ? 0.45f : 1.0f;

            if (_fsm.CurrentState != _deathState)
            {
                offset.y += Mathf.Sin(_flightTime * _cfg.HoverFrequency * 1.9f) * 0.055f * procWeight;
                float vx = _rb != null ? _rb.velocity.x : 0f;
                rotZ = Mathf.Clamp(-vx * 2.2f, -12f, 12f) * procWeight;
            }

            if (_pogoSquishTimer > 0f)
            {
                float u = Mathf.Clamp01(_pogoSquishTimer / Mathf.Max(0.05f, _cfg.PogoRecoilDuration));
                float pulse = Mathf.Sin(u * Mathf.PI);
                sx *= (1f + 0.22f * pulse);
                sy *= (1f - 0.22f * pulse);
                offset.y -= 0.11f * pulse;
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            if (_carryVisualObj != null)
            {
                float vx = _rb != null ? _rb.velocity.x : 0f;
                float sway = Mathf.Clamp(-vx * 0.045f, -0.18f, 0.18f) + Mathf.Sin(_flightTime * 5.2f) * 0.04f;
                _carryVisualObj.transform.localPosition = new Vector3(sway, -0.58f + offset.y * 0.7f, 0f);
            }
        }

        // =========================================================================
        // 1. CARRY FLIGHT / DYNAMIC PATROL (Wider & subtly evasive secondary pogo target)
        // =========================================================================
        private sealed class CarryPatrolState : IEnemyState
        {
            private readonly AknidAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public CarryPatrolState(AknidAI ai) { _ai = ai; }

            public void Enter() { }

            public void Tick(float dt)
            {
                if (_ai.ShouldDiveNudge())
                {
                    _ai.TransitionTo(_ai._diveNudgeState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                Vector2 targetPos = _ai._anchorPos + new Vector2(
                    Mathf.Sin(_ai._flightTime * (_ai._cfg.PatrolSpeed * 0.48f)) * _ai._cfg.PatrolSpanX,
                    Mathf.Sin(_ai._flightTime * _ai._cfg.HoverFrequency) * _ai._cfg.HoverAmplitude);

                // Subtle evasive diagonal bob when player draws near (makes pogo timing slightly more dynamic than Mossmir!)
                if (_ai.IsPlayerAlive())
                {
                    Vector2 pPos = _ai.PlayerChestPos();
                    Vector2 away = _ai._rb.position - pPos;
                    float dist = away.magnitude;
                    if (dist < _ai._cfg.EvadeDetectRange && dist > 0.35f)
                    {
                        float evadeFactor = 1f - (dist / _ai._cfg.EvadeDetectRange);
                        targetPos.x += Mathf.Sign(away.x) * evadeFactor * 1.15f;
                        targetPos.y += Mathf.Clamp(away.y * 0.35f, -0.35f, 0.55f) * evadeFactor;
                    }
                }

                Vector2 toTarget = targetPos - _ai._rb.position;
                Vector2 desiredVel = new Vector2(
                    Mathf.Clamp(toTarget.x * 2.5f, -_ai._cfg.EvadeSpeed, _ai._cfg.EvadeSpeed),
                    Mathf.Clamp(toTarget.y * 3.4f, -3.8f, 3.8f));
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, desiredVel, Mathf.Clamp01(fixedDt * 6.4f));

                if (Mathf.Abs(_ai._rb.velocity.x) > 0.22f)
                {
                    _ai._enemy.SetFacing(_ai._rb.velocity.x >= 0f ? 1 : -1);
                }
            }

            public void Exit() { }
        }

        // =========================================================================
        // 2. ELASTIC POGO RECOIL
        // =========================================================================
        private sealed class PogoRecoilState : IEnemyState
        {
            private readonly AknidAI _ai;
            private float _timer;
            private Vector2 _dipTarget;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public PogoRecoilState(AknidAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._cfg.PogoRecoilDuration;
                _ai._enemy.SetTurnState(true); // Signals hand-drawn Animator Row 2 F4-6 (PogoRecoil)
                Vector2 cur = _ai._rb != null ? _ai._rb.position : (Vector2)_ai.transform.position;
                _dipTarget = new Vector2(cur.x, cur.y - _ai._cfg.PogoRecoilOffsetY);
                if (_ai._rb != null)
                {
                    _ai._rb.velocity = new Vector2(0f, -(_ai._cfg.PogoRecoilOffsetY / Mathf.Max(0.08f, _timer)));
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai._diveCooldown = 0.85f;
                    _ai.TransitionTo(_ai._carryPatrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                Vector2 toDip = _dipTarget - _ai._rb.position;
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, toDip * 6.8f, Mathf.Clamp01(fixedDt * 14f));
            }

            public void Exit()
            {
                _ai._enemy.SetTurnState(false);
            }
        }

        // =========================================================================
        // 3. LIGHT DIVE NUDGE (Only when CarryType == None!)
        // =========================================================================
        private sealed class DiveNudgeState : IEnemyState
        {
            private readonly AknidAI _ai;
            private float _windupTimer;
            private float _diveTimer;
            private bool _diving;
            private Vector2 _diveDir;
            public EnemyAIStateType StateType { get { return _diving ? EnemyAIStateType.Attack : EnemyAIStateType.Windup; } }
            public DiveNudgeState(AknidAI ai) { _ai = ai; }

            public void Enter()
            {
                _windupTimer = 0.22f;
                _diveTimer = _ai._cfg.DiveNudgeDuration;
                _diving = false;
                if (_ai.IsPlayerAlive())
                {
                    Vector2 delta = _ai.PlayerChestPos() - (Vector2)_ai.transform.position;
                    _diveDir = delta.sqrMagnitude > 0.01f ? delta.normalized : Vector2.right;
                    _ai._enemy.SetFacing(_diveDir.x >= 0f ? 1 : -1);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            public void Tick(float dt)
            {
                if (!_diving)
                {
                    _windupTimer -= dt;
                    if (_windupTimer <= 0f)
                    {
                        _diving = true;
                        _ai._enemy.SetAIState(EnemyAIStateType.Attack);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                    }
                }
                else
                {
                    _diveTimer -= dt;
                    if (_diveTimer <= 0f)
                    {
                        _ai._diveCooldown = _ai._cfg.DiveNudgeCooldown;
                        _ai.TransitionTo(_ai._carryPatrolState);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                if (!_diving)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, -_diveDir * 1.5f, Mathf.Clamp01(fixedDt * 10f));
                    return;
                }
                RaycastHit2D wallHit = Physics2D.CircleCast(_ai._rb.position, 0.36f, _diveDir, _ai._cfg.DiveNudgeSpeed * fixedDt + 0.08f, _ai._solidTileMask);
                if (wallHit.collider != null)
                {
                    _ai._rb.velocity = Vector2.zero;
                    _ai._diveCooldown = _ai._cfg.DiveNudgeCooldown;
                    _ai.TransitionTo(_ai._carryPatrolState);
                    return;
                }
                _ai._rb.velocity = _diveDir * _ai._cfg.DiveNudgeSpeed;
            }

            public void Exit() { }
        }

        // =========================================================================
        // 4. STAGGER & DEATH
        // =========================================================================
        private sealed class StaggerState : IEnemyState
        {
            private readonly AknidAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(AknidAI ai) { _ai = ai; }

            public void Enter() { }

            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    _ai._diveCooldown = 0.75f;
                    _ai.TransitionTo(_ai._carryPatrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 16f * fixedDt);
            }

            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly AknidAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(AknidAI ai) { _ai = ai; }

            public void Enter()
            {
                if (_ai._rb != null) _ai._rb.velocity = Vector2.zero;
            }

            public void Tick(float dt) { }
            public void FixedTick(float fixedDt) { }
            public void Exit() { }
        }
    }
}
