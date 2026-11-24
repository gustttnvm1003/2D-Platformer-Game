using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Visual-only ambient motion for shallow reflective subterranean water pools
    /// and floating lily pads. Applies subtle surface ripple shimmer without altering
    /// any colliders or gameplay physics.
    /// </summary>
    public class WaterSurfaceShimmer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private float shimmerSpeed = 1.6f;
        [SerializeField] private float alphaPulseAmount = 0.08f;
        [SerializeField] private float horizontalRippleScale = 0.015f;
        [SerializeField] private float verticalBobAmount = 0.025f;
        [SerializeField] private float phaseOffset = 0f;

        private Vector3 _baseLocalPos;
        private Vector3 _baseLocalScale;
        private Color _baseColor;
        private bool _initialized;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }
            _baseLocalPos = transform.localPosition;
            _baseLocalScale = transform.localScale;
            if (targetRenderer != null)
            {
                _baseColor = targetRenderer.color;
            }
            if (Mathf.Approximately(phaseOffset, 0f))
            {
                phaseOffset = (transform.position.x * 0.37f + transform.position.y * 0.21f);
            }
            _initialized = true;
        }

        public void Configure(float speed, float alphaPulse, float rippleScale, float bobAmount, float phase)
        {
            shimmerSpeed = speed;
            alphaPulseAmount = alphaPulse;
            horizontalRippleScale = rippleScale;
            verticalBobAmount = bobAmount;
            phaseOffset = phase;
        }

        private void Update()
        {
            if (!_initialized) return;

            float t = Time.time * shimmerSpeed + phaseOffset;
            float wave = Mathf.Sin(t);
            float microWave = Mathf.Cos(t * 1.65f);

            transform.localPosition = new Vector3(
                _baseLocalPos.x,
                _baseLocalPos.y + wave * verticalBobAmount,
                _baseLocalPos.z
            );

            transform.localScale = new Vector3(
                _baseLocalScale.x * (1f + microWave * horizontalRippleScale),
                _baseLocalScale.y * (1f - microWave * horizontalRippleScale * 0.5f),
                _baseLocalScale.z
            );

            if (targetRenderer != null)
            {
                Color c = _baseColor;
                c.a = Mathf.Clamp01(_baseColor.a + wave * alphaPulseAmount);
                targetRenderer.color = c;
            }
        }
    }
}
