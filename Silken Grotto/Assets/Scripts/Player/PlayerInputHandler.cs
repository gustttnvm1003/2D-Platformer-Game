using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Clean Legacy Input Manager abstraction for Keyboard and Mouse controls.
    /// Centralizes input reading and buffering so gameplay systems remain decoupled.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Buffering")]
        [SerializeField] private float jumpBufferDuration = 0.14f;
        [SerializeField] private float attackBufferDuration = 0.16f;

        private float _horizontal;
        private float _vertical;
        private bool _sprintHeld;
        private bool _jumpHeld;
        private bool _jumpReleasedThisFrame;
        private bool _attackHeld;
        private bool _attackReleasedThisFrame;
        private float _attackHoldDuration;
        private bool _dashPressed;
        private bool _harpoonPressed;
        private bool _silksoarPressed;
        private bool _silkSpearPressed;
        private bool _paleNailsPressed;
        private bool _tauntPressed;
        private bool _needolinPressed;
        private bool _needolinHeld;
        private bool _cycleCrestPressed;
        private bool _threadGuardPressed;
        private bool _healPressed;
        private bool _interactPressed;

        private float _jumpBufferTimer;
        private float _attackBufferTimer;

        public float Horizontal { get { return _horizontal; } }
        public float Vertical { get { return _vertical; } }
        public bool SprintHeld { get { return _sprintHeld; } }
        public bool JumpHeld { get { return _jumpHeld; } }
        public bool JumpReleasedThisFrame { get { return _jumpReleasedThisFrame; } }
        public bool AttackHeld { get { return _attackHeld; } }
        public bool AttackReleasedThisFrame { get { return _attackReleasedThisFrame; } }
        public float AttackHoldDuration { get { return _attackHoldDuration; } }
        public bool HasBufferedJump { get { return _jumpBufferTimer > 0f; } }
        public bool HasBufferedAttack { get { return _attackBufferTimer > 0f; } }
        public bool DashPressed { get { return _dashPressed; } }
        public bool HarpoonPressed { get { return _harpoonPressed; } }
        public bool SilksoarPressed { get { return _silksoarPressed; } }
        public bool SilkSpearPressed { get { return _silkSpearPressed; } }
        public bool PaleNailsPressed { get { return _paleNailsPressed; } }
        public bool TauntPressed { get { return _tauntPressed; } }
        public bool NeedolinPressed { get { return _needolinPressed; } }
        public bool NeedolinHeld { get { return _needolinHeld; } }
        public bool CycleCrestPressed { get { return _cycleCrestPressed; } }
        public bool ThreadGuardPressed { get { return _threadGuardPressed; } }
        public bool HealPressed { get { return _healPressed; } }
        public bool InteractPressed { get { return _interactPressed; } }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                ClearFrameInputs();
                return;
            }

            if (CaveTransitionManager.Instance != null && CaveTransitionManager.Instance.IsTransitioning)
            {
                ClearFrameInputs();
                _jumpBufferTimer = 0f;
                _attackBufferTimer = 0f;
                _horizontal = CaveTransitionManager.Instance.AutoWalkHorizontal;
                return;
            }

            float dt = Time.deltaTime;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= dt;
            if (_attackBufferTimer > 0f) _attackBufferTimer -= dt;

            float left = ControlBindingsManager.GetAction(ControlAction.MoveLeft) ? -1f : 0f;
            float right = ControlBindingsManager.GetAction(ControlAction.MoveRight) ? 1f : 0f;
            _horizontal = left + right;

            float down = ControlBindingsManager.GetAction(ControlAction.LookDown) ? -1f : 0f;
            float up = ControlBindingsManager.GetAction(ControlAction.LookUp) ? 1f : 0f;
            _vertical = up + down;

            _sprintHeld = ControlBindingsManager.GetAction(ControlAction.Sprint);

            bool jumpDown = ControlBindingsManager.GetActionDown(ControlAction.JumpGlide);
            if (jumpDown)
            {
                _jumpBufferTimer = jumpBufferDuration;
            }
            _jumpHeld = ControlBindingsManager.GetAction(ControlAction.JumpGlide);
            _jumpReleasedThisFrame = ControlBindingsManager.GetActionUp(ControlAction.JumpGlide);

            bool attackDown = ControlBindingsManager.GetActionDown(ControlAction.AttackCharge);
            bool attackHeldNow = ControlBindingsManager.GetAction(ControlAction.AttackCharge);
            bool attackUpNow = ControlBindingsManager.GetActionUp(ControlAction.AttackCharge);
            if (attackDown)
            {
                _attackBufferTimer = attackBufferDuration;
                _attackHoldDuration = 0f;
            }
            _attackHeld = attackHeldNow;
            _attackReleasedThisFrame = attackUpNow;
            if (attackHeldNow)
            {
                _attackHoldDuration += dt;
            }
            else if (!attackUpNow)
            {
                _attackHoldDuration = 0f;
            }

            _dashPressed = ControlBindingsManager.GetActionDown(ControlAction.Dash);
            _harpoonPressed = ControlBindingsManager.GetActionDown(ControlAction.Harpoon);
            _silksoarPressed = ControlBindingsManager.GetActionDown(ControlAction.Silksoar);
            _silkSpearPressed = ControlBindingsManager.GetActionDown(ControlAction.SilkSkill);
            _paleNailsPressed = ControlBindingsManager.GetActionDown(ControlAction.PaleNails);
            _tauntPressed = ControlBindingsManager.GetActionDown(ControlAction.Taunt);
            _needolinPressed = ControlBindingsManager.GetActionDown(ControlAction.Needolin);
            _needolinHeld = ControlBindingsManager.GetAction(ControlAction.Needolin);
            _cycleCrestPressed = ControlBindingsManager.GetActionDown(ControlAction.CycleCrest);
            _threadGuardPressed = ControlBindingsManager.GetActionDown(ControlAction.CrossStitch);
            _healPressed = ControlBindingsManager.GetActionDown(ControlAction.BindHeal);
            _interactPressed = ControlBindingsManager.GetActionDown(ControlAction.Interact);
        }

        public void ConsumeJumpBuffer()
        {
            _jumpBufferTimer = 0f;
        }

        public void ConsumeAttackBuffer()
        {
            _attackBufferTimer = 0f;
        }

        public void ClearFrameInputs()
        {
            _horizontal = 0f;
            _vertical = 0f;
            _sprintHeld = false;
            _jumpHeld = false;
            _jumpReleasedThisFrame = false;
            _attackHeld = false;
            _attackReleasedThisFrame = false;
            _attackHoldDuration = 0f;
            _dashPressed = false;
            _harpoonPressed = false;
            _silksoarPressed = false;
            _silkSpearPressed = false;
            _paleNailsPressed = false;
            _tauntPressed = false;
            _needolinPressed = false;
            _needolinHeld = false;
            _cycleCrestPressed = false;
            _threadGuardPressed = false;
            _healPressed = false;
            _interactPressed = false;
        }
    }
}
