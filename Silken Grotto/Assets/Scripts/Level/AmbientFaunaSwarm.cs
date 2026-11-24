using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Lightweight ambient living fauna swarm (fluttering cave moths, firefly-moths,
    /// desert scarab wisps, frost sprites, or sanctuary golden moths) hovering softly
    /// around lanterns, bioluminescent flora, pools, and sunlight beams.
    /// Zero physics overhead; drives gentle wing-flap & figure-8 flight curves.
    /// </summary>
    public class AmbientFaunaSwarm : MonoBehaviour
    {
        [Header("Fauna Setup")]
        [SerializeField] private Sprite critterSprite;
        [SerializeField] private Material additiveMaterial;
        [SerializeField] private int count = 5;
        [SerializeField] private Vector2 roamRadius = new Vector2(2.6f, 1.35f);
        [SerializeField] private Color tintColorA = new Color(0.88f, 0.96f, 1.0f, 0.85f);
        [SerializeField] private Color tintColorB = new Color(0.70f, 0.88f, 0.98f, 0.72f);
        [SerializeField] private float minScale = 0.22f;
        [SerializeField] private float maxScale = 0.36f;
        [SerializeField] private float flightSpeed = 1.15f;
        [SerializeField] private float flapSpeed = 16.0f;

        private struct Critter
        {
            public Transform Tr;
            public SpriteRenderer Sr;
            public Vector2 Anchor;
            public float RadiusX;
            public float RadiusY;
            public float Speed;
            public float Phase;
            public float BaseScale;
            public float FlapRate;
        }

        private Critter[] _critters;

        public void Configure(
            Sprite sprite, Material mat, int critterCount, Vector2 radius,
            Color colA, Color colB, float minSz, float maxSz, float speed, float flap)
        {
            critterSprite = sprite;
            additiveMaterial = mat;
            count = Mathf.Clamp(critterCount, 2, 10);
            roamRadius = radius;
            tintColorA = colA;
            tintColorB = colB;
            minScale = minSz;
            maxScale = maxSz;
            flightSpeed = speed;
            flapSpeed = flap;
        }

        private void Start()
        {
            if (critterSprite == null) return;
            int n = Mathf.Clamp(count, 2, 10);
            _critters = new Critter[n];

            for (int i = 0; i < n; i++)
            {
                GameObject go = new GameObject("BiomeCritter_" + i);
                go.transform.SetParent(transform, false);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = critterSprite;
                sr.color = Color.Lerp(tintColorA, tintColorB, (i % 3) * 0.5f);
                sr.sortingLayerName = (i % 2 == 0) ? GameConstants.SortVFX : GameConstants.SortNearBackground;
                sr.sortingOrder = (i % 2 == 0) ? 5 : 9;
                if (additiveMaterial != null && (i % 3 == 0))
                {
                    sr.sharedMaterial = additiveMaterial;
                }

                float seed = transform.position.x * 0.31f + i * 1.97f;
                float sc = Mathf.Lerp(minScale, maxScale, 0.5f + 0.5f * Mathf.Sin(seed * 2.3f));
                _critters[i] = new Critter
                {
                    Tr = go.transform,
                    Sr = sr,
                    Anchor = new Vector2(Mathf.Sin(seed * 1.3f) * roamRadius.x * 0.45f, Mathf.Cos(seed * 1.7f) * roamRadius.y * 0.35f),
                    RadiusX = roamRadius.x * (0.45f + 0.45f * (0.5f + 0.5f * Mathf.Cos(seed))),
                    RadiusY = roamRadius.y * (0.40f + 0.50f * (0.5f + 0.5f * Mathf.Sin(seed * 1.9f))),
                    Speed = flightSpeed * (0.78f + 0.44f * (0.5f + 0.5f * Mathf.Sin(seed * 3.1f))),
                    Phase = seed * 3.7f,
                    BaseScale = sc,
                    FlapRate = flapSpeed * (0.85f + 0.30f * (0.5f + 0.5f * Mathf.Cos(seed * 4.1f)))
                };
            }
        }

        private void Update()
        {
            if (_critters == null) return;
            float t = Time.time;
            for (int i = 0; i < _critters.Length; i++)
            {
                Critter c = _critters[i];
                if (c.Tr == null) continue;

                float ang = t * c.Speed + c.Phase;
                // Gentle figure-8 Lissajous hovering motion
                float x = c.Anchor.x + Mathf.Sin(ang) * c.RadiusX;
                float y = c.Anchor.y + Mathf.Sin(ang * 2.0f + 0.7f) * c.RadiusY * 0.65f + Mathf.Cos(t * 2.6f + c.Phase) * 0.12f;
                c.Tr.localPosition = new Vector3(x, y, 0f);

                // Wing-flutter horizontal pinch & facing tilt
                float dx = Mathf.Cos(ang);
                float wing = 0.58f + 0.42f * Mathf.Abs(Mathf.Sin(t * c.FlapRate + c.Phase));
                float dir = dx >= 0f ? 1f : -1f;
                c.Tr.localScale = new Vector3(dir * c.BaseScale * wing, c.BaseScale, 1f);
                c.Tr.localRotation = Quaternion.Euler(0f, 0f, -dx * 11f);
            }
        }
    }
}
