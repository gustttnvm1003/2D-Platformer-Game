using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Synchronizes Unity Animator parameters with PlayerMovement, PlayerCombat,
    /// and PlayerController states so animations transition smoothly without conflicting.
    /// </summary>
    public class PlayerAnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerController controller;

        [Header("Graceful Extended Idle")]
        [SerializeField] private float idleFlourishDelay = 3.5f;
        [SerializeField] private float idleRepeatInterval = 4.2f;

        [Header("Bench Rest Searching Look-Around")]
        [SerializeField] private float restLookInitialDelay = 3.6f;
        [SerializeField] private float restLookRepeatInterval = 5.0f;

        private bool _hasValidController;
        private float _idleTimer;
        private float _restAlertTimer;
        private bool _hasPlayedInitialRestLook;
        private int _nextGraceVariant;
        private bool _hasPlayedInitialFlourish;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (controller == null) controller = GetComponent<PlayerController>();

            _hasValidController = animator != null && animator.runtimeAnimatorController != null;
        }

        private void OnEnable()
        {
            if (movement != null)
            {
                movement.OnLanded += HandleLanded;
                movement.OnHeroLanded += HandleHeroLanded;
                movement.OnRunStarted += HandleRunStarted;
                movement.OnLedgeSomersault += HandleLedgeSomersault;
                movement.OnWallJumped += HandleWallJumped;
                movement.OnDoubleJumped += HandleDoubleJumped;
            }
            if (combat != null)
            {
                combat.OnAttackStarted += HandleAttackStarted;
            }
        }

        private void OnDisable()
        {
            if (movement != null)
            {
                movement.OnLanded -= HandleLanded;
                movement.OnHeroLanded -= HandleHeroLanded;
                movement.OnRunStarted -= HandleRunStarted;
                movement.OnLedgeSomersault -= HandleLedgeSomersault;
                movement.OnWallJumped -= HandleWallJumped;
                movement.OnDoubleJumped -= HandleDoubleJumped;
            }
            if (combat != null)
            {
                combat.OnAttackStarted -= HandleAttackStarted;
            }
        }

        private void LateUpdate()
        {
            if (!_hasValidController)
            {
                _hasValidController = animator != null && animator.runtimeAnimatorController != null;
                if (!_hasValidController) return;
            }

            bool dead = controller != null && !controller.IsAlive;
            bool hurt = controller != null && controller.IsStaggered;
            bool sitting = controller != null && controller.IsSitting;
            bool healing = controller != null && controller.IsBindingHeal;
            bool witchBind = controller != null && controller.IsWitchBinding;
            bool dashing = movement != null && movement.IsDashing;
            bool airDashing = movement != null && movement.IsAirDashing;
            bool downwardDashing = movement != null && movement.IsDownwardDashing;
            bool heroLanding = movement != null && movement.IsHeroLanding;
            bool mantling = movement != null && movement.IsMantling;
            bool wallSliding = movement != null && movement.IsWallSliding;
            bool gliding = movement != null && movement.IsGliding;
            bool skidding = movement != null && (movement.IsSkidding || movement.IsPivoting);
            bool attacking = combat != null && combat.IsAttacking;
            bool airAttacking = combat != null && combat.IsAirAttacking;
            bool parrying = combat != null && combat.IsCrossStitchParryWindow;
            bool taunting = combat != null && combat.IsTaunting;
            bool harpooning = combat != null && combat.IsHarpooning;
            bool silksoaring = combat != null && combat.IsSilksoaring;
            bool playingNeedolin = combat != null && combat.IsPlayingNeedolin;
            bool grounded = movement == null || movement.IsGrounded;
            float approachSpd = movement != null ? movement.BenchApproachAnimSpeed : 0f;
            float speed = movement != null ? Mathf.Max(approachSpd, Mathf.Abs(movement.HorizontalVelocity)) : 0f;
            float velY = movement != null ? movement.VerticalVelocity : 0f;
            bool running = movement != null && movement.IsRunning;

            // Prevent locomotion animations from competing during locked combat/dash/hurt/dead/sitting/mantle/bind/parry/taunt/traversal states
            float effectiveSpeed = (dead || hurt || dashing || mantling || healing || attacking || sitting || wallSliding || parrying || taunting || harpooning || silksoaring || playingNeedolin) ? 0f : speed;
            bool isGlideAnim = gliding && !grounded && !dead && !hurt && !dashing && !airDashing && !downwardDashing && !attacking && !healing && !wallSliding && !mantling && !parrying && !harpooning && !silksoaring;
            bool isSkidAnim = skidding && grounded && !dead && !hurt && !dashing && !attacking && !sitting && !mantling && !healing && !heroLanding && !parrying && !taunting && !playingNeedolin;
            bool isParryAnim = parrying && !dead && !hurt && !dashing && !airDashing && !downwardDashing && !attacking && !sitting && !mantling && !healing;
            bool isTauntAnim = taunting && grounded && !dead && !hurt && !dashing && !attacking && !sitting && !mantling && !healing && !parrying && !playingNeedolin;
            bool isHarpoonAnim = harpooning && !dead && !hurt && !sitting && !mantling && !healing;
            bool isSilksoarAnim = silksoaring && !dead && !hurt && !sitting && !mantling && !healing;
            bool isNeedolinAnim = playingNeedolin && grounded && !dead && !hurt && !dashing && !attacking && !sitting && !mantling && !healing && !parrying;

            animator.SetBool(GameConstants.AnimIsDead, dead);
            animator.SetBool(GameConstants.AnimIsHurt, hurt && !dead);
            animator.SetBool(GameConstants.AnimIsSitting, sitting && !dead && !hurt);
            animator.SetBool(GameConstants.AnimIsHealing, healing && !witchBind && !dead && !hurt && !sitting);
            animator.SetBool(GameConstants.AnimIsWitchBind, witchBind && !dead && !hurt && !sitting);
            animator.SetBool(GameConstants.AnimIsMantling, mantling && !dead && !hurt && !sitting);
            animator.SetBool(GameConstants.AnimIsDashing, dashing && !airDashing && !downwardDashing && !dead && !hurt && !sitting && !mantling);
            animator.SetBool(GameConstants.AnimIsAirDashing, airDashing && !dead && !hurt && !sitting && !mantling);
            animator.SetBool(GameConstants.AnimIsDownwardDashing, downwardDashing && !dead && !hurt && !sitting && !mantling);
            animator.SetBool(GameConstants.AnimIsHeroLanding, heroLanding && !dead && !hurt && !dashing && !attacking);
            animator.SetBool(GameConstants.AnimIsWallSliding, wallSliding && !dead && !hurt && !sitting && !dashing && !mantling && !attacking);
            animator.SetBool(GameConstants.AnimIsGliding, isGlideAnim);
            animator.SetBool(GameConstants.AnimIsSkidding, isSkidAnim);
            animator.SetBool(GameConstants.AnimIsParrying, isParryAnim);
            animator.SetBool(GameConstants.AnimIsTaunting, isTauntAnim);
            animator.SetBool(GameConstants.AnimIsHarpooning, isHarpoonAnim);
            animator.SetBool(GameConstants.AnimIsSilksoaring, isSilksoarAnim);
            animator.SetBool(GameConstants.AnimIsPlayingNeedolin, isNeedolinAnim);
            animator.SetBool(GameConstants.AnimIsAttacking, attacking && !dead && !hurt && !sitting && !mantling && !healing);
            animator.SetBool(GameConstants.AnimIsAirAttacking, airAttacking && !dead && !hurt && !sitting && !mantling);
            animator.SetBool(GameConstants.AnimIsGrounded, grounded || sitting || mantling);
            animator.SetBool(GameConstants.AnimIsRunning, running && effectiveSpeed > 0.25f);
            animator.SetFloat(GameConstants.AnimSpeed, effectiveSpeed);
            animator.SetFloat(GameConstants.AnimVelocityY, velY);

            if (sitting && !dead && !hurt)
            {
                bool playingPaused = GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing;
                if (!playingPaused && !animator.IsInTransition(0))
                {
                    AnimatorStateInfo sitState = animator.GetCurrentAnimatorStateInfo(0);
                    if (sitState.IsTag("RestLook") || sitState.IsTag("SitDown"))
                    {
                        _restAlertTimer = 0f;
                    }
                    else
                    {
                        float lookThreshold = _hasPlayedInitialRestLook ? restLookRepeatInterval : restLookInitialDelay;
                        _restAlertTimer += Time.deltaTime;
                        if (_restAlertTimer >= lookThreshold)
                        {
                            _restAlertTimer = 0f;
                            _hasPlayedInitialRestLook = true;
                            animator.SetTrigger(GameConstants.AnimTriggerRestAlert);
                        }
                    }
                }
            }
            else
            {
                _restAlertTimer = 0f;
                _hasPlayedInitialRestLook = false;
                animator.ResetTrigger(GameConstants.AnimTriggerRestAlert);
            }

            bool charging = combat != null && combat.IsChargingAttack;
            UpdateGracefulExtendedIdle(dead, hurt, sitting || mantling || healing || heroLanding, dashing || wallSliding || isGlideAnim || isSkidAnim || isHarpoonAnim || isSilksoarAnim, attacking || charging || isParryAnim || isTauntAnim || isNeedolinAnim, grounded, effectiveSpeed, velY);
        }

        private void UpdateGracefulExtendedIdle(
            bool dead, bool hurt, bool sitting, bool dashing, bool attacking,
            bool grounded, float effectiveSpeed, float velY)
        {
            bool playingPaused = GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing;
            bool trulyIdle = !playingPaused && !dead && !hurt && !sitting && !dashing && !attacking
                && grounded && effectiveSpeed < 0.12f && Mathf.Abs(velY) < 0.15f;

            if (!trulyIdle)
            {
                _idleTimer = 0f;
                _hasPlayedInitialFlourish = false;
                animator.ResetTrigger(GameConstants.AnimTriggerGraceIdle);
                animator.SetInteger(GameConstants.AnimIdleVariant, 0);
                return;
            }

            if (animator.IsInTransition(0)) return;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("GraceIdle"))
            {
                _idleTimer = 0f;
                return;
            }

            float threshold = _hasPlayedInitialFlourish ? idleRepeatInterval : idleFlourishDelay;
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= threshold)
            {
                _idleTimer = 0f;
                _hasPlayedInitialFlourish = true;
                _nextGraceVariant = (_nextGraceVariant % 3) + 1;
                animator.SetInteger(GameConstants.AnimIdleVariant, _nextGraceVariant);
                animator.SetTrigger(GameConstants.AnimTriggerGraceIdle);
            }
        }

        public void TriggerSitDown()
        {
            if (!_hasValidController)
            {
                _hasValidController = animator != null && animator.runtimeAnimatorController != null;
            }
            if (_hasValidController)
            {
                animator.SetTrigger(GameConstants.AnimTriggerSitDown);
            }
        }

        public void TriggerRestJumpOff()
        {
            if (!_hasValidController)
            {
                _hasValidController = animator != null && animator.runtimeAnimatorController != null;
            }
            if (_hasValidController)
            {
                animator.SetTrigger(GameConstants.AnimTriggerRestJumpOff);
            }
        }

        private void HandleLanded()
        {
            if (!_hasValidController) return;
            if (combat != null && combat.IsAttacking) return;
            animator.SetTrigger(GameConstants.AnimTriggerLand);
        }

        private void HandleHeroLanded()
        {
            if (!_hasValidController) return;
            if (combat != null && combat.IsAttacking) return;
            animator.SetBool(GameConstants.AnimIsHeroLanding, true);
        }

        private void HandleRunStarted()
        {
            if (!_hasValidController) return;
            if (combat != null && combat.IsAttacking) return;
            animator.SetTrigger(GameConstants.AnimTriggerRunStart);
        }

        private void HandleLedgeSomersault()
        {
            if (!_hasValidController) return;
            if (combat != null && combat.IsAttacking) return;
            animator.SetTrigger(GameConstants.AnimTriggerLedgeSomersault);
        }

        private void HandleWallJumped()
        {
            if (!_hasValidController) return;
            if (combat != null && combat.IsAttacking) return;
            animator.SetTrigger(GameConstants.AnimTriggerWallJump);
        }

        private void HandleDoubleJumped()
        {
            if (!_hasValidController) return;
            animator.SetTrigger(GameConstants.AnimTriggerDoubleJump);
        }

        private void HandleAttackStarted(int comboStep, bool isAir)
        {
            if (!_hasValidController) return;
            animator.SetInteger(GameConstants.AnimComboStep, comboStep);
            animator.SetBool(GameConstants.AnimIsAirAttacking, isAir);
            animator.SetTrigger(GameConstants.AnimTriggerAttack);
        }
    }
}
