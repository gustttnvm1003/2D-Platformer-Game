using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Smoothly crossfades atmospheric parallax layers and camera clear color
    /// between the Primary Biome (Lush Moss Grotto: #10241E -> #285442) and the
    /// Secondary Sub-Biome (Upper Dark Carved-Stone Cavern: #0A1417 -> #1B2C30)
    /// based on camera world position. Strictly visual — zero gameplay/physics changes.
    /// </summary>
    public class BiomeAtmosphereController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer[] mossGrottoRenderers;
        [SerializeField] private SpriteRenderer[] stoneCavernRenderers;
        [SerializeField] private SpriteRenderer[] mossLightShaftRenderers;
        [SerializeField] private SpriteRenderer[] stoneLightShaftRenderers;

        [Header("Transition Region (Moss Grotto -> Upper Dark Stone Cavern)")]
        [SerializeField] private float stoneBlendStartX = 64f;
        [SerializeField] private float stoneBlendEndX = 86f;
        [SerializeField] private float stoneBlendStartY = 6.5f;
        [SerializeField] private float stoneBlendEndY = 12.5f;
        [SerializeField] private float transitionSpeed = 2.4f;

        [Header("Camera Clear Palette")]
        [SerializeField] private Color mossCameraBg = new Color(0.063f, 0.141f, 0.118f, 1f);  // #10241E
        [SerializeField] private Color stoneCameraBg = new Color(0.039f, 0.078f, 0.090f, 1f); // #0A1417

        private float[] _mossBaseAlpha;
        private float[] _stoneBaseAlpha;
        private float[] _mossShaftBaseAlpha;
        private float[] _stoneShaftBaseAlpha;
        private float _currentStoneWeight;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
            _mossBaseAlpha = CaptureBaseAlphas(mossGrottoRenderers);
            _stoneBaseAlpha = CaptureBaseAlphas(stoneCavernRenderers);
            _mossShaftBaseAlpha = CaptureBaseAlphas(mossLightShaftRenderers);
            _stoneShaftBaseAlpha = CaptureBaseAlphas(stoneLightShaftRenderers);
        }

        private void Start()
        {
            _currentStoneWeight = EvaluateTargetWeight();
            ApplyBlend(_currentStoneWeight);
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
                if (targetCamera == null) return;
            }

            float targetWeight = EvaluateTargetWeight();
            _currentStoneWeight = Mathf.MoveTowards(_currentStoneWeight, targetWeight, Time.deltaTime * transitionSpeed);
            ApplyBlend(_currentStoneWeight);
        }

        private float EvaluateTargetWeight()
        {
            if (targetCamera == null) return 0f;
            Vector3 p = targetCamera.transform.position;
            float wx = Mathf.InverseLerp(stoneBlendStartX, stoneBlendEndX, p.x);
            float wy = Mathf.InverseLerp(stoneBlendStartY, stoneBlendEndY, p.y);
            // Upper Zone 3 (Summit), Zone 4 (Thornbridge), and Zone 5 (Ancient Canopy Arena) blend into Dark Stone Cavern
            float combined = Mathf.Max(wx, wy * 0.85f);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(combined));
        }

        private void ApplyBlend(float stoneWeight)
        {
            float mossWeight = 1f - stoneWeight;

            ApplyGroupAlpha(mossGrottoRenderers, _mossBaseAlpha, mossWeight);
            ApplyGroupAlpha(stoneCavernRenderers, _stoneBaseAlpha, stoneWeight);
            ApplyGroupAlpha(mossLightShaftRenderers, _mossShaftBaseAlpha, mossWeight);
            ApplyGroupAlpha(stoneLightShaftRenderers, _stoneShaftBaseAlpha, stoneWeight);

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = Color.Lerp(mossCameraBg, stoneCameraBg, stoneWeight);
            }
        }

        private static float[] CaptureBaseAlphas(SpriteRenderer[] renderers)
        {
            if (renderers == null) return new float[0];
            float[] alphas = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                alphas[i] = renderers[i] != null ? renderers[i].color.a : 1f;
            }
            return alphas;
        }

        private static void ApplyGroupAlpha(SpriteRenderer[] renderers, float[] baseAlphas, float weight)
        {
            if (renderers == null || baseAlphas == null) return;
            int len = Mathf.Min(renderers.Length, baseAlphas.Length);
            for (int i = 0; i < len; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null) continue;
                Color c = sr.color;
                c.a = baseAlphas[i] * weight;
                sr.color = c;
            }
        }
    }
}
