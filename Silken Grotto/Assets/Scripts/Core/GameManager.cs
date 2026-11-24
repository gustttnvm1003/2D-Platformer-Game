using UnityEngine;
using UnityEngine.SceneManagement;

namespace SilkenGrotto
{
    /// <summary>
    /// Coordinates global game states (Playing, Paused, UpgradeMenu, GameOver),
    /// scene transitions, and clean Time.timeScale control.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene State")]
        [SerializeField] private bool isMainMenuScene;
        [SerializeField] private PlayerController player;
        [SerializeField] private SaveManager saveManager;

        private GameState _currentState = GameState.Playing;
        public GameState CurrentState { get { return _currentState; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
            _currentState = isMainMenuScene ? GameState.MainMenu : GameState.Playing;

            // Keep solid body movement crisp (prevents body-stacking/wedging while Hitbox & Hurtbox triggers handle all combat)
            Physics2D.IgnoreLayerCollision(GameConstants.LayerPlayer, GameConstants.LayerEnemy, true);
            Physics2D.IgnoreLayerCollision(GameConstants.LayerEnemy, GameConstants.LayerEnemy, true);
        }

        private void Start()
        {
            if (!isMainMenuScene)
            {
                if (player == null) player = FindObjectOfType<PlayerController>();
                if (saveManager == null) saveManager = FindObjectOfType<SaveManager>();

                if (saveManager != null && SaveManager.ShouldLoadOnGameplayStart)
                {
                    saveManager.LoadAndApplyToScene(player);
                }
                else if (player != null)
                {
                    player.InitializeFreshRun();
                }

                SetState(GameState.Playing);
            }
            else
            {
                SetState(GameState.MainMenu);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (Instance == this) Instance = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            bool hideCursor = !isMainMenuScene && _currentState == GameState.Playing;
            Cursor.visible = !hideCursor;
            Cursor.lockState = hideCursor ? CursorLockMode.Confined : CursorLockMode.None;
        }

        private void Update()
        {
            if (isMainMenuScene)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ControlsMenuController menuControls = FindObjectOfType<ControlsMenuController>();
                    if (menuControls != null && menuControls.IsOpen)
                    {
                        if (!menuControls.IsWaitingForRebind)
                        {
                            menuControls.Hide();
                        }
                        return;
                    }
                    SettingsMenuController menuSettings = FindObjectOfType<SettingsMenuController>();
                    if (menuSettings != null && menuSettings.IsOpen)
                    {
                        menuSettings.Hide();
                    }
                }
                return;
            }

            if (_currentState == GameState.Playing && Cursor.visible)
            {
                ApplyCursorState();
            }

            // Never intercept hotkeys while the player is actively pressing a key to rebind controls!
            ControlsMenuController activeControls = FindObjectOfType<ControlsMenuController>();
            if (activeControls != null && activeControls.IsWaitingForRebind)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_currentState == GameState.Playing)
                {
                    SetState(GameState.Paused);
                }
                else if (_currentState == GameState.Paused)
                {
                    SettingsMenuController settings = FindObjectOfType<SettingsMenuController>();
                    InventoryMenuController invPause = FindObjectOfType<InventoryMenuController>();
                    if (settings != null && settings.IsOpen)
                    {
                        settings.Hide();
                    }
                    else if (activeControls != null && activeControls.IsOpen)
                    {
                        activeControls.Hide();
                    }
                    else if (invPause != null && invPause.IsOpenFromPause)
                    {
                        invPause.HideFromPause();
                    }
                    else
                    {
                        SetState(GameState.Playing);
                    }
                }
                else if (_currentState == GameState.UpgradeMenu || _currentState == GameState.InventoryMenu)
                {
                    SetState(GameState.Playing);
                }
            }
            else if (ControlBindingsManager.GetActionDown(ControlAction.Inventory))
            {
                if (_currentState == GameState.Playing)
                {
                    SetState(GameState.InventoryMenu);
                }
                else if (_currentState == GameState.InventoryMenu)
                {
                    SetState(GameState.Playing);
                }
            }
            else if (ControlBindingsManager.GetActionDown(ControlAction.Upgrades))
            {
                if (_currentState == GameState.Playing)
                {
                    SetState(GameState.UpgradeMenu);
                }
                else if (_currentState == GameState.UpgradeMenu)
                {
                    SetState(GameState.Playing);
                }
            }
        }

        public void SetState(GameState newState)
        {
            _currentState = newState;
            switch (_currentState)
            {
                case GameState.Playing:
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                case GameState.UpgradeMenu:
                case GameState.InventoryMenu:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;
            }
            ApplyCursorState();
            GameEvents.RaiseGameStateChanged(_currentState);
        }

        public void ResumeGame()
        {
            if (_currentState == GameState.Paused || _currentState == GameState.UpgradeMenu || _currentState == GameState.InventoryMenu)
            {
                SetState(GameState.Playing);
            }
        }

        public void OpenUpgradeMenu()
        {
            if (_currentState == GameState.Playing)
            {
                SetState(GameState.UpgradeMenu);
            }
        }

        public void RespawnAtCheckpoint()
        {
            if (player == null) player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.RespawnAtLastCheckpoint();
            }
            SetState(GameState.Playing);
        }

        public void SaveCurrentProgress()
        {
            if (saveManager == null) saveManager = FindObjectOfType<SaveManager>();
            if (player == null) player = FindObjectOfType<PlayerController>();
            if (saveManager != null && player != null)
            {
                saveManager.SaveFromScene(player);
            }
        }

        public void StartNewGame()
        {
            SaveManager.ClearSaveFile();
            SaveManager.ShouldLoadOnGameplayStart = false;
            AreaZoneTrigger.ResetActiveAreaTracking();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConstants.SceneGameplay);
        }

        public void ContinueGame()
        {
            SaveManager.ShouldLoadOnGameplayStart = SaveManager.HasValidSaveFile();
            AreaZoneTrigger.ResetActiveAreaTracking();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConstants.SceneGameplay);
        }

        public void ReturnToMainMenu()
        {
            if (!isMainMenuScene)
            {
                SaveCurrentProgress();
            }
            AreaZoneTrigger.ResetActiveAreaTracking();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConstants.SceneMainMenu);
        }

        public void QuitGame()
        {
            if (!isMainMenuScene)
            {
                SaveCurrentProgress();
            }
            Application.Quit();
        }

        private void HandlePlayerDied()
        {
            if (_currentState != GameState.GameOver)
            {
                SetState(GameState.GameOver);
            }
        }
    }
}
