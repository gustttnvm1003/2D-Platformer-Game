using UnityEngine;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Game Over screen displayed upon player defeat, offering Checkpoint Respawn
    /// or Return to Main Menu.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (respawnButton != null) respawnButton.onClick.AddListener(OnRespawnClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(state == GameState.GameOver);
            }
        }

        private void OnRespawnClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.RespawnAtCheckpoint();
        }

        private void OnMainMenuClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
        }
    }
}
