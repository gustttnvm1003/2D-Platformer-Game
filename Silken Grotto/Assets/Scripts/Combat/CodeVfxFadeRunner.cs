using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Lightweight self-cleaning fade/motion runner for code-drawn LineRenderer VFX bursts.
    /// </summary>
    public class CodeVfxFadeRunner : MonoBehaviour
    {
        private LineRenderer[] _lines;
        private Color _baseColor;
        private float _baseWidth;
        private float _duration;
        private float _elapsed;
        private Vector3 _velocity;
        private float _endScale;

        public void Initialize(LineRenderer[] lines, Color baseColor, float baseWidth, float duration, Vector3 velocity, float endScale)
        {
            _lines = lines;
            _baseColor = baseColor;
            _baseWidth = baseWidth;
            _duration = Mathf.Max(0.05f, duration);
            _velocity = velocity;
            _endScale = endScale;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(_elapsed / _duration);
            if (u >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += _velocity * Time.deltaTime;
            float sc = Mathf.Lerp(1f, _endScale, u);
            transform.localScale = Vector3.one * sc;

            float alpha = Mathf.Pow(1f - u, 1.65f);
            if (_lines != null)
            {
                for (int i = 0; i < _lines.Length; i++)
                {
                    LineRenderer lr = _lines[i];
                    if (lr == null) continue;
                    Color c = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * alpha);
                    lr.startColor = c;
                    lr.endColor = c;
                    lr.startWidth = _baseWidth * Mathf.Lerp(1f, 0.25f, u);
                    lr.endWidth = _baseWidth * Mathf.Lerp(0.85f, 0.1f, u);
                }
            }
        }
    }
}
