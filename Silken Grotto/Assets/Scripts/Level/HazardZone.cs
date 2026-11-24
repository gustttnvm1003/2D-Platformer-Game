using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Environmental thorn/spike hazard. Deals damage and returns player to safe ledge.
    /// Also implements IDamageable strictly for downward aerial Pogo bounces (Silksong feel!).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HazardZone : MonoBehaviour, IDamageable
    {
        [SerializeField] private int damage = 1;

        public bool IsAlive { get { return true; } }
        public bool IsInvulnerable { get { return false; } }

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        public bool TakeDamage(DamageInfo info)
        {
            // Only clang/allow pogo bounce when struck downward in midair
            if (!info.IsDownwardPogo) return false;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnHitImpact(info.HitPoint, new Color(0.92f, 0.96f, 0.90f, 1f));
            }
            return true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null || !other.CompareTag(GameConstants.TagPlayer)) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsAlive && !player.IsInvulnerable)
            {
                player.TakeHazardDamage(damage, transform.position);
            }
        }
    }
}
