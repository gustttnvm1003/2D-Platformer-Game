using UnityEngine;

namespace SilkenGrotto
{
    public enum BellhartFurmArchetype
    {
        Furm,
        WingedFurm
    }

    /// <summary>
    /// Refactored 60 FPS Finite State Machine AI for Batch 2 Bellhart Furm species
    /// (strictly preserving furm.png, winged_furm.png, and their 12-24 FPS sprite sheets untouched):
    /// 1. Furm (Underground Ambush Jumper):
    ///    BurrowedHidden (invulnerable underground, subtle quivering earth mound via code)
    ///    -> EruptWarning (0.22s soil tremble + dirt puff) -> FastPopJump (explosive fast pop-out arc toward player)
    ///    -> VulnerableWindow (0.65s exposed on ground for player punish) -> BurrowDown -> BurrowedHidden.
    /// 2. Winged Furm (Vertical Bobber — Dizzy Helmet Reaction):
    ///    VerticalPatrol (smooth up-down sinusoidal bobbing along fixed X axis, no complex attack)
    ///    -> HelmetClangDizzy (when hit from above via pogo or frontal/helmet strike: bell/metal clang + sparks + short pushback
    ///       + 0.80s Dizzy state swaying side-to-side with code-drawn orbiting dizzy stars above head) -> VerticalPatrol.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class BellhartFurmAI : MonoBehaviour
    {
        [Header("Archetype")]
        [SerializeField] private BellhartFurmArchetype archetype = BellhartFurmArchetype.Furm;

        [Header("Procedural Motion Juice")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hoverAmplitude = 2.65f;
        [SerializeField] private float hoverFrequency = 1.65f;

        private EnemyController _enemy;
        private Rigidbody2D _rb;
        private Hurtbox _hurtbox;
        private EnemyContactHazard _contactHazard;
        private EnemyStateMachine _fsm;
        private Transform _playerTransform;
        private PlayerController _playerController;
        private ENEMY_COMBAT_CONFIG.Batch2Profile _profile;

        private LayerMask _groundMask;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private Vector3 _anchorPosition;

        private float _animPhase;
        private float _cooldownTimer;
        private float _pogoRecoilTimer;

        // Furm underground ambush state & code-drawn earth mound
        private GameObject _burrowMoundObj;
        private bool _isBurrowed;
        private bool _isEruptWarning;
        private float _burrowSinkProgress; // 0 = surfaced, 1 = submerged

        // Winged Furm dizzy helmet state & hand-drawn orbiting dizzy halo sprite overlay
        private GameObject _dizzyStarsObj;
        private SpriteSheetVfxPlayer _dizzyHaloVfx;
        private bool _usingSpriteDizzyHalo;
        private bool _isDizzy;
        private float _dizzyTimer;

        private BurrowedHiddenState _burrowedHiddenState;
        private EruptWarningState _eruptWarningState;
        private FastPopJumpState _fastPopJumpState;
        private VulnerableWindowState _vulnerableWindowState;
        private BurrowDownState _burrowDownState;

        private VerticalPatrolState _verticalPatrolState;
        private HelmetClangDizzyState _dizzyState;

        private StaggerState _staggerState;
        private DeathState _deathState;

        public BellhartFurmArchetype Archetype { get { return archetype; } }

        public void ConfigureArchetype(BellhartFurmArchetype kind)
        {
            archetype = kind;
            if (kind == BellhartFurmArchetype.WingedFurm)
            {
                hoverAmplitude = ENEMY_COMBAT_CONFIG.WingedFurm.VerticalAmplitude;
                hoverFrequency = ENEMY_COMBAT_CONFIG.WingedFurm.VerticalBobSpeed;
            }
        }

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _rb = GetComponent<Rigidbody2D>();
            _hurtbox = GetComponentInChildren<Hurtbox>();
            _contactHazard = GetComponentInChildren<EnemyContactHazard>();
            _fsm = new EnemyStateMachine();
            _profile = archetype == BellhartFurmArchetype.WingedFurm
                ? ENEMY_COMBAT_CONFIG.WingedFurm
                : ENEMY_COMBAT_CONFIG.Furm;

            _groundMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform) | (1 << GameConstants.LayerBreakableWall);
            _animPhase = (Mathf.Abs(transform.position.x) * 0.69f) % 6.28f;
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

            _burrowedHiddenState = new BurrowedHiddenState(this);
            _eruptWarningState = new EruptWarningState(this);
            _fastPopJumpState = new FastPopJumpState(this);
            _vulnerableWindowState = new VulnerableWindowState(this);
            _burrowDownState = new BurrowDownState(this);

            _verticalPatrolState = new VerticalPatrolState(this);
            _dizzyState = new HelmetClangDizzyState(this);

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
            _profile = archetype == BellhartFurmArchetype.WingedFurm
                ? ENEMY_COMBAT_CONFIG.WingedFurm
                : ENEMY_COMBAT_CONFIG.Furm;
            _anchorPosition = transform.position;
            ResolvePlayer();

            if (archetype == BellhartFurmArchetype.WingedFurm)
            {
                hoverAmplitude = _profile.VerticalAmplitude;
                hoverFrequency = _profile.VerticalBobSpeed;
                if (_rb != null)
                {
                    _rb.gravityScale = 0f;
                    _rb.drag = 0f;
                }
                Sprite[] dizzyFrames = VFXManager.Instance != null ? VFXManager.Instance.GetDizzyLoopFrames() : null;
                if (dizzyFrames != null && dizzyFrames.Length > 0)
                {
                    _usingSpriteDizzyHalo = true;
                    _dizzyStarsObj = new GameObject("HandDrawnVFX_DizzyHalo");
                    _dizzyStarsObj.transform.SetParent(transform, false);
                    _dizzyStarsObj.transform.localPosition = new Vector3(0f, 0.86f, 0f);
                    _dizzyStarsObj.transform.localScale = Vector3.one * ENEMY_VFX_CONFIG.WingedFurmDizzyLoop.DefaultScale;
                    _dizzyHaloVfx = _dizzyStarsObj.AddComponent<SpriteSheetVfxPlayer>();
                    _dizzyHaloVfx.ConfigureSorting(
                        ENEMY_VFX_CONFIG.WingedFurmDizzyLoop.SortingOrder,
                        VFXManager.Instance.DefaultSpriteMaterial);
                    _dizzyStarsObj.SetActive(false);
                }
                else
                {
                    _usingSpriteDizzyHalo = false;
                    _dizzyStarsObj = ChoralCodeVFX.CreateDizzyStarsVisual(transform, new Vector3(0f, 0.96f, 0f));
                }
                TransitionTo(_verticalPatrolState);
            }
            else
            {
                _burrowMoundObj = ChoralCodeVFX.CreateBurrowMoundVisual(transform);
                TransitionTo(_burrowedHiddenState);
            }
        }

