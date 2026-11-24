using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Studio-grade pooled 2D VFX & Combat Feedback Manager:
    /// - Razor-sharp pure-white (#FFFFFF) crescent slash trails (80ms-120ms lifetime)
    /// - Multi-ray white impact starburst flash (4-6 elongated light spikes)
    /// - Thin pale-white movement air/wind streaks (jump, dash, fast downward drop)
    /// - Hand-drawn dark-green/olive foliage & leaf debris burst (10-18 spinning fluttering leaves)
    /// - Enemy hit/defeat glowing lime-green (#9BE840) organic droplets + pale shell shards
    /// - Soft pale mist puffs & dirt/moss specks for jump & landing dust
    /// - Ceiling water drip splash rings/ripples
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Combat & Lighting VFX Sprites")]
        [SerializeField] private Sprite slashArcSprite;
        [SerializeField] private Sprite impactStarburstSprite;
        [SerializeField] private Sprite hitSparkSprite;
        [SerializeField] private Sprite windStreakSprite;
        [SerializeField] private Sprite threadGuardSprite;
        [SerializeField] private Sprite healRibbonSprite;

        [Header("Pooled Particle Sprites")]
        [SerializeField] private Sprite softParticleSprite;
        [SerializeField] private Sprite mossLeafSprite;
        [SerializeField] private Sprite organicDropletSprite;
        [SerializeField] private Sprite shellShardSprite;
        [SerializeField] private Sprite dustPuffSprite;
        [SerializeField] private Sprite mossSpeckSprite;
        [SerializeField] private Sprite splashRingSprite;

        [Header("Hand-Drawn Animated Player VFX Sheets")]
        [SerializeField] private Sprite[] dashBurstFrames;
        [SerializeField] private Sprite[] floorDashDustFrames;
        [SerializeField] private Sprite[] heroLandingWaveFrames;

        [Header("Hand-Drawn Shared Enemy VFX Sheet (5x6 @ 192x192)")]
        [SerializeField] private Sprite[] enemyLaserVfxFrames;
        [SerializeField] private Sprite[] enemyIceSpikesVfxFrames;
        [SerializeField] private Sprite[] enemyDriznitVfxFrames;
        [SerializeField] private Sprite[] enemyBellVfxFrames;
        [SerializeField] private Sprite[] enemyHelmetDizzyVfxFrames;

        private Sprite[] _laserChargeFrames;
        private Sprite[] _laserBeamFrames;
        private Sprite[] _laserDissipateFrames;
        private Sprite[] _iceSpikesEmergingFrames;
        private Sprite[] _iceSpikesActiveFrames;
        private Sprite[] _iceShatterFrames;
        private Sprite[] _driznitSpinFrames;
        private Sprite[] _driznitRicochetFrames;
        private Sprite[] _bellSpinFrames;
        private Sprite[] _bellImpactFrames;
        private Sprite[] _helmetClangFrames;
        private Sprite[] _dizzyLoopFrames;

        public bool HasSharedEnemyVfxSheet
        {
            get
            {
                EnsureEnemyVfxSlicesCached();
                return _laserBeamFrames != null && _laserBeamFrames.Length > 0 && _laserBeamFrames[0] != null;
            }
        }

        [Header("Materials & Pool")]
        [SerializeField] private Material defaultSpriteMaterial;
        [SerializeField] private Material additiveVfxMaterial;
        [SerializeField] private PooledParticleSystem2D particlePool;

        [Header("Optional VFX Prefabs")]
        [SerializeField] private GameObject hitImpactPrefab;
        [SerializeField] private GameObject enemyDeathPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (particlePool == null)
            {
                particlePool = GetComponent<PooledParticleSystem2D>();
                if (particlePool == null)
                {
                    particlePool = gameObject.AddComponent<PooledParticleSystem2D>();
                }
            }
            particlePool.ConfigureMaterials(defaultSpriteMaterial, additiveVfxMaterial);
            EnsureEnemyVfxSlicesCached();
        }

        private static Sprite[] SliceFrames(Sprite[] src, int start, int count)
        {
            if (src == null || src.Length < start + count) return null;
            Sprite[] res = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                if (src[start + i] == null) return null;
                res[i] = src[start + i];
            }
            return res;
        }

        private void EnsureEnemyVfxSlicesCached()
        {
            if (_laserChargeFrames == null && enemyLaserVfxFrames != null && enemyLaserVfxFrames.Length >= 6)
            {
                _laserChargeFrames = SliceFrames(enemyLaserVfxFrames, 0, 2);
                _laserBeamFrames = SliceFrames(enemyLaserVfxFrames, 2, 3);
                _laserDissipateFrames = SliceFrames(enemyLaserVfxFrames, 5, 1);
            }
            if (_iceSpikesEmergingFrames == null && enemyIceSpikesVfxFrames != null && enemyIceSpikesVfxFrames.Length >= 6)
            {
                _iceSpikesEmergingFrames = SliceFrames(enemyIceSpikesVfxFrames, 0, 2);
                _iceSpikesActiveFrames = SliceFrames(enemyIceSpikesVfxFrames, 2, 2);
                _iceShatterFrames = SliceFrames(enemyIceSpikesVfxFrames, 4, 2);
            }
            if (_driznitSpinFrames == null && enemyDriznitVfxFrames != null && enemyDriznitVfxFrames.Length >= 6)
            {
                _driznitSpinFrames = SliceFrames(enemyDriznitVfxFrames, 0, 4);
                _driznitRicochetFrames = SliceFrames(enemyDriznitVfxFrames, 4, 2);
            }
            if (_bellSpinFrames == null && enemyBellVfxFrames != null && enemyBellVfxFrames.Length >= 6)
            {
                _bellSpinFrames = SliceFrames(enemyBellVfxFrames, 0, 4);
                _bellImpactFrames = SliceFrames(enemyBellVfxFrames, 4, 2);
            }
            if (_helmetClangFrames == null && enemyHelmetDizzyVfxFrames != null && enemyHelmetDizzyVfxFrames.Length >= 6)
            {
                _helmetClangFrames = SliceFrames(enemyHelmetDizzyVfxFrames, 0, 2);
                _dizzyLoopFrames = SliceFrames(enemyHelmetDizzyVfxFrames, 2, 4);
            }
        }

        public Sprite[] GetLaserChargeFrames() { EnsureEnemyVfxSlicesCached(); return _laserChargeFrames; }
        public Sprite[] GetLaserBeamFrames() { EnsureEnemyVfxSlicesCached(); return _laserBeamFrames; }
        public Sprite[] GetLaserDissipateFrames() { EnsureEnemyVfxSlicesCached(); return _laserDissipateFrames; }
        public Sprite[] GetIceSpikesEmergingFrames() { EnsureEnemyVfxSlicesCached(); return _iceSpikesEmergingFrames; }
        public Sprite[] GetIceSpikesActiveFrames() { EnsureEnemyVfxSlicesCached(); return _iceSpikesActiveFrames; }
        public Sprite[] GetIceShatterFrames() { EnsureEnemyVfxSlicesCached(); return _iceShatterFrames; }
        public Sprite[] GetDriznitSpinFrames() { EnsureEnemyVfxSlicesCached(); return _driznitSpinFrames; }
        public Sprite[] GetDriznitRicochetFrames() { EnsureEnemyVfxSlicesCached(); return _driznitRicochetFrames; }
        public Sprite[] GetBellSpinFrames() { EnsureEnemyVfxSlicesCached(); return _bellSpinFrames; }
        public Sprite[] GetBellImpactFrames() { EnsureEnemyVfxSlicesCached(); return _bellImpactFrames; }
        public Sprite[] GetHelmetClangFrames() { EnsureEnemyVfxSlicesCached(); return _helmetClangFrames; }
        public Sprite[] GetDizzyLoopFrames() { EnsureEnemyVfxSlicesCached(); return _dizzyLoopFrames; }
        public Material DefaultSpriteMaterial { get { return defaultSpriteMaterial; } }

        public bool SpawnLaserDissipate(Vector3 origin, Vector2 dir, float beamLength)
        {
            Sprite[] frames = GetLaserDissipateFrames();
            if (frames == null || frames.Length == 0) return false;
            Vector2 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            float angleDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float sx = Mathf.Max(0.25f, beamLength / ENEMY_VFX_CONFIG.CellWorldSize);
            GameObject go = new GameObject("EnemyVFX_LaserDissipate");
            go.transform.position = origin + (Vector3)(d * (beamLength * 0.5f));
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            go.transform.localScale = new Vector3(sx, ENEMY_VFX_CONFIG.ServitorLaserDissipate.DefaultScale, 1f);
            SpriteSheetVfxPlayer player = go.AddComponent<SpriteSheetVfxPlayer>();
            player.ConfigureSorting(ENEMY_VFX_CONFIG.ServitorLaserDissipate.SortingOrder, defaultSpriteMaterial);
            player.Play(frames, ENEMY_VFX_CONFIG.ServitorLaserDissipate.Fps, false, true, 0.11f);
            return true;
        }

        public bool SpawnIceSpikeShatter(Vector3 position, float scaleMultiplier)
        {
            Sprite[] frames = GetIceShatterFrames();
            if (frames == null || frames.Length == 0) return false;
            ENEMY_VFX_CONFIG.VfxClipMeta meta = ENEMY_VFX_CONFIG.MnemonIceShatter;
            SpawnOneShotEnemyVfx("EnemyVFX_IceShatter", frames, position, Random.Range(0f, 360f),
                meta.DefaultScale * Mathf.Max(0.4f, scaleMultiplier), meta.Fps, meta.SortingOrder, 0.14f);
            return true;
        }

        public bool SpawnDriznitRicochetSpark(Vector3 position, Vector2 surfaceNormal)
        {
            Sprite[] frames = GetDriznitRicochetFrames();
            if (frames == null || frames.Length == 0) return false;
            ENEMY_VFX_CONFIG.VfxClipMeta meta = ENEMY_VFX_CONFIG.DriznitRicochetSpark;
            float rot = surfaceNormal.sqrMagnitude > 0.001f
                ? Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg
                : Random.Range(0f, 360f);
            SpawnOneShotEnemyVfx("EnemyVFX_DriznitRicochet", frames, position, rot,
                meta.DefaultScale, meta.Fps, meta.SortingOrder, 0.12f);
            return true;
        }

        public bool SpawnBellImpactSonic(Vector3 position)
        {
            Sprite[] frames = GetBellImpactFrames();
            if (frames == null || frames.Length == 0) return false;
            ENEMY_VFX_CONFIG.VfxClipMeta meta = ENEMY_VFX_CONFIG.BellbearerImpactSonic;
            SpawnOneShotEnemyVfx("EnemyVFX_BellImpactSonic", frames, position, Random.Range(-15f, 15f),
                meta.DefaultScale, meta.Fps, meta.SortingOrder, 0.14f);
            return true;
        }

        public bool SpawnHelmetClangSpark(Vector3 position, int normalDirX)
        {
            Sprite[] frames = GetHelmetClangFrames();
            if (frames == null || frames.Length == 0) return false;
            ENEMY_VFX_CONFIG.VfxClipMeta meta = ENEMY_VFX_CONFIG.WingedFurmHelmetClang;
            float rot = normalDirX >= 0 ? Random.Range(-18f, 18f) : (180f + Random.Range(-18f, 18f));
            SpawnOneShotEnemyVfx("EnemyVFX_HelmetClang", frames, position, rot,
                meta.DefaultScale, meta.Fps, meta.SortingOrder, 0.12f);
            return true;
        }

        private void SpawnOneShotEnemyVfx(string objName, Sprite[] frames, Vector3 position, float rotDeg, float scale, float fps, int sortingOrder, float minHoldSeconds)
        {
            GameObject go = new GameObject(objName);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotDeg);
            go.transform.localScale = Vector3.one * scale;
            SpriteSheetVfxPlayer player = go.AddComponent<SpriteSheetVfxPlayer>();
            player.ConfigureSorting(sortingOrder, defaultSpriteMaterial);
            player.Play(frames, fps, false, true, minHoldSeconds);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Spawns a razor-sharp crescent slash trail with crisp 80ms-120ms lifetime and rapid tapered fade-out.
        /// </summary>
        public void SpawnSlashEffect(Vector3 position, float facingSign, float angleDegrees, Color tint, float scale = 1f)
        {
            if (slashArcSprite == null) return;

            PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
            p.Sprite = slashArcSprite;
            p.Position = position;
            p.Velocity = new Vector2((facingSign < 0f ? -1f : 1f) * 1.4f, 0f);
            p.Gravity = 0f;
            p.Drag = 8f;
            p.Lifetime = Random.Range(0.088f, 0.115f); // Strictly within 80ms-120ms spec!
            p.StartScale = Mathf.Abs(scale);
            p.EndScale = Mathf.Abs(scale) * 1.14f;
            p.ScaleXMultiplier = facingSign < 0f ? -1f : 1f;
            p.RotationDegrees = angleDegrees;
            p.AngularVelocity = 0f;
            p.Color = tint;
            p.FadePower = 1.85f;
            p.UseAdditiveMaterial = true;
            p.SortingLayerName = GameConstants.SortVFX;
            p.SortingOrder = 14;
            particlePool.Emit(p);
        }

        /// <summary>
        /// Default overload preserving full backward compatibility.
        /// </summary>
        public void SpawnHitImpact(Vector3 position, Color tint)
        {
            SpawnHitImpact(position, tint, false, false);
        }

        /// <summary>
        /// Triggers a sharp multi-ray pure-white starburst flash, plus optional glowing lime-green
        /// organic droplets (#9BE840) & pale shell shards on enemy hits or metallic sparks on armor/stone.
        /// </summary>
        public void SpawnHitImpact(Vector3 position, Color tint, bool isEnemyOrganicHit, bool isMetallicHit)
        {
            if (hitImpactPrefab != null)
            {
                GameObject obj = Instantiate(hitImpactPrefab, position, Quaternion.identity);
                Destroy(obj, 0.8f);
            }

            Sprite star = impactStarburstSprite != null ? impactStarburstSprite : hitSparkSprite;
            if (star != null)
            {
                // Sharp pure-white multi-ray starburst flash (4-6 elongated light spikes)
                PooledParticleSystem2D.ParticleParams flash = new PooledParticleSystem2D.ParticleParams();
                flash.Sprite = star;
                flash.Position = position;
                flash.Velocity = Vector2.zero;
                flash.Gravity = 0f;
                flash.Drag = 0f;
                flash.Lifetime = 0.095f;
                flash.StartScale = 0.42f;
                flash.EndScale = 0.88f;
                flash.ScaleXMultiplier = 1f;
                flash.RotationDegrees = Random.Range(0f, 360f);
                flash.AngularVelocity = Random.Range(-45f, 45f);
                flash.Color = Color.white;
                flash.FadePower = 2.1f;
                flash.UseAdditiveMaterial = true;
                flash.SortingLayerName = GameConstants.SortVFX;
                flash.SortingOrder = 18;
                particlePool.Emit(flash);
            }

            if (isEnemyOrganicHit)
            {
                SpawnEnemyHitDropletsAndShards(position, 6, 4);
            }
            else if (isMetallicHit)
            {
                // Crisp pale-white/silver metallic clink sparks
                Sprite streak = windStreakSprite != null ? windStreakSprite : softParticleSprite;
                for (int i = 0; i < 6; i++)
                {
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float spd = Random.Range(4.8f, 9.2f);
                    PooledParticleSystem2D.ParticleParams sp = new PooledParticleSystem2D.ParticleParams();
                    sp.Sprite = streak;
                    sp.Position = position;
                    sp.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd);
                    sp.Gravity = 9.5f;
                    sp.Drag = 3.5f;
                    sp.Lifetime = Random.Range(0.14f, 0.24f);
                    sp.StartScale = Random.Range(0.18f, 0.30f);
                    sp.EndScale = 0.05f;
                    sp.Color = new Color(0.96f, 0.99f, 1f, 0.95f);
                    sp.FadePower = 1.6f;
                    sp.AlignToVelocity = true;
                    sp.UseAdditiveMaterial = true;
                    sp.SortingLayerName = GameConstants.SortVFX;
                    sp.SortingOrder = 16;
                    particlePool.Emit(sp);
                }
            }
            else
            {
                SpawnSoftBurst(position, tint, 7, 2.4f, 5.4f, 0.26f, 0.16f, true);
            }
        }

        /// <summary>
        /// Emits thin pale-white air/wind streaks trailing jumps, dashes, and fast downward drops.
        /// </summary>
        public void SpawnMovementStreaks(Vector3 position, Vector2 travelDirection, int count = 3, float spread = 0.45f)
        {
            Sprite streak = windStreakSprite != null ? windStreakSprite : softParticleSprite;
            if (streak == null) return;

            Vector2 dir = travelDirection.sqrMagnitude > 0.001f ? travelDirection.normalized : Vector2.right;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            for (int i = 0; i < count; i++)
            {
                float lat = Random.Range(-spread, spread);
                float back = Random.Range(-0.25f, 0.18f);
                Vector3 spawnPos = position + new Vector3(perp.x * lat + dir.x * back, perp.y * lat + dir.y * back, 0f);

                PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                p.Sprite = streak;
                p.Position = spawnPos;
                p.Velocity = -dir * Random.Range(2.2f, 4.8f);
                p.Gravity = 0f;
                p.Drag = 4.5f;
                p.Lifetime = Random.Range(0.11f, 0.19f);
                p.StartScale = Random.Range(0.32f, 0.56f);
                p.EndScale = p.StartScale * 0.65f;
                p.ScaleXMultiplier = Random.Range(1.0f, 1.45f);
                p.RotationDegrees = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                p.Color = new Color(0.95f, 0.99f, 0.97f, Random.Range(0.55f, 0.85f));
                p.FadePower = 1.7f;
                p.AlignToVelocity = false;
                p.UseAdditiveMaterial = true;
                p.SortingLayerName = GameConstants.SortVFX;
                p.SortingOrder = 9;
                particlePool.Emit(p);
            }
        }

        /// <summary>
        /// Emits 10-18 tiny hand-drawn dark-green/olive leaf fragments that burst upward,
        /// spin, and flutter down with aerodynamic drag (dash, hard land, or slashing mossy terrain).
        /// </summary>
        public void SpawnFoliageLeafBurst(Vector3 position, int count = 14, float horizontalBias = 0f)
        {
            Sprite leaf = mossLeafSprite != null ? mossLeafSprite : mossSpeckSprite;
            if (leaf == null) return;

            int finalCount = Mathf.Clamp(count, 10, 18);
            for (int i = 0; i < finalCount; i++)
            {
                float vx = Random.Range(-3.4f, 3.4f) + horizontalBias * Random.Range(0.6f, 2.2f);
                float vy = Random.Range(2.2f, 5.8f);

                // Hand-drawn dark-green / olive / bright moss palette variation
                int shade = i % 4;
                Color leafCol;
                if (shade == 0) leafCol = new Color(0.32f, 0.58f, 0.24f, 0.96f);       // deep moss
                else if (shade == 1) leafCol = new Color(0.52f, 0.72f, 0.28f, 0.96f);  // lush moss
                else if (shade == 2) leafCol = new Color(0.45f, 0.52f, 0.34f, 0.94f);  // olive leaf
                else leafCol = new Color(0.64f, 0.84f, 0.34f, 0.95f);                  // bright highlight

                PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                p.Sprite = leaf;
                p.Position = position + new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.02f, 0.35f), 0f);
                p.Velocity = new Vector2(vx, vy);
                p.Gravity = Random.Range(4.8f, 7.2f);
                p.Drag = Random.Range(2.4f, 3.8f);
                p.Lifetime = Random.Range(0.75f, 1.35f);
                p.StartScale = Random.Range(0.14f, 0.26f);
                p.EndScale = p.StartScale * 0.72f;
                p.ScaleXMultiplier = Random.value > 0.5f ? 1f : -1f;
                p.RotationDegrees = Random.Range(0f, 360f);
                p.AngularVelocity = Random.Range(-360f, 360f);
                p.FlutterAmplitude = Random.Range(0.85f, 1.95f);
                p.FlutterFrequency = Random.Range(6.5f, 11.5f);
                p.Color = leafCol;
                p.FadePower = 2.4f;
                p.UseAdditiveMaterial = false;
                p.SortingLayerName = GameConstants.SortVFX;
                p.SortingOrder = 12;
                particlePool.Emit(p);
            }
        }

        /// <summary>
        /// Spawns a crisp pale-white expanding splash ring/ripple and tiny droplets on ceiling water drip impact.
        /// </summary>
        public void SpawnWaterSplashRing(Vector3 surfacePosition)
        {
            if (splashRingSprite != null)
            {
                PooledParticleSystem2D.ParticleParams ring = new PooledParticleSystem2D.ParticleParams();
                ring.Sprite = splashRingSprite;
                ring.Position = surfacePosition;
                ring.Velocity = Vector2.zero;
                ring.Gravity = 0f;
                ring.Drag = 0f;
                ring.Lifetime = 0.28f;
                ring.StartScale = 0.14f;
                ring.EndScale = 0.42f;
                ring.Color = new Color(0.92f, 1f, 0.96f, 0.85f);
                ring.FadePower = 1.6f;
                ring.UseAdditiveMaterial = true;
                ring.SortingLayerName = GameConstants.SortVFX;
                ring.SortingOrder = 7;
                particlePool.Emit(ring);
            }

            Sprite speck = softParticleSprite;
            if (speck != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    PooledParticleSystem2D.ParticleParams drop = new PooledParticleSystem2D.ParticleParams();
                    drop.Sprite = speck;
                    drop.Position = surfacePosition + new Vector3(Random.Range(-0.12f, 0.12f), 0.04f, 0f);
                    drop.Velocity = new Vector2(Random.Range(-1.1f, 1.1f), Random.Range(1.4f, 2.8f));
                    drop.Gravity = 11f;
                    drop.Drag = 1.2f;
                    drop.Lifetime = 0.24f;
                    drop.StartScale = 0.07f;
                    drop.EndScale = 0.02f;
                    drop.Color = new Color(0.90f, 1f, 0.96f, 0.78f);
                    drop.FadePower = 1.4f;
                    drop.UseAdditiveMaterial = true;
                    drop.SortingLayerName = GameConstants.SortVFX;
                    drop.SortingOrder = 8;
                    particlePool.Emit(drop);
                }
            }
        }

        public void SpawnJumpDust(Vector3 feetPosition)
        {
            SpawnMistAndSpecks(feetPosition, 4, 4, 1.4f, 2.8f, 0.26f);
            SpawnMovementStreaks(feetPosition + Vector3.up * 0.45f, Vector2.up, 2, 0.35f);
        }

        public void SpawnLandingDust(Vector3 feetPosition)
        {
            SpawnLandingDust(feetPosition, false, true);
        }

        public void SpawnLandingDust(Vector3 feetPosition, bool isHardLanding, bool isMossySurface)
        {
            SpawnMistAndSpecks(feetPosition, isHardLanding ? 7 : 5, isHardLanding ? 8 : 5, 1.8f, isHardLanding ? 4.4f : 3.2f, isHardLanding ? 0.36f : 0.28f);
            if (isHardLanding)
            {
                PlayAnimatedVfxStrip(heroLandingWaveFrames, feetPosition, 1f, 1.28f, 20f, 15);
            }
            if (isHardLanding && isMossySurface)
            {
                SpawnFoliageLeafBurst(feetPosition + Vector3.up * 0.12f, 12, 0f);
            }
        }

        public void SpawnDoubleJumpRing(Vector3 feetPosition)
        {
            SpawnWaterSplashRing(feetPosition + Vector3.up * 0.08f);
            SpawnSoftBurst(feetPosition, new Color(0.82f, 0.99f, 0.92f, 0.92f), 10, 2.2f, 4.2f, 0.32f, 0.18f, true);
            SpawnMovementStreaks(feetPosition + Vector3.up * 0.5f, Vector2.up, 3, 0.42f);
        }

        public void SpawnDashEffect(Vector3 position, float facingSign)
        {
            SpawnDashEffect(position, facingSign, false, false);
        }

        public void SpawnDashEffect(Vector3 position, float facingSign, bool groundedOnMoss)
        {
            SpawnDashEffect(position, facingSign, groundedOnMoss, groundedOnMoss);
        }

        public void SpawnDashEffect(Vector3 position, float facingSign, bool isGrounded, bool groundedOnMoss)
        {
            float sign = facingSign >= 0f ? 1f : -1f;
            Vector2 dir = new Vector2(sign, 0f);
            SpawnMovementStreaks(position, dir, 4, 0.48f);
            PlayAnimatedVfxStrip(dashBurstFrames, position - new Vector3(sign * 0.65f, 0f, 0f), sign, 1.15f, 24f, 15);
            if (isGrounded)
            {
                PlayAnimatedVfxStrip(floorDashDustFrames, position - new Vector3(sign * 0.25f, 0.90f, 0f), sign, 1.05f, 20f, 14);
            }
            SpawnSoftBurst(position, new Color(0.95f, 0.98f, 0.95f, 0.48f), 5, 1.6f, 3.6f, 0.20f, 0.14f, true);

            if (groundedOnMoss)
            {
                SpawnFoliageLeafBurst(position - Vector3.up * 0.75f, 12, -dir.x * 1.4f);
            }
        }

        private void PlayAnimatedVfxStrip(Sprite[] frames, Vector3 position, float facingSign, float uniformScale, float fps, int sortOrder)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return;
            StartCoroutine(AnimatedVfxStripRoutine(frames, position, facingSign >= 0f ? 1f : -1f, uniformScale, Mathf.Max(1f, fps), sortOrder));
        }

        private IEnumerator AnimatedVfxStripRoutine(Sprite[] frames, Vector3 position, float flipX, float uniformScale, float fps, int sortOrder)
        {
            GameObject go = new GameObject("HandDrawnVFX");
            go.transform.position = position;
            go.transform.localScale = new Vector3(flipX * uniformScale, uniformScale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (defaultSpriteMaterial != null) sr.sharedMaterial = defaultSpriteMaterial;
            sr.sortingLayerName = GameConstants.SortVFX;
            sr.sortingOrder = sortOrder;

            float frameTime = 1f / fps;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null) sr.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
            if (go != null) Destroy(go);
        }

        public void SpawnEnemyDeath(Vector3 position)
        {
            if (enemyDeathPrefab != null)
            {
                GameObject obj = Instantiate(enemyDeathPrefab, position, Quaternion.identity);
                Destroy(obj, 1.5f);
            }

            // Crisp central starburst + rich burst of glowing lime-green (#9BE840) droplets & pale shell shards
            SpawnHitImpact(position, Color.white, false, false);
            SpawnEnemyHitDropletsAndShards(position, 15, 12);
        }

        public void SpawnPropBreakDebris(Vector3 centerPos, BreakablePropKind kind, float dirBiasX)
        {
            float bias = Mathf.Clamp(dirBiasX, -1.5f, 1.5f);
            if (kind == BreakablePropKind.TallGrass)
            {
                SpawnFoliageLeafBurst(centerPos, 14, bias * 1.4f);
                return;
            }

            if (kind == BreakablePropKind.BriarBramble)
            {
                SpawnFoliageLeafBurst(centerPos, 16, bias * 1.3f);
                SpawnSoftBurst(centerPos, new Color(0.70f, 0.92f, 0.48f, 0.88f), 7, 1.6f, 4.2f, 0.24f, 0.16f, false);
                return;
            }

            bool isMetallic = kind == BreakablePropKind.CrackedBell
                || kind == BreakablePropKind.RustedGear
                || kind == BreakablePropKind.RustedChain
                || kind == BreakablePropKind.BrassCandelabra
                || kind == BreakablePropKind.OldLantern;
            bool isIce = kind == BreakablePropKind.IceCrystalCluster || kind == BreakablePropKind.FrozenWoodPost;
            bool isSilk = kind == BreakablePropKind.SilkSpool || kind == BreakablePropKind.DrySilkCocoon || kind == BreakablePropKind.WebBarricade;
            bool isShellwood = kind == BreakablePropKind.HollowBugShell || kind == BreakablePropKind.FragileWoodPillar;
            bool isSandDesert = kind == BreakablePropKind.DryCoralFormation || kind == BreakablePropKind.AncientBonePile;
            bool isRoyalGold = kind == BreakablePropKind.GildedPalaceUrn || kind == BreakablePropKind.CeremonialUrn || kind == BreakablePropKind.CrackedSentinelStatue;

            SpawnHitImpact(centerPos, Color.white, false, isMetallic || isIce || kind == BreakablePropKind.AncientUrn);

            Sprite shard = shellShardSprite != null ? shellShardSprite : mossSpeckSprite;
            if (shard != null)
            {
                int count = isIce || isMetallic ? 13 : 11;
                for (int i = 0; i < count; i++)
                {
                    Color tint = ResolvePropShardColor(kind, i, isMetallic, isIce, isSilk, isShellwood, isSandDesert, isRoyalGold);

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = shard;
                    p.Position = centerPos + new Vector3(Random.Range(-0.26f, 0.26f), Random.Range(-0.18f, 0.28f), 0f);
                    p.Velocity = new Vector2(Random.Range(-4.4f, 4.4f) + bias * 1.8f, Random.Range(2.4f, 6.8f));
                    p.Gravity = isSilk ? Random.Range(3.2f, 5.5f) : Random.Range(11.5f, 15.5f);
                    p.Drag = isSilk ? Random.Range(2.6f, 4.0f) : Random.Range(1.0f, 2.2f);
                    p.Lifetime = Random.Range(0.42f, 0.78f);
                    p.StartScale = Random.Range(0.20f, 0.36f);
                    p.EndScale = p.StartScale * 0.58f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-540f, 540f);
                    p.Color = tint;
                    p.FadePower = 1.7f;
                    p.UseAdditiveMaterial = isIce && (i % 3 == 0);
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 12;
                    particlePool.Emit(p);
                }
            }

            if (isShellwood)
            {
                SpawnSoftBurst(centerPos, new Color(0.68f, 0.44f, 0.98f, 0.90f), 10, 1.5f, 4.5f, 0.38f, 0.22f, true);
            }
            else if (isSandDesert)
            {
                SpawnSoftBurst(centerPos, new Color(0.84f, 0.66f, 0.42f, 0.72f), 12, 1.8f, 5.0f, 0.42f, 0.30f, false);
            }
            else if (isIce)
            {
                SpawnSoftBurst(centerPos, new Color(0.76f, 0.94f, 1.0f, 0.94f), 12, 2.0f, 5.4f, 0.34f, 0.18f, true);
            }
            else if (isSilk)
            {
                SpawnSoftBurst(centerPos, new Color(0.94f, 0.96f, 1.0f, 0.85f), 10, 1.2f, 3.6f, 0.45f, 0.22f, true);
            }
            else if (isMetallic || isRoyalGold)
            {
                SpawnSoftBurst(centerPos, new Color(1.0f, 0.86f, 0.48f, 0.92f), 10, 2.2f, 5.8f, 0.30f, 0.16f, true);
            }
            else
            {
                SpawnFoliageLeafBurst(centerPos, 8, bias);
                SpawnMistAndSpecks(centerPos - Vector3.up * 0.2f, 4, 4, 1.5f, 3.2f, 0.28f);
            }
        }

        private static Color ResolvePropShardColor(
            BreakablePropKind kind, int i, bool isMetallic, bool isIce, bool isSilk,
            bool isShellwood, bool isSandDesert, bool isRoyalGold)
        {
            int mod = i % 3;
            if (isIce)
            {
                return mod == 0 ? new Color(0.84f, 0.96f, 1.0f, 0.96f)
                     : (mod == 1 ? new Color(0.56f, 0.82f, 0.96f, 0.94f) : new Color(0.95f, 0.99f, 1.0f, 0.98f));
            }
            if (isSilk)
            {
                return mod == 0 ? new Color(0.94f, 0.95f, 0.98f, 0.94f)
                     : (mod == 1 ? new Color(0.76f, 0.79f, 0.86f, 0.92f) : new Color(0.56f, 0.50f, 0.46f, 0.92f));
            }
            if (isShellwood)
            {
                return mod == 0 ? new Color(0.42f, 0.34f, 0.28f, 0.96f)
                     : (mod == 1 ? new Color(0.62f, 0.44f, 0.86f, 0.94f) : new Color(0.52f, 0.56f, 0.64f, 0.94f));
            }
            if (isSandDesert)
            {
                return mod == 0 ? new Color(0.82f, 0.46f, 0.34f, 0.96f)
                     : (mod == 1 ? new Color(0.76f, 0.64f, 0.46f, 0.96f) : new Color(0.88f, 0.82f, 0.72f, 0.96f));
            }
            if (isMetallic)
            {
                return mod == 0 ? new Color(0.84f, 0.70f, 0.38f, 0.96f)
                     : (mod == 1 ? new Color(0.52f, 0.44f, 0.36f, 0.96f) : new Color(1.0f, 0.86f, 0.54f, 0.96f));
            }
            if (isRoyalGold)
            {
                return mod == 0 ? new Color(0.72f, 0.76f, 0.82f, 0.96f)
                     : (mod == 1 ? new Color(0.88f, 0.72f, 0.36f, 0.96f) : new Color(0.66f, 0.24f, 0.30f, 0.96f));
            }
            if (kind == BreakablePropKind.WoodenBarrel || kind == BreakablePropKind.RottenLog || kind == BreakablePropKind.TownCrate || kind == BreakablePropKind.ChoirBench)
            {
                return mod == 0 ? new Color(0.46f, 0.36f, 0.26f, 0.96f)
                     : (mod == 1 ? new Color(0.32f, 0.24f, 0.18f, 0.96f) : new Color(0.56f, 0.60f, 0.48f, 0.96f));
            }
            return mod == 0 ? new Color(0.68f, 0.72f, 0.69f, 0.96f)
                 : (mod == 1 ? new Color(0.46f, 0.50f, 0.50f, 0.96f) : new Color(0.84f, 0.86f, 0.80f, 0.96f));
        }

        /// <summary>
        /// Emits 5-8 mossy leaf & shell particles on hit (more on defeat) matching Mossmir / Mossgrub flora biology.
        /// </summary>
        public void SpawnMossEnemyHitBurst(Vector3 position, bool isDeath)
        {
            int leafCount = isDeath ? 12 : Random.Range(5, 9);
            Sprite leafSp = mossLeafSprite != null ? mossLeafSprite : mossSpeckSprite;
            if (leafSp != null)
            {
                for (int i = 0; i < leafCount; i++)
                {
                    float ang = Random.Range(-30f, 210f) * Mathf.Deg2Rad;
                    float spd = isDeath ? Random.Range(2.4f, 6.4f) : Random.Range(1.8f, 4.4f);
                    Color leafCol = (i % 3 == 0)
                        ? new Color(0.49f, 0.72f, 0.21f, 0.96f)
                        : ((i % 3 == 1) ? new Color(0.64f, 0.83f, 0.31f, 0.96f) : new Color(0.24f, 0.45f, 0.18f, 0.96f));

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = leafSp;
                    p.Position = position + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.18f, 0.22f), 0f);
                    p.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(0.9f, 2.2f));
                    p.Gravity = Random.Range(5.2f, 8.2f);
                    p.Drag = Random.Range(2.0f, 3.4f);
                    p.Lifetime = Random.Range(0.55f, 1.05f);
                    p.StartScale = Random.Range(0.16f, 0.28f);
                    p.EndScale = p.StartScale * 0.65f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-440f, 440f);
                    p.FlutterAmplitude = Random.Range(0.9f, 1.8f);
                    p.FlutterFrequency = Random.Range(7f, 12f);
                    p.Color = leafCol;
                    p.FadePower = 2.1f;
                    p.UseAdditiveMaterial = false;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 14;
                    particlePool.Emit(p);
                }
            }

            if (!isDeath)
            {
                SpawnEnemyHitDropletsAndShards(position, 2, 2);
            }
        }

        /// <summary>
        /// Emits jagged pale-ice and dark obsidian crystal shards on hit & defeat for Mount Fay enemies.
        /// </summary>
        public void SpawnCrystalIceHitBurst(Vector3 position, bool isDeath)
        {
            int shardCount = isDeath ? 16 : 6;
            Sprite shardSp = shellShardSprite != null ? shellShardSprite : hitSparkSprite;
            if (shardSp != null)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float spd = isDeath ? Random.Range(3.2f, 7.8f) : Random.Range(2.0f, 5.2f);
                    int mod = i % 3;
                    Color shardCol = mod == 0
                        ? new Color(0.76f, 0.96f, 1.0f, 0.96f)
                        : (mod == 1 ? new Color(0.95f, 0.99f, 1.0f, 0.98f) : new Color(0.22f, 0.28f, 0.38f, 0.96f));

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = shardSp;
                    p.Position = position + new Vector3(Random.Range(-0.24f, 0.24f), Random.Range(-0.24f, 0.24f), 0f);
                    p.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(0.8f, 2.4f));
                    p.Gravity = Random.Range(9.0f, 14.2f);
                    p.Drag = Random.Range(1.2f, 2.4f);
                    p.Lifetime = isDeath ? Random.Range(0.45f, 0.88f) : Random.Range(0.32f, 0.56f);
                    p.StartScale = isDeath ? Random.Range(0.22f, 0.40f) : Random.Range(0.16f, 0.28f);
                    p.EndScale = p.StartScale * 0.45f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-580f, 580f);
                    p.Color = shardCol;
                    p.FadePower = 1.6f;
                    p.UseAdditiveMaterial = mod != 2;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 14;
                    particlePool.Emit(p);
                }
            }

            SpawnSoftBurst(
                position,
                new Color(0.80f, 0.96f, 1.0f, 0.94f),
                isDeath ? 12 : 4,
                1.8f,
                isDeath ? 5.8f : 3.4f,
                isDeath ? 0.36f : 0.24f,
                isDeath ? 0.22f : 0.14f,
                true
            );
        }

        /// <summary>
        /// Emits luminous royal-gold and white-silk threads & sparks on hit and defeat for Choral Chambers enemies.
        /// </summary>
        public void SpawnGoldenSilkHitBurst(Vector3 position, bool isDeath)
        {
            int count = isDeath ? 18 : 6;
            Sprite sp = shellShardSprite != null ? shellShardSprite : mossSpeckSprite;
            if (sp != null)
            {
                for (int i = 0; i < count; i++)
                {
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float spd = isDeath ? Random.Range(2.8f, 7.6f) : Random.Range(1.8f, 4.8f);
                    int mod = i % 3;
                    Color col = mod == 0
                        ? new Color(1.0f, 0.86f, 0.36f, 0.98f)
                        : (mod == 1 ? new Color(0.99f, 0.96f, 0.88f, 0.98f) : new Color(0.94f, 0.68f, 0.24f, 0.95f));

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = sp;
                    p.Position = position + new Vector3(Random.Range(-0.24f, 0.24f), Random.Range(-0.22f, 0.24f), 0f);
                    p.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(0.7f, 2.2f));
                    p.Gravity = Random.Range(3.4f, 6.2f);
                    p.Drag = Random.Range(2.2f, 3.8f);
                    p.Lifetime = isDeath ? Random.Range(0.55f, 1.05f) : Random.Range(0.30f, 0.56f);
                    p.StartScale = isDeath ? Random.Range(0.20f, 0.38f) : Random.Range(0.14f, 0.25f);
                    p.EndScale = p.StartScale * 0.42f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-520f, 520f);
                    p.FlutterAmplitude = Random.Range(0.8f, 1.7f);
                    p.FlutterFrequency = Random.Range(6f, 11f);
                    p.Color = col;
                    p.FadePower = 1.8f;
                    p.UseAdditiveMaterial = true;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 14;
                    particlePool.Emit(p);
                }
            }

            SpawnSoftBurst(
                position,
                new Color(1.0f, 0.88f, 0.46f, 0.96f),
                isDeath ? 14 : 4,
                1.6f,
                isDeath ? 5.6f : 3.2f,
                isDeath ? 0.42f : 0.24f,
                isDeath ? 0.24f : 0.14f,
                true
            );
        }

        /// <summary>
        /// Emits fluttering tattered cloth scraps and billowing charcoal ash dust for Drapemite / Drapefly hits & defeats.
        /// </summary>
        public void SpawnClothDustHitBurst(Vector3 position, bool isDeath)
        {
            int shredCount = isDeath ? 16 : 6;
            Sprite shredSp = mossLeafSprite != null ? mossLeafSprite : mossSpeckSprite;
            if (shredSp != null)
            {
                for (int i = 0; i < shredCount; i++)
                {
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float spd = isDeath ? Random.Range(2.4f, 6.2f) : Random.Range(1.6f, 4.2f);
                    int mod = i % 3;
                    Color ragCol = mod == 0
                        ? new Color(0.26f, 0.25f, 0.28f, 0.95f)
                        : (mod == 1 ? new Color(0.60f, 0.59f, 0.63f, 0.94f) : new Color(0.42f, 0.41f, 0.44f, 0.95f));

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = shredSp;
                    p.Position = position + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.18f, 0.22f), 0f);
                    p.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(0.8f, 2.0f));
                    p.Gravity = Random.Range(3.0f, 5.2f);
                    p.Drag = Random.Range(2.4f, 3.8f);
                    p.Lifetime = isDeath ? Random.Range(0.58f, 1.08f) : Random.Range(0.32f, 0.58f);
                    p.StartScale = isDeath ? Random.Range(0.18f, 0.34f) : Random.Range(0.14f, 0.24f);
                    p.EndScale = p.StartScale * 0.55f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-420f, 420f);
                    p.FlutterAmplitude = Random.Range(1.2f, 2.4f);
                    p.FlutterFrequency = Random.Range(7f, 12f);
                    p.Color = ragCol;
                    p.FadePower = 2.0f;
                    p.UseAdditiveMaterial = false;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 14;
                    particlePool.Emit(p);
                }
            }

            SpawnSoftBurst(
                position,
                new Color(0.56f, 0.55f, 0.60f, 0.78f),
                isDeath ? 12 : 4,
                1.4f,
                isDeath ? 4.4f : 2.8f,
                isDeath ? 0.48f : 0.26f,
                isDeath ? 0.32f : 0.16f,
                false
            );
        }

        /// <summary>
        /// Emits metallic bronze bell shards and dark soot-fur puffs on hit and defeat for Bellhart Furm species.
        /// </summary>
        public void SpawnBellBronzeHitBurst(Vector3 position, bool isDeath)
        {
            int shardCount = isDeath ? 16 : 6;
            Sprite sp = shellShardSprite != null ? shellShardSprite : mossSpeckSprite;
            if (sp != null)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float spd = isDeath ? Random.Range(2.8f, 7.2f) : Random.Range(1.8f, 4.8f);
                    int mod = i % 3;
                    Color shardCol = mod == 0
                        ? new Color(0.88f, 0.68f, 0.32f, 0.96f)
                        : (mod == 1 ? new Color(0.24f, 0.23f, 0.26f, 0.96f) : new Color(1.0f, 0.86f, 0.52f, 0.96f));

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = sp;
                    p.Position = position + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.20f, 0.24f), 0f);
                    p.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(1.0f, 2.5f));
                    p.Gravity = Random.Range(10.5f, 15.0f);
                    p.Drag = Random.Range(1.2f, 2.2f);
                    p.Lifetime = isDeath ? Random.Range(0.45f, 0.85f) : Random.Range(0.28f, 0.52f);
                    p.StartScale = isDeath ? Random.Range(0.20f, 0.36f) : Random.Range(0.14f, 0.24f);
                    p.EndScale = p.StartScale * 0.48f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-560f, 560f);
                    p.Color = shardCol;
                    p.FadePower = 1.7f;
                    p.UseAdditiveMaterial = mod == 2;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 14;
                    particlePool.Emit(p);
                }
            }

            SpawnSoftBurst(
                position,
                new Color(0.82f, 0.72f, 0.54f, 0.82f),
                isDeath ? 12 : 4,
                1.6f,
                isDeath ? 4.8f : 3.0f,
                isDeath ? 0.42f : 0.24f,
                isDeath ? 0.26f : 0.14f,
                false
            );
        }

        /// <summary>
        /// Emits bone-white shell fragments and dark ash dust bursts for Skullwing, PharlidDiver, Mawling, and Marrowmaw.
        /// </summary>
        public void SpawnBoneCarapaceHitBurst(Vector3 position, bool isDeath)
        {
            int shardCount = isDeath ? 15 : 5;
            Sprite sp = shellShardSprite != null ? shellShardSprite : mossSpeckSprite;
            if (sp != null)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float spd = isDeath ? Random.Range(2.6f, 6.8f) : Random.Range(1.8f, 4.4f);
                    int mod = i % 3;
                    Color shardCol = mod == 0
                        ? new Color(0.92f, 0.91f, 0.88f, 0.96f)
                        : (mod == 1 ? new Color(0.36f, 0.37f, 0.42f, 0.96f) : new Color(0.76f, 0.78f, 0.82f, 0.94f));

                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = sp;
                    p.Position = position + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.20f, 0.24f), 0f);
                    p.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(0.9f, 2.3f));
                    p.Gravity = Random.Range(10.0f, 14.5f);
                    p.Drag = Random.Range(1.2f, 2.2f);
                    p.Lifetime = isDeath ? Random.Range(0.42f, 0.80f) : Random.Range(0.26f, 0.48f);
                    p.StartScale = isDeath ? Random.Range(0.18f, 0.34f) : Random.Range(0.12f, 0.22f);
                    p.EndScale = p.StartScale * 0.45f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-540f, 540f);
                    p.Color = shardCol;
                    p.FadePower = 1.7f;
                    p.UseAdditiveMaterial = false;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 14;
                    particlePool.Emit(p);
                }
            }

            SpawnSoftBurst(
                position,
                new Color(0.76f, 0.77f, 0.82f, 0.82f),
                isDeath ? 12 : 4,
                1.5f,
                isDeath ? 4.6f : 2.8f,
                isDeath ? 0.40f : 0.22f,
                isDeath ? 0.25f : 0.14f,
                false
            );
        }

        public void SpawnSkillBurst(Vector3 position, Color tint)
        {
            SpawnSoftBurst(position, tint, 16, 2.2f, 5.6f, 0.48f, 0.22f, true);
            Sprite star = impactStarburstSprite != null ? impactStarburstSprite : hitSparkSprite;
            if (star != null)
            {
                PooledParticleSystem2D.ParticleParams flash = new PooledParticleSystem2D.ParticleParams();
                flash.Sprite = star;
                flash.Position = position;
                flash.Lifetime = 0.24f;
                flash.StartScale = 0.55f;
                flash.EndScale = 1.25f;
                flash.RotationDegrees = Random.Range(0f, 360f);
                flash.AngularVelocity = 70f;
                flash.Color = tint;
                flash.FadePower = 1.6f;
                flash.UseAdditiveMaterial = true;
                flash.SortingLayerName = GameConstants.SortVFX;
                flash.SortingOrder = 15;
                particlePool.Emit(flash);
            }
        }

        public GameObject SpawnAttachedThreadGuard(Transform parent, float duration)
        {
            if (threadGuardSprite == null || parent == null) return null;
            GameObject shield = new GameObject("VFX_ThreadGuardAura");
            shield.transform.SetParent(parent, false);
            shield.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            shield.transform.localScale = Vector3.one * 1.15f;

            SpriteRenderer sr = shield.AddComponent<SpriteRenderer>();
            sr.sprite = threadGuardSprite;
            sr.color = new Color(0.88f, 0.99f, 0.96f, 0.88f);
            sr.sortingLayerName = GameConstants.SortVFX;
            sr.sortingOrder = 12;
            if (additiveVfxMaterial != null) sr.sharedMaterial = additiveVfxMaterial;

            StartCoroutine(SpinAndExpire(shield, sr, duration));
            return shield;
        }

        /// <summary>
        /// Dynamically spawns swirling luminous silk ribbons and inward-converging particles
        /// around the heroine during Bind Healing without baking VFX into the character sprite strip.
        /// </summary>
        public void SpawnBindChannelRibbons(Transform target, float duration)
        {
            if (target == null) return;
            StartCoroutine(BindChannelRoutine(target, Mathf.Max(0.25f, duration)));
        }

        public void SpawnBindHealBurst(Vector3 centerPos)
        {
            SpawnWaterSplashRing(centerPos);
            SpawnSoftBurst(centerPos, new Color(0.92f, 1f, 0.92f, 0.96f), 14, 2.4f, 6.2f, 0.42f, 0.22f, true);

            Sprite star = impactStarburstSprite != null ? impactStarburstSprite : hitSparkSprite;
            if (star != null)
            {
                PooledParticleSystem2D.ParticleParams flash = new PooledParticleSystem2D.ParticleParams();
                flash.Sprite = star;
                flash.Position = centerPos;
                flash.Lifetime = 0.26f;
                flash.StartScale = 0.70f;
                flash.EndScale = 1.65f;
                flash.RotationDegrees = Random.Range(0f, 360f);
                flash.AngularVelocity = 95f;
                flash.Color = new Color(0.96f, 1f, 0.90f, 1f);
                flash.FadePower = 1.6f;
                flash.UseAdditiveMaterial = true;
                flash.SortingLayerName = GameConstants.SortVFX;
                flash.SortingOrder = 16;
                particlePool.Emit(flash);
            }
        }

        private IEnumerator BindChannelRoutine(Transform target, float duration)
        {
            Sprite ribbon = healRibbonSprite != null ? healRibbonSprite : slashArcSprite;
            GameObject[] rings = new GameObject[3];
            SpriteRenderer[] ringSrs = new SpriteRenderer[3];

            if (ribbon != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject go = new GameObject("VFX_BindRibbon_" + i);
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = ribbon;
                    sr.color = i == 2 ? new Color(0.85f, 0.99f, 0.88f, 0.78f) : new Color(1f, 1f, 0.96f, 0.88f);
                    sr.sortingLayerName = GameConstants.SortVFX;
                    sr.sortingOrder = 15 + i;
                    if (additiveVfxMaterial != null) sr.sharedMaterial = additiveVfxMaterial;
                    rings[i] = go;
                    ringSrs[i] = sr;
                }
            }

            float elapsed = 0f;
            float particleTick = 0f;
            while (elapsed < duration && target != null)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                particleTick -= dt;
                float u = Mathf.Clamp01(elapsed / duration);
                Vector3 core = target.position + Vector3.up * 1.02f;

                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] == null) continue;
                    rings[i].transform.position = core;
                    float scale = Mathf.Lerp(1.25f + i * 0.22f, 0.42f, u);
                    rings[i].transform.localScale = Vector3.one * scale;
                    rings[i].transform.rotation = Quaternion.Euler(0f, 0f, elapsed * (320f + i * 145f) * (i % 2 == 0 ? 1f : -1f) + i * 120f);
                    Color rc = ringSrs[i].color;
                    rc.a = Mathf.Sin(u * Mathf.PI * 0.9f) * 0.92f;
                    ringSrs[i].color = rc;
                }

                if (particleTick <= 0f && softParticleSprite != null)
                {
                    particleTick = 0.045f;
                    float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float r = Random.Range(0.95f, 1.45f) * (1f - u * 0.55f);
                    Vector3 spawnPos = core + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
                    PooledParticleSystem2D.ParticleParams sp = new PooledParticleSystem2D.ParticleParams();
                    sp.Sprite = softParticleSprite;
                    sp.Position = spawnPos;
                    sp.Velocity = ((Vector2)(core - spawnPos)).normalized * Random.Range(2.4f, 4.2f);
                    sp.Lifetime = 0.24f;
                    sp.StartScale = 0.14f;
                    sp.EndScale = 0.04f;
                    sp.Color = new Color(0.88f, 1f, 0.86f, 0.90f);
                    sp.FadePower = 1.4f;
                    sp.UseAdditiveMaterial = true;
                    sp.SortingLayerName = GameConstants.SortVFX;
                    sp.SortingOrder = 14;
                    particlePool.Emit(sp);
                }
                yield return null;
            }

            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] != null) Destroy(rings[i]);
            }
        }

        private void SpawnEnemyHitDropletsAndShards(Vector3 position, int dropletCount, int shardCount)
        {
            // 1. Glowing lime-green organic droplets/orbs (#9BE840) scattering outward with gravity
            Sprite dropSp = organicDropletSprite != null ? organicDropletSprite : softParticleSprite;
            if (dropSp != null)
            {
                for (int i = 0; i < dropletCount; i++)
                {
                    float ang = Random.Range(-25f, 205f) * Mathf.Deg2Rad;
                    float spd = Random.Range(2.4f, 6.8f);
                    PooledParticleSystem2D.ParticleParams d = new PooledParticleSystem2D.ParticleParams();
                    d.Sprite = dropSp;
                    d.Position = position + new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(-0.22f, 0.22f), 0f);
                    d.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(0.8f, 2.4f));
                    d.Gravity = 13.5f;
                    d.Drag = 1.4f;
                    d.Lifetime = Random.Range(0.36f, 0.68f);
                    d.StartScale = Random.Range(0.16f, 0.30f);
                    d.EndScale = 0.06f;
                    d.Color = new Color(0.608f, 0.910f, 0.251f, 0.96f); // Exact #9BE840 glowing lime-green
                    d.FadePower = 1.7f;
                    d.UseAdditiveMaterial = false;
                    d.SortingLayerName = GameConstants.SortVFX;
                    d.SortingOrder = 14;
                    particlePool.Emit(d);
                }
            }

            // 2. Small pale shell shards / petal fragments tumbling outward with gravity
            Sprite shardSp = shellShardSprite != null ? shellShardSprite : mossSpeckSprite;
            if (shardSp != null)
            {
                for (int i = 0; i < shardCount; i++)
                {
                    float ang = Random.Range(-15f, 195f) * Mathf.Deg2Rad;
                    float spd = Random.Range(2.8f, 7.2f);
                    bool crimsonPetal = (i % 4 == 3);
                    PooledParticleSystem2D.ParticleParams s = new PooledParticleSystem2D.ParticleParams();
                    s.Sprite = shardSp;
                    s.Position = position + new Vector3(Random.Range(-0.24f, 0.24f), Random.Range(-0.18f, 0.24f), 0f);
                    s.Velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + Random.Range(1.2f, 3.0f));
                    s.Gravity = 15.0f;
                    s.Drag = 1.2f;
                    s.Lifetime = Random.Range(0.45f, 0.85f);
                    s.StartScale = Random.Range(0.14f, 0.25f);
                    s.EndScale = s.StartScale * 0.72f;
                    s.RotationDegrees = Random.Range(0f, 360f);
                    s.AngularVelocity = Random.Range(-520f, 520f);
                    s.Color = crimsonPetal
                        ? new Color(0.84f, 0.28f, 0.34f, 0.95f)
                        : new Color(0.95f, 0.93f, 0.87f, 0.96f);
                    s.FadePower = 2.0f;
                    s.UseAdditiveMaterial = false;
                    s.SortingLayerName = GameConstants.SortVFX;
                    s.SortingOrder = 13;
                    particlePool.Emit(s);
                }
            }
        }

        private void SpawnMistAndSpecks(Vector3 feetPosition, int puffCount, int speckCount, float minSpeed, float maxSpeed, float puffSize)
        {
            Sprite puffSp = dustPuffSprite != null ? dustPuffSprite : softParticleSprite;
            if (puffSp != null)
            {
                for (int i = 0; i < puffCount; i++)
                {
                    float side = (i % 2 == 0) ? 1f : -1f;
                    PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                    p.Sprite = puffSp;
                    p.Position = feetPosition + new Vector3(Random.Range(-0.28f, 0.28f), Random.Range(0.04f, 0.16f), 0f);
                    p.Velocity = new Vector2(side * Random.Range(minSpeed * 0.7f, maxSpeed), Random.Range(0.35f, 1.35f));
                    p.Gravity = 0.6f;
                    p.Drag = 4.2f;
                    p.Lifetime = Random.Range(0.28f, 0.44f);
                    p.StartScale = puffSize * Random.Range(0.75f, 1.15f);
                    p.EndScale = p.StartScale * 1.65f;
                    p.RotationDegrees = Random.Range(0f, 360f);
                    p.AngularVelocity = Random.Range(-65f, 65f);
                    p.Color = new Color(0.86f, 0.93f, 0.87f, Random.Range(0.32f, 0.48f));
                    p.FadePower = 1.5f;
                    p.UseAdditiveMaterial = false;
                    p.SortingLayerName = GameConstants.SortVFX;
                    p.SortingOrder = 8;
                    particlePool.Emit(p);
                }
            }

            Sprite speckSp = mossSpeckSprite != null ? mossSpeckSprite : softParticleSprite;
            if (speckSp != null)
            {
                for (int i = 0; i < speckCount; i++)
                {
                    PooledParticleSystem2D.ParticleParams sp = new PooledParticleSystem2D.ParticleParams();
                    sp.Sprite = speckSp;
                    sp.Position = feetPosition + new Vector3(Random.Range(-0.30f, 0.30f), 0.06f, 0f);
                    sp.Velocity = new Vector2(Random.Range(-maxSpeed * 0.9f, maxSpeed * 0.9f), Random.Range(1.4f, 3.6f));
                    sp.Gravity = 13.5f;
                    sp.Drag = 1.5f;
                    sp.Lifetime = Random.Range(0.28f, 0.52f);
                    sp.StartScale = Random.Range(0.14f, 0.24f);
                    sp.EndScale = 0.06f;
                    sp.RotationDegrees = Random.Range(0f, 360f);
                    sp.AngularVelocity = Random.Range(-320f, 320f);
                    sp.Color = new Color(0.68f, 0.78f, 0.58f, 0.88f);
                    sp.FadePower = 1.8f;
                    sp.UseAdditiveMaterial = false;
                    sp.SortingLayerName = GameConstants.SortVFX;
                    sp.SortingOrder = 9;
                    particlePool.Emit(sp);
                }
            }
        }

        private void SpawnSoftBurst(Vector3 pos, Color color, int count, float minSpeed, float maxSpeed, float lifetime, float size, bool additive)
        {
            if (softParticleSprite == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float speed = Random.Range(minSpeed, maxSpeed);
                PooledParticleSystem2D.ParticleParams p = new PooledParticleSystem2D.ParticleParams();
                p.Sprite = softParticleSprite;
                p.Position = pos;
                p.Velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                p.Gravity = 0f;
                p.Drag = 3.2f;
                p.Lifetime = lifetime * Random.Range(0.75f, 1.15f);
                p.StartScale = Random.Range(size * 0.65f, size * 1.35f);
                p.EndScale = p.StartScale * 0.35f;
                p.Color = color;
                p.FadePower = 1.4f;
                p.UseAdditiveMaterial = additive;
                p.SortingLayerName = GameConstants.SortVFX;
                p.SortingOrder = 11;
                particlePool.Emit(p);
            }
        }

        private IEnumerator SpinAndExpire(GameObject obj, SpriteRenderer sr, float duration)
        {
            float elapsed = 0f;
            Color baseCol = sr.color;
            while (elapsed < duration && obj != null)
            {
                elapsed += Time.deltaTime;
                obj.transform.Rotate(0f, 0f, 110f * Time.deltaTime);
                if (duration - elapsed < 0.25f)
                {
                    float fade = Mathf.Clamp01((duration - elapsed) / 0.25f);
                    sr.color = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * fade);
                }
                yield return null;
            }
            if (obj != null) Destroy(obj);
        }
    }
}
