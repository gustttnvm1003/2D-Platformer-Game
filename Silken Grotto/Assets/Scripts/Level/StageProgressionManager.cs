using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    [Serializable]
    public class StageRuntimeBinding
    {
        public StageConfigData config;
        public GameObject backdropRoot;
    }

    /// <summary>
    /// Coordinates progression across all 8 Silken Grotto stages:
    /// toggles per-stage backdrops & Moss Grotto parallax, switches DynamicWeatherController
    /// profiles, updates camera background tint, and syncs stage state on portal transition or Save/Load.
    /// </summary>
    public class StageProgressionManager : MonoBehaviour
    {
        public static StageProgressionManager Instance { get; private set; }

        [Header("Scene Atmosphere References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BiomeAtmosphereController mossGrottoParallax;
        [SerializeField] private GameObject mossGrottoParallaxRoot;
        [SerializeField] private DynamicWeatherController weatherController;

        [Header("Registered Stages (00 Moss Grotto .. 07 Aula Choral Chambers)")]
        [SerializeField] private List<StageRuntimeBinding> stages = new List<StageRuntimeBinding>();

        private StageConfigData _currentStage;
        public StageConfigData CurrentStage { get { return _currentStage; } }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
        }

        private void Start()
        {
            if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
            if (weatherController == null) weatherController = DynamicWeatherController.Instance;

            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                SyncStageFromPosition(player.transform.position);
            }
            else if (stages.Count > 0 && stages[0] != null && stages[0].config != null)
            {
                ActivateStage(stages[0].config.stageId);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SyncStageFromPosition(Vector3 worldPos)
        {
            StageConfigData best = null;
            for (int i = 0; i < stages.Count; i++)
            {
                StageRuntimeBinding b = stages[i];
                if (b == null || b.config == null) continue;
                StageConfigData cfg = b.config;
                if (worldPos.x >= cfg.cameraMinBounds.x - 4f && worldPos.x <= cfg.cameraMaxBounds.x + 4f)
                {
                    best = cfg;
                    break;
                }
            }

            if (best == null && stages.Count > 0 && stages[0] != null)
            {
                best = stages[0].config;
            }

            if (best != null)
            {
                ActivateStage(best.stageId);
            }
        }

        public void ActivateStage(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return;

            StageRuntimeBinding matched = null;
            for (int i = 0; i < stages.Count; i++)
            {
                StageRuntimeBinding b = stages[i];
                if (b == null || b.config == null) continue;
                bool isMatch = b.config.stageId == stageId;
                if (b.backdropRoot != null)
                {
                    b.backdropRoot.SetActive(isMatch);
                    if (isMatch)
                    {
                        StageBackdropController sbc = b.backdropRoot.GetComponent<StageBackdropController>();
                        if (sbc != null) sbc.SyncImmediate();
                    }
                }
                if (isMatch) matched = b;
            }

            if (matched == null) return;
            _currentStage = matched.config;

            bool isPrologMossGrotto = _currentStage.stageIndex == 0;
            if (mossGrottoParallax != null)
            {
                mossGrottoParallax.enabled = isPrologMossGrotto;
            }
            if (mossGrottoParallaxRoot != null)
            {
                mossGrottoParallaxRoot.SetActive(isPrologMossGrotto);
            }

            if (targetCamera == null) targetCamera = UnityEngine.Camera.main;
            if (targetCamera != null && !isPrologMossGrotto)
            {
                targetCamera.backgroundColor = _currentStage.cameraClearColor;
                CameraFollow2D follow = targetCamera.GetComponent<CameraFollow2D>();
                if (follow != null)
                {
                    follow.SetZoneBounds(_currentStage.cameraMinBounds, _currentStage.cameraMaxBounds);
                    follow.SnapToTarget();
                }
            }

            if (weatherController == null) weatherController = DynamicWeatherController.Instance;
            if (weatherController != null)
            {
                weatherController.SetWeatherProfile(_currentStage.weatherType, true);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.CrossfadeStageMusic(_currentStage.stageBgm, _currentStage.stageIndex, 1.15f);
                AudioManager.Instance.CrossfadeCavernAmbience(_currentStage.useDarkStoneAcoustics, 0.70f);
            }
        }
    }
}
