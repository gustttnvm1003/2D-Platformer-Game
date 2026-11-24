using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Gently tracks the active camera with subtle 2.5D parallax inside a stage chamber
    /// so crisp 16:9 hand-drawn stage backdrops always frame edge-to-edge without gaps.
    /// </summary>
    public class StageBackdropController : MonoBehaviour
    {
        [SerializeField] private Transform targetCamera;
        [SerializeField] private Vector2 stageCenter = Vector2.zero;
        [SerializeField] private float parallaxX = 0.14f;
        [SerializeField] private float parallaxY = 0.16f;
        [SerializeField] private float maxOffsetX = 4.2f;
        [SerializeField] private float maxOffsetY = 2.2f;

        private void Awake()
        {
            if (targetCamera == null && UnityEngine.Camera.main != null)
            {
                targetCamera = UnityEngine.Camera.main.transform;
            }
        }

        public void Configure(Transform cam, Vector2 center, float px, float py, float limitX, float limitY)
        {
            targetCamera = cam;
            stageCenter = center;
            parallaxX = px;
            parallaxY = py;
            maxOffsetX = limitX;
            maxOffsetY = limitY;
            SyncImmediate();
        }

        public void SyncImmediate()
        {
            if (targetCamera == null && UnityEngine.Camera.main != null)
            {
                targetCamera = UnityEngine.Camera.main.transform;
            }
            if (targetCamera == null) return;
            UpdatePosition();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                if (UnityEngine.Camera.main != null) targetCamera = UnityEngine.Camera.main.transform;
                if (targetCamera == null) return;
            }
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector3 camPos = targetCamera.position;
            float dx = Mathf.Clamp(-(camPos.x - stageCenter.x) * parallaxX, -maxOffsetX, maxOffsetX);
            float dy = Mathf.Clamp(-(camPos.y - stageCenter.y) * parallaxY, -maxOffsetY, maxOffsetY);
            transform.position = new Vector3(camPos.x + dx, camPos.y + dy, 0f);
        }
    }
}
