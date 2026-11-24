using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// In-world Ancient Moss Altar that opens the Attribute Upgrade & Skill Overview Menu.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class UpgradeAltar : MonoBehaviour, IInteractable
    {
        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        public string GetInteractPrompt()
        {
            return "[E] Attune at Upgrade Altar (or press TAB)";
        }

        public void Interact(PlayerController player)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OpenUpgradeMenu();
            }
        }
    }
}
