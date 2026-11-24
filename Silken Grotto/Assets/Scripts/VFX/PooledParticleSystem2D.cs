using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Lightweight, zero-GC pooled 2D sprite particle engine for ambient and gameplay particles
    /// (foliage/leaf debris with flutter drag, lime organic droplets, pale shell shards,
    /// movement wind streaks, jump/landing mist puffs, starburst flashes, and splash rings).
    /// </summary>
    public class PooledParticleSystem2D : MonoBehaviour
    {
        public struct ParticleParams
        {
            public Sprite Sprite;
            public Vector3 Position;
            public Vector2 Velocity;
            public float Gravity;
            public float Drag;
            public float Lifetime;
            public float StartScale;
            public float EndScale;
            public float ScaleXMultiplier;
            public float RotationDegrees;
            public float AngularVelocity;
            public float FlutterAmplitude;
            public float FlutterFrequency;
            public Color Color;
            public float FadePower;
            public bool AlignToVelocity;
            public bool UseAdditiveMaterial;
            public string SortingLayerName;
            public int SortingOrder;
        }

        private class ParticleSlot
        {
            public GameObject Go;
            public Transform Tr;
            public SpriteRenderer Sr;
            public bool Active;
            public float Age;
            public float Lifetime;
            public Vector2 Velocity;
            public float Gravity;
            public float Drag;
            public float StartScale;
            public float EndScale;
            public float ScaleXMult;
            public float Rotation;
            public float AngularVel;
            public float FlutterAmp;
            public float FlutterFreq;
            public float FlutterSeed;
            public Color BaseColor;
            public float FadePower;
            public bool AlignToVelocity;
        }

        [Header("Pool Configuration")]
        [SerializeField] private int poolSize = 200;
        [SerializeField] private Material defaultSpriteMaterial;
        [SerializeField] private Material additiveMaterial;

        private ParticleSlot[] _slots;
        private int _nextCursor;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void ConfigureMaterials(Material defaultMat, Material addMat)
        {
            if (defaultMat != null) defaultSpriteMaterial = defaultMat;
            if (addMat != null) additiveMaterial = addMat;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            int count = Mathf.Clamp(poolSize, 32, 400);
            _slots = new ParticleSlot[count];
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("P2D_" + i);
                go.transform.SetParent(transform, false);
                go.SetActive(false);

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = GameConstants.SortVFX;
                sr.sortingOrder = 11;

                ParticleSlot slot = new ParticleSlot();
                slot.Go = go;
                slot.Tr = go.transform;
                slot.Sr = sr;
                slot.Active = false;
                _slots[i] = slot;
            }
        }

        public void Emit(ParticleParams p)
        {
            if (p.Sprite == null) return;
            EnsureInitialized();

            ParticleSlot slot = AcquireSlot();
            if (slot == null) return;

            slot.Active = true;
            slot.Age = 0f;
            slot.Lifetime = Mathf.Max(0.04f, p.Lifetime);
            slot.Velocity = p.Velocity;
            slot.Gravity = p.Gravity;
            slot.Drag = Mathf.Max(0f, p.Drag);
            slot.StartScale = Mathf.Max(0.01f, p.StartScale);
            slot.EndScale = Mathf.Max(0.005f, p.EndScale);
            slot.ScaleXMult = Mathf.Approximately(p.ScaleXMultiplier, 0f) ? 1f : p.ScaleXMultiplier;
            slot.Rotation = p.RotationDegrees;
            slot.AngularVel = p.AngularVelocity;
            slot.FlutterAmp = p.FlutterAmplitude;
            slot.FlutterFreq = p.FlutterFrequency;
            slot.FlutterSeed = Random.Range(0f, 6.283f);
            slot.BaseColor = p.Color;
            slot.FadePower = p.FadePower > 0.1f ? p.FadePower : 1.2f;
            slot.AlignToVelocity = p.AlignToVelocity;

            slot.Tr.position = p.Position;
            slot.Tr.localScale = new Vector3(slot.StartScale * slot.ScaleXMult, slot.StartScale, 1f);
            slot.Tr.rotation = Quaternion.Euler(0f, 0f, slot.Rotation);

            slot.Sr.sprite = p.Sprite;
            slot.Sr.color = p.Color;
            slot.Sr.sortingLayerName = string.IsNullOrEmpty(p.SortingLayerName) ? GameConstants.SortVFX : p.SortingLayerName;
            slot.Sr.sortingOrder = p.SortingOrder;

            if (p.UseAdditiveMaterial && additiveMaterial != null)
            {
                slot.Sr.sharedMaterial = additiveMaterial;
            }
            else if (defaultSpriteMaterial != null)
            {
                slot.Sr.sharedMaterial = defaultSpriteMaterial;
            }

            slot.Go.SetActive(true);
        }

        private ParticleSlot AcquireSlot()
        {
            int len = _slots.Length;
            for (int i = 0; i < len; i++)
            {
                int idx = (_nextCursor + i) % len;
                if (!_slots[idx].Active)
                {
                    _nextCursor = (idx + 1) % len;
                    return _slots[idx];
                }
            }
            // Recycle oldest slot cleanly when pool is saturated
            ParticleSlot recycled = _slots[_nextCursor];
            _nextCursor = (_nextCursor + 1) % len;
            return recycled;
        }

        private void Update()
        {
            if (!_initialized) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                ParticleSlot s = _slots[i];
                if (!s.Active) continue;

                s.Age += dt;
                if (s.Age >= s.Lifetime)
                {
                    s.Active = false;
                    s.Go.SetActive(false);
                    continue;
                }

                float t = s.Age / s.Lifetime;

                // Gravity & aerodynamic drag
                s.Velocity.y -= s.Gravity * dt;
                if (s.Drag > 0f)
                {
                    float damp = Mathf.Clamp01(1f - s.Drag * dt);
                    s.Velocity *= damp;
                }

                Vector3 pos = s.Tr.position;
                pos.x += s.Velocity.x * dt;
                pos.y += s.Velocity.y * dt;

                // Organic leaf / mote fluttering sway
                if (s.FlutterAmp > 0.001f)
                {
                    float sway = Mathf.Sin(s.Age * s.FlutterFreq + s.FlutterSeed) * s.FlutterAmp;
                    pos.x += sway * dt;
                }
                s.Tr.position = pos;

                // Rotation
                if (s.AlignToVelocity && s.Velocity.sqrMagnitude > 0.04f)
                {
                    float ang = Mathf.Atan2(s.Velocity.y, s.Velocity.x) * Mathf.Rad2Deg;
                    s.Tr.rotation = Quaternion.Euler(0f, 0f, ang);
                }
                else if (!Mathf.Approximately(s.AngularVel, 0f))
                {
                    s.Rotation += s.AngularVel * dt;
                    s.Tr.rotation = Quaternion.Euler(0f, 0f, s.Rotation);
                }

                // Scale & rapid tapered alpha fade
                float curScale = Mathf.Lerp(s.StartScale, s.EndScale, t);
                s.Tr.localScale = new Vector3(curScale * s.ScaleXMult, curScale, 1f);

                float alphaFactor = Mathf.Pow(Mathf.Clamp01(1f - t), s.FadePower);
                Color c = s.BaseColor;
                c.a = s.BaseColor.a * alphaFactor;
                s.Sr.color = c;
            }
        }
    }
}
