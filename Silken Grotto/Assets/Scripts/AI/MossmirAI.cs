using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Refactored Silksong-style Mossmir AI (Primary Aerial Pogo Trainer):
    /// - HoverPatrol: calm, highly predictable sine-wave hover & gentle horizontal sway around (anchorX, anchorY).
    /// - PogoRecoil: on downward pogo hit, absorbs elastic downward dip (recoilOffsetY for ~0.18s) then glides smoothly
    ///   back up to anchorY so the player can reliably chain 2-3+ pogos across chasms or up to high ledges!
    /// - LightRetaliation: if player lingers directly in front horizontally within aggroRange, performs a brief controlled
    ///   buzz nudge then returns straight to anchor so level pogo layouts never break.
    /// - Adaptive rendering: drives 12-24 FPS sprite sheet animations AND elastic procedural flight/pogo squash!
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class MossmirAI : MonoBehaviour
    {
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
        private float _hoverTime;
        private float _retaliationCooldown;
        private float _pogoSquishTimer;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private LayerMask _solidTileMask;

        private HoverPatrolState _hoverPatrolState;
        private PogoRecoilState _pogoRecoilState;
        private LightRetaliationState _lightRetaliationState;
        private StaggerState _staggerState;
        private DeathState _deathState;

        private void Awake()
        {
            _cfg = ENEMY_COMBAT_CONFIG.Mossmir;
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<Animator>();
            _hasSpriteSheetAnimator = _animator != null && _animator.runtimeAnimatorController != null;

            _fsm = new EnemyStateMachine();
            _solidTileMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerBreakableWall);
            _hoverTime = (Mathf.Abs(transform.position.x) * 0.65f) % 6.28f;

            if (visualTransform == null)
            {
                Transform v = transform.Find("Visual");
                visualTransform = v != null ? v : transform;
            }
            _baseVisualScale = visualTransform.localScale;
            _baseVisualScale.x = Mathf.Abs(_baseVisualScale.x);
            _baseVisualLocalPos = visualTransform.localPosition;

            _hoverPatrolState = new HoverPatrolState(this);
            _pogoRecoilState = new PogoRecoilState(this);
            _lightRetaliationState = new LightRetaliationState(this);
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
            TransitionTo(_hoverPatrolState);
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            _hoverTime += dt;
            if (_retaliationCooldown > 0f) _retaliationCooldown -= dt;
            if (_pogoSquishTimer > 0f) _pogoSquishTimer -= dt;

            _fsm.Tick(dt);
            ApplyAdaptiveHoverVisuals();
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
                // Elastic downward pogo recoil (0.18s) keeping Mossmir reliably underneath the player!
                _pogoSquishTimer = _cfg.PogoRecoilDuration;
                ChoralCodeVFX.SpawnMossPogoBurst(transform.position + Vector3.up * 0.32f, new Color(0.56f, 0.92f, 0.34f, 0.96f));
                TransitionTo(_pogoRecoilState);
                return;
            }

            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            _enemy.SetTurnState(false);
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

        private void FacePlayer()
        {
            if (!IsPlayerAlive()) return;
            float dx = _playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.12f)
            {
                _enemy.SetFacing(dx >= 0f ? 1 : -1);
            }
        }

        private bool ShouldLightRetaliate()
        {
            if (!IsPlayerAlive() || _retaliationCooldown > 0f) return false;
            Vector2 pPos = PlayerChestPos();
            Vector2 myPos = transform.position;
            // Only buzz forward when player hovers/stands horizontally aligned right in front (NOT when pogoing from above!)
            float dx = pPos.x - myPos.x;
            float dy = Mathf.Abs(pPos.y - myPos.y);
            if (dy > _cfg.LightRetaliationVertTolerance) return false;
            float dist = Mathf.Abs(dx);
            if (dist > _cfg.LightRetaliationRange || dist < 0.75f) return false;
            RaycastHit2D wallHit = Physics2D.Linecast(myPos, pPos, _solidTileMask);
            return wallHit.collider == null;
        }

        private void ApplyAdaptiveHoverVisuals()
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
                offset.y += Mathf.Sin(_hoverTime * _cfg.HoverFrequency * 1.6f) * 0.045f * procWeight;
                float vx = _rb != null ? _rb.velocity.x : 0f;
                rotZ = Mathf.Clamp(-vx * 2.1f, -10f, 10f) * procWeight;
            }

            if (_pogoSquishTimer > 0f)
            {
                // Elastic bounce squash on downward pogo hit!
                float u = Mathf.Clamp01(_pogoSquishTimer / Mathf.Max(0.05f, _cfg.PogoRecoilDuration));
                float pulse = Mathf.Sin(u * Mathf.PI);
                sx *= (1f + 0.22f * pulse);
                sy *= (1f - 0.22f * pulse);
                offset.y -= 0.12f * pulse;
            }

            visualTransform.localScale = new Vector3(dir * sx, sy, _baseVisualScale.z);
            visualTransform.localPosition = _baseVisualLocalPos + offset;
            visualTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        // =========================================================================
        // 1. HOVER PATROL (Predictable aerial pogo trainer anchor loop)
        // =========================================================================
        private sealed class HoverPatrolState : IEnemyState
        {
            private readonly MossmirAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }
            public HoverPatrolState(MossmirAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai.FacePlayer();
            }

            public void Tick(float dt)
            {
                _ai.FacePlayer();
                if (_ai.ShouldLightRetaliate())
                {
                    _ai.TransitionTo(_ai._lightRetaliationState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                Vector2 targetPos = _ai._anchorPos + new Vector2(
                    Mathf.Sin(_ai._hoverTime * _ai._cfg.PatrolSpeed) * _ai._cfg.PatrolSpanX,
                    Mathf.Sin(_ai._hoverTime * _ai._cfg.HoverFrequency) * _ai._cfg.HoverAmplitude);

                Vector2 toTarget = targetPos - _ai._rb.position;
                Vector2 desiredVel = new Vector2(
                    Mathf.Clamp(toTarget.x * 2.6f, -_ai._cfg.AnchorReturnSpeed, _ai._cfg.AnchorReturnSpeed),
                    Mathf.Clamp(toTarget.y * 3.8f, -4.4f, 4.4f));
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, desiredVel, Mathf.Clamp01(fixedDt * 7.5f));
            }

            public void Exit() { }
        }

        // =========================================================================
        // 2. POGO RECOIL (~0.18s gentle downward dip -> smooth return to anchorY!)
        // =========================================================================
        private sealed class PogoRecoilState : IEnemyState
        {
            private readonly MossmirAI _ai;
            private float _timer;
            private Vector2 _recoilTarget;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public PogoRecoilState(MossmirAI ai) { _ai = ai; }

            public void Enter()
            {
                _timer = _ai._cfg.PogoRecoilDuration;
                _ai._enemy.SetTurnState(true); // Signals hand-drawn Animator Row 2 F1-3 (PogoRecoil)
                float curX = _ai._rb != null ? _ai._rb.position.x : _ai.transform.position.x;
                _recoilTarget = new Vector2(Mathf.Lerp(curX, _ai._anchorPos.x, 0.35f), _ai._anchorPos.y - _ai._cfg.PogoRecoilOffsetY);
                if (_ai._rb != null)
                {
                    // Replace raw knockback with controlled elastic downward recoil so Mossmir stays under player!
                    _ai._rb.velocity = new Vector2(0f, -(_ai._cfg.PogoRecoilOffsetY / Mathf.Max(0.08f, _timer)));
                }
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai._retaliationCooldown = 0.75f; // Stay calm briefly so consecutive pogo bounces feel effortless!
                    _ai.TransitionTo(_ai._hoverPatrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                Vector2 toDip = _recoilTarget - _ai._rb.position;
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, toDip * 6.5f, Mathf.Clamp01(fixedDt * 14f));
            }

            public void Exit()
            {
                _ai._enemy.SetTurnState(false);
            }
        }

        // =========================================================================
        // 3. LIGHT RETALIATION (Short horizontal buzz nudge -> immediate return to anchor)
        // =========================================================================
        private sealed class LightRetaliationState : IEnemyState
        {
            private readonly MossmirAI _ai;
            private float _windupTimer;
            private float _nudgeTimer;
            private bool _nudging;
            private Vector2 _nudgeDir;
            public EnemyAIStateType StateType { get { return _nudging ? EnemyAIStateType.Attack : EnemyAIStateType.Windup; } }
            public LightRetaliationState(MossmirAI ai) { _ai = ai; }

            public void Enter()
            {
                _windupTimer = 0.22f;
                _nudgeTimer = _ai._cfg.LightRetaliationDuration;
                _nudging = false;
                _ai.FacePlayer();
                int dir = _ai._enemy.FacingDirection;
                _nudgeDir = new Vector2(dir, 0f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyWindup();
            }

            public void Tick(float dt)
            {
                if (!_nudging)
                {
                    _windupTimer -= dt;
                    if (_windupTimer <= 0f)
                    {
                        _nudging = true;
                        _ai._enemy.SetAIState(EnemyAIStateType.Attack);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
                    }
                }
                else
                {
                    _nudgeTimer -= dt;
                    if (_nudgeTimer <= 0f)
                    {
                        _ai._retaliationCooldown = _ai._cfg.LightRetaliationCooldown;
                        _ai.TransitionTo(_ai._hoverPatrolState);
                    }
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                if (!_nudging)
                {
                    _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, -_nudgeDir * 1.4f, Mathf.Clamp01(fixedDt * 10f));
                    return;
                }
                RaycastHit2D wallHit = Physics2D.CircleCast(_ai._rb.position, 0.36f, _nudgeDir, _ai._cfg.LightRetaliationSpeed * fixedDt + 0.08f, _ai._solidTileMask);
                if (wallHit.collider != null)
                {
                    _ai._rb.velocity = Vector2.zero;
                    _ai._retaliationCooldown = _ai._cfg.LightRetaliationCooldown;
                    _ai.TransitionTo(_ai._hoverPatrolState);
                    return;
                }
                _ai._rb.velocity = _nudgeDir * _ai._cfg.LightRetaliationSpeed;
            }

            public void Exit() { }
        }

        // =========================================================================
        // 4. STAGGER (Horizontal non-pogo hit) & DEATH
        // =========================================================================
        private sealed class StaggerState : IEnemyState
        {
            private readonly MossmirAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(MossmirAI ai) { _ai = ai; }

            public void Enter() { }

            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    _ai._retaliationCooldown = 0.65f;
                    _ai.TransitionTo(_ai._hoverPatrolState);
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
            private readonly MossmirAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(MossmirAI ai) { _ai = ai; }

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
