using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilkenGrotto
{
    /// <summary>
    /// Attaches delicate crisp harp/bell hover chime SFX to UI Buttons upon pointer hover
    /// or keyboard/gamepad selection.
    /// </summary>
    public class UIButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverIfInteractable();
        }

        public void OnSelect(BaseEventData eventData)
        {
            PlayHoverIfInteractable();
        }

        private void PlayHoverIfInteractable()
        {
            if (_selectable != null && !_selectable.interactable) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIHover();
            }
        }
    }
}
