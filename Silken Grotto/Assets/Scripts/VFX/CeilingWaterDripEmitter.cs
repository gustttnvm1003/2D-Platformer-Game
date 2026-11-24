using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Pooled ceiling water-drip emitter placed beneath overhanging moss ledges and stalactites.
    /// Drops thin bright-white vertical droplets that spawn a crisp splash ring/ripple upon
    /// hitting platforms or shallow water pools.
    /// </summary>
    public class CeilingWaterDripEmitter : MonoBehaviour
    {
        private class DripSlot
        {
            public GameObject Go;
            public Transform Tr;
            public SpriteRenderer Sr;
            public bool Active;
            public float VelocityY;
            public float TargetFloorY;
            public float MaxTravelY;
        }

        [Header("Drip Visuals")]
        [SerializeField] private Sprite waterDripSprite;
        [SerializeField] private Material additiveMaterial;

        [Header("Emission Tuning")]
        [SerializeField] private float spanWidth = 10f;
        [SerializeField] private float minInterval = 0.85f;
        [SerializeField] private float maxInterval = 2.4f;
        [SerializeField] private float dripGravity = 16.5f;
        [SerializeField] private float maxDropDistance = 14f;
        [SerializeField] private float audibleRadius = 11.5f;
        [SerializeField] private int poolCount = 8;

        private DripSlot[] _drops;
        private float _spawnTimer;
        private int _groundMask;
        private Transform _camTransform;

        private void Awake()
        {
            _groundMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform);
            _spawnTimer = Random.Range(0.25f, maxInterval);

            int count = Mathf.Clamp(poolCount, 2, 16);
            _drops = new DripSlot[count];
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("WaterDrip_" + i);
                go.transform.SetParent(transform, false);
                go.SetActive(false);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = waterDripSprite;
                sr.color = new Color(0.95f, 1f, 0.98f, 0.88f);
                sr.sortingLayerName = GameConstants.SortVFX;
                sr.sortingOrder = 6;
                if (additiveMaterial != null) sr.sharedMaterial = additiveMaterial;

                DripSlot slot = new DripSlot();
                slot.Go = go;
                slot.Tr = go.transform;
                slot.Sr = sr;
                slot.Active = false;
                _drops[i] = slot;
            }
        }

        public void Configure(float width, float minInt, float maxInt, float maxDist)
        {
            spanWidth = Mathf.Max(1f, width);
            minInterval = Mathf.Max(0.2f, minInt);
            maxInterval = Mathf.Max(minInterval, maxInt);
            maxDropDistance = Mathf.Max(2f, maxDist);
        }

        private void Update()
        {
            if (waterDripSprite == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            if (_camTransform == null && UnityEngine.Camera.main != null)
            {
                _camTransform = UnityEngine.Camera.main.transform;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Only emit when reasonably near the camera viewport
            bool nearCam = _camTransform == null || Mathf.Abs(_camTransform.position.x - transform.position.x) <= (spanWidth * 0.5f + 16f);
            if (nearCam)
            {
                _spawnTimer -= dt;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = Random.Range(minInterval, maxInterval);
                    SpawnDrop();
                }
            }

            for (int i = 0; i < _drops.Length; i++)
            {
                DripSlot d = _drops[i];
                if (!d.Active) continue;

                d.VelocityY += dripGravity * dt;
                Vector3 pos = d.Tr.position;
                pos.y -= d.VelocityY * dt;

                // Subtle vertical motion stretch as droplet accelerates
                float stretchY = Mathf.Clamp(0.22f + d.VelocityY * 0.014f, 0.22f, 0.42f);
                d.Tr.localScale = new Vector3(0.20f, stretchY, 1f);

                if (pos.y <= d.TargetFloorY)
                {
                    pos.y = d.TargetFloorY;
                    d.Active = false;
                    d.Go.SetActive(false);
                    TriggerSplash(pos);
                }
                else
                {
                    d.Tr.position = pos;
                }
            }
        }

        private void SpawnDrop()
        {
            DripSlot free = null;
            for (int i = 0; i < _drops.Length; i++)
            {
                if (!_drops[i].Active)
                {
                    free = _drops[i];
                    break;
                }
            }
            if (free == null) return;

            float x = transform.position.x + Random.Range(-spanWidth * 0.5f, spanWidth * 0.5f);
            float startY = transform.position.y;
            Vector2 origin = new Vector2(x, startY);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, maxDropDistance, _groundMask);

            float floorY = hit.collider != null ? hit.point.y + 0.06f : (startY - maxDropDistance);
            if (startY - floorY < 1.2f) return;

            free.Active = true;
            free.VelocityY = Random.Range(0.4f, 1.2f);
            free.TargetFloorY = floorY;
            free.MaxTravelY = startY - floorY;
            free.Tr.position = new Vector3(x, startY, 0f);
            free.Tr.localScale = new Vector3(0.20f, 0.22f, 1f);
            free.Go.SetActive(true);
        }

        private void TriggerSplash(Vector3 impactPos)
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnWaterSplashRing(impactPos);
            }

            if (AudioManager.Instance != null && _camTransform != null)
            {
                float dist = Vector2.Distance(_camTransform.position, impactPos);
                if (dist <= audibleRadius)
                {
                    float atten = Mathf.Clamp01(1f - (dist / audibleRadius));
                    AudioManager.Instance.PlayWaterDrip(0.18f * atten * atten);
                }
            }
        }
    }
}
