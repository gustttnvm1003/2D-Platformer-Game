using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Lightweight unintrusive animation driver for stage environmental details:
    /// fluttering crimson banners in windy courtyards, warm flickering trail lanterns
    /// in Bellhart & Mount Fay, and gently swaying cobwebs in Weavenest Atla.
    /// </summary>
    public class AmbientPropAnimator : MonoBehaviour
    {
        [SerializeField] private StageAmbientPropKind propKind = StageAmbientPropKind.CrimsonBanner;
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private float speed = 2.4f;
        [SerializeField] private float amplitude = 5.5f;
        [SerializeField] private float phaseOffset;

        private Vector3 _baseScale;
        private Color _baseColor;
        private bool _captured;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
            CaptureBase();
        }

        private void CaptureBase()
        {
            if (_captured) return;
            _baseScale = transform.localScale;
            _baseColor = targetRenderer != null ? targetRenderer.color : Color.white;
            _captured = true;
        }

        public void Configure(StageAmbientPropKind kind, SpriteRenderer sr, float animSpeed, float animAmp, float phase)
        {
            propKind = kind;
            targetRenderer = sr;
            speed = animSpeed;
            amplitude = animAmp;
            phaseOffset = phase;
            _captured = false;
            CaptureBase();
        }

        private void Update()
        {
            float t = Time.time * speed + phaseOffset;

            switch (propKind)
            {
                case StageAmbientPropKind.CrimsonBanner:
                    float gust = Mathf.Sin(t) * amplitude + Mathf.Sin(t * 2.3f) * (amplitude * 0.35f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, gust);
                    float stretch = 1f + Mathf.Sin(t * 1.9f) * 0.045f;
                    transform.localScale = new Vector3(_baseScale.x * stretch, _baseScale.y, _baseScale.z);
                    break;

                case StageAmbientPropKind.WarmLantern:
                    float flicker = 0.84f + 0.16f * Mathf.Sin(t * 1.7f) + 0.07f * Mathf.Sin(t * 4.9f);
                    transform.localScale = _baseScale * (0.94f + 0.08f * flicker);
                    if (targetRenderer != null)
                    {
                        Color c = _baseColor;
                        c.a = Mathf.Clamp01(_baseColor.a * flicker);
                        targetRenderer.color = c;
                    }
                    break;

                case StageAmbientPropKind.SwayingWeb:
                    float sway = Mathf.Sin(t * 0.85f) * amplitude;
                    transform.localRotation = Quaternion.Euler(0f, 0f, sway);
                    break;

                case StageAmbientPropKind.CathedralGodRay:
                    float breathe = 0.78f + 0.22f * Mathf.Sin(t * 0.55f);
                    if (targetRenderer != null)
                    {
                        Color c = _baseColor;
                        c.a = Mathf.Clamp01(_baseColor.a * breathe);
                        targetRenderer.color = c;
                    }
                    break;

                case StageAmbientPropKind.SwayingFlora:
                    float breeze = Mathf.Sin(t) * amplitude + Mathf.Sin(t * 2.1f) * (amplitude * 0.3f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, breeze);
                    break;

                case StageAmbientPropKind.HangingBellCluster:
                    float pendulum = Mathf.Sin(t * 0.9f) * amplitude;
                    transform.localRotation = Quaternion.Euler(0f, 0f, pendulum);
                    break;
            }
        }
    }
}
