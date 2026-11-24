using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Luminous cave mouth / zone doorway trigger connecting two locations bidirectionally.
    /// Pulses an additive radial glow cue (#D9F2A8 to #F2FFD4 at 35%-55% opacity) bleeding inward
    /// and invokes CaveTransitionManager when the player walks into the doorway.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CaveEntrancePortal : MonoBehaviour, IInteractable
    {
        [Header("Portal Pairing & Destination")]
        [SerializeField] private CaveEntrancePortal linkedDestination;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;
        [SerializeField] private int walkDirection = 1;
        [SerializeField] private int arrivalWalkDirection = 1;

        [Header("Destination Room Bounds & Audio")]
        [SerializeField] private string roomId = "zone_cavern";
        [SerializeField] private string roomTitle = "Sunken Root Cavern";
        [SerializeField] private string roomSubtitle = "Forgotten Roots & Secret Weaves";
        [SerializeField] private Vector2 targetCamMinBounds = new Vector2(11f, -9.5f);
        [SerializeField] private Vector2 targetCamMaxBounds = new Vector2(74f, 22f);
        [SerializeField] private bool useDarkStoneAcoustics;

        [Header("Visual Doorway Radial Glow Cue")]
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private float baseGlowAlpha = 0.45f;
        [SerializeField] private float glowPulseAmplitude = 0.09f;
        [SerializeField] private float glowPulseSpeed = 1.65f;

        private BoxCollider2D _trigger;
        private bool _suppressedUntilExit;
        private float _retriggerCooldownUntil;

        public CaveEntrancePortal LinkedDestination { get { return linkedDestination; } }
        public int WalkDirection { get { return walkDirection; } }
        public int ArrivalWalkDirection { get { return arrivalWalkDirection; } }
        public string RoomId { get { return roomId; } }
        public string RoomTitle { get { return roomTitle; } }
        public string RoomSubtitle { get { return roomSubtitle; } }
        public Vector2 TargetCamMinBounds { get { return targetCamMinBounds; } }
        public Vector2 TargetCamMaxBounds { get { return targetCamMaxBounds; } }
        public bool UseDarkStoneAcoustics { get { return useDarkStoneAcoustics; } }

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider2D>();
            _trigger.isTrigger = true;
            if (glowRenderer == null) glowRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (glowRenderer == null) return;
            float wave = Mathf.Sin(Time.time * glowPulseSpeed + transform.position.x * 0.45f);
            float alpha = Mathf.Clamp(baseGlowAlpha + wave * glowPulseAmplitude, 0.35f, 0.55f);
            Color c = glowRenderer.color;
            c.a = alpha;
            glowRenderer.color = c;
        }

        public void Configure(
            CaveEntrancePortal destination,
            Vector3 spawnOff,
            int triggerWalkDir,
            int exitWalkDir,
            string id,
            string title,
            string subtitle,
            Vector2 camMin,
            Vector2 camMax,
            bool darkAcoustics,
            SpriteRenderer glow)
        {
            linkedDestination = destination;
            spawnOffset = spawnOff;
            walkDirection = triggerWalkDir >= 0 ? 1 : -1;
            arrivalWalkDirection = exitWalkDir >= 0 ? 1 : -1;
            roomId = id;
            roomTitle = title;
            roomSubtitle = subtitle;
            targetCamMinBounds = camMin;
            targetCamMaxBounds = camMax;
            useDarkStoneAcoustics = darkAcoustics;
            glowRenderer = glow;
        }

        public Vector3 GetSpawnWorldPosition()
        {
            return transform.position + spawnOffset;
        }

        public void SuppressReturnTriggerUntilExit()
        {
            _suppressedUntilExit = true;
            _retriggerCooldownUntil = Time.time + 0.90f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTrigger(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null || other.isTrigger || !other.CompareTag(GameConstants.TagPlayer)) return;
            _suppressedUntilExit = false;
        }

        public string GetInteractPrompt()
        {
            string destName = linkedDestination != null && !string.IsNullOrEmpty(linkedDestination.RoomTitle)
                ? linkedDestination.RoomTitle
                : roomTitle;
            string arrow = walkDirection >= 0 ? "->" : "<-";
            return "[E / Walk " + arrow + "] Enter " + destName;
        }

        public void Interact(PlayerController player)
        {
            if (player == null || !player.IsAlive || player.IsSitting) return;
            if (CaveTransitionManager.Instance == null || CaveTransitionManager.Instance.IsTransitioning || CaveTransitionManager.Instance.IsInCooldown) return;
            _retriggerCooldownUntil = Time.time + 1.25f;
            CaveTransitionManager.Instance.BeginPortalTransition(this, player);
        }

        private void TryTrigger(Collider2D other)
        {
            if (other == null || other.isTrigger || !other.CompareTag(GameConstants.TagPlayer)) return;
            if (_suppressedUntilExit || Time.time < _retriggerCooldownUntil) return;
            if (CaveTransitionManager.Instance == null || CaveTransitionManager.Instance.IsTransitioning || CaveTransitionManager.Instance.IsInCooldown) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive || player.IsSitting) return;

            PlayerMovement move = player.Movement;
            PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
            if (move != null)
            {
                if (move.IsDashing) return;
                // Fire when walking/pushing cleanly into the doorway OR pressing Up/Interact while inside the gate
                bool movingIn = move.HorizontalVelocity * walkDirection > 0.20f;
                bool holdingIn = input != null && (input.Horizontal * walkDirection > 0.25f || input.Vertical > 0.5f || input.InteractPressed);
                if (!movingIn && !holdingIn) return;
            }

            _retriggerCooldownUntil = Time.time + 1.25f;
            CaveTransitionManager.Instance.BeginPortalTransition(this, player);
        }
    }
}
