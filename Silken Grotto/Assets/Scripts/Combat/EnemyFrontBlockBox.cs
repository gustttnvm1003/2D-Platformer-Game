using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Dedicated frontal blocking collider separated from BodyHurtbox (Reed BlockStance & Grand Reed GuardStance).
    /// Blocks horizontal strikes from the front (0 damage, metallic clang, sparks, light player knockback),
    /// while downward aerial pogo attacks from above and strikes from behind pass straight through to BodyHurtbox!
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class EnemyFrontBlockBox : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyController owner;
        [SerializeField] private Vector2 localFrontOffset = new Vector2(0.68f, 0.86f);
        [SerializeField] private Vector2 boxSize = new Vector2(0.72f, 1.32f);
        [SerializeField] private Color guardVisualColor = new Color(1.0f, 0.92f, 0.56f, 0.95f);

        private BoxCollider2D _box;
        private GameObject _guardVisual;
        private bool _isBlocking;

        public bool IsAlive { get { return owner != null && owner.IsAlive; } }
        public bool IsInvulnerable { get { return false; } }
        public bool IsBlocking { get { return _isBlocking; } }

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<EnemyController>();
            _box = GetComponent<BoxCollider2D>();
            _box.isTrigger = true;
            _box.size = boxSize;
            _box.offset = Vector2.zero;
            gameObject.layer = GameConstants.LayerEnemy;
            EnsureGuardVisual();
            SetBlockActive(false);
        }

        public void Configure(EnemyController ownerController, Vector2 frontOffset, Vector2 size, Color tint)
        {
            owner = ownerController;
            localFrontOffset = frontOffset;
            boxSize = size;
            guardVisualColor = tint;
            if (_box == null) _box = GetComponent<BoxCollider2D>();
            if (_box != null)
            {
                _box.isTrigger = true;
                _box.size = boxSize;
                _box.offset = Vector2.zero;
            }
            gameObject.layer = GameConstants.LayerEnemy;
            UpdateFacingPosition(owner != null ? owner.FacingDirection : 1);
        }

        private void EnsureGuardVisual()
        {
            if (_guardVisual == null)
            {
                _guardVisual = ChoralCodeVFX.CreateGuardStanceVisual(transform, boxSize, guardVisualColor);
            }
        }

        public void UpdateFacingPosition(int facingDir)
        {
            int dir = facingDir >= 0 ? 1 : -1;
            transform.localPosition = new Vector3(Mathf.Abs(localFrontOffset.x) * dir, localFrontOffset.y, 0f);
            transform.localScale = new Vector3(dir, 1f, 1f);
        }

        public void SetBlockActive(bool active)
        {
            _isBlocking = active && IsAlive;
            if (_box == null) _box = GetComponent<BoxCollider2D>();
            if (_box != null) _box.enabled = _isBlocking;
            EnsureGuardVisual();
            if (_guardVisual != null) _guardVisual.SetActive(_isBlocking);
            if (_isBlocking && owner != null)
            {
                UpdateFacingPosition(owner.FacingDirection);
            }
        }

        private void LateUpdate()
        {
            if (_isBlocking && owner != null)
            {
                UpdateFacingPosition(owner.FacingDirection);
            }
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (!_isBlocking || owner == null || !owner.IsAlive) return false;

            // Downward pogo from above, guard-breaking strikes (Charge Attack / Cross Stitch / Thread Storm), & attacks from behind bypass frontBlockBox cleanly!
            if (info.IsDownwardPogo) return false;
            if (info.BreaksGuard)
            {
                Vector3 breakPos = info.HitPoint.sqrMagnitude > 0.0001f ? info.HitPoint : transform.position;
                ChoralCodeVFX.SpawnBlockSparks(breakPos, -owner.FacingDirection);
                SetBlockActive(false);
                return false;
            }
            if (info.Source != null)
            {
                float dx = info.Source.transform.position.x - owner.transform.position.x;
                if (dx * owner.FacingDirection <= 0.05f) return false;
            }

            Vector3 sparkPos = info.HitPoint.sqrMagnitude > 0.0001f ? info.HitPoint : transform.position;
            owner.TriggerFrontalBlockFeedback(info, sparkPos);
            return false; // 0 damage dealt; attack is cleanly parried/blocked
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isBlocking ? new Color(1f, 0.9f, 0.25f, 0.7f) : new Color(0.5f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireCube(transform.position, boxSize);
        }
    }
}
