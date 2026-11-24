using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Softly animates diagonal volumetric god-ray light beams using additive/screen
    /// blending, subtle breathing intensity, and gentle atmospheric lateral sway.
    /// Strictly visual — preserves BiomeAtmosphereController base alpha blending.
    /// </summary>
    public class VolumetricGodRayAnimator : MonoBehaviour
    {
        [SerializeField] private float breathSpeed = 0.65f;
        [SerializeField] private float breathScaleAmount = 0.025f;
        [SerializeField] private float swaySpeed = 0.38f;
        [SerializeField] private float swayAmplitudeX = 0.24f;
        [SerializeField] private float phaseOffset = 0f;

        private Vector3 _baseScale;
        private float _lastSwayX;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (Mathf.Approximately(phaseOffset, 0f))
            {
                phaseOffset = transform.position.x * 0.13f;
            }
        }

        public void Configure(float speed, float scaleAmp, float swaySpd, float swayAmp, float phase)
        {
            breathSpeed = speed;
            breathScaleAmount = scaleAmp;
            swaySpeed = swaySpd;
            swayAmplitudeX = swayAmp;
            phaseOffset = phase;
        }

        private void LateUpdate()
        {
            float t = Time.time;
            float breath = Mathf.Sin(t * breathSpeed + phaseOffset);
            float swayX = Mathf.Sin(t * swaySpeed + phaseOffset * 1.3f) * swayAmplitudeX;

            // Apply delta sway so ParallaxLayer horizontal tracking stays 100% intact!
            float deltaSway = swayX - _lastSwayX;
            _lastSwayX = swayX;
            transform.position = new Vector3(transform.position.x + deltaSway, transform.position.y, transform.position.z);

            transform.localScale = new Vector3(
                _baseScale.x * (1f + breath * breathScaleAmount),
                _baseScale.y,
                _baseScale.z
            );
        }
    }
}
