using System;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Camera-anchored multi-layer dynamic weather & volumetric atmospheric fog engine.
    /// Features:
    /// - Reduced, balanced 3-depth particle layers (Background, Playable/VFX, Foreground)
    ///   with smooth alpha fade-in/out over lifetime so particles stay subtle and atmospheric.
    /// - 3 seamlessly scrolling volumetric mist & storm-cloud bands (Mid Wind Clouds,
    ///   Low Ground-Hugging Drift Fog, Foreground Gust Haze) delivering genuine swirling
    ///   Sandstorm dust fog in Sands of Karak and driving Blizzard snow fog in Mount Fay.
    /// </summary>
    public class DynamicWeatherController : MonoBehaviour
    {
        public static DynamicWeatherController Instance { get; private set; }

        [Serializable]
        private class ScrollingFogBand
        {
            public Transform bandRoot;
            public Transform tileA;
            public Transform tileB;
            public SpriteRenderer rendererA;
            public SpriteRenderer rendererB;
            public float tileWorldWidth = 26f;
            public float baseSpeedX;
            public Color baseColor = new Color(1f, 1f, 1f, 0f);
            public float scrollOffset;

            public void ApplyVisual(Color col, float speedX, float tiltDeg, float bobOffsetY, bool isGroundFog)
            {
                baseColor = col;
                baseSpeedX = speedX;
                bool visible = col.a > 0.008f;
                if (bandRoot != null)
                {
                    bandRoot.gameObject.SetActive(visible);
                    if (visible)
                    {
                        bandRoot.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
                        Vector3 p = bandRoot.localPosition;
                        bandRoot.localPosition = new Vector3(0f, isGroundFog ? (-2.9f + bobOffsetY) : bobOffsetY, p.z);
                    }
                }
                if (rendererA != null) rendererA.color = col;
                if (rendererB != null) rendererB.color = col;
            }

            public void Tick(float dt, float gustRatio, float pulse)
            {
                if (bandRoot == null || !bandRoot.gameObject.activeSelf || tileWorldWidth <= 0.1f) return;

                float effectiveSpeed = baseSpeedX * (1f + gustRatio * 0.28f);
                scrollOffset += effectiveSpeed * dt;
                // Keep offset neatly wrapped inside [-tileWorldWidth, 0]
                scrollOffset = Mathf.Repeat(scrollOffset, tileWorldWidth);
                if (scrollOffset > 0f) scrollOffset -= tileWorldWidth;

                if (tileA != null) tileA.localPosition = new Vector3(scrollOffset, 0f, 0f);
                if (tileB != null) tileB.localPosition = new Vector3(scrollOffset + tileWorldWidth, 0f, 0f);

                Color c = baseColor;
                c.a = Mathf.Clamp01(baseColor.a * pulse);
                if (rendererA != null) rendererA.color = c;
                if (rendererB != null) rendererB.color = c;
            }
        }

        [Header("Particle Depth Layers")]
        [SerializeField] private ParticleSystem backgroundParticles;
        [SerializeField] private ParticleSystem playableParticles;
        [SerializeField] private ParticleSystem foregroundParticles;

        [Header("Materials & Atmospheric Veil")]
        [SerializeField] private Material glowMoteMaterial;
        [SerializeField] private Material streakParticleMaterial;
        [SerializeField] private SpriteRenderer fogVeilRenderer;

        [Header("Scrolling Volumetric Storm & Mist Cloud Bands")]
        [SerializeField] private ScrollingFogBand midMistBand = new ScrollingFogBand();
        [SerializeField] private ScrollingFogBand groundFogBand = new ScrollingFogBand();
        [SerializeField] private ScrollingFogBand fgMistBand = new ScrollingFogBand();

        private WeatherProfileType _activeProfile = WeatherProfileType.MossGrottoSunSpores;
        private Color _targetFogColor = new Color(0.12f, 0.24f, 0.18f, 0.04f);
        private float _windBaseX;
        private float _windGustAmp;
        private float _windGustFreq = 0.8f;
        private float _baseVelY;
        private bool _initialized;

        public WeatherProfileType ActiveProfile { get { return _activeProfile; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (!_initialized)
            {
                SetWeatherProfile(_activeProfile, true);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ConfigureReferences(
            ParticleSystem bg,
            ParticleSystem mid,
            ParticleSystem fg,
            Material glowMat,
            Material streakMat,
            SpriteRenderer fogVeil)
        {
            backgroundParticles = bg;
            playableParticles = mid;
            foregroundParticles = fg;
            glowMoteMaterial = glowMat;
            streakParticleMaterial = streakMat;
            fogVeilRenderer = fogVeil;
        }

        public void ConfigureFogBand(
            int bandIndex,
            Transform bandRoot,
            Transform tileA,
            Transform tileB,
            SpriteRenderer srA,
            SpriteRenderer srB,
            float tileWidth)
        {
            ScrollingFogBand target = bandIndex == 0 ? midMistBand : (bandIndex == 1 ? groundFogBand : fgMistBand);
            target.bandRoot = bandRoot;
            target.tileA = tileA;
            target.tileB = tileB;
            target.rendererA = srA;
            target.rendererB = srB;
            target.tileWorldWidth = Mathf.Max(8f, tileWidth);
            target.scrollOffset = 0f;
        }

        public void SetWeatherProfile(WeatherProfileType profile, bool clearExisting)
        {
            _activeProfile = profile;
            _initialized = true;

            switch (profile)
            {
                case WeatherProfileType.MossGrottoSunSpores:
                    ApplyLayeredProfile(
                        glowMoteMaterial, glowMoteMaterial, glowMoteMaterial,
                        new Color(0.78f, 0.96f, 0.42f, 0.42f), new Color(0.92f, 1.0f, 0.68f, 0.62f),
                        3.5f, 4.5f, 1.8f,
                        0.08f, 0.16f, 0.13f, 0.24f, 0.18f, 0.30f,
                        0.08f, 0.38f, 0.52f, 0.08f, 5.5f, 9.5f, false,
                        new Color(0.16f, 0.34f, 0.24f, 0.04f),
                        new Color(0.32f, 0.55f, 0.36f, 0.06f), -0.55f, 0f,
                        new Color(0.24f, 0.46f, 0.30f, 0.08f), -0.75f,
                        new Color(0f, 0f, 0f, 0f), 0f, 0f,
                        clearExisting);
                    break;

                case WeatherProfileType.WeavenestSilkDust:
                    ApplyLayeredProfile(
                        glowMoteMaterial, glowMoteMaterial, glowMoteMaterial,
                        new Color(0.84f, 0.88f, 0.96f, 0.40f), new Color(0.98f, 0.99f, 1.0f, 0.64f),
                        5.0f, 7.0f, 3.0f,
                        0.06f, 0.12f, 0.10f, 0.18f, 0.13f, 0.22f,
                        -0.22f, 0.32f, 0.62f, 0.12f, 5.2f, 8.8f, false,
                        new Color(0.12f, 0.15f, 0.24f, 0.07f),
                        new Color(0.76f, 0.82f, 0.96f, 0.11f), -0.85f, 2f,
                        new Color(0.72f, 0.78f, 0.94f, 0.16f), -1.10f,
                        new Color(0.84f, 0.88f, 0.98f, 0.06f), -1.40f, 0f,
                        clearExisting);
                    break;

                case WeatherProfileType.ShellwoodBiolumFog:
                    ApplyLayeredProfile(
                        glowMoteMaterial, glowMoteMaterial, glowMoteMaterial,
                        new Color(0.68f, 0.44f, 0.98f, 0.52f), new Color(0.42f, 0.86f, 1.0f, 0.70f),
                        5.5f, 8.0f, 3.5f,
                        0.08f, 0.15f, 0.12f, 0.24f, 0.16f, 0.30f,
                        -0.35f, 0.45f, 0.50f, 0.20f, 5.0f, 8.2f, false,
                        new Color(0.20f, 0.16f, 0.36f, 0.14f),
                        new Color(0.50f, 0.42f, 0.86f, 0.22f), -1.2f, 3f,
                        new Color(0.46f, 0.44f, 0.82f, 0.36f), -1.6f,
                        new Color(0.58f, 0.52f, 0.92f, 0.12f), -2.2f, 0f,
                        clearExisting);
                    break;

                case WeatherProfileType.KarakSandstorm:
                    // Crisp reduced sand streaks + HEAVY rolling volumetric ochre Sandstorm Dust Fog!
                    ApplyLayeredProfile(
                        streakParticleMaterial, streakParticleMaterial, streakParticleMaterial,
                        new Color(0.88f, 0.68f, 0.42f, 0.46f), new Color(0.96f, 0.82f, 0.56f, 0.70f),
                        12.0f, 16.0f, 8.0f,
                        0.26f, 0.54f, 0.36f, 0.78f, 0.48f, 0.96f,
                        -16.5f, 4.2f, 1.35f, -1.25f, 1.5f, 2.5f, true,
                        new Color(0.76f, 0.56f, 0.30f, 0.24f),
                        new Color(0.86f, 0.66f, 0.38f, 0.44f), -8.8f, -2.5f,
                        new Color(0.92f, 0.74f, 0.46f, 0.58f), -12.8f,
                        new Color(0.95f, 0.80f, 0.54f, 0.26f), -16.5f, -4.0f,
                        clearExisting);
                    break;

                case WeatherProfileType.BellhartColdMist:
                    ApplyLayeredProfile(
                        glowMoteMaterial, glowMoteMaterial, glowMoteMaterial,
                        new Color(0.62f, 0.72f, 0.86f, 0.36f), new Color(0.92f, 0.95f, 1.0f, 0.54f),
                        4.5f, 6.0f, 2.5f,
                        0.07f, 0.13f, 0.10f, 0.20f, 0.16f, 0.30f,
                        -0.68f, 0.40f, 0.44f, -0.06f, 5.2f, 9.0f, false,
                        new Color(0.14f, 0.20f, 0.30f, 0.14f),
                        new Color(0.58f, 0.68f, 0.84f, 0.20f), -1.4f, 1.5f,
                        new Color(0.64f, 0.74f, 0.88f, 0.34f), -1.9f,
                        new Color(0.70f, 0.80f, 0.94f, 0.11f), -2.5f, 0f,
                        clearExisting);
                    break;

                case WeatherProfileType.MountFayBlizzard:
                    // Clean reduced diagonal snow streaks + DENSE billowing glacial Blizzard Snow Fog!
                    ApplyLayeredProfile(
                        glowMoteMaterial, streakParticleMaterial, streakParticleMaterial,
                        new Color(0.84f, 0.93f, 1.0f, 0.56f), new Color(0.98f, 0.99f, 1.0f, 0.88f),
                        14.0f, 20.0f, 9.0f,
                        0.11f, 0.22f, 0.16f, 0.34f, 0.24f, 0.46f,
                        -9.6f, 2.8f, 1.15f, -8.2f, 2.0f, 3.2f, true,
                        new Color(0.66f, 0.80f, 0.94f, 0.25f),
                        new Color(0.82f, 0.91f, 1.0f, 0.46f), -7.8f, -22f,
                        new Color(0.88f, 0.95f, 1.0f, 0.56f), -10.8f,
                        new Color(0.94f, 0.98f, 1.0f, 0.28f), -14.2f, -26f,
                        clearExisting);
                    break;

                case WeatherProfileType.CourtyardLightSnowWind:
                    ApplyLayeredProfile(
                        glowMoteMaterial, glowMoteMaterial, glowMoteMaterial,
                        new Color(0.84f, 0.90f, 0.98f, 0.46f), new Color(0.97f, 0.98f, 1.0f, 0.72f),
                        6.0f, 9.0f, 4.0f,
                        0.08f, 0.15f, 0.11f, 0.22f, 0.16f, 0.28f,
                        -4.2f, 1.3f, 0.90f, -3.0f, 3.2f, 5.0f, false,
                        new Color(0.36f, 0.46f, 0.58f, 0.10f),
                        new Color(0.76f, 0.86f, 0.96f, 0.16f), -3.8f, -12f,
                        new Color(0.80f, 0.88f, 0.98f, 0.22f), -5.0f,
                        new Color(0.88f, 0.94f, 1.0f, 0.10f), -6.5f, -15f,
                        clearExisting);
                    break;

                case WeatherProfileType.ChoralGoldenDust:
                    ApplyLayeredProfile(
                        glowMoteMaterial, glowMoteMaterial, glowMoteMaterial,
                        new Color(0.98f, 0.84f, 0.48f, 0.42f), new Color(1.0f, 0.96f, 0.76f, 0.72f),
                        5.5f, 7.5f, 3.0f,
                        0.07f, 0.14f, 0.10f, 0.20f, 0.14f, 0.26f,
                        0.16f, 0.28f, 0.40f, 0.22f, 5.8f, 9.8f, false,
                        new Color(0.72f, 0.56f, 0.26f, 0.08f),
                        new Color(0.96f, 0.82f, 0.46f, 0.12f), 0.7f, 4f,
                        new Color(0.94f, 0.80f, 0.42f, 0.11f), 0.9f,
                        new Color(0f, 0f, 0f, 0f), 0f, 0f,
                        clearExisting);
                    break;
            }
        }

        private void ApplyLayeredProfile(
            Material bgMat, Material midMat, Material fgMat,
            Color colA, Color colB,
            float bgRate, float midRate, float fgRate,
            float bgMinSz, float bgMaxSz,
            float midMinSz, float midMaxSz,
            float fgMinSz, float fgMaxSz,
            float vxBase, float gustAmp, float gustFreq, float vyBase,
            float minLife, float maxLife, bool alignVelocity,
            Color veilCol,
            Color midMistCol, float midMistSpeedX, float midMistTiltDeg,
            Color groundFogCol, float groundFogSpeedX,
            Color fgMistCol, float fgMistSpeedX, float fgMistTiltDeg,
            bool clearExisting)
        {
            _windBaseX = vxBase;
            _windGustAmp = gustAmp;
            _windGustFreq = gustFreq;
            _baseVelY = vyBase;
            _targetFogColor = veilCol;

            ConfigureSingleLayer(backgroundParticles, bgMat, GameConstants.SortMidBackground, 6,
                new Color(colA.r, colA.g, colA.b, colA.a * 0.72f), new Color(colB.r, colB.g, colB.b, colB.a * 0.72f),
                bgRate, bgMinSz, bgMaxSz, vxBase * 0.65f, vyBase * 0.70f, minLife * 1.15f, maxLife * 1.15f, alignVelocity, clearExisting);

            ConfigureSingleLayer(playableParticles, midMat, GameConstants.SortVFX, 3,
                colA, colB,
                midRate, midMinSz, midMaxSz, vxBase, vyBase, minLife, maxLife, alignVelocity, clearExisting);

            ConfigureSingleLayer(foregroundParticles, fgMat, GameConstants.SortForeground, 5,
                new Color(colA.r, colA.g, colA.b, colA.a * 0.85f), new Color(colB.r, colB.g, colB.b, colB.a * 0.88f),
                fgRate, fgMinSz, fgMaxSz, vxBase * 1.25f, vyBase * 1.15f, minLife * 0.85f, maxLife * 0.85f, alignVelocity, clearExisting);

            if (fogVeilRenderer != null)
            {
                fogVeilRenderer.color = veilCol;
            }

            midMistBand.ApplyVisual(midMistCol, midMistSpeedX, midMistTiltDeg, 0.8f, false);
            groundFogBand.ApplyVisual(groundFogCol, groundFogSpeedX, 0f, 0f, true);
            fgMistBand.ApplyVisual(fgMistCol, fgMistSpeedX, fgMistTiltDeg, -0.4f, false);
        }

        private void ConfigureSingleLayer(
            ParticleSystem ps, Material mat, string sortLayer, int sortOrder,
            Color colA, Color colB, float rate, float minSz, float maxSz,
            float vx, float vy, float minLife, float maxLife, bool stretched, bool clearExisting)
        {
            if (ps == null) return;
            if (clearExisting) ps.Clear();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSz, maxSz);
            main.startColor = new ParticleSystem.MinMaxGradient(colA, colB);
            main.maxParticles = 95;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 17f, 1f);

            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(vx - 0.55f, vx + 0.55f);
            vel.y = new ParticleSystem.MinMaxCurve(vy - 0.30f, vy + 0.30f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.ColorOverLifetimeModule colLife = ps.colorOverLifetime;
            colLife.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.18f), new GradientAlphaKey(1f, 0.78f), new GradientAlphaKey(0f, 1f) }
            );
            colLife.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
            if (pr != null)
            {
                pr.sortingLayerName = sortLayer;
                pr.sortingOrder = sortOrder;
                pr.renderMode = stretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
                if (stretched)
                {
                    pr.velocityScale = 0.050f;
                    pr.lengthScale = 1.40f;
                }
                if (mat != null) pr.sharedMaterial = mat;
            }

            if (!ps.isPlaying) ps.Play();
        }

        private void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;
            float gustWave = Mathf.Sin(Time.time * _windGustFreq) + 0.35f * Mathf.Sin(Time.time * (_windGustFreq * 2.1f));
            float gust = gustWave * _windGustAmp;

            UpdateLayerWind(backgroundParticles, (_windBaseX + gust) * 0.65f, _baseVelY * 0.70f);
            UpdateLayerWind(playableParticles, _windBaseX + gust, _baseVelY);
            UpdateLayerWind(foregroundParticles, (_windBaseX + gust) * 1.25f, _baseVelY * 1.15f);

            float midPulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 0.85f);
            float groundPulse = 0.90f + 0.10f * Mathf.Sin(Time.time * 0.62f + 1.1f);
            float fgPulse = 0.82f + 0.18f * Mathf.Sin(Time.time * 1.15f + 2.3f);

            midMistBand.Tick(dt, gustWave, midPulse);
            groundFogBand.Tick(dt, gustWave * 0.75f, groundPulse);
            fgMistBand.Tick(dt, gustWave * 1.2f, fgPulse);

            if (fogVeilRenderer != null)
            {
                float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 0.75f);
                Color c = _targetFogColor;
                c.a = Mathf.Clamp01(_targetFogColor.a * pulse);
                fogVeilRenderer.color = c;
            }
        }

        private static void UpdateLayerWind(ParticleSystem ps, float vx, float vy)
        {
            if (ps == null) return;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(vx - 0.50f, vx + 0.50f);
            vel.y = new ParticleSystem.MinMaxCurve(vy - 0.22f, vy + 0.22f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }
    }
}