        private void Update()
        {
            if (_enemy == null || !_enemy.IsAlive) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_playerTransform == null) ResolvePlayer();
            float dt = Time.deltaTime;
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;
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

        private void SetFurmBurrowExposure(bool exposed)
        {
            if (_hurtbox != null) _hurtbox.gameObject.SetActive(exposed);
            if (_contactHazard != null)
            {
                _contactHazard.enabled = exposed;
                if (!exposed) _contactHazard.SuppressContactDamage(10f);
            }
            // Keep visual active so hand-drawn furm_sheet.png Row 1 (BurrowedHidden & EruptWarning) renders directly!
            if (visualTransform != null && visualTransform != transform)
            {
                visualTransform.gameObject.SetActive(true);
            }
            if (_burrowMoundObj != null)
            {
                _burrowMoundObj.SetActive(false);
            }
        }

        private bool IsHelmetStrike(DamageInfo info)
        {
            if (info.IsDownwardPogo) return true;
            if (info.Source == null) return true;
            // Top half or frontal helmet hit on Winged Furm triggers the bell clang + dizzy wobble!
            float hitY = info.HitPoint.sqrMagnitude > 0.001f ? info.HitPoint.y : transform.position.y + 0.1f;
            bool upperHelmet = hitY >= transform.position.y - 0.10f;
            float dx = info.Source.transform.position.x - transform.position.x;
            bool frontal = dx * _enemy.FacingDirection >= -0.15f;
            return upperHelmet || frontal;
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (!_enemy.IsAlive) return;

            if (archetype == BellhartFurmArchetype.WingedFurm)
            {
                _pogoRecoilTimer = 0.20f;
                if (IsHelmetStrike(info))
                {
                    // Iconic bell-helmet clang + Row 5 F1-2 hand-drawn metallic spark + short pushback + 0.80s Dizzy state!
                    Vector3 sparkPos = info.HitPoint.sqrMagnitude > 0.001f
                        ? info.HitPoint
                        : transform.position + new Vector3(0f, 0.55f, 0f);
                    int clangDir = info.KnockbackDirection.x >= 0f ? -1 : 1;
                    if (VFXManager.Instance == null || !VFXManager.Instance.SpawnHelmetClangSpark(sparkPos, clangDir))
                    {
                        ChoralCodeVFX.SpawnBlockSparks(sparkPos, clangDir);
                    }
                    if (info.IsDownwardPogo)
                    {
                        ChoralCodeVFX.SpawnMossPogoBurst(transform.position + new Vector3(0f, 0.55f, 0f), new Color(1.0f, 0.88f, 0.52f, 0.96f));
                    }
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayPropBreak(BreakablePropKind.CrackedBell, 0.92f);
                        AudioManager.Instance.PlayArmorClink(0.90f);
                    }
                    if (_rb != null)
                    {
                        Vector2 pushDir = info.IsDownwardPogo
                            ? new Vector2(info.KnockbackDirection.x * 0.45f, -1f).normalized
                            : (info.KnockbackDirection.sqrMagnitude > 0.001f ? info.KnockbackDirection.normalized : Vector2.down);
                        _rb.velocity = pushDir * _profile.HelmetPushbackForce;
                    }
                    TransitionTo(_dizzyState);
                    return;
                }

                TransitionTo(_staggerState);
                return;
            }

