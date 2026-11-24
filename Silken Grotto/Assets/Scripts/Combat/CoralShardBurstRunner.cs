using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// 100% code-driven 2D ballistic particle runner for Coral Furm's crunchy coral shards
    /// (red-orange / reef-white jagged chunks under real gravity) and Pharlid's falling ceiling dust.
    /// Requires zero external image files.
    /// </summary>
    public class CoralShardBurstRunner : MonoBehaviour
    {
        private struct ShardParticle
        {
            public Transform Root;
            public LineRenderer Line;
            public Vector2 Velocity;
            public float SpinDegPerSec;
            public float AngleDeg;
            public Color BaseColor;
            public float BaseWidth;
            public bool HasBounced;
        }

        private ShardParticle[] _particles;
        private float _gravity = 19.5f;
        private float _duration = 0.65f;
        private float _elapsed;
        private LayerMask _floorMask;
        private bool _enableFloorBounce;

        public void InitializeShards(
            Vector3 origin,
            int count,
            float minSpeed,
            float maxSpeed,
            float upBias,
            float gravity,
            float duration,
            bool enableFloorBounce,
            Color[] palette,
            float shardSizeMin,
            float shardSizeMax)
        {
            transform.position = origin;
            _gravity = gravity;
            _duration = Mathf.Max(0.15f, duration);
            _elapsed = 0f;
            _enableFloorBounce = enableFloorBounce;
            _floorMask = (1 << GameConstants.LayerGround) | (1 << GameConstants.LayerOneWayPlatform) | (1 << GameConstants.LayerBreakableWall);

            int safeCount = Mathf.Clamp(count, 2, 24);
            _particles = new ShardParticle[safeCount];
            Material mat = ChoralCodeVFX.GetLineMaterial();

            for (int i = 0; i < safeCount; i++)
            {
                GameObject pGo = new GameObject("Shard_" + i);
                pGo.transform.SetParent(transform, false);
                pGo.transform.localPosition = new Vector3(Random.Range(-0.18f, 0.18f), Random.Range(-0.14f, 0.16f), 0f);

                LineRenderer lr = pGo.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.sortingLayerName = GameConstants.SortVFX;
                lr.sortingOrder = 18;
                if (mat != null) lr.sharedMaterial = mat;

                Color col = (palette != null && palette.Length > 0)
                    ? palette[i % palette.Length]
                    : new Color(0.95f, 0.42f, 0.32f, 1f);

                float sz = Random.Range(shardSizeMin, shardSizeMax);
                if (enableFloorBounce)
                {
                    // Jagged 4-point coral polygon chunk
                    lr.positionCount = 4;
                    lr.startWidth = sz * 0.62f;
                    lr.endWidth = sz * 0.28f;
                    lr.SetPosition(0, new Vector3(-sz, -sz * 0.45f, 0f));
                    lr.SetPosition(1, new Vector3(0f, sz * 0.92f, 0f));
                    lr.SetPosition(2, new Vector3(sz * 0.88f, -sz * 0.35f, 0f));
                    lr.SetPosition(3, new Vector3(-sz * 0.85f, -sz * 0.42f, 0f));
                }
                else
                {
                    // Vertical grit/dust streak for Pharlid ceiling DropWarning
                    lr.positionCount = 2;
                    lr.startWidth = sz * 0.55f;
                    lr.endWidth = sz * 0.22f;
                    lr.SetPosition(0, new Vector3(0f, sz * 1.1f, 0f));
                    lr.SetPosition(1, new Vector3(0f, -sz * 1.1f, 0f));
                }

                lr.startColor = col;
                lr.endColor = col;

                float angRad = enableFloorBounce
                    ? Random.Range(18f, 162f) * Mathf.Deg2Rad
                    : Random.Range(245f, 295f) * Mathf.Deg2Rad;
                float spd = Random.Range(minSpeed, maxSpeed);
                Vector2 vel = new Vector2(Mathf.Cos(angRad) * spd, Mathf.Sin(angRad) * spd + upBias);

                _particles[i] = new ShardParticle
                {
                    Root = pGo.transform,
                    Line = lr,
                    Velocity = vel,
                    SpinDegPerSec = enableFloorBounce ? Random.Range(-540f, 540f) : Random.Range(-45f, 45f),
                    AngleDeg = Random.Range(0f, 360f),
                    BaseColor = col,
                    BaseWidth = lr.startWidth,
                    HasBounced = false
                };
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _elapsed += dt;
            float u = Mathf.Clamp01(_elapsed / _duration);
            if (u >= 1f || _particles == null)
            {
                Destroy(gameObject);
                return;
            }

            float alpha = Mathf.Pow(1f - u, 1.45f);
            for (int i = 0; i < _particles.Length; i++)
            {
                ShardParticle p = _particles[i];
                if (p.Root == null || p.Line == null) continue;

                p.Velocity.y -= _gravity * dt;
                Vector3 prevPos = p.Root.position;
                Vector3 nextPos = prevPos + (Vector3)(p.Velocity * dt);

                if (_enableFloorBounce && !p.HasBounced && p.Velocity.y < 0f)
                {
                    float fallDist = Mathf.Abs(p.Velocity.y * dt) + 0.08f;
                    RaycastHit2D hit = Physics2D.Raycast(prevPos, Vector2.down, fallDist, _floorMask);
                    if (hit.collider != null)
                    {
                        nextPos.y = hit.point.y + 0.05f;
                        p.Velocity.y = Mathf.Abs(p.Velocity.y) * 0.36f;
                        p.Velocity.x *= 0.62f;
                        p.SpinDegPerSec *= 0.45f;
                        p.HasBounced = true;
                    }
                }

                p.Root.position = nextPos;
                p.AngleDeg += p.SpinDegPerSec * dt;
                p.Root.localRotation = Quaternion.Euler(0f, 0f, p.AngleDeg);

                Color c = new Color(p.BaseColor.r, p.BaseColor.g, p.BaseColor.b, p.BaseColor.a * alpha);
                p.Line.startColor = c;
                p.Line.endColor = c;
                p.Line.startWidth = p.BaseWidth * Mathf.Lerp(1f, 0.35f, u);
                p.Line.endWidth = p.BaseWidth * Mathf.Lerp(0.55f, 0.12f, u);

                _particles[i] = p;
            }
        }
    }
}
