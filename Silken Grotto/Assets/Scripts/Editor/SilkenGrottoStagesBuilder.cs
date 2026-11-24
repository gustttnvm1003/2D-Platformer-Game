#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SilkenGrotto.Editor
{
    /// <summary>
    /// Modular HD builder for all 8 progression stages (00 Moss Grotto .. 07 Aula Choral Chambers):
    /// - Builds all 8 StageConfigData ScriptableObjects with dedicated Cliff/Platform/Gate/Arch/Ceiling/Frame art
    /// - Wires camera-anchored DynamicWeatherController with reduced particle rates + 3 scrolling volumetric storm fog bands
    /// - Populates every stage with Moss-Grotto-parity multi-depth visuals: visible ceilings & dripping water,
    ///   support arches beneath bridges, reflective ground pools, curated biome architecture/flora/houses/organs/bells,
    ///   living AmbientFaunaSwarm moth/scarab/frost clusters, biome-tinted combat fauna, and bidirectional gates.
    /// </summary>
    public static class SilkenGrottoStagesBuilder
    {
        public const string StageFolder = "Assets/ScriptableObjects/Stages";
        private const string ArtStages = "Assets/Art/Environments/Stages/";
        private const string ArtEnv = "Assets/Art/Environments/";
        private const string ArtBg = "Assets/Art/Backgrounds/";
        private const string ArtMusic = "Assets/Audio/Music/";

        public static readonly string[] StageBgmFiles = new string[]
        {
            "BGM_MossGrotto.wav",
            "BGM_WeavenestAtla.wav",
            "BGM_Shellwood.wav",
            "BGM_SandsOfKarak.wav",
            "BGM_Bellhart.wav",
            "BGM_MountFay.wav",
            "BGM_GrandGateCourtyard.wav",
            "BGM_AulaChoralChambers.wav"
        };

        public static AudioClip LoadStageBgm(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= StageBgmFiles.Length) return null;
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ArtMusic + StageBgmFiles[stageIndex]);
        }

        private static readonly string[] StageAssetFiles = new string[]
        {
            "Stage_00_MossGrotto.asset",
            "Stage_01_WeavenestAtla.asset",
            "Stage_02_Shellwood.asset",
            "Stage_03_SandsOfKarak.asset",
            "Stage_04_Bellhart.asset",
            "Stage_05_MountFay.asset",
            "Stage_06_GrandGateCourtyard.asset",
            "Stage_07_AulaChoralChambers.asset"
        };

        public static List<StageConfigData> LoadAllStageConfigsFromDisk()
        {
            List<StageConfigData> list = new List<StageConfigData>();
            for (int i = 0; i < StageAssetFiles.Length; i++)
            {
                StageConfigData cfg = AssetDatabase.LoadAssetAtPath<StageConfigData>(StageFolder + "/" + StageAssetFiles[i]);
                if (cfg != null) list.Add(cfg);
            }
            return list;
        }

        public static void CreateAllStageConfigAssets(SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            if (!AssetDatabase.IsValidFolder(StageFolder))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Stages");
            }

            assets.AllStageConfigs.Clear();
            assets.StageById.Clear();

            // -----------------------------------------------------------------
            // STAGE 0: Prolog — Moss Grotto
            // -----------------------------------------------------------------
            StageConfigData s0 = CreateStageAsset("Stage_00_MossGrotto.asset");
            s0.stageId = "stage_00_moss_grotto";
            s0.stageIndex = 0;
            s0.displayName = "Moss Grotto";
            s0.subtitle = "Overgrown Sanctuary";
            s0.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBg + "BG_L0_MossBackdrop.png");
            s0.cliffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_MossGroundTile.png");
            s0.platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_StoneBridgeWalkway.png");
            s0.supportArchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_RootSupportArch.png");
            s0.ceilingStalactiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_StalactiteVines.png");
            s0.foregroundFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBg + "FG_L5_SilhouetteFraming.png");
            s0.foregroundFrameTint = Color.white;
            s0.exitGateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Gate_MossyCaveMouth.png");
            s0.entryGateSprite = s0.exitGateSprite;
            s0.cameraClearColor = new Color(0.063f, 0.141f, 0.118f, 1f);
            s0.godRayTint = new Color(0.92f, 1.0f, 0.78f, 0.28f);
            s0.showGodRays = true;
            s0.weatherType = WeatherProfileType.MossGrottoSunSpores;
            s0.useDarkStoneAcoustics = false;
            s0.stageBgm = LoadStageBgm(0);
            s0.originX = 0f;
            s0.stageWidth = 144f;
            s0.groundY = 0f;
            s0.cameraMinBounds = new Vector2(-10.5f, -3.5f);
            s0.cameraMaxBounds = new Vector2(144.5f, 29f);
            s0.leftSpawnOffset = new Vector2(0f, 0.02f);
            s0.rightExitOffset = new Vector2(141.6f, 14.2f);
            s0.exitGateVisual = TransitionGateVisualType.MossyCaveMouth;
            s0.nextStageId = "stage_01_weavenest_atla";
            s0.previousStageId = string.Empty;
            ClearLists(s0);
            RegisterStage(assets, s0);

            // -----------------------------------------------------------------
            // STAGE 1: Weavenest Atla (X: 220 .. 360) — Extended Multi-Vault Loom
            // -----------------------------------------------------------------
            StageConfigData s1 = BuildStageTemplate(
                "Stage_01_WeavenestAtla.asset", "stage_01_weavenest_atla", 1,
                "Weavenest Atla", "Ancient Loom of Forgotten Silk",
                "01_weavenest_atla.jpg", "Cliff_Weavenest.png", "Plat_WeavenestCarved.png",
                ArtEnv + "Env_RootSupportArch.png", ArtEnv + "Env_StalactiteVines.png",
                ArtBg + "FG_CitadelFraming.png", new Color(0.14f, 0.16f, 0.24f, 0.90f),
                "Gate_CaveLightExit.png", "Gate_MossyCaveMouth.png",
                new Color(0.09f, 0.11f, 0.17f, 1f), new Color(0.84f, 0.90f, 1.0f, 0.22f), true,
                WeatherProfileType.WeavenestSilkDust, true, false,
                220f, 140f, 0f,
                TransitionGateVisualType.CaveLightOpening, "stage_02_shellwood",
                TransitionGateVisualType.MossyCaveMouth, "stage_00_moss_grotto");
            AddPlatform(s1, "Step_WestCarved", new Vector2(14.0f, 2.86f), new Vector2(7.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SilkLedge_Low", new Vector2(23.5f, 3.8f), new Vector2(5.6f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SilkLedge_High", new Vector2(32.0f, 6.6f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "WeaverDais_WestMid", new Vector2(42.0f, 2.86f), new Vector2(8.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SilkLedge_GapBridge", new Vector2(53.0f, 4.6f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SecretShelf_Weavenest", new Vector2(64.0f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SecretRoof_Weavenest", new Vector2(64.0f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "LoomBalcony_MidHigh", new Vector2(74.0f, 7.6f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "WeaverCliff_EastMid", new Vector2(85.0f, 2.86f), new Vector2(8.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SilkSpan_East1", new Vector2(96.0f, 4.8f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SilkSpan_EastHigh", new Vector2(105.5f, 7.2f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "WeaverDais_East", new Vector2(116.5f, 3.06f), new Vector2(7.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s1, "SilkLedge_ExitApproach", new Vector2(127.5f, 4.9f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddDestructible(s1, "Spool_Entry", new Vector2(10.0f, 0f), BreakablePropKind.SilkSpool, 1, 1, 6, 7f, false, false, 1.0f);
            AddDestructible(s1, "Cocoon_Step", new Vector2(15.2f, 3.20f), BreakablePropKind.DrySilkCocoon, 1, 1, 7, 8f, false, true, 1.0f);
            AddDestructible(s1, "WebBarrier_WestMid", new Vector2(28.0f, 0f), BreakablePropKind.WebBarricade, 2, 2, 7, 10f, false, false, 1.08f);
            AddDestructible(s1, "Spool_High", new Vector2(32.0f, 6.94f), BreakablePropKind.SilkSpool, 1, 2, 8, 9f, false, true, 0.96f);
            AddDestructible(s1, "Cocoon_WestDais", new Vector2(43.0f, 3.20f), BreakablePropKind.DrySilkCocoon, 2, 2, 7, 9f, false, false, 1.05f);
            AddDestructible(s1, "Spool_SecretTop", new Vector2(64.0f, 10.08f), BreakablePropKind.SilkSpool, 1, 2, 8, 9f, false, false, 1.0f);
            AddDestructible(s1, "WebBarrier_MidEast", new Vector2(79.5f, 0f), BreakablePropKind.WebBarricade, 2, 2, 8, 10f, false, true, 1.08f);
            AddDestructible(s1, "Cocoon_EastMid", new Vector2(86.2f, 3.20f), BreakablePropKind.DrySilkCocoon, 2, 2, 8, 9f, false, false, 1.04f);
            AddDestructible(s1, "Spool_EastHigh", new Vector2(105.5f, 7.54f), BreakablePropKind.SilkSpool, 1, 2, 9, 9f, false, true, 0.96f);
            AddDestructible(s1, "Cocoon_EastDais", new Vector2(117.8f, 3.40f), BreakablePropKind.DrySilkCocoon, 2, 2, 8, 9f, false, false, 1.04f);
            AddDestructible(s1, "WebBarrier_Exit", new Vector2(129.5f, 0f), BreakablePropKind.WebBarricade, 2, 2, 9, 10f, false, true, 1.08f);
            AddDecor(s1, "Weaver_GiantLoomW", new Vector2(17.5f, 0f), "Stages/Decor_WeavenestLoom.png", GameConstants.SortNearBackground, 4, 1.25f, false, new Color(0.86f, 0.90f, 0.98f, 0.95f), false);
            AddDecor(s1, "Weaver_StoneArchMid", new Vector2(31.0f, 0f), "Stages/Decor_WeavenestArch.png", GameConstants.SortNearBackground, 3, 1.12f, false, new Color(0.82f, 0.86f, 0.94f, 0.92f), false);
            AddDecor(s1, "Weaver_MaskTotemW", new Vector2(42.0f, 3.2f), "Stages/Decor_WeavenestTotem.png", GameConstants.SortNearBackground, 5, 1.05f, false, new Color(0.90f, 0.92f, 0.98f, 0.96f), false);
            AddDecor(s1, "Weaver_CenterLoom", new Vector2(74.5f, 0f), "Stages/Decor_WeavenestLoom.png", GameConstants.SortNearBackground, 4, 1.28f, true, new Color(0.86f, 0.90f, 0.98f, 0.95f), false);
            AddDecor(s1, "Weaver_EastArchMid", new Vector2(98.0f, 0f), "Stages/Decor_WeavenestArch.png", GameConstants.SortNearBackground, 3, 1.18f, true, new Color(0.84f, 0.88f, 0.96f, 0.94f), false);
            AddDecor(s1, "Weaver_MaskTotemE", new Vector2(116.5f, 3.4f), "Stages/Decor_WeavenestTotem.png", GameConstants.SortNearBackground, 5, 1.08f, true, new Color(0.90f, 0.92f, 0.98f, 0.96f), false);
            AddDecor(s1, "Weaver_AncientArchExit", new Vector2(133.0f, 0f), "Stages/Decor_WeavenestArch.png", GameConstants.SortNearBackground, 4, 1.18f, false, new Color(0.84f, 0.88f, 0.96f, 0.94f), false);
            AddDecor(s1, "Weaver_FernCluster", new Vector2(8.5f, 0f), "Env_CurledFerns.png", GameConstants.SortNearBackground, 6, 0.72f, false, new Color(0.45f, 0.54f, 0.68f, 0.90f), true);
            AddPool(s1, "Weaver_ReflectPoolA", new Vector2(21.5f, 0f), 5.6f, new Color(0.62f, 0.74f, 0.92f, 0.88f), new Color(0.82f, 0.92f, 1.0f, 0.56f));
            AddPool(s1, "Weaver_ReflectPoolB", new Vector2(50.5f, 0f), 5.6f, new Color(0.62f, 0.74f, 0.92f, 0.88f), new Color(0.82f, 0.92f, 1.0f, 0.56f));
            AddPool(s1, "Weaver_ReflectPoolC", new Vector2(93.5f, 0f), 6.0f, new Color(0.62f, 0.74f, 0.92f, 0.88f), new Color(0.82f, 0.92f, 1.0f, 0.56f));
            AddPool(s1, "Weaver_ReflectPoolD", new Vector2(124.0f, 0f), 5.4f, new Color(0.62f, 0.74f, 0.92f, 0.88f), new Color(0.82f, 0.92f, 1.0f, 0.56f));
            AddAmbientProp(s1, "Web_CanopyA", new Vector2(18f, 11.4f), StageAmbientPropKind.SwayingWeb, 1.25f, false);
            AddAmbientProp(s1, "Web_CanopyB", new Vector2(48f, 11.6f), StageAmbientPropKind.SwayingWeb, 1.35f, true);
            AddAmbientProp(s1, "Web_CanopyC", new Vector2(84f, 11.4f), StageAmbientPropKind.SwayingWeb, 1.30f, false);
            AddAmbientProp(s1, "Web_CanopyD", new Vector2(118f, 11.6f), StageAmbientPropKind.SwayingWeb, 1.35f, true);
            AddFaunaSwarm(s1, "Weavenest_SilkMothsA", new Vector2(17.5f, 2.5f), new Vector2(3.2f, 1.5f), 5, new Color(0.88f, 0.92f, 1.0f, 0.90f), new Color(0.66f, 0.80f, 1.0f, 0.75f));
            AddFaunaSwarm(s1, "Weavenest_SilkMothsB", new Vector2(42.0f, 5.2f), new Vector2(3.4f, 1.6f), 5, new Color(0.90f, 0.95f, 1.0f, 0.90f), new Color(0.70f, 0.84f, 0.98f, 0.75f));
            AddFaunaSwarm(s1, "Weavenest_SilkMothsC", new Vector2(85.0f, 4.8f), new Vector2(3.6f, 1.6f), 5, new Color(0.88f, 0.92f, 1.0f, 0.90f), new Color(0.66f, 0.80f, 1.0f, 0.75f));
            AddFaunaSwarm(s1, "Weavenest_SilkMothsD", new Vector2(116.5f, 5.4f), new Vector2(3.4f, 1.6f), 5, new Color(0.90f, 0.95f, 1.0f, 0.90f), new Color(0.70f, 0.84f, 0.98f, 0.75f));
            RegisterStage(assets, s1);

            // -----------------------------------------------------------------
            // STAGE 2: Shellwood (X: 420 .. 564) — Twin Briar Chasms & Lush Hollow
            // -----------------------------------------------------------------
            StageConfigData s2 = BuildStageTemplate(
                "Stage_02_Shellwood.asset", "stage_02_shellwood", 2,
                "Shellwood", "Bioluminescent Root Hollow",
                "02_shellwood.jpg", "Cliff_Shellwood.png", "Plat_ShellwoodBridge.png",
                ArtEnv + "Env_RootSupportArch.png", ArtEnv + "Env_StalactiteVines.png",
                ArtBg + "FG_L5_SilhouetteFraming.png", new Color(0.18f, 0.16f, 0.28f, 0.92f),
                "Gate_AncientStoneWall.png", "Gate_CaveLightExit.png",
                new Color(0.07f, 0.12f, 0.16f, 1f), new Color(0.68f, 0.56f, 0.98f, 0.22f), true,
                WeatherProfileType.ShellwoodBiolumFog, false, false,
                420f, 144f, 0f,
                TransitionGateVisualType.AncientStoneWallGate, "stage_03_sands_of_karak",
                TransitionGateVisualType.CaveLightOpening, "stage_01_weavenest_atla");
            AddPlatform(s2, "RootCliff_West", new Vector2(14.0f, 2.86f), new Vector2(7.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "FungusBridge_W1", new Vector2(25.0f, 1.5f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "FungusBridge_WHigh", new Vector2(34.5f, 4.6f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "FungusBridge_W2", new Vector2(43.0f, 1.7f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "MidHollow_Terrace", new Vector2(56.5f, 2.86f), new Vector2(8.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "SecretShelf_Shellwood", new Vector2(74.5f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "SecretRoof_Shellwood", new Vector2(74.5f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "CanopyDeck_MidHigh", new Vector2(86.5f, 6.8f), new Vector2(6.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "FungusBridge_E1", new Vector2(99.5f, 1.6f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "FungusBridge_EHigh", new Vector2(109.0f, 4.8f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "FungusBridge_E2", new Vector2(117.5f, 1.8f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "RootCliff_East", new Vector2(128.0f, 3.16f), new Vector2(7.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s2, "CanopyBranch_East", new Vector2(135.0f, 6.4f), new Vector2(5.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddHazard(s2, "Shellwood_BriarPitW1", new Vector2(26.5f, -2.2f), 4.4f);
            AddHazard(s2, "Shellwood_BriarPitW2", new Vector2(35.0f, -2.2f), 4.6f);
            AddHazard(s2, "Shellwood_BriarPitW3", new Vector2(43.0f, -2.2f), 4.4f);
            AddHazard(s2, "Shellwood_BriarPitE1", new Vector2(101.0f, -2.2f), 4.4f);
            AddHazard(s2, "Shellwood_BriarPitE2", new Vector2(110.0f, -2.2f), 4.8f);
            AddHazard(s2, "Shellwood_BriarPitE3", new Vector2(118.0f, -2.2f), 4.2f);
            AddDestructible(s2, "Shell_West", new Vector2(9.5f, 0f), BreakablePropKind.HollowBugShell, 1, 1, 7, 7f, false, false, 1.04f);
            AddDestructible(s2, "WoodPillar_WestCliff", new Vector2(15.6f, 3.20f), BreakablePropKind.FragileWoodPillar, 2, 2, 7, 8f, false, false, 1.0f);
            AddDestructible(s2, "Briar_WHigh", new Vector2(34.5f, 4.94f), BreakablePropKind.BriarBramble, 2, 2, 8, 9f, false, false, 0.96f);
            AddDestructible(s2, "Shell_MidHollow", new Vector2(58.0f, 3.20f), BreakablePropKind.HollowBugShell, 1, 2, 8, 8f, false, true, 1.02f);
            AddDestructible(s2, "WoodPillar_SecretTop", new Vector2(74.5f, 10.08f), BreakablePropKind.FragileWoodPillar, 2, 2, 8, 9f, false, true, 1.0f);
            AddDestructible(s2, "Briar_MidEast", new Vector2(91.5f, 0f), BreakablePropKind.BriarBramble, 2, 2, 8, 9f, false, false, 1.04f);
            AddDestructible(s2, "Briar_EHigh", new Vector2(109.0f, 5.14f), BreakablePropKind.BriarBramble, 2, 2, 8, 9f, false, true, 0.96f);
            AddDestructible(s2, "Shell_East", new Vector2(126.2f, 3.50f), BreakablePropKind.HollowBugShell, 1, 2, 8, 8f, false, true, 1.02f);
            AddDestructible(s2, "WoodPillar_East", new Vector2(135.4f, 0f), BreakablePropKind.FragileWoodPillar, 2, 2, 8, 9f, false, true, 1.05f);
            AddDecor(s2, "Shellwood_BiolumFernW", new Vector2(8.0f, 0f), "Stages/Decor_ShellwoodBiolumFern.png", GameConstants.SortNearBackground, 6, 0.85f, false, Color.white, true);
            AddDecor(s2, "Shellwood_GlowShroomsW", new Vector2(12.8f, 3.20f), "Stages/Decor_ShellwoodGlowShrooms.png", GameConstants.SortNearBackground, 5, 0.88f, true, Color.white, false);
            AddDecor(s2, "Shellwood_StoneArchPitW", new Vector2(34.5f, -2.2f), "Env_CarvedStoneArch.png", GameConstants.SortNearBackground, 4, 1.15f, false, new Color(0.56f, 0.62f, 0.78f, 0.90f), false);
            AddDecor(s2, "Shellwood_BiolumFernMid", new Vector2(64.5f, 0f), "Stages/Decor_ShellwoodBiolumFern.png", GameConstants.SortNearBackground, 6, 0.92f, false, Color.white, true);
            AddDecor(s2, "Shellwood_GlowShroomsMid", new Vector2(89.0f, 0f), "Stages/Decor_ShellwoodGlowShrooms.png", GameConstants.SortNearBackground, 5, 0.96f, true, Color.white, false);
            AddDecor(s2, "Shellwood_StoneArchPitE", new Vector2(109.0f, -2.2f), "Env_CarvedStoneArch.png", GameConstants.SortNearBackground, 4, 1.15f, true, new Color(0.56f, 0.62f, 0.78f, 0.90f), false);
            AddDecor(s2, "Shellwood_GlowShroomsE", new Vector2(129.0f, 3.5f), "Stages/Decor_ShellwoodGlowShrooms.png", GameConstants.SortNearBackground, 5, 0.92f, false, Color.white, false);
            AddDecor(s2, "Shellwood_BiolumFernE", new Vector2(137.2f, 0f), "Stages/Decor_ShellwoodBiolumFern.png", GameConstants.SortNearBackground, 6, 0.92f, true, Color.white, true);
            AddPool(s2, "Shellwood_VioletBogW", new Vector2(17.5f, 0f), 4.8f, new Color(0.52f, 0.44f, 0.92f, 0.90f), new Color(0.74f, 0.66f, 1.0f, 0.60f));
            AddPool(s2, "Shellwood_VioletBogMid", new Vector2(51.5f, 0f), 5.6f, new Color(0.52f, 0.44f, 0.92f, 0.90f), new Color(0.74f, 0.66f, 1.0f, 0.60f));
            AddPool(s2, "Shellwood_VioletBogEastMid", new Vector2(88.0f, 0f), 5.4f, new Color(0.52f, 0.44f, 0.92f, 0.90f), new Color(0.74f, 0.66f, 1.0f, 0.60f));
            AddPool(s2, "Shellwood_VioletBogE", new Vector2(127.5f, 0f), 5.0f, new Color(0.52f, 0.44f, 0.92f, 0.90f), new Color(0.74f, 0.66f, 1.0f, 0.60f));
            AddFaunaSwarm(s2, "Shellwood_FirefliesW", new Vector2(13.8f, 4.2f), new Vector2(3.4f, 1.6f), 6, new Color(0.78f, 0.52f, 1.0f, 0.92f), new Color(0.45f, 0.92f, 1.0f, 0.84f));
            AddFaunaSwarm(s2, "Shellwood_FirefliesMid", new Vector2(57.0f, 4.8f), new Vector2(3.8f, 1.8f), 6, new Color(0.78f, 0.52f, 1.0f, 0.92f), new Color(0.45f, 0.92f, 1.0f, 0.84f));
            AddFaunaSwarm(s2, "Shellwood_FirefliesE", new Vector2(128.0f, 4.8f), new Vector2(3.4f, 1.6f), 6, new Color(0.78f, 0.52f, 1.0f, 0.92f), new Color(0.45f, 0.92f, 1.0f, 0.84f));
            RegisterStage(assets, s2);

            // -----------------------------------------------------------------
            // STAGE 3: Sands of Karak (X: 620 .. 764) — Extended Coral Desert Wastes
            // -----------------------------------------------------------------
            StageConfigData s3 = BuildStageTemplate(
                "Stage_03_SandsOfKarak.asset", "stage_03_sands_of_karak", 3,
                "Sands of Karak", "Windswept Coral Wastes",
                "03_sands_of_karak.jpg", "Cliff_Karak.png", "Plat_KarakCoral.png",
                ArtEnv + "Env_RootSupportArch.png", ArtEnv + "Env_StalactiteVines.png",
                ArtBg + "FG_CitadelFraming.png", new Color(0.24f, 0.16f, 0.10f, 0.90f),
                "Gate_SandWallBreach.png", "Gate_AncientStoneWall.png",
                new Color(0.24f, 0.16f, 0.10f, 1f), new Color(0.98f, 0.80f, 0.50f, 0.24f), true,
                WeatherProfileType.KarakSandstorm, true, false,
                620f, 144f, 0f,
                TransitionGateVisualType.RuinedSandWallGate, "stage_04_bellhart",
                TransitionGateVisualType.AncientStoneWallGate, "stage_02_shellwood");
            AddPlatform(s3, "DesertCliff_1", new Vector2(14.5f, 2.86f), new Vector2(7.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "CoralShelf_1", new Vector2(25.0f, 4.4f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "CoralShelf_2", new Vector2(35.0f, 6.8f), new Vector2(6.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "DesertCliff_WestMid", new Vector2(45.5f, 2.86f), new Vector2(8.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "CoralSpan_Mid", new Vector2(58.0f, 5.0f), new Vector2(6.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "SecretShelf_Karak", new Vector2(74.0f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "SecretRoof_Karak", new Vector2(74.0f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "WindMesa_High", new Vector2(86.0f, 7.2f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "DesertCliff_EastMid", new Vector2(96.5f, 3.16f), new Vector2(8.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "CoralShelf_East1", new Vector2(108.5f, 5.0f), new Vector2(5.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "CoralShelf_EastHigh", new Vector2(118.5f, 7.4f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s3, "DesertCliff_Gate", new Vector2(128.5f, 2.86f), new Vector2(7.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddDestructible(s3, "Coral_West", new Vector2(9.6f, 0f), BreakablePropKind.DryCoralFormation, 1, 1, 7, 7f, false, false, 1.05f);
            AddDestructible(s3, "Gear_Cliff", new Vector2(15.5f, 3.20f), BreakablePropKind.RustedGear, 2, 2, 8, 8f, false, false, 1.0f);
            AddDestructible(s3, "Bones_CenterW", new Vector2(29.0f, 0f), BreakablePropKind.AncientBonePile, 1, 2, 7, 6f, false, true, 1.05f);
            AddDestructible(s3, "Coral_HighW", new Vector2(35.0f, 7.14f), BreakablePropKind.DryCoralFormation, 2, 2, 9, 9f, false, true, 0.96f);
            AddDestructible(s3, "Gear_MidW", new Vector2(46.8f, 3.20f), BreakablePropKind.RustedGear, 2, 2, 8, 8f, false, true, 0.96f);
            AddDestructible(s3, "Coral_SecretTop", new Vector2(74.0f, 10.08f), BreakablePropKind.DryCoralFormation, 2, 2, 9, 9f, false, false, 1.02f);
            AddDestructible(s3, "Bones_MidEast", new Vector2(89.0f, 0f), BreakablePropKind.AncientBonePile, 1, 2, 8, 7f, false, true, 1.04f);
            AddDestructible(s3, "Gear_EastCliff", new Vector2(98.0f, 3.50f), BreakablePropKind.RustedGear, 2, 2, 8, 8f, false, false, 1.0f);
            AddDestructible(s3, "Coral_EastGate", new Vector2(129.5f, 3.20f), BreakablePropKind.DryCoralFormation, 2, 2, 9, 9f, false, true, 0.98f);
            AddDestructible(s3, "Bones_Exit", new Vector2(134.8f, 0f), BreakablePropKind.AncientBonePile, 1, 2, 8, 7f, false, false, 1.02f);
            AddDecor(s3, "Karak_GiantCoralW", new Vector2(20.5f, 0f), "Stages/Decor_KarakGiantCoral.png", GameConstants.SortNearBackground, 4, 1.15f, false, new Color(0.96f, 0.88f, 0.80f, 0.96f), true);
            AddDecor(s3, "Karak_SandArchW", new Vector2(36.0f, 0f), "Stages/Decor_KarakSandArch.png", GameConstants.SortNearBackground, 3, 1.25f, false, new Color(0.92f, 0.84f, 0.72f, 0.94f), false);
            AddDecor(s3, "Karak_GiantCoralMid", new Vector2(61.5f, 0f), "Stages/Decor_KarakGiantCoral.png", GameConstants.SortNearBackground, 4, 1.24f, true, new Color(0.96f, 0.88f, 0.80f, 0.96f), true);
            AddDecor(s3, "Karak_SandArchE", new Vector2(104.5f, 0f), "Stages/Decor_KarakSandArch.png", GameConstants.SortNearBackground, 3, 1.28f, true, new Color(0.92f, 0.84f, 0.72f, 0.94f), false);
            AddDecor(s3, "Karak_GiantCoralE", new Vector2(121.0f, 0f), "Stages/Decor_KarakGiantCoral.png", GameConstants.SortNearBackground, 4, 1.22f, false, new Color(0.96f, 0.88f, 0.80f, 0.96f), true);
            AddDecor(s3, "Karak_ChainA", new Vector2(17.5f, 12.6f), "Stages/Prop_RustedChain.png", GameConstants.SortNearBackground, 5, 1.45f, false, new Color(0.84f, 0.72f, 0.62f, 0.92f), true);
            AddDecor(s3, "Karak_ChainB", new Vector2(56.0f, 12.6f), "Stages/Prop_RustedChain.png", GameConstants.SortNearBackground, 5, 1.55f, true, new Color(0.84f, 0.72f, 0.62f, 0.92f), true);
            AddDecor(s3, "Karak_ChainC", new Vector2(96.0f, 12.6f), "Stages/Prop_RustedChain.png", GameConstants.SortNearBackground, 5, 1.45f, false, new Color(0.84f, 0.72f, 0.62f, 0.92f), true);
            AddDecor(s3, "Karak_ChainD", new Vector2(126.0f, 12.6f), "Stages/Prop_RustedChain.png", GameConstants.SortNearBackground, 5, 1.55f, true, new Color(0.84f, 0.72f, 0.62f, 0.92f), true);
            AddFaunaSwarm(s3, "Karak_SandScarabsA", new Vector2(21.0f, 2.4f), new Vector2(3.6f, 1.4f), 5, new Color(1.0f, 0.78f, 0.42f, 0.88f), new Color(0.94f, 0.56f, 0.34f, 0.75f));
            AddFaunaSwarm(s3, "Karak_SandScarabsMid", new Vector2(62.0f, 2.6f), new Vector2(3.8f, 1.6f), 5, new Color(1.0f, 0.78f, 0.42f, 0.88f), new Color(0.94f, 0.56f, 0.34f, 0.75f));
            AddFaunaSwarm(s3, "Karak_SandScarabsB", new Vector2(120.5f, 2.6f), new Vector2(3.6f, 1.4f), 5, new Color(1.0f, 0.78f, 0.42f, 0.88f), new Color(0.94f, 0.56f, 0.34f, 0.75f));
            RegisterStage(assets, s3);

            // -----------------------------------------------------------------
            // STAGE 4: Bellhart (X: 820 .. 968) — Extended Bell Town & Damp Caves
            // -----------------------------------------------------------------
            StageConfigData s4 = BuildStageTemplate(
                "Stage_04_Bellhart.asset", "stage_04_bellhart", 4,
                "Bellhart", "Silent Town of Hanging Bells",
                "04_bellhart.jpg", "Cliff_Bellhart.png", "Plat_BellhartRoof.png",
                ArtStages + "Env_CitadelSupportArch.png", ArtEnv + "Env_StalactiteVines.png",
                ArtBg + "FG_CitadelFraming.png", new Color(0.14f, 0.17f, 0.26f, 0.92f),
                "Gate_BellhartIronGate.png", "Gate_SandWallBreach.png",
                new Color(0.10f, 0.13f, 0.20f, 1f), new Color(0.76f, 0.84f, 0.96f, 0.16f), false,
                WeatherProfileType.BellhartColdMist, true, false,
                820f, 148f, 0f,
                TransitionGateVisualType.OldIronGate, "stage_05_mount_fay",
                TransitionGateVisualType.RuinedSandWallGate, "stage_03_sands_of_karak");
            AddPlatform(s4, "ShellRoof_West", new Vector2(15.0f, 2.96f), new Vector2(7.6f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "TownBridge_W", new Vector2(26.0f, 4.4f), new Vector2(6.6f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "BellTower_WestDeck", new Vector2(37.0f, 6.6f), new Vector2(6.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "ShellRoof_MidW", new Vector2(48.5f, 3.16f), new Vector2(8.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "PlazaArchway_Mid", new Vector2(61.0f, 5.0f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "SecretShelf_BellVault", new Vector2(77.0f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "SecretRoof_BellVault", new Vector2(77.0f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "GreatBell_Belfry", new Vector2(90.0f, 7.0f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "ShellRoof_MidE", new Vector2(101.5f, 3.16f), new Vector2(7.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "TownBridge_E", new Vector2(113.5f, 4.8f), new Vector2(6.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "BellTower_EastDeck", new Vector2(124.5f, 6.8f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s4, "ShellRoof_East", new Vector2(134.5f, 2.96f), new Vector2(7.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddDestructible(s4, "Bell_Entry", new Vector2(10.2f, 0f), BreakablePropKind.CrackedBell, 2, 2, 8, 8f, false, false, 1.02f);
            AddDestructible(s4, "Crate_RoofW", new Vector2(16.4f, 3.30f), BreakablePropKind.TownCrate, 1, 1, 7, 6f, false, false, 0.98f);
            AddDestructible(s4, "Lantern_BridgeW", new Vector2(26.6f, 4.74f), BreakablePropKind.OldLantern, 1, 1, 7, 7f, false, false, 0.95f);
            AddDestructible(s4, "Bell_MidW", new Vector2(49.8f, 3.50f), BreakablePropKind.CrackedBell, 2, 2, 9, 9f, false, true, 1.06f);
            AddDestructible(s4, "Crate_Plaza", new Vector2(58.5f, 0f), BreakablePropKind.TownCrate, 1, 2, 8, 7f, false, false, 1.0f);
            AddDestructible(s4, "Bell_SecretTop", new Vector2(77.0f, 10.08f), BreakablePropKind.CrackedBell, 2, 2, 9, 9f, false, false, 1.04f);
            AddDestructible(s4, "Lantern_MidE", new Vector2(102.8f, 3.5f), BreakablePropKind.OldLantern, 1, 2, 8, 7f, false, true, 0.96f);
            AddDestructible(s4, "Crate_East", new Vector2(135.8f, 3.3f), BreakablePropKind.TownCrate, 1, 2, 8, 7f, false, true, 0.96f);
            AddDestructible(s4, "Bell_Gate", new Vector2(140.2f, 0f), BreakablePropKind.CrackedBell, 2, 2, 9, 9f, false, false, 1.0f);
            AddDecor(s4, "Bellhart_HouseW", new Vector2(19.5f, 0f), "Stages/Decor_BellhartHouseA.png", GameConstants.SortNearBackground, 4, 1.15f, false, new Color(0.92f, 0.94f, 0.98f, 0.95f), false);
            AddDecor(s4, "Bellhart_PillarW", new Vector2(31.5f, 0f), "Env_StonePillar.png", GameConstants.SortNearBackground, 3, 1.15f, false, new Color(0.62f, 0.68f, 0.78f, 0.92f), false);
            AddDecor(s4, "Bellhart_HouseMidW", new Vector2(55.0f, 0f), "Stages/Decor_BellhartHouseB.png", GameConstants.SortNearBackground, 4, 1.18f, false, new Color(0.92f, 0.94f, 0.98f, 0.95f), false);
            AddDecor(s4, "Bellhart_HouseMidE", new Vector2(93.5f, 0f), "Stages/Decor_BellhartHouseA.png", GameConstants.SortNearBackground, 4, 1.18f, true, new Color(0.92f, 0.94f, 0.98f, 0.95f), false);
            AddDecor(s4, "Bellhart_PillarE", new Vector2(110.5f, 0f), "Env_StonePillar.png", GameConstants.SortNearBackground, 3, 1.18f, true, new Color(0.62f, 0.68f, 0.78f, 0.92f), false);
            AddDecor(s4, "Bellhart_HouseE", new Vector2(126.5f, 0f), "Stages/Decor_BellhartHouseB.png", GameConstants.SortNearBackground, 4, 1.15f, false, new Color(0.92f, 0.94f, 0.98f, 0.95f), false);
            AddDecor(s4, "Bellhart_StreetLampA", new Vector2(12.2f, 0f), "Stages/Prop_OldLantern.png", GameConstants.SortNearBackground, 6, 1.05f, false, Color.white, false);
            AddDecor(s4, "Bellhart_StreetLampMid", new Vector2(64.5f, 0f), "Stages/Prop_OldLantern.png", GameConstants.SortNearBackground, 6, 1.05f, true, Color.white, false);
            AddDecor(s4, "Bellhart_StreetLampB", new Vector2(138.8f, 0f), "Stages/Prop_OldLantern.png", GameConstants.SortNearBackground, 6, 1.05f, true, Color.white, false);
            AddAmbientProp(s4, "Bellhart_HangBellW", new Vector2(16.5f, 12.6f), StageAmbientPropKind.HangingBellCluster, 1.18f, false);
            AddAmbientProp(s4, "Bellhart_HangBellMid", new Vector2(61.0f, 12.6f), StageAmbientPropKind.HangingBellCluster, 1.24f, true);
            AddAmbientProp(s4, "Bellhart_HangBellE", new Vector2(118.0f, 12.6f), StageAmbientPropKind.HangingBellCluster, 1.25f, false);
            AddAmbientProp(s4, "Bellhart_LanternGlowA", new Vector2(12.2f, 2.3f), StageAmbientPropKind.WarmLantern, 1.05f, false);
            AddAmbientProp(s4, "Bellhart_LanternGlowB", new Vector2(64.5f, 2.3f), StageAmbientPropKind.WarmLantern, 1.10f, true);
            AddAmbientProp(s4, "Bellhart_LanternGlowC", new Vector2(138.8f, 2.3f), StageAmbientPropKind.WarmLantern, 1.05f, false);
            AddPool(s4, "Bellhart_RainPuddleW", new Vector2(27.5f, 0f), 5.6f, new Color(0.64f, 0.74f, 0.88f, 0.86f), new Color(0.84f, 0.90f, 1.0f, 0.52f));
            AddPool(s4, "Bellhart_RainPuddleMid", new Vector2(62.0f, 0f), 6.0f, new Color(0.64f, 0.74f, 0.88f, 0.86f), new Color(0.84f, 0.90f, 1.0f, 0.52f));
            AddPool(s4, "Bellhart_RainPuddleE", new Vector2(115.5f, 0f), 5.8f, new Color(0.64f, 0.74f, 0.88f, 0.86f), new Color(0.84f, 0.90f, 1.0f, 0.52f));
            AddFaunaSwarm(s4, "Bellhart_LampMothsA", new Vector2(12.2f, 2.6f), new Vector2(2.4f, 1.2f), 5, new Color(1.0f, 0.82f, 0.48f, 0.90f), new Color(0.95f, 0.92f, 0.78f, 0.76f));
            AddFaunaSwarm(s4, "Bellhart_LampMothsMid", new Vector2(64.5f, 2.8f), new Vector2(2.8f, 1.4f), 5, new Color(1.0f, 0.82f, 0.48f, 0.90f), new Color(0.95f, 0.92f, 0.78f, 0.76f));
            AddFaunaSwarm(s4, "Bellhart_LampMothsB", new Vector2(138.8f, 2.6f), new Vector2(2.4f, 1.2f), 5, new Color(1.0f, 0.82f, 0.48f, 0.90f), new Color(0.95f, 0.92f, 0.78f, 0.76f));
            RegisterStage(assets, s4);

            // -----------------------------------------------------------------
            // STAGE 5: Mount Fay (X: 1020 .. 1164) — Extended Frozen Ridge Ascent
            // -----------------------------------------------------------------
            StageConfigData s5 = BuildStageTemplate(
                "Stage_05_MountFay.asset", "stage_05_mount_fay", 5,
                "Mount Fay", "Blizzard Ridge of Frozen Bells",
                "05_mount_fay.jpg", "Cliff_MountFay.png", "Plat_MountFayIce.png",
                ArtStages + "Env_CitadelSupportArch.png", ArtStages + "Decor_MountFayIcicles.png",
                ArtBg + "FG_CitadelFraming.png", new Color(0.18f, 0.26f, 0.36f, 0.92f),
                "Gate_RoyalGrandGate.png", "Gate_BellhartIronGate.png",
                new Color(0.14f, 0.22f, 0.32f, 1f), new Color(0.84f, 0.94f, 1.0f, 0.20f), true,
                WeatherProfileType.MountFayBlizzard, true, true,
                1020f, 144f, 0f,
                TransitionGateVisualType.RoyalGrandGate, "stage_06_grand_gate_courtyard",
                TransitionGateVisualType.OldIronGate, "stage_04_bellhart");
            AddPlatform(s5, "IceCliff_West", new Vector2(14.5f, 2.86f), new Vector2(7.0f, 0.68f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "SnowLedge_1", new Vector2(25.0f, 4.8f), new Vector2(5.8f, 0.72f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "IcePeak_West", new Vector2(36.0f, 7.54f), new Vector2(7.0f, 0.72f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "SnowLedge_Mid1", new Vector2(47.5f, 4.6f), new Vector2(6.0f, 0.72f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "IceRidge_Center", new Vector2(59.0f, 2.86f), new Vector2(8.2f, 0.68f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "SecretShelf_GlacialVault", new Vector2(76.5f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "SecretRoof_GlacialVault", new Vector2(76.5f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "GlacialArch_High", new Vector2(89.0f, 6.8f), new Vector2(6.4f, 0.72f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "IceCliff_EastMid", new Vector2(101.0f, 3.06f), new Vector2(7.8f, 0.68f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "SnowLedge_East", new Vector2(112.5f, 5.2f), new Vector2(6.0f, 0.72f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "IceSummit_East", new Vector2(123.5f, 7.94f), new Vector2(6.6f, 0.72f), StagePlatformKind.SlipperyIceOneWay);
            AddPlatform(s5, "IceStep_Exit", new Vector2(133.5f, 2.86f), new Vector2(6.8f, 0.68f), StagePlatformKind.SlipperyIceOneWay);
            AddDestructible(s5, "IceCluster_West", new Vector2(10.0f, 0f), BreakablePropKind.IceCrystalCluster, 2, 2, 8, 8f, false, false, 1.08f);
            AddDestructible(s5, "FrozenPost_CliffW", new Vector2(16.0f, 3.20f), BreakablePropKind.FrozenWoodPost, 1, 1, 8, 7f, false, false, 0.96f);
            AddDestructible(s5, "IceCluster_PeakW", new Vector2(36.5f, 7.90f), BreakablePropKind.IceCrystalCluster, 2, 2, 10, 10f, false, false, 1.05f);
            AddDestructible(s5, "IceBarrier_MidW", new Vector2(49.0f, 0f), BreakablePropKind.IceCrystalCluster, 2, 2, 9, 10f, false, true, 1.15f);
            AddDestructible(s5, "FrozenPost_Mid", new Vector2(60.2f, 3.20f), BreakablePropKind.FrozenWoodPost, 1, 2, 8, 8f, false, false, 1.0f);
            AddDestructible(s5, "IceCluster_SecretTop", new Vector2(76.5f, 10.08f), BreakablePropKind.IceCrystalCluster, 2, 2, 10, 10f, false, true, 1.06f);
            AddDestructible(s5, "IceBarrier_EastMid", new Vector2(102.5f, 3.40f), BreakablePropKind.IceCrystalCluster, 2, 2, 9, 9f, false, false, 1.04f);
            AddDestructible(s5, "FrozenPost_SummitE", new Vector2(124.2f, 8.30f), BreakablePropKind.FrozenWoodPost, 1, 2, 8, 8f, false, true, 0.98f);
            AddDestructible(s5, "IceCluster_Exit", new Vector2(135.2f, 3.20f), BreakablePropKind.IceCrystalCluster, 2, 2, 9, 9f, false, false, 1.02f);
            AddDecor(s5, "Fay_RootsW", new Vector2(8.0f, 0f), "Stages/Decor_MountFayRoots.png", GameConstants.SortNearBackground, 4, 1.10f, false, Color.white, false);
            AddDecor(s5, "Fay_BellArchW", new Vector2(27.5f, 0f), "Stages/Decor_MountFayBellArch.png", GameConstants.SortNearBackground, 3, 1.30f, false, new Color(0.92f, 0.96f, 1.0f, 0.94f), false);
            AddDecor(s5, "Fay_BellArchMid", new Vector2(67.0f, 0f), "Stages/Decor_MountFayBellArch.png", GameConstants.SortNearBackground, 3, 1.32f, true, new Color(0.92f, 0.96f, 1.0f, 0.94f), false);
            AddDecor(s5, "Fay_RootsMidE", new Vector2(105.0f, 0f), "Stages/Decor_MountFayRoots.png", GameConstants.SortNearBackground, 4, 1.15f, true, Color.white, false);
            AddDecor(s5, "Fay_RootsE", new Vector2(137.0f, 0f), "Stages/Decor_MountFayRoots.png", GameConstants.SortNearBackground, 4, 1.15f, false, Color.white, false);
            AddDecor(s5, "Fay_TrailPostA", new Vector2(11.0f, 0f), "Stages/Prop_FrozenPost.png", GameConstants.SortNearBackground, 6, 1.05f, false, Color.white, false);
            AddDecor(s5, "Fay_TrailPostB", new Vector2(63.5f, 0f), "Stages/Prop_FrozenPost.png", GameConstants.SortNearBackground, 6, 1.05f, false, Color.white, false);
            AddDecor(s5, "Fay_TrailPostC", new Vector2(136.0f, 0f), "Stages/Prop_FrozenPost.png", GameConstants.SortNearBackground, 6, 1.05f, true, Color.white, false);
            AddAmbientProp(s5, "Fay_LanternA", new Vector2(11.0f, 2.2f), StageAmbientPropKind.WarmLantern, 1.15f, false);
            AddAmbientProp(s5, "Fay_LanternMid", new Vector2(63.5f, 2.2f), StageAmbientPropKind.WarmLantern, 1.18f, true);
            AddAmbientProp(s5, "Fay_LanternB", new Vector2(89.0f, 9.2f), StageAmbientPropKind.WarmLantern, 1.15f, true);
            AddAmbientProp(s5, "Fay_LanternC", new Vector2(136.0f, 2.2f), StageAmbientPropKind.WarmLantern, 1.15f, false);
            AddFaunaSwarm(s5, "Fay_FrostWispsA", new Vector2(11.2f, 2.8f), new Vector2(2.6f, 1.2f), 4, new Color(0.74f, 0.94f, 1.0f, 0.90f), new Color(1.0f, 0.86f, 0.60f, 0.78f));
            AddFaunaSwarm(s5, "Fay_FrostWispsMid", new Vector2(63.5f, 3.0f), new Vector2(2.8f, 1.4f), 4, new Color(0.74f, 0.94f, 1.0f, 0.90f), new Color(1.0f, 0.86f, 0.60f, 0.78f));
            AddFaunaSwarm(s5, "Fay_FrostWispsB", new Vector2(136.0f, 2.8f), new Vector2(2.6f, 1.2f), 4, new Color(0.74f, 0.94f, 1.0f, 0.90f), new Color(1.0f, 0.86f, 0.60f, 0.78f));
            RegisterStage(assets, s5);

            // -----------------------------------------------------------------
            // STAGE 6: Grand Gate & Courtyard (X: 1220 .. 1360) — Royal Citadel Cloisters
            // -----------------------------------------------------------------
            StageConfigData s6 = BuildStageTemplate(
                "Stage_06_GrandGateCourtyard.asset", "stage_06_grand_gate_courtyard", 6,
                "Grand Gate & Courtyard", "Outer Citadel of the Choral Chambers",
                "06_grand_gate_courtyard.jpg", "Cliff_Courtyard.png", "Plat_CourtyardStone.png",
                ArtStages + "Env_CitadelSupportArch.png", ArtStages + "Decor_MountFayIcicles.png",
                ArtBg + "FG_CitadelFraming.png", new Color(0.16f, 0.20f, 0.28f, 0.90f),
                "Gate_ChoralGrandDoors.png", "Gate_RoyalGrandGate.png",
                new Color(0.18f, 0.22f, 0.28f, 1f), new Color(0.88f, 0.92f, 0.98f, 0.18f), true,
                WeatherProfileType.CourtyardLightSnowWind, true, false,
                1220f, 140f, 0f,
                TransitionGateVisualType.GrandInteriorDoors, "stage_07_aula_choral_chambers",
                TransitionGateVisualType.RoyalGrandGate, "stage_05_mount_fay");
            AddPlatform(s6, "PalaceStep_1", new Vector2(13.5f, 2.86f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "PalaceBalcony_W", new Vector2(24.5f, 4.4f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "RuinPillar_West", new Vector2(36.0f, 6.8f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "GrandLanding_Mid", new Vector2(48.0f, 2.86f), new Vector2(8.4f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "PalaceBalcony_Mid", new Vector2(60.5f, 4.6f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "SecretShelf_CloisterVault", new Vector2(74.5f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "SecretRoof_CloisterVault", new Vector2(74.5f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "CloisterTerrace_E", new Vector2(88.5f, 6.6f), new Vector2(6.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "PalaceStep_EastMid", new Vector2(100.0f, 2.86f), new Vector2(8.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "PalaceBalcony_E", new Vector2(112.5f, 4.6f), new Vector2(7.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s6, "GrandThreshold", new Vector2(126.0f, 2.86f), new Vector2(7.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddDestructible(s6, "Statue_West", new Vector2(12.0f, 3.20f), BreakablePropKind.CrackedSentinelStatue, 2, 2, 9, 9f, false, false, 1.05f);
            AddDestructible(s6, "GildedUrn_BalcW", new Vector2(25.5f, 4.74f), BreakablePropKind.GildedPalaceUrn, 1, 2, 9, 8f, false, false, 1.0f);
            AddDestructible(s6, "Chain_WestMid", new Vector2(32.5f, 0f), BreakablePropKind.RustedChain, 1, 2, 8, 7f, false, false, 1.05f);
            AddDestructible(s6, "Statue_MidW", new Vector2(49.5f, 3.20f), BreakablePropKind.CrackedSentinelStatue, 2, 2, 10, 10f, false, true, 1.08f);
            AddDestructible(s6, "GildedUrn_BalcMid", new Vector2(61.5f, 4.94f), BreakablePropKind.GildedPalaceUrn, 1, 2, 10, 8f, false, true, 1.02f);
            AddDestructible(s6, "Statue_EastMid", new Vector2(101.2f, 3.20f), BreakablePropKind.CrackedSentinelStatue, 2, 2, 10, 10f, false, false, 1.06f);
            AddDestructible(s6, "GildedUrn_BalcE", new Vector2(113.5f, 4.94f), BreakablePropKind.GildedPalaceUrn, 1, 2, 10, 8f, false, true, 1.02f);
            AddDestructible(s6, "Chain_Exit", new Vector2(123.5f, 3.20f), BreakablePropKind.RustedChain, 1, 2, 8, 7f, false, true, 1.05f);
            AddDecor(s6, "Court_ArchW", new Vector2(18.5f, 0f), "Env_CarvedStoneArch.png", GameConstants.SortNearBackground, 3, 1.25f, false, new Color(0.76f, 0.80f, 0.88f, 0.94f), false);
            AddDecor(s6, "Court_PillarCenterW", new Vector2(36.0f, 0f), "Env_StonePillar.png", GameConstants.SortNearBackground, 3, 1.32f, false, new Color(0.78f, 0.82f, 0.90f, 0.92f), false);
            AddDecor(s6, "Court_ArchMid", new Vector2(64.5f, 0f), "Env_CarvedStoneArch.png", GameConstants.SortNearBackground, 3, 1.28f, true, new Color(0.76f, 0.80f, 0.88f, 0.94f), false);
            AddDecor(s6, "Court_PillarCenterE", new Vector2(94.5f, 0f), "Env_StonePillar.png", GameConstants.SortNearBackground, 3, 1.32f, true, new Color(0.78f, 0.82f, 0.90f, 0.92f), false);
            AddDecor(s6, "Court_ArchE", new Vector2(116.5f, 0f), "Env_CarvedStoneArch.png", GameConstants.SortNearBackground, 3, 1.25f, false, new Color(0.76f, 0.80f, 0.88f, 0.94f), false);
            AddDecor(s6, "Court_BellBracket", new Vector2(44.0f, 12.6f), "Stages/Decor_CourtyardBellBracket.png", GameConstants.SortNearBackground, 5, 1.08f, false, Color.white, false);
            AddDecor(s6, "Court_BellBracketE", new Vector2(106.0f, 12.6f), "Stages/Decor_CourtyardBellBracket.png", GameConstants.SortNearBackground, 5, 1.08f, true, Color.white, false);
            AddDecor(s6, "Court_GuardStatueBg", new Vector2(128.4f, 3.20f), "Stages/Prop_CrackedStatue.png", GameConstants.SortNearBackground, 5, 1.18f, true, new Color(0.84f, 0.88f, 0.94f, 0.96f), false);
            AddAmbientProp(s6, "Courtyard_BannerA", new Vector2(18.5f, 9.6f), StageAmbientPropKind.CrimsonBanner, 1.15f, false);
            AddAmbientProp(s6, "Courtyard_BannerMid", new Vector2(56.5f, 10.6f), StageAmbientPropKind.CrimsonBanner, 1.22f, false);
            AddAmbientProp(s6, "Courtyard_BannerMidE", new Vector2(96.0f, 10.6f), StageAmbientPropKind.CrimsonBanner, 1.20f, true);
            AddAmbientProp(s6, "Courtyard_BannerC", new Vector2(122.0f, 9.6f), StageAmbientPropKind.CrimsonBanner, 1.15f, true);
            AddFaunaSwarm(s6, "Courtyard_SnowWispsW", new Vector2(36.0f, 4.0f), new Vector2(4.2f, 1.8f), 4, new Color(0.90f, 0.94f, 1.0f, 0.84f), new Color(1.0f, 0.86f, 0.65f, 0.72f));
            AddFaunaSwarm(s6, "Courtyard_SnowWispsE", new Vector2(104.0f, 4.0f), new Vector2(4.2f, 1.8f), 4, new Color(0.90f, 0.94f, 1.0f, 0.84f), new Color(1.0f, 0.86f, 0.65f, 0.72f));
            RegisterStage(assets, s6);

            // -----------------------------------------------------------------
            // STAGE 7: Aula Choral Chambers (X: 1420 .. 1560) — Grand Citadel Sanctum!
            // -----------------------------------------------------------------
            StageConfigData s7 = BuildStageTemplate(
                "Stage_07_AulaChoralChambers.asset", "stage_07_aula_choral_chambers", 7,
                "Aula Choral Chambers", "Grand Sanctum of the Golden Organ",
                "07_aula_choral_chambers.jpg", "Cliff_Choral.png", "Plat_ChoralBalcony.png",
                ArtStages + "Env_CitadelSupportArch.png", ArtEnv + "Env_StalactiteVines.png",
                ArtBg + "FG_CitadelFraming.png", new Color(0.12f, 0.10f, 0.14f, 0.92f),
                "Gate_ChoralGrandDoors.png", "Gate_ChoralGrandDoors.png",
                new Color(0.11f, 0.11f, 0.15f, 1f), new Color(1.0f, 0.92f, 0.64f, 0.30f), true,
                WeatherProfileType.ChoralGoldenDust, true, false,
                1420f, 140f, 0f,
                TransitionGateVisualType.GrandInteriorDoors, string.Empty,
                TransitionGateVisualType.GrandInteriorDoors, "stage_06_grand_gate_courtyard");
            AddPlatform(s7, "ChoirBalcony_W1", new Vector2(14.5f, 3.6f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "OrganLoft_W", new Vector2(26.0f, 6.8f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "WestChoirDais", new Vector2(38.0f, 2.86f), new Vector2(8.6f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "ChoirBalcony_MidW", new Vector2(50.5f, 4.8f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "SanctumDais_Mid", new Vector2(71.0f, 1.0f), new Vector2(14.0f, 2.0f), StagePlatformKind.SolidCliff);
            AddPlatform(s7, "ChoirBalcony_CenterHigh", new Vector2(71.0f, 6.4f), new Vector2(7.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "SecretShelf_ChoralVault", new Vector2(90.5f, 5.86f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "SecretRoof_ChoralVault", new Vector2(90.5f, 9.74f), new Vector2(12.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "ChoirBalcony_E1", new Vector2(105.5f, 4.2f), new Vector2(6.8f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "OrganLoft_E", new Vector2(117.0f, 6.8f), new Vector2(6.0f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddPlatform(s7, "EastChoirDais", new Vector2(127.0f, 2.86f), new Vector2(8.2f, 0.68f), StagePlatformKind.OneWayPlatform);
            AddDestructible(s7, "ChoirBench_W", new Vector2(12.5f, 0f), BreakablePropKind.ChoirBench, 2, 2, 10, 8f, false, false, 1.02f);
            AddDestructible(s7, "Candelabra_W", new Vector2(15.5f, 3.94f), BreakablePropKind.BrassCandelabra, 1, 2, 10, 9f, false, false, 1.0f);
            AddDestructible(s7, "CeremonialUrn_MidW", new Vector2(39.5f, 3.20f), BreakablePropKind.CeremonialUrn, 1, 2, 12, 10f, false, false, 1.06f);
            AddDestructible(s7, "ChoirBench_Mid", new Vector2(56.5f, 0f), BreakablePropKind.ChoirBench, 2, 2, 11, 9f, false, false, 1.04f);
            AddDestructible(s7, "Candelabra_Center", new Vector2(71.0f, 6.74f), BreakablePropKind.BrassCandelabra, 1, 2, 12, 10f, false, false, 1.02f);
            AddDestructible(s7, "CeremonialUrn_MidE", new Vector2(81.5f, 0f), BreakablePropKind.CeremonialUrn, 1, 2, 12, 10f, false, true, 1.06f);
            AddDestructible(s7, "Candelabra_E", new Vector2(104.5f, 4.54f), BreakablePropKind.BrassCandelabra, 1, 2, 10, 9f, false, true, 1.0f);
            AddDestructible(s7, "ChoirBench_E", new Vector2(128.0f, 3.20f), BreakablePropKind.ChoirBench, 2, 2, 10, 8f, false, true, 1.02f);
            AddDecor(s7, "Choral_OrganLeft", new Vector2(9.2f, 0f), "Stages/Decor_ChoralOrganLoft.png", GameConstants.SortNearBackground, 3, 1.15f, false, Color.white, false);
            AddDecor(s7, "Choral_OrganMidW", new Vector2(45.5f, 0f), "Stages/Decor_ChoralOrganConsole.png", GameConstants.SortNearBackground, 3, 1.18f, false, Color.white, false);
            AddDecor(s7, "Choral_GrandDoorsBg", new Vector2(71.0f, 2.0f), "Stages/Gate_ChoralGrandDoors.png", GameConstants.SortNearBackground, 3, 1.18f, false, new Color(0.95f, 0.90f, 0.80f, 0.94f), false);
            AddDecor(s7, "Choral_KnightW", new Vector2(63.0f, 2.0f), "Stages/Prop_CrackedStatue.png", GameConstants.SortNearBackground, 5, 1.15f, false, new Color(0.92f, 0.88f, 0.80f, 0.95f), false);
            AddDecor(s7, "Choral_KnightE", new Vector2(79.0f, 2.0f), "Stages/Prop_CrackedStatue.png", GameConstants.SortNearBackground, 5, 1.15f, true, new Color(0.92f, 0.88f, 0.80f, 0.95f), false);
            AddDecor(s7, "Choral_OrganRight", new Vector2(131.5f, 0f), "Stages/Decor_ChoralOrganLoft.png", GameConstants.SortNearBackground, 3, 1.18f, true, Color.white, false);
            AddAmbientProp(s7, "Choral_BellW", new Vector2(21.0f, 12.6f), StageAmbientPropKind.HangingBellCluster, 1.15f, false);
            AddAmbientProp(s7, "Choral_BellCenter", new Vector2(71.0f, 12.6f), StageAmbientPropKind.HangingBellCluster, 1.22f, false);
            AddAmbientProp(s7, "Choral_BellE", new Vector2(118.0f, 12.6f), StageAmbientPropKind.HangingBellCluster, 1.15f, true);
            AddAmbientProp(s7, "Choral_BannerW", new Vector2(28.0f, 10.6f), StageAmbientPropKind.CrimsonBanner, 1.15f, false);
            AddAmbientProp(s7, "Choral_BannerCenter", new Vector2(71.0f, 10.8f), StageAmbientPropKind.CrimsonBanner, 1.22f, false);
            AddAmbientProp(s7, "Choral_BannerE", new Vector2(112.0f, 10.6f), StageAmbientPropKind.CrimsonBanner, 1.15f, true);
            AddAmbientProp(s7, "Choral_GodRayW", new Vector2(24.0f, 8.8f), StageAmbientPropKind.CathedralGodRay, 1.35f, false);
            AddAmbientProp(s7, "Choral_GodRayCenter", new Vector2(71.0f, 9.2f), StageAmbientPropKind.CathedralGodRay, 1.60f, false);
            AddAmbientProp(s7, "Choral_GodRayE", new Vector2(116.0f, 8.8f), StageAmbientPropKind.CathedralGodRay, 1.35f, true);
            AddFaunaSwarm(s7, "Choral_GoldenMothsW", new Vector2(24.0f, 4.4f), new Vector2(3.6f, 1.8f), 5, new Color(1.0f, 0.92f, 0.56f, 0.92f), new Color(1.0f, 0.98f, 0.84f, 0.78f));
            AddFaunaSwarm(s7, "Choral_GoldenMothsC", new Vector2(71.0f, 4.6f), new Vector2(4.0f, 2.0f), 6, new Color(1.0f, 0.92f, 0.56f, 0.92f), new Color(1.0f, 0.98f, 0.84f, 0.78f));
            AddFaunaSwarm(s7, "Choral_GoldenMothsE", new Vector2(116.0f, 4.4f), new Vector2(3.6f, 1.8f), 5, new Color(1.0f, 0.92f, 0.56f, 0.92f), new Color(1.0f, 0.98f, 0.84f, 0.78f));
            RegisterStage(assets, s7);

            // Persist all 8 StageConfigData assets to disk immediately so EditorSceneManager.NewScene cannot lose them
            AssetDatabase.SaveAssets();
        }

        public static DynamicWeatherController BuildWeatherEngineOnCamera(Camera cam, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            Material glowMat = assets.WeatherGlowMaterial != null
                ? assets.WeatherGlowMaterial
                : AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WeatherGlowMote.mat");
            Material streakMat = assets.WeatherStreakMaterial != null
                ? assets.WeatherStreakMaterial
                : AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WeatherStreak.mat");

            GameObject root = new GameObject("DynamicWeatherSystem");
            root.transform.SetParent(cam.transform, false);
            root.transform.localPosition = new Vector3(0f, 0f, 10f);

            ParticleSystem bgPs = CreateWeatherLayer(root.transform, "Weather_BackgroundLayer");
            ParticleSystem midPs = CreateWeatherLayer(root.transform, "Weather_PlayableLayer");
            ParticleSystem fgPs = CreateWeatherLayer(root.transform, "Weather_ForegroundLayer");

            GameObject veilGo = new GameObject("Weather_FogVeil");
            veilGo.transform.SetParent(root.transform, false);
            veilGo.transform.localPosition = Vector3.zero;
            veilGo.transform.localScale = new Vector3(38f, 22f, 1f);
            SpriteRenderer veilSr = veilGo.AddComponent<SpriteRenderer>();
            veilSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SoftParticle.png");
            veilSr.color = new Color(0.16f, 0.34f, 0.24f, 0.04f);
            veilSr.sortingLayerName = GameConstants.SortForeground;
            veilSr.sortingOrder = 2;

            DynamicWeatherController weather = root.AddComponent<DynamicWeatherController>();
            weather.ConfigureReferences(bgPs, midPs, fgPs, glowMat, streakMat, veilSr);

            // 3 Seamless Scrolling Volumetric Storm & Mist Cloud Bands (Sandstorm + Blizzard + Cavern Haze)
            Sprite stormCloudSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_StormMistCloud.png");
            Sprite groundFogSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_GroundFogBank.png");
            BuildScrollingFogBand(weather, root.transform, 0, "FogBand_MidWindClouds", stormCloudSp, GameConstants.SortMidBackground, 7, 2.75f, 2.25f, new Vector3(0f, 0.8f, 0f));
            BuildScrollingFogBand(weather, root.transform, 1, "FogBand_GroundDrift", groundFogSp, GameConstants.SortVFX, 2, 2.65f, 2.35f, new Vector3(0f, -2.9f, 0f));
            BuildScrollingFogBand(weather, root.transform, 2, "FogBand_ForegroundGusts", stormCloudSp, GameConstants.SortForeground, 4, 2.85f, 2.25f, new Vector3(0f, -0.4f, 0f));

            return weather;
        }

        private static void BuildScrollingFogBand(
            DynamicWeatherController weather, Transform parent, int index, string name,
            Sprite sprite, string sortLayer, int sortOrder, float scaleX, float scaleY, Vector3 localPos)
        {
            if (sprite == null) return;
            GameObject band = new GameObject(name);
            band.transform.SetParent(parent, false);
            band.transform.localPosition = localPos;

            float tileW = Mathf.Max(10f, sprite.bounds.size.x * scaleX);
            SpriteRenderer srA;
            SpriteRenderer srB;
            Transform tA = CreateFogTile(band.transform, "TileA", sprite, sortLayer, sortOrder, scaleX, scaleY, 0f, out srA);
            Transform tB = CreateFogTile(band.transform, "TileB", sprite, sortLayer, sortOrder, scaleX, scaleY, tileW, out srB);
            band.SetActive(false);

            weather.ConfigureFogBand(index, band.transform, tA, tB, srA, srB, tileW);
        }

        private static Transform CreateFogTile(
            Transform parent, string name, Sprite sp, string sortLayer, int sortOrder,
            float sx, float sy, float localX, out SpriteRenderer sr)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(localX, 0f, 0f);
            go.transform.localScale = new Vector3(sx, sy, 1f);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = new Color(1f, 1f, 1f, 0f);
            sr.sortingLayerName = sortLayer;
            sr.sortingOrder = sortOrder;
            return go.transform;
        }

        public static StageProgressionManager BuildAllProgressionStages(
            Camera cam,
            GameObject mossParallaxRoot,
            BiomeAtmosphereController mossBiomeCtrl,
            DynamicWeatherController weatherCtrl,
            Transform systemsRoot,
            SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            List<StageConfigData> stageConfigs = LoadAllStageConfigsFromDisk();
            if (stageConfigs.Count < StageAssetFiles.Length)
            {
                CreateAllStageConfigAssets(assets);
                stageConfigs = LoadAllStageConfigsFromDisk();
            }

            if (assets.SilkOrbPrefab == null) assets.SilkOrbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/SilkOrbPickup.prefab");
            if (assets.SpikeHazardPrefab == null) assets.SpikeHazardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/SpikeHazard.prefab");
            if (assets.CheckpointPrefab == null) assets.CheckpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/CheckpointShrine.prefab");
            if (assets.UpgradeAltarPrefab == null) assets.UpgradeAltarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Interactables/UpgradeAltar.prefab");
            if (assets.LancerPrefab == null) assets.LancerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/CrimsonLancer.prefab");
            if (assets.MossmirPrefab == null) assets.MossmirPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Mossmir.prefab");
            if (assets.MossgrubPrefab == null) assets.MossgrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Mossgrub.prefab");
            if (assets.AknidPrefab == null) assets.AknidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/Aknid.prefab");
            if (assets.WingedPilgrimPrefab == null) assets.WingedPilgrimPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/WingedPilgrim.prefab");
            if (assets.PilgrimGrovellerPrefab == null) assets.PilgrimGrovellerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/PilgrimGroveller.prefab");
            if (assets.PilgrimHulkPrefab == null) assets.PilgrimHulkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/PilgrimHulk.prefab");
            if (assets.PilgrimBellbearerPrefab == null) assets.PilgrimBellbearerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/PilgrimBellbearer.prefab");
            if (assets.CovetousPilgrimPrefab == null) assets.CovetousPilgrimPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/CovetousPilgrim.prefab");
            if (assets.PondcatcherPrefab == null) assets.PondcatcherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/Pondcatcher.prefab");
            if (assets.DriznitPrefab == null) assets.DriznitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/Driznit.prefab");
            if (assets.ConchflyPrefab == null) assets.ConchflyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/Conchfly.prefab");
            if (assets.PharlidPrefab == null) assets.PharlidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/Pharlid.prefab");
            if (assets.CoralFurmPrefab == null) assets.CoralFurmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Karak/CoralFurm.prefab");
            if (assets.ServitorBoranPrefab == null) assets.ServitorBoranPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/ServitorBoran.prefab");
            if (assets.MnemonordPrefab == null) assets.MnemonordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/Mnemonord.prefab");
            if (assets.MnemonidPrefab == null) assets.MnemonidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/Mnemonid.prefab");
            if (assets.DriftlinPrefab == null) assets.DriftlinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MountFay/Driftlin.prefab");
            if (assets.GrandReedPrefab == null) assets.GrandReedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/GrandReed.prefab");
            if (assets.ReedPrefab == null) assets.ReedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/Reed.prefab");
            if (assets.ChoristorPrefab == null) assets.ChoristorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/Choristor.prefab");
            if (assets.ChoirHornheadPrefab == null) assets.ChoirHornheadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/ChoirHornhead.prefab");
            if (assets.ChoirElderPrefab == null) assets.ChoirElderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/ChoirElder.prefab");
            if (assets.ChoirFlyerPrefab == null) assets.ChoirFlyerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/ChoralChambers/ChoirFlyer.prefab");
            if (assets.DrapemitePrefab == null) assets.DrapemitePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Courtyard/Drapemite.prefab");
            if (assets.DrapeflyPrefab == null) assets.DrapeflyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Courtyard/Drapefly.prefab");
            if (assets.FurmPrefab == null) assets.FurmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Furm.prefab");
            if (assets.WingedFurmPrefab == null) assets.WingedFurmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/WingedFurm.prefab");
            if (assets.SkullwingPrefab == null) assets.SkullwingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Skullwing.prefab");
            if (assets.PharlidDiverPrefab == null) assets.PharlidDiverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/PharlidDiver.prefab");
            if (assets.MawlingPrefab == null) assets.MawlingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Mawling.prefab");
            if (assets.MarrowmawPrefab == null) assets.MarrowmawPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Marrowmaw.prefab");
            if (assets.BellhartMossmirPrefab == null) assets.BellhartMossmirPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Mossmir.prefab");
            if (assets.BellhartMossgrubPrefab == null) assets.BellhartMossgrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Bellhart/Mossgrub.prefab");
            if (assets.LancerStats == null) assets.LancerStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_CrimsonLancer.asset");
            if (assets.MossmirStats == null) assets.MossmirStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Mossmir.asset");
            if (assets.MossgrubStats == null) assets.MossgrubStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Mossgrub.asset");
            if (assets.AknidStats == null) assets.AknidStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Aknid.asset");
            if (assets.WingedPilgrimStats == null) assets.WingedPilgrimStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_WingedPilgrim.asset");
            if (assets.PilgrimGrovellerStats == null) assets.PilgrimGrovellerStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_PilgrimGroveller.asset");
            if (assets.PilgrimHulkStats == null) assets.PilgrimHulkStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_PilgrimHulk.asset");
            if (assets.PilgrimBellbearerStats == null) assets.PilgrimBellbearerStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_PilgrimBellbearer.asset");
            if (assets.CovetousPilgrimStats == null) assets.CovetousPilgrimStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_CovetousPilgrim.asset");
            if (assets.PondcatcherStats == null) assets.PondcatcherStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Pondcatcher.asset");
            if (assets.DriznitStats == null) assets.DriznitStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Driznit.asset");
            if (assets.ConchflyStats == null) assets.ConchflyStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Conchfly.asset");
            if (assets.PharlidStats == null) assets.PharlidStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Pharlid.asset");
            if (assets.CoralFurmStats == null) assets.CoralFurmStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_CoralFurm.asset");
            if (assets.ServitorBoranStats == null) assets.ServitorBoranStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_ServitorBoran.asset");
            if (assets.MnemonordStats == null) assets.MnemonordStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Mnemonord.asset");
            if (assets.MnemonidStats == null) assets.MnemonidStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Mnemonid.asset");
            if (assets.DriftlinStats == null) assets.DriftlinStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Driftlin.asset");
            if (assets.GrandReedStats == null) assets.GrandReedStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_GrandReed.asset");
            if (assets.ReedStats == null) assets.ReedStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Reed.asset");
            if (assets.ChoristorStats == null) assets.ChoristorStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Choristor.asset");
            if (assets.ChoirHornheadStats == null) assets.ChoirHornheadStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_ChoirHornhead.asset");
            if (assets.ChoirElderStats == null) assets.ChoirElderStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_ChoirElder.asset");
            if (assets.ChoirFlyerStats == null) assets.ChoirFlyerStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_ChoirFlyer.asset");
            if (assets.DrapemiteStats == null) assets.DrapemiteStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Drapemite.asset");
            if (assets.DrapeflyStats == null) assets.DrapeflyStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Drapefly.asset");
            if (assets.FurmStats == null) assets.FurmStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Furm.asset");
            if (assets.WingedFurmStats == null) assets.WingedFurmStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_WingedFurm.asset");
            if (assets.SkullwingStats == null) assets.SkullwingStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Skullwing.asset");
            if (assets.PharlidDiverStats == null) assets.PharlidDiverStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_PharlidDiver.asset");
            if (assets.MawlingStats == null) assets.MawlingStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Mawling.asset");
            if (assets.MarrowmawStats == null) assets.MarrowmawStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Marrowmaw.asset");
            if (assets.BellhartMossmirStats == null) assets.BellhartMossmirStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Bellhart_Mossmir.asset");
            if (assets.BellhartMossgrubStats == null) assets.BellhartMossgrubStats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Bellhart_Mossgrub.asset");
            if (assets.AdditiveVFXMaterial == null) assets.AdditiveVFXMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_AdditiveVFX.mat");
            if (assets.VolumetricGodRayMaterial == null) assets.VolumetricGodRayMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_VolumetricGodRay.mat");
            if (assets.CheckpointPrefab == null) assets.CheckpointPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/CheckpointShrine.prefab");
            if (assets.UpgradeAltarPrefab == null) assets.UpgradeAltarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/UpgradeAltar.prefab");
            if (assets.BreakableWallPrefab == null) assets.BreakableWallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/BreakableVineWall.prefab");
            if (assets.InventoryPickupPrefab == null) assets.InventoryPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/InventoryPickup.prefab");
            if (assets.SilkOrbPrefab == null) assets.SilkOrbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/SilkOrbPickup.prefab");

            GameObject worldRoot = new GameObject("World_ProgressionStages");
            StageProgressionManager mgr = systemsRoot.gameObject.AddComponent<StageProgressionManager>();

            Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_CaveDoorwayGlow.png");
            Sprite rottenLogSp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_RottenLog.png");
            Sprite mossBridgeSp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_StoneBridgeWalkway.png");
            Sprite dripSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_WaterDrip.png");
            Sprite waterPoolSp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_WaterPoolLilyPads.png");
            Sprite waterGlintSp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_WaterShimmerGlint.png");
            Sprite critterMothSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_BioCritterMoth.png");

            // Enhance Prolog Stage 0 (Moss Grotto) with HD Rotten Logs (leaving Zone 1's existing platforms cleanly intact)
            Transform prologExtras = new GameObject("Stage00_PrologExtras").transform;
            prologExtras.SetParent(worldRoot.transform, false);
            SpawnStageDestructible(prologExtras, "Z1_RottenLog_Glade", new Vector2(11.6f, 0f), BreakablePropKind.RottenLog, 1, 1, 5, 5f, false, false, 1.0f, rottenLogSp, assets.SilkOrbPrefab);
            SpawnStageDestructible(prologExtras, "Z1_RottenLog_East", new Vector2(24.6f, 1.6f), BreakablePropKind.RottenLog, 1, 1, 5, 5f, false, true, 0.96f, rottenLogSp, assets.SilkOrbPrefab);

            List<StageRuntimeBinding> bindings = new List<StageRuntimeBinding>();
            Dictionary<string, CaveEntrancePortal> rightExitPortals = new Dictionary<string, CaveEntrancePortal>();
            Dictionary<string, CaveEntrancePortal> leftEntryPortals = new Dictionary<string, CaveEntrancePortal>();

            for (int i = 0; i < stageConfigs.Count; i++)
            {
                StageConfigData cfg = stageConfigs[i];
                if (cfg == null) continue;

                if (cfg.stageIndex == 0)
                {
                    bindings.Add(new StageRuntimeBinding { config = cfg, backdropRoot = null });

                    // Prolog Right Exit Gate -> Mossy Dark Cave Mouth at end of Moss Grotto Arena (X = 141.6, Y = 14.2)
                    Vector3 prologExitPos = new Vector3(cfg.rightExitOffset.x, cfg.rightExitOffset.y, 0f);
                    SpawnGateVisual(prologExtras, "GateVisual_MossToWeavenest", prologExitPos, cfg.exitGateSprite, 1.25f, false);
                    CaveEntrancePortal prologExit = CreateStagePortalEndpoint(prologExtras, "Portal_00_ExitRight", prologExitPos, glowSprite, assets.AdditiveVFXMaterial, 1, new Color(0.78f, 0.98f, 0.68f, 0.48f));
                    CreateInvisibleBarrier(prologExtras, "Z5_PortalBackstopWall", new Vector2(143.6f, 16.0f), new Vector2(1.4f, 4.4f));
                    rightExitPortals[cfg.stageId] = prologExit;
                    continue;
                }

                GameObject stageGo = new GameObject(string.Format("Stage_{0:00}_{1}", cfg.stageIndex, cfg.displayName.Replace(" ", "")));
                stageGo.transform.SetParent(worldRoot.transform, false);

                // 1. Camera-tracking 16:9 Stage Backdrop + Volumetric God Rays + Foreground Framing Silhouettes
                GameObject backdropGo = BuildStageBackdrop(stageGo.transform, cfg, cam.transform, assets.VolumetricGodRayMaterial);
                backdropGo.SetActive(false);
                bindings.Add(new StageRuntimeBinding { config = cfg, backdropRoot = backdropGo });

                // 2. Main Ground Floor, Visible Biome Ceiling Roof, Hanging Overhangs, Drips & Outer Walls
                BuildStageEnclosure(stageGo.transform, cfg, dripSprite, assets.AdditiveVFXMaterial);

                // 3. Modular Platforms (Solid Multi-Row Cliffs, One-Way Jump-Through Ledges + Support Arches!)
                for (int p = 0; p < cfg.platforms.Count; p++)
                {
                    StagePlatformSpec ps = cfg.platforms[p];
                    Vector2 worldCenter = new Vector2(cfg.originX + ps.localCenter.x, cfg.groundY + ps.localCenter.y);
                    bool isOneWay = ps.kind == StagePlatformKind.OneWayPlatform || ps.kind == StagePlatformKind.SlipperyIceOneWay;
                    bool isIce = ps.kind == StagePlatformKind.SlipperyIceCliff || ps.kind == StagePlatformKind.SlipperyIceOneWay;

                    if (isOneWay)
                    {
                        bool attachSupportArch = ps.localCenter.y <= 3.55f && ps.label != "SanctumDais_Mid";
                        CreateSolidStagePlatform(stageGo.transform, ps.label, worldCenter, ps.size, cfg.platformSprite, ps.tint, isIce, attachSupportArch ? cfg.supportArchSprite : null, ps.localCenter.y);
                    }
                    else
                    {
                        CreateSolidStageCliff(stageGo.transform, ps.label, worldCenter, ps.size, cfg.cliffSprite != null ? cfg.cliffSprite : cfg.platformSprite, ps.tint, isIce);
                    }
                }

                // 4. Reflective Water / Bog Pools on Stage Floor
                for (int pl = 0; pl < cfg.pools.Count; pl++)
                {
                    CreateStagePool(stageGo.transform, cfg, cfg.pools[pl], waterPoolSp, waterGlintSp);
                }

                // 5. Non-Destructible Environmental Storytelling (Houses, Organ Lofts, Arches, Giant Corals, Flora)
                for (int dc = 0; dc < cfg.decors.Count; dc++)
                {
                    SpawnStageDecor(stageGo.transform, cfg, cfg.decors[dc]);
                }

                // 6. Biome-Specific Destructible Obstacles (Hit-flash, debris, SFX & popping shard drops)
                for (int d = 0; d < cfg.destructibles.Count; d++)
                {
                    StageDestructibleSpec ds = cfg.destructibles[d];
                    Vector2 worldPos = new Vector2(cfg.originX + ds.localSurfacePos.x, cfg.groundY + ds.localSurfacePos.y);
                    Sprite propSp = LoadDestructibleSprite(ds.kind);
                    SpawnStageDestructible(stageGo.transform, ds.label, worldPos, ds.kind, ds.maxHits,
                        ds.orbDropCount, ds.essencePerOrb, ds.rewardSilk, ds.blocksMovement, ds.flipX, ds.scale,
                        propSp, assets.SilkOrbPrefab);
                }

                // 7. Stage Hazards (Shellwood Thorn Pit)
                for (int h = 0; h < cfg.hazards.Count; h++)
                {
                    StageHazardSpec hz = cfg.hazards[h];
                    if (assets.SpikeHazardPrefab != null)
                    {
                        GameObject sp = (GameObject)PrefabUtility.InstantiatePrefab(assets.SpikeHazardPrefab);
                        sp.name = hz.label;
                        sp.transform.SetParent(stageGo.transform, false);
                        sp.transform.position = new Vector3(cfg.originX + hz.localPos.x, cfg.groundY + hz.localPos.y, 0f);
                        sp.transform.localScale = new Vector3(hz.width / 2.8f, 1f, 1f);
                    }
                }

                // 8. Stage Ambient Live Props (Lantern glows, banners, swaying webs, hanging bells, god rays)
                for (int a = 0; a < cfg.ambientProps.Count; a++)
                {
                    SpawnStageAmbientProp(stageGo.transform, cfg, cfg.ambientProps[a], assets);
                }

                // 9. Living Ambient Biome Fauna Swarms (Silk Moths, Firefly-Moths, Scarabs, Frost Wisps, Choir Moths)
                for (int f = 0; f < cfg.faunaSwarms.Count; f++)
                {
                    SpawnStageFaunaSwarm(stageGo.transform, cfg, cfg.faunaSwarms[f], critterMothSp, assets.AdditiveVFXMaterial);
                }

                // 10. Checkpoint Sanctuary Bench + Curated Biome-Tinted Encounters per Stage
                SpawnStageSanctuaryAndEncounters(stageGo.transform, cfg, assets);

                // 11. Stage AreaZoneTrigger (keeps camera bounds & area banner locked inside this stage!)
                GameObject zoneGo = new GameObject("AreaZone_" + cfg.stageId);
                zoneGo.transform.SetParent(stageGo.transform, false);
                zoneGo.transform.position = new Vector3(cfg.originX + cfg.stageWidth * 0.5f, cfg.groundY + 7.5f, 0f);
                BoxCollider2D zoneCol = zoneGo.AddComponent<BoxCollider2D>();
                zoneCol.isTrigger = true;
                zoneCol.size = new Vector2(cfg.stageWidth + 4f, 24f);
                AreaZoneTrigger zoneTrig = zoneGo.AddComponent<AreaZoneTrigger>();
                zoneTrig.Configure(cfg.stageId, cfg.displayName, cfg.subtitle, cfg.cameraMinBounds, cfg.cameraMaxBounds);
                EditorUtility.SetDirty(zoneTrig);

                // 12. Left Entry Gate & Right Exit Gate (Calibrated cleanly to 3.8u–4.6u world height!)
                Vector3 leftGatePos = new Vector3(cfg.originX + 1.8f, cfg.groundY, 0f);
                float leftScale = cfg.entryGateVisual == TransitionGateVisualType.GrandInteriorDoors ? 1.05f : 1.18f;
                SpawnGateVisual(stageGo.transform, "GateVisual_EntryLeft", leftGatePos, cfg.entryGateSprite, leftScale, true);
                CaveEntrancePortal leftPortal = CreateStagePortalEndpoint(stageGo.transform, "Portal_EntryLeft", leftGatePos, glowSprite, assets.AdditiveVFXMaterial, -1, GetPortalGlowColor(cfg.weatherType));
                leftEntryPortals[cfg.stageId] = leftPortal;

                if (!string.IsNullOrEmpty(cfg.nextStageId))
                {
                    Vector3 rightGatePos = new Vector3(cfg.originX + cfg.rightExitOffset.x, cfg.groundY + cfg.rightExitOffset.y, 0f);
                    float gateScale = cfg.exitGateVisual == TransitionGateVisualType.GrandInteriorDoors ? 1.12f : 1.24f;
                    SpawnGateVisual(stageGo.transform, "GateVisual_ExitRight", rightGatePos, cfg.exitGateSprite, gateScale, false);
                    CaveEntrancePortal rightPortal = CreateStagePortalEndpoint(stageGo.transform, "Portal_ExitRight", rightGatePos, glowSprite, assets.AdditiveVFXMaterial, 1, GetPortalGlowColor(cfg.weatherType));
                    rightExitPortals[cfg.stageId] = rightPortal;
                }
            }

            // Pair every adjacent Stage i (Right Exit) <-> Stage i+1 (Left Entry) bidirectionally!
            int pairedPortals = 0;
            for (int i = 0; i < stageConfigs.Count - 1; i++)
            {
                StageConfigData fromCfg = stageConfigs[i];
                StageConfigData toCfg = stageConfigs[i + 1];
                CaveEntrancePortal exitP;
                CaveEntrancePortal entryP;
                if (!rightExitPortals.TryGetValue(fromCfg.stageId, out exitP)) continue;
                if (!leftEntryPortals.TryGetValue(toCfg.stageId, out entryP)) continue;
                pairedPortals++;

                exitP.Configure(
                    entryP,
                    new Vector3(-1.4f, 0.02f, 0f),
                    1, -1,
                    fromCfg.stageId, fromCfg.displayName, fromCfg.subtitle,
                    fromCfg.stageIndex == 0 ? new Vector2(100f, 8f) : fromCfg.cameraMinBounds,
                    fromCfg.stageIndex == 0 ? new Vector2(144.5f, 29f) : fromCfg.cameraMaxBounds,
                    fromCfg.useDarkStoneAcoustics,
                    exitP.GetComponentInChildren<SpriteRenderer>());

                entryP.Configure(
                    exitP,
                    new Vector3(1.6f, 0.02f, 0f),
                    -1, 1,
                    toCfg.stageId, toCfg.displayName, toCfg.subtitle,
                    toCfg.cameraMinBounds, toCfg.cameraMaxBounds,
                    toCfg.useDarkStoneAcoustics,
                    entryP.GetComponentInChildren<SpriteRenderer>());
            }

            SilkenGrottoProjectBuilder.SetField(mgr, "targetCamera", cam);
            SilkenGrottoProjectBuilder.SetField(mgr, "mossGrottoParallax", mossBiomeCtrl);
            SilkenGrottoProjectBuilder.SetField(mgr, "mossGrottoParallaxRoot", mossParallaxRoot);
            SilkenGrottoProjectBuilder.SetField(mgr, "weatherController", weatherCtrl);

            SerializedObject mgrSo = new SerializedObject(mgr);
            SerializedProperty stagesProp = mgrSo.FindProperty("stages");
            if (stagesProp != null && stagesProp.isArray)
            {
                stagesProp.arraySize = bindings.Count;
                for (int i = 0; i < bindings.Count; i++)
                {
                    SerializedProperty elem = stagesProp.GetArrayElementAtIndex(i);
                    SerializedProperty cfgP = elem.FindPropertyRelative("config");
                    SerializedProperty bgP = elem.FindPropertyRelative("backdropRoot");
                    if (cfgP != null) cfgP.objectReferenceValue = bindings[i].config;
                    if (bgP != null) bgP.objectReferenceValue = bindings[i].backdropRoot;
                }
                mgrSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (bindings.Count < StageAssetFiles.Length || pairedPortals < StageAssetFiles.Length - 1)
            {
                Debug.LogError(string.Format("[SilkenGrotto] Stage progression incomplete! Built {0} stages and {1} portal pairs.", bindings.Count, pairedPortals));
            }
            else
            {
                Debug.Log(string.Format("[SilkenGrotto] HD Progression World Built: {0} stages & {1} bidirectional transition gates.", bindings.Count, pairedPortals));
            }

            return mgr;
        }

        private static StageConfigData CreateStageAsset(string fileName)
        {
            string path = StageFolder + "/" + fileName;
            StageConfigData existing = AssetDatabase.LoadAssetAtPath<StageConfigData>(path);
            if (existing != null) return existing;
            StageConfigData created = ScriptableObject.CreateInstance<StageConfigData>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void ClearLists(StageConfigData s)
        {
            s.platforms.Clear();
            s.destructibles.Clear();
            s.hazards.Clear();
            s.ambientProps.Clear();
            s.decors.Clear();
            s.pools.Clear();
            s.faunaSwarms.Clear();
        }

        private static StageConfigData BuildStageTemplate(
            string assetFile, string id, int index, string name, string sub,
            string bgFile, string cliffFile, string platFile,
            string archPath, string stalactitePath, string fgFramePath, Color fgTint,
            string exitGateFile, string entryGateFile,
            Color clearCol, Color rayCol, bool showRays,
            WeatherProfileType weather, bool darkAudio, bool slipperyGround,
            float originX, float width, float groundY,
            TransitionGateVisualType exitVis, string nextId,
            TransitionGateVisualType entryVis, string prevId)
        {
            StageConfigData s = CreateStageAsset(assetFile);
            s.stageId = id;
            s.stageIndex = index;
            s.displayName = name;
            s.subtitle = sub;
            s.backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBg + bgFile);
            s.cliffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + cliffFile);
            s.platformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + platFile);
            s.supportArchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(archPath);
            s.ceilingStalactiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(stalactitePath);
            s.foregroundFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fgFramePath);
            s.foregroundFrameTint = fgTint;
            s.exitGateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + exitGateFile);
            s.entryGateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + entryGateFile);
            s.cameraClearColor = clearCol;
            s.godRayTint = rayCol;
            s.showGodRays = showRays;
            s.weatherType = weather;
            s.useDarkStoneAcoustics = darkAudio;
            s.stageBgm = LoadStageBgm(index);
            s.originX = originX;
            s.stageWidth = width;
            s.groundY = groundY;
            s.slipperyGround = slipperyGround;
            s.cameraMinBounds = new Vector2(originX, index == 2 ? -3.8f : -2.2f);
            s.cameraMaxBounds = new Vector2(originX + width, 16.5f);
            s.leftSpawnOffset = new Vector2(3.6f, 0.02f);
            s.rightExitOffset = new Vector2(width - 2.2f, 0f);
            s.exitGateVisual = exitVis;
            s.nextStageId = nextId;
            s.entryGateVisual = entryVis;
            s.previousStageId = prevId;
            ClearLists(s);
            return s;
        }

        private static void RegisterStage(SilkenGrottoProjectBuilder.BuiltAssets assets, StageConfigData s)
        {
            EditorUtility.SetDirty(s);
            assets.AllStageConfigs.Add(s);
            assets.StageById[s.stageId] = s;
        }

        private static void AddPlatform(StageConfigData s, string label, Vector2 center, Vector2 size, StagePlatformKind kind)
        {
            s.platforms.Add(new StagePlatformSpec { label = label, localCenter = center, size = size, kind = kind, tint = Color.white });
        }

        private static void AddDestructible(
            StageConfigData s, string label, Vector2 surfacePos, BreakablePropKind kind,
            int hits, int orbCount, int essenceEach, float silk, bool solid, bool flipX, float scale)
        {
            s.destructibles.Add(new StageDestructibleSpec
            {
                label = label,
                localSurfacePos = surfacePos,
                kind = kind,
                maxHits = hits,
                orbDropCount = orbCount,
                essencePerOrb = essenceEach,
                rewardSilk = silk,
                blocksMovement = solid,
                flipX = flipX,
                scale = scale
            });
        }

        private static void AddHazard(StageConfigData s, string label, Vector2 localPos, float width)
        {
            s.hazards.Add(new StageHazardSpec { label = label, localPos = localPos, width = width });
        }

        private static void AddAmbientProp(StageConfigData s, string label, Vector2 localPos, StageAmbientPropKind kind, float scale, bool flipX)
        {
            s.ambientProps.Add(new StageAmbientPropSpec { label = label, localPos = localPos, kind = kind, scale = scale, flipX = flipX });
        }

        private static void AddDecor(StageConfigData s, string label, Vector2 localPos, string relEnvAsset, string sortLayer, int order, float scale, bool flipX, Color tint, bool sway)
        {
            s.decors.Add(new StageDecorSpec
            {
                label = label,
                localPos = localPos,
                assetFileName = relEnvAsset,
                sortLayer = sortLayer,
                sortOrder = order,
                scale = scale,
                flipX = flipX,
                tint = tint,
                swayWithWind = sway
            });
        }

        private static void AddPool(StageConfigData s, string label, Vector2 bottomCenter, float width, Color waterCol, Color glintCol)
        {
            s.pools.Add(new StagePoolSpec { label = label, localBottomCenter = bottomCenter, width = width, waterTint = waterCol, glintTint = glintCol });
        }

        private static void AddFaunaSwarm(StageConfigData s, string label, Vector2 center, Vector2 radius, int count, Color colA, Color colB)
        {
            s.faunaSwarms.Add(new StageFaunaSwarmSpec { label = label, localCenter = center, roamRadius = radius, count = count, colorA = colA, colorB = colB });
        }

        private static ParticleSystem CreateWeatherLayer(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            return go.AddComponent<ParticleSystem>();
        }

        private static GameObject BuildStageBackdrop(Transform parent, StageConfigData cfg, Transform camTransform, Material godRayMat)
        {
            GameObject bgGo = new GameObject("Backdrop_" + cfg.stageId);
            bgGo.transform.SetParent(parent, false);
            Vector2 center = new Vector2(cfg.originX + cfg.stageWidth * 0.5f, cfg.groundY + 5.2f);
            bgGo.transform.position = new Vector3(center.x, center.y, 0f);

            SpriteRenderer sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = cfg.backgroundSprite;
            sr.color = new Color(0.92f, 0.94f, 0.96f, 1f);
            sr.sortingLayerName = GameConstants.SortFarBackground;
            sr.sortingOrder = 0;
            bgGo.transform.localScale = Vector3.one * 1.16f;

            StageBackdropController ctrl = bgGo.AddComponent<StageBackdropController>();
            ctrl.Configure(camTransform, center, 0.036f, 0.14f, 2.6f, 2.2f);

            if (cfg.showGodRays)
            {
                Sprite shaftSp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBg + "BG_L2_StoneLightShafts.png");
                if (shaftSp != null)
                {
                    GameObject rayGo = new GameObject("StageGodRays");
                    rayGo.transform.SetParent(bgGo.transform, false);
                    rayGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    rayGo.transform.localScale = Vector3.one * 1.15f;
                    SpriteRenderer raySr = rayGo.AddComponent<SpriteRenderer>();
                    raySr.sprite = shaftSp;
                    raySr.color = cfg.godRayTint;
                    raySr.sortingLayerName = GameConstants.SortMidBackground;
                    raySr.sortingOrder = 4;
                    if (godRayMat != null) raySr.sharedMaterial = godRayMat;
                    VolumetricGodRayAnimator anim = rayGo.AddComponent<VolumetricGodRayAnimator>();
                    anim.Configure(0.55f, 0.020f, 0.34f, 0.22f, cfg.stageIndex * 1.1f);
                }
            }

            // Foreground silhouette framing tied cleanly to the camera via a counter-parallax follower!
            if (cfg.foregroundFrameSprite != null)
            {
                GameObject fgGo = new GameObject("StageForegroundFrame");
                fgGo.transform.SetParent(bgGo.transform, false);
                fgGo.transform.localPosition = Vector3.zero;
                fgGo.transform.localScale = Vector3.one * 1.08f;
                SpriteRenderer fgSr = fgGo.AddComponent<SpriteRenderer>();
                fgSr.sprite = cfg.foregroundFrameSprite;
                fgSr.color = cfg.foregroundFrameTint;
                fgSr.sortingLayerName = GameConstants.SortForeground;
                fgSr.sortingOrder = 6;
            }

            return bgGo;
        }

        private static void BuildStageEnclosure(Transform parent, StageConfigData cfg, Sprite dripSprite, Material addMat)
        {
            Sprite cliffSp = cfg.cliffSprite != null ? cfg.cliffSprite : cfg.platformSprite;

            // Main Walkable Ground Floors
            if (cfg.stageIndex == 2)
            {
                // Twin bioluminescent briar root chasms across full 144u Shellwood expanse
                CreateSolidStageCliff(parent, "Floor_West", new Vector2(cfg.originX + 11.0f, cfg.groundY - 1.2f), new Vector2(24.0f, 2.4f), cliffSp, Color.white, false);
                CreateSolidStageCliff(parent, "Floor_PitBedWest", new Vector2(cfg.originX + 35.0f, cfg.groundY - 3.4f), new Vector2(24.0f, 2.4f), cliffSp, new Color(0.65f, 0.68f, 0.72f, 1f), false);
                CreateSolidStageCliff(parent, "Floor_MidHollow", new Vector2(cfg.originX + 72.0f, cfg.groundY - 1.2f), new Vector2(50.0f, 2.4f), cliffSp, Color.white, false);
                CreateSolidStageCliff(parent, "Floor_PitBedEast", new Vector2(cfg.originX + 109.0f, cfg.groundY - 3.4f), new Vector2(24.0f, 2.4f), cliffSp, new Color(0.65f, 0.68f, 0.72f, 1f), false);
                CreateSolidStageCliff(parent, "Floor_East", new Vector2(cfg.originX + 133.5f, cfg.groundY - 1.2f), new Vector2(25.0f, 2.4f), cliffSp, Color.white, false);
            }
            else
            {
                CreateSolidStageCliff(parent, "MainGround", new Vector2(cfg.originX + cfg.stageWidth * 0.5f, cfg.groundY - 1.2f), new Vector2(cfg.stageWidth + 4f, 2.4f), cliffSp, Color.white, cfg.slipperyGround);
            }

            // Outer Invisible Containment Barriers
            CreateInvisibleBarrier(parent, "LeftOuterWall", new Vector2(cfg.originX - 0.6f, cfg.groundY + 8.0f), new Vector2(1.4f, 20f));
            CreateInvisibleBarrier(parent, "RightOuterWall", new Vector2(cfg.originX + cfg.stageWidth + 0.6f, cfg.groundY + 8.0f), new Vector2(1.4f, 20f));

            // Visible Biome Ceiling Overhang (inverted cliff deck at Y=14.6 + hanging stalactites / icicles!)
            CreateStageCeilingOverhang(parent, cfg, cliffSp, dripSprite, addMat);
        }

        private static void CreateStageCeilingOverhang(Transform parent, StageConfigData cfg, Sprite ceilSprite, Sprite dripSprite, Material addMat)
        {
            // Keep open mountain sky cleaner in Courtyard & Karak outdoor spans while still framing top edges
            float ceilY = 14.8f;
            CreateInvisibleBarrier(parent, "CeilingCap", new Vector2(cfg.originX + cfg.stageWidth * 0.5f, ceilY + 0.9f), new Vector2(cfg.stageWidth + 4f, 2.0f));

            if (ceilSprite != null && cfg.stageIndex != 6)
            {
                GameObject visCeil = new GameObject("BiomeCeilingRim");
                visCeil.transform.SetParent(parent, false);
                visCeil.transform.position = new Vector3(cfg.originX + cfg.stageWidth * 0.5f, ceilY + 0.25f, 0f);
                Vector2 sz = new Vector2(cfg.stageWidth + 4f, 1.8f);
                float unitW = Mathf.Max(1.6f, ceilSprite.bounds.size.x);
                float unitH = Mathf.Max(0.8f, ceilSprite.bounds.size.y);
                int cols = Mathf.Max(1, Mathf.CeilToInt(sz.x / unitW));
                float segW = sz.x / cols;
                Color ceilTint = cfg.stageIndex == 7 ? new Color(0.55f, 0.48f, 0.44f, 1f) : new Color(0.58f, 0.62f, 0.68f, 1f);
                for (int c = 0; c < cols; c++)
                {
                    GameObject tile = new GameObject("Ceil_" + c);
                    tile.transform.SetParent(visCeil.transform, false);
                    tile.transform.localPosition = new Vector3(-sz.x * 0.5f + segW * (c + 0.5f), 0f, 0f);
                    tile.transform.localScale = new Vector3(((c % 2 == 1) ? -1f : 1f) * (segW * 1.06f / unitW), -(sz.y * 1.1f / unitH), 1f);
                    SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = ceilSprite;
                    sr.color = ceilTint;
                    sr.sortingLayerName = GameConstants.SortEnvironment;
                    sr.sortingOrder = 6;
                }
            }

            if (cfg.ceilingStalactiteSprite != null && (cfg.stageIndex == 1 || cfg.stageIndex == 2 || cfg.stageIndex == 3 || cfg.stageIndex == 5))
            {
                Color stTint = cfg.stageIndex == 5
                    ? Color.white
                    : (cfg.stageIndex == 3 ? new Color(0.82f, 0.66f, 0.54f, 0.94f) : new Color(0.56f, 0.60f, 0.72f, 0.95f));
                for (float lx = 10f; lx <= cfg.stageWidth - 10f; lx += 13.5f)
                {
                    GameObject st = new GameObject("CeilingStalactite_" + Mathf.RoundToInt(lx));
                    st.transform.SetParent(parent, false);
                    st.transform.position = new Vector3(cfg.originX + lx, ceilY - 0.55f, 0f);
                    float sx = (Mathf.RoundToInt(lx) % 2 == 0) ? 1.15f : -1.05f;
                    st.transform.localScale = new Vector3(sx, 1.12f, 1f);
                    SpriteRenderer sr = st.AddComponent<SpriteRenderer>();
                    sr.sprite = cfg.ceilingStalactiteSprite;
                    sr.color = stTint;
                    sr.sortingLayerName = GameConstants.SortEnvironment;
                    sr.sortingOrder = 5;
                }
            }

            // Living Water Drips in damp caverns (Weavenest, Shellwood, Bellhart)
            if (dripSprite != null && (cfg.stageIndex == 1 || cfg.stageIndex == 2 || cfg.stageIndex == 4))
            {
                for (float lx = 14f; lx <= cfg.stageWidth - 14f; lx += 18f)
                {
                    GameObject dGo = new GameObject("CeilingDrip_" + Mathf.RoundToInt(lx));
                    dGo.transform.SetParent(parent, false);
                    dGo.transform.position = new Vector3(cfg.originX + lx, ceilY - 1.2f, 0f);
                    CeilingWaterDripEmitter emitter = dGo.AddComponent<CeilingWaterDripEmitter>();
                    emitter.Configure(11f, 1.1f, 2.7f, 15.5f);
                    SilkenGrottoProjectBuilder.SetField(emitter, "waterDripSprite", dripSprite);
                    SilkenGrottoProjectBuilder.SetField(emitter, "additiveMaterial", addMat);
                }
            }
        }

        private static void CreateInvisibleBarrier(Transform parent, string name, Vector2 center, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static void CreateSolidStageCliff(Transform parent, string name, Vector2 center, Vector2 size, Sprite sprite, Color tint, bool isSlipperyIce)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            if (isSlipperyIce)
            {
                SurfaceMaterialZone ice = go.AddComponent<SurfaceMaterialZone>();
                ice.Configure(true, 0.38f, 0.20f, true);
            }

            BuildTiledCliffVisual(go.transform, size, sprite, tint, 3);
        }

        private static void CreateSolidStagePlatform(
            Transform parent, string name, Vector2 center, Vector2 size, Sprite sprite, Color tint, bool isSlipperyIce, Sprite archSprite, float localCenterY)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = Vector2.zero;

            if (isSlipperyIce)
            {
                SurfaceMaterialZone ice = go.AddComponent<SurfaceMaterialZone>();
                ice.Configure(true, 0.36f, 0.22f, true);
            }

            BuildTiledPlatformVisual(go.transform, size, sprite, tint, 4);

            // Attach decorative root/citadel support arch ONLY on low platforms floating close to the ground.
            if (archSprite != null)
            {
                GameObject archGo = new GameObject("UnderSupportArch");
                archGo.transform.SetParent(go.transform, false);
                archGo.transform.localPosition = new Vector3(0f, -size.y * 0.30f, 0f);
                float archBoundsW = Mathf.Max(1.6f, archSprite.bounds.size.x);
                float archBoundsH = Mathf.Max(0.8f, archSprite.bounds.size.y);
                float sx = Mathf.Clamp(size.x / archBoundsW * 0.88f, 0.92f, 1.55f);
                float targetDrop = name.StartsWith("FungusBridge_")
                    ? (localCenterY + 1.85f)
                    : Mathf.Max(1.6f, localCenterY - size.y * 0.25f);
                float sy = Mathf.Clamp(targetDrop / (archBoundsH * 0.95f), 0.72f, 1.55f);
                archGo.transform.localScale = new Vector3(sx, sy, 1f);
                SpriteRenderer asr = archGo.AddComponent<SpriteRenderer>();
                asr.sprite = archSprite;
                asr.color = new Color(0.84f, 0.88f, 0.92f, 0.88f);
                asr.sortingLayerName = GameConstants.SortNearBackground;
                asr.sortingOrder = 7;
            }
        }

        private static void BuildTiledCliffVisual(Transform parent, Vector2 size, Sprite sprite, Color tint, int sortOrder)
        {
            if (sprite == null) return;
            float unitW = Mathf.Max(1.8f, sprite.bounds.size.x);
            float unitH = Mathf.Max(0.8f, sprite.bounds.size.y);
            int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / unitW));
            int rows = size.y > 2.25f ? Mathf.Max(2, Mathf.CeilToInt(size.y / unitH)) : 1;
            float segW = size.x / cols;
            float segH = size.y / rows;
            const float topPadRatio = 0.04f;

            for (int r = 0; r < rows; r++)
            {
                bool isTop = (r == rows - 1);
                float rowBot = -size.y * 0.5f + segH * r;
                float rowTop = isTop ? (size.y * 0.5f + 0.03f) : (rowBot + segH);
                float calibratedH = isTop ? (rowTop - rowBot) / (1f - topPadRatio) : (rowTop - rowBot) * 1.12f;
                float localY = (rowBot + (rowBot + calibratedH)) * 0.5f;
                Color rowTint = isTop ? tint : new Color(tint.r * 0.65f, tint.g * 0.65f, tint.b * 0.68f, 1f);

                for (int c = 0; c < cols; c++)
                {
                    GameObject tile = new GameObject("CliffTile_" + r + "_" + c);
                    tile.transform.SetParent(parent, false);
                    float lx = -size.x * 0.5f + segW * (c + 0.5f);
                    tile.transform.localPosition = new Vector3(lx, localY, 0f);
                    float flipX = (c % 2 == 1 && cols > 1) ? -1f : 1f;
                    tile.transform.localScale = new Vector3(flipX * ((segW * 1.06f) / unitW), calibratedH / unitH, 1f);

                    SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.color = rowTint;
                    sr.sortingLayerName = GameConstants.SortEnvironment;
                    sr.sortingOrder = isTop ? (sortOrder + 1) : sortOrder;
                }
            }
        }

        private static void BuildTiledPlatformVisual(Transform parent, Vector2 size, Sprite sprite, Color tint, int sortOrder)
        {
            if (sprite == null) return;
            float unitW = Mathf.Max(1.6f, sprite.bounds.size.x);
            float unitH = Mathf.Max(0.45f, sprite.bounds.size.y);
            int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / unitW));
            float segW = size.x / cols;
            float visualH = size.y * 1.32f;
            float alignedLocalY = size.y * 0.5f + 0.03f - visualH * (0.5f - 0.025f);

            for (int c = 0; c < cols; c++)
            {
                GameObject tile = new GameObject("Tile_" + c);
                tile.transform.SetParent(parent, false);
                float lx = -size.x * 0.5f + segW * (c + 0.5f);
                tile.transform.localPosition = new Vector3(lx, alignedLocalY, 0f);
                float flipX = (c % 2 == 1 && cols > 1) ? -1f : 1f;
                tile.transform.localScale = new Vector3(flipX * ((segW * 1.04f) / unitW), visualH / unitH, 1f);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
                sr.sortingLayerName = GameConstants.SortEnvironment;
                sr.sortingOrder = sortOrder;
            }
        }

        private static void CreateStagePool(Transform parent, StageConfigData cfg, StagePoolSpec spec, Sprite waterSp, Sprite glintSp)
        {
            if (waterSp == null) return;
            GameObject root = new GameObject(spec.label);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(cfg.originX + spec.localBottomCenter.x, cfg.groundY + spec.localBottomCenter.y, 0f);

            float nativeW = Mathf.Max(1f, waterSp.bounds.size.x);
            float scaleX = spec.width / nativeW;

            GameObject poolVis = new GameObject("WaterPoolSurface");
            poolVis.transform.SetParent(root.transform, false);
            poolVis.transform.localPosition = new Vector3(0f, -0.40f, 0f);
            poolVis.transform.localScale = new Vector3(scaleX, 0.58f, 1f);
            SpriteRenderer psr = poolVis.AddComponent<SpriteRenderer>();
            psr.sprite = waterSp;
            psr.color = spec.waterTint;
            psr.sortingLayerName = GameConstants.SortEnvironment;
            psr.sortingOrder = 6;
            poolVis.AddComponent<WaterSurfaceShimmer>().Configure(1.35f, 0.04f, 0.008f, 0.012f, spec.localBottomCenter.x * 0.43f);

            if (glintSp != null)
            {
                GameObject glint = new GameObject("WaterSurfaceGlint");
                glint.transform.SetParent(root.transform, false);
                glint.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                glint.transform.localScale = new Vector3(scaleX * 0.88f, 0.52f, 1f);
                SpriteRenderer gsr = glint.AddComponent<SpriteRenderer>();
                gsr.sprite = glintSp;
                gsr.color = spec.glintTint;
                gsr.sortingLayerName = GameConstants.SortEnvironment;
                gsr.sortingOrder = 7;
                glint.AddComponent<WaterSurfaceShimmer>().Configure(2.15f, 0.16f, 0.032f, 0.014f, spec.localBottomCenter.x * 0.71f + 1.2f);
            }
        }

        private static void SpawnStageDecor(Transform parent, StageConfigData cfg, StageDecorSpec spec)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + spec.assetFileName);
            if (sp == null) return;

            GameObject go = new GameObject(spec.label);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cfg.originX + spec.localPos.x, cfg.groundY + spec.localPos.y, 0f);
            go.transform.localScale = new Vector3((spec.flipX ? -1f : 1f) * spec.scale, spec.scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = spec.tint;
            sr.sortingLayerName = string.IsNullOrEmpty(spec.sortLayer) ? GameConstants.SortNearBackground : spec.sortLayer;
            sr.sortingOrder = spec.sortOrder;

            if (spec.swayWithWind)
            {
                AmbientPropAnimator anim = go.AddComponent<AmbientPropAnimator>();
                anim.Configure(StageAmbientPropKind.SwayingFlora, sr, 1.85f, 3.2f, spec.localPos.x * 0.41f);
            }
        }

        private static void SpawnStageDestructible(
            Transform parent, string name, Vector2 surfacePos, BreakablePropKind kind,
            int maxHits, int orbDropCount, int essencePerOrb, float rewardSilk,
            bool solidObstacle, bool flipX, float scale, Sprite sprite, GameObject orbPrefab)
        {
            if (sprite == null) return;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(surfacePos.x, surfacePos.y, 0f);
            go.tag = GameConstants.TagBreakable;
            go.layer = solidObstacle ? GameConstants.LayerBreakableWall : GameConstants.LayerDestructibleFoliage;

            GameObject vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);

            SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = GameConstants.SortInteractables;
            sr.sortingOrder = solidObstacle ? 4 : 3;

            float colW = Mathf.Clamp(sprite.bounds.size.x * scale * 0.72f, 0.55f, 1.45f);
            float colH = Mathf.Clamp(sprite.bounds.size.y * scale * 0.85f, 0.65f, 2.2f);
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = !solidObstacle;
            col.offset = new Vector2(0f, colH * 0.5f);
            col.size = new Vector2(colW, colH);

            BreakableProp prop = go.AddComponent<BreakableProp>();
            prop.Configure(kind, maxHits, orbDropCount * essencePerOrb, rewardSilk, solidObstacle, sr, orbPrefab, orbDropCount, essencePerOrb);
        }

        private static void SpawnStageAmbientProp(Transform parent, StageConfigData cfg, StageAmbientPropSpec spec, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            GameObject go = new GameObject(spec.label);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cfg.originX + spec.localPos.x, cfg.groundY + spec.localPos.y, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            AmbientPropAnimator anim = go.AddComponent<AmbientPropAnimator>();
            float phase = spec.localPos.x * 0.37f;

            switch (spec.kind)
            {
                case StageAmbientPropKind.WarmLantern:
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_WeatherGlowMote.png");
                    sr.color = new Color(1.0f, 0.68f, 0.24f, 0.58f);
                    sr.sortingLayerName = GameConstants.SortVFX;
                    sr.sortingOrder = 6;
                    if (assets.AdditiveVFXMaterial != null) sr.sharedMaterial = assets.AdditiveVFXMaterial;
                    go.transform.localScale = Vector3.one * (3.4f * spec.scale);
                    anim.Configure(spec.kind, sr, 2.8f, 0.14f, phase);
                    break;

                case StageAmbientPropKind.CrimsonBanner:
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_CrimsonBanner.png");
                    sr.color = new Color(0.95f, 0.92f, 0.92f, 0.96f);
                    sr.sortingLayerName = GameConstants.SortNearBackground;
                    sr.sortingOrder = 5;
                    go.transform.localScale = new Vector3((spec.flipX ? -1f : 1f) * spec.scale, spec.scale, 1f);
                    anim.Configure(spec.kind, sr, 2.6f, 5.4f, phase);
                    break;

                case StageAmbientPropKind.SwayingWeb:
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_WebBarricade.png");
                    sr.color = new Color(0.84f, 0.88f, 0.96f, 0.34f);
                    sr.sortingLayerName = GameConstants.SortNearBackground;
                    sr.sortingOrder = 4;
                    go.transform.localScale = new Vector3((spec.flipX ? -1f : 1f) * spec.scale * 1.4f, spec.scale * 1.15f, 1f);
                    anim.Configure(spec.kind, sr, 1.3f, 3.8f, phase);
                    break;

                case StageAmbientPropKind.CathedralGodRay:
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_WeatherGlowMote.png");
                    sr.color = new Color(1.0f, 0.91f, 0.62f, 0.24f);
                    sr.sortingLayerName = GameConstants.SortMidBackground;
                    sr.sortingOrder = 5;
                    if (assets.AdditiveVFXMaterial != null) sr.sharedMaterial = assets.AdditiveVFXMaterial;
                    go.transform.localScale = new Vector3(4.2f * spec.scale, 13.5f * spec.scale, 1f);
                    go.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                    anim.Configure(spec.kind, sr, 0.9f, 0.22f, phase);
                    break;

                case StageAmbientPropKind.HangingBellCluster:
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Decor_BellhartHangingBell.png");
                    sr.color = new Color(0.94f, 0.94f, 0.98f, 0.96f);
                    sr.sortingLayerName = GameConstants.SortNearBackground;
                    sr.sortingOrder = 6;
                    go.transform.localScale = new Vector3((spec.flipX ? -1f : 1f) * spec.scale, spec.scale, 1f);
                    anim.Configure(spec.kind, sr, 1.25f, 3.2f, phase);
                    break;
            }
        }

        private static void SpawnStageFaunaSwarm(
            Transform parent, StageConfigData cfg, StageFaunaSwarmSpec spec, Sprite critterSp, Material addMat)
        {
            if (critterSp == null) return;
            GameObject go = new GameObject(spec.label);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cfg.originX + spec.localCenter.x, cfg.groundY + spec.localCenter.y, 0f);
            AmbientFaunaSwarm swarm = go.AddComponent<AmbientFaunaSwarm>();
            swarm.Configure(critterSp, addMat, spec.count, spec.roamRadius, spec.colorA, spec.colorB, 0.22f, 0.36f, 1.15f, 16f);
        }

        private static void SpawnStageSanctuaryAndEncounters(Transform parent, StageConfigData cfg, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            if (assets.CheckpointPrefab != null)
            {
                GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(assets.CheckpointPrefab);
                cp.name = "Checkpoint_" + cfg.stageId;
                cp.transform.SetParent(parent, false);
                float benchLocalX = cfg.stageIndex == 7 ? 72.5f : 6.4f;
                float benchY = cfg.stageIndex == 7 ? cfg.groundY + 2.0f : cfg.groundY;
                cp.transform.position = new Vector3(cfg.originX + benchLocalX, benchY, 0f);
                CheckpointShrine shrine = cp.GetComponent<CheckpointShrine>();
                if (shrine != null) shrine.Configure("cp_" + cfg.stageId, cfg.displayName + " Bench");
            }

            if (cfg.stageIndex == 7 && assets.UpgradeAltarPrefab != null)
            {
                GameObject altar = (GameObject)PrefabUtility.InstantiatePrefab(assets.UpgradeAltarPrefab);
                altar.name = "UpgradeAltar_Choral";
                altar.transform.SetParent(parent, false);
                altar.transform.position = new Vector3(cfg.originX + 67.5f, cfg.groundY + 2.0f, 0f);
            }

            // Mid-stage Sanctuaries & Secret Breakable Exploration Alcoves across expanded 140..148u maps
            SpawnStageExplorationFeatures(parent, cfg, assets);

            // Rich Biome-Adapted Fauna Encounters distributed smoothly across every map zone
            if (cfg.stageIndex == 1)
            {
                Color silkTint = new Color(0.84f, 0.88f, 1.0f, 1f);
                Color lancerSilk = new Color(0.78f, 0.84f, 1.0f, 1f);
                SpawnStageEnemy(assets.MossgrubPrefab, parent, "Weavenest_WebCrawlerW1", "weavenest_mg_w1", new Vector3(cfg.originX + 14.0f, 3.22f, 0f), assets.MossgrubStats, assets.SilkOrbPrefab, silkTint, 0.96f);
                SpawnStageEnemy(assets.MossmirPrefab, parent, "Weavenest_SilkMiteW1", "weavenest_mm_w1", new Vector3(cfg.originX + 26.0f, 6.20f, 0f), assets.MossmirStats, assets.SilkOrbPrefab, silkTint, 1.0f);
                SpawnStageEnemy(assets.LancerPrefab, parent, "Weavenest_LoomWardenW", "weavenest_l_w", new Vector3(cfg.originX + 42.0f, 3.25f, 0f), assets.LancerStats, assets.SilkOrbPrefab, lancerSilk, 1.0f);
                SpawnStageEnemy(assets.MossgrubPrefab, parent, "Weavenest_WebCrawlerMid", "weavenest_mg_mid", new Vector3(cfg.originX + 53.0f, 0.05f, 0f), assets.MossgrubStats, assets.SilkOrbPrefab, silkTint, 0.98f);
                SpawnStageEnemy(assets.MossmirPrefab, parent, "Weavenest_SilkMiteMid", "weavenest_mm_mid", new Vector3(cfg.originX + 74.0f, 9.20f, 0f), assets.MossmirStats, assets.SilkOrbPrefab, silkTint, 1.02f);
                SpawnStageEnemy(assets.LancerPrefab, parent, "Weavenest_LoomWardenMid", "weavenest_l_mid", new Vector3(cfg.originX + 85.0f, 3.22f, 0f), assets.LancerStats, assets.SilkOrbPrefab, lancerSilk, 1.0f);
                SpawnStageEnemy(assets.MossmirPrefab, parent, "Weavenest_SilkMiteE1", "weavenest_mm_e1", new Vector3(cfg.originX + 98.0f, 6.60f, 0f), assets.MossmirStats, assets.SilkOrbPrefab, silkTint, 1.0f);
                SpawnStageEnemy(assets.MossgrubPrefab, parent, "Weavenest_WebCrawlerE", "weavenest_mg_e", new Vector3(cfg.originX + 116.5f, 3.45f, 0f), assets.MossgrubStats, assets.SilkOrbPrefab, silkTint, 0.96f);
                SpawnStageEnemy(assets.LancerPrefab, parent, "Weavenest_LoomWardenE", "weavenest_l_e", new Vector3(cfg.originX + 124.0f, 0.05f, 0f), assets.LancerStats, assets.SilkOrbPrefab, lancerSilk, 1.04f);
                SpawnStageEnemy(assets.MossmirPrefab, parent, "Weavenest_SilkMiteE2", "weavenest_mm_e2", new Vector3(cfg.originX + 131.5f, 6.40f, 0f), assets.MossmirStats, assets.SilkOrbPrefab, silkTint, 1.0f);
            }
            else if (cfg.stageIndex == 2)
            {
                // 7 Shellwood biome species deployed across West Root Cliffs, Twin Briar Chasms, Mid Hollow & East Canopy
                SpawnStageEnemy(assets.PilgrimGrovellerPrefab, parent, "Shellwood_PilgrimGrovellerW", "shellwood_grov_w", new Vector3(cfg.originX + 14.0f, 3.22f, 0f), assets.PilgrimGrovellerStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageAknid(assets.AknidPrefab, parent, "Shellwood_AknidW", "shellwood_aknid_w", new Vector3(cfg.originX + 25.0f, 6.10f, 0f), assets.AknidStats, assets.SilkOrbPrefab, AknidCarryType.Berry);
                SpawnStageEnemy(assets.WingedPilgrimPrefab, parent, "Shellwood_WingedPilgrimW", "shellwood_wpilgrim_w", new Vector3(cfg.originX + 36.0f, 7.60f, 0f), assets.WingedPilgrimStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.PilgrimBellbearerPrefab, parent, "Shellwood_PilgrimBellbearerW", "shellwood_bell_w", new Vector3(cfg.originX + 43.0f, 2.05f, 0f), assets.PilgrimBellbearerStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.PondcatcherPrefab, parent, "Shellwood_PondcatcherMid", "shellwood_pond_mid", new Vector3(cfg.originX + 56.5f, 3.22f, 0f), assets.PondcatcherStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.CovetousPilgrimPrefab, parent, "Shellwood_CovetousPilgrimMid", "shellwood_covet_mid", new Vector3(cfg.originX + 67.0f, 0.02f, 0f), assets.CovetousPilgrimStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageAknid(assets.AknidPrefab, parent, "Shellwood_AknidMid", "shellwood_aknid_mid", new Vector3(cfg.originX + 86.5f, 9.20f, 0f), assets.AknidStats, assets.SilkOrbPrefab, AknidCarryType.Flea);
                SpawnStageEnemy(assets.PilgrimHulkPrefab, parent, "Shellwood_PilgrimHulkMid", "shellwood_hulk_mid", new Vector3(cfg.originX + 91.5f, 0.02f, 0f), assets.PilgrimHulkStats, assets.SilkOrbPrefab, Color.white, 1.06f);
                SpawnStageEnemy(assets.WingedPilgrimPrefab, parent, "Shellwood_WingedPilgrimE", "shellwood_wpilgrim_e", new Vector3(cfg.originX + 102.0f, 6.20f, 0f), assets.WingedPilgrimStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.PilgrimBellbearerPrefab, parent, "Shellwood_PilgrimBellbearerE", "shellwood_bell_e", new Vector3(cfg.originX + 109.0f, 5.15f, 0f), assets.PilgrimBellbearerStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.PilgrimGrovellerPrefab, parent, "Shellwood_PilgrimGrovellerE", "shellwood_grov_e", new Vector3(cfg.originX + 117.5f, 2.15f, 0f), assets.PilgrimGrovellerStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.PondcatcherPrefab, parent, "Shellwood_PondcatcherE", "shellwood_pond_e", new Vector3(cfg.originX + 128.0f, 3.52f, 0f), assets.PondcatcherStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.CovetousPilgrimPrefab, parent, "Shellwood_CovetousPilgrimE", "shellwood_covet_e", new Vector3(cfg.originX + 132.5f, 0.02f, 0f), assets.CovetousPilgrimStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.PilgrimHulkPrefab, parent, "Shellwood_PilgrimHulkE", "shellwood_hulk_e", new Vector3(cfg.originX + 137.0f, 0.02f, 0f), assets.PilgrimHulkStats, assets.SilkOrbPrefab, Color.white, 1.08f);
            }
            else if (cfg.stageIndex == 3)
            {
                // 4 Karak species across extended Coral Wastes (144u) — includes both Ground & CeilingDrop Pharlid ambushes!
                SpawnStagePharlid(assets.PharlidPrefab, parent, "Karak_PharlidA", "karak_pharlid_a", new Vector3(cfg.originX + 14.5f, 3.22f, 0f), assets.PharlidStats, assets.SilkOrbPrefab, PharlidAmbushMode.Ground);
                SpawnStageEnemy(assets.DriznitPrefab, parent, "Karak_DriznitA", "karak_driznit_a", new Vector3(cfg.originX + 25.0f, 7.60f, 0f), assets.DriznitStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.CoralFurmPrefab, parent, "Karak_CoralFurmA", "karak_coralfurm_a", new Vector3(cfg.originX + 30.0f, 0.02f, 0f), assets.CoralFurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ConchflyPrefab, parent, "Karak_ConchflyW", "karak_conchfly_w", new Vector3(cfg.originX + 39.0f, 5.80f, 0f), assets.ConchflyStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.CoralFurmPrefab, parent, "Karak_CoralFurmB", "karak_coralfurm_b", new Vector3(cfg.originX + 45.5f, 3.22f, 0f), assets.CoralFurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DriznitPrefab, parent, "Karak_DriznitMid", "karak_driznit_mid", new Vector3(cfg.originX + 58.0f, 8.20f, 0f), assets.DriznitStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStagePharlid(assets.PharlidPrefab, parent, "Karak_PharlidMid", "karak_pharlid_mid", new Vector3(cfg.originX + 58.5f, 4.12f, 0f), assets.PharlidStats, assets.SilkOrbPrefab, PharlidAmbushMode.CeilingDrop);
                SpawnStageEnemy(assets.CoralFurmPrefab, parent, "Karak_CoralFurmC", "karak_coralfurm_c", new Vector3(cfg.originX + 96.5f, 3.52f, 0f), assets.CoralFurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ConchflyPrefab, parent, "Karak_ConchflyE", "karak_conchfly_e", new Vector3(cfg.originX + 108.5f, 7.80f, 0f), assets.ConchflyStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStagePharlid(assets.PharlidPrefab, parent, "Karak_PharlidE", "karak_pharlid_e", new Vector3(cfg.originX + 114.5f, 0.02f, 0f), assets.PharlidStats, assets.SilkOrbPrefab, PharlidAmbushMode.Ground);
                SpawnStagePharlid(assets.PharlidPrefab, parent, "Karak_PharlidCeilingE", "karak_pharlid_ceil_e", new Vector3(cfg.originX + 118.5f, 6.52f, 0f), assets.PharlidStats, assets.SilkOrbPrefab, PharlidAmbushMode.CeilingDrop);
                SpawnStageEnemy(assets.DriznitPrefab, parent, "Karak_DriznitE1", "karak_driznit_e1", new Vector3(cfg.originX + 123.0f, 6.20f, 0f), assets.DriznitStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.CoralFurmPrefab, parent, "Karak_CoralFurmD", "karak_coralfurm_d", new Vector3(cfg.originX + 128.5f, 3.22f, 0f), assets.CoralFurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DriznitPrefab, parent, "Karak_DriznitE2", "karak_driznit_e2", new Vector3(cfg.originX + 135.5f, 5.40f, 0f), assets.DriznitStats, assets.SilkOrbPrefab, Color.white, 1.0f);
            }
            else if (cfg.stageIndex == 4)
            {
                // All 8 Bellhart species distributed spaciously across extended 148u Bellhart Streets, Roofs, Towers & Caves
                SpawnStageEnemy(assets.BellhartMossgrubPrefab, parent, "Bellhart_MossgrubA", "bellhart_mossgrub_a", new Vector3(cfg.originX + 12.2f, 0.02f, 0f), assets.BellhartMossgrubStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.FurmPrefab, parent, "Bellhart_FurmA", "bellhart_furm_a", new Vector3(cfg.originX + 14.5f, 3.32f, 0f), assets.FurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStagePharlid(assets.PharlidDiverPrefab, parent, "Bellhart_PharlidDiverA", "bellhart_pharlid_a", new Vector3(cfg.originX + 19.5f, 10.60f, 0f), assets.PharlidDiverStats, assets.SilkOrbPrefab, PharlidAmbushMode.CeilingDrop);
                SpawnStageEnemy(assets.BellhartMossmirPrefab, parent, "Bellhart_MossmirA", "bellhart_mossmir_a", new Vector3(cfg.originX + 24.0f, 6.40f, 0f), assets.BellhartMossmirStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MawlingPrefab, parent, "Bellhart_MawlingA", "bellhart_mawling_a", new Vector3(cfg.originX + 26.0f, 4.75f, 0f), assets.MawlingStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MawlingPrefab, parent, "Bellhart_MawlingB", "bellhart_mawling_b", new Vector3(cfg.originX + 32.0f, 0.02f, 0f), assets.MawlingStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.SkullwingPrefab, parent, "Bellhart_SkullwingA", "bellhart_skullwing_a", new Vector3(cfg.originX + 37.0f, 9.20f, 0f), assets.SkullwingStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.BellhartMossgrubPrefab, parent, "Bellhart_MossgrubB", "bellhart_mossgrub_b", new Vector3(cfg.originX + 37.0f, 6.95f, 0f), assets.BellhartMossgrubStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.FurmPrefab, parent, "Bellhart_FurmB", "bellhart_furm_b", new Vector3(cfg.originX + 48.5f, 3.52f, 0f), assets.FurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.WingedFurmPrefab, parent, "Bellhart_WingedFurmA", "bellhart_wfurm_a", new Vector3(cfg.originX + 58.0f, 9.40f, 0f), assets.WingedFurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MarrowmawPrefab, parent, "Bellhart_Marrowmaw", "bellhart_marrowmaw_w", new Vector3(cfg.originX + 61.5f, 0.02f, 0f), assets.MarrowmawStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStagePharlid(assets.PharlidDiverPrefab, parent, "Bellhart_PharlidDiverB", "bellhart_pharlid_b", new Vector3(cfg.originX + 86.0f, 10.60f, 0f), assets.PharlidDiverStats, assets.SilkOrbPrefab, PharlidAmbushMode.CeilingDrop);
                SpawnStageEnemy(assets.BellhartMossmirPrefab, parent, "Bellhart_MossmirB", "bellhart_mossmir_b", new Vector3(cfg.originX + 94.0f, 5.80f, 0f), assets.BellhartMossmirStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.FurmPrefab, parent, "Bellhart_FurmC", "bellhart_furm_c", new Vector3(cfg.originX + 101.5f, 3.52f, 0f), assets.FurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MawlingPrefab, parent, "Bellhart_MawlingC", "bellhart_mawling_c", new Vector3(cfg.originX + 108.5f, 0.02f, 0f), assets.MawlingStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.SkullwingPrefab, parent, "Bellhart_SkullwingB", "bellhart_skullwing_b", new Vector3(cfg.originX + 114.0f, 8.20f, 0f), assets.SkullwingStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.WingedFurmPrefab, parent, "Bellhart_WingedFurmB", "bellhart_wfurm_b", new Vector3(cfg.originX + 125.0f, 9.80f, 0f), assets.WingedFurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.BellhartMossgrubPrefab, parent, "Bellhart_MossgrubC", "bellhart_mossgrub_c", new Vector3(cfg.originX + 124.5f, 7.15f, 0f), assets.BellhartMossgrubStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MarrowmawPrefab, parent, "Bellhart_MarrowmawE", "bellhart_marrowmaw_e", new Vector3(cfg.originX + 129.0f, 0.02f, 0f), assets.MarrowmawStats, assets.SilkOrbPrefab, Color.white, 1.04f);
                SpawnStageEnemy(assets.FurmPrefab, parent, "Bellhart_FurmD", "bellhart_furm_d", new Vector3(cfg.originX + 134.5f, 3.32f, 0f), assets.FurmStats, assets.SilkOrbPrefab, Color.white, 1.0f);
            }
            else if (cfg.stageIndex == 5)
            {
                // 4 Mount Fay glacial species across 144u Blizzard Ridge
                SpawnStageEnemy(assets.ServitorBoranPrefab, parent, "MountFay_ServitorBoranA", "fay_boran_a", new Vector3(cfg.originX + 14.5f, 3.22f, 0f), assets.ServitorBoranStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MnemonidPrefab, parent, "MountFay_MnemonidA", "fay_mnemonid_a", new Vector3(cfg.originX + 25.0f, 7.60f, 0f), assets.MnemonidStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DriftlinPrefab, parent, "MountFay_DriftlinA", "fay_driftlin_a", new Vector3(cfg.originX + 36.0f, 11.20f, 0f), assets.DriftlinStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MnemonordPrefab, parent, "MountFay_MnemonordW", "fay_mnemonord_w", new Vector3(cfg.originX + 47.5f, 7.40f, 0f), assets.MnemonordStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ServitorBoranPrefab, parent, "MountFay_ServitorBoranMid", "fay_boran_mid", new Vector3(cfg.originX + 59.0f, 3.22f, 0f), assets.ServitorBoranStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MnemonidPrefab, parent, "MountFay_MnemonidMid", "fay_mnemonid_mid", new Vector3(cfg.originX + 88.0f, 9.40f, 0f), assets.MnemonidStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MnemonordPrefab, parent, "MountFay_Mnemonord", "fay_mnemonord", new Vector3(cfg.originX + 101.0f, 6.80f, 0f), assets.MnemonordStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DriftlinPrefab, parent, "MountFay_DriftlinB", "fay_driftlin_b", new Vector3(cfg.originX + 114.0f, 9.60f, 0f), assets.DriftlinStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.MnemonidPrefab, parent, "MountFay_MnemonidE", "fay_mnemonid_e", new Vector3(cfg.originX + 124.0f, 10.60f, 0f), assets.MnemonidStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ServitorBoranPrefab, parent, "MountFay_ServitorBoranB", "fay_boran_b", new Vector3(cfg.originX + 133.5f, 3.22f, 0f), assets.ServitorBoranStats, assets.SilkOrbPrefab, Color.white, 1.0f);
            }
            else if (cfg.stageIndex == 6)
            {
                // Grand Gate & Courtyard Drapemite & Drapefly ambush across 140u outer Citadel cloisters
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteA", "court_drapemite_a", new Vector3(cfg.originX + 13.5f, 3.22f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapeflyPrefab, parent, "Courtyard_DrapeflyA", "court_drapefly_a", new Vector3(cfg.originX + 22.0f, 8.20f, 0f), assets.DrapeflyStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteB", "court_drapemite_b", new Vector3(cfg.originX + 24.5f, 4.76f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteC", "court_drapemite_c", new Vector3(cfg.originX + 38.0f, 0.02f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapeflyPrefab, parent, "Courtyard_DrapeflyB", "court_drapefly_b", new Vector3(cfg.originX + 46.0f, 9.60f, 0f), assets.DrapeflyStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteD", "court_drapemite_d", new Vector3(cfg.originX + 60.5f, 4.96f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapeflyPrefab, parent, "Courtyard_DrapeflyMid", "court_drapefly_mid", new Vector3(cfg.originX + 86.0f, 9.80f, 0f), assets.DrapeflyStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteE", "court_drapemite_e", new Vector3(cfg.originX + 100.0f, 3.22f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapeflyPrefab, parent, "Courtyard_DrapeflyC", "court_drapefly_c", new Vector3(cfg.originX + 112.5f, 8.40f, 0f), assets.DrapeflyStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteF", "court_drapemite_f", new Vector3(cfg.originX + 118.0f, 0.02f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.DrapemitePrefab, parent, "Courtyard_DrapemiteG", "court_drapemite_g", new Vector3(cfg.originX + 126.0f, 3.22f, 0f), assets.DrapemiteStats, assets.SilkOrbPrefab, Color.white, 1.0f);
            }
            else if (cfg.stageIndex == 7)
            {
                // Aula Choral Chambers: Citadel inner sanctum garrison across 140u hall (SanctumDais bench/altar calm around localX 67.5..72.5)
                SpawnStageEnemy(assets.ChoristorPrefab, parent, "Aula_ChoristorW", "aula_choristor_w", new Vector3(cfg.originX + 14.5f, 3.96f, 0f), assets.ChoristorStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoirHornheadPrefab, parent, "Aula_ChoirHornheadW", "aula_hornhead_w", new Vector3(cfg.originX + 20.0f, 0.02f, 0f), assets.ChoirHornheadStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ReedPrefab, parent, "Aula_ReedW", "aula_reed_w", new Vector3(cfg.originX + 26.0f, 7.16f, 0f), assets.ReedStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoirFlyerPrefab, parent, "Aula_ChoirFlyerW", "aula_flyer_w", new Vector3(cfg.originX + 35.0f, 9.60f, 0f), assets.ChoirFlyerStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.GrandReedPrefab, parent, "Aula_GrandReedW", "aula_grand_reed_w", new Vector3(cfg.originX + 38.0f, 3.22f, 0f), assets.GrandReedStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoirElderPrefab, parent, "Aula_ChoirElderW", "aula_choir_elder_w", new Vector3(cfg.originX + 52.0f, 0.02f, 0f), assets.ChoirElderStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoirFlyerPrefab, parent, "Aula_ChoirFlyerC", "aula_flyer_c", new Vector3(cfg.originX + 71.0f, 10.60f, 0f), assets.ChoirFlyerStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.GrandReedPrefab, parent, "Aula_GrandReedCenter", "aula_grand_reed_c", new Vector3(cfg.originX + 71.0f, 6.76f, 0f), assets.GrandReedStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoirHornheadPrefab, parent, "Aula_ChoirHornheadE", "aula_hornhead_e", new Vector3(cfg.originX + 100.0f, 0.02f, 0f), assets.ChoirHornheadStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoristorPrefab, parent, "Aula_ChoristorE", "aula_choristor_e", new Vector3(cfg.originX + 105.5f, 4.56f, 0f), assets.ChoristorStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ReedPrefab, parent, "Aula_ReedE", "aula_reed_e", new Vector3(cfg.originX + 117.0f, 7.16f, 0f), assets.ReedStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.ChoirElderPrefab, parent, "Aula_ChoirElder", "aula_choir_elder", new Vector3(cfg.originX + 121.0f, 0.02f, 0f), assets.ChoirElderStats, assets.SilkOrbPrefab, Color.white, 1.0f);
                SpawnStageEnemy(assets.GrandReedPrefab, parent, "Aula_GrandReedFinal", "aula_grand_reed_e", new Vector3(cfg.originX + 128.5f, 3.22f, 0f), assets.GrandReedStats, assets.SilkOrbPrefab, Color.white, 1.04f);
            }
        }

        private static void SpawnStageExplorationFeatures(Transform parent, StageConfigData cfg, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            if (cfg.stageIndex == 1)
            {
                SpawnExtraBench(assets, parent, "Checkpoint_WeavenestMid", "cp_weavenest_mid", "Weavenest Mid Loom", new Vector3(cfg.originX + 74.5f, cfg.groundY, 0f));
                SpawnSecretVault(assets, parent, cfg, "wall_weavenest_vault", "Hidden Loom Archive Opened!", 58.6f, 64.5f, "pickup_s1_spool", "item_woven_spool", 20);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 29.8f, 7.65f, 0f), 4, 1.5f, 20);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 103.5f, 8.25f, 0f), 4, 1.5f, 20);
                SpawnStagePickup(assets, parent, "Pickup_S1_Shard", "pickup_s1_shard", "item_essence_shard", 1, new Vector3(cfg.originX + 74.0f, 8.85f, 0f));
            }
            else if (cfg.stageIndex == 2)
            {
                SpawnExtraBench(assets, parent, "Checkpoint_ShellwoodMid", "cp_shellwood_mid", "Shellwood Hollow Bench", new Vector3(cfg.originX + 61.5f, cfg.groundY, 0f));
                SpawnStageAltar(assets, parent, "UpgradeAltar_Shellwood", new Vector3(cfg.originX + 54.0f, cfg.groundY, 0f));
                SpawnSecretVault(assets, parent, cfg, "wall_shellwood_vault", "Overgrown Root Cache Uncovered!", 69.0f, 75.0f, "pickup_s2_mossberry", "item_mossberry_dew", 22);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 32.5f, 5.65f, 0f), 4, 1.5f, 20);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 133.2f, 7.45f, 0f), 3, 1.6f, 22);
                SpawnStagePickup(assets, parent, "Pickup_S2_Shard", "pickup_s2_shard", "item_essence_shard", 1, new Vector3(cfg.originX + 86.5f, 8.05f, 0f));
            }
            else if (cfg.stageIndex == 3)
            {
                SpawnExtraBench(assets, parent, "Checkpoint_KarakMid", "cp_karak_mid", "Karak Oasis Watch", new Vector3(cfg.originX + 89.0f, cfg.groundY, 0f));
                SpawnStageAltar(assets, parent, "UpgradeAltar_Karak", new Vector3(cfg.originX + 51.5f, cfg.groundY, 0f));
                SpawnSecretVault(assets, parent, cfg, "wall_karak_vault", "Buried Coral Cache Opened!", 68.5f, 74.5f, "pickup_s3_shard", "item_essence_shard", 25);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 33.0f, 7.85f, 0f), 4, 1.5f, 22);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 116.5f, 8.45f, 0f), 4, 1.5f, 22);
                SpawnStagePickup(assets, parent, "Pickup_S3_Spool", "pickup_s3_spool", "item_woven_spool", 1, new Vector3(cfg.originX + 86.0f, 8.45f, 0f));
            }
            else if (cfg.stageIndex == 4)
            {
                SpawnExtraBench(assets, parent, "Checkpoint_BellhartMid", "cp_bellhart_mid", "Bellhart Plaza Bench", new Vector3(cfg.originX + 67.0f, cfg.groundY, 0f));
                SpawnStageAltar(assets, parent, "UpgradeAltar_Bellhart", new Vector3(cfg.originX + 54.0f, cfg.groundY, 0f));
                SpawnSecretVault(assets, parent, cfg, "wall_bellhart_vault", "Bellringer Crypt Uncovered!", 71.5f, 77.5f, "pickup_s4_mossberry", "item_mossberry_dew", 25);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 35.0f, 7.65f, 0f), 4, 1.5f, 22);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 88.0f, 8.05f, 0f), 4, 1.5f, 25);
                SpawnStagePickup(assets, parent, "Pickup_S4_Shard", "pickup_s4_shard", "item_essence_shard", 1, new Vector3(cfg.originX + 124.5f, 8.05f, 0f));
            }
            else if (cfg.stageIndex == 5)
            {
                SpawnExtraBench(assets, parent, "Checkpoint_FayMid", "cp_mount_fay_mid", "Mount Fay Ridge Shelter", new Vector3(cfg.originX + 65.5f, cfg.groundY, 0f));
                SpawnStageAltar(assets, parent, "UpgradeAltar_Fay", new Vector3(cfg.originX + 94.5f, cfg.groundY, 0f));
                SpawnSecretVault(assets, parent, cfg, "wall_fay_vault", "Glacial Bell Cache Revealed!", 71.0f, 77.0f, "pickup_s5_shard", "item_essence_shard", 28);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 34.0f, 8.85f, 0f), 4, 1.5f, 25);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 121.5f, 9.25f, 0f), 4, 1.5f, 25);
                SpawnStagePickup(assets, parent, "Pickup_S5_Spool", "pickup_s5_spool", "item_woven_spool", 1, new Vector3(cfg.originX + 89.0f, 8.05f, 0f));
            }
            else if (cfg.stageIndex == 6)
            {
                SpawnExtraBench(assets, parent, "Checkpoint_CourtyardMid", "cp_courtyard_mid", "Citadel Cloister Bench", new Vector3(cfg.originX + 55.0f, cfg.groundY, 0f));
                SpawnSecretVault(assets, parent, cfg, "wall_courtyard_vault", "Cloister Armory Uncovered!", 69.0f, 75.0f, "pickup_s6_spool", "item_woven_spool", 28);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 34.0f, 7.85f, 0f), 4, 1.5f, 26);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 86.5f, 7.75f, 0f), 4, 1.5f, 26);
                SpawnStagePickup(assets, parent, "Pickup_S6_Shard", "pickup_s6_shard", "item_essence_shard", 1, new Vector3(cfg.originX + 36.0f, 8.15f, 0f));
            }
            else if (cfg.stageIndex == 7)
            {
                SpawnSecretVault(assets, parent, cfg, "wall_choral_vault", "Grand Choir Reliquary Opened!", 85.0f, 91.0f, "pickup_s7_relic", "item_essence_shard", 35);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 24.0f, 7.85f, 0f), 4, 1.5f, 30);
                SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + 115.0f, 7.85f, 0f), 4, 1.5f, 30);
                SpawnStagePickup(assets, parent, "Pickup_S7_Dew", "pickup_s7_dew", "item_mossberry_dew", 1, new Vector3(cfg.originX + 71.0f, 7.85f, 0f));
            }
        }

        private static void SpawnExtraBench(SilkenGrottoProjectBuilder.BuiltAssets assets, Transform parent, string name, string cpId, string title, Vector3 pos)
        {
            if (assets.CheckpointPrefab == null) return;
            GameObject cp = (GameObject)PrefabUtility.InstantiatePrefab(assets.CheckpointPrefab);
            cp.name = name;
            cp.transform.SetParent(parent, false);
            cp.transform.position = pos;
            CheckpointShrine shrine = cp.GetComponent<CheckpointShrine>();
            if (shrine != null) shrine.Configure(cpId, title);
        }

        private static void SpawnStageAltar(SilkenGrottoProjectBuilder.BuiltAssets assets, Transform parent, string name, Vector3 pos)
        {
            if (assets.UpgradeAltarPrefab == null) return;
            GameObject altar = (GameObject)PrefabUtility.InstantiatePrefab(assets.UpgradeAltarPrefab);
            altar.name = name;
            altar.transform.SetParent(parent, false);
            altar.transform.position = pos;
        }

        private static void SpawnSecretVault(
            SilkenGrottoProjectBuilder.BuiltAssets assets, Transform parent, StageConfigData cfg,
            string wallId, string message, float wallLocalX, float rewardLocalX, string pickupId, string itemId, int orbValue)
        {
            const float secretShelfTopY = 6.20f;
            if (assets.BreakableWallPrefab != null)
            {
                GameObject wallGo = (GameObject)PrefabUtility.InstantiatePrefab(assets.BreakableWallPrefab);
                wallGo.name = "SecretWall_" + wallId;
                wallGo.transform.SetParent(parent, false);
                wallGo.transform.position = new Vector3(cfg.originX + wallLocalX, cfg.groundY + secretShelfTopY + 1.6f, 0f);
                BreakableWall bw = wallGo.GetComponent<BreakableWall>();
                if (bw != null) bw.Configure(wallId, 3, message);
            }
            SpawnStageOrbTrail(assets.SilkOrbPrefab, parent, new Vector3(cfg.originX + rewardLocalX - 1.5f, cfg.groundY + secretShelfTopY + 1.1f, 0f), 4, 1.35f, orbValue);
            SpawnStagePickup(assets, parent, "SecretPickup_" + pickupId, pickupId, itemId, 1, new Vector3(cfg.originX + rewardLocalX + 1.4f, cfg.groundY + secretShelfTopY + 0.95f, 0f));
        }

        private static void SpawnStageOrbTrail(GameObject orbPrefab, Transform parent, Vector3 startPos, int count, float spacing, int valueEach)
        {
            if (orbPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                GameObject orb = (GameObject)PrefabUtility.InstantiatePrefab(orbPrefab);
                orb.name = "StageSilkOrb_" + i;
                orb.transform.SetParent(parent, false);
                orb.transform.position = startPos + new Vector3(i * spacing, 0f, 0f);
                SilkOrbPickup p = orb.GetComponent<SilkOrbPickup>();
                if (p != null)
                {
                    SerializedObject so = new SerializedObject(p);
                    SerializedProperty valProp = so.FindProperty("essenceValue");
                    if (valProp != null) valProp.intValue = valueEach;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SpawnStagePickup(
            SilkenGrottoProjectBuilder.BuiltAssets assets, Transform parent,
            string objName, string pickupId, string itemId, int count, Vector3 worldPos)
        {
            if (assets == null || assets.InventoryPickupPrefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(assets.InventoryPickupPrefab);
            go.name = objName;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            InventoryPickup pickup = go.GetComponent<InventoryPickup>();
            InventoryItemData itemData = null;
            if (assets.ItemById != null) assets.ItemById.TryGetValue(itemId, out itemData);
            if (pickup != null)
            {
                Sprite iconSprite = itemData != null ? itemData.icon : null;
                pickup.Configure(pickupId, itemId, count, iconSprite);
                SerializedObject so = new SerializedObject(pickup);
                SerializedProperty idProp = so.FindProperty("pickupId");
                SerializedProperty itemProp = so.FindProperty("itemId");
                SerializedProperty qtyProp = so.FindProperty("quantity");
                if (idProp != null) idProp.stringValue = pickupId;
                if (itemProp != null) itemProp.stringValue = itemId;
                if (qtyProp != null) qtyProp.intValue = Mathf.Max(1, count);
                so.ApplyModifiedPropertiesWithoutUndo();
                SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && iconSprite != null)
                {
                    SerializedObject srSo = new SerializedObject(sr);
                    SerializedProperty spProp = srSo.FindProperty("m_Sprite");
                    if (spProp != null) spProp.objectReferenceValue = iconSprite;
                    srSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SpawnStageAknid(
            GameObject prefab, Transform parent, string name, string id, Vector3 pos,
            EnemyStatsData stats, GameObject orbPrefab, AknidCarryType carryPayload)
        {
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            EnemyController ec = go.GetComponent<EnemyController>();
            if (ec != null)
            {
                SerializedObject so = new SerializedObject(ec);
                SerializedProperty idP = so.FindProperty("enemyId");
                SerializedProperty stP = so.FindProperty("statsData");
                SerializedProperty orbP = so.FindProperty("silkOrbPrefab");
                if (idP != null) idP.stringValue = id;
                if (stP != null) stP.objectReferenceValue = stats;
                if (orbP != null && orbPrefab != null) orbP.objectReferenceValue = orbPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AknidAI ai = go.GetComponent<AknidAI>();
            if (ai != null)
            {
                SerializedObject aiSo = new SerializedObject(ai);
                SerializedProperty carryP = aiSo.FindProperty("carryType");
                if (carryP != null) carryP.intValue = (int)carryPayload;
                aiSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SpawnStagePharlid(
            GameObject prefab, Transform parent, string name, string id, Vector3 pos,
            EnemyStatsData stats, GameObject orbPrefab, PharlidAmbushMode ambushMode)
        {
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            EnemyController ec = go.GetComponent<EnemyController>();
            if (ec != null)
            {
                SerializedObject so = new SerializedObject(ec);
                SerializedProperty idP = so.FindProperty("enemyId");
                SerializedProperty stP = so.FindProperty("statsData");
                SerializedProperty orbP = so.FindProperty("silkOrbPrefab");
                if (idP != null) idP.stringValue = id;
                if (stP != null) stP.objectReferenceValue = stats;
                if (orbP != null && orbPrefab != null) orbP.objectReferenceValue = orbPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            KarakEnemyAI ai = go.GetComponent<KarakEnemyAI>();
            if (ai != null)
            {
                SerializedObject aiSo = new SerializedObject(ai);
                SerializedProperty modeP = aiSo.FindProperty("ambushMode");
                if (modeP != null) modeP.intValue = (int)ambushMode;
                aiSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SpawnStageEnemy(
            GameObject prefab, Transform parent, string name, string id, Vector3 pos,
            EnemyStatsData stats, GameObject orbPrefab, Color biomeTint, float scaleMult)
        {
            if (prefab == null) return;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (Mathf.Abs(scaleMult - 1f) > 0.01f)
            {
                go.transform.localScale = go.transform.localScale * scaleMult;
            }

            EnemyController ec = go.GetComponent<EnemyController>();
            if (ec != null)
            {
                SerializedObject so = new SerializedObject(ec);
                SerializedProperty idP = so.FindProperty("enemyId");
                SerializedProperty stP = so.FindProperty("statsData");
                SerializedProperty orbP = so.FindProperty("silkOrbPrefab");
                if (idP != null) idP.stringValue = id;
                if (stP != null) stP.objectReferenceValue = stats;
                if (orbP != null && orbPrefab != null) orbP.objectReferenceValue = orbPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                SerializedObject srSo = new SerializedObject(sr);
                SerializedProperty colP = srSo.FindProperty("m_Color");
                if (colP != null) colP.colorValue = biomeTint;
                srSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SpawnGateVisual(Transform parent, string name, Vector3 pos, Sprite gateSprite, float scale, bool flipX)
        {
            if (gateSprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = gateSprite;
            sr.sortingLayerName = GameConstants.SortEnvironment;
            sr.sortingOrder = 6;
        }

        private static CaveEntrancePortal CreateStagePortalEndpoint(
            Transform parent, string name, Vector3 pos, Sprite glowSprite, Material additiveMat, int enterDir, Color glowTint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.layer = GameConstants.LayerInteractable;

            BoxCollider2D trig = go.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            trig.offset = new Vector2(0f, 2.0f);
            trig.size = new Vector2(1.9f, 4.0f);

            GameObject glowGo = new GameObject("DoorwayRadialGlow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(enterDir >= 0 ? -0.35f : 0.35f, 2.1f, 0f);
            glowGo.transform.localScale = new Vector3(enterDir >= 0 ? 1.75f : -1.75f, 1.35f, 1f);
            SpriteRenderer sr = glowGo.AddComponent<SpriteRenderer>();
            sr.sprite = glowSprite;
            sr.color = glowTint;
            sr.sortingLayerName = GameConstants.SortVFX;
            sr.sortingOrder = 4;
            if (additiveMat != null) sr.sharedMaterial = additiveMat;

            return go.AddComponent<CaveEntrancePortal>();
        }

        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/0. Moss Grotto (Exit Gate)", false, 40)]
        private static void WarpStage0Exit() { WarpPlayModePlayerToStage(0, true); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/1. Weavenest Atla", false, 41)]
        private static void WarpStage1() { WarpPlayModePlayerToStage(1, false); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/2. Shellwood", false, 42)]
        private static void WarpStage2() { WarpPlayModePlayerToStage(2, false); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/3. Sands of Karak", false, 43)]
        private static void WarpStage3() { WarpPlayModePlayerToStage(3, false); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/4. Bellhart", false, 44)]
        private static void WarpStage4() { WarpPlayModePlayerToStage(4, false); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/5. Mount Fay (Blizzard)", false, 45)]
        private static void WarpStage5() { WarpPlayModePlayerToStage(5, false); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/6. Grand Gate & Courtyard", false, 46)]
        private static void WarpStage6() { WarpPlayModePlayerToStage(6, false); }
        [MenuItem("Silken Grotto/Warp Stage (Play Mode)/7. Aula Choral Chambers (Final)", false, 47)]
        private static void WarpStage7() { WarpPlayModePlayerToStage(7, false); }

        private static void WarpPlayModePlayerToStage(int stageIndex, bool nearRightExit)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[SilkenGrotto] Masuk Play Mode terlebih dahulu untuk menggunakan Warp Stage.");
                return;
            }
            List<StageConfigData> all = LoadAllStageConfigsFromDisk();
            if (stageIndex < 0 || stageIndex >= all.Count || all[stageIndex] == null) return;
            StageConfigData cfg = all[stageIndex];
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player == null) return;

            Vector3 targetPos = nearRightExit
                ? new Vector3(cfg.originX + cfg.rightExitOffset.x - 4.0f, cfg.groundY + cfg.rightExitOffset.y + 0.05f, 0f)
                : new Vector3(cfg.originX + cfg.leftSpawnOffset.x, cfg.groundY + cfg.leftSpawnOffset.y + 0.05f, 0f);

            AreaZoneTrigger.ClearActiveZonesForTransition();
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            player.transform.position = targetPos;
            if (StageProgressionManager.Instance != null) StageProgressionManager.Instance.ActivateStage(cfg.stageId);
            CameraFollow2D cam = Object.FindObjectOfType<CameraFollow2D>();
            if (cam != null)
            {
                if (stageIndex == 0 && nearRightExit)
                    cam.SetZoneBounds(new Vector2(100f, 8f), new Vector2(144.5f, 29f));
                else
                    cam.SetZoneBounds(cfg.cameraMinBounds, cfg.cameraMaxBounds);
                cam.SnapToTarget();
            }
            AreaZoneTrigger.SetCurrentAreaBanner(cfg.stageId, cfg.displayName, cfg.subtitle);
        }

        private static Color GetPortalGlowColor(WeatherProfileType w)
        {
            switch (w)
            {
                case WeatherProfileType.ShellwoodBiolumFog: return new Color(0.76f, 0.64f, 1.0f, 0.45f);
                case WeatherProfileType.KarakSandstorm: return new Color(1.0f, 0.84f, 0.56f, 0.45f);
                case WeatherProfileType.MountFayBlizzard:
                case WeatherProfileType.CourtyardLightSnowWind: return new Color(0.80f, 0.94f, 1.0f, 0.45f);
                case WeatherProfileType.ChoralGoldenDust: return new Color(1.0f, 0.92f, 0.64f, 0.48f);
                default: return new Color(0.88f, 0.98f, 0.82f, 0.45f);
            }
        }

        private static Sprite LoadDestructibleSprite(BreakablePropKind kind)
        {
            switch (kind)
            {
                case BreakablePropKind.RottenLog: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_RottenLog.png");
                case BreakablePropKind.SilkSpool: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_SilkSpool.png");
                case BreakablePropKind.DrySilkCocoon: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_SilkCocoon.png");
                case BreakablePropKind.WebBarricade: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_WebBarricade.png");
                case BreakablePropKind.HollowBugShell: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_HollowShell.png");
                case BreakablePropKind.FragileWoodPillar: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_WoodPillar.png");
                case BreakablePropKind.DryCoralFormation: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_DryCoral.png");
                case BreakablePropKind.RustedGear: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_RustedGear.png");
                case BreakablePropKind.AncientBonePile: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_BonePile.png");
                case BreakablePropKind.CrackedBell: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_CrackedBell.png");
                case BreakablePropKind.TownCrate: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_TownCrate.png");
                case BreakablePropKind.OldLantern: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_OldLantern.png");
                case BreakablePropKind.IceCrystalCluster: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_IceCrystal.png");
                case BreakablePropKind.FrozenWoodPost: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_FrozenPost.png");
                case BreakablePropKind.CrackedSentinelStatue: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_CrackedStatue.png");
                case BreakablePropKind.GildedPalaceUrn: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_GildedUrn.png");
                case BreakablePropKind.RustedChain: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_RustedChain.png");
                case BreakablePropKind.ChoirBench: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_ChoirBench.png");
                case BreakablePropKind.BrassCandelabra: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_BrassCandelabra.png");
                case BreakablePropKind.CeremonialUrn: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtStages + "Prop_CeremonialUrn.png");
                case BreakablePropKind.BriarBramble: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_BreakableBriarBramble.png");
                case BreakablePropKind.WoodenBarrel: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_BreakableBarrel.png");
                case BreakablePropKind.AncientUrn: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_BreakableUrn.png");
                default: return AssetDatabase.LoadAssetAtPath<Sprite>(ArtEnv + "Env_BreakableTallGrass.png");
            }
        }
    }
}
#endif
