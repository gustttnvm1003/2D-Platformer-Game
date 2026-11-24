using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Collectible world item pickup that adds a specific InventoryItemData entry
    /// into the player's Inventory Pouch and tracks collection for Save/Load persistence.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class InventoryPickup : MonoBehaviour
    {
        [SerializeField] private string pickupId;
        [SerializeField] private string itemId = "item_essence_shard";
        [SerializeField] private int quantity = 1;
        [SerializeField] private float magnetRadius = 2.6f;
        [SerializeField] private float magnetSpeed = 9f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 _basePosition;
        private float _bobSeed;
        private Transform _playerTransform;
        private bool _collected;

        public string PickupId { get { return pickupId; } }

        private void Awake()
        {
            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            _basePosition = transform.position;
            _bobSeed = Random.Range(0f, 6.28f);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Configure(string uniquePickupId, string targetItemId, int count, Sprite iconSprite)
        {
            pickupId = uniquePickupId;
            itemId = targetItemId;
            quantity = Mathf.Max(1, count);
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && iconSprite != null)
            {
                spriteRenderer.sprite = iconSprite;
            }
        }

        public void ForceSetCollectedFromSave()
        {
            _collected = true;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_collected) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            transform.position = _basePosition + new Vector3(0f, Mathf.Sin(Time.time * 2.8f + _bobSeed) * 0.14f, 0f);

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
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);
                    _basePosition = transform.position;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || other == null || !other.CompareTag(GameConstants.TagPlayer)) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.Inventory != null)
            {
                _collected = true;
                player.Inventory.AddItem(itemId, quantity, true);
                player.RecordCollectedPickup(pickupId);

                if (AudioManager.Instance != null) AudioManager.Instance.PlayArmorClink(0.75f);
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.SpawnHitImpact(transform.position, new Color(0.68f, 0.96f, 0.82f, 0.95f), false, false);
                }
                gameObject.SetActive(false);
            }
        }
    }
}
