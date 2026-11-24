using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Configures a multi-depth bioluminescent spore field (#C8F560 / #E9FFAD) with soft
    /// radial glow, gentle sinusoidal horizontal sway, and configurable parallax size/layering.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AmbientSporeField : MonoBehaviour
    {
        [Header("Visuals & Depth Layer")]
        [SerializeField] private Material particleMaterial;
        [SerializeField] private string sortingLayerName = GameConstants.SortVFX;
        [SerializeField] private int sortingOrder = 2;

        [Header("Distribution & Bioluminescence")]
        [SerializeField] private Vector3 boxSize = new Vector3(155f, 42f, 1f);
        [SerializeField] private float emissionRate = 24f;
        [SerializeField] private float minParticleSize = 0.08f;
        [SerializeField] private float maxParticleSize = 0.22f;
        [SerializeField] private int maxParticles = 260;

        [Header("Sinusoidal Horizontal Sway")]
        [SerializeField] private float swayAmplitude = 0.42f;
        [SerializeField] private float swayFrequency = 0.62f;
        [SerializeField] private float upwardDriftMin = 0.08f;
        [SerializeField] private float upwardDriftMax = 0.34f;
        [SerializeField] private float swayPhaseOffset = 0f;

        private ParticleSystem _ps;
        private ParticleSystem.VelocityOverLifetimeModule _velModule;
        private bool _ready;

        private void Start()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) return;

            ParticleSystem.MainModule main = _ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 10.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
            // Exact #C8F560 & #E9FFAD bioluminescent chartreuse glow palette
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.784f, 0.961f, 0.376f, 0.78f),
                new Color(0.914f, 1.000f, 0.678f, 0.62f)
            );
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            ParticleSystem.EmissionModule emission = _ps.emission;
            emission.rateOverTime = emissionRate;

            ParticleSystem.ShapeModule shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            _velModule = _ps.velocityOverLifetime;
            _velModule.enabled = true;
            _velModule.x = new ParticleSystem.MinMaxCurve(-swayAmplitude, swayAmplitude);
            _velModule.y = new ParticleSystem.MinMaxCurve(upwardDriftMin, upwardDriftMax);

            ParticleSystemRenderer renderer = _ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = string.IsNullOrEmpty(sortingLayerName) ? GameConstants.SortVFX : sortingLayerName;
                renderer.sortingOrder = sortingOrder;
                if (particleMaterial != null)
                {
                    renderer.sharedMaterial = particleMaterial;
                }
            }

            if (!_ps.isPlaying) _ps.Play();
            _ready = true;
        }

        private void Update()
        {
            if (!_ready || _ps == null) return;
            // Gentle sinusoidal horizontal cavern breeze sway across the drifting spores
            float wind = Mathf.Sin(Time.time * swayFrequency + swayPhaseOffset) * swayAmplitude;
            _velModule.x = new ParticleSystem.MinMaxCurve(wind - swayAmplitude * 0.45f, wind + swayAmplitude * 0.45f);
        }

        public void ConfigureLayer(
            string sortLayer, int sortOrd, Vector3 size, float rate,
            float minSz, float maxSz, int maxCount, float swayAmp, float swayFreq, float phase)
        {
            sortingLayerName = sortLayer;
            sortingOrder = sortOrd;
            boxSize = size;
            emissionRate = rate;
            minParticleSize = minSz;
            maxParticleSize = maxSz;
            maxParticles = maxCount;
            swayAmplitude = swayAmp;
            swayFrequency = swayFreq;
            swayPhaseOffset = phase;
        }

        public void SetMaterial(Material mat)
        {
            particleMaterial = mat;
        }
    }
}
