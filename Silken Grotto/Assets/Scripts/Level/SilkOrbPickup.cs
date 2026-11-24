using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Collectible Silk Essence orb dropped by defeated enemies or hidden in secret exploration alcoves.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class SilkOrbPickup : MonoBehaviour
    {
        [SerializeField] private int essenceValue = 15;
        [SerializeField] private float magnetRadius = 3.2f;
        [SerializeField] private float magnetSpeed = 10f;

        private Vector3 _basePosition;
        private Vector2 _dropVelocity;
        private bool _isDropped;
        private float _bobSeed;
        private Transform _playerTransform;

        private void Awake()
        {
            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            _basePosition = transform.position;
            _bobSeed = Random.Range(0f, 6.28f);
        }

        public void InitializeDrop(int value, Vector2 burstVelocity)
        {
            essenceValue = Mathf.Max(1, value);
            _dropVelocity = burstVelocity;
            _isDropped = true;
        }

        public void SetValue(int value)
        {
            essenceValue = Mathf.Max(1, value);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            if (_isDropped)
            {
                transform.position += (Vector3)(_dropVelocity * dt);
                _dropVelocity.y -= 11f * dt;
                _dropVelocity.x *= (1f - 3.5f * dt);
                if (_dropVelocity.y < -2.5f)
                {
                    _isDropped = false;
                    _basePosition = transform.position;
                }
            }
            else
            {
                transform.position = _basePosition + new Vector3(0f, Mathf.Sin(Time.time * 3.2f + _bobSeed) * 0.16f, 0f);
            }

            if (_playerTransform == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
                if (p != null) _playerTransform = p.transform;
            }

            if (_playerTransform != null)
            {
                Vector3 targetPos = _playerTransform.position + Vector3.up * 0.9f;
                float dist = Vector2.Distance(transform.position, targetPos);
                if (dist <= magnetRadius)
                {
                    _isDropped = false;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * dt);
                    _basePosition = transform.position;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag(GameConstants.TagPlayer)) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.Upgrades != null)
            {
                player.Upgrades.AddSilkEssence(essenceValue);
                if (player.Stats != null) player.Stats.AddSilk(6f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.62f);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnHitImpact(transform.position, new Color(0.78f, 0.98f, 0.62f, 0.95f), false, false);
                }
                Destroy(gameObject);
            }
        }
    }
}
