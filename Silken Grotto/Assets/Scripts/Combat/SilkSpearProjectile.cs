using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Piercing ranged projectile launched by the Silk Spear skill.
    /// Damages each enemy once along its flight path and stops on solid walls.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class SilkSpearProjectile : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private int _damage;
        private float _speed;
        private int _direction;
        private float _lifetime;
        private GameObject _owner;
        private LayerMask _targetMask;
        private LayerMask _wallMask;
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();

        public void Initialize(
            int damage,
            float speed,
            int direction,
            float lifetime,
            GameObject owner,
            LayerMask targetMask,
            LayerMask wallMask)
        {
            _damage = Mathf.Max(1, damage);
            _speed = Mathf.Max(4f, speed);
            _direction = direction >= 0 ? 1 : -1;
            _lifetime = Mathf.Max(0.2f, lifetime);
            _owner = owner;
            _targetMask = targetMask;
            _wallMask = wallMask;

            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.velocity = new Vector2(_direction * _speed, 0f);

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;

            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * _direction;
            transform.localScale = s;
        }

        private void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
            {
                return;
            }

            int layerBit = 1 << other.gameObject.layer;

            if ((layerBit & _targetMask.value) != 0)
            {
                Hurtbox hurtbox = other.GetComponent<Hurtbox>();
                IDamageable target = hurtbox != null ? hurtbox.Owner : other.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive && !_hitTargets.Contains(target))
                {
                    Vector3 hitPos = other.ClosestPoint(transform.position);
                    DamageInfo info = new DamageInfo(
                        _damage,
                        new Vector2(_direction, 0.25f),
                        8.5f,
                        0.28f,
                        _owner,
                        hitPos,
                        false
                    );

                    bool hit = hurtbox != null ? hurtbox.ReceiveHit(info) : target.TakeDamage(info);
                    if (hit)
                    {
                        _hitTargets.Add(target);
                        bool isEnemy = target is EnemyController;
                        if (VFXManager.Instance != null)
                        {
                            VFXManager.Instance.SpawnHitImpact(hitPos, Color.white, isEnemy, !isEnemy);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyHit();
                        GameEvents.RaiseCameraShake(0.048f, 0.07f);
                    }
                }
                return;
            }

            if ((layerBit & _wallMask.value) != 0)
            {
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnHitImpact(transform.position, Color.white, false, true);
                }
                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.72f);
                Destroy(gameObject);
            }
        }
    }
}
