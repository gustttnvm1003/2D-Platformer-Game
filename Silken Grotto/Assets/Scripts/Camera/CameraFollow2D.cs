using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Smooth 2D side-scrolling Metroidvania camera with directional look-ahead,
    /// dynamic zone clamping, and configurable combat screen shake.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target & Framing")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Vector3 baseOffset = new Vector3(0f, 1.85f, -10f);
        [SerializeField] private float smoothTimeX = 0.14f;
        [SerializeField] private float smoothTimeY = 0.18f;
        [SerializeField] private float lookAheadDistance = 1.85f;
        [SerializeField] private float lookAheadSmooth = 4.5f;

        [Header("Zone Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-10.5f, -12f);
        [SerializeField] private Vector2 maxBounds = new Vector2(144.5f, 32f);

        private Camera _cam;
        private float _velX;
        private float _velY;
        private float _currentLookAhead;
        private float _shakeTimer;
        private float _shakeIntensity;
        private bool _screenShakeEnabled = true;

        public bool ScreenShakeEnabled
        {
            get { return _screenShakeEnabled; }
            set
            {
                _screenShakeEnabled = value;
                PlayerPrefs.SetInt(GameConstants.PrefScreenShake, value ? 1 : 0);
            }
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _screenShakeEnabled = PlayerPrefs.GetInt(GameConstants.PrefScreenShake, 1) == 1;
        }

        private void OnEnable()
        {
            GameEvents.OnCameraShakeRequested += HandleShakeRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnCameraShakeRequested -= HandleShakeRequested;
        }

        private void Start()
        {
            ResolveTarget();
            if (target != null)
            {
                Vector3 initPos = ComputeClampedTargetPos(target.position + baseOffset);
                transform.position = initPos;
            }
        }

        private void ResolveTarget()
        {
            if (target == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
                if (p != null) target = p.transform;
            }
            if (target != null && playerMovement == null)
            {
                playerMovement = target.GetComponent<PlayerMovement>();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            playerMovement = newTarget != null ? newTarget.GetComponent<PlayerMovement>() : null;
        }

        public void SetZoneBounds(Vector2 min, Vector2 max)
        {
            useBounds = true;
            minBounds = min;
            maxBounds = max;
        }

        public void SnapToTarget()
        {
            if (target == null) ResolveTarget();
            if (target == null) return;
            float look = playerMovement != null ? playerMovement.FacingDirection * lookAheadDistance : 0f;
            _currentLookAhead = look;
            _velX = 0f;
            _velY = 0f;
            Vector3 desired = new Vector3(
                target.position.x + baseOffset.x + _currentLookAhead,
                target.position.y + baseOffset.y,
                baseOffset.z
            );
            transform.position = ComputeClampedTargetPos(desired);
        }

        private void HandleShakeRequested(float intensity, float duration)
        {
            if (!_screenShakeEnabled) return;
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                ResolveTarget();
                if (target == null) return;
            }

            float dt = Time.deltaTime;
            float desiredLook = playerMovement != null ? playerMovement.FacingDirection * lookAheadDistance : 0f;
            _currentLookAhead = Mathf.Lerp(_currentLookAhead, desiredLook, dt * lookAheadSmooth);

            Vector3 desired = new Vector3(
                target.position.x + baseOffset.x + _currentLookAhead,
                target.position.y + baseOffset.y,
                baseOffset.z
            );

            Vector3 clamped = ComputeClampedTargetPos(desired);

            float nextX = Mathf.SmoothDamp(transform.position.x, clamped.x, ref _velX, smoothTimeX);
            float nextY = Mathf.SmoothDamp(transform.position.y, clamped.y, ref _velY, smoothTimeY);

            Vector3 finalPos = new Vector3(nextX, nextY, baseOffset.z);

            if (_shakeTimer > 0f && _screenShakeEnabled)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                Vector2 jitter = Random.insideUnitCircle * _shakeIntensity;
                finalPos.x += jitter.x;
                finalPos.y += jitter.y;
                if (_shakeTimer <= 0f)
                {
                    _shakeIntensity = 0f;
                }
            }

            transform.position = finalPos;
        }

        private Vector3 ComputeClampedTargetPos(Vector3 pos)
        {
            if (!useBounds) return pos;

            float halfHeight = _cam != null && _cam.orthographic ? _cam.orthographicSize : 6.2f;
            float halfWidth = halfHeight * (_cam != null ? _cam.aspect : (16f / 9f));

            float clampMinX = minBounds.x + halfWidth;
            float clampMaxX = maxBounds.x - halfWidth;
            float clampMinY = minBounds.y + halfHeight;
            float clampMaxY = maxBounds.y - halfHeight;

            if (clampMinX <= clampMaxX) pos.x = Mathf.Clamp(pos.x, clampMinX, clampMaxX);
            else pos.x = (minBounds.x + maxBounds.x) * 0.5f;

            if (clampMinY <= clampMaxY) pos.y = Mathf.Clamp(pos.y, clampMinY, clampMaxY);
            else pos.y = (minBounds.y + maxBounds.y) * 0.5f;

            return pos;
        }
    }
}
