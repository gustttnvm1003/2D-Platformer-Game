#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SilkenGrotto.Editor
{
    /// <summary>
    /// Automates full Unity asset configuration: Sprite slicing (2D hand-drawn sheets),
    /// ScriptableObject creation, AnimationClips & AnimatorControllers, Prefabs,
    /// TagManager verification, and Scene generation via SilkenGrottoSceneBuilder.
    /// Automatically initializes on first project open if scenes/prefabs are not yet built.
    /// </summary>
    [InitializeOnLoad]
    public static class SilkenGrottoProjectBuilder
    {
        private const string SessionInitKey = "SilkenGrotto_AutoBuildV38_ReaperAndWitchCrests";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/Gameplay/MossGrotto.unity";

        public class BuiltAssets
        {
            public PlayerStatsConfig PlayerStatsConfig;
            public AttackData[] ComboAttacks;
            public AttackData AirForwardAttack;
            public AttackData UpAttack;
            public AttackData DownAttack;

            public EnemyStatsData LancerStats;
            public EnemyStatsData CaptainStats;
            public EnemyStatsData MossmirStats;
            public EnemyStatsData MossgrubStats;
            public EnemyStatsData AknidStats;
            public EnemyStatsData WingedPilgrimStats;
            public EnemyStatsData PilgrimGrovellerStats;
            public EnemyStatsData PilgrimHulkStats;
            public EnemyStatsData PilgrimBellbearerStats;
            public EnemyStatsData CovetousPilgrimStats;
            public EnemyStatsData PondcatcherStats;
            public EnemyStatsData DriznitStats;
            public EnemyStatsData ConchflyStats;
            public EnemyStatsData PharlidStats;
            public EnemyStatsData CoralFurmStats;
            public EnemyStatsData ServitorBoranStats;
            public EnemyStatsData MnemonordStats;
            public EnemyStatsData MnemonidStats;
            public EnemyStatsData DriftlinStats;
            public EnemyStatsData GrandReedStats;
            public EnemyStatsData ReedStats;
            public EnemyStatsData ChoristorStats;
            public EnemyStatsData ChoirHornheadStats;
            public EnemyStatsData ChoirElderStats;
            public EnemyStatsData ChoirFlyerStats;
            public EnemyStatsData DrapemiteStats;
            public EnemyStatsData DrapeflyStats;
            public EnemyStatsData FurmStats;
            public EnemyStatsData WingedFurmStats;
            public EnemyStatsData SkullwingStats;
            public EnemyStatsData PharlidDiverStats;
            public EnemyStatsData MawlingStats;
            public EnemyStatsData MarrowmawStats;
            public EnemyStatsData BellhartMossmirStats;
            public EnemyStatsData BellhartMossgrubStats;

            public List<SkillData> AllSkills = new List<SkillData>();
            public Dictionary<SkillType, SkillData> SkillByType = new Dictionary<SkillType, SkillData>();
            public List<UpgradeData> AllUpgrades = new List<UpgradeData>();
            public List<InventoryItemData> AllInventoryItems = new List<InventoryItemData>();
            public Dictionary<string, InventoryItemData> ItemById = new Dictionary<string, InventoryItemData>();
            public List<StageConfigData> AllStageConfigs = new List<StageConfigData>();
            public Dictionary<string, StageConfigData> StageById = new Dictionary<string, StageConfigData>();

            public AnimatorController PlayerAnimator;
            public AnimatorController EnemyAnimator;
            public AnimatorController MossmirAnimator;
            public AnimatorController MossgrubAnimator;
            public Dictionary<string, AnimatorController> BiomeEnemyAnimators = new Dictionary<string, AnimatorController>();
            public Material SporeParticleMaterial;
            public Material WeatherGlowMaterial;
            public Material WeatherStreakMaterial;
            public Material VolumetricGodRayMaterial;
            public Material AdditiveVFXMaterial;
            public Material DefaultSpriteMaterial;
            public Material EnemyHitFlashMaterial;

            public GameObject PlayerPrefab;
            public GameObject LancerPrefab;
            public GameObject CaptainPrefab;
            public GameObject MossmirPrefab;
            public GameObject MossgrubPrefab;
            public GameObject AknidPrefab;
            public GameObject WingedPilgrimPrefab;
            public GameObject PilgrimGrovellerPrefab;
            public GameObject PilgrimHulkPrefab;
            public GameObject PilgrimBellbearerPrefab;
            public GameObject CovetousPilgrimPrefab;
            public GameObject PondcatcherPrefab;
            public GameObject DriznitPrefab;
            public GameObject ConchflyPrefab;
            public GameObject PharlidPrefab;
            public GameObject CoralFurmPrefab;
            public GameObject ServitorBoranPrefab;
            public GameObject MnemonordPrefab;
            public GameObject MnemonidPrefab;
            public GameObject DriftlinPrefab;
            public GameObject GrandReedPrefab;
            public GameObject ReedPrefab;
            public GameObject ChoristorPrefab;
            public GameObject ChoirHornheadPrefab;
            public GameObject ChoirElderPrefab;
            public GameObject ChoirFlyerPrefab;
            public GameObject DrapemitePrefab;
            public GameObject DrapeflyPrefab;
            public GameObject FurmPrefab;
            public GameObject WingedFurmPrefab;
            public GameObject SkullwingPrefab;
            public GameObject PharlidDiverPrefab;
            public GameObject MawlingPrefab;
            public GameObject MarrowmawPrefab;
            public GameObject BellhartMossmirPrefab;
            public GameObject BellhartMossgrubPrefab;
            public GameObject CheckpointPrefab;
            public GameObject AbilityPedestalPrefab;
            public GameObject UpgradeAltarPrefab;
            public GameObject SilkOrbPrefab;
            public GameObject InventoryPickupPrefab;
            public GameObject BreakableWallPrefab;
            public GameObject SpikeHazardPrefab;
            public GameObject HUDCanvasPrefab;
        }

        static SilkenGrottoProjectBuilder()
        {
            EditorApplication.delayCall += TryAutoBuildOnFirstOpen;
        }

        private static void TryAutoBuildOnFirstOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool(SessionInitKey, false)) return;
            SessionState.SetBool(SessionInitKey, true);

            bool hasMain = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
            bool hasGame = AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) != null;
            bool hasPlayer = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab") != null;
            bool hasGraceIdleAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_IdleGrace1.anim") != null;
            bool hasNewParallax = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/BG_L0_MossBackdrop.png") != null;
            bool hasVolumetricMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_VolumetricGodRay.mat") != null;
            bool hasCore45Sheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Player_Core45_EngineSheet.png") != null;
            bool hasIdleSmoothSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Player_IdleSmooth_Sheet.png") != null;
            bool hasGraceSmoothSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Player_IdleGraceSmooth_Sheet.png") != null;
            bool hasMovementSmoothSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Player_MovementSmooth_Sheet.png") != null;
            bool hasCombatDashSmoothSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Player_CombatDashSmooth_Sheet.png") != null;
            bool hasWallClingAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_WallCling.anim") != null;
            bool hasJumpApexAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_JumpApex.anim") != null;
            bool hasMossmirPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Mossmir.prefab") != null;
            bool hasMossgrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Mossgrub.prefab") != null;
            bool hasMossmirSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/Mossmir_Sheet.png") != null;
            bool hasMossgrubSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/Mossgrub_Sheet.png") != null;
            bool hasDoorwayGlow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_CaveDoorwayGlow.png") != null;
            bool hasLedgeMantleSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/ledge_mantle.png") != null;
            bool hasHealBindSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/heal_bind.png") != null;
            bool hasLedgeMantleAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_LedgeMantle.anim") != null;
            bool hasBindHealAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_BindHeal.anim") != null;
            bool hasSilksongMoveSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/silken_weaver_movement_sheet.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/player_vfx_sheet.png") != null
                && LoadSheetSprites("Assets/Art/Effects/player_vfx_sheet.png", "VFX_DashBurst", 8).Length >= 8
                && AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_HeroLand.anim") != null
                && AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player/Player_LedgeSomersault.anim") != null;
            bool hasDestructibleFoliageLayer = !string.IsNullOrEmpty(LayerMask.LayerToName(GameConstants.LayerDestructibleFoliage));
            bool hasBreakableTallGrass = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BreakableTallGrass.png") != null;
            bool hasHealthCrestFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_HealthCrestFrame.png") != null;
            bool hasSilkSpoolBase = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_SilkSpoolBase.png") != null;
            bool hasShellwoodEnemyArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/aknid.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/pondcatcher.png") != null;
            GameObject aknidGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/Aknid.prefab");
            bool hasShellwoodPrefabs = aknidGo != null && aknidGo.GetComponent<AknidAI>() != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/Pondcatcher.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/Pondcatcher.prefab").GetComponent<PondcatcherAI>() != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/PilgrimBellbearer.prefab") != null;
            bool hasKarakEnemyArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/driznit.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/conchfly.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/pharlid.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/coral_furm.png") != null;
            bool hasKarakPrefabs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/Driznit.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/Conchfly.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/Pharlid.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/CoralFurm.prefab") != null;
            bool hasFayEnemyArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/servitor_boran.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/mnemonord.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/mnemonid.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/driftlin.png") != null;
            bool hasFayPrefabs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/ServitorBoran.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/Mnemonord.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/Mnemonid.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/Driftlin.prefab") != null;
            bool hasChoralEnemyArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/grand_reed.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/reed.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/choristor.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/choir_hornhead.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/choir_elder.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/choir_flyer.png") != null;
            GameObject grandReedGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/GrandReed.prefab");
            GameObject reedGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/Reed.prefab");
            bool hasChoralPrefabs = grandReedGo != null && grandReedGo.transform.Find("FrontBlockBox") != null
                && reedGo != null && reedGo.transform.Find("FrontBlockBox") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/ChoirElder.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/ChoirFlyer.prefab") != null;
            bool hasDrapeEnemyArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/drapemite.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/drapefly.png") != null;
            bool hasDrapePrefabs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Courtyard/Drapemite.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Courtyard/Drapefly.prefab") != null;
            bool hasBellhartEnemyArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/furm.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/winged_furm.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/skullwing.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/pharlid_diver.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/mawling.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/marrowmaw.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/mossmir.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/mossgrub.png") != null;
            GameObject pharlidDiverGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/PharlidDiver.prefab");
            bool hasBellhartPrefabs = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Furm.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/WingedFurm.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Skullwing.prefab") != null
                && pharlidDiverGo != null && pharlidDiverGo.GetComponent<KarakEnemyAI>() != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Mawling.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Marrowmaw.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Mossmir.prefab") != null
                && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Mossgrub.prefab") != null;
            bool hasBiomeEnemySheets = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/Sheets/furm_Sheet.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/Sheets/grand_reed_Sheet.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/Sheets/bellhart_mossmir_Sheet.png") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/Sheets/bellhart_mossgrub_Sheet.png") != null
                && LoadSheetSprites("Assets/Art/Characters/Enemies/Sheets/furm_Sheet.png", "furm_Idle", 12).Length >= 12;
            bool hasBiomeAnimControllers = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Enemies/furmAnimator.controller") != null
                && AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Enemies/grand_reedAnimator.controller") != null
                && AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Enemies/bellhart_mossmirAnimator.controller") != null
                && AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Enemies/bellhart_mossgrubAnimator.controller") != null;
            string mgCtrlFullPath = System.IO.Path.Combine(Application.dataPath, "Animations/Enemies/MossgrubAnimator.controller");
            bool hasIntactMossAnimators = System.IO.File.Exists(mgCtrlFullPath) && System.IO.File.ReadAllText(mgCtrlFullPath).Contains("m_Name: Patrol");

            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GameplayHUDCanvas.prefab");
            bool hasCrestHudLayout = hudPrefab != null && hudPrefab.transform.Find("HUD_GameplayDimGroup/HUD_TopLeft/CrestNeedleFrame") != null;
            bool hasCleanMinimalHud = hudPrefab != null
                && hudPrefab.transform.Find("HUD_GameplayDimGroup/HUD_TopLeft/SilkValueText") == null
                && hudPrefab.transform.Find("InventoryOverlay") != null;
            StageConfigData finalStageCfg = AssetDatabase.LoadAssetAtPath<StageConfigData>("Assets/ScriptableObjects/Stages/Stage_07_AulaChoralChambers.asset");
            bool hasAllStageConfigs = finalStageCfg != null && finalStageCfg.stageBgm != null
                && AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_AulaChoralChambers.wav") != null;
            string idleAnimFullPath = System.IO.Path.Combine(Application.dataPath, "Animations/Player/Player_Idle.anim");
            bool hasRestoredSmoothIdle = System.IO.File.Exists(idleAnimFullPath) && System.IO.File.ReadAllText(idleAnimFullPath).Contains("m_SampleRate: 14");
            bool hasWalkRestSmoothSheet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Player/Player_WalkRestSmooth_Sheet.png") != null
                && LoadSheetSprites("Assets/Art/Characters/Player/Player_WalkRestSmooth_Sheet.png", "SmoothWalk", 12).Length >= 12
                && LoadSheetSprites("Assets/Art/Characters/Player/Player_WalkRestSmooth_Sheet.png", "SmoothRest", 12).Length >= 12
                && LoadSheetSprites("Assets/Art/Characters/Player/Player_WalkRestSmooth_Sheet.png", "SmoothRestLook", 24).Length >= 24;
            bool hasStagesBuiltInScene = false;
            try
            {
                string fullScenePath = System.IO.Path.Combine(Application.dataPath, "Scenes/Gameplay/MossGrotto.unity");
                if (System.IO.File.Exists(fullScenePath))
                {
                    string sceneText = System.IO.File.ReadAllText(fullScenePath);
                    hasStagesBuiltInScene = sceneText.Contains("Portal_00_ExitRight")
                        && sceneText.Contains("Stage_07_AulaChoralChambers")
                        && sceneText.Contains("FogBand_GroundDrift")
                        && !sceneText.Contains("Z1_OneWayHangingRootA")
                        && !sceneText.Contains("Z1_OneWayHangingRootB")
                        && !sceneText.Contains("Z4_WestBridge_BoneArch")
                        && !sceneText.Contains("m_UseOneWay: 1")
                        && sceneText.Contains("SecretShelf_Weavenest")
                        && sceneText.Contains("SecretShelf_ChoralVault")
                        && sceneText.Contains("Checkpoint_stage_04_bellhart_mid")
                        && sceneText.Contains("SecretWall_wall_shellwood_vault")
                        && sceneText.Contains("Shellwood_PilgrimHulkE")
                        && sceneText.Contains("Karak_ConchflyE")
                        && sceneText.Contains("Bellhart_FurmA")
                        && sceneText.Contains("Bellhart_WingedFurmB")
                        && sceneText.Contains("Bellhart_SkullwingB")
                        && sceneText.Contains("Bellhart_PharlidDiverB")
                        && sceneText.Contains("Bellhart_MarrowmawE")
                        && sceneText.Contains("Bellhart_MossgrubC")
                        && !sceneText.Contains("Bellhart_BellMite")
                        && sceneText.Contains("MountFay_ServitorBoranB")
                        && sceneText.Contains("MountFay_Mnemonord")
                        && sceneText.Contains("Courtyard_DrapemiteG")
                        && sceneText.Contains("Courtyard_DrapeflyC")
                        && !sceneText.Contains("Courtyard_ChoirHornheadA")
                        && sceneText.Contains("Aula_GrandReedFinal");
                }
            }
            catch { }

            if (!hasMain || !hasGame || !hasPlayer || !hasGraceIdleAnim || !hasNewParallax || !hasVolumetricMat || !hasCore45Sheet || !hasIdleSmoothSheet || !hasGraceSmoothSheet || !hasMovementSmoothSheet || !hasCombatDashSmoothSheet || !hasWallClingAnim || !hasJumpApexAnim || !hasMossmirPrefab || !hasMossgrubPrefab || !hasMossmirSheet || !hasMossgrubSheet || !hasDoorwayGlow || !hasLedgeMantleSheet || !hasHealBindSheet || !hasLedgeMantleAnim || !hasBindHealAnim || !hasSilksongMoveSheet || !hasWalkRestSmoothSheet || !hasDestructibleFoliageLayer || !hasBreakableTallGrass || !hasHealthCrestFrame || !hasSilkSpoolBase || !hasShellwoodEnemyArt || !hasShellwoodPrefabs || !hasKarakEnemyArt || !hasKarakPrefabs || !hasFayEnemyArt || !hasFayPrefabs || !hasChoralEnemyArt || !hasChoralPrefabs || !hasDrapeEnemyArt || !hasDrapePrefabs || !hasBellhartEnemyArt || !hasBellhartPrefabs || !hasBiomeEnemySheets || !hasBiomeAnimControllers || !hasIntactMossAnimators || !hasCrestHudLayout || !hasCleanMinimalHud || !hasAllStageConfigs || !hasRestoredSmoothIdle || !hasStagesBuiltInScene)
            {
                Debug.Log("[SilkenGrotto] Updating ScriptableObjects, Animations, Prefabs, and playable Scenes...");
                BuildCompleteProject();
            }
        }

        [MenuItem("Silken Grotto/1. Build Complete Project (All Assets & Scenes)", false, 1)]
        public static void BuildCompleteProject()
        {
            EnsureTagsAndLayers();
            ConfigureAllSpriteImporters();

            BuiltAssets assets = new BuiltAssets();
            CreateMaterials(assets);
            CreateScriptableObjects(assets);
            CreateAnimationsAndControllers(assets);
            CreatePrefabs(assets);

            SilkenGrottoSceneBuilder.BuildMainMenuScene(MainMenuScenePath, assets);
            SilkenGrottoSceneBuilder.BuildGameplayScene(GameplayScenePath, assets);

            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            Debug.Log("[SilkenGrotto] Complete project built successfully! Press Play in MossGrotto or open MainMenu scene.");
        }

        [MenuItem("Silken Grotto/2. Open Gameplay Scene (MossGrotto)", false, 20)]
        public static void OpenGameplayScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayScenePath) == null)
            {
                BuildCompleteProject();
                return;
            }
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Silken Grotto/3. Open Main Menu Scene", false, 21)]
        public static void OpenMainMenuScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) == null)
            {
                BuildCompleteProject();
                return;
            }
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        private static void EnsureTagsAndLayers()
        {
            Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagAssets == null || tagAssets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(tagAssets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            string[] neededTags = new string[]
            {
                GameConstants.TagEnemy,
                GameConstants.TagCheckpoint,
                GameConstants.TagHazard,
                GameConstants.TagInteractable,
                GameConstants.TagSecretArea,
                GameConstants.TagBreakable
            };

            for (int i = 0; i < neededTags.Length; i++)
            {
                AddTagIfMissing(tagsProp, neededTags[i]);
            }

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            SetLayerName(layersProp, GameConstants.LayerGround, GameConstants.LayerNameGround);
            SetLayerName(layersProp, GameConstants.LayerOneWayPlatform, GameConstants.LayerNameOneWayPlatform);
            SetLayerName(layersProp, GameConstants.LayerPlayer, GameConstants.LayerNamePlayer);
            SetLayerName(layersProp, GameConstants.LayerEnemy, GameConstants.LayerNameEnemy);
            SetLayerName(layersProp, GameConstants.LayerHazard, GameConstants.LayerNameHazard);
            SetLayerName(layersProp, GameConstants.LayerInteractable, GameConstants.LayerNameInteractable);
            SetLayerName(layersProp, GameConstants.LayerBreakableWall, GameConstants.LayerNameBreakableWall);
            SetLayerName(layersProp, GameConstants.LayerPlayerAttack, "PlayerAttack");
            SetLayerName(layersProp, GameConstants.LayerEnemyAttack, "EnemyAttack");
            SetLayerName(layersProp, GameConstants.LayerDestructibleFoliage, GameConstants.LayerNameDestructibleFoliage);

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddTagIfMissing(SerializedProperty tagsProp, string tag)
        {
            if (tagsProp == null) return;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            int idx = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(idx);
            tagsProp.GetArrayElementAtIndex(idx).stringValue = tag;
        }

        private static void SetLayerName(SerializedProperty layersProp, int index, string layerName)
        {
            if (layersProp == null || index < 0 || index >= layersProp.arraySize) return;
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(index);
            if (sp != null) sp.stringValue = layerName;
        }

        private static void ConfigureAllSpriteImporters()
        {
            // 2D Hand-drawn character sprite sheets (512x512 cells, calibrated to exact 2.284u height & heel pivot.y = 30.5/512 = 0.0596f)
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_Locomotion_Sheet.png",
                "PlayerLoco",
                4, 2, 512, 184.3f, new Vector2(0.5f, 0.0596f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_LocomotionExt_Sheet.png",
                "PlayerLocoExt",
                4, 2, 512, 184.3f, new Vector2(0.5f, 0.0596f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_Action_Sheet.png",
                "PlayerAct",
                4, 2, 512, 184.3f, new Vector2(0.5f, 0.0596f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_ActionExt_Sheet.png",
                "PlayerActExt",
                4, 2, 512, 184.3f, new Vector2(0.5f, 0.0596f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_IdleGrace_Sheet.png",
                "PlayerGrace",
                4, 2, 512, 184.3f, new Vector2(0.5f, 0.0596f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_BenchSit_Sheet.png",
                "PlayerBench",
                4, 1, 512, 184.3f, new Vector2(0.5f, 0.0596f)
            );
            // Upgraded 45-frame core engine sheet (12x6 of 320x320, 3840x1920 -> ppu=116f matches exact 2.284u world scale & floor contact)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_Core45_EngineSheet.png",
                new string[] { "CoreIdle", "CoreRun", "CoreJumpFall", "CoreDash", "CoreSlash", "CoreWall" },
                new int[] { 0, 0, 0, 0, 0, 0 },
                new int[] { 8, 10, 12, 6, 5, 4 },
                320, 116f, new Vector2(0.5f, 0.0656f), 4096
            );
            // Smooth 24-frame cloth-physics Base Idle (6x4 of 320x320, 1920x1280 -> 265px / 116 PPU = 2.2845u canonical height)
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/Player_IdleSmooth_Sheet.png",
                "IdleSmooth",
                6, 4, 320, 116f, new Vector2(0.5f, 0.0656f)
            );
            // Smooth 3x24-frame (72 total) Graceful AFK Extended Idles (12x6 of 320x320 -> 277px / 121.25 PPU = exact 2.2845u & footBot 20px = 0.0625f!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_IdleGraceSmooth_Sheet.png",
                new string[] { "Grace1", "Grace1", "Grace2", "Grace2", "Grace3", "Grace3" },
                new int[] { 0, 12, 0, 12, 0, 12 },
                new int[] { 12, 12, 12, 12, 12, 12 },
                320, 121.25f, new Vector2(0.5f, 0.0625f), 4096
            );
            // Expanded Smooth Run (20f) + Jump/Fall (24f) + Wall Cling/Jump (20f) Sheet (12x6 = 3840x1920)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_MovementSmooth_Sheet.png",
                new string[] { "SmoothRun", "SmoothRun", "SmoothJumpFall", "SmoothJumpFall", "SmoothWall", "SmoothWall" },
                new int[] { 0, 12, 0, 12, 0, 12 },
                new int[] { 12, 8, 12, 12, 12, 8 },
                320, 116f, new Vector2(0.5f, 0.0656f), 4096
            );
            // Expanded Smooth Dash/AirDash (14f) + Needle Slash (12f) Sheet (12x3 = 3840x960)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_CombatDashSmooth_Sheet.png",
                new string[] { "SmoothDash", "SmoothDash", "SmoothSlash" },
                new int[] { 0, 12, 0 },
                new int[] { 12, 2, 12 },
                320, 116f, new Vector2(0.5f, 0.0656f), 4096
            );
            // Modular Ledge Mantle (5x1, 1600x320) & Bind Heal (4x1, 1280x320) Strips matching 116 PPU & ground baseline
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/ledge_mantle.png",
                "LedgeMantle",
                5, 1, 320, 116f, new Vector2(0.5f, 0.0656f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Player/heal_bind.png",
                "HealBind",
                4, 1, 320, 116f, new Vector2(0.5f, 0.0656f)
            );
            // Master 6x8 (2048x1536, 256x256/cell) Silksong-style Movement Sprite Sheet (212px / 92.8 PPU = exact 2.2845u height & 17px baseline!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/silken_weaver_movement_sheet.png",
                new string[] { "SW_Rest", "SW_IdleLand", "SW_Walk", "SW_DashSprint", "SW_SomersaultFall", "SW_AirDashHero" },
                new int[] { 0, 0, 0, 0, 0, 0 },
                new int[] { 8, 8, 8, 8, 8, 8 },
                256, 92.8f, new Vector2(0.5f, 0.06640625f), 2048
            );
            // Smooth 12x4 Upright Walk (12f) + Pinned Bench Rest (12f) + Seated Look-Left/Right Search (24f) Sheet (3840x1280, 320x320/cell @ 116 PPU!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_WalkRestSmooth_Sheet.png",
                new string[] { "SmoothWalk", "SmoothRest", "SmoothRestLook", "SmoothRestLook" },
                new int[] { 0, 0, 0, 12 },
                new int[] { 12, 12, 12, 12 },
                320, 116f, new Vector2(0.5f, 0.065625f), 4096
            );
            // Expansion 5x6 Moveset Sheet (1920x1600, 320x320/cell @ 116 PPU: Glide, Skid/Pivot, Charge Slash, Cross Stitch, Thread Storm!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_MovesetExt_Sheet.png",
                new string[] { "ExtGlide", "ExtSkidPivot", "ExtChargeSlash", "ExtCrossStitch", "ExtThreadStorm" },
                new int[] { 0, 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6, 6 },
                320, 116f, new Vector2(0.5f, 0.065625f), 2048
            );
            // Expansion 4x6 Silk Skills + Taunt Sheet (1920x1280, 320x320/cell @ 116 PPU: Sharpdart, Pale Nails, Rune Rage, Taunt!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_SilkSkillsExt_Sheet.png",
                new string[] { "ExtSharpdart", "ExtPaleNails", "ExtRuneRage", "ExtTaunt" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                320, 116f, new Vector2(0.5f, 0.065625f), 2048
            );
            // Expansion 3x6 Traversal & Needolin Sheet (1920x960, 320x320/cell @ 116 PPU: Harpoon, Silksoar, Needolin!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_TraversalExt_Sheet.png",
                new string[] { "ExtHarpoon", "ExtSilksoar", "ExtNeedolin" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                320, 116f, new Vector2(0.5f, 0.065625f), 2048
            );
            // Expansion 3x6 Reaper & Witch Combat Crests Sheet (1920x960, 320x320/cell @ 116 PPU: Reaper Scythe, Witch Whip, Witch Root Leech Bind!)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Player/Player_CrestsExt_Sheet.png",
                new string[] { "ExtReaperSlash", "ExtWitchWhip", "ExtWitchBind" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                320, 116f, new Vector2(0.5f, 0.065625f), 2048
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Enemies/CrimsonLancer_Sheet.png",
                "Lancer",
                4, 2, 512, 205f, new Vector2(0.5f, 0.0665f)
            );
            ConfigureGridSheet(
                "Assets/Art/Characters/Enemies/CrimsonLancerExt_Sheet.png",
                "LancerExt",
                4, 2, 512, 205f, new Vector2(0.5f, 0.0665f)
            );
            // Mossmir Flying Bug 6x4 Sheet (1536x1024, center pivot)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Enemies/Mossmir_Sheet.png",
                new string[] { "MossmirIdle", "MossmirWindup", "MossmirLunge", "MossmirHurt" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 3, 4, 3 },
                256, 160f, new Vector2(0.5f, 0.5f), 2048
            );
            // Mossgrub Spiky Crawler 6x4 Sheet (1536x1024, bottom-center legs locked at 20px above cell bottom)
            ConfigureRowMappedSheet(
                "Assets/Art/Characters/Enemies/Mossgrub_Sheet.png",
                new string[] { "MossgrubPatrol", "MossgrubCharge", "MossgrubTurn", "MossgrubHurt" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 4, 2, 3 },
                256, 165f, new Vector2(0.5f, 0.078125f), 2048
            );

            // Backgrounds (Legacy + Upgraded 6-Layer Progressive DoF Biome Parallax Stack)
            string[] bgs = new string[]
            {
                "Assets/Art/Backgrounds/BG_MossGrotto_Reference.png",
                "Assets/Art/Backgrounds/BG_FarCanopy.png",
                "Assets/Art/Backgrounds/BG_MidRootsRuins.png",
                "Assets/Art/Backgrounds/BG_LightShafts.png",
                "Assets/Art/Backgrounds/FG_DarkFerns.png",
                "Assets/Art/Backgrounds/BG_L0_MossBackdrop.png",
                "Assets/Art/Backgrounds/BG_L0_StoneCavernBackdrop.png",
                "Assets/Art/Backgrounds/BG_L1_DeepMossCavern.png",
                "Assets/Art/Backgrounds/BG_L1_DeepStoneCavern.png",
                "Assets/Art/Backgrounds/BG_L2_MidCavernRoots.png",
                "Assets/Art/Backgrounds/BG_L2_MossLightShafts.png",
                "Assets/Art/Backgrounds/BG_L2_StoneLightShafts.png",
                "Assets/Art/Backgrounds/BG_L3_NearMossRuins.png",
                "Assets/Art/Backgrounds/BG_L3_NearStoneRuins.png",
                "Assets/Art/Backgrounds/FG_L5_SilhouetteFraming.png",
                "Assets/Art/Backgrounds/FG_CitadelFraming.png",
                "Assets/Art/Backgrounds/01_weavenest_atla.jpg",
                "Assets/Art/Backgrounds/02_shellwood.jpg",
                "Assets/Art/Backgrounds/03_sands_of_karak.jpg",
                "Assets/Art/Backgrounds/04_bellhart.jpg",
                "Assets/Art/Backgrounds/05_mount_fay.jpg",
                "Assets/Art/Backgrounds/06_grand_gate_courtyard.jpg",
                "Assets/Art/Backgrounds/07_aula_choral_chambers.jpg"
            };
            for (int i = 0; i < bgs.Length; i++) ConfigureSingleSprite(bgs[i], 100f, new Vector2(0.5f, 0.5f));

            // Progression Stage One-Way Platforms & Solid Cliffs (Calibrated HD PPU & Center Pivot)
            string[] stagePlats = new string[]
            {
                "Assets/Art/Environments/Stages/Plat_WeavenestCarved.png",
                "Assets/Art/Environments/Stages/Plat_ShellwoodBridge.png",
                "Assets/Art/Environments/Stages/Plat_KarakCoral.png",
                "Assets/Art/Environments/Stages/Plat_BellhartRoof.png",
                "Assets/Art/Environments/Stages/Plat_MountFayIce.png",
                "Assets/Art/Environments/Stages/Plat_CourtyardStone.png",
                "Assets/Art/Environments/Stages/Plat_ChoralBalcony.png",
                "Assets/Art/Environments/Stages/Cliff_Weavenest.png",
                "Assets/Art/Environments/Stages/Cliff_Shellwood.png",
                "Assets/Art/Environments/Stages/Cliff_Karak.png",
                "Assets/Art/Environments/Stages/Cliff_Bellhart.png",
                "Assets/Art/Environments/Stages/Cliff_MountFay.png",
                "Assets/Art/Environments/Stages/Cliff_Courtyard.png",
                "Assets/Art/Environments/Stages/Cliff_Choral.png"
            };
            for (int i = 0; i < stagePlats.Length; i++) ConfigureSingleSprite(stagePlats[i], 155f, new Vector2(0.5f, 0.5f));

            // Progression Stage Transition Gates (Calibrated ~3.6u - 4.4u tall, Bottom-Center Pivot)
            string[] stageGates = new string[]
            {
                "Assets/Art/Environments/Stages/Gate_MossyCaveMouth.png",
                "Assets/Art/Environments/Stages/Gate_CaveLightExit.png",
                "Assets/Art/Environments/Stages/Gate_AncientStoneWall.png",
                "Assets/Art/Environments/Stages/Gate_SandWallBreach.png",
                "Assets/Art/Environments/Stages/Gate_BellhartIronGate.png",
                "Assets/Art/Environments/Stages/Gate_RoyalGrandGate.png"
            };
            for (int i = 0; i < stageGates.Length; i++) ConfigureSingleSprite(stageGates[i], 135f, new Vector2(0.5f, 0.0f));
            ConfigureSingleSprite("Assets/Art/Environments/Stages/Gate_ChoralGrandDoors.png", 195f, new Vector2(0.5f, 0.0f));

            // Floor-Standing Destructibles & Architectural Decors (Calibrated HD PPU & Bottom-Center Pivot)
            string[] stageBottomProps = new string[]
            {
                "Assets/Art/Environments/Stages/Prop_RottenLog.png",
                "Assets/Art/Environments/Stages/Prop_SilkSpool.png",
                "Assets/Art/Environments/Stages/Prop_SilkCocoon.png",
                "Assets/Art/Environments/Stages/Prop_WebBarricade.png",
                "Assets/Art/Environments/Stages/Prop_HollowShell.png",
                "Assets/Art/Environments/Stages/Prop_WoodPillar.png",
                "Assets/Art/Environments/Stages/Prop_DryCoral.png",
                "Assets/Art/Environments/Stages/Prop_RustedGear.png",
                "Assets/Art/Environments/Stages/Prop_BonePile.png",
                "Assets/Art/Environments/Stages/Prop_CrackedBell.png",
                "Assets/Art/Environments/Stages/Prop_TownCrate.png",
                "Assets/Art/Environments/Stages/Prop_OldLantern.png",
                "Assets/Art/Environments/Stages/Prop_IceCrystal.png",
                "Assets/Art/Environments/Stages/Prop_FrozenPost.png",
                "Assets/Art/Environments/Stages/Prop_CrackedStatue.png",
                "Assets/Art/Environments/Stages/Prop_GildedUrn.png",
                "Assets/Art/Environments/Stages/Prop_RustedChain.png",
                "Assets/Art/Environments/Stages/Prop_ChoirBench.png",
                "Assets/Art/Environments/Stages/Prop_BrassCandelabra.png",
                "Assets/Art/Environments/Stages/Prop_CeremonialUrn.png"
            };
            for (int i = 0; i < stageBottomProps.Length; i++) ConfigureSingleSprite(stageBottomProps[i], 165f, new Vector2(0.5f, 0.0f));

            string[] stageBottomDecor = new string[]
            {
                "Assets/Art/Environments/Stages/Decor_WeavenestLoom.png",
                "Assets/Art/Environments/Stages/Decor_WeavenestTotem.png",
                "Assets/Art/Environments/Stages/Decor_WeavenestArch.png",
                "Assets/Art/Environments/Stages/Decor_ShellwoodBiolumFern.png",
                "Assets/Art/Environments/Stages/Decor_ShellwoodGlowShrooms.png",
                "Assets/Art/Environments/Stages/Decor_KarakGiantCoral.png",
                "Assets/Art/Environments/Stages/Decor_KarakSandArch.png",
                "Assets/Art/Environments/Stages/Decor_BellhartHouseA.png",
                "Assets/Art/Environments/Stages/Decor_BellhartHouseB.png",
                "Assets/Art/Environments/Stages/Decor_MountFayBellArch.png",
                "Assets/Art/Environments/Stages/Decor_MountFayRoots.png",
                "Assets/Art/Environments/Stages/Decor_ChoralOrganLoft.png",
                "Assets/Art/Environments/Stages/Decor_ChoralOrganConsole.png"
            };
            for (int i = 0; i < stageBottomDecor.Length; i++) ConfigureSingleSprite(stageBottomDecor[i], 125f, new Vector2(0.5f, 0.02f));

            ConfigureSingleSprite("Assets/Art/Environments/Stages/Prop_CrimsonBanner.png", 145f, new Vector2(0.5f, 0.92f));
            ConfigureSingleSprite("Assets/Art/Environments/Stages/Decor_BellhartHangingBell.png", 125f, new Vector2(0.5f, 0.96f));
            ConfigureSingleSprite("Assets/Art/Environments/Stages/Decor_MountFayIcicles.png", 125f, new Vector2(0.5f, 0.97f));
            ConfigureSingleSprite("Assets/Art/Environments/Stages/Decor_CourtyardBellBracket.png", 125f, new Vector2(0.5f, 0.96f));
            ConfigureSingleSprite("Assets/Art/Environments/Stages/Env_CitadelSupportArch.png", 135f, new Vector2(0.5f, 0.95f));

            // Environments (Upgraded Painterly Platforms, Bridges, Supports, Water Pools, & Cavern Props)
            ConfigureSingleSprite("Assets/Art/Environments/Env_MossGroundTile.png", 140f, new Vector2(0.5f, 0.5f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_CarvedStonePlatform.png", 140f, new Vector2(0.5f, 0.5f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_StoneBridgeWalkway.png", 140f, new Vector2(0.5f, 0.96f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_RootSupportArch.png", 135f, new Vector2(0.5f, 0.95f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_HangingMossTendrils.png", 140f, new Vector2(0.5f, 0.98f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_WaterPoolLilyPads.png", 135f, new Vector2(0.5f, 0.12f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_WaterShimmerGlint.png", 135f, new Vector2(0.5f, 0.5f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_GiantMushrooms.png", 130f, new Vector2(0.5f, 0.04f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_CurledFerns.png", 145f, new Vector2(0.5f, 0.04f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_PaleSpikyGrass.png", 150f, new Vector2(0.5f, 0.05f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_BronzeCageRuin.png", 140f, new Vector2(0.5f, 0.04f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_CarvedStoneArch.png", 115f, new Vector2(0.5f, 0.03f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_StalactiteVines.png", 125f, new Vector2(0.5f, 0.97f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_WoodBridge.png", 140f, new Vector2(0.5f, 0.5f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_StonePillar.png", 140f, new Vector2(0.5f, 0.025f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_CheckpointShrine.png", 155f, new Vector2(0.5f, 0.025f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_AbilityPedestal.png", 160f, new Vector2(0.5f, 0.025f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_UpgradeAltar.png", 160f, new Vector2(0.5f, 0.025f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_BreakableVines.png", 140f, new Vector2(0.5f, 0.5f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_BreakableTallGrass.png", 210f, new Vector2(0.5f, 0.03f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_BreakableBarrel.png", 250f, new Vector2(0.5f, 0.03f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_BreakableUrn.png", 260f, new Vector2(0.5f, 0.03f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_BreakableBriarBramble.png", 240f, new Vector2(0.5f, 0.03f));
            ConfigureSingleSprite("Assets/Art/Environments/Env_SpikeThorns.png", 150f, new Vector2(0.5f, 0.1f));

            // VFX, Pooled Particles & UI
            string[] vfxAndUi = new string[]
            {
                "Assets/Art/Effects/VFX_SlashArc.png",
                "Assets/Art/Effects/VFX_ImpactStarburst.png",
                "Assets/Art/Effects/VFX_WindStreak.png",
                "Assets/Art/Effects/VFX_CaveDoorwayGlow.png",
                "Assets/Art/Effects/VFX_HealRibbon.png",
                "Assets/Art/Effects/VFX_GlowingSpore.png",
                "Assets/Art/Effects/VFX_WaterDrip.png",
                "Assets/Art/Effects/VFX_SplashRing.png",
                "Assets/Art/Effects/VFX_MossLeaf.png",
                "Assets/Art/Effects/VFX_OrganicDroplet.png",
                "Assets/Art/Effects/VFX_ShellShard.png",
                "Assets/Art/Effects/VFX_DustPuff.png",
                "Assets/Art/Effects/VFX_MossSpeck.png",
                "Assets/Art/Effects/VFX_SilkSpear.png",
                "Assets/Art/Effects/VFX_ThreadGuard.png",
                "Assets/Art/Effects/VFX_HitSpark.png",
                "Assets/Art/Effects/VFX_SoftParticle.png",
                "Assets/Art/Effects/VFX_SilkOrb.png",
                "Assets/Art/Effects/VFX_WeatherGlowMote.png",
                "Assets/Art/Effects/VFX_WeatherStreak.png",
                "Assets/Art/Effects/VFX_StormMistCloud.png",
                "Assets/Art/Effects/VFX_GroundFogBank.png",
                "Assets/Art/Effects/VFX_BioCritterMoth.png",
                "Assets/Art/UI/UI_MaskHealthFull.png",
                "Assets/Art/UI/UI_MaskHealthEmpty.png",
                "Assets/Art/UI/UI_HealthCrestFrame.png",
                "Assets/Art/UI/UI_HealthCrestGlow.png",
                "Assets/Art/UI/UI_SilkSpoolBase.png",
                "Assets/Art/UI/UI_SilkThreadFill.png",
                "Assets/Art/UI/UI_Item_EssenceVessel.png",
                "Assets/Art/UI/UI_Item_EssenceShard.png",
                "Assets/Art/UI/UI_Item_MossberryDew.png",
                "Assets/Art/UI/UI_Item_WovenSpool.png",
                "Assets/Art/UI/UI_Item_WeaverRelic.png",
                "Assets/Art/UI/UI_PanelFrame.png",
                "Assets/Art/UI/UI_SkillIcon_Dash.png",
                "Assets/Art/UI/UI_SkillIcon_DoubleJump.png",
                "Assets/Art/UI/UI_SkillIcon_AirDash.png",
                "Assets/Art/UI/UI_SkillIcon_SilkSpear.png",
                "Assets/Art/UI/UI_SkillIcon_EnhancedAttack.png",
                "Assets/Art/UI/UI_SkillIcon_ThreadGuard.png"
            };
            for (int i = 0; i < vfxAndUi.Length; i++) ConfigureSingleSprite(vfxAndUi[i], 100f, new Vector2(0.5f, 0.5f));

            // 2x8 Hand-Drawn Silksong Dash Flash Burst & Ground/Hero Landing VFX Sheet (2048x512, 256x256/cell, Bilinear!)
            ConfigurePlayerVfxSheet("Assets/Art/Effects/player_vfx_sheet.png", 256, 115f, 2048);

            // Shellwood 7 New Enemies + Bell Projectile Sprites (all right-facing +X standardized)
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/aknid.png", 185f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/winged_pilgrim.png", 175f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/BellProjectile.png", 150f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/pilgrim_groveller.png", 180f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/pilgrim_bellbearer.png", 180f, new Vector2(0.48f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/pilgrim_hulk.png", 145f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/covetous_pilgrim.png", 155f, new Vector2(0.42f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/pondcatcher.png", 185f, new Vector2(0.48f, 0.02f));

            // Sands of Karak 4 New Enemies + Stinger Dart Projectile Sprites (all right-facing +X standardized)
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/driznit.png", 185f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/conchfly.png", 165f, new Vector2(0.50f, 0.45f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/KarakDartProjectile.png", 155f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/pharlid.png", 175f, new Vector2(0.48f, 0.03f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/coral_furm.png", 165f, new Vector2(0.50f, 0.02f));

            // Mount Fay 4 New Enemies + White Energy Wave & Ice Crystal Projectile Sprites
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/servitor_boran.png", 148f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/mnemonord.png", 155f, new Vector2(0.50f, 0.48f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/mnemonid.png", 185f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/driftlin.png", 175f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/FayLightWaveProjectile.png", 115f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/FayIceCrystalProjectile.png", 130f, new Vector2(0.50f, 0.50f));

            // Choral Chambers (Map 6 Courtyard & Map 7 Aula) 6 New Enemies + Golden Sound Wave
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/grand_reed.png", 135f, new Vector2(0.58f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/reed.png", 152f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/choristor.png", 158f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/choir_hornhead.png", 155f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/choir_elder.png", 140f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/choir_flyer.png", 165f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/ChoralSoundWaveProjectile.png", 125f, new Vector2(0.50f, 0.50f));

            // Courtyard 2 New Drape Enemies (Map 6 Grand Gate & Courtyard)
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/drapemite.png", 175f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/drapefly.png", 170f, new Vector2(0.50f, 0.50f));

            // Bellhart 2 New Bell-Armored Enemies (Map 4 Bellhart)
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/furm.png", 165f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/winged_furm.png", 165f, new Vector2(0.50f, 0.48f));

            // Bellhart Extended Cavern & Town Enemies (Map 4 Bellhart)
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/skullwing.png", 175f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/pharlid_diver.png", 165f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/mawling.png", 175f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/marrowmaw.png", 135f, new Vector2(0.50f, 0.02f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/mossmir.png", 170f, new Vector2(0.50f, 0.50f));
            ConfigureSingleSprite("Assets/Art/Characters/Enemies/mossgrub.png", 165f, new Vector2(0.50f, 0.02f));

            // Multi-Frame 8x4 (32-frame) Sprite Sheets for all 31 uploaded biome enemies (Idle/Move/Attack/Defeated)
            ConfigureAllBiomeEnemySheets();
        }

        private struct BiomeSheetSpec
        {
            public readonly string Stem;
            public readonly int CellSize;
            public readonly float Ppu;
            public readonly Vector2 Pivot;

            public BiomeSheetSpec(string stem, int cellSize, float ppu, Vector2 pivot)
            {
                Stem = stem;
                CellSize = cellSize;
                Ppu = ppu;
                Pivot = pivot;
            }
        }

        private static readonly BiomeSheetSpec[] AllBiomeEnemySheets = new BiomeSheetSpec[]
        {
            new BiomeSheetSpec("aknid", 340, 151.85f, new Vector2(0.4485f, 0.5000f)),
            new BiomeSheetSpec("winged_pilgrim", 340, 140.54f, new Vector2(0.4515f, 0.5000f)),
            new BiomeSheetSpec("pilgrim_groveller", 340, 168.80f, new Vector2(0.4206f, 0.0882f)),
            new BiomeSheetSpec("pilgrim_bellbearer", 340, 142.41f, new Vector2(0.4075f, 0.0882f)),
            new BiomeSheetSpec("pilgrim_hulk", 512, 139.06f, new Vector2(0.4209f, 0.0879f)),
            new BiomeSheetSpec("covetous_pilgrim", 512, 127.89f, new Vector2(0.3809f, 0.0879f)),
            new BiomeSheetSpec("pondcatcher", 512, 139.10f, new Vector2(0.4093f, 0.0879f)),
            new BiomeSheetSpec("driznit", 340, 133.01f, new Vector2(0.4485f, 0.5000f)),
            new BiomeSheetSpec("conchfly", 512, 155.57f, new Vector2(0.4502f, 0.4500f)),
            new BiomeSheetSpec("pharlid", 340, 126.75f, new Vector2(0.4075f, 0.0882f)),
            new BiomeSheetSpec("coral_furm", 340, 120.84f, new Vector2(0.4191f, 0.0882f)),
            new BiomeSheetSpec("furm", 340, 119.94f, new Vector2(0.4191f, 0.0882f)),
            new BiomeSheetSpec("winged_furm", 340, 124.92f, new Vector2(0.4515f, 0.4800f)),
            new BiomeSheetSpec("skullwing", 340, 132.60f, new Vector2(0.4485f, 0.5000f)),
            new BiomeSheetSpec("pharlid_diver", 340, 156.27f, new Vector2(0.4485f, 0.5000f)),
            new BiomeSheetSpec("mawling", 256, 171.33f, new Vector2(0.4219f, 0.0938f)),
            new BiomeSheetSpec("marrowmaw", 512, 135.00f, new Vector2(0.4209f, 0.0879f)),
            new BiomeSheetSpec("bellhart_mossmir", 256, 143.93f, new Vector2(0.4492f, 0.5000f)),
            new BiomeSheetSpec("bellhart_mossgrub", 256, 145.36f, new Vector2(0.4219f, 0.0938f)),
            new BiomeSheetSpec("servitor_boran", 512, 113.08f, new Vector2(0.4199f, 0.0879f)),
            new BiomeSheetSpec("mnemonord", 512, 97.33f, new Vector2(0.4502f, 0.4800f)),
            new BiomeSheetSpec("mnemonid", 340, 145.20f, new Vector2(0.4485f, 0.5000f)),
            new BiomeSheetSpec("driftlin", 512, 172.67f, new Vector2(0.4502f, 0.5000f)),
            new BiomeSheetSpec("drapemite", 340, 134.14f, new Vector2(0.4191f, 0.0882f)),
            new BiomeSheetSpec("drapefly", 340, 146.89f, new Vector2(0.4485f, 0.5000f)),
            new BiomeSheetSpec("grand_reed", 512, 97.93f, new Vector2(0.4649f, 0.0879f)),
            new BiomeSheetSpec("reed", 512, 145.61f, new Vector2(0.4209f, 0.0879f)),
            new BiomeSheetSpec("choristor", 340, 117.69f, new Vector2(0.4206f, 0.0882f)),
            new BiomeSheetSpec("choir_hornhead", 340, 142.36f, new Vector2(0.4191f, 0.0882f)),
            new BiomeSheetSpec("choir_elder", 512, 140.00f, new Vector2(0.4199f, 0.0879f)),
            new BiomeSheetSpec("choir_flyer", 340, 115.27f, new Vector2(0.4485f, 0.5000f))
        };

        private static void ConfigureAllBiomeEnemySheets()
        {
            for (int i = 0; i < AllBiomeEnemySheets.Length; i++)
            {
                BiomeSheetSpec s = AllBiomeEnemySheets[i];
                string sheetPath = "Assets/Art/Characters/Enemies/Sheets/" + s.Stem + "_Sheet.png";
                int texLimit = s.CellSize <= 340 ? 4096 : 8192;
                ConfigureRowMappedSheet(
                    sheetPath,
                    new string[] { s.Stem + "_Idle", s.Stem + "_Move", s.Stem + "_Atk", s.Stem + "_Defeat" },
                    new int[] { 0, 0, 0, 0 },
                    new int[] { 12, 12, 12, 12 },
                    s.CellSize, s.Ppu, s.Pivot, texLimit
                );
            }

            ConfigureMountFayHandDrawnSheets();
        }

        private static void ConfigureMountFayHandDrawnSheets()
        {
            // Dedicated Silksong-style 2D hand-drawn sprite sheets (FilterMode.Bilinear via ApplyMultiSpriteMeta)
            // 1. Servitor Boran: 4x6 grid @ 256x256 (1536x1024), facing RIGHT, feet-anchored pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.ServitorBoran.SpriteSheetPath,
                new string[] { "servitor_boran_Scan", "servitor_boran_Charge", "servitor_boran_Fire", "servitor_boran_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                256, 84.0f, new Vector2(0.4453f, 0.0898f), 2048
            );

            // 2. Driftlin: 3x6 grid @ 192x192 (1152x576), facing RIGHT, body-centered pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Driftlin.SpriteSheetPath,
                new string[] { "driftlin_Float", "driftlin_Pogo", "driftlin_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 94.0f, new Vector2(0.5000f, 0.4800f), 2048
            );

            // 3. Mnemonid: 4x6 grid @ 192x192 (1152x768), facing RIGHT, crystal-centered pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Mnemonid.SpriteSheetPath,
                new string[] { "mnemonid_Dormant", "mnemonid_Trigger", "mnemonid_Spike", "mnemonid_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 92.0f, new Vector2(0.5026f, 0.5026f), 2048
            );

            // 4. Mnemonord: 4x6 grid @ 320x320 (1920x1280), facing RIGHT, centered pivot with large spike headroom
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Mnemonord.SpriteSheetPath,
                new string[] { "mnemonord_Dormant", "mnemonord_Trigger", "mnemonord_Spike", "mnemonord_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                320, 84.0f, new Vector2(0.5000f, 0.5000f), 2048
            );

            // 5. Drapemite: 4x6 grid @ 192x192 (1152x768), facing RIGHT, consistent ground baseline pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Drapemite.SpriteSheetPath,
                new string[] { "drapemite_Patrol", "drapemite_Leap", "drapemite_Land", "drapemite_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 94.0f, new Vector2(0.4600f, 0.1146f), 2048
            );

            // 6. Drapefly: 3x6 grid @ 192x192 (1152x576), facing RIGHT, centered airborne pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Drapefly.SpriteSheetPath,
                new string[] { "drapefly_Hover", "drapefly_Sweep", "drapefly_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 90.0f, new Vector2(0.5000f, 0.5000f), 2048
            );

            // 7. Pondcatcher: 4x6 grid @ 256x256 (1536x1024), facing RIGHT, consistent ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Pondcatcher.SpriteSheetPath,
                new string[] { "pondcatcher_Hunt", "pondcatcher_Thrust", "pondcatcher_DownPoke", "pondcatcher_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                256, 76.0f, new Vector2(0.4727f, 0.1289f), 2048
            );

            // 8. Furm: 4x6 grid @ 192x192 (1152x768), facing RIGHT, consistent ground surface baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Furm.SpriteSheetPath,
                new string[] { "furm_Burrow", "furm_PopJump", "furm_VulnReburrow", "furm_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 92.0f, new Vector2(0.5000f, 0.1302f), 2048
            );

            // 9. Winged Furm: 3x6 grid @ 192x192 (1152x576), facing RIGHT, centered airborne pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.WingedFurm.SpriteSheetPath,
                new string[] { "winged_furm_Patrol", "winged_furm_Dizzy", "winged_furm_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 96.0f, new Vector2(0.5000f, 0.4800f), 2048
            );

            // 10. Pilgrim Groveller: 4x6 grid @ 192x192 (1152x768), facing RIGHT, ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.PilgrimGroveller.SpriteSheetPath,
                new string[] { "pilgrim_groveller_Walk", "pilgrim_groveller_Hop", "pilgrim_groveller_Land", "pilgrim_groveller_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 94.0f, new Vector2(0.4900f, 0.1042f), 2048
            );

            // 11. Winged Pilgrim: 3x6 grid @ 192x192 (1152x576), facing RIGHT, centered airborne pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.WingedPilgrim.SpriteSheetPath,
                new string[] { "winged_pilgrim_Patrol", "winged_pilgrim_Dive", "winged_pilgrim_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 94.0f, new Vector2(0.4900f, 0.4900f), 2048
            );

            // 12. Pilgrim Bellbearer: 4x6 grid @ 256x256 (1536x1024), facing RIGHT, ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.PilgrimBellbearer.SpriteSheetPath,
                new string[] { "pilgrim_bellbearer_Walk", "pilgrim_bellbearer_Throw", "pilgrim_bellbearer_Reload", "pilgrim_bellbearer_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                256, 98.0f, new Vector2(0.4800f, 0.1250f), 2048
            );

            // 13. Covetous Pilgrim: 5x6 grid @ 256x256 (1536x1280), facing RIGHT, ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.CovetousPilgrim.SpriteSheetPath,
                new string[] { "covetous_pilgrim_March", "covetous_pilgrim_Swing", "covetous_pilgrim_Slam", "covetous_pilgrim_Guard", "covetous_pilgrim_HDDefeat" },
                new int[] { 0, 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6, 6 },
                256, 84.0f, new Vector2(0.4434f, 0.1055f), 2048
            );

            // 14. Pilgrim Hulk: 4x6 grid @ 320x320 (1920x1280), facing RIGHT, ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.PilgrimHulk.SpriteSheetPath,
                new string[] { "pilgrim_hulk_Walk", "pilgrim_hulk_Roll", "pilgrim_hulk_Bonk", "pilgrim_hulk_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                320, 98.0f, new Vector2(0.5000f, 0.1094f), 2048
            );

            // 15. Driznit: 4x6 grid @ 192x192 (1152x768), facing RIGHT, centered airborne pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Driznit.SpriteSheetPath,
                new string[] { "driznit_Hover", "driznit_Throw", "driznit_Retreat", "driznit_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 90.0f, new Vector2(0.4900f, 0.4900f), 2048
            );

            // 16. Conchfly: 3x6 grid @ 192x192 (1152x576), facing RIGHT, centered airborne pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Conchfly.SpriteSheetPath,
                new string[] { "conchfly_Patrol", "conchfly_Dart", "conchfly_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 92.0f, new Vector2(0.5000f, 0.5000f), 2048
            );

            // 17. Pharlid: 4x6 grid @ 192x192 (1152x768), facing RIGHT, ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Pharlid.SpriteSheetPath,
                new string[] { "pharlid_Skitter", "pharlid_Lunge", "pharlid_Ambush", "pharlid_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 88.0f, new Vector2(0.4609f, 0.1094f), 2048
            );

            // 18. Coral Furm: 4x6 grid @ 192x192 (1152x768), facing RIGHT, ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.CoralFurm.SpriteSheetPath,
                new string[] { "coral_furm_Crawl", "coral_furm_Charge", "coral_furm_Brace", "coral_furm_HDDefeat" },
                new int[] { 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6 },
                192, 94.0f, new Vector2(0.4922f, 0.1146f), 2048
            );

            // 19. Moss Grub: 3x6 grid @ 192x192 (1152x576), facing RIGHT, consistent ground baseline Y pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.MossGrub.SpriteSheetPath,
                new string[] { "moss_grub_Patrol", "moss_grub_Provoked", "moss_grub_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 114.0f, new Vector2(0.5000f, 0.1250f), 2048
            );

            // 20. Mossmir: 3x6 grid @ 192x192 (1152x576), facing RIGHT, centered airborne pogo target pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Mossmir.SpriteSheetPath,
                new string[] { "mossmir_Hover", "mossmir_Action", "mossmir_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 118.0f, new Vector2(0.5000f, 0.4900f), 2048
            );

            // 21. Aknid: 3x6 grid @ 192x192 (1152x576), facing RIGHT, centered airborne carrier pivot
            ConfigureRowMappedSheet(
                ENEMY_COMBAT_CONFIG.Aknid.SpriteSheetPath,
                new string[] { "aknid_Carry", "aknid_Action", "aknid_HDDefeat" },
                new int[] { 0, 0, 0 },
                new int[] { 6, 6, 6 },
                192, 106.0f, new Vector2(0.5000f, 0.4800f), 2048
            );

            // 22. Shared 2D Hand-Drawn Enemy VFX Sheet: 5x6 grid @ 192x192 (1152x960), FilterMode.Bilinear, centered pivot
            ConfigureRowMappedSheet(
                ENEMY_VFX_CONFIG.SheetPath,
                new string[]
                {
                    ENEMY_VFX_CONFIG.PrefixLaser,
                    ENEMY_VFX_CONFIG.PrefixIceSpikes,
                    ENEMY_VFX_CONFIG.PrefixDriznitProj,
                    ENEMY_VFX_CONFIG.PrefixBellProj,
                    ENEMY_VFX_CONFIG.PrefixHelmetDizzy
                },
                new int[] { 0, 0, 0, 0, 0 },
                new int[] { 6, 6, 6, 6, 6 },
                ENEMY_VFX_CONFIG.CellSize,
                ENEMY_VFX_CONFIG.PixelsPerUnit,
                new Vector2(0.5000f, 0.5000f),
                2048
            );
        }

        private static void ConfigureSingleSprite(string assetPath, float ppu, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        #pragma warning disable 0618
        private static void ConfigureGridSheet(string assetPath, string prefix, int cols, int rows, int cellSize, float ppu, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 4096;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            int index = 0;
            // Top row first (y = (rows - 1 - r) * cellSize because Unity texture origin is bottom-left)
            for (int r = 0; r < rows; r++)
            {
                int unityY = (rows - 1 - r) * cellSize;
                for (int c = 0; c < cols; c++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.name = prefix + "_" + index;
                    meta.rect = new Rect(c * cellSize, unityY, cellSize, cellSize);
                    meta.alignment = (int)SpriteAlignment.Custom;
                    meta.pivot = pivot;
                    metas.Add(meta);
                    index++;
                }
            }

            ApplyMultiSpriteMeta(assetPath, ppu, 2048, metas);
        }

        private static void ConfigureRowMappedSheet(
            string assetPath,
            string[] rowPrefixes,
            int[] rowOffsets,
            int[] rowCounts,
            int cellSize,
            float ppu,
            Vector2 pivot,
            int maxTextureSize)
        {
            int totalRows = rowPrefixes.Length;
            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            for (int r = 0; r < totalRows; r++)
            {
                int unityY = (totalRows - 1 - r) * cellSize;
                for (int c = 0; c < rowCounts[r]; c++)
                {
                    SpriteMetaData meta = new SpriteMetaData();
                    meta.name = rowPrefixes[r] + "_" + (rowOffsets[r] + c);
                    meta.rect = new Rect(c * cellSize, unityY, cellSize, cellSize);
                    meta.alignment = (int)SpriteAlignment.Custom;
                    meta.pivot = pivot;
                    metas.Add(meta);
                }
            }

            ApplyMultiSpriteMeta(assetPath, ppu, maxTextureSize, metas);
        }

        private static void ConfigurePlayerVfxSheet(string assetPath, int cellSize, float ppu, int maxTextureSize)
        {
            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            // Row 0 (top in image -> y = cellSize in Unity bottom-left coords): Dash Flash Burst (8 frames, center pivot)
            for (int c = 0; c < 8; c++)
            {
                SpriteMetaData m = new SpriteMetaData();
                m.name = "VFX_DashBurst_" + c;
                m.rect = new Rect(c * cellSize, cellSize, cellSize, cellSize);
                m.alignment = (int)SpriteAlignment.Custom;
                m.pivot = new Vector2(0.5f, 0.5f);
                metas.Add(m);
            }
            // Row 1 (bottom in image -> y = 0 in Unity bottom-left coords): Ground Dust (0-3) & Hero Landing Wave (4-7), ground-anchored pivot
            for (int c = 0; c < 8; c++)
            {
                SpriteMetaData m = new SpriteMetaData();
                m.name = "VFX_GroundImpact_" + c;
                m.rect = new Rect(c * cellSize, 0, cellSize, cellSize);
                m.alignment = (int)SpriteAlignment.Custom;
                m.pivot = new Vector2(0.5f, 0.125f);
                metas.Add(m);
            }
            ApplyMultiSpriteMeta(assetPath, ppu, maxTextureSize, metas);
        }

        private static Sprite[] SliceSprites(Sprite[] src, int start, int count)
        {
            Sprite[] slice = new Sprite[count];
            System.Array.Copy(src, start, slice, 0, count);
            return slice;
        }

        private static void ApplyMultiSpriteMeta(string assetPath, float ppu, int maxTextureSize, List<SpriteMetaData> metas)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxTextureSize;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();
        }
        #pragma warning restore 0618

        private static void CreateMaterials(BuiltAssets assets)
        {
            Shader addShader = Shader.Find("SilkenGrotto/VolumetricAdditive2D");
            if (addShader == null) addShader = Shader.Find("Particles/Additive");
            if (addShader == null) addShader = Shader.Find("Sprites/Default");

            // 1. Additive Bioluminescent Spore Material (#C8F560 / #E9FFAD radial glow)
            string sporeMatPath = "Assets/Materials/Mat_SporeParticle.mat";
            Material sporeMat = AssetDatabase.LoadAssetAtPath<Material>(sporeMatPath);
            if (sporeMat == null)
            {
                sporeMat = new Material(addShader);
                AssetDatabase.CreateAsset(sporeMat, sporeMatPath);
            }
            else
            {
                sporeMat.shader = addShader;
            }
            Texture2D sporeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Effects/VFX_GlowingSpore.png");
            if (sporeTex == null) sporeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Effects/VFX_SoftParticle.png");
            if (sporeTex != null) sporeMat.mainTexture = sporeTex;
            sporeMat.SetFloat("_PulseSpeed", 1.25f);
            sporeMat.SetFloat("_PulseAmp", 0.22f);
            EditorUtility.SetDirty(sporeMat);
            assets.SporeParticleMaterial = sporeMat;

            // 1b. Dynamic Weather Glow Mote & Wind Streak Particle Materials
            Shader weatherShader = Shader.Find("Particles/Standard Unlit");
            if (weatherShader == null) weatherShader = addShader;

            string wGlowPath = "Assets/Materials/Mat_WeatherGlowMote.mat";
            Material wGlowMat = AssetDatabase.LoadAssetAtPath<Material>(wGlowPath);
            if (wGlowMat == null)
            {
                wGlowMat = new Material(addShader);
                AssetDatabase.CreateAsset(wGlowMat, wGlowPath);
            }
            else
            {
                wGlowMat.shader = addShader;
            }
            Texture2D wGlowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Effects/VFX_WeatherGlowMote.png");
            if (wGlowTex != null) wGlowMat.mainTexture = wGlowTex;
            wGlowMat.SetFloat("_PulseSpeed", 0.9f);
            wGlowMat.SetFloat("_PulseAmp", 0.14f);
            EditorUtility.SetDirty(wGlowMat);
            assets.WeatherGlowMaterial = wGlowMat;

            string wStreakPath = "Assets/Materials/Mat_WeatherStreak.mat";
            Material wStreakMat = AssetDatabase.LoadAssetAtPath<Material>(wStreakPath);
            if (wStreakMat == null)
            {
                wStreakMat = new Material(addShader);
                AssetDatabase.CreateAsset(wStreakMat, wStreakPath);
            }
            else
            {
                wStreakMat.shader = addShader;
            }
            Texture2D wStreakTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Effects/VFX_WeatherStreak.png");
            if (wStreakTex != null) wStreakMat.mainTexture = wStreakTex;
            wStreakMat.SetFloat("_PulseSpeed", 0f);
            wStreakMat.SetFloat("_PulseAmp", 0f);
            EditorUtility.SetDirty(wStreakMat);
            assets.WeatherStreakMaterial = wStreakMat;

            // 2. Animated Volumetric God Ray Additive/Screen Material
            string rayMatPath = "Assets/Materials/Mat_VolumetricGodRay.mat";
            Material rayMat = AssetDatabase.LoadAssetAtPath<Material>(rayMatPath);
            if (rayMat == null)
            {
                rayMat = new Material(addShader);
                AssetDatabase.CreateAsset(rayMat, rayMatPath);
            }
            else
            {
                rayMat.shader = addShader;
            }
            rayMat.SetFloat("_PulseSpeed", 0.65f);
            rayMat.SetFloat("_PulseAmp", 0.18f);
            EditorUtility.SetDirty(rayMat);
            assets.VolumetricGodRayMaterial = rayMat;

            // 3. Crisp Static Additive Combat VFX Material (Slash Arcs, Starburst Flash, Wind Streaks, Splash Rings)
            string vfxMatPath = "Assets/Materials/Mat_VFXAdditive.mat";
            Material vfxMat = AssetDatabase.LoadAssetAtPath<Material>(vfxMatPath);
            if (vfxMat == null)
            {
                vfxMat = new Material(addShader);
                AssetDatabase.CreateAsset(vfxMat, vfxMatPath);
            }
            else
            {
                vfxMat.shader = addShader;
            }
            vfxMat.SetFloat("_PulseSpeed", 0f);
            vfxMat.SetFloat("_PulseAmp", 0f);
            EditorUtility.SetDirty(vfxMat);
            assets.AdditiveVFXMaterial = vfxMat;

            assets.DefaultSpriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            // 4. Crisp Pure-White Hit-Flash Sprite Material for Enemies
            Shader flashShader = Shader.Find("SilkenGrotto/SpriteHitFlash");
            if (flashShader == null) flashShader = Shader.Find("Sprites/Default");
            string flashMatPath = "Assets/Materials/Mat_EnemyHitFlash.mat";
            Material flashMat = AssetDatabase.LoadAssetAtPath<Material>(flashMatPath);
            if (flashMat == null)
            {
                flashMat = new Material(flashShader);
                AssetDatabase.CreateAsset(flashMat, flashMatPath);
            }
            else
            {
                flashMat.shader = flashShader;
            }
            flashMat.SetColor("_FlashColor", Color.white);
            flashMat.SetFloat("_FlashAmount", 0f);
            EditorUtility.SetDirty(flashMat);
            assets.EnemyHitFlashMaterial = flashMat;
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void CreateScriptableObjects(BuiltAssets assets)
        {
            // 1. Player Stats Config
            assets.PlayerStatsConfig = CreateOrLoadAsset<PlayerStatsConfig>("Assets/ScriptableObjects/Characters/PlayerStatsConfig.asset");
            assets.PlayerStatsConfig.baseMaxHealth = 5;
            assets.PlayerStatsConfig.baseMaxSilk = 100f;
            assets.PlayerStatsConfig.bindHealSilkCost = 35f;
            assets.PlayerStatsConfig.bindHealMaskAmount = 2;
            assets.PlayerStatsConfig.baseAttackDamage = 12;
            assets.PlayerStatsConfig.baseWalkSpeed = 6.2f;
            assets.PlayerStatsConfig.baseRunSpeed = 9.4f;
            assets.PlayerStatsConfig.baseJumpForce = 15.2f;
            EditorUtility.SetDirty(assets.PlayerStatsConfig);

            // 2. Attack Data ScriptableObjects
            AttackData c1 = CreateOrLoadAsset<AttackData>("Assets/ScriptableObjects/Characters/Attack_Combo1.asset");
            c1.attackName = "Needle Slash I";
            c1.comboStep = 1;
            c1.direction = AttackDirection.Forward;
            c1.baseDamage = 12;
            c1.animationDuration = 0.24f;
            c1.hitboxActiveStart = 0.16f;
            c1.hitboxActiveEnd = 0.62f;
            c1.cooldown = 0.06f;
            c1.hitboxOffset = new Vector2(1.30f, 1.05f);
            c1.hitboxSize = new Vector2(2.0f, 1.15f);
            c1.knockbackForce = 6.0f;
            c1.hitStopDuration = 0.036f;
            c1.slashAngleDegrees = 0f;
            c1.slashScale = 0.85f;
            c1.slashTint = Color.white;
            EditorUtility.SetDirty(c1);

            AttackData c2 = CreateOrLoadAsset<AttackData>("Assets/ScriptableObjects/Characters/Attack_Combo2.asset");
            c2.attackName = "Rising Thread Cut";
            c2.comboStep = 2;
            c2.direction = AttackDirection.Forward;
            c2.baseDamage = 15;
            c2.animationDuration = 0.25f;
            c2.hitboxActiveStart = 0.16f;
            c2.hitboxActiveEnd = 0.64f;
            c2.cooldown = 0.07f;
            c2.hitboxOffset = new Vector2(1.35f, 1.25f);
            c2.hitboxSize = new Vector2(2.1f, 1.35f);
            c2.knockbackForce = 7.0f;
            c2.hitStopDuration = 0.040f;
            c2.slashAngleDegrees = 22f;
            c2.slashScale = 0.92f;
            c2.slashTint = Color.white;
            EditorUtility.SetDirty(c2);

            AttackData c3 = CreateOrLoadAsset<AttackData>("Assets/ScriptableObjects/Characters/Attack_Combo3.asset");
            c3.attackName = "Piercing Greatneedle";
            c3.comboStep = 3;
            c3.direction = AttackDirection.Forward;
            c3.baseDamage = 20;
            c3.animationDuration = 0.30f;
            c3.hitboxActiveStart = 0.20f;
            c3.hitboxActiveEnd = 0.68f;
            c3.cooldown = 0.14f;
            c3.hitboxOffset = new Vector2(1.60f, 1.05f);
            c3.hitboxSize = new Vector2(2.55f, 1.10f);
            c3.knockbackForce = 9.5f;
            c3.staggerDuration = 0.30f;
            c3.hitStopDuration = 0.048f;
            c3.silkGainOnHit = 18f;
            c3.locksHorizontalMovement = true;
            c3.slashAngleDegrees = -5f;
            c3.slashScale = 1.05f;
            c3.slashTint = Color.white;
            EditorUtility.SetDirty(c3);

            AttackData airF = CreateOrLoadAsset<AttackData>("Assets/ScriptableObjects/Characters/Attack_AirForward.asset");
            airF.attackName = "Aerial Needle Slash";
            airF.comboStep = 1;
            airF.direction = AttackDirection.AirForward;
            airF.baseDamage = 13;
            airF.animationDuration = 0.24f;
            airF.hitboxOffset = new Vector2(1.35f, 1.05f);
            airF.hitboxSize = new Vector2(2.1f, 1.25f);
            airF.hitStopDuration = 0.036f;
            airF.slashAngleDegrees = -8f;
            airF.slashScale = 0.88f;
            airF.slashTint = Color.white;
            EditorUtility.SetDirty(airF);

            AttackData upA = CreateOrLoadAsset<AttackData>("Assets/ScriptableObjects/Characters/Attack_Up.asset");
            upA.attackName = "Upward Needle Arc";
            upA.comboStep = 2;
            upA.direction = AttackDirection.Up;
            upA.baseDamage = 13;
            upA.animationDuration = 0.24f;
            upA.hitboxOffset = new Vector2(0.25f, 2.25f);
            upA.hitboxSize = new Vector2(1.75f, 1.85f);
            upA.hitStopDuration = 0.036f;
            upA.slashAngleDegrees = 82f;
            upA.slashScale = 0.85f;
            upA.slashTint = Color.white;
            EditorUtility.SetDirty(upA);

            AttackData downA = CreateOrLoadAsset<AttackData>("Assets/ScriptableObjects/Characters/Attack_DownPogo.asset");
            downA.attackName = "Downward Pogo Strike";
            downA.comboStep = 3;
            downA.direction = AttackDirection.Down;
            downA.baseDamage = 14;
            downA.animationDuration = 0.25f;
            downA.hitboxOffset = new Vector2(0.15f, -0.35f);
            downA.hitboxSize = new Vector2(1.65f, 1.75f);
            downA.hitStopDuration = 0.038f;
            downA.slashAngleDegrees = -85f;
            downA.slashScale = 0.85f;
            downA.slashTint = Color.white;
            EditorUtility.SetDirty(downA);

            assets.ComboAttacks = new AttackData[] { c1, c2, c3 };
            assets.AirForwardAttack = airF;
            assets.UpAttack = upA;
            assets.DownAttack = downA;

            // 3. Enemy Stats ScriptableObjects
            assets.LancerStats = CreateOrLoadAsset<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_CrimsonLancer.asset");
            assets.LancerStats.enemyName = "Crimson Lancer";
            assets.LancerStats.isElite = false;
            assets.LancerStats.maxHealth = 45;
            assets.LancerStats.silkEssenceReward = 22;
            assets.LancerStats.orbDropCount = 3;
            assets.LancerStats.patrolSpeed = 2.4f;
            assets.LancerStats.chaseSpeed = 4.4f;
            assets.LancerStats.detectRange = 7.5f;
            assets.LancerStats.attackRange = 2.15f;
            assets.LancerStats.attackDamage = 1;
            assets.LancerStats.windupDuration = 0.42f;
            assets.LancerStats.attackActiveDuration = 0.24f;
            assets.LancerStats.recoveryDuration = 0.48f;
            assets.LancerStats.attackCooldown = 1.25f;
            EditorUtility.SetDirty(assets.LancerStats);

            assets.CaptainStats = CreateOrLoadAsset<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_CrimsonCaptain.asset");
            assets.CaptainStats.enemyName = "Crimson Canopy Captain";
            assets.CaptainStats.isElite = true;
            assets.CaptainStats.maxHealth = 135;
            assets.CaptainStats.silkEssenceReward = 95;
            assets.CaptainStats.orbDropCount = 7;
            assets.CaptainStats.patrolSpeed = 2.8f;
            assets.CaptainStats.chaseSpeed = 5.2f;
            assets.CaptainStats.detectRange = 10.5f;
            assets.CaptainStats.loseTargetRange = 16.0f;
            assets.CaptainStats.attackRange = 2.65f;
            assets.CaptainStats.attackDamage = 2;
            assets.CaptainStats.windupDuration = 0.36f;
            assets.CaptainStats.attackActiveDuration = 0.28f;
            assets.CaptainStats.recoveryDuration = 0.42f;
            assets.CaptainStats.attackCooldown = 1.05f;
            assets.CaptainStats.lungeSpeed = 9.2f;
            assets.CaptainStats.knockbackResistance = 0.45f;
            EditorUtility.SetDirty(assets.CaptainStats);

            assets.MossmirStats = CreateMossGrottoStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_Mossmir.asset", ENEMY_COMBAT_CONFIG.Mossmir);
            assets.MossgrubStats = CreateMossGrottoStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_Mossgrub.asset", ENEMY_COMBAT_CONFIG.MossGrub);

            // Shellwood 7 Dedicated Enemy Stats (Aknid sourced from ENEMY_COMBAT_CONFIG.Aknid!)
            assets.AknidStats = CreateMossGrottoStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_Aknid.asset", ENEMY_COMBAT_CONFIG.Aknid);

            assets.WingedPilgrimStats = CreatePilgrimStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_WingedPilgrim.asset",
                ENEMY_COMBAT_CONFIG.WingedPilgrim);

            assets.PilgrimGrovellerStats = CreatePilgrimStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_PilgrimGroveller.asset",
                ENEMY_COMBAT_CONFIG.PilgrimGroveller);

            assets.PilgrimHulkStats = CreatePilgrimStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_PilgrimHulk.asset",
                ENEMY_COMBAT_CONFIG.PilgrimHulk);

            assets.PilgrimBellbearerStats = CreatePilgrimStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_PilgrimBellbearer.asset",
                ENEMY_COMBAT_CONFIG.PilgrimBellbearer);

            assets.CovetousPilgrimStats = CreatePilgrimStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_CovetousPilgrim.asset",
                ENEMY_COMBAT_CONFIG.CovetousPilgrim);

            assets.PondcatcherStats = CreateBatch2StatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Pondcatcher.asset",
                ENEMY_COMBAT_CONFIG.Pondcatcher);

            // Sands of Karak 4 Dedicated Enemy Stats (sourced from centralized ENEMY_COMBAT_CONFIG)
            assets.DriznitStats = CreateKarakStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Driznit.asset",
                ENEMY_COMBAT_CONFIG.Driznit);

            assets.ConchflyStats = CreateKarakStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Conchfly.asset",
                ENEMY_COMBAT_CONFIG.Conchfly);

            assets.PharlidStats = CreateKarakStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Pharlid.asset",
                ENEMY_COMBAT_CONFIG.Pharlid);

            assets.CoralFurmStats = CreateKarakStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_CoralFurm.asset",
                ENEMY_COMBAT_CONFIG.CoralFurm);

            // Mount Fay 4 Dedicated Enemy Stats (sourced from centralized ENEMY_COMBAT_CONFIG)
            assets.ServitorBoranStats = CreateMountFayStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_ServitorBoran.asset",
                ENEMY_COMBAT_CONFIG.ServitorBoran);

            assets.MnemonordStats = CreateMountFayStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Mnemonord.asset",
                ENEMY_COMBAT_CONFIG.Mnemonord);

            assets.MnemonidStats = CreateMountFayStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Mnemonid.asset",
                ENEMY_COMBAT_CONFIG.Mnemonid);

            assets.DriftlinStats = CreateMountFayStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Driftlin.asset",
                ENEMY_COMBAT_CONFIG.Driftlin);

            // Choral Chambers 6 Dedicated Enemy Stats (sourced centrally from ENEMY_COMBAT_CONFIG!)
            assets.GrandReedStats = CreateChoralStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_GrandReed.asset", ENEMY_COMBAT_CONFIG.GrandReed);
            assets.ReedStats = CreateChoralStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_Reed.asset", ENEMY_COMBAT_CONFIG.Reed);
            assets.ChoristorStats = CreateChoralStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_Choristor.asset", ENEMY_COMBAT_CONFIG.Choristor);
            assets.ChoirHornheadStats = CreateChoralStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_ChoirHornhead.asset", ENEMY_COMBAT_CONFIG.ChoirHornhead);
            assets.ChoirElderStats = CreateChoralStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_ChoirElder.asset", ENEMY_COMBAT_CONFIG.ChoirElder);
            assets.ChoirFlyerStats = CreateChoralStatsAsset("Assets/ScriptableObjects/Enemies/Enemy_ChoirFlyer.asset", ENEMY_COMBAT_CONFIG.ChoirFlyer);

            // Courtyard 2 Dedicated Drape Enemy Stats (sourced from ENEMY_COMBAT_CONFIG)
            assets.DrapemiteStats = CreateBatch2StatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Drapemite.asset",
                ENEMY_COMBAT_CONFIG.Drapemite);

            assets.DrapeflyStats = CreateBatch2StatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Drapefly.asset",
                ENEMY_COMBAT_CONFIG.Drapefly);

            // Bellhart 2 Furm Enemy Stats (sourced from ENEMY_COMBAT_CONFIG)
            assets.FurmStats = CreateBatch2StatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Furm.asset",
                ENEMY_COMBAT_CONFIG.Furm);

            assets.WingedFurmStats = CreateBatch2StatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_WingedFurm.asset",
                ENEMY_COMBAT_CONFIG.WingedFurm);

            // Bellhart Extended Cavern & Town Enemies (Map 4 Bellhart)
            assets.SkullwingStats = CreateEnemyStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Skullwing.asset",
                "Skullwing", false, 28, 18, 2,
                3.4f, 4.8f, 5.0f, 0.20f,
                10.0f, 14.5f, 8.5f,
                1, 6.6f, 0.24f, 0.42f, 0.44f, 0.85f, 14.5f, 0.08f, 0.16f);

            assets.PharlidDiverStats = CreateKarakStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_PharlidDiver.asset",
                ENEMY_COMBAT_CONFIG.Pharlid);

            assets.MawlingStats = CreateEnemyStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Mawling.asset",
                "Mawling", false, 24, 15, 2,
                3.6f, 6.2f, 4.2f, 0.20f,
                8.5f, 12.0f, 5.4f,
                1, 6.4f, 0.16f, 0.44f, 0.34f, 0.65f, 12.6f, 0.08f, 0.15f);

            assets.MarrowmawStats = CreateEnemyStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Marrowmaw.asset",
                "Marrowmaw", false, 82, 42, 6,
                1.9f, 3.4f, 3.8f, 0.35f,
                9.0f, 13.5f, 6.2f,
                2, 10.5f, 0.48f, 0.56f, 0.62f, 1.15f, 13.4f, 0.78f, 0.10f);

            assets.BellhartMossmirStats = CreateMossGrottoStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Bellhart_Mossmir.asset",
                ENEMY_COMBAT_CONFIG.Mossmir);

            assets.BellhartMossgrubStats = CreateMossGrottoStatsAsset(
                "Assets/ScriptableObjects/Enemies/Enemy_Bellhart_Mossgrub.asset",
                ENEMY_COMBAT_CONFIG.MossGrub);

            // 4. Skills ScriptableObjects
            assets.AllSkills.Clear();
            assets.SkillByType.Clear();
            RegisterSkill(assets, "Assets/ScriptableObjects/Skills/Skill_Dash.asset", "skill_dash", "Silk Dash",
                "Dart rapidly forward with brief invulnerability.", SkillType.Dash, false, 0f, 0.60f, "RMB / K", 17.5f, 0.20f,
                new Color(0.92f, 0.36f, 0.44f, 1f), "Assets/Art/UI/UI_SkillIcon_Dash.png");

            RegisterSkill(assets, "Assets/ScriptableObjects/Skills/Skill_DoubleJump.asset", "skill_doublejump", "Gale Leap",
                "Weave silk beneath your feet to leap a second time mid-air.", SkillType.DoubleJump, false, 0f, 0.10f, "SPACE (Air)", 1f, 0.1f,
                new Color(0.46f, 0.90f, 0.78f, 1f), "Assets/Art/UI/UI_SkillIcon_DoubleJump.png");

            RegisterSkill(assets, "Assets/ScriptableObjects/Skills/Skill_AirDash.asset", "skill_airdash", "Sky Thread Dash",
                "Extend Silk Dash while airborne to cross vast mossy chasms.", SkillType.AirDash, false, 8f, 0.75f, "RMB (Air)", 18.5f, 0.21f,
                new Color(0.56f, 0.85f, 1f, 1f), "Assets/Art/UI/UI_SkillIcon_AirDash.png");

            RegisterSkill(assets, "Assets/ScriptableObjects/Skills/Skill_SilkSpear.asset", "skill_silkspear", "Silk Spear",
                "Hurl a piercing silk-bound needle across the cavern (Costs 25 Silk).", SkillType.SilkSpear, false, 25f, 1.40f, "Q / F", 1.85f, 0.25f,
                new Color(0.96f, 0.94f, 0.88f, 1f), "Assets/Art/UI/UI_SkillIcon_SilkSpear.png");

            RegisterSkill(assets, "Assets/ScriptableObjects/Skills/Skill_EnhancedAttack.asset", "skill_enhanced", "Needle Mastery",
                "Passive: Empower every melee strike with +35% damage and extended reach.", SkillType.EnhancedAttack, false, 0f, 0f, "PASSIVE", 1.35f, 0f,
                new Color(0.98f, 0.74f, 0.36f, 1f), "Assets/Art/UI/UI_SkillIcon_EnhancedAttack.png");

            RegisterSkill(assets, "Assets/ScriptableObjects/Skills/Skill_ThreadGuard.asset", "skill_threadguard", "Thread Guard",
                "Weave a defensive silk barrier that nullifies the next enemy strike (Costs 20 Silk).", SkillType.ThreadGuard, false, 20f, 3.0f, "R / V", 1f, 2.8f,
                new Color(0.58f, 0.95f, 0.88f, 1f), "Assets/Art/UI/UI_SkillIcon_ThreadGuard.png");

            // 5. Upgrades ScriptableObjects
            assets.AllUpgrades.Clear();
            RegisterUpgrade(assets, "Assets/ScriptableObjects/Upgrades/Upgrade_MaxHealth.asset", UpgradeType.MaxHealth,
                "Shell Vitality", "Adds +1 Maximum Health Mask.", 5, 35, 30, 1f, "Mask");
            RegisterUpgrade(assets, "Assets/ScriptableObjects/Upgrades/Upgrade_AttackDamage.asset", UpgradeType.AttackDamage,
                "Honed Greatneedle", "Increases base needle attack power.", 5, 30, 25, 4f, "ATK");
            RegisterUpgrade(assets, "Assets/ScriptableObjects/Upgrades/Upgrade_MovementSpeed.asset", UpgradeType.MovementSpeed,
                "Fleet Stride", "Boosts walking and running speed.", 4, 25, 25, 0.75f, "SPD");
            RegisterUpgrade(assets, "Assets/ScriptableObjects/Upgrades/Upgrade_JumpControl.asset", UpgradeType.JumpControl,
                "Acrobatic Grace", "Improves jump height and mid-air agility.", 4, 25, 25, 0.65f, "AGI");
            RegisterUpgrade(assets, "Assets/ScriptableObjects/Upgrades/Upgrade_MaxSilkEnergy.asset", UpgradeType.MaxSilkEnergy,
                "Expanded Spool", "Raises Maximum Silk Energy capacity.", 5, 30, 25, 20f, "SILK");
            RegisterUpgrade(assets, "Assets/ScriptableObjects/Upgrades/Upgrade_Defense.asset", UpgradeType.Defense,
                "Woven Cloak Armor", "Reduces incoming damage by +8% per tier.", 4, 35, 30, 0.08f, "DEF");

            // 6. Inventory Item Catalog ScriptableObjects
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Inventory"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Inventory");
            }
            assets.AllInventoryItems.Clear();
            assets.ItemById.Clear();
            RegisterInventoryItem(assets, "Assets/ScriptableObjects/Inventory/Item_MossberryDew.asset",
                "item_mossberry_dew", "Mossberry Tonic", InventoryItemCategory.Consumable,
                "Sweet luminous nectar pressed from cavern mossberries.",
                "Effect: Restores +1 Mask Health inside Inventory.",
                "Assets/Art/UI/UI_Item_MossberryDew.png", 9, true, 1, 0f, 0);
            RegisterInventoryItem(assets, "Assets/ScriptableObjects/Inventory/Item_EssenceShard.asset",
                "item_essence_shard", "Luminous Essence Shard", InventoryItemCategory.EssenceRelic,
                "Crystallized Silk Essence humming faintly with weaver light.",
                "Effect: Channel inside Inventory to gain +35 Silk Essence.",
                "Assets/Art/UI/UI_Item_EssenceShard.png", 99, true, 0, 0f, 35);
            RegisterInventoryItem(assets, "Assets/ScriptableObjects/Inventory/Item_WovenSpool.asset",
                "item_woven_spool", "Spun Silk Bobbin", InventoryItemCategory.Consumable,
                "Tightly wound spare threads spun by the ancient sanctuary.",
                "Effect: Restores +45 Silk Energy immediately on use.",
                "Assets/Art/UI/UI_Item_WovenSpool.png", 9, true, 0, 45f, 0);
            RegisterInventoryItem(assets, "Assets/ScriptableObjects/Inventory/Item_WeaverRelic.asset",
                "item_weaver_relic", "Weaver Signet Seal", InventoryItemCategory.KeyArtifact,
                "An etched crimson-bronze emblem proving passage across Thornbridge.",
                "Sanctuary Artifact — stored permanently in your Inventory Pouch.",
                "Assets/Art/UI/UI_Item_WeaverRelic.png", 1, false, 0, 0f, 0);

            // 7. Modular 8-Stage Progression Config ScriptableObjects (00 Moss Grotto .. 07 Aula Choral Chambers)
            SilkenGrottoStagesBuilder.CreateAllStageConfigAssets(assets);
        }

        private static void RegisterSkill(
            BuiltAssets assets, string path, string id, string name, string desc, SkillType type,
            bool defaultUnlocked, float silkCost, float cooldown, string hint, float power, float dur,
            Color accent, string iconPath)
        {
            SkillData s = CreateOrLoadAsset<SkillData>(path);
            s.skillId = id;
            s.displayName = name;
            s.description = desc;
            s.skillType = type;
            s.unlockedByDefault = defaultUnlocked;
            s.silkCost = silkCost;
            s.cooldown = cooldown;
            s.inputHint = hint;
            s.powerValue = power;
            s.duration = dur;
            s.accentColor = accent;
            s.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            EditorUtility.SetDirty(s);

            assets.AllSkills.Add(s);
            assets.SkillByType[type] = s;
        }

        private static void RegisterUpgrade(
            BuiltAssets assets, string path, UpgradeType type, string name, string desc,
            int maxLvl, int baseCost, int costStep, float bonusPerLvl, string unit)
        {
            UpgradeData u = CreateOrLoadAsset<UpgradeData>(path);
            u.upgradeType = type;
            u.displayName = name;
            u.description = desc;
            u.maxLevel = maxLvl;
            u.baseCost = baseCost;
            u.costPerLevel = costStep;
            u.bonusPerLevel = bonusPerLvl;
            u.unitLabel = unit;
            EditorUtility.SetDirty(u);
            assets.AllUpgrades.Add(u);
        }

        private static void RegisterInventoryItem(
            BuiltAssets assets, string path, string id, string name, InventoryItemCategory cat,
            string desc, string effectSummary, string iconPath, int maxStack,
            bool usable, int healMasks, float healSilk, int essenceYield)
        {
            InventoryItemData item = CreateOrLoadAsset<InventoryItemData>(path);
            item.itemId = id;
            item.displayName = name;
            item.category = cat;
            item.description = desc;
            item.effectSummary = effectSummary;
            item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            item.maxStack = Mathf.Max(1, maxStack);
            item.usableFromInventory = usable;
            item.restoreHealthMasks = Mathf.Max(0, healMasks);
            item.restoreSilkAmount = Mathf.Max(0f, healSilk);
            item.essenceYield = Mathf.Max(0, essenceYield);
            EditorUtility.SetDirty(item);

            assets.AllInventoryItems.Add(item);
            assets.ItemById[id] = item;
        }

        public static Sprite[] LoadSheetSprites(string sheetPath, string prefix, int count)
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            Sprite[] ordered = new Sprite[count];
            for (int i = 0; i < subAssets.Length; i++)
            {
                Sprite sp = subAssets[i] as Sprite;
                if (sp == null) continue;
                for (int k = 0; k < count; k++)
                {
                    if (sp.name == prefix + "_" + k)
                    {
                        ordered[k] = sp;
                        break;
                    }
                }
            }
            return ordered;
        }

        private static AnimationClip CreateSpriteClip(string path, Sprite[] frames, float frameRate, bool loop)
        {
            float[] times = new float[frames.Length];
            for (int i = 0; i < frames.Length; i++) times[i] = i / frameRate;
            return CreateTimedSpriteClip(path, frames, times, frameRate, loop);
        }

        private static AnimationClip CreateTimedSpriteClip(string path, Sprite[] frames, float[] times, float frameRate, bool loop)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
            clip.frameRate = frameRate;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorCurveBinding binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "Visual";
            binding.propertyName = "m_Sprite";

            int len = Mathf.Min(frames.Length, times.Length);
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[len];
            for (int i = 0; i < len; i++)
            {
                keys[i] = new ObjectReferenceKeyframe();
                keys[i].time = times[i];
                keys[i].value = frames[i];
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void CreateAnimationsAndControllers(BuiltAssets assets)
        {
            Sprite[] loco = LoadSheetSprites("Assets/Art/Characters/Player/Player_Locomotion_Sheet.png", "PlayerLoco", 8);
            Sprite[] locoExt = LoadSheetSprites("Assets/Art/Characters/Player/Player_LocomotionExt_Sheet.png", "PlayerLocoExt", 8);
            Sprite[] act = LoadSheetSprites("Assets/Art/Characters/Player/Player_Action_Sheet.png", "PlayerAct", 8);
            Sprite[] actExt = LoadSheetSprites("Assets/Art/Characters/Player/Player_ActionExt_Sheet.png", "PlayerActExt", 8);
            Sprite[] grace = LoadSheetSprites("Assets/Art/Characters/Player/Player_IdleGrace_Sheet.png", "PlayerGrace", 8);
            Sprite[] idleSmooth = LoadSheetSprites("Assets/Art/Characters/Player/Player_IdleSmooth_Sheet.png", "IdleSmooth", 24);
            string graceSmoothPath = "Assets/Art/Characters/Player/Player_IdleGraceSmooth_Sheet.png";
            Sprite[] grace1Smooth = LoadSheetSprites(graceSmoothPath, "Grace1", 24);
            Sprite[] grace2Smooth = LoadSheetSprites(graceSmoothPath, "Grace2", 24);
            Sprite[] grace3Smooth = LoadSheetSprites(graceSmoothPath, "Grace3", 24);
            Sprite[] bench = LoadSheetSprites("Assets/Art/Characters/Player/Player_BenchSit_Sheet.png", "PlayerBench", 4);
            Sprite[] enemy = LoadSheetSprites("Assets/Art/Characters/Enemies/CrimsonLancer_Sheet.png", "Lancer", 8);
            Sprite[] enemyExt = LoadSheetSprites("Assets/Art/Characters/Enemies/CrimsonLancerExt_Sheet.png", "LancerExt", 8);
            string core45Path = "Assets/Art/Characters/Player/Player_Core45_EngineSheet.png";
            Sprite[] coreIdle = LoadSheetSprites(core45Path, "CoreIdle", 8);
            string movSmoothPath = "Assets/Art/Characters/Player/Player_MovementSmooth_Sheet.png";
            Sprite[] smoothRun = LoadSheetSprites(movSmoothPath, "SmoothRun", 20);
            Sprite[] smoothJumpFall = LoadSheetSprites(movSmoothPath, "SmoothJumpFall", 24);
            Sprite[] smoothWall = LoadSheetSprites(movSmoothPath, "SmoothWall", 20);
            string combSmoothPath = "Assets/Art/Characters/Player/Player_CombatDashSmooth_Sheet.png";
            Sprite[] smoothDash = LoadSheetSprites(combSmoothPath, "SmoothDash", 14);
            Sprite[] smoothSlash = LoadSheetSprites(combSmoothPath, "SmoothSlash", 12);
            Sprite[] ledgeMantle = LoadSheetSprites("Assets/Art/Characters/Player/ledge_mantle.png", "LedgeMantle", 5);
            Sprite[] healBind = LoadSheetSprites("Assets/Art/Characters/Player/heal_bind.png", "HealBind", 4);

            // 6x8 Silksong-Style Master Movement Sheet (48 frames @ 256x256px, 12-24 FPS mapped to 60 FPS PlayerController)
            string swMovePath = "Assets/Art/Characters/Player/silken_weaver_movement_sheet.png";
            Sprite[] swRest = LoadSheetSprites(swMovePath, "SW_Rest", 8);
            Sprite[] swIdleLand = LoadSheetSprites(swMovePath, "SW_IdleLand", 8);
            Sprite[] swWalk = LoadSheetSprites(swMovePath, "SW_Walk", 8);
            Sprite[] swDashSprint = LoadSheetSprites(swMovePath, "SW_DashSprint", 8);
            Sprite[] swSomersaultFall = LoadSheetSprites(swMovePath, "SW_SomersaultFall", 8);
            Sprite[] swAirDashHero = LoadSheetSprites(swMovePath, "SW_AirDashHero", 8);
            bool hasSwSheet = swIdleLand.Length >= 8 && swIdleLand[0] != null && swAirDashHero.Length >= 8 && swAirDashHero[7] != null;

            // Smooth 12-frame Upright Walk, Pinned 12-frame Bench Rest & 24-frame Look-Left/Right Searching (320px @ 116 PPU)
            string walkRestSmoothPath = "Assets/Art/Characters/Player/Player_WalkRestSmooth_Sheet.png";
            Sprite[] smoothWalk = LoadSheetSprites(walkRestSmoothPath, "SmoothWalk", 12);
            Sprite[] smoothRest = LoadSheetSprites(walkRestSmoothPath, "SmoothRest", 12);
            Sprite[] smoothRestLook = LoadSheetSprites(walkRestSmoothPath, "SmoothRestLook", 24);
            bool hasWalkRestSmooth = smoothWalk.Length >= 12 && smoothWalk[0] != null && smoothRest.Length >= 12 && smoothRest[0] != null;
            bool hasRestLookSmooth = smoothRestLook.Length >= 24 && smoothRestLook[0] != null;

            // Expansion 5x6 Moveset Sheet (320px @ 116 PPU: Glide, Skid/Pivot, Charge Slash, Cross Stitch, Thread Storm)
            string movesetExtPath = "Assets/Art/Characters/Player/Player_MovesetExt_Sheet.png";
            Sprite[] extGlide = LoadSheetSprites(movesetExtPath, "ExtGlide", 6);
            Sprite[] extSkidPivot = LoadSheetSprites(movesetExtPath, "ExtSkidPivot", 6);
            Sprite[] extChargeSlash = LoadSheetSprites(movesetExtPath, "ExtChargeSlash", 6);
            Sprite[] extCrossStitch = LoadSheetSprites(movesetExtPath, "ExtCrossStitch", 6);
            Sprite[] extThreadStorm = LoadSheetSprites(movesetExtPath, "ExtThreadStorm", 6);

            // Expansion 4x6 Silk Skills + Taunt Sheet (320px @ 116 PPU: Sharpdart, Pale Nails, Rune Rage, Taunt)
            string silkSkillsExtPath = "Assets/Art/Characters/Player/Player_SilkSkillsExt_Sheet.png";
            Sprite[] extSharpdart = LoadSheetSprites(silkSkillsExtPath, "ExtSharpdart", 6);
            Sprite[] extPaleNails = LoadSheetSprites(silkSkillsExtPath, "ExtPaleNails", 6);
            Sprite[] extRuneRage = LoadSheetSprites(silkSkillsExtPath, "ExtRuneRage", 6);
            Sprite[] extTaunt = LoadSheetSprites(silkSkillsExtPath, "ExtTaunt", 6);

            // Expansion 3x6 Traversal & Needolin Sheet (320px @ 116 PPU: Harpoon, Silksoar, Needolin)
            string traversalExtPath = "Assets/Art/Characters/Player/Player_TraversalExt_Sheet.png";
            Sprite[] extHarpoon = LoadSheetSprites(traversalExtPath, "ExtHarpoon", 6);
            Sprite[] extSilksoar = LoadSheetSprites(traversalExtPath, "ExtSilksoar", 6);
            Sprite[] extNeedolin = LoadSheetSprites(traversalExtPath, "ExtNeedolin", 6);

            // Expansion 3x6 Reaper & Witch Crests Sheet (320px @ 116 PPU: ReaperSlash, WitchWhip, WitchBind)
            string crestsExtPath = "Assets/Art/Characters/Player/Player_CrestsExt_Sheet.png";
            Sprite[] extReaperSlash = LoadSheetSprites(crestsExtPath, "ExtReaperSlash", 6);
            Sprite[] extWitchWhip = LoadSheetSprites(crestsExtPath, "ExtWitchWhip", 6);
            Sprite[] extWitchBind = LoadSheetSprites(crestsExtPath, "ExtWitchBind", 6);

            // Restore beloved smooth 24-frame cloth-physics standing Idle animation (14 FPS, 2.284u canonical height)
            AnimationClip pIdle = CreateSpriteClip("Assets/Animations/Player/Player_Idle.anim",
                idleSmooth, 14.0f, true);
            AnimationClip pGrace1 = CreateSpriteClip("Assets/Animations/Player/Player_IdleGrace1.anim",
                grace1Smooth, 14.0f, false);
            AnimationClip pGrace2 = CreateSpriteClip("Assets/Animations/Player/Player_IdleGrace2.anim",
                grace2Smooth, 14.0f, false);
            AnimationClip pGrace3 = CreateSpriteClip("Assets/Animations/Player/Player_IdleGrace3.anim",
                grace3Smooth, 14.0f, false);

            // Hybrid Optimal: Snappy Silksong micro-transitions/bursts + Smooth high-frame looping locomotion!
            AnimationClip pRunStart = CreateSpriteClip("Assets/Animations/Player/Player_RunStart.anim",
                hasSwSheet ? SliceSprites(swWalk, 0, 2) : SliceSprites(smoothRun, 0, 2), 18.0f, false);
            AnimationClip pWalk = CreateSpriteClip("Assets/Animations/Player/Player_Walk.anim",
                hasWalkRestSmooth ? smoothWalk : (hasSwSheet ? SliceSprites(swWalk, 2, 6) : new Sprite[] { loco[2], locoExt[1], locoExt[2], loco[3], locoExt[1], locoExt[3] }),
                hasWalkRestSmooth ? 16.0f : (hasSwSheet ? 14.0f : 9.5f), true);
            AnimationClip pDash = CreateSpriteClip("Assets/Animations/Player/Player_Dash.anim",
                hasSwSheet ? SliceSprites(swDashSprint, 0, 2) : smoothDash, hasSwSheet ? 22.0f : 58.0f, false);
            AnimationClip pDashToSprint = CreateSpriteClip("Assets/Animations/Player/Player_DashToSprint.anim",
                hasSwSheet ? SliceSprites(swDashSprint, 2, 1) : SliceSprites(smoothRun, 0, 1), 18.0f, false);
            AnimationClip pRun = CreateSpriteClip("Assets/Animations/Player/Player_Run.anim",
                smoothRun, 30.0f, true);
            AnimationClip pJump = CreateSpriteClip("Assets/Animations/Player/Player_Jump.anim",
                SliceSprites(smoothJumpFall, 0, 10), 28.0f, false);
            AnimationClip pJumpApex = CreateSpriteClip("Assets/Animations/Player/Player_JumpApex.anim",
                SliceSprites(smoothJumpFall, 10, 4), 16.0f, true);
            AnimationClip pLedgeSomersault = CreateSpriteClip("Assets/Animations/Player/Player_LedgeSomersault.anim",
                hasSwSheet ? SliceSprites(swSomersaultFall, 0, 4) : SliceSprites(smoothJumpFall, 10, 4), 22.0f, false);
            AnimationClip pSomersaultToFall = CreateSpriteClip("Assets/Animations/Player/Player_SomersaultToFall.anim",
                hasSwSheet ? SliceSprites(swSomersaultFall, 4, 1) : SliceSprites(smoothJumpFall, 14, 1), 18.0f, false);
            AnimationClip pFall = CreateSpriteClip("Assets/Animations/Player/Player_Fall.anim",
                SliceSprites(smoothJumpFall, 14, 6), 18.0f, true);
            AnimationClip pLand = CreateSpriteClip("Assets/Animations/Player/Player_Land.anim",
                hasSwSheet ? SliceSprites(swIdleLand, 4, 4) : SliceSprites(smoothJumpFall, 20, 4), hasSwSheet ? 18.0f : 28.0f, false);
            AnimationClip pAirDash = CreateSpriteClip("Assets/Animations/Player/Player_AirDash.anim",
                hasSwSheet ? SliceSprites(swAirDashHero, 0, 2) : smoothDash, 22.0f, false);
            AnimationClip pDownwardDash = CreateSpriteClip("Assets/Animations/Player/Player_DownwardDash.anim",
                hasSwSheet ? SliceSprites(swAirDashHero, 2, 2) : smoothDash, 24.0f, false);
            AnimationClip pHeroLand = CreateSpriteClip("Assets/Animations/Player/Player_HeroLand.anim",
                hasSwSheet ? SliceSprites(swAirDashHero, 4, 4) : SliceSprites(smoothJumpFall, 20, 4), 18.0f, false);
            AnimationClip pDoubleJump = CreateSpriteClip("Assets/Animations/Player/Player_DoubleJump.anim",
                new Sprite[] { act[1], loco[6], actExt[1] }, 12f, false);

            AnimationClip pAtk1 = CreateSpriteClip("Assets/Animations/Player/Player_Attack1.anim",
                smoothSlash, 48.0f, false);
            AnimationClip pAtk2 = CreateSpriteClip("Assets/Animations/Player/Player_Attack2.anim",
                new Sprite[] { actExt[2], act[4], actExt[5] }, 12f, false);
            AnimationClip pAtk3 = CreateSpriteClip("Assets/Animations/Player/Player_Attack3.anim",
                new Sprite[] { actExt[2], act[5], actExt[3] }, 10f, false);
            AnimationClip pAirAtk = CreateSpriteClip("Assets/Animations/Player/Player_AirAttack.anim",
                new Sprite[] { actExt[2], actExt[4], actExt[3] }, 12f, false);
            AnimationClip pAtkUp = CreateSpriteClip("Assets/Animations/Player/Player_AttackUp.anim",
                new Sprite[] { actExt[2], actExt[5], actExt[3] }, 12f, false);
            AnimationClip pAtkDown = CreateSpriteClip("Assets/Animations/Player/Player_AttackDown.anim",
                new Sprite[] { act[1], actExt[6], act[1] }, 12f, false);

            AnimationClip pWallCling = CreateSpriteClip("Assets/Animations/Player/Player_WallCling.anim",
                SliceSprites(smoothWall, 0, 10), 16.0f, true);
            AnimationClip pWallJump = CreateSpriteClip("Assets/Animations/Player/Player_WallJump.anim",
                SliceSprites(smoothWall, 10, 10), 34.0f, false);

            AnimationClip pHurt = CreateSpriteClip("Assets/Animations/Player/Player_Hurt.anim",
                new Sprite[] { act[6], actExt[7] }, 10f, false);
            AnimationClip pDeath = CreateSpriteClip("Assets/Animations/Player/Player_Death.anim",
                new Sprite[] { act[6], actExt[7], act[7] }, 5f, false);
            AnimationClip pSitDown = CreateSpriteClip("Assets/Animations/Player/Player_SitDown.anim",
                new Sprite[] { bench[0], bench[1] }, 8f, false);
            AnimationClip pSitting = CreateSpriteClip("Assets/Animations/Player/Player_Sitting.anim",
                hasWalkRestSmooth ? smoothRest : (hasSwSheet ? SliceSprites(swRest, 0, 4) : new Sprite[] { bench[2], bench[3] }),
                hasWalkRestSmooth ? 10.0f : (hasSwSheet ? 12.0f : 2.2f), true);
            AnimationClip pRestAlert = CreateSpriteClip("Assets/Animations/Player/Player_RestAlertGlance.anim",
                hasRestLookSmooth ? smoothRestLook : (hasSwSheet ? SliceSprites(swRest, 4, 2) : new Sprite[] { bench[2], bench[3] }),
                hasRestLookSmooth ? 12.0f : 14.0f, false);
            AnimationClip pRestJumpOff = CreateSpriteClip("Assets/Animations/Player/Player_RestJumpOff.anim",
                hasSwSheet ? SliceSprites(swRest, 6, 2) : new Sprite[] { bench[1], bench[0] }, 18.0f, false);
            AnimationClip pStandUp = CreateSpriteClip("Assets/Animations/Player/Player_StandUp.anim",
                new Sprite[] { bench[1], bench[0] }, 9f, false);
            AnimationClip pLedgeMantle = CreateSpriteClip("Assets/Animations/Player/Player_LedgeMantle.anim",
                ledgeMantle, 13.2f, false);
            AnimationClip pBindHeal = CreateTimedSpriteClip("Assets/Animations/Player/Player_BindHeal.anim",
                healBind, new float[] { 0.00f, 0.14f, 0.50f, 0.66f }, 24f, false);
            AnimationClip pGlide = CreateSpriteClip("Assets/Animations/Player/Player_Glide.anim",
                extGlide, 14.0f, true);
            AnimationClip pSkidPivot = CreateSpriteClip("Assets/Animations/Player/Player_SkidPivot.anim",
                extSkidPivot, 22.0f, false);
            AnimationClip pChargeSlash = CreateSpriteClip("Assets/Animations/Player/Player_ChargeSlash.anim",
                extChargeSlash, 20.0f, false);
            AnimationClip pCrossStitch = CreateSpriteClip("Assets/Animations/Player/Player_CrossStitch.anim",
                extCrossStitch, 18.0f, false);
            AnimationClip pThreadStorm = CreateSpriteClip("Assets/Animations/Player/Player_ThreadStorm.anim",
                extThreadStorm, 18.0f, true);
            AnimationClip pSharpdart = CreateSpriteClip("Assets/Animations/Player/Player_Sharpdart.anim",
                extSharpdart, 24.0f, false);
            AnimationClip pPaleNails = CreateSpriteClip("Assets/Animations/Player/Player_PaleNails.anim",
                extPaleNails, 20.0f, false);
            AnimationClip pRuneRage = CreateSpriteClip("Assets/Animations/Player/Player_RuneRage.anim",
                extRuneRage, 18.0f, false);
            AnimationClip pTaunt = CreateSpriteClip("Assets/Animations/Player/Player_Taunt.anim",
                extTaunt, 16.0f, false);
            AnimationClip pHarpoon = CreateSpriteClip("Assets/Animations/Player/Player_Harpoon.anim",
                extHarpoon, 22.0f, false);
            AnimationClip pSilksoar = CreateSpriteClip("Assets/Animations/Player/Player_Silksoar.anim",
                extSilksoar, 20.0f, false);
            AnimationClip pNeedolin = CreateSpriteClip("Assets/Animations/Player/Player_Needolin.anim",
                extNeedolin, 12.0f, true);
            AnimationClip pReaperSlash = CreateSpriteClip("Assets/Animations/Player/Player_ReaperSlash.anim",
                extReaperSlash, 20.0f, false);
            AnimationClip pWitchWhip = CreateSpriteClip("Assets/Animations/Player/Player_WitchWhip.anim",
                extWitchWhip, 22.0f, false);
            AnimationClip pWitchBind = CreateSpriteClip("Assets/Animations/Player/Player_WitchBind.anim",
                extWitchBind, 16.0f, false);

            // Player Animator Controller
            string pCtrlPath = "Assets/Animations/Player/PlayerAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(pCtrlPath) != null) AssetDatabase.DeleteAsset(pCtrlPath);
            AnimatorController pCtrl = AnimatorController.CreateAnimatorControllerAtPath(pCtrlPath);
            pCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            pCtrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("VelocityY", AnimatorControllerParameterType.Float);
            pCtrl.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("ComboStep", AnimatorControllerParameterType.Int);
            pCtrl.AddParameter("IsAirAttacking", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsSitting", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsWallSliding", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IdleVariant", AnimatorControllerParameterType.Int);
            pCtrl.AddParameter("Land", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("DoubleJump", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("WallJump", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("SitDown", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("GraceIdle", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("IsMantling", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsHealing", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsAirDashing", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsDownwardDashing", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsHeroLanding", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("RunStart", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("LedgeSomersault", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("RestAlert", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("RestJumpOff", AnimatorControllerParameterType.Trigger);
            pCtrl.AddParameter("IsGliding", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsSkidding", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsParrying", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsTaunting", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsHarpooning", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsSilksoaring", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsPlayingNeedolin", AnimatorControllerParameterType.Bool);
            pCtrl.AddParameter("IsWitchBind", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = pCtrl.layers[0].stateMachine;
            AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = pIdle;
            AnimatorState stGrace1 = sm.AddState("IdleGrace1"); stGrace1.motion = pGrace1; stGrace1.tag = "GraceIdle";
            AnimatorState stGrace2 = sm.AddState("IdleGrace2"); stGrace2.motion = pGrace2; stGrace2.tag = "GraceIdle";
            AnimatorState stGrace3 = sm.AddState("IdleGrace3"); stGrace3.motion = pGrace3; stGrace3.tag = "GraceIdle";
            AnimatorState stRunStart = sm.AddState("RunStart"); stRunStart.motion = pRunStart;
            AnimatorState stWalk = sm.AddState("Walking"); stWalk.motion = pWalk;
            AnimatorState stRun = sm.AddState("Running"); stRun.motion = pRun;
            AnimatorState stDashToSprint = sm.AddState("DashToSprint"); stDashToSprint.motion = pDashToSprint;
            AnimatorState stJump = sm.AddState("Jumping"); stJump.motion = pJump;
            AnimatorState stApex = sm.AddState("JumpApex"); stApex.motion = pJumpApex;
            AnimatorState stSomersault = sm.AddState("LedgeSomersault"); stSomersault.motion = pLedgeSomersault;
            AnimatorState stSomersaultToFall = sm.AddState("SomersaultToFall"); stSomersaultToFall.motion = pSomersaultToFall;
            AnimatorState stFall = sm.AddState("Falling"); stFall.motion = pFall;
            AnimatorState stLand = sm.AddState("Landing"); stLand.motion = pLand;
            AnimatorState stHeroLand = sm.AddState("HeroLanding"); stHeroLand.motion = pHeroLand;
            AnimatorState stDblJump = sm.AddState("DoubleJump"); stDblJump.motion = pDoubleJump;
            AnimatorState stDash = sm.AddState("Dash"); stDash.motion = pDash;
            AnimatorState stAirDash = sm.AddState("AirDash"); stAirDash.motion = pAirDash;
            AnimatorState stDownDash = sm.AddState("DownwardDash"); stDownDash.motion = pDownwardDash;
            AnimatorState stAtk1 = sm.AddState("Attack1"); stAtk1.motion = pAtk1;
            AnimatorState stAtk2 = sm.AddState("ComboAttack2"); stAtk2.motion = pAtk2;
            AnimatorState stAtk3 = sm.AddState("ComboAttack3"); stAtk3.motion = pAtk3;
            AnimatorState stAirAtk = sm.AddState("AirAttack"); stAirAtk.motion = pAirAtk;
            AnimatorState stAtkUp = sm.AddState("AttackUp"); stAtkUp.motion = pAtkUp;
            AnimatorState stAtkDown = sm.AddState("AttackDownPogo"); stAtkDown.motion = pAtkDown;
            AnimatorState stWallCling = sm.AddState("WallCling"); stWallCling.motion = pWallCling;
            AnimatorState stWallJump = sm.AddState("WallJump"); stWallJump.motion = pWallJump;
            AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = pHurt;
            AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = pDeath;
            AnimatorState stSitDown = sm.AddState("SitDown"); stSitDown.motion = pSitDown; stSitDown.tag = "SitDown";
            AnimatorState stSitting = sm.AddState("SittingRest"); stSitting.motion = pSitting;
            AnimatorState stRestAlert = sm.AddState("RestAlertGlance"); stRestAlert.motion = pRestAlert; stRestAlert.tag = "RestLook";
            AnimatorState stRestJumpOff = sm.AddState("RestJumpOff"); stRestJumpOff.motion = pRestJumpOff;
            AnimatorState stStandUp = sm.AddState("StandUp"); stStandUp.motion = pStandUp;
            AnimatorState stLedgeMantle = sm.AddState("LedgeMantle"); stLedgeMantle.motion = pLedgeMantle;
            AnimatorState stBindHeal = sm.AddState("BindHeal"); stBindHeal.motion = pBindHeal;
            AnimatorState stGlide = sm.AddState("Glide"); stGlide.motion = pGlide;
            AnimatorState stSkidPivot = sm.AddState("SkidPivot"); stSkidPivot.motion = pSkidPivot;
            AnimatorState stChargeSlash = sm.AddState("ChargeSlash"); stChargeSlash.motion = pChargeSlash;
            AnimatorState stCrossStitch = sm.AddState("CrossStitch"); stCrossStitch.motion = pCrossStitch;
            AnimatorState stThreadStorm = sm.AddState("ThreadStorm"); stThreadStorm.motion = pThreadStorm;
            AnimatorState stSharpdart = sm.AddState("Sharpdart"); stSharpdart.motion = pSharpdart;
            AnimatorState stPaleNails = sm.AddState("PaleNails"); stPaleNails.motion = pPaleNails;
            AnimatorState stRuneRage = sm.AddState("RuneRage"); stRuneRage.motion = pRuneRage;
            AnimatorState stTaunt = sm.AddState("Taunt"); stTaunt.motion = pTaunt;
            AnimatorState stHarpoon = sm.AddState("Harpoon"); stHarpoon.motion = pHarpoon;
            AnimatorState stSilksoar = sm.AddState("Silksoar"); stSilksoar.motion = pSilksoar;
            AnimatorState stNeedolin = sm.AddState("Needolin"); stNeedolin.motion = pNeedolin;
            AnimatorState stReaperSlash = sm.AddState("ReaperSlash"); stReaperSlash.motion = pReaperSlash;
            AnimatorState stWitchWhip = sm.AddState("WitchWhip"); stWitchWhip.motion = pWitchWhip;
            AnimatorState stWitchBind = sm.AddState("WitchBind"); stWitchBind.motion = pWitchBind;
            sm.defaultState = stIdle;

            // Graceful Feminine Extended Idles (triggered after ~3.5s calm idle, interruptible on frame 0!)
            AnimatorStateTransition tG1 = AddFastTrans(stIdle, stGrace1);
            tG1.duration = 0.06f;
            tG1.AddCondition(AnimatorConditionMode.If, 0, "GraceIdle");
            tG1.AddCondition(AnimatorConditionMode.Equals, 1, "IdleVariant");

            AnimatorStateTransition tG2 = AddFastTrans(stIdle, stGrace2);
            tG2.duration = 0.06f;
            tG2.AddCondition(AnimatorConditionMode.If, 0, "GraceIdle");
            tG2.AddCondition(AnimatorConditionMode.Equals, 2, "IdleVariant");

            AnimatorStateTransition tG3 = AddFastTrans(stIdle, stGrace3);
            tG3.duration = 0.06f;
            tG3.AddCondition(AnimatorConditionMode.If, 0, "GraceIdle");
            tG3.AddCondition(AnimatorConditionMode.Equals, 3, "IdleVariant");

            AnimatorState[] graceStates = new AnimatorState[] { stGrace1, stGrace2, stGrace3 };
            for (int g = 0; g < graceStates.Length; g++)
            {
                AnimatorState gs = graceStates[g];
                AnimatorStateTransition gDone = gs.AddTransition(stIdle);
                gDone.hasExitTime = true;
                gDone.exitTime = 0.96f;
                gDone.duration = 0.06f;

                AddFastTrans(gs, stWalk).AddCondition(AnimatorConditionMode.Greater, 0.18f, "Speed");

                AnimatorStateTransition gJump = AddFastTrans(gs, stJump);
                gJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                gJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");

                AnimatorStateTransition gFall = AddFastTrans(gs, stFall);
                gFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                gFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");
            }

            // Grounded locomotion transitions (with Silksong RunStart frame-reuse into Sprint)
            AnimatorStateTransition tIdleRunStart = AddFastTrans(stIdle, stRunStart);
            tIdleRunStart.AddCondition(AnimatorConditionMode.If, 0, "RunStart");
            AnimatorStateTransition tFallRunLand = AddFastTrans(stFall, stRunStart);
            tFallRunLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            tFallRunLand.AddCondition(AnimatorConditionMode.If, 0, "RunStart");
            AnimatorStateTransition tRunStartToRun = stRunStart.AddTransition(stRun);
            tRunStartToRun.hasExitTime = true;
            tRunStartToRun.exitTime = 0.88f;
            tRunStartToRun.duration = 0.02f;
            tRunStartToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            AnimatorStateTransition tRunStartToWalk = stRunStart.AddTransition(stWalk);
            tRunStartToWalk.hasExitTime = true;
            tRunStartToWalk.exitTime = 0.88f;
            tRunStartToWalk.duration = 0.02f;
            tRunStartToWalk.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            AddFastTrans(stRunStart, stIdle).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

            AddFastTrans(stIdle, stWalk).AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            AddFastTrans(stWalk, stIdle).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            AddFastTrans(stWalk, stRun).AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            AddFastTrans(stRun, stWalk).AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
            AddFastTrans(stRun, stIdle).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

            // Silksong Ledge Somersault -> Transition to Fall -> Falling Loop
            AnimatorStateTransition tAnySomersault = AddAnyTrans(sm, stSomersault);
            tAnySomersault.AddCondition(AnimatorConditionMode.If, 0, "LedgeSomersault");
            tAnySomersault.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnySomersault.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AnimatorStateTransition tSomToTrans = stSomersault.AddTransition(stSomersaultToFall);
            tSomToTrans.hasExitTime = true;
            tSomToTrans.exitTime = 0.92f;
            tSomToTrans.duration = 0.02f;
            AnimatorStateTransition tSomTransToFall = stSomersaultToFall.AddTransition(stFall);
            tSomTransToFall.hasExitTime = true;
            tSomTransToFall.exitTime = 0.90f;
            tSomTransToFall.duration = 0.02f;
            AddFastTrans(stSomersault, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AddFastTrans(stSomersaultToFall, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // Jump & Fall
            AnimatorStateTransition tIdleJump = AddFastTrans(stIdle, stJump);
            tIdleJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tIdleJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");
            AnimatorStateTransition tWalkJump = AddFastTrans(stWalk, stJump);
            tWalkJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tWalkJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");
            AnimatorStateTransition tRunJump = AddFastTrans(stRun, stJump);
            tRunJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tRunJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");

            AnimatorStateTransition tIdleFall = AddFastTrans(stIdle, stFall);
            tIdleFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tIdleFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");
            AnimatorStateTransition tWalkFall = AddFastTrans(stWalk, stFall);
            tWalkFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tWalkFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");
            AnimatorStateTransition tJumpApex = AddFastTrans(stJump, stApex);
            tJumpApex.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tJumpApex.AddCondition(AnimatorConditionMode.Less, 3.4f, "VelocityY");
            AnimatorStateTransition tDblApex = AddFastTrans(stDblJump, stApex);
            tDblApex.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tDblApex.AddCondition(AnimatorConditionMode.Less, 3.4f, "VelocityY");
            AnimatorStateTransition tApexFall = AddFastTrans(stApex, stFall);
            tApexFall.AddCondition(AnimatorConditionMode.Less, -3.4f, "VelocityY");
            AnimatorStateTransition tJumpFall = AddFastTrans(stJump, stFall);
            tJumpFall.AddCondition(AnimatorConditionMode.Less, -3.5f, "VelocityY");
            AnimatorStateTransition tDblFall = AddFastTrans(stDblJump, stFall);
            tDblFall.AddCondition(AnimatorConditionMode.Less, -3.5f, "VelocityY");

            // Landing & Heavy Hero Landing
            AnimatorStateTransition tAnyHeroLand = AddAnyTrans(sm, stHeroLand);
            tAnyHeroLand.AddCondition(AnimatorConditionMode.If, 0, "IsHeroLanding");
            tAnyHeroLand.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyHeroLand.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AnimatorStateTransition tHeroLandDone = stHeroLand.AddTransition(stIdle);
            tHeroLandDone.hasExitTime = true;
            tHeroLandDone.exitTime = 0.92f;
            tHeroLandDone.duration = 0.03f;
            AnimatorStateTransition tHeroLandRun = AddFastTrans(stHeroLand, stRunStart);
            tHeroLandRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHeroLanding");
            tHeroLandRun.AddCondition(AnimatorConditionMode.Greater, 0.35f, "Speed");

            AddFastTrans(stFall, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AddFastTrans(stApex, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AddFastTrans(stJump, stIdle).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tLandDone = stLand.AddTransition(stIdle);
            tLandDone.hasExitTime = true;
            tLandDone.exitTime = 0.85f;
            tLandDone.duration = 0.04f;
            AddFastTrans(stLand, stWalk).AddCondition(AnimatorConditionMode.Greater, 0.35f, "Speed");

            // Wall Cling & Wall Jump transitions
            AnimatorStateTransition tAnyWallCling = AddAnyTrans(sm, stWallCling);
            tAnyWallCling.AddCondition(AnimatorConditionMode.If, 0, "IsWallSliding");
            tAnyWallCling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyWallCling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            tAnyWallCling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            AddFastTrans(stWallCling, stWallJump).AddCondition(AnimatorConditionMode.If, 0, "WallJump");
            AddFastTrans(stWallCling, stIdle).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tWallToJump = AddFastTrans(stWallCling, stJump);
            tWallToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWallSliding");
            tWallToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tWallToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");
            AnimatorStateTransition tWallToFall = AddFastTrans(stWallCling, stFall);
            tWallToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWallSliding");
            tWallToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tWallToFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VelocityY");

            AnimatorStateTransition tAnyWallJump = AddAnyTrans(sm, stWallJump);
            tAnyWallJump.AddCondition(AnimatorConditionMode.If, 0, "WallJump");
            tAnyWallJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tWallJumpApex = stWallJump.AddTransition(stApex);
            tWallJumpApex.hasExitTime = true;
            tWallJumpApex.exitTime = 0.85f;
            tWallJumpApex.duration = 0.04f;
            tWallJumpApex.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tWallJumpApex.AddCondition(AnimatorConditionMode.Less, 3.4f, "VelocityY");
            AnimatorStateTransition tWallJumpFall = AddFastTrans(stWallJump, stFall);
            tWallJumpFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tWallJumpFall.AddCondition(AnimatorConditionMode.Less, -3.2f, "VelocityY");
            AddFastTrans(stWallJump, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // AnyState priority triggers (never conflict with locomotion)
            AnimatorStateTransition tAnyDeath = AddAnyTrans(sm, stDeath);
            tAnyDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            AddFastTrans(stDeath, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");

            AnimatorStateTransition tAnyHurt = AddAnyTrans(sm, stHurt);
            tAnyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
            tAnyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

            // Instant Floor Dash -> DashToSprint -> Running, plus AirDash & DownwardDash
            AnimatorStateTransition tAnyDash = AddAnyTrans(sm, stDash);
            tAnyDash.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");
            tAnyDash.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tDashToTrans = AddFastTrans(stDash, stDashToSprint);
            tDashToTrans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            tDashToTrans.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            AnimatorStateTransition tDashTransDone = stDashToSprint.AddTransition(stRun);
            tDashTransDone.hasExitTime = true;
            tDashTransDone.exitTime = 0.90f;
            tDashTransDone.duration = 0.02f;
            AddFastTrans(stDashToSprint, stIdle).AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
            AnimatorStateTransition tDashToIdle = AddFastTrans(stDash, stIdle);
            tDashToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            tDashToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

            AnimatorStateTransition tAnyAirDash = AddAnyTrans(sm, stAirDash);
            tAnyAirDash.AddCondition(AnimatorConditionMode.If, 0, "IsAirDashing");
            tAnyAirDash.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stAirDash, stFall).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAirDashing");

            AnimatorStateTransition tAnyDownDash = AddAnyTrans(sm, stDownDash);
            tAnyDownDash.AddCondition(AnimatorConditionMode.If, 0, "IsDownwardDashing");
            tAnyDownDash.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stDownDash, stHeroLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AddFastTrans(stDownDash, stFall).AddCondition(AnimatorConditionMode.IfNot, 0, "IsDownwardDashing");

            AnimatorStateTransition tAnyDbl = AddAnyTrans(sm, stDblJump);
            tAnyDbl.AddCondition(AnimatorConditionMode.If, 0, "DoubleJump");
            tAnyDbl.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");

            AnimatorStateTransition tAnyAirAtk = AddAnyTrans(sm, stAirAtk);
            tAnyAirAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyAirAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAirAttacking");
            tAnyAirAtk.AddCondition(AnimatorConditionMode.Equals, 1, "ComboStep");
            tAnyAirAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stAirAtk, stFall).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

            AnimatorStateTransition tAnyA1 = AddAnyTrans(sm, stAtk1);
            tAnyA1.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyA1.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAirAttacking");
            tAnyA1.AddCondition(AnimatorConditionMode.Equals, 1, "ComboStep");
            AddFastTrans(stAtk1, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

            AnimatorStateTransition tAnyA2 = AddAnyTrans(sm, stAtk2);
            tAnyA2.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyA2.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAirAttacking");
            tAnyA2.AddCondition(AnimatorConditionMode.Equals, 2, "ComboStep");
            AddFastTrans(stAtk2, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

            AnimatorStateTransition tAnyA3 = AddAnyTrans(sm, stAtk3);
            tAnyA3.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyA3.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAirAttacking");
            tAnyA3.AddCondition(AnimatorConditionMode.Equals, 3, "ComboStep");
            AddFastTrans(stAtk3, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

            AnimatorStateTransition tAnyAtkUp = AddAnyTrans(sm, stAtkUp);
            tAnyAtkUp.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyAtkUp.AddCondition(AnimatorConditionMode.Equals, 4, "ComboStep");
            tAnyAtkUp.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tUpToIdle = AddFastTrans(stAtkUp, stIdle);
            tUpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tUpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tUpToFall = AddFastTrans(stAtkUp, stFall);
            tUpToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tUpToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            AnimatorStateTransition tAnyDownPogo = AddAnyTrans(sm, stAtkDown);
            tAnyDownPogo.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyDownPogo.AddCondition(AnimatorConditionMode.Equals, 5, "ComboStep");
            tAnyDownPogo.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stAtkDown, stFall).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

            // Bench Sit & Sanctuary Rest transitions
            AnimatorStateTransition tAnySitDown = AddAnyTrans(sm, stSitDown);
            tAnySitDown.AddCondition(AnimatorConditionMode.If, 0, "SitDown");
            tAnySitDown.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");

            AnimatorStateTransition tSitToRest = stSitDown.AddTransition(stSitting);
            tSitToRest.hasExitTime = true;
            tSitToRest.exitTime = 0.92f;
            tSitToRest.duration = 0.04f;
            tSitToRest.AddCondition(AnimatorConditionMode.If, 0, "IsSitting");
            AddFastTrans(stSitDown, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsSitting");

            // Bench Sit, Rest Alert Glance & Instant Jump-Off transitions
            AnimatorStateTransition tSitAlert = AddFastTrans(stSitting, stRestAlert);
            tSitAlert.AddCondition(AnimatorConditionMode.If, 0, "RestAlert");
            tSitAlert.AddCondition(AnimatorConditionMode.If, 0, "IsSitting");
            AnimatorStateTransition tAlertBack = stRestAlert.AddTransition(stSitting);
            tAlertBack.hasExitTime = true;
            tAlertBack.exitTime = 0.98f;
            tAlertBack.duration = 0.02f;
            tAlertBack.AddCondition(AnimatorConditionMode.If, 0, "IsSitting");
            AddFastTrans(stRestAlert, stStandUp).AddCondition(AnimatorConditionMode.IfNot, 0, "IsSitting");

            AnimatorStateTransition tAnyRestJump = AddAnyTrans(sm, stRestJumpOff);
            tAnyRestJump.AddCondition(AnimatorConditionMode.If, 0, "RestJumpOff");
            tAnyRestJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stRestJumpOff, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tRestJumpToFall = stRestJumpOff.AddTransition(stFall);
            tRestJumpToFall.hasExitTime = true;
            tRestJumpToFall.exitTime = 0.92f;
            tRestJumpToFall.duration = 0.03f;

            AddFastTrans(stSitting, stStandUp).AddCondition(AnimatorConditionMode.IfNot, 0, "IsSitting");
            AnimatorStateTransition tStandDone = stStandUp.AddTransition(stIdle);
            tStandDone.hasExitTime = true;
            tStandDone.exitTime = 0.88f;
            tStandDone.duration = 0.04f;
            AddFastTrans(stStandUp, stWalk).AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");

            // Agile Ledge-Grab & Mantle Climb transitions
            AnimatorStateTransition tAnyMantle = AddAnyTrans(sm, stLedgeMantle);
            tAnyMantle.AddCondition(AnimatorConditionMode.If, 0, "IsMantling");
            tAnyMantle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(stLedgeMantle, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsMantling");

            // Bind Healing transitions (supports both Ground & Mid-Air Bind!)
            AnimatorStateTransition tAnyHeal = AddAnyTrans(sm, stBindHeal);
            tAnyHeal.AddCondition(AnimatorConditionMode.If, 0, "IsHealing");
            tAnyHeal.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyHeal.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AnimatorStateTransition tHealToIdle = AddFastTrans(stBindHeal, stIdle);
            tHealToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHealing");
            tHealToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tHealToFall = AddFastTrans(stBindHeal, stFall);
            tHealToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHealing");
            tHealToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Drifter's Cloak Glide transitions (Row 1: ExtGlide)
            AnimatorStateTransition tAnyGlide = AddAnyTrans(sm, stGlide);
            tAnyGlide.AddCondition(AnimatorConditionMode.If, 0, "IsGliding");
            tAnyGlide.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyGlide.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            tAnyGlide.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tAnyGlide.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHealing");
            AddFastTrans(stGlide, stLand).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tGlideToFall = AddFastTrans(stGlide, stFall);
            tGlideToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGliding");
            tGlideToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Sprint Skid Stop & Pivot transitions (Row 2: ExtSkidPivot)
            AnimatorStateTransition tAnySkid = AddAnyTrans(sm, stSkidPivot);
            tAnySkid.AddCondition(AnimatorConditionMode.If, 0, "IsSkidding");
            tAnySkid.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            tAnySkid.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnySkid.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            tAnySkid.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            tAnySkid.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            AnimatorStateTransition tSkidToRun = AddFastTrans(stSkidPivot, stRun);
            tSkidToRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSkidding");
            tSkidToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
            AnimatorStateTransition tSkidToWalk = AddFastTrans(stSkidPivot, stWalk);
            tSkidToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSkidding");
            tSkidToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
            tSkidToWalk.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");
            AnimatorStateTransition tSkidToIdle = AddFastTrans(stSkidPivot, stIdle);
            tSkidToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSkidding");
            tSkidToIdle.AddCondition(AnimatorConditionMode.Less, 0.25f, "Speed");
            AnimatorStateTransition tSkidToJump = AddFastTrans(stSkidPivot, stJump);
            tSkidToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tSkidToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");
            AnimatorStateTransition tSkidToFall = AddFastTrans(stSkidPivot, stFall);
            tSkidToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tSkidToFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");

            // Charge Slash 2-Hit Guard-Breaker transitions (Row 3: ExtChargeSlash, ComboStep == 6)
            AnimatorStateTransition tAnyChargeSlash = AddAnyTrans(sm, stChargeSlash);
            tAnyChargeSlash.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyChargeSlash.AddCondition(AnimatorConditionMode.Equals, 6, "ComboStep");
            tAnyChargeSlash.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tChargeToIdle = AddFastTrans(stChargeSlash, stIdle);
            tChargeToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tChargeToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tChargeToFall = AddFastTrans(stChargeSlash, stFall);
            tChargeToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tChargeToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Cross Stitch Parry Stance transitions (Row 4: ExtCrossStitch)
            AnimatorStateTransition tAnyParry = AddAnyTrans(sm, stCrossStitch);
            tAnyParry.AddCondition(AnimatorConditionMode.If, 0, "IsParrying");
            tAnyParry.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyParry.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            tAnyParry.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tAnyParry.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            AnimatorStateTransition tParryToIdle = AddFastTrans(stCrossStitch, stIdle);
            tParryToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsParrying");
            tParryToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tParryToFall = AddFastTrans(stCrossStitch, stFall);
            tParryToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsParrying");
            tParryToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Thread Storm 360-Degree Silk Cyclone transitions (Row 5: ExtThreadStorm, ComboStep == 7)
            AnimatorStateTransition tAnyThreadStorm = AddAnyTrans(sm, stThreadStorm);
            tAnyThreadStorm.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyThreadStorm.AddCondition(AnimatorConditionMode.Equals, 7, "ComboStep");
            tAnyThreadStorm.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tStormToIdle = AddFastTrans(stThreadStorm, stIdle);
            tStormToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tStormToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tStormToFall = AddFastTrans(stThreadStorm, stFall);
            tStormToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tStormToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Sharpdart Piercing i-Frame Silk Blitz transitions (Row 1: ExtSharpdart, ComboStep == 8)
            AnimatorStateTransition tAnySharpdart = AddAnyTrans(sm, stSharpdart);
            tAnySharpdart.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnySharpdart.AddCondition(AnimatorConditionMode.Equals, 8, "ComboStep");
            tAnySharpdart.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tSharpToIdle = AddFastTrans(stSharpdart, stIdle);
            tSharpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tSharpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tSharpToFall = AddFastTrans(stSharpdart, stFall);
            tSharpToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tSharpToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Pale Nails Homing Silk Claws transitions (Row 2: ExtPaleNails, ComboStep == 9)
            AnimatorStateTransition tAnyPaleNails = AddAnyTrans(sm, stPaleNails);
            tAnyPaleNails.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyPaleNails.AddCondition(AnimatorConditionMode.Equals, 9, "ComboStep");
            tAnyPaleNails.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tNailsToIdle = AddFastTrans(stPaleNails, stIdle);
            tNailsToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tNailsToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tNailsToFall = AddFastTrans(stPaleNails, stFall);
            tNailsToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tNailsToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Rune Rage Delayed Multi-Rune Detonation transitions (Row 3: ExtRuneRage, ComboStep == 10)
            AnimatorStateTransition tAnyRuneRage = AddAnyTrans(sm, stRuneRage);
            tAnyRuneRage.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyRuneRage.AddCondition(AnimatorConditionMode.Equals, 10, "ComboStep");
            tAnyRuneRage.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tRuneToIdle = AddFastTrans(stRuneRage, stIdle);
            tRuneToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tRuneToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tRuneToFall = AddFastTrans(stRuneRage, stFall);
            tRuneToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tRuneToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Taunt / Garama! Sonic Challenge transitions (Row 4: ExtTaunt)
            AnimatorStateTransition tAnyTaunt = AddAnyTrans(sm, stTaunt);
            tAnyTaunt.AddCondition(AnimatorConditionMode.If, 0, "IsTaunting");
            tAnyTaunt.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            tAnyTaunt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyTaunt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            tAnyTaunt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tAnyTaunt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            AnimatorStateTransition tTauntToIdle = AddFastTrans(stTaunt, stIdle);
            tTauntToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTaunting");
            tTauntToIdle.AddCondition(AnimatorConditionMode.Less, 0.25f, "Speed");
            AnimatorStateTransition tTauntToWalk = AddFastTrans(stTaunt, stWalk);
            tTauntToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTaunting");
            tTauntToWalk.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");
            AnimatorStateTransition tTauntToFall = AddFastTrans(stTaunt, stFall);
            tTauntToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Harpoon / Clawline Zip-Pull transitions (Row 1: ExtHarpoon)
            AnimatorStateTransition tAnyHarpoon = AddAnyTrans(sm, stHarpoon);
            tAnyHarpoon.AddCondition(AnimatorConditionMode.If, 0, "IsHarpooning");
            tAnyHarpoon.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyHarpoon.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AnimatorStateTransition tHarpoonToIdle = AddFastTrans(stHarpoon, stIdle);
            tHarpoonToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHarpooning");
            tHarpoonToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tHarpoonToFall = AddFastTrans(stHarpoon, stFall);
            tHarpoonToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHarpooning");
            tHarpoonToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Silksoar Vertical Rocket Launch transitions (Row 2: ExtSilksoar)
            AnimatorStateTransition tAnySilksoar = AddAnyTrans(sm, stSilksoar);
            tAnySilksoar.AddCondition(AnimatorConditionMode.If, 0, "IsSilksoaring");
            tAnySilksoar.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnySilksoar.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AnimatorStateTransition tSoarToIdle = AddFastTrans(stSilksoar, stIdle);
            tSoarToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSilksoaring");
            tSoarToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tSoarToFall = AddFastTrans(stSilksoar, stFall);
            tSoarToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSilksoaring");
            tSoarToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Needolin Silk-Violin Melody transitions (Row 3: ExtNeedolin)
            AnimatorStateTransition tAnyNeedolin = AddAnyTrans(sm, stNeedolin);
            tAnyNeedolin.AddCondition(AnimatorConditionMode.If, 0, "IsPlayingNeedolin");
            tAnyNeedolin.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            tAnyNeedolin.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyNeedolin.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            tAnyNeedolin.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tAnyNeedolin.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            AnimatorStateTransition tNeedolinToIdle = AddFastTrans(stNeedolin, stIdle);
            tNeedolinToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsPlayingNeedolin");
            tNeedolinToIdle.AddCondition(AnimatorConditionMode.Less, 0.25f, "Speed");
            AnimatorStateTransition tNeedolinToWalk = AddFastTrans(stNeedolin, stWalk);
            tNeedolinToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsPlayingNeedolin");
            tNeedolinToWalk.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");
            AnimatorStateTransition tNeedolinToFall = AddFastTrans(stNeedolin, stFall);
            tNeedolinToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Reaper Crest Heavy Wide Scythe Cleave transitions (Row 1: ExtReaperSlash, ComboStep == 11)
            AnimatorStateTransition tAnyReaper = AddAnyTrans(sm, stReaperSlash);
            tAnyReaper.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyReaper.AddCondition(AnimatorConditionMode.Equals, 11, "ComboStep");
            tAnyReaper.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tReaperToIdle = AddFastTrans(stReaperSlash, stIdle);
            tReaperToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tReaperToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tReaperToFall = AddFastTrans(stReaperSlash, stFall);
            tReaperToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tReaperToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Witch Crest Long-Range Thorny Silk Whip transitions (Row 2: ExtWitchWhip, ComboStep == 12)
            AnimatorStateTransition tAnyWitchWhip = AddAnyTrans(sm, stWitchWhip);
            tAnyWitchWhip.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyWitchWhip.AddCondition(AnimatorConditionMode.Equals, 12, "ComboStep");
            tAnyWitchWhip.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AnimatorStateTransition tWhipToIdle = AddFastTrans(stWitchWhip, stIdle);
            tWhipToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tWhipToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tWhipToFall = AddFastTrans(stWitchWhip, stFall);
            tWhipToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            tWhipToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            // Witch Crest Root Leech Offensive Bind transitions (Row 3: ExtWitchBind)
            AnimatorStateTransition tAnyWitchBind = AddAnyTrans(sm, stWitchBind);
            tAnyWitchBind.AddCondition(AnimatorConditionMode.If, 0, "IsWitchBind");
            tAnyWitchBind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            tAnyWitchBind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AnimatorStateTransition tWBindToIdle = AddFastTrans(stWitchBind, stIdle);
            tWBindToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWitchBind");
            tWBindToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            AnimatorStateTransition tWBindToFall = AddFastTrans(stWitchBind, stFall);
            tWBindToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWitchBind");
            tWBindToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

            assets.PlayerAnimator = pCtrl;

            // Expanded Smooth Enemy Animation Clips (Crimson Lancer)
            AnimationClip eIdle = CreateSpriteClip("Assets/Animations/Enemies/Lancer_Idle.anim",
                new Sprite[] { enemy[0], enemyExt[0], enemy[1], enemyExt[0] }, 4.2f, true);
            AnimationClip eWalk = CreateSpriteClip("Assets/Animations/Enemies/Lancer_Walk.anim",
                new Sprite[] { enemy[2], enemyExt[1], enemyExt[2], enemy[3], enemyExt[3], enemyExt[1] }, 9.0f, true);
            AnimationClip eWind = CreateSpriteClip("Assets/Animations/Enemies/Lancer_Windup.anim",
                new Sprite[] { enemy[4], enemyExt[4] }, 6.0f, false);
            AnimationClip eAtk = CreateSpriteClip("Assets/Animations/Enemies/Lancer_Attack.anim",
                new Sprite[] { enemyExt[5], enemy[5], enemyExt[6] }, 12.0f, false);
            AnimationClip eHurt = CreateSpriteClip("Assets/Animations/Enemies/Lancer_Hurt.anim",
                new Sprite[] { enemy[6], enemyExt[7] }, 10.0f, false);
            AnimationClip eDeath = CreateSpriteClip("Assets/Animations/Enemies/Lancer_Death.anim",
                new Sprite[] { enemy[6], enemyExt[7], enemy[7] }, 5.0f, false);

            string eCtrlPath = "Assets/Animations/Enemies/CrimsonLancerAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(eCtrlPath) != null) AssetDatabase.DeleteAsset(eCtrlPath);
            AnimatorController eCtrl = AnimatorController.CreateAnimatorControllerAtPath(eCtrlPath);
            eCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            eCtrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
            eCtrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            eCtrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
            eCtrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine esm = eCtrl.layers[0].stateMachine;
            AnimatorState eStIdle = esm.AddState("Idle"); eStIdle.motion = eIdle;
            AnimatorState eStWalk = esm.AddState("Walk"); eStWalk.motion = eWalk;
            AnimatorState eStWind = esm.AddState("Windup"); eStWind.motion = eWind;
            AnimatorState eStAtk = esm.AddState("Attack"); eStAtk.motion = eAtk;
            AnimatorState eStHurt = esm.AddState("Hurt"); eStHurt.motion = eHurt;
            AnimatorState eStDeath = esm.AddState("Death"); eStDeath.motion = eDeath;
            esm.defaultState = eStIdle;

            AddFastTrans(eStIdle, eStWalk).AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            AddFastTrans(eStWalk, eStIdle).AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            AddAnyTrans(esm, eStDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");

            AnimatorStateTransition eAnyHurt = AddAnyTrans(esm, eStHurt);
            eAnyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
            eAnyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(eStHurt, eStIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

            AnimatorStateTransition eAnyWind = AddAnyTrans(esm, eStWind);
            eAnyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
            eAnyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(eStWind, eStAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            AddFastTrans(eStWind, eStIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

            AnimatorStateTransition eAnyAtk = AddAnyTrans(esm, eStAtk);
            eAnyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            eAnyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(eStAtk, eStIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

            assets.EnemyAnimator = eCtrl;

            // Mossmir (Flying Bug) Clips & Animator Controller
            string mossmirSheet = "Assets/Art/Characters/Enemies/Mossmir_Sheet.png";
            Sprite[] mmIdleF = LoadSheetSprites(mossmirSheet, "MossmirIdle", 6);
            Sprite[] mmWindF = LoadSheetSprites(mossmirSheet, "MossmirWindup", 3);
            Sprite[] mmLungeF = LoadSheetSprites(mossmirSheet, "MossmirLunge", 4);
            Sprite[] mmHurtF = LoadSheetSprites(mossmirSheet, "MossmirHurt", 3);

            AnimationClip mmIdle = CreateSpriteClip("Assets/Animations/Enemies/Mossmir_Idle.anim", mmIdleF, 13.5f, true);
            AnimationClip mmWind = CreateSpriteClip("Assets/Animations/Enemies/Mossmir_Windup.anim",
                new Sprite[] { mmWindF[0], mmWindF[1], mmWindF[2], mmWindF[1] }, 11.0f, true);
            AnimationClip mmLunge = CreateSpriteClip("Assets/Animations/Enemies/Mossmir_Lunge.anim", mmLungeF, 12.5f, true);
            AnimationClip mmHurt = CreateSpriteClip("Assets/Animations/Enemies/Mossmir_Hurt.anim",
                new Sprite[] { mmHurtF[0], mmHurtF[1] }, 12.0f, false);
            AnimationClip mmDeath = CreateSpriteClip("Assets/Animations/Enemies/Mossmir_Death.anim", mmHurtF, 9.5f, false);

            string mmCtrlPath = "Assets/Animations/Enemies/MossmirAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(mmCtrlPath) != null) AssetDatabase.DeleteAsset(mmCtrlPath);
            AnimatorController mmCtrl = AnimatorController.CreateAnimatorControllerAtPath(mmCtrlPath);
            mmCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            mmCtrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
            mmCtrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            mmCtrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
            mmCtrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine mmsm = mmCtrl.layers[0].stateMachine;
            AnimatorState mmStIdle = mmsm.AddState("Idle"); mmStIdle.motion = mmIdle;
            AnimatorState mmStWind = mmsm.AddState("Windup"); mmStWind.motion = mmWind;
            AnimatorState mmStLunge = mmsm.AddState("Attack"); mmStLunge.motion = mmLunge;
            AnimatorState mmStHurt = mmsm.AddState("Hurt"); mmStHurt.motion = mmHurt;
            AnimatorState mmStDeath = mmsm.AddState("Death"); mmStDeath.motion = mmDeath;
            mmsm.defaultState = mmStIdle;

            AddAnyTrans(mmsm, mmStDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            AnimatorStateTransition mmAnyHurt = AddAnyTrans(mmsm, mmStHurt);
            mmAnyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
            mmAnyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(mmStHurt, mmStIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

            AnimatorStateTransition mmAnyWind = AddAnyTrans(mmsm, mmStWind);
            mmAnyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
            mmAnyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(mmStWind, mmStLunge).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            AddFastTrans(mmStWind, mmStIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

            AnimatorStateTransition mmAnyAtk = AddAnyTrans(mmsm, mmStLunge);
            mmAnyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
            mmAnyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(mmStLunge, mmStIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            assets.MossmirAnimator = mmCtrl;

            // Mossgrub (Spiky Ground Crawler) Clips & Animator Controller
            string mossgrubSheet = "Assets/Art/Characters/Enemies/Mossgrub_Sheet.png";
            Sprite[] mgPatrolF = LoadSheetSprites(mossgrubSheet, "MossgrubPatrol", 6);
            Sprite[] mgChargeF = LoadSheetSprites(mossgrubSheet, "MossgrubCharge", 4);
            Sprite[] mgTurnF = LoadSheetSprites(mossgrubSheet, "MossgrubTurn", 2);
            Sprite[] mgHurtF = LoadSheetSprites(mossgrubSheet, "MossgrubHurt", 3);

            AnimationClip mgPatrol = CreateSpriteClip("Assets/Animations/Enemies/Mossgrub_Patrol.anim", mgPatrolF, 13.0f, true);
            AnimationClip mgCharge = CreateSpriteClip("Assets/Animations/Enemies/Mossgrub_Charge.anim", mgChargeF, 16.0f, true);
            AnimationClip mgTurn = CreateSpriteClip("Assets/Animations/Enemies/Mossgrub_Turn.anim", mgTurnF, 14.0f, false);
            AnimationClip mgHurt = CreateSpriteClip("Assets/Animations/Enemies/Mossgrub_Hurt.anim",
                new Sprite[] { mgHurtF[0], mgHurtF[1] }, 12.0f, false);
            AnimationClip mgDeath = CreateSpriteClip("Assets/Animations/Enemies/Mossgrub_Death.anim", mgHurtF, 10.0f, false);

            string mgCtrlPath = "Assets/Animations/Enemies/MossgrubAnimator.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(mgCtrlPath) != null) AssetDatabase.DeleteAsset(mgCtrlPath);
            AnimatorController mgCtrl = AnimatorController.CreateAnimatorControllerAtPath(mgCtrlPath);
            mgCtrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            mgCtrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
            mgCtrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            mgCtrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
            mgCtrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
            mgCtrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
            mgCtrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine mgsm = mgCtrl.layers[0].stateMachine;
            AnimatorState mgStPatrol = mgsm.AddState("Patrol"); mgStPatrol.motion = mgPatrol;
            AnimatorState mgStCharge = mgsm.AddState("Charge"); mgStCharge.motion = mgCharge;
            AnimatorState mgStTurn = mgsm.AddState("Turn"); mgStTurn.motion = mgTurn;
            AnimatorState mgStHurt = mgsm.AddState("Hurt"); mgStHurt.motion = mgHurt;
            AnimatorState mgStDeath = mgsm.AddState("Death"); mgStDeath.motion = mgDeath;
            mgsm.defaultState = mgStPatrol;

            AddAnyTrans(mgsm, mgStDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            AnimatorStateTransition mgAnyHurt = AddAnyTrans(mgsm, mgStHurt);
            mgAnyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
            mgAnyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            AddFastTrans(mgStHurt, mgStPatrol).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

            AnimatorStateTransition mgAnyTurn = AddAnyTrans(mgsm, mgStTurn);
            mgAnyTurn.AddCondition(AnimatorConditionMode.If, 0, "IsTurning");
            mgAnyTurn.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            mgAnyTurn.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
            AddFastTrans(mgStTurn, mgStPatrol).AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");

            AddFastTrans(mgStPatrol, mgStCharge).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
            AddFastTrans(mgStCharge, mgStPatrol).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
            assets.MossgrubAnimator = mgCtrl;

            CreateAllBiomeEnemyAnimators(assets);
        }

        private static void CreateAllBiomeEnemyAnimators(BuiltAssets assets)
        {
            if (assets.BiomeEnemyAnimators == null)
            {
                assets.BiomeEnemyAnimators = new Dictionary<string, AnimatorController>();
            }
            assets.BiomeEnemyAnimators.Clear();

            for (int i = 0; i < AllBiomeEnemySheets.Length; i++)
            {
                BiomeSheetSpec s = AllBiomeEnemySheets[i];
                string sheetPath = "Assets/Art/Characters/Enemies/Sheets/" + s.Stem + "_Sheet.png";
                Sprite[] idleF = LoadSheetSprites(sheetPath, s.Stem + "_Idle", 12);
                Sprite[] moveF = LoadSheetSprites(sheetPath, s.Stem + "_Move", 12);
                Sprite[] atkF = LoadSheetSprites(sheetPath, s.Stem + "_Atk", 12);
                Sprite[] defF = LoadSheetSprites(sheetPath, s.Stem + "_Defeat", 12);
                if (idleF.Length < 12 || moveF.Length < 12 || atkF.Length < 12 || defF.Length < 12) continue;

                float idleFps = 15.0f, moveFps = 18.0f, windFps = 14.0f, atkFps = 18.0f, hurtFps = 16.0f, deathFps = 14.0f;
                ENEMY_COMBAT_CONFIG.Profile choralProfile;
                ENEMY_COMBAT_CONFIG.MossGrottoProfile mossProfile;
                ENEMY_COMBAT_CONFIG.KarakProfile karakProfile;
                ENEMY_COMBAT_CONFIG.MountFayProfile fayProfile;
                ENEMY_COMBAT_CONFIG.Batch2Profile batch2Profile;
                ENEMY_COMBAT_CONFIG.PilgrimProfile pilgrimProfile;
                if (ENEMY_COMBAT_CONFIG.TryGetByStem(s.Stem, out choralProfile))
                {
                    idleFps = choralProfile.IdleFps;
                    moveFps = choralProfile.MoveFps;
                    windFps = choralProfile.WindupFps;
                    atkFps = choralProfile.AttackFps;
                    hurtFps = choralProfile.HurtFps;
                    deathFps = choralProfile.DeathFps;
                }
                else if (ENEMY_COMBAT_CONFIG.TryGetMossByStem(s.Stem, out mossProfile))
                {
                    idleFps = mossProfile.IdleFps;
                    moveFps = mossProfile.MoveFps;
                    windFps = mossProfile.WindupFps;
                    atkFps = mossProfile.AttackFps;
                    hurtFps = mossProfile.HurtFps;
                    deathFps = mossProfile.DeathFps;
                }
                else if (ENEMY_COMBAT_CONFIG.TryGetKarakByStem(s.Stem, out karakProfile))
                {
                    idleFps = karakProfile.IdleFps;
                    moveFps = karakProfile.MoveFps;
                    windFps = karakProfile.WindupFps;
                    atkFps = karakProfile.AttackFps;
                    hurtFps = karakProfile.HurtFps;
                    deathFps = karakProfile.DeathFps;
                }
                else if (ENEMY_COMBAT_CONFIG.TryGetMountFayByStem(s.Stem, out fayProfile))
                {
                    idleFps = fayProfile.IdleFps;
                    moveFps = fayProfile.MoveFps;
                    windFps = fayProfile.WindupFps;
                    atkFps = fayProfile.AttackFps;
                    hurtFps = fayProfile.HurtFps;
                    deathFps = fayProfile.DeathFps;
                }
                else if (ENEMY_COMBAT_CONFIG.TryGetBatch2ByStem(s.Stem, out batch2Profile))
                {
                    idleFps = batch2Profile.IdleFps;
                    moveFps = batch2Profile.MoveFps;
                    windFps = batch2Profile.WindupFps;
                    atkFps = batch2Profile.AttackFps;
                    hurtFps = batch2Profile.HurtFps;
                    deathFps = batch2Profile.DeathFps;
                }
                else if (ENEMY_COMBAT_CONFIG.TryGetPilgrimByStem(s.Stem, out pilgrimProfile))
                {
                    idleFps = pilgrimProfile.IdleFps;
                    moveFps = pilgrimProfile.MoveFps;
                    windFps = pilgrimProfile.WindupFps;
                    atkFps = pilgrimProfile.AttackFps;
                    hurtFps = pilgrimProfile.HurtFps;
                    deathFps = pilgrimProfile.DeathFps;
                }

                AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/" + s.Stem + "_Idle.anim", idleF, idleFps, true);
                AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/" + s.Stem + "_Move.anim", moveF, moveFps, true);
                AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/" + s.Stem + "_Windup.anim", SliceSprites(atkF, 0, 4), windFps, true);
                AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/" + s.Stem + "_Attack.anim", SliceSprites(atkF, 4, 8), atkFps, true);
                AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/" + s.Stem + "_Hurt.anim", SliceSprites(defF, 0, 4), hurtFps, false);
                AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/" + s.Stem + "_Death.anim", SliceSprites(defF, 4, 8), deathFps, false);

                string ctrlPath = "Assets/Animations/Enemies/" + s.Stem + "Animator.controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                sm.defaultState = stIdle;

                AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                AnimatorStateTransition moveToIdle = AddFastTrans(stMove, stIdle);
                moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");
                moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");

                AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                AddFastTrans(stAtk, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

                assets.BiomeEnemyAnimators[s.Stem] = ctrl;
            }

            CreateMountFayHandDrawnAnimators(assets);
        }

        private static void CreateMountFayHandDrawnAnimators(BuiltAssets assets)
        {
            // 1. Servitor Boran (4x6 @ 256x256, 14-20 FPS): LanternScan / LaserCharge / LaserFire / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.MountFayProfile cfg = ENEMY_COMBAT_CONFIG.ServitorBoran;
                Sprite[] scanF = LoadSheetSprites(cfg.SpriteSheetPath, "servitor_boran_Scan", 6);
                Sprite[] chargeF = LoadSheetSprites(cfg.SpriteSheetPath, "servitor_boran_Charge", 6);
                Sprite[] fireF = LoadSheetSprites(cfg.SpriteSheetPath, "servitor_boran_Fire", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "servitor_boran_HDDefeat", 6);
                if (scanF != null && scanF.Length == 6 && chargeF != null && chargeF.Length == 6 &&
                    fireF != null && fireF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/servitor_boran_Idle.anim", scanF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/servitor_boran_Move.anim", scanF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/servitor_boran_Windup.anim", chargeF, cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/servitor_boran_Attack.anim", fireF, cfg.AttackFps, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/servitor_boran_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/servitor_boran_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/servitor_boranAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AnimatorStateTransition moveToIdle = AddFastTrans(stMove, stIdle);
                    moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");
                    moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

                    assets.BiomeEnemyAnimators["servitor_boran"] = ctrl;
                }
            }

            // 2. Driftlin (3x6 @ 192x192, 12-18 FPS): StationaryFloat / PogoBounce (via IsCharging) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.MountFayProfile cfg = ENEMY_COMBAT_CONFIG.Driftlin;
                Sprite[] floatF = LoadSheetSprites(cfg.SpriteSheetPath, "driftlin_Float", 6);
                Sprite[] pogoF = LoadSheetSprites(cfg.SpriteSheetPath, "driftlin_Pogo", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "driftlin_HDDefeat", 6);
                if (floatF != null && floatF.Length == 6 && pogoF != null && pogoF.Length == 6 &&
                    defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/driftlin_Idle.anim", floatF, cfg.IdleFps, true);
                    AnimationClip cPogo = CreateSpriteClip("Assets/Animations/Enemies/driftlin_Move.anim", pogoF, cfg.MoveFps, false);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/driftlin_Windup.anim", pogoF, cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/driftlin_Attack.anim", pogoF, cfg.AttackFps, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/driftlin_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/driftlin_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/driftlinAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cPogo;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    // Trigger PogoBounce strictly via IsCharging (set by MountFayEnemyAI.PogoBounceState)
                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

                    assets.BiomeEnemyAnimators["driftlin"] = ctrl;
                }
            }

            // 3. Mnemonid (4x6 @ 192x192, 14-20 FPS): DormantFloat / SpikeTriggered / SpikeActive (1-4 loop + 5-6 retract) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.MountFayProfile cfg = ENEMY_COMBAT_CONFIG.Mnemonid;
                Sprite[] dormF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonid_Dormant", 6);
                Sprite[] trigF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonid_Trigger", 6);
                Sprite[] spikeF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonid_Spike", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonid_HDDefeat", 6);
                if (dormF != null && dormF.Length == 6 && trigF != null && trigF.Length == 6 &&
                    spikeF != null && spikeF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Idle.anim", dormF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Move.anim", dormF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Windup.anim", trigF, cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Attack.anim", SliceSprites(spikeF, 0, 4), cfg.AttackFps, true);
                    AnimationClip cRetract = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Retract.anim", SliceSprites(spikeF, 4, 2), 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/mnemonid_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/mnemonidAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stRetract = sm.AddState("SpikeRetract"); stRetract.motion = cRetract;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    // Smooth 2-frame spike retraction before settling into DormantFloat
                    AddFastTrans(stAtk, stRetract).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition retractDone = stRetract.AddTransition(stIdle);
                    retractDone.hasExitTime = true;
                    retractDone.exitTime = 1.0f;
                    retractDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["mnemonid"] = ctrl;
                }
            }

            // 4. Mnemonord (4x6 @ 320x320, 12-18 FPS): DormantFloat / SpikeTriggered / SpikeActive (1-4 loop + 5-6 retract) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.MountFayProfile cfg = ENEMY_COMBAT_CONFIG.Mnemonord;
                Sprite[] dormF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonord_Dormant", 6);
                Sprite[] trigF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonord_Trigger", 6);
                Sprite[] spikeF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonord_Spike", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "mnemonord_HDDefeat", 6);
                if (dormF != null && dormF.Length == 6 && trigF != null && trigF.Length == 6 &&
                    spikeF != null && spikeF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Idle.anim", dormF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Move.anim", dormF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Windup.anim", trigF, cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Attack.anim", SliceSprites(spikeF, 0, 4), cfg.AttackFps, true);
                    AnimationClip cRetract = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Retract.anim", SliceSprites(spikeF, 4, 2), 14f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/mnemonord_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/mnemonordAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stRetract = sm.AddState("SpikeRetract"); stRetract.motion = cRetract;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stRetract).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition retractDone = stRetract.AddTransition(stIdle);
                    retractDone.hasExitTime = true;
                    retractDone.exitTime = 1.0f;
                    retractDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["mnemonord"] = ctrl;
                }
            }

            // 5. Drapemite (4x6 @ 192x192, 16-24 FPS): GroundPatrol (1-2 Idle, 3-6 Walk) / Crouch (1-2) & SlashLeap (3-6) / LandRecover (6f) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.Batch2Profile cfg = ENEMY_COMBAT_CONFIG.Drapemite;
                Sprite[] patrolF = LoadSheetSprites(cfg.SpriteSheetPath, "drapemite_Patrol", 6);
                Sprite[] leapF = LoadSheetSprites(cfg.SpriteSheetPath, "drapemite_Leap", 6);
                Sprite[] landF = LoadSheetSprites(cfg.SpriteSheetPath, "drapemite_Land", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "drapemite_HDDefeat", 6);
                if (patrolF != null && patrolF.Length == 6 && leapF != null && leapF.Length == 6 &&
                    landF != null && landF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/drapemite_Idle.anim", SliceSprites(patrolF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/drapemite_Move.anim", SliceSprites(patrolF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/drapemite_Windup.anim", SliceSprites(leapF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/drapemite_Attack.anim", SliceSprites(leapF, 2, 4), cfg.AttackFps, false);
                    AnimationClip cLand = CreateSpriteClip("Assets/Animations/Enemies/drapemite_LandRecover.anim", landF, 18f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/drapemite_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/drapemite_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/drapemiteAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stLand = sm.AddState("LandRecover"); stLand.motion = cLand;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    // Plays full 6-frame Row 3 LandRecover on landing before settling into Idle/Patrol
                    AddFastTrans(stAtk, stLand).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition landDone = stLand.AddTransition(stIdle);
                    landDone.hasExitTime = true;
                    landDone.exitTime = 1.0f;
                    landDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["drapemite"] = ctrl;
                }
            }

            // 6. Drapefly (3x6 @ 192x192, 16-24 FPS): AirHover (6f) / SweepWindup (1-2) + SideToSideSweep (3-5) + Airbrake (6) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.Batch2Profile cfg = ENEMY_COMBAT_CONFIG.Drapefly;
                Sprite[] hoverF = LoadSheetSprites(cfg.SpriteSheetPath, "drapefly_Hover", 6);
                Sprite[] sweepF = LoadSheetSprites(cfg.SpriteSheetPath, "drapefly_Sweep", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "drapefly_HDDefeat", 6);
                if (hoverF != null && hoverF.Length == 6 && sweepF != null && sweepF.Length == 6 &&
                    defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/drapefly_Idle.anim", hoverF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/drapefly_Move.anim", hoverF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/drapefly_Windup.anim", SliceSprites(sweepF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/drapefly_Attack.anim", SliceSprites(sweepF, 2, 3), cfg.AttackFps, true);
                    AnimationClip cBrake = CreateSpriteClip("Assets/Animations/Enemies/drapefly_BrakeTurn.anim", SliceSprites(sweepF, 5, 1), 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/drapefly_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/drapefly_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/drapeflyAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stBrake = sm.AddState("BrakeTurn"); stBrake.motion = cBrake;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    // Plays Row 2 Frame 6 aerodynamic airbrake before settling into AirHover
                    AddFastTrans(stAtk, stBrake).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition brakeDone = stBrake.AddTransition(stIdle);
                    brakeDone.hasExitTime = true;
                    brakeDone.exitTime = 1.0f;
                    brakeDone.duration = 0.04f;

                    assets.BiomeEnemyAnimators["drapefly"] = ctrl;
                }
            }

            // 7. Pondcatcher (4x6 @ 256x256, 14-20 FPS): HuntIdle (1-2) + Step (3-6) / SpearThrust (R2) / DownwardSpearPoke (R3 via IsCharging) / Hurt & Death (R4)
            {
                ENEMY_COMBAT_CONFIG.Batch2Profile cfg = ENEMY_COMBAT_CONFIG.Pondcatcher;
                Sprite[] huntF = LoadSheetSprites(cfg.SpriteSheetPath, "pondcatcher_Hunt", 6);
                Sprite[] thrustF = LoadSheetSprites(cfg.SpriteSheetPath, "pondcatcher_Thrust", 6);
                Sprite[] downF = LoadSheetSprites(cfg.SpriteSheetPath, "pondcatcher_DownPoke", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "pondcatcher_HDDefeat", 6);
                if (huntF != null && huntF.Length == 6 && thrustF != null && thrustF.Length == 6 &&
                    downF != null && downF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_Idle.anim", SliceSprites(huntF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_Move.anim", SliceSprites(huntF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_Windup.anim", SliceSprites(thrustF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_Attack.anim", SliceSprites(thrustF, 2, 2), cfg.AttackFps, false);
                    AnimationClip cRec = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_ThrustRecover.anim", SliceSprites(thrustF, 4, 2), 16f, false);
                    AnimationClip cDownIdle = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_DownPokeFull.anim", downF, 16f, false);
                    AnimationClip cDownWind = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_DownWindup.anim", SliceSprites(downF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cDownAtk = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_DownAttack.anim", SliceSprites(downF, 2, 2), cfg.AttackFps, false);
                    AnimationClip cDownRec = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_DownRecover.anim", SliceSprites(downF, 4, 2), 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/pondcatcher_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/pondcatcherAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stRec = sm.AddState("ThrustRecover"); stRec.motion = cRec;
                    AnimatorState stDownFull = sm.AddState("IdleDownPoke"); stDownFull.motion = cDownIdle;
                    AnimatorState stDownWind = sm.AddState("DownWindup"); stDownWind.motion = cDownWind;
                    AnimatorState stDownAtk = sm.AddState("DownAttack"); stDownAtk.motion = cDownAtk;
                    AnimatorState stDownRec = sm.AddState("DownRecover"); stDownRec.motion = cDownRec;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");
                    // Periodic idle pond-fishing downward poke (IsCharging == true while Idle)
                    AnimatorStateTransition idleToDown = AddFastTrans(stIdle, stDownFull);
                    idleToDown.AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    idleToDown.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");
                    idleToDown.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AddFastTrans(stDownFull, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    // Horizontal Spear Windup -> Thrust -> Recover (IsCharging == false)
                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stRec).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition recDone = stRec.AddTransition(stIdle);
                    recDone.hasExitTime = true; recDone.exitTime = 1.0f; recDone.duration = 0.02f;

                    // Diagonal Downward Spear Windup -> DownPoke -> Recover (IsCharging == true)
                    AnimatorStateTransition anyDWind = AddAnyTrans(sm, stDownWind);
                    anyDWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyDWind.AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    anyDWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyDWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stDownWind, stDownAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stDownWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyDAtk = AddAnyTrans(sm, stDownAtk);
                    anyDAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyDAtk.AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    anyDAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyDAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stDownAtk, stDownRec).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition dRecDone = stDownRec.AddTransition(stIdle);
                    dRecDone.hasExitTime = true; dRecDone.exitTime = 1.0f; dRecDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["pondcatcher"] = ctrl;
                }
            }

            // 8. Furm (4x6 @ 192x192, 16-24 FPS): Burrowed (R1 1-3) -> EruptWarning (R1 4-6) -> FastPopJump (R2 1-6) -> Vulnerable (R3 1-3) -> Reburrow (R3 4-6 via IsCharging) -> Hurt & Death (R4)
            {
                ENEMY_COMBAT_CONFIG.Batch2Profile cfg = ENEMY_COMBAT_CONFIG.Furm;
                Sprite[] burF = LoadSheetSprites(cfg.SpriteSheetPath, "furm_Burrow", 6);
                Sprite[] popF = LoadSheetSprites(cfg.SpriteSheetPath, "furm_PopJump", 6);
                Sprite[] vulF = LoadSheetSprites(cfg.SpriteSheetPath, "furm_VulnReburrow", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "furm_HDDefeat", 6);
                if (burF != null && burF.Length == 6 && popF != null && popF.Length == 6 &&
                    vulF != null && vulF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/furm_Idle.anim", SliceSprites(burF, 0, 3), cfg.IdleFps, true);
                    AnimationClip cReburrow = CreateSpriteClip("Assets/Animations/Enemies/furm_Move.anim", SliceSprites(vulF, 3, 3), cfg.MoveFps, false);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/furm_Windup.anim", SliceSprites(burF, 3, 3), cfg.WindupFps, true);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/furm_Attack.anim", popF, cfg.AttackFps, false);
                    AnimationClip cVuln = CreateSpriteClip("Assets/Animations/Enemies/furm_Vulnerable.anim", SliceSprites(vulF, 0, 3), 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/furm_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/furm_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/furmAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cReburrow;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stVuln = sm.AddState("Vulnerable"); stVuln.motion = cVuln;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    // Reburrow triggered cleanly via IsCharging from BurrowDownState!
                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stVuln, stMove).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stMove).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AnimatorStateTransition hurtToVuln = AddFastTrans(stHurt, stVuln);
                    hurtToVuln.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    hurtToVuln.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    // Lands cleanly into VulnerableWindow (Row 3 Frames 1-3) until BurrowDown sets IsCharging!
                    AddFastTrans(stAtk, stVuln).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

                    assets.BiomeEnemyAnimators["furm"] = ctrl;
                }
            }

            // 9. Winged Furm (3x6 @ 192x192, 14-20 FPS): VerticalPatrol (R1 6f) / HelmetClang (R2 1-2) -> DizzyLoop (R2 3-6 via IsCharging) / Hurt & Death (R3)
            {
                ENEMY_COMBAT_CONFIG.Batch2Profile cfg = ENEMY_COMBAT_CONFIG.WingedFurm;
                Sprite[] patF = LoadSheetSprites(cfg.SpriteSheetPath, "winged_furm_Patrol", 6);
                Sprite[] dizF = LoadSheetSprites(cfg.SpriteSheetPath, "winged_furm_Dizzy", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "winged_furm_HDDefeat", 6);
                if (patF != null && patF.Length == 6 && dizF != null && dizF.Length == 6 &&
                    defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/winged_furm_Idle.anim", patF, cfg.IdleFps, true);
                    AnimationClip cClang = CreateSpriteClip("Assets/Animations/Enemies/winged_furm_Move.anim", SliceSprites(dizF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cDizzy = CreateSpriteClip("Assets/Animations/Enemies/winged_furm_DizzyLoop.anim", SliceSprites(dizF, 2, 4), cfg.AttackFps, true);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/winged_furm_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/winged_furm_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/winged_furmAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stClang = sm.AddState("HelmetClang"); stClang.motion = cClang;
                    AnimatorState stDizzy = sm.AddState("DizzyLoop"); stDizzy.motion = cDizzy;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    // HelmetClangDizzyState sets IsCharging = true -> plays 2-frame Clang then loops 4-frame Dizzy!
                    AddFastTrans(stIdle, stClang).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AnimatorStateTransition clangToDizzy = stClang.AddTransition(stDizzy);
                    clangToDizzy.hasExitTime = true;
                    clangToDizzy.exitTime = 1.0f;
                    clangToDizzy.duration = 0.02f;
                    AddFastTrans(stClang, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
                    AddFastTrans(stDizzy, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    assets.BiomeEnemyAnimators["winged_furm"] = ctrl;
                }
            }

            // 10. Pilgrim Groveller (4x6 @ 192x192, 14-20 FPS): SlowWalkPatrol (1-2 Idle, 3-6 Walk) / JumpTelegraph (1-2) & LungingHop (3-6) / LandPause (6f) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.PilgrimProfile cfg = ENEMY_COMBAT_CONFIG.PilgrimGroveller;
                Sprite[] walkF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_groveller_Walk", 6);
                Sprite[] hopF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_groveller_Hop", 6);
                Sprite[] landF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_groveller_Land", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_groveller_HDDefeat", 6);
                if (walkF != null && walkF.Length == 6 && hopF != null && hopF.Length == 6 &&
                    landF != null && landF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_Idle.anim", SliceSprites(walkF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_Move.anim", SliceSprites(walkF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_Windup.anim", SliceSprites(hopF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_Attack.anim", SliceSprites(hopF, 2, 4), cfg.AttackFps, false);
                    AnimationClip cLand = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_LandPause.anim", landF, 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_groveller_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/pilgrim_grovellerAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stLand = sm.AddState("LandPause"); stLand.motion = cLand;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stLand).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition landDone = stLand.AddTransition(stIdle);
                    landDone.hasExitTime = true; landDone.exitTime = 1.0f; landDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["pilgrim_groveller"] = ctrl;
                }
            }

            // 11. Winged Pilgrim (3x6 @ 192x192, 16-22 FPS): AirCirclePatrol (6f) / DiveTelegraph (1-2), BodyThrowDive (3-4), BrakeAndRise (5-6) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.PilgrimProfile cfg = ENEMY_COMBAT_CONFIG.WingedPilgrim;
                Sprite[] patF = LoadSheetSprites(cfg.SpriteSheetPath, "winged_pilgrim_Patrol", 6);
                Sprite[] diveF = LoadSheetSprites(cfg.SpriteSheetPath, "winged_pilgrim_Dive", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "winged_pilgrim_HDDefeat", 6);
                if (patF != null && patF.Length == 6 && diveF != null && diveF.Length == 6 &&
                    defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_Idle.anim", patF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_Move.anim", patF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_Windup.anim", SliceSprites(diveF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_Attack.anim", SliceSprites(diveF, 2, 2), cfg.AttackFps, true);
                    AnimationClip cBrake = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_BrakeRise.anim", SliceSprites(diveF, 4, 2), 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/winged_pilgrim_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/winged_pilgrimAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stBrake = sm.AddState("BrakeAndRise"); stBrake.motion = cBrake;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stBrake).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition brakeDone = stBrake.AddTransition(stIdle);
                    brakeDone.hasExitTime = true; brakeDone.exitTime = 1.0f; brakeDone.duration = 0.03f;

                    assets.BiomeEnemyAnimators["winged_pilgrim"] = ctrl;
                }
            }

            // 12. Pilgrim Bellbearer (4x6 @ 256x256, 14-20 FPS): SpacingWalk (1-2 Idle, 3-6 Walk) / ThrowBell (R2 1-2 Windup, 3-6 Throw+Follow) / ReloadBell (R3 1-3) + RetreatStep (R3 4-6 via IsCharging) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.PilgrimProfile cfg = ENEMY_COMBAT_CONFIG.PilgrimBellbearer;
                Sprite[] walkF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_bellbearer_Walk", 6);
                Sprite[] throwF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_bellbearer_Throw", 6);
                Sprite[] relF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_bellbearer_Reload", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_bellbearer_HDDefeat", 6);
                if (walkF != null && walkF.Length == 6 && throwF != null && throwF.Length == 6 &&
                    relF != null && relF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_Idle.anim", SliceSprites(walkF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_Move.anim", SliceSprites(walkF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_Windup.anim", SliceSprites(throwF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_Attack.anim", SliceSprites(throwF, 2, 4), cfg.AttackFps, false);
                    AnimationClip cReload = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_ReloadBell.anim", SliceSprites(relF, 0, 3), 14f, false);
                    AnimationClip cRetreat = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_RetreatStep.anim", SliceSprites(relF, 3, 3), 16f, true);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_bellbearer_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/pilgrim_bellbearerAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stRetreat = sm.AddState("RetreatStep"); stRetreat.motion = cRetreat;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stReload = sm.AddState("ReloadBell"); stReload.motion = cReload;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AnimatorStateTransition toWalk = AddFastTrans(stIdle, stMove);
                    toWalk.AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    toWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");
                    AddFastTrans(stIdle, stRetreat).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stMove, stRetreat).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stRetreat, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    // Draws fresh bell from waist pouch right after throwing!
                    AddFastTrans(stAtk, stReload).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition reloadDone = stReload.AddTransition(stIdle);
                    reloadDone.hasExitTime = true; reloadDone.exitTime = 1.0f; reloadDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["pilgrim_bellbearer"] = ctrl;
                }
            }

            // 13. Covetous Pilgrim (5x6 @ 256x256, 14-20 FPS): MarchPatrol (R1) / ChainSwing (R2) / HeavyStepSlam (R3 via IsCharging) / BackstepGuard (R4 via IsTurning) / Hurt & Death (R5)
            {
                ENEMY_COMBAT_CONFIG.PilgrimProfile cfg = ENEMY_COMBAT_CONFIG.CovetousPilgrim;
                Sprite[] marchF = LoadSheetSprites(cfg.SpriteSheetPath, "covetous_pilgrim_March", 6);
                Sprite[] swingF = LoadSheetSprites(cfg.SpriteSheetPath, "covetous_pilgrim_Swing", 6);
                Sprite[] slamF = LoadSheetSprites(cfg.SpriteSheetPath, "covetous_pilgrim_Slam", 6);
                Sprite[] guardF = LoadSheetSprites(cfg.SpriteSheetPath, "covetous_pilgrim_Guard", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "covetous_pilgrim_HDDefeat", 6);
                if (marchF != null && marchF.Length == 6 && swingF != null && swingF.Length == 6 &&
                    slamF != null && slamF.Length == 6 && guardF != null && guardF.Length == 6 &&
                    defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Idle.anim", SliceSprites(marchF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Move.anim", SliceSprites(marchF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cSwingWind = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Windup.anim", SliceSprites(swingF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cSwingAtk = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Attack.anim", SliceSprites(swingF, 2, 2), cfg.AttackFps, false);
                    AnimationClip cSwingRec = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_SwingRecover.anim", SliceSprites(swingF, 4, 2), 16f, false);
                    AnimationClip cSlamWind = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_SlamWindup.anim", SliceSprites(slamF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cSlamAtk = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_SlamAttack.anim", SliceSprites(slamF, 2, 2), cfg.AttackFps, false);
                    AnimationClip cSlamRec = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_SlamRecover.anim", SliceSprites(slamF, 4, 2), 16f, false);
                    AnimationClip cBackstep = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Backstep.anim", SliceSprites(guardF, 0, 3), 18f, false);
                    AnimationClip cGuard = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_GuardStance.anim", SliceSprites(guardF, 3, 3), 14f, true);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/covetous_pilgrim_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/covetous_pilgrimAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stSwingWind = sm.AddState("Windup"); stSwingWind.motion = cSwingWind;
                    AnimatorState stSwingAtk = sm.AddState("Attack"); stSwingAtk.motion = cSwingAtk;
                    AnimatorState stSwingRec = sm.AddState("SwingRecover"); stSwingRec.motion = cSwingRec;
                    AnimatorState stSlamWind = sm.AddState("SlamWindup"); stSlamWind.motion = cSlamWind;
                    AnimatorState stSlamAtk = sm.AddState("SlamAttack"); stSlamAtk.motion = cSlamAtk;
                    AnimatorState stSlamRec = sm.AddState("SlamRecover"); stSlamRec.motion = cSlamRec;
                    AnimatorState stBackstep = sm.AddState("Backstep"); stBackstep.motion = cBackstep;
                    AnimatorState stGuard = sm.AddState("GuardStance"); stGuard.motion = cGuard;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    // Row 2: ChainSwing (IsCharging == false, IsTurning == false)
                    AnimatorStateTransition anySW = AddAnyTrans(sm, stSwingWind);
                    anySW.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anySW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
                    anySW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anySW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anySW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stSwingWind, stSwingAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stSwingWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anySA = AddAnyTrans(sm, stSwingAtk);
                    anySA.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anySA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");
                    anySA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anySA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anySA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stSwingAtk, stSwingRec).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition srDone = stSwingRec.AddTransition(stIdle);
                    srDone.hasExitTime = true; srDone.exitTime = 1.0f; srDone.duration = 0.02f;

                    // Row 3: HeavyStepSlam (IsCharging == true, IsTurning == false)
                    AnimatorStateTransition anyHW = AddAnyTrans(sm, stSlamWind);
                    anyHW.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyHW.AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    anyHW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anyHW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyHW.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stSlamWind, stSlamAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stSlamWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyHA = AddAnyTrans(sm, stSlamAtk);
                    anyHA.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyHA.AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    anyHA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anyHA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyHA.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stSlamAtk, stSlamRec).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition hrDone = stSlamRec.AddTransition(stIdle);
                    hrDone.hasExitTime = true; hrDone.exitTime = 1.0f; hrDone.duration = 0.02f;

                    // Row 4: BackstepGuard (IsTurning == true -> Backstep 1-3 -> GuardStance 4-6!)
                    AnimatorStateTransition anyBG = AddAnyTrans(sm, stBackstep);
                    anyBG.AddCondition(AnimatorConditionMode.If, 0, "IsTurning");
                    anyBG.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyBG.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyBG.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stBackstep, stGuard).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AddFastTrans(stGuard, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");

                    assets.BiomeEnemyAnimators["covetous_pilgrim"] = ctrl;
                }
            }

            // 14. Pilgrim Hulk (4x6 @ 320x320, 14-22 FPS): HeavyWalk (R1) / RollCurl (1-2) & 360-deg RollingCharge (3-6) / WallBonk (1-2 via IsTurning) & UncurlRecover (3-6) / Hurt & Death
            {
                ENEMY_COMBAT_CONFIG.PilgrimProfile cfg = ENEMY_COMBAT_CONFIG.PilgrimHulk;
                Sprite[] walkF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_hulk_Walk", 6);
                Sprite[] rollF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_hulk_Roll", 6);
                Sprite[] bonkF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_hulk_Bonk", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "pilgrim_hulk_HDDefeat", 6);
                if (walkF != null && walkF.Length == 6 && rollF != null && rollF.Length == 6 &&
                    bonkF != null && bonkF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_Idle.anim", SliceSprites(walkF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_Move.anim", SliceSprites(walkF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_Windup.anim", SliceSprites(rollF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_Attack.anim", SliceSprites(rollF, 2, 4), cfg.AttackFps, true);
                    AnimationClip cBonk = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_WallBonk.anim", SliceSprites(bonkF, 0, 2), 16f, false);
                    AnimationClip cUncurl = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_UncurlRecover.anim", SliceSprites(bonkF, 2, 4), 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/pilgrim_hulk_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/pilgrim_hulkAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stBonk = sm.AddState("WallBonk"); stBonk.motion = cBonk;
                    AnimatorState stUncurl = sm.AddState("UncurlRecover"); stUncurl.motion = cUncurl;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    // Wall impact -> WallBonk (R3 F1-2) -> UncurlRecover (R3 F3-6) -> Idle!
                    AnimatorStateTransition atkToBonk = AddFastTrans(stAtk, stBonk);
                    atkToBonk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    atkToBonk.AddCondition(AnimatorConditionMode.If, 0, "IsTurning");
                    AnimatorStateTransition bonkToUncurl = stBonk.AddTransition(stUncurl);
                    bonkToUncurl.hasExitTime = true; bonkToUncurl.exitTime = 1.0f; bonkToUncurl.duration = 0.02f;

                    // Clean stop without wall bonk -> directly into UncurlRecover (R3 F3-6) -> Idle!
                    AnimatorStateTransition atkToUncurl = AddFastTrans(stAtk, stUncurl);
                    atkToUncurl.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    atkToUncurl.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    AnimatorStateTransition uncurlDone = stUncurl.AddTransition(stIdle);
                    uncurlDone.hasExitTime = true; uncurlDone.exitTime = 1.0f; uncurlDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["pilgrim_hulk"] = ctrl;
                }
            }

            // 15. Driznit (4x6 @ 192x192, 16-22 FPS): HoverSpacing (R1) / ThrowWindup (1-2), ThrowBouncer (3-4), FollowThrough (5-6) / RepositionRetreat (R3 via IsCharging) / Hurt & Death (R4)
            {
                ENEMY_COMBAT_CONFIG.KarakProfile cfg = ENEMY_COMBAT_CONFIG.Driznit;
                Sprite[] hovF = LoadSheetSprites(cfg.SpriteSheetPath, "driznit_Hover", 6);
                Sprite[] thrF = LoadSheetSprites(cfg.SpriteSheetPath, "driznit_Throw", 6);
                Sprite[] retF = LoadSheetSprites(cfg.SpriteSheetPath, "driznit_Retreat", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "driznit_HDDefeat", 6);
                if (hovF != null && hovF.Length == 6 && thrF != null && thrF.Length == 6 &&
                    retF != null && retF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/driznit_Idle.anim", hovF, cfg.IdleFps, true);
                    AnimationClip cRetreat = CreateSpriteClip("Assets/Animations/Enemies/driznit_Move.anim", retF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/driznit_Windup.anim", SliceSprites(thrF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/driznit_Attack.anim", SliceSprites(thrF, 2, 2), cfg.AttackFps, false);
                    AnimationClip cFollow = CreateSpriteClip("Assets/Animations/Enemies/driznit_ThrowFollow.anim", SliceSprites(thrF, 4, 2), 18f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/driznit_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/driznit_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/driznitAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stRetreat = sm.AddState("RepositionRetreat"); stRetreat.motion = cRetreat;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stFollow = sm.AddState("ThrowFollow"); stFollow.motion = cFollow;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    // Evasive angled backward retreat driven by IsCharging in KarakEnemyAI!
                    AddFastTrans(stIdle, stRetreat).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stRetreat, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stFollow).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition fDone = stFollow.AddTransition(stIdle);
                    fDone.hasExitTime = true; fDone.exitTime = 1.0f; fDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["driznit"] = ctrl;
                }
            }

            // 16. Conchfly (3x6 @ 192x192, 18-24 FPS): AirPatrol (R1) / AimTelegraph (1-2), FastDiagonalDart (3-4), BrakeAndRecover (5-6) / Hurt & Death (R3)
            {
                ENEMY_COMBAT_CONFIG.KarakProfile cfg = ENEMY_COMBAT_CONFIG.Conchfly;
                Sprite[] patF = LoadSheetSprites(cfg.SpriteSheetPath, "conchfly_Patrol", 6);
                Sprite[] dartF = LoadSheetSprites(cfg.SpriteSheetPath, "conchfly_Dart", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "conchfly_HDDefeat", 6);
                if (patF != null && patF.Length == 6 && dartF != null && dartF.Length == 6 &&
                    defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/conchfly_Idle.anim", patF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/conchfly_Move.anim", patF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/conchfly_Windup.anim", SliceSprites(dartF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/conchfly_Attack.anim", SliceSprites(dartF, 2, 2), cfg.AttackFps, true);
                    AnimationClip cBrake = CreateSpriteClip("Assets/Animations/Enemies/conchfly_BrakeRecover.anim", SliceSprites(dartF, 4, 2), 18f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/conchfly_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/conchfly_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/conchflyAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stBrake = sm.AddState("BrakeRecover"); stBrake.motion = cBrake;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stBrake).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition bDone = stBrake.AddTransition(stIdle);
                    bDone.hasExitTime = true; bDone.exitTime = 1.0f; bDone.duration = 0.03f;

                    assets.BiomeEnemyAnimators["conchfly"] = ctrl;
                }
            }

            // 17. Pharlid (4x6 @ 192x192, 18-24 FPS): SkitterPatrol (R1) / Lunge (R2) / CeilingCling & AmbushDrop (R3 via IsTurning) / Hurt & Death (R4)
            {
                ENEMY_COMBAT_CONFIG.KarakProfile cfg = ENEMY_COMBAT_CONFIG.Pharlid;
                Sprite[] skitF = LoadSheetSprites(cfg.SpriteSheetPath, "pharlid_Skitter", 6);
                Sprite[] lungF = LoadSheetSprites(cfg.SpriteSheetPath, "pharlid_Lunge", 6);
                Sprite[] ambF = LoadSheetSprites(cfg.SpriteSheetPath, "pharlid_Ambush", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "pharlid_HDDefeat", 6);
                if (skitF != null && skitF.Length == 6 && lungF != null && lungF.Length == 6 &&
                    ambF != null && ambF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/pharlid_Idle.anim", SliceSprites(skitF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/pharlid_Move.anim", SliceSprites(skitF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/pharlid_Windup.anim", SliceSprites(lungF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/pharlid_Attack.anim", SliceSprites(lungF, 2, 2), cfg.AttackFps, false);
                    AnimationClip cPivot = CreateSpriteClip("Assets/Animations/Enemies/pharlid_LungePivot.anim", SliceSprites(lungF, 4, 2), 20f, false);
                    AnimationClip cCling = CreateSpriteClip("Assets/Animations/Enemies/pharlid_CeilingCling.anim", SliceSprites(ambF, 0, 2), 18f, true);
                    AnimationClip cDrop = CreateSpriteClip("Assets/Animations/Enemies/pharlid_AmbushDrop.anim", SliceSprites(ambF, 2, 3), 22f, true);
                    AnimationClip cDropLand = CreateSpriteClip("Assets/Animations/Enemies/pharlid_AmbushLand.anim", SliceSprites(ambF, 5, 1), 18f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/pharlid_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/pharlid_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/pharlidAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stPivot = sm.AddState("LungePivot"); stPivot.motion = cPivot;
                    AnimatorState stCling = sm.AddState("CeilingCling"); stCling.motion = cCling;
                    AnimatorState stDrop = sm.AddState("AmbushDrop"); stDrop.motion = cDrop;
                    AnimatorState stDropLand = sm.AddState("AmbushLand"); stDropLand.motion = cDropLand;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    // Row 3: CeilingCling & AmbushDrop (driven by IsTurning == true!)
                    AnimatorStateTransition anyCling = AddAnyTrans(sm, stCling);
                    anyCling.AddCondition(AnimatorConditionMode.If, 0, "IsTurning");
                    anyCling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    anyCling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyCling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stCling, stDrop).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stCling, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    AddFastTrans(stDrop, stDropLand).AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    AnimatorStateTransition dlDone = stDropLand.AddTransition(stIdle);
                    dlDone.hasExitTime = true; dlDone.exitTime = 1.0f; dlDone.duration = 0.02f;

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    // Row 2: Ground LungeWindup -> QuickLunge -> LungePivot (IsTurning == false)
                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stPivot).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition pDone = stPivot.AddTransition(stIdle);
                    pDone.hasExitTime = true; pDone.exitTime = 1.0f; pDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["pharlid"] = ctrl;
                }
            }

            // 18. Coral Furm (4x6 @ 192x192, 14-20 FPS): CrawlPatrol (R1) / CoralCharge + SkidBrake (R2) / ShellBrace (R3 on hit) / Hurt & Shatter Death (R4)
            {
                ENEMY_COMBAT_CONFIG.KarakProfile cfg = ENEMY_COMBAT_CONFIG.CoralFurm;
                Sprite[] crwF = LoadSheetSprites(cfg.SpriteSheetPath, "coral_furm_Crawl", 6);
                Sprite[] chgF = LoadSheetSprites(cfg.SpriteSheetPath, "coral_furm_Charge", 6);
                Sprite[] brcF = LoadSheetSprites(cfg.SpriteSheetPath, "coral_furm_Brace", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "coral_furm_HDDefeat", 6);
                if (crwF != null && crwF.Length == 6 && chgF != null && chgF.Length == 6 &&
                    brcF != null && brcF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_Idle.anim", SliceSprites(crwF, 0, 2), cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_Move.anim", SliceSprites(crwF, 2, 4), cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_Windup.anim", SliceSprites(chgF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cAtk = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_Attack.anim", SliceSprites(chgF, 2, 3), cfg.AttackFps, true);
                    AnimationClip cSkid = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_SkidBrake.anim", SliceSprites(chgF, 5, 1), 16f, false);
                    AnimationClip cBrace = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_ShellBrace.anim", brcF, 16f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/coral_furm_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/coral_furmAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cAtk;
                    AnimatorState stSkid = sm.AddState("SkidBrake"); stSkid.motion = cSkid;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stBrace = sm.AddState("ShellBrace"); stBrace.motion = cBrace;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    // On every hit: crisp cracked flinch (R4 F1-2) -> immediately tucks & emerges in ShellBrace (R3 F1-6)!
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AnimatorStateTransition hurtToBrace = stHurt.AddTransition(stBrace);
                    hurtToBrace.hasExitTime = true; hurtToBrace.exitTime = 1.0f; hurtToBrace.duration = 0.02f;
                    AnimatorStateTransition braceDone = stBrace.AddTransition(stIdle);
                    braceDone.hasExitTime = true; braceDone.exitTime = 1.0f; braceDone.duration = 0.02f;

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stSkid).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                    AnimatorStateTransition skDone = stSkid.AddTransition(stIdle);
                    skDone.hasExitTime = true; skDone.exitTime = 1.0f; skDone.duration = 0.02f;

                    assets.BiomeEnemyAnimators["coral_furm"] = ctrl;
                }
            }

            // 19. Moss Grub (3x6 @ 192x192, 12-18 FPS): PassivePatrol (R1) / ProvokedAlert + ProvokedCharge (R2) / Hurt & Death (R3)
            {
                ENEMY_COMBAT_CONFIG.MossGrottoProfile cfg = ENEMY_COMBAT_CONFIG.MossGrub;
                Sprite[] patF = LoadSheetSprites(cfg.SpriteSheetPath, "moss_grub_Patrol", 6);
                Sprite[] prvF = LoadSheetSprites(cfg.SpriteSheetPath, "moss_grub_Provoked", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "moss_grub_HDDefeat", 6);
                if (patF != null && patF.Length == 6 && prvF != null && prvF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cPatrol = CreateSpriteClip("Assets/Animations/Enemies/moss_grub_Patrol.anim", patF, cfg.MoveFps, true);
                    AnimationClip cAlert = CreateSpriteClip("Assets/Animations/Enemies/moss_grub_ProvokedAlert.anim", SliceSprites(prvF, 0, 2), cfg.WindupFps, false);
                    AnimationClip cCharge = CreateSpriteClip("Assets/Animations/Enemies/moss_grub_ProvokedCharge.anim", SliceSprites(prvF, 2, 4), cfg.AttackFps, true);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/moss_grub_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/moss_grub_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/MossgrubAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsCharging", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stPatrol = sm.AddState("Patrol"); stPatrol.motion = cPatrol;
                    AnimatorState stAlert = sm.AddState("ProvokedAlert"); stAlert.motion = cAlert;
                    AnimatorState stCharge = sm.AddState("ProvokedCharge"); stCharge.motion = cCharge;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stPatrol;

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stAlert).AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    AddFastTrans(stHurt, stPatrol).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyAlert = AddAnyTrans(sm, stAlert);
                    anyAlert.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyAlert.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAlert.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAlert, stCharge).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stAlert, stPatrol).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AddFastTrans(stPatrol, stCharge).AddCondition(AnimatorConditionMode.If, 0, "IsCharging");
                    AddFastTrans(stCharge, stPatrol).AddCondition(AnimatorConditionMode.IfNot, 0, "IsCharging");

                    assets.MossgrubAnimator = ctrl;
                    assets.BiomeEnemyAnimators["bellhart_mossgrub"] = ctrl;
                }
            }

            // 20. Mossmir (3x6 @ 192x192, 16-22 FPS): HoverPatrol (R1) / PogoRecoil + LightRetaliation (R2) / Hurt & Death (R3)
            {
                ENEMY_COMBAT_CONFIG.MossGrottoProfile cfg = ENEMY_COMBAT_CONFIG.Mossmir;
                Sprite[] hovF = LoadSheetSprites(cfg.SpriteSheetPath, "mossmir_Hover", 6);
                Sprite[] actF = LoadSheetSprites(cfg.SpriteSheetPath, "mossmir_Action", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "mossmir_HDDefeat", 6);
                if (hovF != null && hovF.Length == 6 && actF != null && actF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cHover = CreateSpriteClip("Assets/Animations/Enemies/mossmir_Hover.anim", hovF, cfg.IdleFps, true);
                    AnimationClip cPogo = CreateSpriteClip("Assets/Animations/Enemies/mossmir_PogoRecoil.anim", SliceSprites(actF, 0, 3), 18f, false);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/mossmir_RetaliateWindup.anim", SliceSprites(actF, 3, 1), cfg.WindupFps, false);
                    AnimationClip cRetal = CreateSpriteClip("Assets/Animations/Enemies/mossmir_LightRetaliation.anim", SliceSprites(actF, 3, 3), cfg.AttackFps, true);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/mossmir_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/mossmir_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/MossmirAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cHover;
                    AnimatorState stPogo = sm.AddState("PogoRecoil"); stPogo.motion = cPogo;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cRetal;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");

                    // Downward Pogo Recoil (IsHurt && IsTurning -> Row 2 F1-3 elastic dip & bounce!)
                    AnimatorStateTransition anyPogo = AddAnyTrans(sm, stPogo);
                    anyPogo.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyPogo.AddCondition(AnimatorConditionMode.If, 0, "IsTurning");
                    anyPogo.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stPogo, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    // Normal non-pogo hit stagger (IsHurt && !IsTurning -> Row 3 F1-2)
                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

                    assets.MossmirAnimator = ctrl;
                    assets.BiomeEnemyAnimators["bellhart_mossmir"] = ctrl;
                }
            }

            // 21. Aknid (3x6 @ 192x192, 16-22 FPS): CarryFlight (R1) / DiveNudge + PogoRecoil (R2) / Hurt & Death (R3)
            {
                ENEMY_COMBAT_CONFIG.MossGrottoProfile cfg = ENEMY_COMBAT_CONFIG.Aknid;
                Sprite[] carF = LoadSheetSprites(cfg.SpriteSheetPath, "aknid_Carry", 6);
                Sprite[] actF = LoadSheetSprites(cfg.SpriteSheetPath, "aknid_Action", 6);
                Sprite[] defF = LoadSheetSprites(cfg.SpriteSheetPath, "aknid_HDDefeat", 6);
                if (carF != null && carF.Length == 6 && actF != null && actF.Length == 6 && defF != null && defF.Length == 6)
                {
                    AnimationClip cIdle = CreateSpriteClip("Assets/Animations/Enemies/aknid_CarryIdle.anim", carF, cfg.IdleFps, true);
                    AnimationClip cMove = CreateSpriteClip("Assets/Animations/Enemies/aknid_CarryMove.anim", carF, cfg.MoveFps, true);
                    AnimationClip cWind = CreateSpriteClip("Assets/Animations/Enemies/aknid_DiveWindup.anim", SliceSprites(actF, 0, 1), cfg.WindupFps, false);
                    AnimationClip cDive = CreateSpriteClip("Assets/Animations/Enemies/aknid_DiveNudge.anim", SliceSprites(actF, 0, 3), cfg.AttackFps, true);
                    AnimationClip cPogo = CreateSpriteClip("Assets/Animations/Enemies/aknid_PogoRecoil.anim", SliceSprites(actF, 3, 3), 18f, false);
                    AnimationClip cHurt = CreateSpriteClip("Assets/Animations/Enemies/aknid_Hurt.anim", SliceSprites(defF, 0, 2), cfg.HurtFps, false);
                    AnimationClip cDeath = CreateSpriteClip("Assets/Animations/Enemies/aknid_Death.anim", SliceSprites(defF, 2, 4), cfg.DeathFps, false);

                    string ctrlPath = "Assets/Animations/Enemies/aknidAnimator.controller";
                    if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
                    AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                    ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
                    ctrl.AddParameter("IsWindup", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsTurning", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);
                    ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

                    AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
                    AnimatorState stIdle = sm.AddState("Idle"); stIdle.motion = cIdle;
                    AnimatorState stMove = sm.AddState("Move"); stMove.motion = cMove;
                    AnimatorState stWind = sm.AddState("Windup"); stWind.motion = cWind;
                    AnimatorState stAtk = sm.AddState("Attack"); stAtk.motion = cDive;
                    AnimatorState stPogo = sm.AddState("PogoRecoil"); stPogo.motion = cPogo;
                    AnimatorState stHurt = sm.AddState("Hurt"); stHurt.motion = cHurt;
                    AnimatorState stDeath = sm.AddState("Death"); stDeath.motion = cDeath;
                    sm.defaultState = stIdle;

                    AddFastTrans(stIdle, stMove).AddCondition(AnimatorConditionMode.Greater, 0.14f, "Speed");
                    AddFastTrans(stMove, stIdle).AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

                    AddAnyTrans(sm, stDeath).AddCondition(AnimatorConditionMode.If, 0, "IsDead");

                    // Downward Pogo Recoil (IsHurt && IsTurning -> Row 2 F4-6!)
                    AnimatorStateTransition anyPogo = AddAnyTrans(sm, stPogo);
                    anyPogo.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyPogo.AddCondition(AnimatorConditionMode.If, 0, "IsTurning");
                    anyPogo.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stPogo, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyHurt = AddAnyTrans(sm, stHurt);
                    anyHurt.AddCondition(AnimatorConditionMode.If, 0, "IsHurt");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTurning");
                    anyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    AddFastTrans(stHurt, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");

                    AnimatorStateTransition anyWind = AddAnyTrans(sm, stWind);
                    anyWind.AddCondition(AnimatorConditionMode.If, 0, "IsWindup");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyWind.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stWind, stAtk).AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    AddFastTrans(stWind, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsWindup");

                    AnimatorStateTransition anyAtk = AddAnyTrans(sm, stAtk);
                    anyAtk.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                    anyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsHurt");
                    AddFastTrans(stAtk, stIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

                    assets.BiomeEnemyAnimators["aknid"] = ctrl;
                }
            }
        }

        private static AnimatorStateTransition AddFastTrans(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.03f;
            return t;
        }

        private static AnimatorStateTransition AddAnyTrans(AnimatorStateMachine sm, AnimatorState to)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.duration = 0.02f;
            t.canTransitionToSelf = false;
            return t;
        }

        private static void CreatePrefabs(BuiltAssets assets)
        {
            assets.SilkOrbPrefab = BuildSilkOrbPrefab();
            assets.PlayerPrefab = BuildPlayerPrefab(assets);
            assets.LancerPrefab = BuildEnemyPrefab("Assets/Prefabs/Enemies/CrimsonLancer.prefab", "CrimsonLancer", assets.LancerStats, assets.EnemyAnimator, 1.0f, Color.white);
            assets.CaptainPrefab = BuildEnemyPrefab("Assets/Prefabs/Enemies/CrimsonCaptain.prefab", "CrimsonCaptain", assets.CaptainStats, assets.EnemyAnimator, 1.38f, new Color(1f, 0.84f, 0.80f, 1f));
            assets.MossmirPrefab = BuildMossmirPrefab(assets);
            assets.MossgrubPrefab = BuildMossgrubPrefab(assets);
            BuildAllShellwoodEnemyPrefabs(assets);
            BuildAllKarakEnemyPrefabs(assets);
            BuildAllMountFayEnemyPrefabs(assets);
            BuildAllChoralEnemyPrefabs(assets);
            BuildAllCourtyardDrapePrefabs(assets);
            BuildAllBellhartFurmPrefabs(assets);
            BuildAllBellhartExtendedPrefabs(assets);
            assets.CheckpointPrefab = BuildCheckpointPrefab();
            assets.AbilityPedestalPrefab = BuildAbilityPedestalPrefab();
            assets.UpgradeAltarPrefab = BuildUpgradeAltarPrefab();
            assets.InventoryPickupPrefab = BuildInventoryPickupPrefab();
            assets.BreakableWallPrefab = BuildBreakableWallPrefab();
            assets.SpikeHazardPrefab = BuildSpikeHazardPrefab();
            assets.HUDCanvasPrefab = SilkenGrottoSceneBuilder.BuildHUDCanvasPrefab("Assets/Prefabs/UI/GameplayHUDCanvas.prefab");
        }

        private static GameObject BuildPlayerPrefab(BuiltAssets assets)
        {
            GameObject root = new GameObject("Player");
            root.tag = GameConstants.TagPlayer;
            root.layer = GameConstants.LayerPlayer;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.2f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D bodyCol = root.AddComponent<CapsuleCollider2D>();
            bodyCol.offset = new Vector2(0f, 1.025f);
            bodyCol.size = new Vector2(0.68f, 2.05f);

            // Visual child (nudged -0.025f to absorb Box2D defaultContactOffset so heels always plant firmly on ground)
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.025f, 0f);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite[] idleSmoothInit = LoadSheetSprites("Assets/Art/Characters/Player/Player_IdleSmooth_Sheet.png", "IdleSmooth", 24);
            Sprite[] loco = LoadSheetSprites("Assets/Art/Characters/Player/Player_Locomotion_Sheet.png", "PlayerLoco", 8);
            if (idleSmoothInit.Length > 0 && idleSmoothInit[0] != null) sr.sprite = idleSmoothInit[0];
            else if (loco.Length > 0) sr.sprite = loco[0];
            if (assets.EnemyHitFlashMaterial != null) sr.sharedMaterial = assets.EnemyHitFlashMaterial;
            sr.sortingLayerName = GameConstants.SortPlayer;
            sr.sortingOrder = 5;

            Animator anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = assets.PlayerAnimator;

            // Hurtbox child
            GameObject hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = GameConstants.LayerPlayer;
            hurtGo.tag = GameConstants.TagPlayer;
            BoxCollider2D hCol = hurtGo.AddComponent<BoxCollider2D>();
            hCol.isTrigger = true;
            hCol.offset = new Vector2(0f, 1.05f);
            hCol.size = new Vector2(0.62f, 1.90f);
            Hurtbox hurtbox = hurtGo.AddComponent<Hurtbox>();
            SetBoolField(hurtbox, "useWhiteShaderFlash", false);

            // Melee Hitbox child
            GameObject hitGo = new GameObject("MeleeHitbox");
            hitGo.transform.SetParent(root.transform, false);
            hitGo.layer = GameConstants.LayerPlayerAttack;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            hitbox.SetTargetLayers((1 << GameConstants.LayerEnemy) | (1 << GameConstants.LayerBreakableWall) | (1 << GameConstants.LayerHazard) | (1 << GameConstants.LayerDestructibleFoliage));

            // Core Player scripts
            PlayerInputHandler input = root.AddComponent<PlayerInputHandler>();
            PlayerStats stats = root.AddComponent<PlayerStats>();
            SkillManager skills = root.AddComponent<SkillManager>();
            UpgradeManager upgrades = root.AddComponent<UpgradeManager>();
            InventoryManager inventory = root.AddComponent<InventoryManager>();
            PlayerMovement move = root.AddComponent<PlayerMovement>();
            PlayerCombat combat = root.AddComponent<PlayerCombat>();
            PlayerAnimatorController animCtrl = root.AddComponent<PlayerAnimatorController>();
            PlayerController controller = root.AddComponent<PlayerController>();

            // Wire serialized fields cleanly
            SetField(stats, "config", assets.PlayerStatsConfig);
            SetObjectList(skills, "allSkills", assets.AllSkills);
            SetObjectList(upgrades, "upgrades", assets.AllUpgrades);
            SetField(upgrades, "playerStats", stats);
            SetObjectList(inventory, "itemCatalog", assets.AllInventoryItems);
            SetField(inventory, "playerStats", stats);
            SetField(inventory, "upgradeManager", upgrades);

            SetField(move, "inputHandler", input);
            SetField(move, "stats", stats);
            SetField(move, "skillManager", skills);
            SetField(move, "visualRoot", visual.transform);

            SetField(combat, "inputHandler", input);
            SetField(combat, "movement", move);
            SetField(combat, "stats", stats);
            SetField(combat, "skillManager", skills);
            SetField(combat, "meleeHitbox", hitbox);
            SetObjectArray(combat, "groundComboAttacks", assets.ComboAttacks);
            SetField(combat, "airForwardAttack", assets.AirForwardAttack);
            SetField(combat, "upAttack", assets.UpAttack);
            SetField(combat, "downAirAttack", assets.DownAttack);
            SetField(combat, "silkSpearSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SilkSpear.png"));

            SetField(animCtrl, "animator", anim);
            SetField(animCtrl, "movement", move);
            SetField(animCtrl, "combat", combat);
            SetField(animCtrl, "controller", controller);

            SetField(hurtbox, "damageableComponent", controller);
            SetField(hurtbox, "spriteRenderer", sr);

            SetField(controller, "inputHandler", input);
            SetField(controller, "stats", stats);
            SetField(controller, "movement", move);
            SetField(controller, "combat", combat);
            SetField(controller, "animController", animCtrl);
            SetField(controller, "skillManager", skills);
            SetField(controller, "upgradeManager", upgrades);
            SetField(controller, "inventoryManager", inventory);
            SetField(controller, "hurtbox", hurtbox);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Player/Player.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildEnemyPrefab(string prefabPath, string name, EnemyStatsData statsData, AnimatorController animCtrl, float scale, Color tint)
        {
            GameObject root = new GameObject(name);
            root.tag = GameConstants.TagEnemy;
            root.layer = GameConstants.LayerEnemy;
            root.transform.localScale = Vector3.one * scale;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.offset = new Vector2(0f, 0.80f);
            col.size = new Vector2(0.92f, 1.60f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.025f, 0f);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite[] frames = LoadSheetSprites("Assets/Art/Characters/Enemies/CrimsonLancer_Sheet.png", "Lancer", 8);
            if (frames.Length > 0) sr.sprite = frames[0];
            sr.color = tint;
            sr.sortingLayerName = GameConstants.SortEnemies;
            sr.sortingOrder = 5;

            Animator anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;

            GameObject hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = GameConstants.LayerEnemy;
            BoxCollider2D hCol = hurtGo.AddComponent<BoxCollider2D>();
            hCol.isTrigger = true;
            hCol.offset = new Vector2(0f, 0.85f);
            hCol.size = new Vector2(1.05f, 1.65f);
            Hurtbox hurtbox = hurtGo.AddComponent<Hurtbox>();

            GameObject hitGo = new GameObject("AttackHitbox");
            hitGo.transform.SetParent(root.transform, false);
            hitGo.layer = GameConstants.LayerEnemyAttack;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            hitbox.SetTargetLayers(1 << GameConstants.LayerPlayer);

            EnemyController ec = root.AddComponent<EnemyController>();
            root.AddComponent<CrimsonLancerAI>();
            EnemyAnimatorDriver driver = root.AddComponent<EnemyAnimatorDriver>();

            SetField(ec, "statsData", statsData);
            SetField(ec, "visualRoot", visual.transform);
            SetField(ec, "hurtbox", hurtbox);
            SetField(ec, "attackHitbox", hitbox);
            SetField(ec, "silkOrbSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SilkOrb.png"));

            SetField(hurtbox, "damageableComponent", ec);
            SetField(hurtbox, "spriteRenderer", sr);
            SetField(driver, "animator", anim);
            SetField(driver, "enemyController", ec);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildMossmirPrefab(BuiltAssets assets)
        {
            GameObject root = new GameObject("Mossmir");
            root.tag = GameConstants.TagEnemy;
            root.layer = GameConstants.LayerEnemy;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D bodyCol = root.AddComponent<CircleCollider2D>();
            bodyCol.offset = Vector2.zero;
            bodyCol.radius = 0.34f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite[] frames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Mossmir.SpriteSheetPath, "mossmir_Hover", 6);
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                frames = LoadSheetSprites("Assets/Art/Characters/Enemies/Mossmir_Sheet.png", "MossmirIdle", 6);
            }
            if (frames != null && frames.Length > 0) sr.sprite = frames[0];
            if (assets.EnemyHitFlashMaterial != null) sr.sharedMaterial = assets.EnemyHitFlashMaterial;
            sr.sortingLayerName = GameConstants.SortEnemies;
            sr.sortingOrder = 5;

            Animator anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = assets.MossmirAnimator;

            GameObject hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = GameConstants.LayerEnemy;
            CircleCollider2D hCol = hurtGo.AddComponent<CircleCollider2D>();
            hCol.isTrigger = true;
            hCol.offset = new Vector2(0f, 0.08f);
            hCol.radius = ENEMY_COMBAT_CONFIG.Mossmir.TopPogoHurtboxRadius; // Widened responsive top pogo hurtbox
            Hurtbox hurtbox = hurtGo.AddComponent<Hurtbox>();

            GameObject hazardGo = new GameObject("ContactHazard");
            hazardGo.transform.SetParent(root.transform, false);
            hazardGo.layer = GameConstants.LayerEnemyAttack;
            CircleCollider2D cCol = hazardGo.AddComponent<CircleCollider2D>();
            cCol.isTrigger = true;
            cCol.offset = Vector2.zero;
            cCol.radius = 0.36f;
            EnemyContactHazard contact = hazardGo.AddComponent<EnemyContactHazard>();

            EnemyController ec = root.AddComponent<EnemyController>();
            MossmirAI ai = root.AddComponent<MossmirAI>();
            EnemyAnimatorDriver driver = root.AddComponent<EnemyAnimatorDriver>();

            SetField(ec, "statsData", assets.MossmirStats);
            SetField(ec, "visualRoot", visual.transform);
            SetField(ec, "hurtbox", hurtbox);
            SetField(ec, "contactHazard", contact);
            SetBoolField(ec, "isFlyingEnemy", true);
            SetBoolField(ec, "isMossyCreature", true);
            SetFloatField(ec, "bodyCenterHeight", 0f);
            SetField(ec, "silkOrbSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SilkOrb.png"));

            contact.Configure(ec, ENEMY_COMBAT_CONFIG.Mossmir.ContactDamage, ENEMY_COMBAT_CONFIG.Mossmir.ContactKnockback);
            SetField(contact, "enemyController", ec);
            SetField(hurtbox, "damageableComponent", ec);
            SetField(hurtbox, "spriteRenderer", sr);
            SetBoolField(hurtbox, "useWhiteShaderFlash", true);
            SetFloatField(hurtbox, "whiteHitFlashDuration", 0.048f);
            SetField(ai, "visualTransform", visual.transform);
            SetField(driver, "animator", anim);
            SetField(driver, "enemyController", ec);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Enemies/Mossmir.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildMossgrubPrefab(BuiltAssets assets)
        {
            GameObject root = new GameObject("Mossgrub");
            root.tag = GameConstants.TagEnemy;
            root.layer = GameConstants.LayerEnemy;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D bodyCol = root.AddComponent<BoxCollider2D>();
            bodyCol.offset = new Vector2(0f, 0.36f);
            bodyCol.size = new Vector2(0.96f, 0.72f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.025f, 0f);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            Sprite[] frames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.MossGrub.SpriteSheetPath, "moss_grub_Patrol", 6);
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                frames = LoadSheetSprites("Assets/Art/Characters/Enemies/Mossgrub_Sheet.png", "MossgrubPatrol", 6);
            }
            if (frames != null && frames.Length > 0) sr.sprite = frames[0];
            if (assets.EnemyHitFlashMaterial != null) sr.sharedMaterial = assets.EnemyHitFlashMaterial;
            sr.sortingLayerName = GameConstants.SortEnemies;
            sr.sortingOrder = 5;

            Animator anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = assets.MossgrubAnimator;

            GameObject hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = GameConstants.LayerEnemy;
            BoxCollider2D hCol = hurtGo.AddComponent<BoxCollider2D>();
            hCol.isTrigger = true;
            hCol.offset = new Vector2(0f, 0.44f);
            hCol.size = new Vector2(1.30f, 0.92f);
            Hurtbox hurtbox = hurtGo.AddComponent<Hurtbox>();

            // Full body & dorsal spikes inflict 1-mask contact hazard unless suppressed by clean downward pogo
            GameObject hazardGo = new GameObject("ContactHazard");
            hazardGo.transform.SetParent(root.transform, false);
            hazardGo.layer = GameConstants.LayerEnemyAttack;
            BoxCollider2D cCol = hazardGo.AddComponent<BoxCollider2D>();
            cCol.isTrigger = true;
            cCol.offset = new Vector2(0f, 0.36f);
            cCol.size = new Vector2(1.02f, 0.74f);
            EnemyContactHazard contact = hazardGo.AddComponent<EnemyContactHazard>();

            EnemyController ec = root.AddComponent<EnemyController>();
            MossgrubAI ai = root.AddComponent<MossgrubAI>();
            EnemyAnimatorDriver driver = root.AddComponent<EnemyAnimatorDriver>();

            SetField(ec, "statsData", assets.MossgrubStats);
            SetField(ec, "visualRoot", visual.transform);
            SetField(ec, "hurtbox", hurtbox);
            SetField(ec, "contactHazard", contact);
            SetBoolField(ec, "isFlyingEnemy", false);
            SetBoolField(ec, "isMossyCreature", true);
            SetFloatField(ec, "bodyCenterHeight", 0.42f);
            SetFloatField(ec, "ledgeProbeForward", 0.58f);
            SetFloatField(ec, "ledgeProbeDepth", 0.95f);
            SetFloatField(ec, "wallProbeDistance", 0.62f);
            SetFloatField(ec, "wallProbeHeight", 0.36f);
            SetField(ec, "silkOrbSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SilkOrb.png"));

            contact.Configure(ec, ENEMY_COMBAT_CONFIG.MossGrub.ContactDamage, ENEMY_COMBAT_CONFIG.MossGrub.ContactKnockback);
            SetField(contact, "enemyController", ec);
            SetField(hurtbox, "damageableComponent", ec);
            SetField(hurtbox, "spriteRenderer", sr);
            SetBoolField(hurtbox, "useWhiteShaderFlash", true);
            SetFloatField(hurtbox, "whiteHitFlashDuration", 0.048f);
            SetField(ai, "visualTransform", visual.transform);
            SetField(driver, "animator", anim);
            SetField(driver, "enemyController", ec);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Enemies/Mossgrub.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static EnemyStatsData CreateEnemyStatsAsset(
            string assetPath, string displayName, bool isElite, int maxHp, int essenceReward, int orbDrops,
            float patrolSpd, float chaseSpd, float patrolDist, float idlePause,
            float detectRng, float loseRng, float atkRng,
            int atkDmg, float atkKnockback, float windup, float active, float recovery, float cooldown,
            float lungeSpd, float kbResist, float stagger)
        {
            EnemyStatsData s = CreateOrLoadAsset<EnemyStatsData>(assetPath);
            s.enemyName = displayName;
            s.isElite = isElite;
            s.maxHealth = maxHp;
            s.silkEssenceReward = essenceReward;
            s.orbDropCount = orbDrops;
            s.patrolSpeed = patrolSpd;
            s.chaseSpeed = chaseSpd;
            s.patrolDistance = patrolDist;
            s.idlePauseDuration = idlePause;
            s.detectRange = detectRng;
            s.loseTargetRange = loseRng;
            s.attackRange = atkRng;
            s.attackDamage = atkDmg;
            s.attackKnockbackForce = atkKnockback;
            s.windupDuration = windup;
            s.attackActiveDuration = active;
            s.recoveryDuration = recovery;
            s.attackCooldown = cooldown;
            s.lungeSpeed = lungeSpd;
            s.knockbackResistance = kbResist;
            s.staggerDuration = stagger;
            EditorUtility.SetDirty(s);
            return s;
        }

        private static EnemyStatsData CreateMossGrottoStatsAsset(string assetPath, ENEMY_COMBAT_CONFIG.MossGrottoProfile p)
        {
            return CreateEnemyStatsAsset(
                assetPath,
                p.DisplayName, false, p.MaxHealth, p.SilkEssenceReward, p.OrbDropCount,
                p.PatrolSpeed, p.AggroSpeed, p.PatrolSpanX, 0.25f,
                p.DetectRange, p.LoseTargetRange, p.DetectRange,
                p.ContactDamage, p.ContactKnockback,
                0.22f, 0.28f, 0.32f, 0.85f,
                p.AggroSpeed, p.KnockbackResistance, p.StaggerDuration);
        }

        private static GameObject BuildAknidPrefab(BuiltAssets assets)
        {
            ENEMY_COMBAT_CONFIG.MossGrottoProfile cfg = ENEMY_COMBAT_CONFIG.Aknid;
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, "Aknid", cfg.SpritePath, assets.AknidStats, true, true, false, 0.72f,
                new Vector2(0.84f, 0.84f), new Vector2(cfg.TopPogoHurtboxRadius * 2f, cfg.TopPogoHurtboxRadius * 2f),
                cfg.ContactDamage, cfg.ContactKnockback,
                out root, out visualTransform, out ec);

            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            AknidAI ai = root.AddComponent<AknidAI>();
            ai.ConfigureCarry(cfg.DefaultCarryType);
            SetField(ai, "visualTransform", visualTransform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Enemies/Shellwood/Aknid.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildAllShellwoodEnemyPrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/Shellwood"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "Shellwood");
            }

            Sprite bellSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/BellProjectile.png");

            assets.AknidPrefab = BuildAknidPrefab(assets);

            assets.WingedPilgrimPrefab = BuildShellwoodEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Shellwood/WingedPilgrim.prefab", "WingedPilgrim",
                ENEMY_COMBAT_CONFIG.WingedPilgrim, assets.WingedPilgrimStats, null);

            assets.PilgrimGrovellerPrefab = BuildShellwoodEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Shellwood/PilgrimGroveller.prefab", "PilgrimGroveller",
                ENEMY_COMBAT_CONFIG.PilgrimGroveller, assets.PilgrimGrovellerStats, null);

            assets.PilgrimHulkPrefab = BuildShellwoodEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Shellwood/PilgrimHulk.prefab", "PilgrimHulk",
                ENEMY_COMBAT_CONFIG.PilgrimHulk, assets.PilgrimHulkStats, null);

            assets.PilgrimBellbearerPrefab = BuildShellwoodEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Shellwood/PilgrimBellbearer.prefab", "PilgrimBellbearer",
                ENEMY_COMBAT_CONFIG.PilgrimBellbearer, assets.PilgrimBellbearerStats, bellSprite);

            assets.CovetousPilgrimPrefab = BuildShellwoodEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Shellwood/CovetousPilgrim.prefab", "CovetousPilgrim",
                ENEMY_COMBAT_CONFIG.CovetousPilgrim, assets.CovetousPilgrimStats, null);

            assets.PondcatcherPrefab = BuildPondcatcherPrefab(assets);
        }

        private static EnemyStatsData CreatePilgrimStatsAsset(string assetPath, ENEMY_COMBAT_CONFIG.PilgrimProfile p)
        {
            float windup = p.JumpTelegraphDuration > 0.01f
                ? p.JumpTelegraphDuration
                : (p.DiveTelegraphDuration > 0.01f ? p.DiveTelegraphDuration
                : (p.BellWindupDuration > 0.01f ? p.BellWindupDuration
                : (p.RollCurlTelegraphDuration > 0.01f ? p.RollCurlTelegraphDuration : p.ChainSwingWindup)));
            float lunge = p.RollSpeed > 0.1f
                ? p.RollSpeed
                : (p.DiveSpeed > 0.1f ? p.DiveSpeed : (p.HopSpeedX > 0.1f ? p.HopSpeedX : p.BellThrowSpeed));

            return CreateEnemyStatsAsset(
                assetPath,
                p.DisplayName, p.IsElite, p.MaxHealth, p.SilkEssenceReward, p.RosaryDropCount,
                p.PatrolSpeed, p.AggroSpeed, p.PatrolSpanX, 0.25f,
                p.DetectRange, p.LoseTargetRange, p.AttackRange,
                p.AttackDamage, p.ContactKnockback,
                windup, 0.30f, p.RecoverDuration, p.AttackCooldown,
                lunge, p.KnockbackResistance, p.StaggerDuration);
        }

        private static GameObject BuildPondcatcherPrefab(BuiltAssets assets)
        {
            ENEMY_COMBAT_CONFIG.Batch2Profile cfg = ENEMY_COMBAT_CONFIG.Pondcatcher;
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, "Pondcatcher", cfg.SpritePath, assets.PondcatcherStats, cfg.IsFlying, true, false, 0.72f,
                cfg.BodyColliderSize, cfg.BodyHurtboxSize, cfg.ContactDamage, cfg.ContactKnockback,
                out root, out visualTransform, out ec);

            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            PondcatcherAI ai = root.AddComponent<PondcatcherAI>();
            SetField(ai, "visualTransform", visualTransform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Enemies/Shellwood/Pondcatcher.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static EnemyStatsData CreateBatch2StatsAsset(string assetPath, ENEMY_COMBAT_CONFIG.Batch2Profile p)
        {
            float windup = p.CrouchTelegraphDuration > 0.01f
                ? p.CrouchTelegraphDuration
                : (p.SweepWindupDuration > 0.01f ? p.SweepWindupDuration
                : (p.SpearWindupDuration > 0.01f ? p.SpearWindupDuration
                : (p.EruptWarningDuration > 0.01f ? p.EruptWarningDuration : 0.25f)));
            float lunge = p.LeapSpeedX > 0.1f
                ? p.LeapSpeedX
                : (p.SweepSpeed > 0.1f ? p.SweepSpeed : (p.PopJumpSpeedX > 0.1f ? p.PopJumpSpeedX : 8.0f));
            float recover = p.LandRecoverDuration > 0.01f
                ? p.LandRecoverDuration
                : (p.TurnRecoverDuration > 0.01f ? p.TurnRecoverDuration
                : (p.PullBackRecoverDuration > 0.01f ? p.PullBackRecoverDuration : 0.45f));

            return CreateEnemyStatsAsset(
                assetPath,
                p.DisplayName, false, p.MaxHealth, p.SilkEssenceReward, p.OrbDropCount,
                p.PatrolSpeed, p.AggroSpeed, p.PatrolSpanX, 0.25f,
                p.DetectRange, p.LoseTargetRange, p.AttackRange,
                p.ContactDamage, p.ContactKnockback,
                windup, 0.30f, recover, p.AttackCooldown,
                lunge, p.KnockbackResistance, p.StaggerDuration);
        }

        private static void CreateBiomeEnemyChassis(
            BuiltAssets assets,
            string name,
            string spritePath,
            EnemyStatsData statsData,
            bool isFlying,
            bool isMossyCreature,
            bool hasTopSpikyArmor,
            float topArmorHeightOffset,
            Vector2 colliderSize,
            Vector2 hurtSize,
            int contactDamage,
            float contactKnockback,
            out GameObject root,
            out Transform visualTransform,
            out EnemyController enemyController)
        {
            root = new GameObject(name);
            root.tag = GameConstants.TagEnemy;
            root.layer = GameConstants.LayerEnemy;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = isFlying ? 0f : 3.2f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (isFlying)
            {
                CircleCollider2D bodyCol = root.AddComponent<CircleCollider2D>();
                bodyCol.offset = Vector2.zero;
                bodyCol.radius = colliderSize.x * 0.5f;
            }
            else
            {
                BoxCollider2D bodyCol = root.AddComponent<BoxCollider2D>();
                bodyCol.offset = new Vector2(0f, colliderSize.y * 0.5f);
                bodyCol.size = colliderSize;
            }

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = isFlying ? Vector3.zero : new Vector3(0f, -0.025f, 0f);
            visualTransform = visual.transform;
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            string animStem = System.IO.Path.GetFileNameWithoutExtension(spritePath);
            if (statsData == assets.BellhartMossmirStats ||
                (string.Equals(animStem, "mossmir", System.StringComparison.OrdinalIgnoreCase) && isMossyCreature))
            {
                animStem = "bellhart_mossmir";
            }
            else if (statsData == assets.BellhartMossgrubStats ||
                (string.Equals(animStem, "mossgrub", System.StringComparison.OrdinalIgnoreCase) && isMossyCreature))
            {
                animStem = "bellhart_mossgrub";
            }
            AnimatorController biomeAnim = null;
            if (assets.BiomeEnemyAnimators != null && assets.BiomeEnemyAnimators.TryGetValue(animStem, out biomeAnim) && biomeAnim != null)
            {
                Sprite[] idleFrames = null;
                if (animStem == "servitor_boran")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.ServitorBoran.SpriteSheetPath, "servitor_boran_Scan", 6);
                }
                else if (animStem == "driftlin")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Driftlin.SpriteSheetPath, "driftlin_Float", 6);
                }
                else if (animStem == "mnemonid")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Mnemonid.SpriteSheetPath, "mnemonid_Dormant", 6);
                }
                else if (animStem == "mnemonord")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Mnemonord.SpriteSheetPath, "mnemonord_Dormant", 6);
                }
                else if (animStem == "drapemite")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Drapemite.SpriteSheetPath, "drapemite_Patrol", 6);
                }
                else if (animStem == "drapefly")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Drapefly.SpriteSheetPath, "drapefly_Hover", 6);
                }
                else if (animStem == "pondcatcher")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Pondcatcher.SpriteSheetPath, "pondcatcher_Hunt", 6);
                }
                else if (animStem == "furm")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Furm.SpriteSheetPath, "furm_Burrow", 6);
                }
                else if (animStem == "winged_furm")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.WingedFurm.SpriteSheetPath, "winged_furm_Patrol", 6);
                }
                else if (animStem == "pilgrim_groveller")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.PilgrimGroveller.SpriteSheetPath, "pilgrim_groveller_Walk", 6);
                }
                else if (animStem == "winged_pilgrim")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.WingedPilgrim.SpriteSheetPath, "winged_pilgrim_Patrol", 6);
                }
                else if (animStem == "pilgrim_bellbearer")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.PilgrimBellbearer.SpriteSheetPath, "pilgrim_bellbearer_Walk", 6);
                }
                else if (animStem == "covetous_pilgrim")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.CovetousPilgrim.SpriteSheetPath, "covetous_pilgrim_March", 6);
                }
                else if (animStem == "pilgrim_hulk")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.PilgrimHulk.SpriteSheetPath, "pilgrim_hulk_Walk", 6);
                }
                else if (animStem == "driznit")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Driznit.SpriteSheetPath, "driznit_Hover", 6);
                }
                else if (animStem == "conchfly")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Conchfly.SpriteSheetPath, "conchfly_Patrol", 6);
                }
                else if (animStem == "pharlid")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Pharlid.SpriteSheetPath, "pharlid_Skitter", 6);
                }
                else if (animStem == "coral_furm")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.CoralFurm.SpriteSheetPath, "coral_furm_Crawl", 6);
                }
                else if (animStem == "bellhart_mossgrub")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.MossGrub.SpriteSheetPath, "moss_grub_Patrol", 6);
                }
                else if (animStem == "bellhart_mossmir")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Mossmir.SpriteSheetPath, "mossmir_Hover", 6);
                }
                else if (animStem == "aknid")
                {
                    idleFrames = LoadSheetSprites(ENEMY_COMBAT_CONFIG.Aknid.SpriteSheetPath, "aknid_Carry", 6);
                }
                if (idleFrames == null || idleFrames.Length == 0 || idleFrames[0] == null)
                {
                    idleFrames = LoadSheetSprites("Assets/Art/Characters/Enemies/Sheets/" + animStem + "_Sheet.png", animStem + "_Idle", 12);
                }
                if (idleFrames != null && idleFrames.Length > 0 && idleFrames[0] != null)
                {
                    sr.sprite = idleFrames[0];
                }
            }
            if (assets.EnemyHitFlashMaterial != null) sr.sharedMaterial = assets.EnemyHitFlashMaterial;
            sr.sortingLayerName = GameConstants.SortEnemies;
            sr.sortingOrder = 5;

            Animator biomeAnimatorComp = null;
            if (biomeAnim != null)
            {
                biomeAnimatorComp = root.AddComponent<Animator>();
                biomeAnimatorComp.runtimeAnimatorController = biomeAnim;
            }

            GameObject hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = GameConstants.LayerEnemy;
            if (isFlying)
            {
                CircleCollider2D hCol = hurtGo.AddComponent<CircleCollider2D>();
                hCol.isTrigger = true;
                hCol.offset = Vector2.zero;
                hCol.radius = hurtSize.x * 0.5f;
            }
            else
            {
                BoxCollider2D hCol = hurtGo.AddComponent<BoxCollider2D>();
                hCol.isTrigger = true;
                hCol.offset = new Vector2(0f, hurtSize.y * 0.5f);
                hCol.size = hurtSize;
            }
            Hurtbox hurtbox = hurtGo.AddComponent<Hurtbox>();

            GameObject hazardGo = new GameObject("ContactHazard");
            hazardGo.transform.SetParent(root.transform, false);
            hazardGo.layer = GameConstants.LayerEnemyAttack;
            if (isFlying)
            {
                CircleCollider2D cCol = hazardGo.AddComponent<CircleCollider2D>();
                cCol.isTrigger = true;
                cCol.offset = Vector2.zero;
                cCol.radius = colliderSize.x * 0.52f;
            }
            else
            {
                float hazardExtraY = hasTopSpikyArmor ? 0.14f : 0.04f;
                BoxCollider2D cCol = hazardGo.AddComponent<BoxCollider2D>();
                cCol.isTrigger = true;
                cCol.offset = new Vector2(0f, (colliderSize.y + hazardExtraY) * 0.5f);
                cCol.size = new Vector2(colliderSize.x + 0.08f, colliderSize.y + hazardExtraY);
            }
            EnemyContactHazard contact = hazardGo.AddComponent<EnemyContactHazard>();

            GameObject hitGo = new GameObject("AttackHitbox");
            hitGo.transform.SetParent(root.transform, false);
            hitGo.layer = GameConstants.LayerEnemyAttack;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            hitbox.SetTargetLayers(1 << GameConstants.LayerPlayer);

            enemyController = root.AddComponent<EnemyController>();
            SetField(enemyController, "statsData", statsData);
            SetField(enemyController, "visualRoot", visualTransform);
            SetField(enemyController, "hurtbox", hurtbox);
            SetField(enemyController, "attackHitbox", hitbox);
            SetField(enemyController, "contactHazard", contact);
            SetBoolField(enemyController, "isFlyingEnemy", isFlying);
            SetBoolField(enemyController, "isMossyCreature", isMossyCreature);
            SetBoolField(enemyController, "hasTopSpikyArmor", hasTopSpikyArmor);
            SetFloatField(enemyController, "topArmorHeightOffset", topArmorHeightOffset);
            SetFloatField(enemyController, "bodyCenterHeight", isFlying ? 0f : colliderSize.y * 0.5f);
            SetFloatField(enemyController, "ledgeProbeForward", Mathf.Max(0.55f, colliderSize.x * 0.52f));
            SetFloatField(enemyController, "ledgeProbeDepth", 1.0f);
            SetFloatField(enemyController, "wallProbeDistance", Mathf.Max(0.62f, colliderSize.x * 0.56f));
            SetFloatField(enemyController, "wallProbeHeight", Mathf.Max(0.36f, colliderSize.y * 0.45f));
            SetField(enemyController, "silkOrbPrefab", assets.SilkOrbPrefab);
            SetField(enemyController, "silkOrbSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SilkOrb.png"));

            contact.Configure(enemyController, contactDamage, contactKnockback);
            SetField(contact, "enemyController", enemyController);
            SetField(hurtbox, "damageableComponent", enemyController);
            SetField(hurtbox, "spriteRenderer", sr);
            SetBoolField(hurtbox, "useWhiteShaderFlash", true);

            if (biomeAnimatorComp != null)
            {
                EnemyAnimatorDriver driver = root.AddComponent<EnemyAnimatorDriver>();
                SetField(driver, "animator", biomeAnimatorComp);
                SetField(driver, "enemyController", enemyController);
            }
        }

        private static GameObject BuildShellwoodEnemyPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            ENEMY_COMBAT_CONFIG.PilgrimProfile cfg,
            EnemyStatsData statsData,
            Sprite bellSprite)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, cfg.SpritePath, statsData, cfg.IsFlying, true, false, 0.72f,
                cfg.BodyColliderSize, cfg.BodyHurtboxSize, cfg.ContactDamage, cfg.ContactKnockback,
                out root, out visualTransform, out ec);

            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            ShellwoodEnemyAI ai = root.AddComponent<ShellwoodEnemyAI>();
            ai.ConfigureArchetype(cfg.Archetype, bellSprite);
            SetField(ai, "visualTransform", visualTransform);
            if (bellSprite != null) SetField(ai, "bellProjectileSprite", bellSprite);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildAllKarakEnemyPrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/Karak"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "Karak");
            }

            Sprite dartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/KarakDartProjectile.png");

            assets.DriznitPrefab = BuildKarakEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Karak/Driznit.prefab", "Driznit",
                KarakArchetype.Driznit, ENEMY_COMBAT_CONFIG.Driznit.SpritePath,
                assets.DriznitStats, true, false, new Vector2(0.84f, 0.84f), new Vector2(1.14f, 1.14f), 1, 5.8f, dartSprite, PharlidAmbushMode.Ground);

            assets.ConchflyPrefab = BuildKarakEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Karak/Conchfly.prefab", "Conchfly",
                KarakArchetype.Conchfly, ENEMY_COMBAT_CONFIG.Conchfly.SpritePath,
                assets.ConchflyStats, true, false, new Vector2(1.12f, 1.12f), new Vector2(1.42f, 1.42f), 1, 6.8f, dartSprite, PharlidAmbushMode.Ground);

            assets.PharlidPrefab = BuildKarakEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Karak/Pharlid.prefab", "Pharlid",
                KarakArchetype.Pharlid, ENEMY_COMBAT_CONFIG.Pharlid.SpritePath,
                assets.PharlidStats, false, false, new Vector2(1.10f, 0.90f), new Vector2(1.32f, 1.10f), 1, 6.2f, null, PharlidAmbushMode.Ground);

            // Coral Furm: pogo-friendly (hasTopSpikyArmor = false), crunchy coral shard burst on hit & death
            assets.CoralFurmPrefab = BuildKarakEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Karak/CoralFurm.prefab", "CoralFurm",
                KarakArchetype.CoralFurm, ENEMY_COMBAT_CONFIG.CoralFurm.SpritePath,
                assets.CoralFurmStats, false, false, new Vector2(1.32f, 1.32f), new Vector2(1.48f, 1.42f), 1, 6.8f, null, PharlidAmbushMode.Ground);
        }

        private static GameObject BuildKarakEnemyPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            KarakArchetype archetype,
            string spritePath,
            EnemyStatsData statsData,
            bool isFlying,
            bool hasTopSpikyArmor,
            Vector2 colliderSize,
            Vector2 hurtSize,
            int contactDamage,
            float contactKnockback,
            Sprite dartSprite,
            PharlidAmbushMode ambushMode)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, spritePath, statsData, isFlying, false, hasTopSpikyArmor, 0.72f,
                colliderSize, hurtSize, contactDamage, contactKnockback,
                out root, out visualTransform, out ec);

            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            KarakEnemyAI ai = root.AddComponent<KarakEnemyAI>();
            ai.ConfigureArchetype(archetype, dartSprite);
            ai.ConfigureAmbushMode(ambushMode);
            SetField(ai, "visualTransform", visualTransform);
            if (dartSprite != null) SetField(ai, "dartProjectileSprite", dartSprite);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildAllMountFayEnemyPrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/MountFay"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "MountFay");
            }

            Sprite waveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/FayLightWaveProjectile.png");
            Sprite crySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/FayIceCrystalProjectile.png");

            assets.ServitorBoranPrefab = BuildMountFayEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/MountFay/ServitorBoran.prefab", "ServitorBoran",
                ENEMY_COMBAT_CONFIG.ServitorBoran, assets.ServitorBoranStats, waveSprite, null);

            assets.MnemonordPrefab = BuildMountFayEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/MountFay/Mnemonord.prefab", "Mnemonord",
                ENEMY_COMBAT_CONFIG.Mnemonord, assets.MnemonordStats, null, crySprite);

            assets.MnemonidPrefab = BuildMountFayEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/MountFay/Mnemonid.prefab", "Mnemonid",
                ENEMY_COMBAT_CONFIG.Mnemonid, assets.MnemonidStats, null, crySprite);

            assets.DriftlinPrefab = BuildMountFayEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/MountFay/Driftlin.prefab", "Driftlin",
                ENEMY_COMBAT_CONFIG.Driftlin, assets.DriftlinStats, null, null);
        }

        private static GameObject BuildMountFayEnemyPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            ENEMY_COMBAT_CONFIG.MountFayProfile cfg,
            EnemyStatsData statsData,
            Sprite waveSprite,
            Sprite crystalSprite)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, cfg.SpritePath, statsData, cfg.IsFlying, false, false, 0.72f,
                cfg.BodyColliderSize, cfg.BodyHurtboxSize, cfg.ContactDamage, cfg.ContactKnockback,
                out root, out visualTransform, out ec);

            SetBoolField(ec, "isFrostCrystalCreature", true);
            if (cfg.RespawnAfterSeconds > 0f)
            {
                SetFloatField(ec, "respawnAfterSeconds", cfg.RespawnAfterSeconds);
            }

            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            MountFayEnemyAI ai = root.AddComponent<MountFayEnemyAI>();
            ai.ConfigureArchetype(cfg.Archetype, waveSprite, crystalSprite);
            SetField(ai, "visualTransform", visualTransform);
            if (waveSprite != null) SetField(ai, "lightWaveSprite", waveSprite);
            if (crystalSprite != null) SetField(ai, "iceCrystalSprite", crystalSprite);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static EnemyStatsData CreateMountFayStatsAsset(string assetPath, ENEMY_COMBAT_CONFIG.MountFayProfile p)
        {
            float windup = p.LaserChargeDuration > 0.01f ? p.LaserChargeDuration : (p.SpikeTriggerDelay > 0.01f ? p.SpikeTriggerDelay : 0.20f);
            float activeDur = p.LaserFireDuration > 0.01f ? p.LaserFireDuration : (p.SpikeActiveDuration > 0.01f ? p.SpikeActiveDuration : 0.25f);
            int atkDmg = p.LaserDamage > 0 ? p.LaserDamage : Mathf.Max(1, p.SpikeContactDamage > 0 ? p.SpikeContactDamage : p.ContactDamage);

            return CreateEnemyStatsAsset(
                assetPath,
                p.DisplayName, p.IsElite, p.MaxHealth, p.SilkEssenceReward, p.OrbDropCount,
                0f, 0f, 0f, 0.40f,
                p.DetectRange, p.LoseTargetRange, p.AttackRange,
                atkDmg, p.LaserKnockback > 0f ? p.LaserKnockback : p.ContactKnockback,
                windup, activeDur, p.LaserBurstInterval > 0f ? p.LaserBurstInterval : 0.35f, p.LaserCooldown > 0f ? p.LaserCooldown : 1.0f,
                0f, p.KnockbackResistance, p.StaggerDuration);
        }

        private static EnemyStatsData CreateKarakStatsAsset(string assetPath, ENEMY_COMBAT_CONFIG.KarakProfile p)
        {
            float windup = p.AimTelegraphDuration > 0.01f
                ? p.AimTelegraphDuration
                : (p.ThrowWindupDuration > 0.01f ? p.ThrowWindupDuration : p.LungeWindupDuration);
            float lunge = p.DartSpeed > 0.1f
                ? p.DartSpeed
                : (p.QuickLungeSpeed > 0.1f ? p.QuickLungeSpeed : (p.AlertHopSpeed > 0.1f ? p.AlertHopSpeed : p.ProjectileSpeed));
            float activeDur = p.DartDuration > 0.01f
                ? p.DartDuration
                : (p.AlertHopDuration > 0.01f ? p.AlertHopDuration : 0.30f);

            return CreateEnemyStatsAsset(
                assetPath,
                p.DisplayName, false, p.MaxHealth, p.SilkEssenceReward, p.OrbDropCount,
                p.PatrolSpeed, p.AggroSpeed, p.PatrolSpanX, 0.25f,
                p.DetectRange, p.LoseTargetRange, p.AttackRange,
                p.ContactDamage, p.ContactKnockback,
                windup, activeDur, p.BrakeRecoverDuration, p.AttackCooldown,
                lunge, p.KnockbackResistance, p.StaggerDuration);
        }

        private static EnemyStatsData CreateChoralStatsAsset(string assetPath, ENEMY_COMBAT_CONFIG.Profile p)
        {
            return CreateEnemyStatsAsset(
                assetPath,
                p.DisplayName, p.IsElite, p.MaxHealth, p.SilkEssenceReward, p.OrbDropCount,
                p.PatrolSpeed, p.ChaseSpeed, p.PatrolDistance, p.IdlePauseDuration,
                p.DetectRange, p.LoseTargetRange, p.AttackRange,
                p.PrimaryAttackDamage, p.PrimaryKnockback,
                p.TelegraphDuration, p.AttackActiveDuration, p.RecoveryDuration, p.AttackCooldown,
                p.LungeSpeed, p.KnockbackResistance, p.StaggerDuration);
        }

        private static void BuildAllChoralEnemyPrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/ChoralChambers"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "ChoralChambers");
            }

            Sprite waveSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Enemies/ChoralSoundWaveProjectile.png");

            // All 6 Choral Chambers prefabs strictly preserve existing Assets/Art/Characters/Enemies/*.png art paths
            // and pull hitbox, hurtbox, FrontBlockBox, contact damage, and tuning directly from ENEMY_COMBAT_CONFIG!
            assets.GrandReedPrefab = BuildChoralEnemyPrefab(assets, "Assets/Prefabs/Enemies/ChoralChambers/GrandReed.prefab", "GrandReed", ENEMY_COMBAT_CONFIG.GrandReed, assets.GrandReedStats, null);
            assets.ReedPrefab = BuildChoralEnemyPrefab(assets, "Assets/Prefabs/Enemies/ChoralChambers/Reed.prefab", "Reed", ENEMY_COMBAT_CONFIG.Reed, assets.ReedStats, null);
            assets.ChoristorPrefab = BuildChoralEnemyPrefab(assets, "Assets/Prefabs/Enemies/ChoralChambers/Choristor.prefab", "Choristor", ENEMY_COMBAT_CONFIG.Choristor, assets.ChoristorStats, waveSprite);
            assets.ChoirHornheadPrefab = BuildChoralEnemyPrefab(assets, "Assets/Prefabs/Enemies/ChoralChambers/ChoirHornhead.prefab", "ChoirHornhead", ENEMY_COMBAT_CONFIG.ChoirHornhead, assets.ChoirHornheadStats, null);
            assets.ChoirElderPrefab = BuildChoralEnemyPrefab(assets, "Assets/Prefabs/Enemies/ChoralChambers/ChoirElder.prefab", "ChoirElder", ENEMY_COMBAT_CONFIG.ChoirElder, assets.ChoirElderStats, waveSprite);
            assets.ChoirFlyerPrefab = BuildChoralEnemyPrefab(assets, "Assets/Prefabs/Enemies/ChoralChambers/ChoirFlyer.prefab", "ChoirFlyer", ENEMY_COMBAT_CONFIG.ChoirFlyer, assets.ChoirFlyerStats, waveSprite);
        }

        private static GameObject BuildChoralEnemyPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            ENEMY_COMBAT_CONFIG.Profile cfg,
            EnemyStatsData statsData,
            Sprite waveSprite)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, cfg.SpritePath, statsData, cfg.IsFlying, false, false, 0.72f,
                cfg.BodyColliderSize, cfg.BodyHurtboxSize, cfg.ContactDamage, cfg.ContactKnockback,
                out root, out visualTransform, out ec);

            SetBoolField(ec, "isGoldenSilkCreature", true);

            // Crisp 1-2 frame Silksong white silhouette hit-flash on BodyHurtbox
            Hurtbox bodyHurt = root.GetComponentInChildren<Hurtbox>();
            if (bodyHurt != null)
            {
                SetBoolField(bodyHurt, "useWhiteShaderFlash", true);
                SetFloatField(bodyHurt, "whiteHitFlashDuration", 0.048f);
            }

            // Separated FrontBlockBox for Reed (BlockStance) & Grand Reed (GuardStance)
            EnemyFrontBlockBox frontBlock = null;
            if (cfg.HasFrontBlockBox)
            {
                GameObject fbGo = new GameObject("FrontBlockBox");
                fbGo.transform.SetParent(root.transform, false);
                fbGo.layer = GameConstants.LayerEnemy;
                frontBlock = fbGo.AddComponent<EnemyFrontBlockBox>();
                frontBlock.Configure(ec, cfg.FrontBlockBoxOffset, cfg.FrontBlockBoxSize, new Color(1.0f, 0.92f, 0.56f, 0.95f));
                SetField(frontBlock, "owner", ec);
            }

            ChoralEnemyAI ai = root.AddComponent<ChoralEnemyAI>();
            ai.ConfigureArchetype(cfg.Archetype, waveSprite);
            SetField(ai, "visualTransform", visualTransform);
            if (frontBlock != null) SetField(ai, "frontBlockBox", frontBlock);
            if (waveSprite != null) SetField(ai, "soundWaveSprite", waveSprite);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildAllCourtyardDrapePrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/Courtyard"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "Courtyard");
            }

            assets.DrapemitePrefab = BuildCourtyardDrapePrefab(
                assets, "Assets/Prefabs/Enemies/Courtyard/Drapemite.prefab", "Drapemite",
                CourtyardDrapeArchetype.Drapemite, ENEMY_COMBAT_CONFIG.Drapemite.SpritePath,
                assets.DrapemiteStats, false, ENEMY_COMBAT_CONFIG.Drapemite.BodyColliderSize, ENEMY_COMBAT_CONFIG.Drapemite.BodyHurtboxSize, 1, ENEMY_COMBAT_CONFIG.Drapemite.ContactKnockback);

            assets.DrapeflyPrefab = BuildCourtyardDrapePrefab(
                assets, "Assets/Prefabs/Enemies/Courtyard/Drapefly.prefab", "Drapefly",
                CourtyardDrapeArchetype.Drapefly, ENEMY_COMBAT_CONFIG.Drapefly.SpritePath,
                assets.DrapeflyStats, true, ENEMY_COMBAT_CONFIG.Drapefly.BodyColliderSize, ENEMY_COMBAT_CONFIG.Drapefly.BodyHurtboxSize, 1, ENEMY_COMBAT_CONFIG.Drapefly.ContactKnockback);
        }

        private static GameObject BuildCourtyardDrapePrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            CourtyardDrapeArchetype archetype,
            string spritePath,
            EnemyStatsData statsData,
            bool isFlying,
            Vector2 colliderSize,
            Vector2 hurtSize,
            int contactDamage,
            float contactKnockback)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, spritePath, statsData, isFlying, false, false, 0.72f,
                colliderSize, hurtSize, contactDamage, contactKnockback,
                out root, out visualTransform, out ec);

            SetBoolField(ec, "isDrapeClothCreature", true);
            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            CourtyardDrapeAI ai = root.AddComponent<CourtyardDrapeAI>();
            ai.ConfigureArchetype(archetype);
            SetField(ai, "visualTransform", visualTransform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildAllBellhartFurmPrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/Bellhart"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "Bellhart");
            }

            // Furm (Underground Ambush Jumper) & Winged Furm (Vertical Bobber + Dizzy Helmet) — both pogo-friendly when exposed!
            assets.FurmPrefab = BuildBellhartFurmPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/Furm.prefab", "Furm",
                BellhartFurmArchetype.Furm, ENEMY_COMBAT_CONFIG.Furm.SpritePath,
                assets.FurmStats, false, false, ENEMY_COMBAT_CONFIG.Furm.BodyColliderSize, ENEMY_COMBAT_CONFIG.Furm.BodyHurtboxSize, 1, ENEMY_COMBAT_CONFIG.Furm.ContactKnockback);

            assets.WingedFurmPrefab = BuildBellhartFurmPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/WingedFurm.prefab", "WingedFurm",
                BellhartFurmArchetype.WingedFurm, ENEMY_COMBAT_CONFIG.WingedFurm.SpritePath,
                assets.WingedFurmStats, true, false, ENEMY_COMBAT_CONFIG.WingedFurm.BodyColliderSize, ENEMY_COMBAT_CONFIG.WingedFurm.BodyHurtboxSize, 1, ENEMY_COMBAT_CONFIG.WingedFurm.ContactKnockback);
        }

        private static GameObject BuildBellhartFurmPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            BellhartFurmArchetype archetype,
            string spritePath,
            EnemyStatsData statsData,
            bool isFlying,
            bool hasBellTopArmor,
            Vector2 colliderSize,
            Vector2 hurtSize,
            int contactDamage,
            float contactKnockback)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, spritePath, statsData, isFlying, false, hasBellTopArmor, 0.62f,
                colliderSize, hurtSize, contactDamage, contactKnockback,
                out root, out visualTransform, out ec);

            SetBoolField(ec, "isBellBronzeCreature", true);
            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }
            if (hasBellTopArmor)
            {
                SetBoolField(ec, "topArmorReflectsPogoBounce", true);
            }

            BellhartFurmAI ai = root.AddComponent<BellhartFurmAI>();
            ai.ConfigureArchetype(archetype);
            SetField(ai, "visualTransform", visualTransform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildAllBellhartExtendedPrefabs(BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies/Bellhart"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Enemies", "Bellhart");
            }

            assets.SkullwingPrefab = BuildBellhartExtendedPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/Skullwing.prefab", "Skullwing",
                BellhartExtendedArchetype.Skullwing, "Assets/Art/Characters/Enemies/skullwing.png",
                assets.SkullwingStats, true, false, false, 0.50f, true,
                new Vector2(1.00f, 0.82f), new Vector2(1.24f, 1.00f), 1, 6.0f);

            // Bellhart PharlidDiver reuses KarakEnemyAI's CeilingDrop ambush + ground skitter FSM while strictly preserving pharlid_diver.png!
            assets.PharlidDiverPrefab = BuildKarakEnemyPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/PharlidDiver.prefab", "PharlidDiver",
                KarakArchetype.Pharlid, "Assets/Art/Characters/Enemies/pharlid_diver.png",
                assets.PharlidDiverStats, false, false, new Vector2(0.98f, 0.88f), new Vector2(1.22f, 1.08f), 1, 6.2f, null, PharlidAmbushMode.CeilingDrop);

            assets.MawlingPrefab = BuildBellhartExtendedPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/Mawling.prefab", "Mawling",
                BellhartExtendedArchetype.Mawling, "Assets/Art/Characters/Enemies/mawling.png",
                assets.MawlingStats, false, false, false, 0.50f, true,
                new Vector2(0.76f, 0.80f), new Vector2(0.96f, 0.96f), 1, 5.8f);

            assets.MarrowmawPrefab = BuildBellhartExtendedPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/Marrowmaw.prefab", "Marrowmaw",
                BellhartExtendedArchetype.Marrowmaw, "Assets/Art/Characters/Enemies/marrowmaw.png",
                assets.MarrowmawStats, false, false, false, 1.15f, true,
                new Vector2(1.56f, 1.84f), new Vector2(1.80f, 2.02f), 2, 9.4f);

            // Bellhart Mossmir & Mossgrub reuse the refactored MossmirAI (Pogo Trainer) & MossgrubAI (Passive-Until-Provoked)
            // while strictly preserving their existing mossmir.png / mossgrub.png & bellhart_moss*_Sheet.png art assets!
            assets.BellhartMossmirPrefab = BuildBellhartMossVariantPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/Mossmir.prefab", "Mossmir",
                "Assets/Art/Characters/Enemies/mossmir.png", assets.BellhartMossmirStats, true,
                new Vector2(0.84f, 0.90f), new Vector2(1.36f, 1.36f), 1, 6.0f);

            assets.BellhartMossgrubPrefab = BuildBellhartMossVariantPrefab(
                assets, "Assets/Prefabs/Enemies/Bellhart/Mossgrub.prefab", "Mossgrub",
                "Assets/Art/Characters/Enemies/mossgrub.png", assets.BellhartMossgrubStats, false,
                new Vector2(0.96f, 0.72f), new Vector2(1.24f, 0.90f), 1, 6.0f);
        }

        private static GameObject BuildBellhartMossVariantPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            string spritePath,
            EnemyStatsData statsData,
            bool isFlying,
            Vector2 colliderSize,
            Vector2 hurtSize,
            int contactDamage,
            float contactKnockback)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, spritePath, statsData, isFlying, true, false, 0.50f,
                colliderSize, hurtSize, contactDamage, contactKnockback,
                out root, out visualTransform, out ec);

            Hurtbox hb = root.GetComponentInChildren<Hurtbox>();
            if (hb != null)
            {
                SetBoolField(hb, "useWhiteShaderFlash", true);
                SetFloatField(hb, "whiteHitFlashDuration", 0.048f);
            }

            if (isFlying)
            {
                MossmirAI mmAi = root.AddComponent<MossmirAI>();
                SetField(mmAi, "visualTransform", visualTransform);
            }
            else
            {
                MossgrubAI mgAi = root.AddComponent<MossgrubAI>();
                SetField(mgAi, "visualTransform", visualTransform);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildBellhartExtendedPrefab(
            BuiltAssets assets,
            string prefabPath,
            string name,
            BellhartExtendedArchetype archetype,
            string spritePath,
            EnemyStatsData statsData,
            bool isFlying,
            bool isMossyCreature,
            bool hasTopSpikyArmor,
            float topArmorHeightOffset,
            bool isBoneCarapaceCreature,
            Vector2 colliderSize,
            Vector2 hurtSize,
            int contactDamage,
            float contactKnockback)
        {
            GameObject root;
            Transform visualTransform;
            EnemyController ec;
            CreateBiomeEnemyChassis(
                assets, name, spritePath, statsData, isFlying, isMossyCreature, hasTopSpikyArmor, topArmorHeightOffset,
                colliderSize, hurtSize, contactDamage, contactKnockback,
                out root, out visualTransform, out ec);

            if (isBoneCarapaceCreature)
            {
                SetBoolField(ec, "isBoneCarapaceCreature", true);
            }

            BellhartExtendedAI ai = root.AddComponent<BellhartExtendedAI>();
            ai.ConfigureArchetype(archetype);
            SetField(ai, "visualTransform", visualTransform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildCheckpointPrefab()
        {
            GameObject go = new GameObject("CheckpointShrine");
            go.tag = GameConstants.TagCheckpoint;
            go.layer = GameConstants.LayerInteractable;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_CheckpointShrine.png");
            sr.sortingLayerName = GameConstants.SortInteractables;
            sr.sortingOrder = 2;

            GameObject glow = new GameObject("LanternGlow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0.95f, 1.65f, 0f);
            glow.transform.localScale = Vector3.one * 1.4f;
            SpriteRenderer gsr = glow.AddComponent<SpriteRenderer>();
            gsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SoftParticle.png");
            gsr.color = new Color(0.45f, 1f, 0.85f, 0.5f);
            gsr.sortingLayerName = GameConstants.SortInteractables;
            gsr.sortingOrder = 3;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 1.0f);
            col.size = new Vector2(2.6f, 2.2f);

            CheckpointShrine shrine = go.AddComponent<CheckpointShrine>();
            SetField(shrine, "glowRenderer", gsr);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/CheckpointShrine.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildAbilityPedestalPrefab()
        {
            GameObject go = new GameObject("AbilityPedestal");
            go.tag = GameConstants.TagInteractable;
            go.layer = GameConstants.LayerInteractable;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_AbilityPedestal.png");
            sr.sortingLayerName = GameConstants.SortInteractables;
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 1.2f);
            col.size = new Vector2(2.2f, 2.5f);

            AbilityPedestal ped = go.AddComponent<AbilityPedestal>();
            SetField(ped, "spriteRenderer", sr);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/AbilityPedestal.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildUpgradeAltarPrefab()
        {
            GameObject go = new GameObject("UpgradeAltar");
            go.tag = GameConstants.TagInteractable;
            go.layer = GameConstants.LayerInteractable;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_UpgradeAltar.png");
            sr.sortingLayerName = GameConstants.SortInteractables;
            sr.sortingOrder = 2;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 1.2f);
            col.size = new Vector2(2.4f, 2.5f);

            go.AddComponent<UpgradeAltar>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/UpgradeAltar.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildSilkOrbPrefab()
        {
            GameObject go = new GameObject("SilkOrbPickup");
            go.transform.localScale = Vector3.one * 0.45f;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SilkOrb.png");
            sr.sortingLayerName = GameConstants.SortInteractables;
            sr.sortingOrder = 5;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.7f;
            go.AddComponent<SilkOrbPickup>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/SilkOrbPickup.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildInventoryPickupPrefab()
        {
            GameObject go = new GameObject("InventoryPickup");
            go.transform.localScale = Vector3.one * 0.56f;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Item_EssenceShard.png");
            sr.sortingLayerName = GameConstants.SortInteractables;
            sr.sortingOrder = 6;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;
            InventoryPickup pickup = go.AddComponent<InventoryPickup>();
            SetField(pickup, "spriteRenderer", sr);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/InventoryPickup.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildBreakableWallPrefab()
        {
            GameObject go = new GameObject("BreakableVineWall");
            go.tag = GameConstants.TagBreakable;
            go.layer = GameConstants.LayerBreakableWall;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BreakableVines.png");
            sr.sortingLayerName = GameConstants.SortEnvironment;
            sr.sortingOrder = 4;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.45f, 3.2f);

            BreakableWall bw = go.AddComponent<BreakableWall>();
            SetField(bw, "spriteRenderer", sr);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/BreakableVineWall.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildSpikeHazardPrefab()
        {
            GameObject go = new GameObject("SpikeHazard");
            go.tag = GameConstants.TagHazard;
            go.layer = GameConstants.LayerHazard;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_SpikeThorns.png");
            sr.sortingLayerName = GameConstants.SortEnvironment;
            sr.sortingOrder = 4;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 0.48f);
            col.size = new Vector2(1.8f, 0.85f);

            go.AddComponent<HazardZone>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Environment/SpikeHazard.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        public static void SetField(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void SetBoolField(Object target, string fieldName, bool value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void SetFloatField(Object target, string fieldName, float value)
        {
            if (target == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void SetObjectList<T>(Object target, string fieldName, List<T> items) where T : Object
        {
            if (target == null || items == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = items.Count;
                for (int i = 0; i < items.Count; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void SetObjectArray<T>(Object target, string fieldName, T[] items) where T : Object
        {
            if (target == null || items == null) return;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
