using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Sanctuary Checkpoint Bench / Lantern Shrine.
    /// Restores player health & silk, sets respawn point, saves progress, and shows notification.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CheckpointShrine : MonoBehaviour, IInteractable
    {
        [SerializeField] private string checkpointId = "shrine_glade";
        [SerializeField] private string shrineName = "Moss Glade Bench";
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private Color inactiveColor = new Color(0.55f, 0.72f, 0.68f, 0.45f);
        [SerializeField] private Color activeColor = new Color(0.45f, 1f, 0.85f, 0.95f);

        [Header("Bench Rest Alignment")]
        [SerializeField] private Vector2 seatLocalOffset = new Vector2(-0.32f, 0f);
        [SerializeField] private int benchFacingDirection = 1;

        private bool _activated;
        private bool _playerRestingHere;

        public string CheckpointId { get { return checkpointId; } }
        public int BenchFacingDirection { get { return benchFacingDirection >= 0 ? 1 : -1; } }

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            UpdateVisuals();
        }

        public void Configure(string id, string displayName)
        {
            checkpointId = id;
            shrineName = displayName;
        }

        public Vector3 GetSeatWorldPosition()
        {
            float dir = transform.localScale.x < 0f ? -1f : 1f;
            return transform.position + new Vector3(seatLocalOffset.x * dir, seatLocalOffset.y, 0f);
        }

        public void NotifyPlayerStoodUp()
        {
            _playerRestingHere = false;
        }

        public string GetInteractPrompt()
        {
            if (_playerRestingHere)
            {
                return "[E / Move] Stand Up   |   [TAB] Attune Upgrades";
            }
            return "[E] Sit & Rest at " + shrineName;
        }

        public void Interact(PlayerController player)
        {
            if (player == null || !player.IsAlive) return;

            if (player.IsSitting)
            {
                player.StandUpFromBench();
                return;
            }

            _activated = true;
            _playerRestingHere = true;
            UpdateVisuals();

            Vector3 seatPos = GetSeatWorldPosition();
            player.SetCheckpoint(checkpointId, seatPos);
            player.BeginBenchRest(this);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveCurrentProgress();
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayCheckpoint();
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnSkillBurst(transform.position + new Vector3(0.95f, 1.55f, 0f), activeColor);
            }
            GameEvents.RaiseNotification("CHECKPOINT REACHED", shrineName + " — Resting at Bench. Health & Silk Restored.");
        }

        private void UpdateVisuals()
        {
            if (glowRenderer != null)
            {
                glowRenderer.color = _activated ? activeColor : inactiveColor;
            }
        }
    }
}
