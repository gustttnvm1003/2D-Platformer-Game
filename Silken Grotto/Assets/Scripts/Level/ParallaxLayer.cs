using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Smooth 2D multi-layer parallax controller with seamless multi-tile wrap-around
    /// and deterministic camera-anchored depth scrolling.
    /// Scroll speed relative to world space = (1 - parallaxFactorX).
    /// Example: 0.10x scroll => parallaxFactorX = 0.90f; 1.20x foreground => -0.20f.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [Range(-0.5f, 1f)] [SerializeField] private float parallaxFactorX = 0.5f;
        [Range(-0.2f, 1f)] [SerializeField] private float parallaxFactorY = 0.85f;
        [SerializeField] private bool infiniteHorizontal = true;
        [SerializeField] private int wrapTileCount = 3;

        private Vector3 _lastCameraPos;
        private float _spriteWorldWidth;

        private void Start()
        {
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                _lastCameraPos = cameraTransform.position;
            }

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                _spriteWorldWidth = sr.bounds.size.x;
            }
        }

        public void Configure(float factorX, float factorY, bool infiniteX)
        {
            parallaxFactorX = factorX;
            parallaxFactorY = factorY;
            infiniteHorizontal = infiniteX;
        }

        public void Configure(float factorX, float factorY, bool infiniteX, int tileCount)
        {
            parallaxFactorX = factorX;
            parallaxFactorY = factorY;
            infiniteHorizontal = infiniteX;
            wrapTileCount = Mathf.Max(1, tileCount);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (UnityEngine.Camera.main != null)
                {
                    cameraTransform = UnityEngine.Camera.main.transform;
                    _lastCameraPos = cameraTransform.position;
                }
                else return;
            }

            Vector3 delta = cameraTransform.position - _lastCameraPos;
            transform.position += new Vector3(delta.x * parallaxFactorX, delta.y * parallaxFactorY, 0f);
            _lastCameraPos = cameraTransform.position;

            if (infiniteHorizontal && _spriteWorldWidth > 1f)
            {
                int tiles = Mathf.Max(1, wrapTileCount);
                float totalSpan = _spriteWorldWidth * tiles;
                float halfSpan = totalSpan * 0.5f;
                float diffX = cameraTransform.position.x - transform.position.x;

                if (diffX > halfSpan)
                {
                    int steps = Mathf.FloorToInt((diffX + halfSpan) / totalSpan);
                    transform.position = new Vector3(transform.position.x + steps * totalSpan, transform.position.y, transform.position.z);
                }
                else if (diffX < -halfSpan)
                {
                    int steps = Mathf.FloorToInt((-diffX + halfSpan) / totalSpan);
                    transform.position = new Vector3(transform.position.x - steps * totalSpan, transform.position.y, transform.position.z);
                }
            }
        }
    }
}
