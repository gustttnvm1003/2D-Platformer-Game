using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Precision active-window Hitbox. Only detects overlaps when explicitly armed during
    /// an attack's active frames and tracks targets already hit in the current swing
    /// so damage is NEVER applied continuously every physics tick.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayers;

        private BoxCollider2D _collider;
        private readonly HashSet<IDamageable> _hitTargetsThisSwing = new HashSet<IDamageable>();
        private DamageInfo _currentDamage;
        private bool _isActive;
        private Action<IDamageable, Vector3> _onHitCallback;

        public bool IsActive { get { return _isActive; } }

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            _collider.enabled = false;
            _isActive = false;
        }

        public void SetTargetLayers(LayerMask mask)
        {
            targetLayers = mask;
        }

        public void Activate(Vector2 localOffset, Vector2 size, DamageInfo damageInfo, Action<IDamageable, Vector3> onHitCallback = null)
        {
            _hitTargetsThisSwing.Clear();
            _currentDamage = damageInfo;
            _onHitCallback = onHitCallback;
            _isActive = true;

            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            _collider.offset = localOffset;
            _collider.size = size;
            _collider.enabled = true;

            // Immediate overlap scan so fast attacks register cleanly on activation frame
            ScanOverlaps();
        }

        public void Deactivate()
        {
            _isActive = false;
            _onHitCallback = null;
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            _hitTargetsThisSwing.Clear();
        }

        private void FixedUpdate()
        {
            if (_isActive)
            {
                ScanOverlaps();
            }
        }

        private void ScanOverlaps()
        {
            if (_collider == null || !_collider.enabled) return;

            Bounds b = _collider.bounds;
            Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, b.size, 0f, targetLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                TryApplyHit(hits[i]);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;
            if (((1 << other.gameObject.layer) & targetLayers.value) == 0) return;
            TryApplyHit(other);
        }

        private void TryApplyHit(Collider2D col)
        {
            if (col == null) return;
            if (_currentDamage.Source != null && (col.gameObject == _currentDamage.Source || col.transform.IsChildOf(_currentDamage.Source.transform)))
            {
                return;
            }

            Hurtbox hurtbox = col.GetComponent<Hurtbox>();
            IDamageable target = hurtbox != null ? hurtbox.Owner : col.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;
            if (_hitTargetsThisSwing.Contains(target)) return;

            Vector3 hitPoint = col.ClosestPoint(transform.position);
            DamageInfo info = _currentDamage;
            info.HitPoint = hitPoint;

            bool success = hurtbox != null ? hurtbox.ReceiveHit(info) : target.TakeDamage(info);
            if (success)
            {
                _hitTargetsThisSwing.Add(target);
                if (_onHitCallback != null)
                {
                    _onHitCallback(target, hitPoint);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            Gizmos.color = _isActive ? new Color(1f, 0.2f, 0.25f, 0.55f) : new Color(1f, 0.8f, 0.2f, 0.25f);
            Gizmos.DrawWireCube(transform.TransformPoint(box.offset), box.size);
        }
    }
}
