using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Pause Menu overlay active during GameState.Paused.
    /// Connects Resume, Upgrades, Settings, Save Progress, and Return to Main Menu,
    /// with smooth unscaled-time cross-transitions when switching to/from Settings.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private InventoryMenuController inventoryMenu;
        [SerializeField] private SettingsMenuController settingsMenu;
        [SerializeField] private ControlsMenuController controlsMenu;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Text statusFeedbackText;

        private const float TransitionDuration = 0.16f;
        private CanvasGroup _canvasGroup;
        private Transform _cardTransform;
        private Coroutine _transitionCoroutine;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (inventoryButton != null) inventoryButton.onClick.AddListener(OnInventoryClicked);
            if (upgradesButton != null) upgradesButton.onClick.AddListener(OnUpgradesClicked);
            if (controlsButton != null) controlsButton.onClick.AddListener(OnControlsClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            bool show = state == GameState.Paused;
            if (statusFeedbackText != null) statusFeedbackText.text = string.Empty;

            if (inventoryMenu != null && state != GameState.InventoryMenu) inventoryMenu.HideImmediate();
            if (settingsMenu != null) settingsMenu.HideImmediate();
            if (controlsMenu != null) controlsMenu.HideImmediate();

            if (show)
            {
                TransitionPauseVisibility(true);
            }
            else
            {
                HideImmediate();
            }
        }

        private void TransitionPauseVisibility(bool showing)
        {
            MenuOverlayTransitionHelper.EnsureComponents(panelRoot, ref _canvasGroup, ref _cardTransform);

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }

            if (showing && panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (!gameObject.activeInHierarchy || _canvasGroup == null)
            {
                MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, showing);
                return;
            }

            _transitionCoroutine = StartCoroutine(MenuOverlayTransitionHelper.AnimateFadeScaleRoutine(
                _canvasGroup, _cardTransform, showing, TransitionDuration, 0.94f, false, delegate
                {
                    _transitionCoroutine = null;
                    if (!showing && panelRoot != null)
                    {
                        panelRoot.SetActive(false);
                    }
                }));
        }

        private void HideImmediate()
        {
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            MenuOverlayTransitionHelper.SetImmediate(panelRoot, _canvasGroup, _cardTransform, false);
        }

        private void OnResumeClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        private void OnInventoryClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

            TransitionPauseVisibility(false);

            if (inventoryMenu != null)
            {
                inventoryMenu.ShowFromPause(HandleSubMenuClosed);
            }
        }

        private void OnUpgradesClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.UpgradeMenu);
        }

        private void OnControlsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

            TransitionPauseVisibility(false);

            if (controlsMenu != null)
            {
                controlsMenu.Show(HandleSubMenuClosed);
            }
        }

        private void OnSettingsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

            // Smoothly fade/shrink the Pause Menu away so Settings takes the screen cleanly,
            // and smoothly restore the Pause Menu page when Settings is closed.
            TransitionPauseVisibility(false);

            if (settingsMenu != null)
            {
                settingsMenu.Show(HandleSubMenuClosed);
            }
        }

        private void HandleSubMenuClosed()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Paused)
            {
                TransitionPauseVisibility(true);
            }
        }

        private void OnSaveClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCheckpoint();
            if (GameManager.Instance != null) GameManager.Instance.SaveCurrentProgress();
            if (statusFeedbackText != null) statusFeedbackText.text = "Progress saved to sanctuary record.";
        }

        private void OnMainMenuClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
        }
    }
}
