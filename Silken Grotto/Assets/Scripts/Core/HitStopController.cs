using System.Collections;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Provides crisp melee hit-freeze frames (hitstop) for weighty combat feel
    /// without breaking Pause or Game Over timeScale state.
    /// </summary>
    public class HitStopController : MonoBehaviour
    {
        private Coroutine _activeCoroutine;
        private bool _isHitStopping;

        public void TriggerHitStop(float durationSeconds, float frozenScale = 0.04f)
        {
            if (durationSeconds <= 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
            }

            _activeCoroutine = StartCoroutine(HitStopRoutine(durationSeconds, frozenScale));
        }

        private IEnumerator HitStopRoutine(float duration, float frozenScale)
        {
            _isHitStopping = true;
            Time.timeScale = Mathf.Clamp(frozenScale, 0f, 1f);
            yield return new WaitForSecondsRealtime(duration);
            _isHitStopping = false;

            if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
            {
                Time.timeScale = 1f;
            }

            _activeCoroutine = null;
        }

        private void OnDisable()
        {
            if (_isHitStopping)
            {
                _isHitStopping = false;
                if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
                {
                    Time.timeScale = 1f;
                }
            }
        }
    }
}
