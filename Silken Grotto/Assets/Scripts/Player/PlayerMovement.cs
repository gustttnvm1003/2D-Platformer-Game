using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Responsive 2D Metroidvania platformer movement inspired by Hollow Knight: Silksong.
    /// Supports Walk, Run, Variable Jump, Coyote Time, Jump Buffer, Double Jump, Air Control,
    /// Wall Slide/Jump, Ground & Air Dash, Falling & Landing Detection, and Pogo Bounce.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private SkillManager skillManager;
        [SerializeField] private Transform visualRoot;

        [Header("Collision Checks")]
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, 0.0f);
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.54f, 0.14f);
        [SerializeField] private float wallCheckDistance = 0.44f;
        [SerializeField] private float wallCheckHeight = 1.0f;

        [Header("Movement Tuning")]
        [SerializeField] private float groundAcceleration = 75f;
        [SerializeField] private float groundDeceleration = 85f;
        [SerializeField] private float airAcceleration = 46f;
        [SerializeField] private float airDeceleration = 30f;
        [SerializeField] private float coyoteTimeDuration = 0.12f;
        [SerializeField] private float jumpCutMultiplier = 0.48f;
        [SerializeField] private float fallGravityMultiplier = 1.45f;
        [SerializeField] private float maxFallSpeed = 20f;
        [SerializeField] private float wallSlideMaxSpeed = 3.2f;
        [SerializeField] private float glideMaxFallSpeed = 2.65f;
        [SerializeField] private Vector2 wallJumpForce = new Vector2(10.5f, 14.2f);
        [SerializeField] private float landingDuration = 0.11f;
        [SerializeField] private float footstepInterval = 0.30f;

        private Rigidbody2D _rb;
        private Collider2D _bodyCollider;
        private Collider2D _ignoredMantleCollider;
        private Collider2D _ignoredDropCollider;
        private float _baseGravityScale = 3.2f;
        private RigidbodyInterpolation2D _baseInterpolation = RigidbodyInterpolation2D.Interpolate;
        private int _facingDirection = 1;
        private bool _isGrounded;
        private bool _wasGrounded;
        private bool _isTouchingWall;
        private int _wallDirection;
        private bool _isWallSliding;
        private float _coyoteTimer;
        private bool _hasUsedDoubleJump;
        private bool _hasUsedAirDash;
        private bool _isDashing;
        private bool _dashIsAir;
        private bool _dashIsDownward;
        private float _dashTimer;
        private float _dashSpeed;
        private float _wallJumpLockTimer;
        private float _knockbackLockTimer;
        private float _landingTimer;
        private float _heroLandingTimer;
        private bool _jumpedSinceGrounded;
        private bool _wasMovingOnGround;
        private float _footstepTimer;
        private float _fallStreakTimer;
        private float _peakAirFallSpeed;
        private bool _standingOnStone;
        private bool _standingOnIce;
        private float _iceAccelMult = 1f;
        private float _iceDecelMult = 1f;
        private Collider2D _standingOneWayCollider;
        private bool _movementLockedByCombat;
        private bool _movementLockedByBind;
        private bool _airSuspendLocked;
        private bool _externalVelocityActive;
        private Vector2 _externalVelocity;
        private bool _isGliding;
        private float _glideVfxTimer;
        private float _turnaroundTimer;
        private float _skidPivotTimer;
        private float _skidStopTimer;
        private int _skidLeanDir;
        private float _lungeTimer;
        private float _lungeVelocityX;
        private float _visualJuiceTime;
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualLocalPos;
        private bool _hasCapturedVisualBase;
        private bool _isMantling;
        private float _ledgeCooldownTimer;
        private Coroutine _mantleCoroutine;
        private bool _isDead;
        private bool _isRestingOnBench;
        private float _benchApproachAnimSpeed;
        private Vector3 _lastSafeGroundPosition;

        public int FacingDirection { get { return _facingDirection; } }
        public int WallDirection { get { return _wallDirection; } }
        public bool IsGrounded { get { return _isGrounded; } }
        public bool IsStandingOnStone { get { return _standingOnStone; } }
        public bool IsDashing { get { return _isDashing; } }
        public bool IsAirDashing { get { return _isDashing && _dashIsAir && !_dashIsDownward; } }
        public bool IsDownwardDashing { get { return _isDashing && _dashIsDownward; } }
        public bool IsGliding { get { return _isGliding; } }
        public bool IsSkidding { get { return _skidStopTimer > 0f || _skidPivotTimer > 0f; } }
        public bool IsPivoting { get { return _skidPivotTimer > 0f; } }
        public bool CanDashCancelToSprintAttack { get { return _isDashing && !_dashIsAir && _dashTimer <= 0.075f; } }
        public bool IsWallSliding { get { return _isWallSliding; } }
        public bool IsLanding { get { return _landingTimer > 0f; } }
        public bool IsHeroLanding { get { return _heroLandingTimer > 0f; } }
        public bool IsMantling { get { return _isMantling; } }
        public bool IsRestingOnBench { get { return _isRestingOnBench; } }
        public float BenchApproachAnimSpeed { get { return _benchApproachAnimSpeed; } }
        public bool IsRunning { get { return _isGrounded && inputHandler != null && inputHandler.SprintHeld && Mathf.Abs(HorizontalVelocity) > 0.25f; } }
        public float HorizontalVelocity { get { return _rb != null ? _rb.velocity.x : 0f; } }
        public float VerticalVelocity { get { return _rb != null ? _rb.velocity.y : 0f; } }
        public Vector3 LastSafeGroundPosition { get { return _lastSafeGroundPosition; } }

        public event System.Action OnJumped;
        public event System.Action OnWallJumped;
        public event System.Action OnDoubleJumped;
        public event System.Action OnLanded;
        public event System.Action OnHeroLanded;
        public event System.Action OnRunStarted;
        public event System.Action OnLedgeSomersault;
        public event System.Action OnDashed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            if (skillManager == null) skillManager = GetComponent<SkillManager>();

            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _baseInterpolation = _rb.interpolation;
            _baseGravityScale = _rb.gravityScale > 0.1f ? _rb.gravityScale : 3.2f;
            _rb.gravityScale = _baseGravityScale;

            Collider2D bodyCol = GetComponent<Collider2D>();
            _bodyCollider = bodyCol;
            if (bodyCol != null && bodyCol.sharedMaterial == null)
            {
                PhysicsMaterial2D noFriction = new PhysicsMaterial2D("PlayerFrictionless");
                noFriction.friction = 0f;
                noFriction.bounciness = 0f;
                bodyCol.sharedMaterial = noFriction;
            }

            if (groundLayers.value == 0)
            {
                groundLayers = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform) | (1 << GameConstants.LayerBreakableWall);
            }

            _lastSafeGroundPosition = transform.position;
            CaptureVisualBaseIfNeeded();
        }

        private void CaptureVisualBaseIfNeeded()
        {
            if (_hasCapturedVisualBase) return;
            Transform target = visualRoot != null ? visualRoot : transform;
            if (target == null) return;
            _baseVisualScale = target.localScale;
            _baseVisualScale.x = Mathf.Max(0.01f, Mathf.Abs(_baseVisualScale.x));
            _baseVisualScale.y = Mathf.Max(0.01f, Mathf.Abs(_baseVisualScale.y));
            _baseVisualLocalPos = target.localPosition;
            _hasCapturedVisualBase = true;
        }

        public void SetGroundLayers(LayerMask mask)
        {
            groundLayers = mask;
        }

        public void SetCombatMovementLock(bool locked)
        {
            _movementLockedByCombat = locked;
            if (locked) _isGliding = false;
        }

        public void SetAirSuspendLock(bool locked)
        {
            _airSuspendLocked = locked;
            if (locked)
            {
                _isGliding = false;
                if (_isDashing) EndDash();
                if (_rb != null)
                {
                    _rb.gravityScale = 0f;
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.25f, 0f);
                }
            }
            else if (_rb != null && !_movementLockedByBind && !_isDashing && !_isMantling && !_isRestingOnBench)
            {
                _rb.gravityScale = _baseGravityScale;
            }
        }

        public void ApplyForwardLunge(int dir, float speed, float duration)
        {
            if (_rb == null) return;
            int d = dir >= 0 ? 1 : -1;
            SetFacing(d);
            _isGliding = false;
            _lungeTimer = Mathf.Max(0.04f, duration);
            _lungeVelocityX = d * Mathf.Abs(speed);
            _rb.velocity = new Vector2(_lungeVelocityX, _isGrounded ? _rb.velocity.y : Mathf.Max(_rb.velocity.y, 0.8f));
        }

        public void CancelDashForAttack()
        {
            if (!_isDashing) return;
            _isDashing = false;
            _dashIsAir = false;
            _dashIsDownward = false;
            if (_rb != null) _rb.gravityScale = _baseGravityScale;
        }

        public void SetBindHealLock(bool locked)
        {
            _movementLockedByBind = locked;
            if (locked)
            {
                _isGliding = false;
                _lungeTimer = 0f;
                if (_isDashing) EndDash();
                if (_rb != null)
                {
                    if (!_isGrounded)
                    {
                        // Silksong signature Mid-Air Bind: suspend cleanly in mid-air while weaving Silk!
                        _rb.gravityScale = 0f;
                        _rb.velocity = Vector2.zero;
                        _peakAirFallSpeed = 0f;
                    }
                    else
                    {
                        _rb.velocity = new Vector2(0f, _rb.velocity.y);
                    }
                }
            }
            else if (_rb != null && !_isDashing && !_isMantling && !_isRestingOnBench && !_airSuspendLocked)
            {
                _rb.gravityScale = _baseGravityScale;
            }
        }

        private void RestorePostMantlePhysics()
        {
            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
                if (_ignoredMantleCollider != null)
                {
                    Physics2D.IgnoreCollision(_bodyCollider, _ignoredMantleCollider, false);
                }
            }
            _ignoredMantleCollider = null;
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.interpolation = _baseInterpolation;
                _rb.gravityScale = _baseGravityScale;
                _rb.velocity = Vector2.zero;
            }
            Physics2D.SyncTransforms();
        }

        public void CancelMantleIfActive()
        {
            if (_mantleCoroutine != null)
            {
                StopCoroutine(_mantleCoroutine);
                _mantleCoroutine = null;
            }
            if (_isMantling)
            {
                _isMantling = false;
                RestorePostMantlePhysics();
            }
            else if (_bodyCollider != null && _ignoredMantleCollider != null)
            {
                Physics2D.IgnoreCollision(_bodyCollider, _ignoredMantleCollider, false);
                _ignoredMantleCollider = null;
            }
        }

        private void ClearDropThroughCollision()
        {
            if (_bodyCollider != null && _ignoredDropCollider != null)
            {
                Physics2D.IgnoreCollision(_bodyCollider, _ignoredDropCollider, false);
            }
            _ignoredDropCollider = null;
        }

        private void OnDisable()
        {
            if (_bodyCollider != null && _ignoredMantleCollider != null)
            {
                Physics2D.IgnoreCollision(_bodyCollider, _ignoredMantleCollider, false);
                _ignoredMantleCollider = null;
            }
            ClearDropThroughCollision();
        }

        public void SetDead(bool dead)
        {
            _isDead = dead;
            _isRestingOnBench = false;
            _movementLockedByBind = false;
            _airSuspendLocked = false;
            _isGliding = false;
            _lungeTimer = 0f;
            _turnaroundTimer = 0f;
            _skidPivotTimer = 0f;
            _skidStopTimer = 0f;
            _benchApproachAnimSpeed = 0f;
            if (dead) CancelMantleIfActive();
            if (dead && _rb != null)
            {
                _isDashing = false;
                _dashIsAir = false;
                _dashIsDownward = false;
                _heroLandingTimer = 0f;
                _rb.gravityScale = _baseGravityScale;
                _rb.velocity = new Vector2(0f, _rb.velocity.y);
            }
        }

        public void SetBenchApproach(float animWalkSpeed)
        {
            _benchApproachAnimSpeed = animWalkSpeed;
            _isDashing = false;
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
            }
        }

        public void SetRestingOnBench(bool resting, Vector3 benchSeatPosition, int facingDir)
        {
            _isRestingOnBench = resting;
            _movementLockedByBind = false;
            CancelMantleIfActive();
            _benchApproachAnimSpeed = 0f;
            _isDashing = false;
            _dashIsAir = false;
            _dashIsDownward = false;
            _dashTimer = 0f;
            _knockbackLockTimer = 0f;
            _wallJumpLockTimer = 0f;
            _landingTimer = 0f;
            _heroLandingTimer = 0f;
            _jumpedSinceGrounded = false;
            _wasMovingOnGround = false;
            _movementLockedByCombat = false;

            if (resting)
            {
                _isGrounded = true;
                _hasUsedDoubleJump = false;
                _hasUsedAirDash = false;
                transform.position = benchSeatPosition;
                SetFacing(facingDir);
                if (_rb != null)
                {
                    _rb.gravityScale = 0f;
                    _rb.velocity = Vector2.zero;
                }
            }
            else if (_rb != null)
            {
                _rb.gravityScale = _baseGravityScale;
                _rb.velocity = Vector2.zero;
            }
        }

        public void ResetMovementState(Vector3 spawnPos)
        {
            _isDead = false;
            _isRestingOnBench = false;
            _movementLockedByBind = false;
            _airSuspendLocked = false;
            _isGliding = false;
            _lungeTimer = 0f;
            _turnaroundTimer = 0f;
            _skidPivotTimer = 0f;
            _skidStopTimer = 0f;
            CancelMantleIfActive();
            _ledgeCooldownTimer = 0f;
            _benchApproachAnimSpeed = 0f;
            _isDashing = false;
            _dashIsAir = false;
            _dashIsDownward = false;
            _dashTimer = 0f;
            _knockbackLockTimer = 0f;
            _wallJumpLockTimer = 0f;
            _landingTimer = 0f;
            _heroLandingTimer = 0f;
            _jumpedSinceGrounded = false;
            _wasMovingOnGround = false;
            _hasUsedDoubleJump = false;
            _hasUsedAirDash = false;
            _movementLockedByCombat = false;
            transform.position = spawnPos;
            _lastSafeGroundPosition = spawnPos;
            if (_rb != null)
            {
                _rb.gravityScale = _baseGravityScale;
                _rb.velocity = Vector2.zero;
            }
        }

        private void Update()
        {
            _visualJuiceTime += Time.deltaTime;
            if (_isDead || _isRestingOnBench || _isMantling || _movementLockedByBind || _benchApproachAnimSpeed > 0.01f || inputHandler == null)
            {
                _isGliding = false;
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            if (_coyoteTimer > 0f) _coyoteTimer -= dt;
            if (_wallJumpLockTimer > 0f) _wallJumpLockTimer -= dt;
            if (_knockbackLockTimer > 0f) _knockbackLockTimer -= dt;
            if (_landingTimer > 0f) _landingTimer -= dt;
            if (_heroLandingTimer > 0f) _heroLandingTimer -= dt;
            if (_turnaroundTimer > 0f) _turnaroundTimer -= dt;
            if (_skidPivotTimer > 0f) _skidPivotTimer -= dt;
            if (_skidStopTimer > 0f) _skidStopTimer -= dt;
            if (_lungeTimer > 0f) _lungeTimer -= dt;

            // Detect Silksong Run Start (sharp forward-dipped launch from idle into run/sprint)
            bool movingNow = _isGrounded && !_isDashing && !_movementLockedByCombat && Mathf.Abs(inputHandler.Horizontal) > 0.25f;
            if (movingNow && !_wasMovingOnGround && inputHandler.SprintHeld)
            {
                if (OnRunStarted != null) OnRunStarted();
            }

            // Detect Sprint Pivot (00:01:31) & Sprint Skid Stop (00:01:42) on ground!
            if (_isGrounded && !_isDashing && !_movementLockedByCombat && _knockbackLockTimer <= 0f && _rb != null)
            {
                float vx = _rb.velocity.x;
                float inpX = inputHandler.Horizontal;
                if (Mathf.Abs(vx) >= 6.8f && Mathf.Abs(inpX) > 0.25f && (inpX * vx) < -0.25f)
                {
                    if (_skidPivotTimer <= 0f)
                    {
                        _skidLeanDir = vx >= 0f ? 1 : -1;
                        ChoralCodeVFX.SpawnChargeDust(transform.position, _skidLeanDir, true);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(_standingOnStone);
                    }
                    _skidPivotTimer = 0.16f;
                }
                else if (_wasMovingOnGround && Mathf.Abs(vx) >= 7.6f && Mathf.Abs(inpX) < 0.08f && _skidStopTimer <= 0f)
                {
                    _skidLeanDir = vx >= 0f ? 1 : -1;
                    _skidStopTimer = 0.15f;
                    ChoralCodeVFX.SpawnChargeDust(transform.position, _skidLeanDir, true);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(_standingOnStone);
                }
            }
            _wasMovingOnGround = movingNow;

            if (_isDashing)
            {
                _isGliding = false;
                _dashTimer -= dt;
                if (_dashTimer <= 0f)
                {
                    EndDash();
                }
                return;
            }

            // Facing update + snappy 180-degree Turnaround compression (00:00:34)
            if (_knockbackLockTimer <= 0f && _wallJumpLockTimer <= 0f && !_movementLockedByCombat && _lungeTimer <= 0f)
            {
                int prevFacing = _facingDirection;
                if (inputHandler.Horizontal > 0.05f) SetFacing(1);
                else if (inputHandler.Horizontal < -0.05f) SetFacing(-1);
                if (_isGrounded && _facingDirection != prevFacing)
                {
                    _turnaroundTimer = 0.095f;
                }
            }

            // Dash input (Ground Dash or Air Dash depending on unlocked Skills)
            if (inputHandler.DashPressed && _knockbackLockTimer <= 0f && !_airSuspendLocked)
            {
                TryPerformDash();
            }

            // Jump, Wall Jump, or Double Jump
            if (inputHandler.HasBufferedJump && _knockbackLockTimer <= 0f && !_airSuspendLocked)
            {
                TryPerformJump();
            }

            // Variable Jump Cut
            if (inputHandler.JumpReleasedThisFrame && _rb.velocity.y > 0.5f)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * jumpCutMultiplier);
            }

            // Drifter's Cloak Glide (00:03:36): hold Space/Z while descending in mid-air!
            bool canGlide = !_isGrounded && !_isDashing && !_isWallSliding && !_isMantling
                && !_movementLockedByCombat && !_airSuspendLocked && _knockbackLockTimer <= 0f
                && inputHandler.JumpHeld && !inputHandler.HasBufferedJump
                && _rb != null && _rb.velocity.y < -0.55f;
            if (canGlide)
            {
                if (!_isGliding)
                {
                    _isGliding = true;
                    _glideVfxTimer = 0.02f;
                    _peakAirFallSpeed = 0f;
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnMovementStreaks(transform.position + Vector3.up * 0.4f, Vector2.up, 2, 0.45f);
                    }
                }
                _peakAirFallSpeed = 0f;
                _glideVfxTimer -= dt;
                if (_glideVfxTimer <= 0f)
                {
                    _glideVfxTimer = 0.16f;
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnMovementStreaks(transform.position + Vector3.up * 0.25f, Vector2.up, 1, 0.42f);
                    }
                }
            }
            else
            {
                _isGliding = false;
            }

            // Footstep SFX cadence (surface-aware: muffled moss vs crisp bare carved stone)
            if (_isGrounded && Mathf.Abs(_rb.velocity.x) > 1.2f && !_isDashing)
            {
                _footstepTimer -= dt * (IsRunning ? 1.35f : 1.0f);
                if (_footstepTimer <= 0f)
                {
                    _footstepTimer = footstepInterval;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(_standingOnStone);
                }
            }
            else
            {
                _footstepTimer = 0.08f;
            }

            // Thin pale-white air/wind streaks trailing fast downward drops
            if (!_isGrounded && _rb.velocity.y < -11.5f)
            {
                _fallStreakTimer -= dt;
                if (_fallStreakTimer <= 0f)
                {
                    _fallStreakTimer = 0.075f;
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.SpawnMovementStreaks(transform.position + Vector3.up * 0.9f, Vector2.down, 2, 0.36f);
                    }
                }
            }
            else
            {
                _fallStreakTimer = 0.02f;
            }
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            if (_ledgeCooldownTimer > 0f) _ledgeCooldownTimer -= Time.fixedDeltaTime;
            if (_isMantling) return;
            if (_isRestingOnBench || _benchApproachAnimSpeed > 0.01f)
            {
                _rb.velocity = Vector2.zero;
                _isGrounded = true;
                return;
            }
            if (_isDead)
            {
                _rb.velocity = new Vector2(0f, Mathf.Max(_rb.velocity.y, -maxFallSpeed));
                return;
            }

            UpdateEnvironmentSensors();

            if (_movementLockedByBind)
            {
                if (!_isGrounded)
                {
                    // Mid-Air Bind: stay cleanly suspended in silk threads until Heal Burst completes!
                    _rb.gravityScale = 0f;
                    _rb.velocity = Vector2.Lerp(_rb.velocity, Vector2.zero, Mathf.Clamp01(Time.fixedDeltaTime * 18f));
                }
                else
                {
                    float brakeX = Mathf.MoveTowards(_rb.velocity.x, 0f, groundDeceleration * Time.fixedDeltaTime);
                    _rb.velocity = new Vector2(brakeX, _rb.velocity.y);
                    ApplyVerticalPhysics();
                }
                return;
            }

            if (_externalVelocityActive)
            {
                _rb.gravityScale = 0f;
                _rb.velocity = _externalVelocity;
                return;
            }

            if (_airSuspendLocked)
            {
                _rb.gravityScale = 0f;
                float dampX = Mathf.MoveTowards(_rb.velocity.x, 0f, 22f * Time.fixedDeltaTime);
                _rb.velocity = new Vector2(dampX, 0f);
                return;
            }

            if (_isDashing)
            {
                _rb.gravityScale = 0f;
                if (_dashIsDownward)
                {
                    if (_isGrounded)
                    {
                        EndDash();
                        _heroLandingTimer = 0.24f;
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
                        if (VFXManager.Instance != null) VFXManager.Instance.SpawnLandingDust(transform.position, true, !_standingOnStone);
                        GameEvents.RaiseCameraShake(0.055f, 0.08f);
                        if (OnHeroLanded != null) OnHeroLanded();
                        return;
                    }
                    _rb.velocity = new Vector2(_facingDirection * _dashSpeed * 0.45f, -_dashSpeed * 1.12f);
                }
                else
                {
                    _rb.velocity = new Vector2(_facingDirection * _dashSpeed, 0f);
                }
                return;
            }

            if (_knockbackLockTimer <= 0f)
            {
                ApplyHorizontalMovement();
            }

            ApplyVerticalPhysics();
            TryDetectAndStartLedgeMantle();
        }

        private static readonly float[] LedgeProbeHeights = new float[] { 1.76f, 1.44f, 1.12f, 0.80f, 0.44f };

        private void TryDetectAndStartLedgeMantle()
        {
            if (_isGrounded || _isDashing || _isMantling || _movementLockedByBind || _ledgeCooldownTimer > 0f || _knockbackLockTimer > 0f) return;
            if (CaveTransitionManager.Instance != null && CaveTransitionManager.Instance.IsTransitioning) return;
            if (_rb.velocity.y > 6.2f) return;
            if (inputHandler != null && (inputHandler.Horizontal * _facingDirection) < -0.05f) return;

            int dir = _facingDirection >= 0 ? 1 : -1;
            // Only grab solid Ground ledges — never intercept OneWayPlatforms with horizontal mantle IgnoreCollision!
            int solidMask = 1 << GameConstants.LayerGround;
            Vector2 originBase = (Vector2)transform.position;
            Vector2 forward = new Vector2(dir, 0f);

            // Top-down torso/shoulder/waist sweep ensures both tall ledges and thin floating platforms (0.55u) are caught reliably.
            RaycastHit2D wallHit = default(RaycastHit2D);
            for (int i = 0; i < LedgeProbeHeights.Length; i++)
            {
                RaycastHit2D hit = Physics2D.Raycast(originBase + new Vector2(0f, LedgeProbeHeights[i]), forward, 0.62f, solidMask);
                if (hit.collider != null && !hit.collider.isTrigger && hit.distance > 0.01f && (hit.normal.x * dir) < -0.35f)
                {
                    wallHit = hit;
                    break;
                }
            }
            if (wallHit.collider == null) return;

            // Anchor downward top probe near this collider's upper bound so higher overhead bridges never block lower ledges.
            float maxTopProbeY = Mathf.Min(transform.position.y + 2.26f, wallHit.collider.bounds.max.y + 0.36f);
            float castDist = maxTopProbeY - (transform.position.y + 0.32f);
            if (castDist < 0.25f) return;

            Vector2 downOrigin = new Vector2(wallHit.point.x + dir * 0.20f, maxTopProbeY);
            RaycastHit2D topHit = Physics2D.Raycast(downOrigin, Vector2.down, castDist, solidMask);
            if (topHit.collider == null || topHit.collider.isTrigger || topHit.distance <= 0.04f) return;

            float dy = topHit.point.y - transform.position.y;
            if (dy < 0.45f || dy > 2.02f) return;

            if (Physics2D.Raycast(new Vector2(transform.position.x, topHit.point.y + 0.32f), forward, 0.62f, solidMask).collider != null) return;

            Vector2 standPos = new Vector2(wallHit.point.x + dir * 0.44f, topHit.point.y + 0.01f);
            Collider2D ceilingCheck = Physics2D.OverlapBox(standPos + new Vector2(0f, 1.03f), new Vector2(0.56f, 1.84f), 0f, solidMask);
            if (ceilingCheck != null && !ceilingCheck.isTrigger) return;

            _isMantling = true;
            _isWallSliding = false;
            _mantleCoroutine = StartCoroutine(LedgeMantleRoutine(wallHit.point.x, topHit.point.y, dir, topHit.collider));
        }

        private System.Collections.IEnumerator LedgeMantleRoutine(float wallX, float topY, int dir, Collider2D ledgeCol)
        {
            // Keep _bodyCollider enabled and switch to Kinematic so triggers (AreaZoneTrigger, shrines, pickups)
            // never fire false OnTriggerExit2D camera/banner shifts and Rigidbody2D interpolation stays 1:1 synced.
            _rb.velocity = Vector2.zero;
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.None;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            _ignoredMantleCollider = ledgeCol;
            if (_bodyCollider != null && ledgeCol != null)
            {
                Physics2D.IgnoreCollision(_bodyCollider, ledgeCol, true);
            }
            SetFacing(dir);

            Vector3 startPos = transform.position;
            float catchY = Mathf.Min(topY - 0.50f, Mathf.Max(startPos.y, topY - 1.60f));
            Vector3 catchPos = new Vector3(wallX - dir * 0.34f, catchY, startPos.z);
            Vector3 crestPos = new Vector3(wallX + dir * 0.14f, topY + 0.04f, startPos.z);
            Vector3 landPos = new Vector3(wallX + dir * 0.44f, topY + 0.01f, startPos.z);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayLedgeMantle();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnJumpDust(new Vector3(wallX, topY, 0f));

            // Phase 1 (Frame 0: Ledge Catch glide into hand-grip offset, ~75ms)
            float catchDur = 0.075f;
            float catchElapsed = 0f;
            while (catchElapsed < catchDur)
            {
                catchElapsed += Time.deltaTime;
                float u = Mathf.Clamp01(catchElapsed / catchDur);
                Vector3 nextPos = Vector3.Lerp(startPos, catchPos, u);
                transform.position = nextPos;
                _rb.position = nextPos;
                yield return null;
            }
            transform.position = catchPos;
            _rb.position = catchPos;

            // Phase 2 (Frames 1-2: Push-Up -> Edge Cresting vault arc, ~155ms)
            float climbDur = 0.155f;
            float elapsed = 0f;
            while (elapsed < climbDur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / climbDur);
                float smooth = 1f - Mathf.Pow(1f - u, 2.2f);
                Vector3 a = Vector3.Lerp(catchPos, crestPos, smooth);
                Vector3 b = Vector3.Lerp(crestPos, landPos, smooth);
                Vector3 nextPos = Vector3.Lerp(a, b, smooth);
                transform.position = nextPos;
                _rb.position = nextPos;
                yield return null;
            }

            // Phase 3 (Frames 3-4: Crouch Land & Stand Recovery flush on ledge, ~150ms)
            transform.position = landPos;
            _rb.position = landPos;
            Physics2D.SyncTransforms();

            _isGrounded = true;
            _standingOnStone = DetectStoneSurface(ledgeCol);
            _hasUsedDoubleJump = false;
            _hasUsedAirDash = false;
            _lastSafeGroundPosition = landPos;

            if (VFXManager.Instance != null) VFXManager.Instance.SpawnLandingDust(landPos, false, !_standingOnStone);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayFootstep(_standingOnStone);

            yield return new WaitForSeconds(0.150f);

            RestorePostMantlePhysics();
            _ledgeCooldownTimer = 0.25f;
            _isMantling = false;
            _mantleCoroutine = null;
        }

        private void UpdateEnvironmentSensors()
        {
            _wasGrounded = _isGrounded;
            Vector2 footPos = (Vector2)transform.position + groundCheckOffset;
            Collider2D groundHit = Physics2D.OverlapBox(footPos, groundCheckSize, 0f, groundLayers);
            _isGrounded = groundHit != null && _rb.velocity.y <= 0.6f;

            if (_isGrounded)
            {
                _coyoteTimer = coyoteTimeDuration;
                _hasUsedDoubleJump = false;
                _hasUsedAirDash = false;
                _jumpedSinceGrounded = false;
                SurfaceMaterialZone surfZone = groundHit.GetComponent<SurfaceMaterialZone>();
                _standingOnIce = surfZone != null && surfZone.IsSlipperyIce;
                _iceAccelMult = surfZone != null ? surfZone.AccelerationMultiplier : 1f;
                _iceDecelMult = surfZone != null ? surfZone.DecelerationMultiplier : 1f;
                _standingOnStone = surfZone != null ? surfZone.IsBareStoneAudio : DetectStoneSurface(groundHit);
                _standingOneWayCollider = groundHit.gameObject.layer == GameConstants.LayerOneWayPlatform ? groundHit : null;

                // Record safe platform position away from hazards
                if (groundHit.gameObject.layer == GameConstants.LayerGround)
                {
                    _lastSafeGroundPosition = transform.position;
                }

                if (!_wasGrounded)
                {
                    if (_peakAirFallSpeed > 3.5f || _dashIsDownward)
                    {
                        bool hardLanding = _peakAirFallSpeed > 10.5f || _dashIsDownward;
                        bool runningOnLand = !hardLanding && inputHandler != null && Mathf.Abs(inputHandler.Horizontal) > 0.25f && inputHandler.SprintHeld;
                        _landingTimer = landingDuration;
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayLand();
                        if (VFXManager.Instance != null) VFXManager.Instance.SpawnLandingDust(transform.position, hardLanding, !_standingOnStone);
                        if (hardLanding)
                        {
                            _heroLandingTimer = 0.24f;
                            GameEvents.RaiseCameraShake(0.05f, 0.075f);
                            if (OnHeroLanded != null) OnHeroLanded();
                        }
                        else if (runningOnLand)
                        {
                            // Silksong Frame Reuse: dipped run-start doubles as dipped run-landing!
                            if (OnRunStarted != null) OnRunStarted();
                        }
                        else
                        {
                            if (OnLanded != null) OnLanded();
                        }
                    }
                    _peakAirFallSpeed = 0f;
                }
            }
            else
            {
                // Detect sprinting directly off a platform ledge -> trigger Silksong Ledge Somersault!
                if (_wasGrounded && !_isDashing && !_jumpedSinceGrounded && Mathf.Abs(_rb.velocity.x) > 5.2f && _rb.velocity.y <= 0.6f)
                {
                    if (OnLedgeSomersault != null) OnLedgeSomersault();
                }
                _standingOnIce = false;
                _standingOneWayCollider = null;
                if (_rb.velocity.y < 0f)
                {
                    _peakAirFallSpeed = Mathf.Max(_peakAirFallSpeed, -_rb.velocity.y);
                }
            }

            // Wall sensing on solid Ground layer only
            int solidMask = 1 << GameConstants.LayerGround;
            Vector2 wallOrigin = (Vector2)transform.position + new Vector2(0f, wallCheckHeight);
            RaycastHit2D rightHit = Physics2D.Raycast(wallOrigin, Vector2.right, wallCheckDistance, solidMask);
            RaycastHit2D leftHit = Physics2D.Raycast(wallOrigin, Vector2.left, wallCheckDistance, solidMask);

            if (rightHit.collider != null)
            {
                _isTouchingWall = true;
                _wallDirection = 1;
            }
            else if (leftHit.collider != null)
            {
                _isTouchingWall = true;
                _wallDirection = -1;
            }
            else
            {
                _isTouchingWall = false;
                _wallDirection = 0;
            }

            bool pushingWall = inputHandler != null && Mathf.Sign(inputHandler.Horizontal) == _wallDirection && Mathf.Abs(inputHandler.Horizontal) > 0.1f;
            _isWallSliding = !_isGrounded && _isTouchingWall && pushingWall && _rb.velocity.y < 0f;
        }

        private void ApplyHorizontalMovement()
        {
            if (inputHandler == null || _wallJumpLockTimer > 0f) return;

            if (_lungeTimer > 0f)
            {
                float nextLungeX = Mathf.MoveTowards(_rb.velocity.x, _lungeVelocityX * 0.45f, 38f * Time.fixedDeltaTime);
                _rb.velocity = new Vector2(nextLungeX, _rb.velocity.y);
                return;
            }

            float moveInput = _movementLockedByCombat ? 0f : inputHandler.Horizontal;
            float walkSpd = stats != null ? stats.WalkSpeed : 6.2f;
            float runSpd = stats != null ? stats.RunSpeed : 9.4f;
            float targetSpeed = (inputHandler.SprintHeld ? runSpd : walkSpd) * moveInput;

            float airMult = stats != null ? stats.AirControlMultiplier : 0.85f;
            if (_isGliding) airMult *= 1.18f;
            float accel;
            if (_isGrounded)
            {
                bool pushing = Mathf.Abs(moveInput) > 0.05f;
                accel = pushing ? groundAcceleration : groundDeceleration;
                if (_skidPivotTimer > 0f) accel *= 0.78f;
                else if (_skidStopTimer > 0f && !pushing) accel *= 0.62f;
                if (_standingOnIce)
                {
                    accel *= pushing ? _iceAccelMult : _iceDecelMult;
                }
            }
            else
            {
                accel = (Mathf.Abs(moveInput) > 0.05f ? airAcceleration : airDeceleration) * airMult;
            }

            float nextX = Mathf.MoveTowards(_rb.velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
            _rb.velocity = new Vector2(nextX, _rb.velocity.y);
        }

        private void ApplyVerticalPhysics()
        {
            if (_isWallSliding)
            {
                _rb.gravityScale = _baseGravityScale * 0.65f;
                if (_rb.velocity.y < -wallSlideMaxSpeed)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x, -wallSlideMaxSpeed);
                }
                return;
            }

            if (_isGliding)
            {
                _rb.gravityScale = _baseGravityScale * 0.24f;
                float targetVy = -glideMaxFallSpeed;
                if (_rb.velocity.y < targetVy)
                {
                    float nextY = Mathf.Lerp(_rb.velocity.y, targetVy, Mathf.Clamp01(Time.fixedDeltaTime * 14f));
                    _rb.velocity = new Vector2(_rb.velocity.x, nextY);
                }
                return;
            }

            _rb.gravityScale = _rb.velocity.y < -0.1f ? _baseGravityScale * fallGravityMultiplier : _baseGravityScale;
            if (_rb.velocity.y < -maxFallSpeed)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, -maxFallSpeed);
            }
        }

        private void TryPerformJump()
        {
            float jumpVel = stats != null ? stats.JumpForce : 15.2f;

            // 0. Drop cleanly down through One-Way Platforms when holding Down + Jump
            if (_isGrounded && _standingOneWayCollider != null && inputHandler.Vertical < -0.55f)
            {
                inputHandler.ConsumeJumpBuffer();
                _coyoteTimer = 0f;
                _isGrounded = false;
                StartCoroutine(DropThroughOneWayRoutine(_standingOneWayCollider));
                return;
            }

            // 1. Grounded / Coyote Jump
            if (_coyoteTimer > 0f)
            {
                inputHandler.ConsumeJumpBuffer();
                _coyoteTimer = 0f;
                _isGrounded = false;
                _jumpedSinceGrounded = true;
                _rb.velocity = new Vector2(_rb.velocity.x, jumpVel);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
                if (VFXManager.Instance != null) VFXManager.Instance.SpawnJumpDust(transform.position);
                if (OnJumped != null) OnJumped();
                return;
            }

            // 2. Wall Jump
            if (_isWallSliding || (!_isGrounded && _isTouchingWall))
            {
                inputHandler.ConsumeJumpBuffer();
                int kickDir = -_wallDirection;
                if (kickDir == 0) kickDir = -_facingDirection;
                SetFacing(kickDir);
                _wallJumpLockTimer = 0.14f;
                _rb.velocity = new Vector2(kickDir * wallJumpForce.x, wallJumpForce.y);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
                if (VFXManager.Instance != null) VFXManager.Instance.SpawnJumpDust(transform.position + new Vector3(_wallDirection * 0.35f, 0.8f, 0f));
                if (OnWallJumped != null) OnWallJumped();
                if (OnJumped != null) OnJumped();
                return;
            }

            // 3. Double Jump (ONLY if DoubleJump Skill is unlocked!)
            if (!_hasUsedDoubleJump && skillManager != null && skillManager.TryConsumeSkill(SkillType.DoubleJump, stats))
            {
                inputHandler.ConsumeJumpBuffer();
                _hasUsedDoubleJump = true;
                _rb.velocity = new Vector2(_rb.velocity.x, jumpVel * 0.94f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayDoubleJump();
                if (VFXManager.Instance != null) VFXManager.Instance.SpawnDoubleJumpRing(transform.position);
                if (OnDoubleJumped != null) OnDoubleJumped();
            }
        }

        private void TryPerformDash()
        {
            if (skillManager == null) return;

            SkillType neededSkill = _isGrounded ? SkillType.Dash : SkillType.AirDash;
            if (!_isGrounded)
            {
                if (_hasUsedAirDash) return;
                // Allow ground Dash skill in air ONLY if AirDash skill is unlocked
                if (!skillManager.CanUseSkill(SkillType.AirDash, stats)) return;
            }
            else
            {
                if (!skillManager.CanUseSkill(SkillType.Dash, stats)) return;
            }

            if (!skillManager.TryConsumeSkill(neededSkill, stats)) return;

            SkillData data = skillManager.GetSkillData(neededSkill);
            _dashSpeed = data != null && data.powerValue > 1f ? data.powerValue : 17.5f;
            _dashTimer = data != null && data.duration > 0.05f ? data.duration : 0.20f;
            _isDashing = true;
            _dashIsAir = !_isGrounded;
            _dashIsDownward = !_isGrounded && inputHandler.Vertical < -0.55f;
            if (!_isGrounded) _hasUsedAirDash = true;

            if (Mathf.Abs(inputHandler.Horizontal) > 0.1f)
            {
                SetFacing(inputHandler.Horizontal > 0f ? 1 : -1);
            }

            _rb.gravityScale = 0f;
            _rb.velocity = _dashIsDownward
                ? new Vector2(_facingDirection * _dashSpeed * 0.45f, -_dashSpeed * 1.12f)
                : new Vector2(_facingDirection * _dashSpeed, 0f);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayDash();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnDashEffect(transform.position + Vector3.up * 0.9f, _facingDirection, _isGrounded, _isGrounded && !_standingOnStone);
            if (OnDashed != null) OnDashed();
        }

        public void PerformRestJumpOff()
        {
            if (_rb == null) return;
            _isGrounded = false;
            _jumpedSinceGrounded = true;
            _coyoteTimer = 0f;
            _rb.gravityScale = _baseGravityScale;
            _rb.velocity = new Vector2(_facingDirection * 4.6f, 8.2f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            if (VFXManager.Instance != null) VFXManager.Instance.SpawnJumpDust(transform.position);
        }

        private static bool DetectStoneSurface(Collider2D hit)
        {
            if (hit == null) return false;
            if (hit.gameObject.layer == GameConstants.LayerOneWayPlatform) return true;
            string n = hit.gameObject.name;
            if (string.IsNullOrEmpty(n)) return false;
            return n.IndexOf("Stone", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Bridge", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Climb", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("ArenaDeck", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("AirDashLedge", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Summit", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("SecretEnhanced", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.StartsWith("Z4_")
                || n.StartsWith("Z5_");
        }

        private void EndDash()
        {
            bool wasDownward = _dashIsDownward;
            _isDashing = false;
            _dashIsAir = false;
            _dashIsDownward = false;
            _rb.gravityScale = _baseGravityScale;
            if (wasDownward)
            {
                _rb.velocity = new Vector2(_facingDirection * 2.2f, _isGrounded ? 0f : -12f);
            }
            else
            {
                _rb.velocity = new Vector2(_facingDirection * (stats != null ? stats.WalkSpeed : 6f), 0f);
            }
        }

        private System.Collections.IEnumerator DropThroughOneWayRoutine(Collider2D oneWayCol)
        {
            if (_bodyCollider == null || oneWayCol == null) yield break;
            ClearDropThroughCollision();
            _ignoredDropCollider = oneWayCol;
            Physics2D.IgnoreCollision(_bodyCollider, oneWayCol, true);
            _rb.velocity = new Vector2(_rb.velocity.x, -4.2f);
            float elapsed = 0f;
            float platBottomY = oneWayCol.bounds.min.y;
            while (elapsed < 0.36f)
            {
                elapsed += Time.fixedDeltaTime;
                if (elapsed >= 0.16f && _bodyCollider != null && oneWayCol != null)
                {
                    if (_bodyCollider.bounds.min.y < platBottomY - 0.12f && !_bodyCollider.bounds.Intersects(oneWayCol.bounds))
                    {
                        break;
                    }
                }
                yield return new WaitForFixedUpdate();
            }
            if (_bodyCollider != null && oneWayCol != null)
            {
                Physics2D.IgnoreCollision(_bodyCollider, oneWayCol, false);
            }
            if (_ignoredDropCollider == oneWayCol)
            {
                _ignoredDropCollider = null;
            }
        }

        public void ApplyPogoBounce()
        {
            if (_rb == null) return;
            float jumpVel = stats != null ? stats.JumpForce : 15.2f;
            _rb.velocity = new Vector2(_rb.velocity.x, jumpVel * 0.85f);
            _hasUsedDoubleJump = false;
            _hasUsedAirDash = false;
        }

        public void SetExternalVelocityOverride(bool enabled, Vector2 velocity)
        {
            _externalVelocityActive = enabled;
            _externalVelocity = velocity;
            if (enabled)
            {
                _isGliding = false;
                _isDashing = false;
                _dashIsAir = false;
                _dashIsDownward = false;
                if (_rb != null)
                {
                    _rb.gravityScale = 0f;
                    _rb.velocity = velocity;
                }
            }
            else if (_rb != null)
            {
                _rb.gravityScale = _baseGravityScale;
            }
        }

        public void ApplyTraversalVault(float upwardVelocity, bool refreshAirDash)
        {
            if (_rb == null) return;
            _externalVelocityActive = false;
            _rb.gravityScale = _baseGravityScale;
            _rb.velocity = new Vector2(_rb.velocity.x * 0.45f, upwardVelocity);
            if (refreshAirDash) _hasUsedAirDash = false;
        }

        public void ApplyKnockback(Vector2 direction, float force, float lockDuration)
        {
            if (_rb == null) return;
            CancelMantleIfActive();
            _isDashing = false;
            _dashIsAir = false;
            _dashIsDownward = false;
            _heroLandingTimer = 0f;
            _rb.gravityScale = _baseGravityScale;
            _knockbackLockTimer = Mathf.Max(_knockbackLockTimer, lockDuration);
            Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : new Vector2(-_facingDirection, 0.4f).normalized;
            _rb.velocity = new Vector2(dir.x * force, Mathf.Max(dir.y * force, 4.5f));
        }

        public void SetFacing(int dir)
        {
            if (dir == 0) return;
            _facingDirection = dir > 0 ? 1 : -1;
            CaptureVisualBaseIfNeeded();
            Transform target = visualRoot != null ? visualRoot : transform;
            Vector3 s = target.localScale;
            s.x = Mathf.Abs(s.x) * _facingDirection;
            target.localScale = s;
        }

        private void LateUpdate()
        {
            ApplyLocomotionVisualJuice();
        }

        private void ApplyLocomotionVisualJuice()
        {
            if (visualRoot == null || _isDead || _isRestingOnBench || _isMantling) return;
            CaptureVisualBaseIfNeeded();

            float sx = _baseVisualScale.x;
            float sy = _baseVisualScale.y;
            float rotZ = 0f;
            Vector3 offset = Vector3.zero;

            if (_movementLockedByBind && !_isGrounded)
            {
                // Mid-Air Bind buoyant silk float + pulsing aura compression
                offset.y += Mathf.Sin(_visualJuiceTime * 11f) * 0.045f;
                float pulse = 1f + Mathf.Sin(_visualJuiceTime * 18f) * 0.03f;
                sx *= pulse;
                sy *= (2f - pulse);
            }
            else if (_isGliding)
            {
                // Subtle complementary wind sway layered cleanly over dedicated hand-drawn Glide sprites
                float flutter = Mathf.Sin(_visualJuiceTime * 12f) * 0.015f;
                sx *= (1.03f + flutter);
                sy *= (0.98f - flutter * 0.4f);
                float vx = _rb != null ? _rb.velocity.x : 0f;
                rotZ = Mathf.Clamp(-vx * 1.15f, -7.0f, 7.0f);
            }
            else if (_skidPivotTimer > 0f || _skidStopTimer > 0f)
            {
                // Subtle complementary lean layered cleanly over dedicated hand-drawn Skid/Pivot sprites
                float k = _skidPivotTimer > 0f ? (_skidPivotTimer / 0.16f) : (_skidStopTimer / 0.15f);
                rotZ = _skidLeanDir * 4.0f * k;
                sx *= (1f + 0.03f * k);
                sy *= (1f - 0.03f * k);
            }
            else if (_turnaroundTimer > 0f)
            {
                // Snappy 180-degree horizontal turnaround flip-through compression!
                float u = 1f - Mathf.Clamp01(_turnaroundTimer / 0.095f);
                float zip = Mathf.Lerp(0.46f, 1.0f, u);
                sx *= zip;
                sy *= (1.0f + (1.0f - zip) * 0.10f);
            }
            else if (_lungeTimer > 0f)
            {
                // Aggressive forward lunge tilt for Sprint Attack / Charge Attack / Cross Stitch Counter
                rotZ = -_facingDirection * 7.5f;
                sx *= 1.08f;
                sy *= 0.94f;
            }

            visualRoot.localScale = new Vector3(_facingDirection * sx, sy, _baseVisualScale.z);
            visualRoot.localPosition = _baseVisualLocalPos + offset;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);
            Gizmos.color = Color.cyan;
            Vector3 wOrigin = transform.position + new Vector3(0f, wallCheckHeight, 0f);
            Gizmos.DrawLine(wOrigin, wOrigin + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wOrigin, wOrigin + Vector3.left * wallCheckDistance);
        }
    }
}
