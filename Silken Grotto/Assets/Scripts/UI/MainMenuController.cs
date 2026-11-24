using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Main Menu controller wiring New Game, Continue (when save exists), Settings, and Quit.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private ControlsMenuController controlsMenu;
        [SerializeField] private SettingsMenuController settingsMenu;
        [SerializeField] private Text saveStatusText;
        [SerializeField] private Text controlsLegendText;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGameClicked);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
            if (controlsButton != null) controlsButton.onClick.AddListener(OnControlsClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnEnable()
        {
            ControlBindingsManager.OnBindingsChanged += RefreshControlsLegend;
        }

        private void OnDisable()
        {
            ControlBindingsManager.OnBindingsChanged -= RefreshControlsLegend;
        }

        private void Start()
        {
            bool hasSave = SaveManager.HasValidSaveFile();
            if (continueButton != null)
            {
                continueButton.interactable = hasSave;
            }
            if (saveStatusText != null)
            {
                saveStatusText.text = hasSave
                    ? "Sanctuary Save Record Detected — Continue Available"
                    : "Begin a New Journey into the Moss Grotto";
            }
            RefreshControlsLegend();
        }

        private void RefreshControlsLegend()
        {
            if (controlsLegendText != null)
            {
                controlsLegendText.text = ControlBindingsManager.BuildMainMenuLegend();
            }
        }

        private void OnControlsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (controlsMenu != null) controlsMenu.Show(null);
        }

        private void OnNewGameClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
        }

        private void OnContinueClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.ContinueGame();
        }

        private void OnSettingsClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (settingsMenu != null) settingsMenu.Show();
        }

        private void OnQuitClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }
}
