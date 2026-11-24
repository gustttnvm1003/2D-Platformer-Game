using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Destructible thorny vine barrier concealing Metroidvania secret rooms or return shortcuts.
    /// Implements IDamageable so melee combos and Silk Spear break it cleanly.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BreakableWall : MonoBehaviour, IDamageable
    {
        [SerializeField] private string wallId = "secret_wall_01";
        [SerializeField] private int maxHits = 3;
        [SerializeField] private string secretFoundMessage = "Hidden Sanctuary Uncovered";
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int _currentHits;
        private bool _broken;

        public string WallId { get { return wallId; } }
        public bool IsAlive { get { return !_broken; } }
        public bool IsInvulnerable { get { return false; } }

        private void Awake()
        {
            _currentHits = Mathf.Max(1, maxHits);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Configure(string id, int hits, string message)
        {
            wallId = id;
            maxHits = Mathf.Max(1, hits);
            _currentHits = maxHits;
            secretFoundMessage = message;
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (_broken) return false;
            _currentHits--;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnHitImpact(info.HitPoint, Color.white, false, false);
                VFXManager.Instance.SpawnFoliageLeafBurst(info.HitPoint, 14, info.KnockbackDirection.x * 1.4f);
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyHit();
            GameEvents.RaiseCameraShake(0.045f, 0.065f);

            if (_currentHits <= 0)
            {
                BreakOpen();
            }
            else if (spriteRenderer != null)
            {
                StartCoroutine(HitShakeRoutine());
            }
            return true;
        }

        private void BreakOpen()
        {
            _broken = true;
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnFoliageLeafBurst(transform.position, 18, 0f);
                VFXManager.Instance.SpawnSkillBurst(transform.position, new Color(0.72f, 0.96f, 0.48f, 0.95f));
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyDeath();

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.RecordBrokenWall(wallId);
            }

            if (!string.IsNullOrEmpty(secretFoundMessage))
            {
                GameEvents.RaiseSecretDiscovered(wallId);
                GameEvents.RaiseNotification("SECRET PASSAGE OPENED", secretFoundMessage);
            }

            gameObject.SetActive(false);
        }

        public void ForceSetBrokenFromSave()
        {
            _broken = true;
            gameObject.SetActive(false);
        }

        private IEnumerator HitShakeRoutine()
        {
            Vector3 origin = transform.position;
            Color baseCol = spriteRenderer.color;
            spriteRenderer.color = new Color(0.95f, 0.95f, 0.85f, 1f);
            for (int i = 0; i < 4; i++)
            {
                transform.position = origin + new Vector3((i % 2 == 0 ? 0.08f : -0.08f), 0f, 0f);
                yield return new WaitForSeconds(0.03f);
            }
            transform.position = origin;
            spriteRenderer.color = baseCol;
        }
    }
}
