using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Functional Settings Menu driving real AudioManager volumes, Fullscreen mode,
    /// Display Resolution cycling, and Camera Screen Shake toggle, with smooth
    /// unscaled-time fade & scale transitions so it works cleanly even when paused.
    /// </summary>
    public class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle screenShakeToggle;
        [SerializeField] private Button resolutionCycleButton;
        [SerializeField] private Text resolutionButtonText;
        [SerializeField] private Button closeButton;

        private const float TransitionDuration = 0.18f;
        private readonly Vector2Int[] _resolutions = new Vector2Int[]
        {
            new Vector2Int(1920, 1080),
            new Vector2Int(1600, 900),
            new Vector2Int(1280, 720)
        };
        private int _resolutionIndex = 0;

        private CanvasGroup _canvasGroup;
        private Transform _cardTransform;
        private Coroutine _transitionCoroutine;
        private Action _onClosedCallback;
        private bool _isOpen;

        public bool IsOpen { get { return _isOpen; } }

        private void Awake()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
            if (resolutionCycleButton != null) resolutionCycleButton.onClick.AddListener(OnCycleResolution);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);

            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
            _isOpen = false;
        }

        public void Show()
        {
            Show(null);
        }

        public void Show(Action onClosed)
        {
            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);
            _onClosedCallback = onClosed;
            _isOpen = true;

            if (panelRoot != null)
            {
                // Always render above Pause Menu / Main Menu cards in the same Canvas
                panelRoot.transform.SetAsLastSibling();
                panelRoot.SetActive(true);
            }

            SyncUIFromCurrentSettings();
            StartTransition(true, null);
        }

        public void Hide()
        {
            if (!_isOpen && (panelRoot == null || !panelRoot.activeSelf)) return;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            PlayerPrefs.Save();
            _isOpen = false;

            Action closedCb = _onClosedCallback;
            _onClosedCallback = null;

            StartTransition(false, delegate
            {
                if (panelRoot != null) panelRoot.SetActive(false);
                if (closedCb != null) closedCb();
            });
        }

        public void HideImmediate()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            _isOpen = false;
            _onClosedCallback = null;
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
        }

        private void StartTransition(bool showing, Action onComplete)
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (!gameObject.activeInHierarchy || _canvasGroup == null)
            {
                MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, showing);
                if (onComplete != null) onComplete();
                return;
            }

            _transitionCoroutine = StartCoroutine(MenuOverlayTransitionHelper.AnimateFadeScaleRoutine(
                _canvasGroup, _cardTransform, showing, TransitionDuration, 0.93f, true, delegate
                {
                    _transitionCoroutine = null;
                    if (onComplete != null) onComplete();
                }));
        }

        private void SyncUIFromCurrentSettings()
        {
            float master = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : PlayerPrefs.GetFloat(GameConstants.PrefMasterVolume, 0.85f);
            float music = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : PlayerPrefs.GetFloat(GameConstants.PrefMusicVolume, 0.65f);
            float sfx = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : PlayerPrefs.GetFloat(GameConstants.PrefSfxVolume, 0.85f);

            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(master);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(music);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(sfx);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            if (screenShakeToggle != null) screenShakeToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(GameConstants.PrefScreenShake, 1) == 1);

            UpdateResolutionLabel();
        }

        private void OnMasterChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(val);
            else PlayerPrefs.SetFloat(GameConstants.PrefMasterVolume, val);
        }

        private void OnMusicChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
            else PlayerPrefs.SetFloat(GameConstants.PrefMusicVolume, val);
        }

        private void OnSfxChanged(float val)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(val);
            else PlayerPrefs.SetFloat(GameConstants.PrefSfxVolume, val);
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            PlayerPrefs.SetInt(GameConstants.PrefFullscreen, isFullscreen ? 1 : 0);
            Vector2Int res = _resolutions[_resolutionIndex];
            Screen.SetResolution(res.x, res.y, isFullscreen);
        }

        private void OnScreenShakeChanged(bool enabled)
        {
            PlayerPrefs.SetInt(GameConstants.PrefScreenShake, enabled ? 1 : 0);
            CameraFollow2D cam = FindObjectOfType<CameraFollow2D>();
            if (cam != null) cam.ScreenShakeEnabled = enabled;
        }

        private void OnCycleResolution()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
            Vector2Int res = _resolutions[_resolutionIndex];
            Screen.SetResolution(res.x, res.y, Screen.fullScreen);
            UpdateResolutionLabel();
        }

        private void UpdateResolutionLabel()
        {
            if (resolutionButtonText != null)
            {
                Vector2Int res = _resolutions[_resolutionIndex];
                resolutionButtonText.text = string.Format("Resolution: {0} x {1}", res.x, res.y);
            }
        }
    }
}
