using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Ancient Silk Pedestal that permanently grants an Ability/SkillData ScriptableObject
    /// when interacted with during Metroidvania exploration.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class AbilityPedestal : MonoBehaviour, IInteractable
    {
        [SerializeField] private SkillData skillToUnlock;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private bool _claimed;

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            RefreshClaimedState();
        }

        public void Configure(SkillData skill)
        {
            skillToUnlock = skill;
            RefreshClaimedState();
        }

        public void RefreshClaimedState()
        {
            if (skillToUnlock == null) return;
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null && player.Skills != null && player.Skills.IsUnlocked(skillToUnlock.skillType))
            {
                _claimed = true;
                ApplyClaimedVisual();
            }
        }

        public string GetInteractPrompt()
        {
            if (_claimed || skillToUnlock == null) return string.Empty;
            return "[E] Claim " + skillToUnlock.displayName;
        }

        public void Interact(PlayerController player)
        {
            if (_claimed || player == null || player.Skills == null || skillToUnlock == null) return;

            bool unlocked = player.Skills.UnlockSkill(skillToUnlock, false);
            if (unlocked)
            {
                _claimed = true;
                ApplyClaimedVisual();

                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnSkillBurst(transform.position + Vector3.up * 1.4f, skillToUnlock.accentColor);
                }
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SaveCurrentProgress();
                }
            }
        }

        private void ApplyClaimedVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.55f, 0.58f, 0.58f, 0.75f);
            }
        }
    }
}
