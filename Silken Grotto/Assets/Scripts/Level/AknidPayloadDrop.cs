using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Handles released Aknid carry payloads purely via code-driven visuals (zero new image assets!):
    /// - Berry: drops smoothly to floor as an interactive Mossberry pickup restoring +1 HP (or +25 Silk & +20 Essence if full HP)
    /// - Flea: rescued captive flea that hops upward and flutters away joyfully with celebratory sparkles + +25 Essence!
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class AknidPayloadDrop : MonoBehaviour
    {
        private AknidCarryType _payloadType = AknidCarryType.Berry;
        private Vector2 _velocity;
        private bool _grounded;
        private bool _collected;
        private float _age;
        private float _sparkleTimer;
        private Vector3 _restPos;
        private LayerMask _groundMask;
        private Transform _visualRoot;

        public void Initialize(AknidCarryType type, Vector3 spawnPosition, Vector2 initialVelocity)
        {
            _payloadType = type;
            transform.position = spawnPosition;
            _velocity = initialVelocity;
            _grounded = false;
            _collected = false;
            _age = 0f;
            _sparkleTimer = 0.12f;
            _groundMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform);

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = 0.56f;
                col.enabled = (_payloadType == AknidCarryType.Berry);
            }
            gameObject.layer = GameConstants.LayerInteractable;

            GameObject vis = ChoralCodeVFX.CreateAknidCarryVisual(transform, _payloadType);
            if (vis != null)
            {
                vis.transform.localPosition = Vector3.zero;
                Transform tether = vis.transform.Find("SilkTether");
                if (tether != null) tether.gameObject.SetActive(false);
                _visualRoot = vis.transform;
            }

            if (_payloadType == AknidCarryType.Flea)
            {
                TriggerFleaRescueCelebration();
            }
        }

        private void TriggerFleaRescueCelebration()
        {
            ChoralCodeVFX.SpawnJoyfulFleaSparkles(transform.position);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCheckpoint();
            GameEvents.RaiseNotification("FLEA FREED!", "A captive flea escaped safely! (+25 Silk Essence)");

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null && player.Upgrades != null)
            {
                player.Upgrades.AddSilkEssence(25);
            }
        }

        private void Update()
        {
            if (_collected) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float dt = Time.deltaTime;
            _age += dt;

            if (_payloadType == AknidCarryType.Flea)
            {
                // Joyful escaping hops & upward flutter off screen!
                float flutterX = Mathf.Sin(_age * 11f) * 2.2f;
                float hopY = 3.8f + Mathf.Abs(Mathf.Sin(_age * 9f)) * 2.4f;
                transform.position += new Vector3((_velocity.x + flutterX) * dt, hopY * dt, 0f);
                if (_visualRoot != null)
                {
                    float sc = 1f + Mathf.Sin(_age * 18f) * 0.12f;
                    _visualRoot.localScale = new Vector3(sc, 2f - sc, 1f);
                }

                _sparkleTimer -= dt;
                if (_sparkleTimer <= 0f)
                {
                    _sparkleTimer = 0.24f;
                    ChoralCodeVFX.SpawnJoyfulFleaSparkles(transform.position);
                }
                if (_age >= 1.85f)
                {
                    Destroy(gameObject);
                }
                return;
            }

            // Berry: drop smoothly to ground, then bob gently waiting to be picked up!
            if (!_grounded)
            {
                _velocity.y -= 15.5f * dt;
                _velocity.x *= Mathf.Clamp01(1f - 2.6f * dt);
                Vector3 nextPos = transform.position + (Vector3)(_velocity * dt);

                if (_velocity.y <= 0f)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Abs(_velocity.y * dt) + 0.32f, _groundMask);
                    if (hit.collider != null)
                    {
                        nextPos.y = hit.point.y + 0.26f;
                        _grounded = true;
                        _restPos = nextPos;
                        _velocity = Vector2.zero;
                        ChoralCodeVFX.SpawnMossPogoBurst(hit.point, new Color(0.58f, 0.94f, 0.34f, 0.92f));
                    }
                }
                transform.position = nextPos;
            }
            else
            {
                transform.position = _restPos + new Vector3(0f, Mathf.Sin(_age * 3.4f) * 0.10f, 0f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || _payloadType != AknidCarryType.Berry || other == null) return;
            if (!other.CompareTag(GameConstants.TagPlayer) && other.gameObject.layer != GameConstants.LayerPlayer) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.IsAlive) return;

            _collected = true;
            bool healed = false;
            if (player.Stats != null && player.Stats.CurrentHealth < player.Stats.MaxHealth)
            {
                healed = player.Stats.HealHealth(1);
            }
            if (player.Stats != null) player.Stats.AddSilk(24f);
            if (player.Upgrades != null) player.Upgrades.AddSilkEssence(20);

            if (AudioManager.Instance != null) AudioManager.Instance.PlayBindBurst();
            ChoralCodeVFX.SpawnJoyfulFleaSparkles(transform.position);
            GameEvents.RaiseNotification("MOSSBERRY", healed ? "Restored +1 Mask & +20 Silk Essence!" : "Gained +24 Silk & +20 Silk Essence!");
            Destroy(gameObject);
        }
    }
}
