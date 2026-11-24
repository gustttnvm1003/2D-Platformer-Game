using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Modular coordinator for seamless Area / Cave Entrance Transitions (~0.95s):
    /// 1. Walk-out & input lock (150ms-200ms) using existing walk kinematics & footsteps
    /// 2. Rapid fade-to-black (#000000, 200ms-250ms) while gently dimming HUD to ~35%
    /// 3. Destination teleport, immediate camera bounds snap, ambient/BGM crossfade, & hold (150ms-250ms)
    /// 4. Fade-from-black & auto-walk-in (250ms-300ms) restoring HUD opacity & player control.
    /// </summary>
    public class CaveTransitionManager : MonoBehaviour
    {
        public static CaveTransitionManager Instance { get; private set; }

        [Header("UI Transition Overlay")]
        [SerializeField] private Image blackOverlayImage;
        [SerializeField] private HUDController hudController;

        [Header("Choreography Timings (~0.72s Snappy Fade + Walk)")]
        [SerializeField] private float walkOutDuration = 0.14f;
        [SerializeField] private float fadeToBlackDuration = 0.20f;
        [SerializeField] private float holdBlackDuration = 0.14f;
        [SerializeField] private float fadeFromBlackDuration = 0.24f;
        [SerializeField] private float postExitCooldown = 0.30f;
        [SerializeField] private float walkOutInputStrength = 0.56f;
        [SerializeField] private float walkInInputStrength = 0.62f;
        [SerializeField] private float hudDimAlpha = 0.35f;

        private bool _isTransitioning;
        private float _autoWalkHorizontal;
        private float _globalCooldownUntil;

        public bool IsTransitioning { get { return _isTransitioning; } }
        public float AutoWalkHorizontal { get { return _autoWalkHorizontal; } }
        public bool IsInCooldown { get { return Time.unscaledTime < _globalCooldownUntil; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (hudController == null) hudController = FindObjectOfType<HUDController>();
            EnsureOverlay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ConfigureUI(Image overlay, HUDController hud)
        {
            blackOverlayImage = overlay;
            hudController = hud;
            if (blackOverlayImage != null)
            {
                blackOverlayImage.color = new Color(0f, 0f, 0f, 0f);
                blackOverlayImage.raycastTarget = false;
            }
        }

        private void EnsureOverlay()
        {
            if (hudController == null) hudController = FindObjectOfType<HUDController>();
            if (blackOverlayImage == null && hudController != null)
            {
                Transform existing = hudController.transform.Find("CaveFadeOverlay");
                if (existing != null) blackOverlayImage = existing.GetComponent<Image>();
            }
            if (blackOverlayImage == null && hudController != null)
            {
                GameObject go = new GameObject("CaveFadeOverlay");
                go.transform.SetParent(hudController.transform, false);
                go.transform.SetAsLastSibling();
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                blackOverlayImage = go.AddComponent<Image>();
            }
            if (blackOverlayImage != null)
            {
                blackOverlayImage.color = new Color(0f, 0f, 0f, 0f);
                blackOverlayImage.raycastTarget = false;
            }
        }

        public void BeginPortalTransition(CaveEntrancePortal sourcePortal, PlayerController player)
        {
            if (_isTransitioning || IsInCooldown || sourcePortal == null || player == null || !player.IsAlive) return;
            EnsureOverlay();
            StartCoroutine(TransitionRoutine(sourcePortal, player));
        }

        private IEnumerator TransitionRoutine(CaveEntrancePortal src, PlayerController player)
        {
            _isTransitioning = true;
            if (hudController == null) hudController = FindObjectOfType<HUDController>();

            PlayerMovement move = player.Movement;
            PlayerCombat combat = player.Combat;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            CameraFollow2D cam = FindObjectOfType<CameraFollow2D>();
            CaveEntrancePortal dest = src.LinkedDestination;

            int enterDir = src.WalkDirection != 0 ? (src.WalkDirection > 0 ? 1 : -1) : (move != null ? move.FacingDirection : 1);
            int arriveDir = dest != null && dest.ArrivalWalkDirection != 0 ? dest.ArrivalWalkDirection : enterDir;

            player.CancelBindHeal();
            if (combat != null) combat.CancelActiveAttack();
            if (move != null)
            {
                move.CancelMantleIfActive();
                move.SetFacing(enterDir);
            }

            // Step A: Walk-out forward into glowing edge with input lock (~180ms)
            _autoWalkHorizontal = enterDir * walkOutInputStrength;
            float t = 0f;
            while (t < walkOutDuration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Step B: Rapid fade-to-black (#000000, ~220ms) + HUD dim to ~35% + start 650ms ambience/BGM crossfade
            if (AudioManager.Instance != null && dest != null)
            {
                AudioManager.Instance.CrossfadeCavernAmbience(dest.UseDarkStoneAcoustics, 0.65f);
            }

            t = 0f;
            while (t < fadeToBlackDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeToBlackDuration));
                SetFadeAndHud(u, Mathf.Lerp(1f, hudDimAlpha, u));
                yield return null;
            }
            SetFadeAndHud(1f, hudDimAlpha);

            // Step C: Hold black (~180ms), swap zone/portal position & snap camera immediately
            _autoWalkHorizontal = 0f;
            if (dest != null)
            {
                dest.SuppressReturnTriggerUntilExit();
                AreaZoneTrigger.ClearActiveZonesForTransition();
                if (rb != null) rb.velocity = Vector2.zero;
                player.transform.position = dest.GetSpawnWorldPosition();
                if (move != null) move.SetFacing(arriveDir);

                if (cam != null)
                {
                    cam.SetZoneBounds(dest.TargetCamMinBounds, dest.TargetCamMaxBounds);
                    cam.SnapToTarget();
                }

                if (StageProgressionManager.Instance != null && !string.IsNullOrEmpty(dest.RoomId))
                {
                    StageProgressionManager.Instance.ActivateStage(dest.RoomId);
                }

                if (!string.IsNullOrEmpty(dest.RoomId))
                {
                    player.RecordDiscoveredArea(dest.RoomId);
                    AreaZoneTrigger.SetCurrentAreaBanner(dest.RoomId, dest.RoomTitle, dest.RoomSubtitle);
                }
            }

            yield return new WaitForSeconds(holdBlackDuration);

            // Step D: Fade-from-black + auto-walk-in (~280ms -> ~65px-85px entry walk) + restore HUD opacity
            _autoWalkHorizontal = arriveDir * walkInInputStrength;
            t = 0f;
            while (t < fadeFromBlackDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeFromBlackDuration));
                SetFadeAndHud(1f - u, Mathf.Lerp(hudDimAlpha, 1f, u));
                yield return null;
            }

            SetFadeAndHud(0f, 1f);
            _autoWalkHorizontal = 0f;
            _globalCooldownUntil = Time.unscaledTime + postExitCooldown;
            _isTransitioning = false;
        }

        private void SetFadeAndHud(float fadeAlpha, float hudAlpha)
        {
            if (blackOverlayImage != null)
            {
                Color c = blackOverlayImage.color;
                c.a = Mathf.Clamp01(fadeAlpha);
                blackOverlayImage.color = c;
            }
            if (hudController != null)
            {
                hudController.SetTransitionHudAlpha(hudAlpha);
            }
        }
    }
}
