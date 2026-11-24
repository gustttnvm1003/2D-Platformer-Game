using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Bidirectional Metroidvania room/zone trigger.
    /// Tracks active containing zones across both forward entry and backward return
    /// so the camera always transitions smoothly in every direction.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class AreaZoneTrigger : MonoBehaviour
    {
        [SerializeField] private string areaId = "zone_glade";
        [SerializeField] private string areaTitle = "Sunlit Moss Glade";
        [SerializeField] private string areaSubtitle = "Ancient Sanctuary";
        [SerializeField] private Vector2 cameraMinBounds = new Vector2(-10f, -6f);
        [SerializeField] private Vector2 cameraMaxBounds = new Vector2(36f, 14f);

        private static string _lastActiveAreaId = string.Empty;
        private static readonly List<AreaZoneTrigger> _activeZones = new List<AreaZoneTrigger>();
        private static readonly List<AreaZoneTrigger> _allZones = new List<AreaZoneTrigger>();

        private BoxCollider2D _boxCollider;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _boxCollider.isTrigger = true;
            if (!_allZones.Contains(this))
            {
                _allZones.Add(this);
            }
        }

        private void Start()
        {
            // Ensure initial zone framing is applied immediately even before the first physics step
            GameObject pObj = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (pObj != null && _boxCollider != null && _boxCollider.bounds.Contains(pObj.transform.position + Vector3.up * 0.5f))
            {
                ActivateZone(this, pObj);
            }
        }

        private void OnDestroy()
        {
            _allZones.Remove(this);
            _activeZones.Remove(this);
        }

        public void Configure(string id, string title, string subtitle, Vector2 minBounds, Vector2 maxBounds)
        {
            areaId = id;
            areaTitle = title;
            areaSubtitle = subtitle;
            cameraMinBounds = minBounds;
            cameraMaxBounds = maxBounds;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || other.isTrigger || !other.CompareTag(GameConstants.TagPlayer)) return;
            ActivateZone(this, other.gameObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_boxCollider == null || !_boxCollider.enabled) return;
            if (other == null || other.isTrigger || !other.CompareTag(GameConstants.TagPlayer)) return;

            _activeZones.Remove(this);
            PruneNullZones();

            if (CaveTransitionManager.Instance != null && CaveTransitionManager.Instance.IsTransitioning)
            {
                return;
            }

            if (_activeZones.Count > 0)
            {
                // Returning into an overlapping/previous zone immediately restores its camera bounds
                AreaZoneTrigger nextZone = _activeZones[_activeZones.Count - 1];
                ApplyZoneBoundsAndBanner(nextZone, other.gameObject);
                return;
            }

            // Edge safety fallback: if contiguous borders caused Exit before Enter, resolve from player position
            Vector3 probePos = other.transform.position + Vector3.up * 0.5f;
            AreaZoneTrigger fallback = FindContainingOrNearestZone(probePos, this);
            if (fallback != null)
            {
                ActivateZone(fallback, other.gameObject);
            }
        }

        private static void ActivateZone(AreaZoneTrigger zone, GameObject playerObj)
        {
            if (zone == null) return;
            PruneNullZones();
            _activeZones.Remove(zone);
            _activeZones.Add(zone);
            ApplyZoneBoundsAndBanner(zone, playerObj);
        }

        private static void ApplyZoneBoundsAndBanner(AreaZoneTrigger zone, GameObject playerObj)
        {
            if (zone == null) return;

            CameraFollow2D cam = FindObjectOfType<CameraFollow2D>();
            if (cam != null)
            {
                cam.SetZoneBounds(zone.cameraMinBounds, zone.cameraMaxBounds);
            }

            PlayerController player = playerObj != null ? playerObj.GetComponentInParent<PlayerController>() : null;
            if (player != null)
            {
                player.RecordDiscoveredArea(zone.areaId);
            }

            if (_lastActiveAreaId != zone.areaId)
            {
                _lastActiveAreaId = zone.areaId;
                GameEvents.RaiseAreaEntered(zone.areaTitle, zone.areaSubtitle);
            }
        }

        private static AreaZoneTrigger FindContainingOrNearestZone(Vector3 worldPos, AreaZoneTrigger excludeZone)
        {
            AreaZoneTrigger best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _allZones.Count; i++)
            {
                AreaZoneTrigger z = _allZones[i];
                if (z == null || z == excludeZone || z._boxCollider == null) continue;
                Bounds b = z._boxCollider.bounds;
                if (b.Contains(worldPos))
                {
                    return z;
                }
                float d = (b.ClosestPoint(worldPos) - worldPos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = z;
                }
            }
            return best;
        }

        private static void PruneNullZones()
        {
            for (int i = _activeZones.Count - 1; i >= 0; i--)
            {
                if (_activeZones[i] == null) _activeZones.RemoveAt(i);
            }
            for (int i = _allZones.Count - 1; i >= 0; i--)
            {
                if (_allZones[i] == null) _allZones.RemoveAt(i);
            }
        }

        public static void ResetActiveAreaTracking()
        {
            _lastActiveAreaId = string.Empty;
            _activeZones.Clear();
        }

        public static void ClearActiveZonesForTransition()
        {
            _activeZones.Clear();
        }

        public static void SetCurrentAreaBanner(string id, string title, string subtitle)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_lastActiveAreaId != id)
            {
                _lastActiveAreaId = id;
                GameEvents.RaiseAreaEntered(title, subtitle);
            }
        }
    }
}