            // Ground Furm: takes clean damage while exposed in VulnerableWindow / FastPopJump
            if (info.IsDownwardPogo)
            {
                _pogoRecoilTimer = 0.16f;
                ChoralCodeVFX.SpawnMossPogoBurst(transform.position + new Vector3(0f, 0.9f, 0f), new Color(0.90f, 0.78f, 0.52f, 0.95f));
            }
            TransitionTo(_staggerState);
        }

        private void HandleDied()
        {
            _isBurrowed = false;
            _isEruptWarning = false;
            _isDizzy = false;
            if (_burrowMoundObj != null) _burrowMoundObj.SetActive(false);
            if (_dizzyStarsObj != null) _dizzyStarsObj.SetActive(false);
            if (visualTransform != null && visualTransform != transform) visualTransform.gameObject.SetActive(true);
            TransitionTo(_deathState);
            if (visualTransform != null) visualTransform.localRotation = Quaternion.identity;
        }

        private bool IsPlayerAlive()
        {
            return _playerTransform != null && (_playerController == null || _playerController.IsAlive);
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

        private bool IsGroundedNow()
        {
            return Physics2D.Raycast(transform.position, Vector2.down, 0.30f, _groundMask).collider != null;
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
                if (archetype == BellhartFurmArchetype.WingedFurm)
                {
                    if (_isDizzy)
                    {
                        // Playful dizzy side-to-side helmet wobble!
                        float wobble = Mathf.Sin(_animPhase * 19f);
                        rotZ = wobble * 17f;
                        offset.x = wobble * 0.08f;
                        offset.y = Mathf.Cos(_animPhase * 12f) * 0.05f;
                        sx *= 1f + 0.06f * Mathf.Sin(_animPhase * 22f);
                        sy *= 1f - 0.06f * Mathf.Sin(_animPhase * 22f);
                    }
                    else
                    {
                        offset.y = Mathf.Sin(_animPhase * 5.2f) * 0.045f;
                        float vy = _rb != null ? _rb.velocity.y : 0f;
                        rotZ = Mathf.Clamp(dir * vy * 2.2f, -8f, 8f);
                    }

                    if (_dizzyStarsObj != null && _dizzyStarsObj.activeSelf)
                    {
                        if (_usingSpriteDizzyHalo)
                        {
                            // Subtle playful halo sway paired with Row 5 F3-6 18 FPS sprite orbit loop!
                            _dizzyStarsObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_animPhase * 6.5f) * 7f);
                        }
                        else
                        {
                            // Fallback code-drawn orbiting dizzy stars
                            _dizzyStarsObj.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_animPhase * 7f) * 14f);
                            for (int i = 0; i < _dizzyStarsObj.transform.childCount; i++)
                            {
                                Transform ch = _dizzyStarsObj.transform.GetChild(i);
                                if (ch != null && ch.name.StartsWith("DizzyStar_"))
                                {
                                    float ang = _animPhase * 5.4f + (i * Mathf.PI * 2f / 3f);
                                    ch.localPosition = new Vector3(Mathf.Cos(ang) * 0.58f, Mathf.Sin(ang) * 0.20f, 0f);
                                    ch.localRotation = Quaternion.Euler(0f, 0f, _animPhase * 180f);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Ground Furm: mound tremor while hidden/warning, vertical stretch on FastPopJump, burrow sink squash
                    if (_burrowMoundObj != null && _burrowMoundObj.activeSelf)
                    {
                        float shake = _isEruptWarning
                            ? Mathf.Sin(_animPhase * 82f) * 0.085f
                            : Mathf.Sin(_animPhase * 11f) * 0.015f;
                        _burrowMoundObj.transform.localPosition = new Vector3(shake, 0.05f, 0f);
                        float moundScale = _isEruptWarning ? (1.14f + Mathf.Sin(_animPhase * 55f) * 0.10f) : 1.0f;
                        _burrowMoundObj.transform.localScale = new Vector3(moundScale, moundScale, 1f);
                    }

                    if (st == EnemyAIStateType.Attack)
                    {
                        // Subtle stretch reinforcement on FastPopJump
                        sx *= 0.94f;
                        sy *= 1.06f;
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
        // 1. FURM STATES (BurrowedHidden -> EruptWarning -> FastPopJump -> VulnerableWindow -> BurrowDown)
        // =========================================================================

        private sealed class BurrowedHiddenState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }
            public BurrowedHiddenState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isBurrowed = true;
                _ai._isEruptWarning = false;
                _ai._burrowSinkProgress = 0f;
                _ai._enemy.StopHorizontal();
                _ai.SetFurmBurrowExposure(false);
            }

            public void Tick(float dt)
            {
                if (_ai._cooldownTimer > 0f || !_ai.IsPlayerAlive()) return;
                float dx = Mathf.Abs(_ai._playerTransform.position.x - _ai.transform.position.x);
                if (dx <= _ai._profile.BurrowTriggerRange && _ai.GetVertDeltaToPlayer() <= 3.2f)
                {
                    _ai.FacePlayer();
                    _ai.TransitionTo(_ai._eruptWarningState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { }
        }

        private sealed class EruptWarningState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Windup; } }
            public EruptWarningState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isBurrowed = true;
                _ai._isEruptWarning = true;
                _timer = _ai._profile.EruptWarningDuration; // 0.22s soil tremble telegraph
                ChoralCodeVFX.SpawnBurrowDirtBurst(_ai.transform.position, false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(false);
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._fastPopJumpState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { _ai._isEruptWarning = false; }
        }

        private sealed class FastPopJumpState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            private float _airTime;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Attack; } }
            public FastPopJumpState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isBurrowed = false;
                _ai._burrowSinkProgress = 0f;
                _airTime = 0f;
                _ai.FacePlayer();
                _ai.SetFurmBurrowExposure(true);

                int dir = _ai._enemy.FacingDirection;
                if (_ai._rb != null)
                {
                    float dx = _ai.IsPlayerAlive() ? Mathf.Abs(_ai._playerTransform.position.x - _ai.transform.position.x) : 3.2f;
                    float jumpVx = Mathf.Clamp(dx * 1.9f, 4.4f, _ai._profile.PopJumpSpeedX);
                    // Prevent jumping blindly off a platform cliff if near edge
                    if (!_ai._enemy.HasGroundAhead(dir)) jumpVx *= 0.35f;
                    _ai._rb.velocity = new Vector2(dir * jumpVx, _ai._profile.PopJumpVelY);
                }

                ChoralCodeVFX.SpawnBurrowDirtBurst(_ai.transform.position, true);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            }

            public void Tick(float dt)
            {
                _airTime += dt;
                if ((_airTime > 0.16f && _ai.IsGroundedNow() && (_ai._rb == null || _ai._rb.velocity.y <= 0.15f)) || _airTime >= 0.95f)
                {
                    _ai.TransitionTo(_ai._vulnerableWindowState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                int dir = _ai._enemy.FacingDirection;
                if (_ai._enemy.HasWallAhead(dir) && _ai._rb != null)
                {
                    _ai._rb.velocity = new Vector2(0f, _ai._rb.velocity.y);
                }
            }

            public void Exit() { }
        }

        private sealed class VulnerableWindowState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            private float _timer;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public VulnerableWindowState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._enemy.StopHorizontal();
                _timer = _ai._profile.VulnerableWindowDuration; // 0.65s fair punish window
                if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
            }

            public void Tick(float dt)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _ai.TransitionTo(_ai._burrowDownState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit() { }
        }

        private sealed class BurrowDownState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            private float _elapsed;
            private float _duration;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Idle; } }
            public BurrowDownState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _elapsed = 0f;
                _duration = _ai._profile.BurrowReenterDelay > 0.08f ? _ai._profile.BurrowReenterDelay : 0.26f;
                _ai._enemy.StopHorizontal();
                _ai._enemy.SetChargeState(true); // Drives Row 3 Frames 4-6 (Reburrow)
                ChoralCodeVFX.SpawnBurrowDirtBurst(_ai.transform.position, false);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(false);
            }

            public void Tick(float dt)
            {
                _elapsed += dt;
                _ai._burrowSinkProgress = Mathf.Clamp01(_elapsed / _duration);
                if (_elapsed >= _duration)
                {
                    _ai._cooldownTimer = _ai._profile.AttackCooldown;
                    _ai.TransitionTo(_ai._burrowedHiddenState);
                }
            }

            public void FixedTick(float fixedDt) { _ai._enemy.StopHorizontal(); }
            public void Exit()
            {
                _ai._burrowSinkProgress = 0f;
                _ai._enemy.SetChargeState(false);
            }
        }

        // =========================================================================
        // 2. WINGED FURM STATES (VerticalPatrol <-> HelmetClangDizzy)
        // =========================================================================

        private sealed class VerticalPatrolState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Patrol; } }
            public VerticalPatrolState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDizzy = false;
                _ai._enemy.SetChargeState(false);
                if (_ai._dizzyStarsObj != null) _ai._dizzyStarsObj.SetActive(false);
            }

            public void Tick(float dt) { }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                // Pure smooth vertical up-down bobbing on fixed X axis!
                float targetY = _ai._anchorPosition.y + Mathf.Sin(_ai._animPhase * _ai.hoverFrequency) * _ai.hoverAmplitude;
                Vector2 desiredPos = new Vector2(_ai._anchorPosition.x, targetY);
                Vector2 vel = (desiredPos - _ai._rb.position) * 3.4f;
                _ai._rb.velocity = Vector2.Lerp(_ai._rb.velocity, vel, Mathf.Clamp01(fixedDt * 6.5f));
            }

            public void Exit() { }
        }

        private sealed class HelmetClangDizzyState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            // Recover + IsCharging routes cleanly to Row 2 (HelmetClangDizzy) instead of Row 3 Hurt!
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Recover; } }
            public HelmetClangDizzyState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter()
            {
                _ai._isDizzy = true;
                _ai._dizzyTimer = _ai._profile.DizzyDuration > 0.2f ? _ai._profile.DizzyDuration : 0.80f;
                _ai._enemy.SetChargeState(true);
                // Loop shared hand-drawn Dizzy Halo (Row 5 F3-6 @ 18 FPS) above Winged Furm's helmet throughout Dizzy!
                if (_ai._dizzyStarsObj != null && _ai._usingSpriteDizzyHalo && _ai._dizzyHaloVfx != null && VFXManager.Instance != null)
                {
                    Sprite[] loopFrames = VFXManager.Instance.GetDizzyLoopFrames();
                    if (loopFrames != null && loopFrames.Length > 0)
                    {
                        _ai._dizzyStarsObj.SetActive(true);
                        _ai._dizzyHaloVfx.Play(loopFrames, ENEMY_VFX_CONFIG.WingedFurmDizzyLoop.Fps, true, false);
                    }
                }
                else if (_ai._dizzyStarsObj != null)
                {
                    _ai._dizzyStarsObj.SetActive(true);
                }
            }

            public void Tick(float dt)
            {
                _ai._dizzyTimer -= dt;
                if (_ai._dizzyTimer <= 0f)
                {
                    _ai.TransitionTo(_ai._verticalPatrolState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai._rb == null) return;
                // Dampen initial pushback smoothly while wobbling in place
                _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 10.5f * fixedDt);
            }

            public void Exit()
            {
                _ai._isDizzy = false;
                _ai._enemy.SetChargeState(false);
                if (_ai._dizzyStarsObj != null) _ai._dizzyStarsObj.SetActive(false);
            }
        }

        // =========================================================================
        // SHARED STAGGER & DEATH
        // =========================================================================

        private sealed class StaggerState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Stagger; } }
            public StaggerState(BellhartFurmAI ai) { _ai = ai; }

            public void Enter() { _ai._enemy.DeactivateAttackHitbox(); }

            public void Tick(float dt)
            {
                if (!_ai._enemy.IsStaggered)
                {
                    if (_ai.archetype == BellhartFurmArchetype.WingedFurm)
                        _ai.TransitionTo(_ai._verticalPatrolState);
                    else
                        _ai.TransitionTo(_ai._burrowDownState);
                }
            }

            public void FixedTick(float fixedDt)
            {
                if (_ai.archetype == BellhartFurmArchetype.WingedFurm && _ai._rb != null)
                {
                    _ai._rb.velocity = Vector2.MoveTowards(_ai._rb.velocity, Vector2.zero, 14f * fixedDt);
                }
            }

            public void Exit() { }
        }

        private sealed class DeathState : IEnemyState
        {
            private readonly BellhartFurmAI _ai;
            public EnemyAIStateType StateType { get { return EnemyAIStateType.Death; } }
            public DeathState(BellhartFurmAI ai) { _ai = ai; }
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
