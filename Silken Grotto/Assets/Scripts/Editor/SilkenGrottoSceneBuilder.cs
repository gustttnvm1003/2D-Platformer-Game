#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SilkenGrotto.Editor
{
    /// <summary>
    /// Constructs MainMenu.unity, MossGrotto.unity (5-zone interconnected Metroidvania world),
    /// and GameplayHUDCanvas.prefab with clean serialized component bindings.
    /// </summary>
    public static class SilkenGrottoSceneBuilder
    {
        public static void BuildMainMenuScene(string scenePath, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera (Primary Moss Grotto deep cavern mist #10241E)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.backgroundColor = new Color(0.063f, 0.141f, 0.118f, 1f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // Multi-Layer Painterly Moss Grotto Menu Backdrop + Additive Volumetric God Rays
            GameObject menuBgRoot = new GameObject("Backdrop_MossGrotto_Layered");
            CreateMenuBackdropLayer(menuBgRoot.transform, "L0_Mist", "Assets/Art/Backgrounds/BG_L0_MossBackdrop.png", GameConstants.SortFarBackground, 0, 1.06f, Color.white, null, false);
            CreateMenuBackdropLayer(menuBgRoot.transform, "L1_DeepCavern", "Assets/Art/Backgrounds/BG_L1_DeepMossCavern.png", GameConstants.SortFarBackground, 2, 1.06f, new Color(0.95f, 0.98f, 0.96f, 0.95f), null, false);
            CreateMenuBackdropLayer(menuBgRoot.transform, "L2_MidRoots", "Assets/Art/Backgrounds/BG_L2_MidCavernRoots.png", GameConstants.SortMidBackground, 1, 1.06f, new Color(0.92f, 0.96f, 0.93f, 0.85f), null, false);
            CreateMenuBackdropLayer(menuBgRoot.transform, "L3_NearRuins", "Assets/Art/Backgrounds/BG_L3_NearMossRuins.png", GameConstants.SortNearBackground, 1, 1.06f, new Color(0.92f, 0.96f, 0.93f, 0.88f), null, false);
            CreateMenuBackdropLayer(menuBgRoot.transform, "L2_VolumetricShafts", "Assets/Art/Backgrounds/BG_L2_MossLightShafts.png", GameConstants.SortNearBackground, 3, 1.06f, new Color(0.95f, 1f, 0.88f, 0.92f), assets.VolumetricGodRayMaterial, true);
            CreateMenuBackdropLayer(menuBgRoot.transform, "L5_ForegroundSilhouette", "Assets/Art/Backgrounds/FG_L5_SilhouetteFraming.png", GameConstants.SortForeground, 5, 1.04f, Color.white, null, false);

            // Drifting glowing chartreuse spores (#C8F560 / #E9FFAD)
            GameObject spores = new GameObject("AmbientSpores");
            spores.AddComponent<ParticleSystem>();
            AmbientSporeField sf = spores.AddComponent<AmbientSporeField>();
            sf.ConfigureLayer(GameConstants.SortVFX, 2, new Vector3(22f, 12f, 1f), 16f, 0.08f, 0.22f, 120, 0.36f, 0.55f, 0f);
            SilkenGrottoProjectBuilder.SetField(sf, "particleMaterial", assets.SporeParticleMaterial);

            // Managers
            GameObject mgrs = new GameObject("Managers");
            GameManager gm = mgrs.AddComponent<GameManager>();
            SilkenGrottoProjectBuilder.SetBoolField(gm, "isMainMenuScene", true);
            AudioManager am = mgrs.AddComponent<AudioManager>();
            WireAudioManager(am);
            mgrs.AddComponent<SaveManager>();

            CreateEventSystem();

            // Main Menu Canvas
            GameObject canvasGo = CreateScaledCanvas("MainMenuCanvas");
            Font font = HUDController.LoadSafeFont();
            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_PanelFrame.png");

            // Left dark vignette card
            GameObject card = CreateUIImage("MenuCard", canvasGo.transform, panelSprite, new Color(0.06f, 0.11f, 0.12f, 0.90f));
            SetRect(card, new Vector2(0.08f, 0.14f), new Vector2(0.46f, 0.86f));

            Text title = CreateUIText("Title", card.transform, "SILKEN GROTTO", font, 52, TextAnchor.MiddleCenter, new Color(0.95f, 0.93f, 0.86f, 1f));
            SetRect(title.gameObject, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f));

            Text sub = CreateUIText("Subtitle", card.transform, "Echoes of the Ancient Moss Cavern", font, 20, TextAnchor.MiddleCenter, new Color(0.54f, 0.88f, 0.76f, 0.95f));
            SetRect(sub.gameObject, new Vector2(0.08f, 0.63f), new Vector2(0.92f, 0.73f));

            Button btnNew = CreateMenuButton("Btn_NewGame", card.transform, "NEW GAME", font, new Color(0.68f, 0.20f, 0.28f, 0.96f));
            SetRect(btnNew.gameObject, new Vector2(0.16f, 0.50f), new Vector2(0.84f, 0.585f));

            Button btnCont = CreateMenuButton("Btn_Continue", card.transform, "CONTINUE", font, new Color(0.16f, 0.34f, 0.32f, 0.95f));
            SetRect(btnCont.gameObject, new Vector2(0.16f, 0.39f), new Vector2(0.84f, 0.475f));

            Button btnControls = CreateMenuButton("Btn_Controls", card.transform, "CONTROLS", font, new Color(0.14f, 0.28f, 0.32f, 0.95f));
            SetRect(btnControls.gameObject, new Vector2(0.16f, 0.28f), new Vector2(0.84f, 0.365f));

            Button btnSettings = CreateMenuButton("Btn_Settings", card.transform, "SETTINGS", font, new Color(0.14f, 0.22f, 0.25f, 0.95f));
            SetRect(btnSettings.gameObject, new Vector2(0.16f, 0.17f), new Vector2(0.84f, 0.255f));

            Button btnQuit = CreateMenuButton("Btn_Quit", card.transform, "QUIT", font, new Color(0.14f, 0.16f, 0.18f, 0.95f));
            SetRect(btnQuit.gameObject, new Vector2(0.16f, 0.06f), new Vector2(0.84f, 0.145f));

            Text saveHint = CreateUIText("SaveStatusText", card.transform, "", font, 14, TextAnchor.MiddleCenter, new Color(0.75f, 0.84f, 0.80f, 0.9f));
            SetRect(saveHint.gameObject, new Vector2(0.06f, 0.005f), new Vector2(0.94f, 0.055f));

            // Dynamic Controls legend bottom-right (auto-refreshes when player rebinds keys!)
            GameObject legend = CreateUIImage("ControlsLegend", canvasGo.transform, panelSprite, new Color(0.05f, 0.09f, 0.10f, 0.84f));
            SetRect(legend, new Vector2(0.50f, 0.06f), new Vector2(0.95f, 0.36f));
            Text legTxt = CreateUIText("LegendText", legend.transform,
                ControlBindingsManager.BuildMainMenuLegend(),
                font, 14, TextAnchor.MiddleLeft, new Color(0.84f, 0.90f, 0.86f, 0.95f));
            legTxt.lineSpacing = 1.25f;
            SetRect(legTxt.gameObject, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.93f));

            ControlsMenuController controlsCtrl = BuildControlsPanel(canvasGo.transform, font, panelSprite);
            SettingsMenuController settingsCtrl = BuildSettingsPanel(canvasGo.transform, font, panelSprite);

            MainMenuController mmc = canvasGo.AddComponent<MainMenuController>();
            SilkenGrottoProjectBuilder.SetField(mmc, "newGameButton", btnNew);
            SilkenGrottoProjectBuilder.SetField(mmc, "continueButton", btnCont);
            SilkenGrottoProjectBuilder.SetField(mmc, "controlsButton", btnControls);
            SilkenGrottoProjectBuilder.SetField(mmc, "settingsButton", btnSettings);
            SilkenGrottoProjectBuilder.SetField(mmc, "quitButton", btnQuit);
            SilkenGrottoProjectBuilder.SetField(mmc, "controlsMenu", controlsCtrl);
            SilkenGrottoProjectBuilder.SetField(mmc, "settingsMenu", settingsCtrl);
            SilkenGrottoProjectBuilder.SetField(mmc, "saveStatusText", saveHint);
            SilkenGrottoProjectBuilder.SetField(mmc, "controlsLegendText", legTxt);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        public static void BuildGameplayScene(string scenePath, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with CameraFollow2D (starts in Moss Grotto deep cavern mist #10241E)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.backgroundColor = new Color(0.063f, 0.141f, 0.118f, 1f);
            camGo.transform.position = new Vector3(0f, 2f, -10f);
            camGo.AddComponent<AudioListener>();
            CameraFollow2D camFollow = camGo.AddComponent<CameraFollow2D>();

            // 2. 6-Layer Progressive DoF Multi-Biome Parallax Environment (Moss Grotto Prolog)
            BiomeAtmosphereController mossBiomeCtrl;
            GameObject mossParallaxRoot = BuildParallaxEnvironment(cam, assets, out mossBiomeCtrl);

            // 2b. Camera-Anchored Multi-Layer Dynamic Weather & Atmospheric Particle Engine
            DynamicWeatherController weatherCtrl = SilkenGrottoStagesBuilder.BuildWeatherEngineOnCamera(cam, assets);

            // 3. Core Systems
            GameObject systems = new GameObject("GameSystems");
            GameManager gm = systems.AddComponent<GameManager>();
            SilkenGrottoProjectBuilder.SetBoolField(gm, "isMainMenuScene", false);
            AudioManager am = systems.AddComponent<AudioManager>();
            WireAudioManager(am);
            VFXManager vfx = systems.AddComponent<VFXManager>();
            WireVFXManager(vfx, assets);
            HitStopController hitStop = systems.AddComponent<HitStopController>();
            SaveManager saveMgr = systems.AddComponent<SaveManager>();
            CaveTransitionManager caveTransition = systems.AddComponent<CaveTransitionManager>();

            CreateEventSystem();

            // 4. Player Instance (spawn cleanly grounded in Zone 1 Sunlit Moss Glade)
            GameObject playerGo = (GameObject)PrefabUtility.InstantiatePrefab(assets.PlayerPrefab);
            playerGo.transform.position = new Vector3(0f, 0.02f, 0f);
            PlayerController playerCtrl = playerGo.GetComponent<PlayerController>();
            SilkenGrottoProjectBuilder.SetField(gm, "player", playerCtrl);
            SilkenGrottoProjectBuilder.SetField(gm, "saveManager", saveMgr);
            SilkenGrottoProjectBuilder.SetField(camFollow, "target", playerGo.transform);
            SilkenGrottoProjectBuilder.SetField(camFollow, "playerMovement", playerGo.GetComponent<PlayerMovement>());
            SilkenGrottoProjectBuilder.SetField(playerGo.GetComponent<PlayerCombat>(), "hitStopController", hitStop);

            // 5. Interconnected 5-Zone Moss Grotto Prolog + All 7 Progression Stages (Weavenest Atla .. Aula Choral Chambers)
            BuildMetroidvaniaWorld(assets);
            SilkenGrottoStagesBuilder.BuildAllProgressionStages(cam, mossParallaxRoot, mossBiomeCtrl, weatherCtrl, systems.transform, assets);

            // 6. Gameplay HUD & Overlay Menus Prefab + Cave Transition UI Overlay binding
            GameObject hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(assets.HUDCanvasPrefab);
            if (hudInstance != null)
            {
                HUDController hudCtrl = hudInstance.GetComponent<HUDController>();
                Transform fadeTr = hudInstance.transform.Find("CaveFadeOverlay");
                Image fadeImg = fadeTr != null ? fadeTr.GetComponent<Image>() : null;
                SilkenGrottoProjectBuilder.SetField(caveTransition, "blackOverlayImage", fadeImg);
                SilkenGrottoProjectBuilder.SetField(caveTransition, "hudController", hudCtrl);
            }

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateMenuBackdropLayer(
            Transform parent, string name, string spritePath, string sortLayer, int sortOrder,
            float scale, Color tint, Material customMat, bool animateGodRays)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = tint;
            sr.sortingLayerName = sortLayer;
            sr.sortingOrder = sortOrder;
            if (customMat != null) sr.sharedMaterial = customMat;
            if (animateGodRays)
            {
                VolumetricGodRayAnimator anim = go.AddComponent<VolumetricGodRayAnimator>();
                anim.Configure(0.60f, 0.022f, 0.35f, 0.16f, 0f);
            }
        }

        private static GameObject BuildParallaxEnvironment(Camera cam, SilkenGrottoProjectBuilder.BuiltAssets assets, out BiomeAtmosphereController biomeCtrl)
        {
            Transform camTransform = cam.transform;
            GameObject bgRoot = new GameObject("Environment_Parallax");

            List<SpriteRenderer> mossRenderers = new List<SpriteRenderer>();
            List<SpriteRenderer> stoneRenderers = new List<SpriteRenderer>();
            List<SpriteRenderer> mossShaftRenderers = new List<SpriteRenderer>();
            List<SpriteRenderer> stoneShaftRenderers = new List<SpriteRenderer>();

            // Anchor Y centered on camera start height (y = 2.0f) with high vertical parallax
            // so 15.6u-tall 16:9 tiles frame seamlessly from Sunken Cavern (Y=-5) to Canopy Arena (Y=+18)!
            Vector3 anchor = new Vector3(camTransform.position.x, camTransform.position.y, 0f);

            // =========================================================================
            // 6-LAYER PROGRESSIVE DoF PARALLAX DEPTH STACK
            // Relative world scroll = (1 - factorX)
            // =========================================================================

            // Layer 0 (Far Backdrop — Scroll 0.10x => factorX 0.90f, 7px blur)
            mossRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L0_MossBackdrop", "Assets/Art/Backgrounds/BG_L0_MossBackdrop.png",
                GameConstants.SortFarBackground, 0, camTransform, 0.90f, 0.92f, 1.52f, anchor, Color.white));
            stoneRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L0_StoneCavernBackdrop", "Assets/Art/Backgrounds/BG_L0_StoneCavernBackdrop.png",
                GameConstants.SortFarBackground, 1, camTransform, 0.90f, 0.92f, 1.52f, anchor, Color.white));

            // Layer 1 (Deep Cavern — Scroll 0.25x => factorX 0.75f, 5px heavy DoF blur)
            mossRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L1_DeepMossCavern", "Assets/Art/Backgrounds/BG_L1_DeepMossCavern.png",
                GameConstants.SortFarBackground, 3, camTransform, 0.75f, 0.88f, 1.50f, anchor, new Color(0.95f, 0.98f, 0.96f, 0.96f)));
            stoneRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L1_DeepStoneCavern", "Assets/Art/Backgrounds/BG_L1_DeepStoneCavern.png",
                GameConstants.SortFarBackground, 4, camTransform, 0.75f, 0.88f, 1.50f, anchor, new Color(0.96f, 0.98f, 1f, 0.96f)));

            // Layer 2 (Mid-Far Cavern Roots & Additive Volumetric God Rays — Scroll 0.45x => factorX 0.55f, 2.6px DoF)
            mossRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L2_MidCavernRoots", "Assets/Art/Backgrounds/BG_L2_MidCavernRoots.png",
                GameConstants.SortMidBackground, 1, camTransform, 0.55f, 0.84f, 1.46f, anchor, new Color(0.90f, 0.95f, 0.92f, 0.86f), null, false));
            mossShaftRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L2_MossVolumetricShafts", "Assets/Art/Backgrounds/BG_L2_MossLightShafts.png",
                GameConstants.SortMidBackground, 4, camTransform, 0.55f, 0.86f, 1.48f, anchor, new Color(0.95f, 1f, 0.88f, 0.88f), assets.VolumetricGodRayMaterial, true));
            stoneShaftRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L2_StoneVolumetricShafts", "Assets/Art/Backgrounds/BG_L2_StoneLightShafts.png",
                GameConstants.SortMidBackground, 5, camTransform, 0.55f, 0.86f, 1.48f, anchor, new Color(0.88f, 0.95f, 0.98f, 0.72f), assets.VolumetricGodRayMaterial, true));

            // Layer 3 (Near-Mid Cavern Ruins & Silhouette Foliage — Scroll 0.70x => factorX 0.30f, 1.6px subtle DoF)
            mossRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L3_NearMossRuins", "Assets/Art/Backgrounds/BG_L3_NearMossRuins.png",
                GameConstants.SortNearBackground, 1, camTransform, 0.30f, 0.80f, 1.42f, anchor, new Color(0.92f, 0.96f, 0.94f, 0.92f), null, false));
            stoneRenderers.AddRange(SpawnParallaxTriple(
                bgRoot.transform, "L3_NearStoneRuins", "Assets/Art/Backgrounds/BG_L3_NearStoneRuins.png",
                GameConstants.SortNearBackground, 2, camTransform, 0.30f, 0.80f, 1.42f, anchor, new Color(0.92f, 0.95f, 0.97f, 0.92f), null, false));

            // Layer 5 (Foreground Pure Silhouette Black #040706 Framing — Scroll 1.20x => factorX -0.20f, 2.0px DoF)
            SpawnParallaxTriple(
                bgRoot.transform, "L5_ForegroundSilhouette", "Assets/Art/Backgrounds/FG_L5_SilhouetteFraming.png",
                GameConstants.SortForeground, 6, camTransform, -0.20f, 0.92f, 1.32f, anchor, new Color(1f, 1f, 1f, 0.94f), null, false);

            // Seamless Biome Atmosphere Crossfader (Primary Moss Grotto <-> Secondary Upper Dark Stone Cavern)
            biomeCtrl = bgRoot.AddComponent<BiomeAtmosphereController>();
            SilkenGrottoProjectBuilder.SetField(biomeCtrl, "targetCamera", cam);
            SilkenGrottoProjectBuilder.SetObjectArray(biomeCtrl, "mossGrottoRenderers", mossRenderers.ToArray());
            SilkenGrottoProjectBuilder.SetObjectArray(biomeCtrl, "stoneCavernRenderers", stoneRenderers.ToArray());
            SilkenGrottoProjectBuilder.SetObjectArray(biomeCtrl, "mossLightShaftRenderers", mossShaftRenderers.ToArray());
            SilkenGrottoProjectBuilder.SetObjectArray(biomeCtrl, "stoneLightShaftRenderers", stoneShaftRenderers.ToArray());

            // Calm, atmospheric Ambient Glowing Spores (#C8F560 / #E9FFAD with gentle sinusoidal sway across parallax layers)
            SpawnAmbientSporeLayer(bgRoot.transform, "AmbientSpores_FarMid", new Vector3(66f, 8f, 0f),
                GameConstants.SortMidBackground, 3, new Vector3(165f, 44f, 1f), 5.5f, 0.06f, 0.13f, 52, 0.30f, 0.48f, 0.0f, assets.SporeParticleMaterial);
            SpawnAmbientSporeLayer(bgRoot.transform, "AmbientSpores_Playable", new Vector3(66f, 8f, 0f),
                GameConstants.SortVFX, 2, new Vector3(165f, 44f, 1f), 6.5f, 0.09f, 0.20f, 62, 0.46f, 0.68f, 1.7f, assets.SporeParticleMaterial);
            SpawnAmbientSporeLayer(bgRoot.transform, "AmbientSpores_Foreground", new Vector3(66f, 8f, 0f),
                GameConstants.SortForeground, 4, new Vector3(165f, 44f, 1f), 2.8f, 0.14f, 0.26f, 26, 0.62f, 0.85f, 3.4f, assets.SporeParticleMaterial);

            return bgRoot;
        }

        private static void SpawnAmbientSporeLayer(
            Transform parent, string name, Vector3 pos, string sortLayer, int sortOrder,
            Vector3 boxSize, float rate, float minSz, float maxSz, int maxParticles,
            float swayAmp, float swayFreq, float phase, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<ParticleSystem>();
            AmbientSporeField sf = go.AddComponent<AmbientSporeField>();
            sf.ConfigureLayer(sortLayer, sortOrder, boxSize, rate, minSz, maxSz, maxParticles, swayAmp, swayFreq, phase);
            SilkenGrottoProjectBuilder.SetField(sf, "particleMaterial", mat);
        }

        private static SpriteRenderer[] SpawnParallaxTriple(
            Transform parent, string baseName, string spritePath, string sortLayer, int sortOrder,
            Transform camTransform, float factorX, float factorY, float scale, Vector3 center, Color tint,
            Material customMat = null, bool animateGodRays = false)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sp == null) return new SpriteRenderer[0];
            float tileW = sp.bounds.size.x * scale;
            SpriteRenderer[] result = new SpriteRenderer[3];

            for (int i = -1; i <= 1; i++)
            {
                GameObject go = new GameObject(baseName + "_" + (i + 1));
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(center.x + i * tileW, center.y, 0f);
                go.transform.localScale = Vector3.one * scale;

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.color = tint;
                sr.sortingLayerName = sortLayer;
                sr.sortingOrder = sortOrder;
                if (customMat != null) sr.sharedMaterial = customMat;
                result[i + 1] = sr;

                ParallaxLayer pl = go.AddComponent<ParallaxLayer>();
                pl.Configure(factorX, factorY, true, 3);
                SilkenGrottoProjectBuilder.SetField(pl, "cameraTransform", camTransform);

                if (animateGodRays)
                {
                    VolumetricGodRayAnimator rayAnim = go.AddComponent<VolumetricGodRayAnimator>();
                    rayAnim.Configure(0.62f, 0.024f, 0.36f, 0.22f, (i + 1) * 1.4f);
                }
            }
            return result;
        }

        private static void BuildMetroidvaniaWorld(SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            GameObject levelRoot = new GameObject("Level_MossGrotto");
            GameObject propsRoot = new GameObject("Environment_Props_Layer4");
            propsRoot.transform.SetParent(levelRoot.transform, false);

            // Upgraded Painterly Platform & Architectural Sprites (Layer 4 — 0px Razor-Sharp)
            Sprite mossGroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_MossGroundTile.png");
            Sprite carvedStoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_CarvedStonePlatform.png");
            Sprite stoneBridgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_StoneBridgeWalkway.png");
            Sprite rootArchSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_RootSupportArch.png");
            Sprite hangingMossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_HangingMossTendrils.png");
            Sprite waterPoolSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_WaterPoolLilyPads.png");
            Sprite waterGlintSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_WaterShimmerGlint.png");

            // Environmental Storytelling & Ruin Props
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_StonePillar.png");
            Sprite mushroomSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_GiantMushrooms.png");
            Sprite curledFernSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_CurledFerns.png");
            Sprite spikyGrassSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_PaleSpikyGrass.png");
            Sprite bronzeCageSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BronzeCageRuin.png");
            Sprite carvedArchSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_CarvedStoneArch.png");
            Sprite stalactiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_StalactiteVines.png");
            Sprite tallGrassSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BreakableTallGrass.png");
            Sprite barrelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BreakableBarrel.png");
            Sprite urnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BreakableUrn.png");
            Sprite briarBrambleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environments/Env_BreakableBriarBramble.png");

            Color mossBridgeTint = new Color(0.92f, 0.96f, 0.90f, 1f);
            Color stoneBridgeTint = new Color(0.86f, 0.90f, 0.92f, 1f);
            Color mossCeilingTint = new Color(0.56f, 0.64f, 0.60f, 1f);
            Color stoneCeilingTint = new Color(0.60f, 0.65f, 0.68f, 1f);

            // =========================================================================
            // CAVERN STONE CEILING ROOFS (Anchored across all 5 zones so stalactites & chains never float)
            // =========================================================================
            CreateCarvedStoneCeiling(levelRoot.transform, "Z1_Z2_CavernCeiling", new Vector2(24.8f, 19.5f), new Vector2(74.4f, 2.6f), carvedStoneSprite, mossCeilingTint);
            CreateCarvedStoneCeiling(levelRoot.transform, "Z3_SpireCeiling", new Vector2(73.2f, 24.0f), new Vector2(22.4f, 2.4f), carvedStoneSprite, stoneCeilingTint);
            CreateCarvedStoneCeiling(levelRoot.transform, "Z4_ThornbridgeCeiling", new Vector2(98.2f, 25.4f), new Vector2(27.6f, 2.4f), carvedStoneSprite, stoneCeilingTint);
            CreateCarvedStoneCeiling(levelRoot.transform, "Z5_SanctumCeiling", new Vector2(128.3f, 27.5f), new Vector2(32.6f, 2.6f), carvedStoneSprite, stoneCeilingTint);

            // =========================================================================
            // ZONE 1: SUNLIT MOSS GLADE (Primary Moss Grotto Biome, X: -10 .. 30, Y: 0 .. 14)
            // =========================================================================
            CreateMossPlatform(levelRoot.transform, "Z1_StartGround", new Vector2(6.0f, -1.1f), new Vector2(32.0f, 2.2f), mossGroundSprite, hangingMossSprite, true);
            CreateMossPlatform(levelRoot.transform, "Z1_LeftBoundaryWall", new Vector2(-11.2f, 9.0f), new Vector2(2.4f, 22.0f), mossGroundSprite, null, false);
            CreateSolidStoneBridge(levelRoot.transform, "Z1_BridgeA", new Vector2(12.0f, 2.6f), 5.5f, stoneBridgeSprite, mossBridgeTint);
            CreateMossPlatform(levelRoot.transform, "Z1_EastLedge", new Vector2(26.0f, 0.5f), new Vector2(8.0f, 2.2f), mossGroundSprite, hangingMossSprite, true);

            // Zone 1 Architectural Supports, Reflective Water Pool & Atmospheric Props
            CreateBridgeSupportArch(propsRoot.transform, "Z1_BridgeA_RootArch", new Vector2(12.0f, 2.60f), rootArchSprite, 1.35f, 1.18f);
            CreateReflectiveWaterPool(propsRoot.transform, "Z1_GladeWaterPool", new Vector2(12.0f, 0.0f), 5.8f, waterPoolSprite, waterGlintSprite);
            CreateCavernProp(propsRoot.transform, "Z1_CarvedArchWest", new Vector2(-7.2f, 0f), carvedArchSprite, 1.18f, false, GameConstants.SortNearBackground, 5, new Color(0.76f, 0.84f, 0.78f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z1_CarvedArchEast", new Vector2(23.8f, 1.6f), carvedArchSprite, 1.05f, true, GameConstants.SortNearBackground, 5, new Color(0.76f, 0.84f, 0.78f, 0.92f));
            CreateDecorativePillar(propsRoot.transform, new Vector2(-6.5f, 0f), pillarSprite, 1.1f);
            CreateDecorativePillar(propsRoot.transform, new Vector2(19.5f, 0f), pillarSprite, 0.95f);
            CreateCavernProp(propsRoot.transform, "Z1_MushroomsWest", new Vector2(-8.6f, 0f), mushroomSprite, 0.95f, false, GameConstants.SortNearBackground, 6, new Color(0.84f, 0.92f, 0.86f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_MushroomsEast", new Vector2(21.2f, 0f), mushroomSprite, 0.82f, true, GameConstants.SortNearBackground, 6, new Color(0.84f, 0.92f, 0.86f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_FernsA", new Vector2(-2.1f, 0f), curledFernSprite, 0.76f, false, GameConstants.SortNearBackground, 7, new Color(0.88f, 0.96f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_FernsB", new Vector2(7.6f, 0f), curledFernSprite, 0.72f, true, GameConstants.SortNearBackground, 7, new Color(0.88f, 0.96f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_FernsC", new Vector2(18.0f, 0f), curledFernSprite, 0.80f, false, GameConstants.SortNearBackground, 7, new Color(0.88f, 0.96f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_FernsEastLedge", new Vector2(28.5f, 1.6f), curledFernSprite, 0.70f, true, GameConstants.SortNearBackground, 7, new Color(0.88f, 0.96f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_BronzeCageFloor", new Vector2(1.7f, 0f), bronzeCageSprite, 0.68f, false, GameConstants.SortNearBackground, 6, new Color(0.84f, 0.88f, 0.82f, 0.94f));
            CreateHangingBronzeCage(propsRoot.transform, "Z1_BronzeCageHanging", new Vector2(24.8f, 7.6f), bronzeCageSprite, 0.66f, 10.8f);
            CreateCavernProp(propsRoot.transform, "Z1_CeilingStalactite1", new Vector2(2.5f, 18.34f), stalactiteSprite, 1.15f, false, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.78f, 0.72f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_CeilingStalactite2", new Vector2(15.5f, 18.34f), stalactiteSprite, 1.25f, true, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.78f, 0.72f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z1_CeilingStalactite3", new Vector2(30.5f, 18.34f), stalactiteSprite, 1.10f, false, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.78f, 0.72f, 0.95f));

            // Zone 1 Breakable Ground & Bridge Obstacles
            SpawnBreakableProp(levelRoot.transform, "Z1_Grass_WestA", new Vector2(-2.4f, 0.0f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Grass_EastA", new Vector2(2.2f, 0.0f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, true, false, 1.04f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Urn_Glade", new Vector2(6.4f, 0.0f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, false, true, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Barrel_Glade", new Vector2(7.3f, 0.0f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 4, 5f, false, true, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Urn_BridgeA", new Vector2(10.3f, 2.875f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, true, true, 0.96f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Grass_BridgeA", new Vector2(13.8f, 2.875f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.92f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Briar_GladeEast", new Vector2(20.4f, 0.0f), briarBrambleSprite, BreakablePropKind.BriarBramble, 2, 6, 8f, false, true, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Grass_EastLedge", new Vector2(23.1f, 1.6f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.98f);
            SpawnBreakableProp(levelRoot.transform, "Z1_Barrel_EastLedge", new Vector2(28.3f, 1.6f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 4, 5f, true, true, 0.98f);

            SpawnPrefabAt(assets.UpgradeAltarPrefab, levelRoot.transform, "UpgradeAltar_Glade", new Vector3(-4.5f, 0f, 0f));
            GameObject cp1 = SpawnPrefabAt(assets.CheckpointPrefab, levelRoot.transform, "Checkpoint_MossGlade", new Vector3(4.5f, 0f, 0f));
            cp1.GetComponent<CheckpointShrine>().Configure("cp_glade", "Sunlit Moss Glade Bench");

            SpawnEnemy(assets.LancerPrefab, levelRoot.transform, "Lancer_Z1_A", "lancer_z1_a", new Vector3(16.5f, 0.05f, 0f), assets.LancerStats);
            SpawnEnemy(assets.LancerPrefab, levelRoot.transform, "Lancer_Z1_B", "lancer_z1_b", new Vector3(26.0f, 1.65f, 0f), assets.LancerStats);
            SpawnEnemy(assets.MossgrubPrefab, levelRoot.transform, "Mossgrub_Z1_Glade", "mossgrub_z1_glade", new Vector3(9.5f, 0.05f, 0f), assets.MossgrubStats);
            SpawnEnemy(assets.MossmirPrefab, levelRoot.transform, "Mossmir_Z1_Glade", "mossmir_z1_glade", new Vector3(21.5f, 4.4f, 0f), assets.MossmirStats);

            SpawnOrbRow(assets.SilkOrbPrefab, levelRoot.transform, new Vector3(10.4f, 3.6f, 0f), 3, 1.6f, 15);
            SpawnInventoryPickup(assets, levelRoot.transform, "Pickup_Z1_Mossberry", "pickup_z1_mossberry", "item_mossberry_dew", 1, new Vector3(25.2f, 2.35f, 0f));

            // =========================================================================
            // ZONE 2: SUNKEN ROOT CAVERN (Primary Moss Grotto Deep Pools, X: 28 .. 62, Y: -8 .. 12)
            // =========================================================================
            CreateMossPlatform(levelRoot.transform, "Z2_StepDown", new Vector2(33.5f, -1.7f), new Vector2(7.0f, 1.4f), mossGroundSprite, hangingMossSprite, true);
            CreateMossPlatform(levelRoot.transform, "Z2_CavernFloor", new Vector2(49.0f, -6.2f), new Vector2(26.0f, 2.4f), mossGroundSprite, null, false);
            CreateMossPlatform(levelRoot.transform, "Z2_MidIsland", new Vector2(48.0f, -1.7f), new Vector2(6.5f, 1.4f), mossGroundSprite, hangingMossSprite, true);
            CreateSolidStoneBridge(levelRoot.transform, "Z2_BridgeLeft", new Vector2(40.8f, -2.3f), 5.2f, stoneBridgeSprite, mossBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Z2_UpperBridge", new Vector2(56.2f, 0.8f), 5.5f, stoneBridgeSprite, mossBridgeTint);

            // Zone 2 Water Pools, Root Bridge Arches & Subterranean Cavern Storytelling
            CreateBridgeSupportArch(propsRoot.transform, "Z2_BridgeLeft_RootArch", new Vector2(40.8f, -2.30f), rootArchSprite, 1.45f, 1.25f);
            CreateReflectiveWaterPool(propsRoot.transform, "Z2_PoolWest", new Vector2(38.4f, -5.0f), 5.4f, waterPoolSprite, waterGlintSprite);
            CreateReflectiveWaterPool(propsRoot.transform, "Z2_PoolCenter", new Vector2(46.2f, -5.0f), 5.8f, waterPoolSprite, waterGlintSprite);
            CreateReflectiveWaterPool(propsRoot.transform, "Z2_PoolEast", new Vector2(56.2f, -5.0f), 6.2f, waterPoolSprite, waterGlintSprite);
            CreateCavernProp(propsRoot.transform, "Z2_MushroomsWest", new Vector2(37.2f, -5.0f), mushroomSprite, 1.08f, false, GameConstants.SortNearBackground, 6, new Color(0.80f, 0.90f, 0.84f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z2_MushroomsEast", new Vector2(53.4f, -5.0f), mushroomSprite, 1.15f, true, GameConstants.SortNearBackground, 6, new Color(0.80f, 0.90f, 0.84f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z2_FernsStep", new Vector2(31.4f, -1.0f), curledFernSprite, 0.68f, false, GameConstants.SortNearBackground, 7, new Color(0.86f, 0.94f, 0.86f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z2_FernsIsland", new Vector2(49.8f, -1.0f), curledFernSprite, 0.65f, true, GameConstants.SortNearBackground, 7, new Color(0.86f, 0.94f, 0.86f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z2_SpikyGrassPitA", new Vector2(40.0f, -5.0f), spikyGrassSprite, 0.58f, false, GameConstants.SortNearBackground, 7, new Color(0.82f, 0.86f, 0.78f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z2_SpikyGrassPitB", new Vector2(48.6f, -5.0f), spikyGrassSprite, 0.62f, true, GameConstants.SortNearBackground, 7, new Color(0.82f, 0.86f, 0.78f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z2_BronzeCageIsland", new Vector2(45.8f, -1.0f), bronzeCageSprite, 0.64f, true, GameConstants.SortNearBackground, 6, new Color(0.82f, 0.86f, 0.80f, 0.94f));
            CreateCavernProp(propsRoot.transform, "Z2_CeilingStalactites1", new Vector2(44.0f, 18.34f), stalactiteSprite, 1.20f, false, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.78f, 0.72f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z2_CeilingStalactiteUpper", new Vector2(54.0f, 18.34f), stalactiteSprite, 1.15f, true, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.78f, 0.72f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z2_CeilingStalactites2", new Vector2(56.0f, 12.32f), stalactiteSprite, 1.10f, true, GameConstants.SortNearBackground, 1, new Color(0.70f, 0.80f, 0.74f, 0.94f));

            // Zone 2 Breakable Obstacles
            SpawnBreakableProp(levelRoot.transform, "Z2_Grass_StepDown", new Vector2(31.8f, -1.0f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.95f);
            SpawnBreakableProp(levelRoot.transform, "Z2_Urn_MidIsland", new Vector2(50.2f, -1.0f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, false, true, 0.98f);
            SpawnBreakableProp(levelRoot.transform, "Z2_Barrel_UpperBridge", new Vector2(57.8f, 1.075f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 4, 5f, false, true, 0.96f);
            SpawnBreakableProp(levelRoot.transform, "Z2_Briar_CavernEast", new Vector2(57.8f, -5.0f), briarBrambleSprite, BreakablePropKind.BriarBramble, 2, 6, 8f, true, true, 1.0f);

            // Spike briar hazards in lower pit
            SpawnPrefabAt(assets.SpikeHazardPrefab, levelRoot.transform, "Z2_SpikesA", new Vector3(42.0f, -5.0f, 0f));
            SpawnPrefabAt(assets.SpikeHazardPrefab, levelRoot.transform, "Z2_SpikesB", new Vector3(50.5f, -5.0f, 0f));

            // Secret Alcove tucked cleanly beneath Zone 1 (zero ground overlap!) behind Breakable Vine Wall #1
            CreateMossPlatform(levelRoot.transform, "Z2_SecretFloor", new Vector2(25.0f, -6.2f), new Vector2(22.0f, 2.4f), mossGroundSprite, null, false);
            CreateMossPlatform(levelRoot.transform, "Z2_SecretWallLeft", new Vector2(13.0f, -4.8f), new Vector2(2.0f, 5.2f), mossGroundSprite, null, false);
            CreateReflectiveWaterPool(propsRoot.transform, "Z2_SecretPool", new Vector2(25.8f, -4.98f), 5.2f, waterPoolSprite, waterGlintSprite);
            CreateCavernProp(propsRoot.transform, "Z2_SecretMushrooms", new Vector2(16.2f, -5.0f), mushroomSprite, 0.92f, false, GameConstants.SortNearBackground, 6, new Color(0.74f, 0.86f, 0.80f, 0.94f));
            CreateCavernProp(propsRoot.transform, "Z2_SecretBronzeCage", new Vector2(18.5f, -5.0f), bronzeCageSprite, 0.66f, false, GameConstants.SortNearBackground, 6, new Color(0.78f, 0.84f, 0.78f, 0.94f));
            CreateCavernProp(propsRoot.transform, "Z2_SecretStalactites", new Vector2(23.5f, -0.50f), stalactiteSprite, 0.88f, false, GameConstants.SortNearBackground, 1, new Color(0.68f, 0.78f, 0.72f, 0.94f));
            CreateCavernProp(propsRoot.transform, "Z2_SecretStalactiteWest", new Vector2(18.2f, -2.10f), stalactiteSprite, 0.60f, true, GameConstants.SortNearBackground, 1, new Color(0.68f, 0.78f, 0.72f, 0.94f));
            SpawnBreakableProp(levelRoot.transform, "Z2_Barrel_Secret", new Vector2(16.9f, -5.0f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 5, 6f, false, true, 0.98f);
            SpawnBreakableProp(levelRoot.transform, "Z2_Urn_Secret", new Vector2(17.8f, -5.0f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, true, true, 0.95f);
            SpawnBreakableProp(levelRoot.transform, "Z2_Grass_Secret", new Vector2(26.2f, -5.0f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.95f);
            GameObject bw1 = SpawnPrefabAt(assets.BreakableWallPrefab, levelRoot.transform, "BreakableWall_SecretGuard", new Vector3(31.5f, -3.7f, 0f));
            bw1.transform.localScale = new Vector3(1f, 0.80f, 1f);
            bw1.GetComponent<BreakableWall>().Configure("wall_secret_guard", 3, "Hidden Weaver Alcove Discovered!");
            SpawnAbilityPedestal(assets.AbilityPedestalPrefab, levelRoot.transform, "Pedestal_ThreadGuard", new Vector3(21.0f, -5.0f, 0f), assets.SkillByType[SkillType.ThreadGuard]);
            SpawnOrbRow(assets.SilkOrbPrefab, levelRoot.transform, new Vector3(24.5f, -3.9f, 0f), 3, 1.5f, 20);
            SpawnInventoryPickup(assets, levelRoot.transform, "Pickup_Z2_WovenSpool", "pickup_z2_spool", "item_woven_spool", 1, new Vector3(19.2f, -4.15f, 0f));

            // Main Zone 2 reward: Silk Dash Pedestal
            SpawnAbilityPedestal(assets.AbilityPedestalPrefab, levelRoot.transform, "Pedestal_SilkDash", new Vector3(59.5f, -5.0f, 0f), assets.SkillByType[SkillType.Dash]);
            SpawnEnemy(assets.LancerPrefab, levelRoot.transform, "Lancer_Z2_A", "lancer_z2_a", new Vector3(48.0f, -0.95f, 0f), assets.LancerStats);
            SpawnEnemy(assets.LancerPrefab, levelRoot.transform, "Lancer_Z2_B", "lancer_z2_b", new Vector3(55.5f, -4.95f, 0f), assets.LancerStats);
            SpawnEnemy(assets.MossgrubPrefab, levelRoot.transform, "Mossgrub_Z2_Step", "mossgrub_z2_step", new Vector3(33.5f, -0.95f, 0f), assets.MossgrubStats);
            SpawnEnemy(assets.MossmirPrefab, levelRoot.transform, "Mossmir_Z2_Chasm", "mossmir_z2_chasm", new Vector3(41.0f, 1.4f, 0f), assets.MossmirStats);
            SpawnAknid(assets.AknidPrefab, levelRoot.transform, "Aknid_Z2_Berry", "aknid_z2_berry", new Vector3(52.5f, 3.6f, 0f), assets.AknidStats, AknidCarryType.Berry);

            // =========================================================================
            // ZONE 3: OVERGROWN SPIRE (Biome Transition: Moss Grotto Base -> Upper Dark Carved-Stone Summit)
            // =========================================================================
            CreateMossPlatform(levelRoot.transform, "Z3_Base", new Vector2(72.0f, -6.2f), new Vector2(20.0f, 2.4f), mossGroundSprite, null, false);
            CreateMossPlatform(levelRoot.transform, "Z3_EastWall", new Vector2(83.2f, 2.6f), new Vector2(2.4f, 19.2f), carvedStoneSprite, null, false);
            CreateSolidStoneBridge(levelRoot.transform, "Z3_Climb1", new Vector2(67.5f, -2.2f), 5.5f, stoneBridgeSprite, mossBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Z3_Climb2", new Vector2(76.0f, 0.6f), 5.5f, stoneBridgeSprite, mossBridgeTint);
            CreateMossPlatform(levelRoot.transform, "Z3_CheckpointDeck", new Vector2(67.5f, 2.6f), new Vector2(11.0f, 1.8f), mossGroundSprite, hangingMossSprite, true);
            CreateSolidStoneBridge(levelRoot.transform, "Z3_Climb3", new Vector2(76.5f, 6.2f), 5.5f, stoneBridgeSprite, stoneBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Z3_Climb4", new Vector2(68.5f, 8.9f), 5.5f, stoneBridgeSprite, stoneBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Z3_Climb5", new Vector2(76.5f, 11.6f), 5.5f, stoneBridgeSprite, stoneBridgeTint);

            // Upper Spire transitions cleanly to Secondary Sub-Biome Floating Carved Stone Platforms!
            CreateCarvedStonePlatform(levelRoot.transform, "Z3_SummitDeck", new Vector2(72.0f, 13.2f), new Vector2(20.0f, 2.0f), carvedStoneSprite);

            // Zone 3 Lower Moss Grotto & Upper Dark Stone Props
            CreateBridgeSupportArch(propsRoot.transform, "Z3_Climb1_RootArch", new Vector2(67.5f, -2.20f), rootArchSprite, 1.48f, 1.32f);
            CreateReflectiveWaterPool(propsRoot.transform, "Z3_BaseWaterPoolWest", new Vector2(67.5f, -5.0f), 6.0f, waterPoolSprite, waterGlintSprite);
            CreateReflectiveWaterPool(propsRoot.transform, "Z3_BaseWaterPoolEast", new Vector2(76.8f, -5.0f), 5.4f, waterPoolSprite, waterGlintSprite);
            CreateCavernProp(propsRoot.transform, "Z3_BaseMushrooms", new Vector2(72.5f, -5.0f), mushroomSprite, 1.12f, false, GameConstants.SortNearBackground, 6, new Color(0.80f, 0.90f, 0.84f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z3_CheckpointFerns", new Vector2(70.6f, 3.5f), curledFernSprite, 0.72f, true, GameConstants.SortNearBackground, 7, new Color(0.86f, 0.94f, 0.86f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z3_SummitArch", new Vector2(65.8f, 14.2f), carvedArchSprite, 1.18f, false, GameConstants.SortNearBackground, 5, new Color(0.80f, 0.84f, 0.86f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z3_SummitSpikyGrass1", new Vector2(64.2f, 14.2f), spikyGrassSprite, 0.68f, false, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z3_SummitSpikyGrass2", new Vector2(74.8f, 14.2f), spikyGrassSprite, 0.72f, true, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateHangingBronzeCage(propsRoot.transform, "Z3_SummitHangingCage", new Vector2(74.2f, 20.2f), bronzeCageSprite, 0.64f, 3.0f);
            CreateCavernProp(propsRoot.transform, "Z3_SummitStalactites", new Vector2(70.0f, 22.92f), stalactiteSprite, 1.25f, false, GameConstants.SortEnvironment, 4, new Color(0.70f, 0.76f, 0.78f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z3_SummitStalactiteEast", new Vector2(78.4f, 22.92f), stalactiteSprite, 1.15f, true, GameConstants.SortEnvironment, 4, new Color(0.70f, 0.76f, 0.78f, 0.95f));

            GameObject cp2 = SpawnPrefabAt(assets.CheckpointPrefab, levelRoot.transform, "Checkpoint_Spire", new Vector3(65.5f, 3.5f, 0f));
            cp2.GetComponent<CheckpointShrine>().Configure("cp_spire", "Overgrown Spire Overlook");

            // Double Jump Pedestal on Spire Summit
            SpawnAbilityPedestal(assets.AbilityPedestalPrefab, levelRoot.transform, "Pedestal_DoubleJump", new Vector3(78.5f, 14.2f, 0f), assets.SkillByType[SkillType.DoubleJump]);

            // Secret Left Ledge flush with Summit behind Breakable Wall #2 -> Enhanced Attack Passive
            CreateCarvedStonePlatform(levelRoot.transform, "Z3_SecretEnhancedDeck", new Vector2(54.0f, 13.2f), new Vector2(16.0f, 2.0f), carvedStoneSprite);
            CreateCavernProp(propsRoot.transform, "Z3_SecretSpikyGrassA", new Vector2(53.0f, 14.2f), spikyGrassSprite, 0.65f, false, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z3_SecretSpikyGrassB", new Vector2(57.2f, 14.2f), spikyGrassSprite, 0.62f, true, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z3_SecretBronzeCage", new Vector2(47.4f, 14.2f), bronzeCageSprite, 0.68f, false, GameConstants.SortNearBackground, 6, new Color(0.84f, 0.86f, 0.82f, 0.94f));
            GameObject bw2 = SpawnPrefabAt(assets.BreakableWallPrefab, levelRoot.transform, "BreakableWall_NeedleMastery", new Vector3(60.5f, 16.2f, 0f));
            bw2.transform.localScale = new Vector3(1f, 1.25f, 1f);
            bw2.GetComponent<BreakableWall>().Configure("wall_needle_mastery", 3, "Shrine of Needle Mastery Revealed!");
            SpawnAbilityPedestal(assets.AbilityPedestalPrefab, levelRoot.transform, "Pedestal_EnhancedAttack", new Vector3(50.0f, 14.2f, 0f), assets.SkillByType[SkillType.EnhancedAttack]);

            // High Metroidvania Loop-Back Shortcut Bridges back over Zone 2 toward Zone 1
            CreateSolidStoneBridge(levelRoot.transform, "Shortcut_HighBridge1", new Vector2(40.5f, 11.8f), 6.0f, stoneBridgeSprite, stoneBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Shortcut_HighBridge2", new Vector2(31.5f, 8.8f), 6.0f, stoneBridgeSprite, mossBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Shortcut_HighBridge3", new Vector2(22.0f, 5.8f), 6.0f, stoneBridgeSprite, mossBridgeTint);
            SpawnOrbRow(assets.SilkOrbPrefab, levelRoot.transform, new Vector3(38.0f, 12.8f, 0f), 4, 1.7f, 20);
            SpawnInventoryPickup(assets, levelRoot.transform, "Pickup_Z3_EssenceShard", "pickup_z3_shard", "item_essence_shard", 1, new Vector3(31.5f, 9.75f, 0f));

            // Zone 3 & Shortcut Breakable Obstacles
            SpawnBreakableProp(levelRoot.transform, "Z3_Grass_Base", new Vector2(65.2f, -5.0f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.98f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Briar_BaseEast", new Vector2(79.4f, -5.0f), briarBrambleSprite, BreakablePropKind.BriarBramble, 2, 6, 8f, false, true, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Urn_Checkpoint", new Vector2(63.1f, 3.5f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, false, true, 0.96f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Grass_Climb3", new Vector2(77.9f, 6.475f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, true, false, 0.90f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Barrel_Climb4", new Vector2(67.1f, 9.175f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 4, 5f, false, true, 0.95f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Urn_Summit", new Vector2(64.9f, 14.2f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, true, true, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Grass_Summit", new Vector2(75.8f, 14.2f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.95f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Barrel_Shortcut1", new Vector2(39.0f, 12.075f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 4, 5f, false, true, 0.95f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Grass_Shortcut2", new Vector2(32.8f, 9.075f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, true, false, 0.92f);
            SpawnBreakableProp(levelRoot.transform, "Z3_Urn_Shortcut3", new Vector2(20.4f, 6.075f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, false, true, 0.95f);

            SpawnEnemy(assets.LancerPrefab, levelRoot.transform, "Lancer_Z3_Summit", "lancer_z3_summit", new Vector3(71.5f, 14.25f, 0f), assets.LancerStats);
            SpawnEnemy(assets.MossgrubPrefab, levelRoot.transform, "Mossgrub_Z3_Base", "mossgrub_z3_base", new Vector3(72.0f, -4.95f, 0f), assets.MossgrubStats);
            SpawnEnemy(assets.MossmirPrefab, levelRoot.transform, "Mossmir_Z3_Spire", "mossmir_z3_spire", new Vector3(71.5f, 5.8f, 0f), assets.MossmirStats);

            // =========================================================================
            // ZONE 4: THORNBRIDGE PASSAGE (Secondary Sub-Biome: Upper Dark Carved-Stone Cavern, X: 82 .. 112)
            // =========================================================================
            CreateCarvedStonePlatform(levelRoot.transform, "Z4_ChasmFloor", new Vector2(98.2f, 7.9f), new Vector2(27.6f, 2.2f), carvedStoneSprite);
            SpawnPrefabAt(assets.SpikeHazardPrefab, levelRoot.transform, "Z4_ChasmSpikes1", new Vector3(87.0f, 9.0f, 0f));
            SpawnPrefabAt(assets.SpikeHazardPrefab, levelRoot.transform, "Z4_ChasmSpikes2", new Vector3(91.5f, 9.0f, 0f));
            SpawnPrefabAt(assets.SpikeHazardPrefab, levelRoot.transform, "Z4_ChasmSpikes3", new Vector3(102.0f, 9.0f, 0f));
            SpawnPrefabAt(assets.SpikeHazardPrefab, levelRoot.transform, "Z4_ChasmSpikes4", new Vector3(106.5f, 9.0f, 0f));

            CreateSolidStoneBridge(levelRoot.transform, "Z4_WestBridge", new Vector2(86.2f, 14.0f), 4.6f, stoneBridgeSprite, stoneBridgeTint);
            CreateCarvedStonePlatform(levelRoot.transform, "Z4_MidPillarIsland", new Vector2(96.5f, 13.2f), new Vector2(6.5f, 2.0f), carvedStoneSprite);
            CreateCarvedStonePlatform(levelRoot.transform, "Z4_EastLanding", new Vector2(109.0f, 13.2f), new Vector2(6.0f, 2.0f), carvedStoneSprite);
            CreateSolidStoneBridge(levelRoot.transform, "Z4_HighAirDashLedge", new Vector2(101.5f, 17.8f), 5.5f, stoneBridgeSprite, stoneBridgeTint);

            // Zone 4 Upper Dark Carved-Stone Cavern Storytelling Props
            CreateCavernProp(propsRoot.transform, "Z4_MidIslandArch", new Vector2(96.5f, 14.2f), carvedArchSprite, 1.12f, false, GameConstants.SortNearBackground, 5, new Color(0.80f, 0.84f, 0.88f, 0.90f));
            CreateCavernProp(propsRoot.transform, "Z4_EastLandingArch", new Vector2(109.2f, 14.2f), carvedArchSprite, 1.08f, true, GameConstants.SortNearBackground, 5, new Color(0.80f, 0.84f, 0.88f, 0.90f));
            CreateCavernProp(propsRoot.transform, "Z4_SpikyGrassChasm1", new Vector2(84.8f, 9.0f), spikyGrassSprite, 0.74f, false, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z4_SpikyGrassChasm2", new Vector2(96.4f, 9.0f), spikyGrassSprite, 0.78f, true, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z4_SpikyGrassIslandL", new Vector2(94.4f, 14.2f), spikyGrassSprite, 0.64f, false, GameConstants.SortNearBackground, 7, new Color(0.94f, 0.95f, 0.90f, 0.96f));
            CreateCavernProp(propsRoot.transform, "Z4_SpikyGrassIslandR", new Vector2(98.6f, 14.2f), spikyGrassSprite, 0.62f, true, GameConstants.SortNearBackground, 7, new Color(0.94f, 0.95f, 0.90f, 0.96f));
            CreateCavernProp(propsRoot.transform, "Z4_SpikyGrassEast", new Vector2(110.6f, 14.2f), spikyGrassSprite, 0.68f, false, GameConstants.SortNearBackground, 7, new Color(0.94f, 0.95f, 0.90f, 0.96f));
            CreateHangingBronzeCage(propsRoot.transform, "Z4_HangingCageWest", new Vector2(91.4f, 19.6f), bronzeCageSprite, 0.66f, 4.9f);
            CreateHangingBronzeCage(propsRoot.transform, "Z4_HangingCageEast", new Vector2(105.6f, 20.4f), bronzeCageSprite, 0.62f, 4.6f);
            CreateCavernProp(propsRoot.transform, "Z4_CeilingStalactite1", new Vector2(89.0f, 24.32f), stalactiteSprite, 1.30f, false, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.74f, 0.78f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z4_CeilingStalactite2", new Vector2(103.5f, 24.32f), stalactiteSprite, 1.35f, true, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.74f, 0.78f, 0.95f));

            // Zone 4 Breakable Obstacles
            SpawnBreakableProp(levelRoot.transform, "Z4_Urn_WestBridge", new Vector2(85.2f, 14.275f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, false, true, 0.95f);
            SpawnBreakableProp(levelRoot.transform, "Z4_Grass_MidIsland", new Vector2(94.6f, 14.2f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.92f);
            SpawnBreakableProp(levelRoot.transform, "Z4_Briar_EastLanding", new Vector2(107.6f, 14.2f), briarBrambleSprite, BreakablePropKind.BriarBramble, 2, 6, 8f, false, true, 0.98f);
            SpawnBreakableProp(levelRoot.transform, "Z4_Urn_EastLanding", new Vector2(110.4f, 14.2f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, true, true, 0.96f);

            SpawnAbilityPedestal(assets.AbilityPedestalPrefab, levelRoot.transform, "Pedestal_SilkSpear", new Vector3(96.5f, 14.2f, 0f), assets.SkillByType[SkillType.SilkSpear]);
            SpawnAbilityPedestal(assets.AbilityPedestalPrefab, levelRoot.transform, "Pedestal_AirDash", new Vector3(101.5f, 18.08f, 0f), assets.SkillByType[SkillType.AirDash]);
            SpawnInventoryPickup(assets, levelRoot.transform, "Pickup_Z4_WeaverRelic", "pickup_z4_relic", "item_weaver_relic", 1, new Vector3(103.2f, 18.85f, 0f));
            SpawnAknid(assets.AknidPrefab, levelRoot.transform, "Aknid_Z4_Flea", "aknid_z4_flea", new Vector3(91.0f, 16.0f, 0f), assets.AknidStats, AknidCarryType.Flea);

            // =========================================================================
            // ZONE 5: ANCIENT CANOPY ARENA (Secondary Sub-Biome Boss Sanctum, X: 112 .. 144)
            // =========================================================================
            CreateCarvedStonePlatform(levelRoot.transform, "Z5_ArenaFloor", new Vector2(128.25f, 13.0f), new Vector2(32.5f, 2.4f), carvedStoneSprite);
            // Raise lower edge to Y=17.8 so the Mossy Cave Mouth exit portal at X=141.6 opens seamlessly to Stage 1 (Weavenest Atla)
            CreateCarvedStonePlatform(levelRoot.transform, "Z5_EastBoundaryWall", new Vector2(143.6f, 23.0f), new Vector2(2.4f, 10.4f), carvedStoneSprite);
            CreateSolidStoneBridge(levelRoot.transform, "Z5_ArenaDeckLeft", new Vector2(124.5f, 17.2f), 5.5f, stoneBridgeSprite, stoneBridgeTint);
            CreateSolidStoneBridge(levelRoot.transform, "Z5_ArenaDeckRight", new Vector2(135.5f, 17.2f), 5.5f, stoneBridgeSprite, stoneBridgeTint);

            // Zone 5 Grand Boss Sanctum Architecture & Atmospheric Props
            CreateCavernProp(propsRoot.transform, "Z5_SanctumArchLeft", new Vector2(121.0f, 14.2f), carvedArchSprite, 1.38f, false, GameConstants.SortNearBackground, 4, new Color(0.78f, 0.82f, 0.86f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z5_SanctumArchRight", new Vector2(138.0f, 14.2f), carvedArchSprite, 1.38f, true, GameConstants.SortNearBackground, 4, new Color(0.78f, 0.82f, 0.86f, 0.92f));
            CreateDecorativePillar(propsRoot.transform, new Vector2(116.0f, 14.2f), pillarSprite, 1.25f);
            CreateDecorativePillar(propsRoot.transform, new Vector2(139.0f, 14.2f), pillarSprite, 1.25f);
            CreateCavernProp(propsRoot.transform, "Z5_SpikyGrass1", new Vector2(114.6f, 14.2f), spikyGrassSprite, 0.72f, false, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z5_SpikyGrass2", new Vector2(122.8f, 14.2f), spikyGrassSprite, 0.76f, true, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z5_SpikyGrass3", new Vector2(133.4f, 14.2f), spikyGrassSprite, 0.72f, false, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z5_SpikyGrass4", new Vector2(140.4f, 14.2f), spikyGrassSprite, 0.78f, true, GameConstants.SortNearBackground, 7, new Color(0.92f, 0.94f, 0.88f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z5_ShadowMushroomsW", new Vector2(112.9f, 14.2f), mushroomSprite, 0.96f, false, GameConstants.SortNearBackground, 6, new Color(0.68f, 0.74f, 0.76f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z5_ShadowMushroomsE", new Vector2(141.0f, 14.2f), mushroomSprite, 1.02f, true, GameConstants.SortNearBackground, 6, new Color(0.68f, 0.74f, 0.76f, 0.92f));
            CreateCavernProp(propsRoot.transform, "Z5_RelicCageFloor", new Vector2(115.2f, 14.2f), bronzeCageSprite, 0.70f, false, GameConstants.SortNearBackground, 6, new Color(0.82f, 0.86f, 0.84f, 0.94f));
            CreateHangingBronzeCage(propsRoot.transform, "Z5_SanctumCageLeft", new Vector2(120.5f, 22.8f), bronzeCageSprite, 0.72f, 4.8f);
            CreateHangingBronzeCage(propsRoot.transform, "Z5_SanctumCageRight", new Vector2(136.5f, 22.8f), bronzeCageSprite, 0.72f, 4.8f);
            CreateCavernProp(propsRoot.transform, "Z5_VaultStalactite1", new Vector2(120.0f, 26.32f), stalactiteSprite, 1.38f, false, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.74f, 0.78f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z5_VaultStalactite2", new Vector2(130.0f, 26.32f), stalactiteSprite, 1.45f, true, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.74f, 0.78f, 0.95f));
            CreateCavernProp(propsRoot.transform, "Z5_VaultStalactite3", new Vector2(139.0f, 26.32f), stalactiteSprite, 1.32f, false, GameConstants.SortEnvironment, 4, new Color(0.68f, 0.74f, 0.78f, 0.95f));

            // Zone 5 Perimeter Breakable Relics (leaves boss duel lane unobstructed)
            SpawnBreakableProp(levelRoot.transform, "Z5_Urn_WestEntry", new Vector2(115.6f, 14.2f), urnSprite, BreakablePropKind.AncientUrn, 1, 6, 8f, false, true, 1.0f);
            SpawnBreakableProp(levelRoot.transform, "Z5_Barrel_WestEntry", new Vector2(116.5f, 14.2f), barrelSprite, BreakablePropKind.WoodenBarrel, 1, 5, 6f, false, true, 0.98f);
            SpawnBreakableProp(levelRoot.transform, "Z5_Urn_DeckLeft", new Vector2(122.9f, 17.475f), urnSprite, BreakablePropKind.AncientUrn, 1, 5, 6f, true, true, 0.94f);
            SpawnBreakableProp(levelRoot.transform, "Z5_Grass_DeckRight", new Vector2(137.1f, 17.475f), tallGrassSprite, BreakablePropKind.TallGrass, 1, 0, 0f, false, false, 0.92f);

            GameObject cp3 = SpawnPrefabAt(assets.CheckpointPrefab, levelRoot.transform, "Checkpoint_Canopy", new Vector3(118.2f, 14.2f, 0f));
            cp3.GetComponent<CheckpointShrine>().Configure("cp_canopy", "Ancient Canopy Sanctuary");
            SpawnPrefabAt(assets.UpgradeAltarPrefab, levelRoot.transform, "UpgradeAltar_Canopy", new Vector3(113.8f, 14.2f, 0f));

            // Elite Canopy Captain Boss!
            SpawnEnemy(assets.CaptainPrefab, levelRoot.transform, "Elite_CrimsonCaptain", "elite_canopy_captain", new Vector3(131.5f, 14.25f, 0f), assets.CaptainStats);
            SpawnOrbRow(assets.SilkOrbPrefab, levelRoot.transform, new Vector3(135.5f, 15.2f, 0f), 3, 1.4f, 30);

            // =========================================================================
            // CEILING WATER DRIP EMITTERS (Overhanging Stone Ceilings & Stalactites -> Splash Rings)
            // =========================================================================
            Sprite dripSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_WaterDrip.png");
            CreateCeilingDripZone(propsRoot.transform, "Drips_Z1_Glade", new Vector2(11.5f, 17.9f), 14f, 0.9f, 2.3f, 19f, dripSprite, assets.AdditiveVFXMaterial);
            CreateCeilingDripZone(propsRoot.transform, "Drips_Z2_SecretAlcove", new Vector2(24.5f, -0.8f), 12f, 0.8f, 2.0f, 5f, dripSprite, assets.AdditiveVFXMaterial);
            CreateCeilingDripZone(propsRoot.transform, "Drips_Z2_SunkenCavern", new Vector2(54.0f, 11.9f), 14f, 0.7f, 1.9f, 18f, dripSprite, assets.AdditiveVFXMaterial);
            CreateCeilingDripZone(propsRoot.transform, "Drips_Z3_SpireLower", new Vector2(72.0f, 2.0f), 14f, 0.85f, 2.2f, 9f, dripSprite, assets.AdditiveVFXMaterial);
            CreateCeilingDripZone(propsRoot.transform, "Drips_Z4_ThornbridgeVault", new Vector2(96.5f, 23.8f), 22f, 1.1f, 2.6f, 16f, dripSprite, assets.AdditiveVFXMaterial);
            CreateCeilingDripZone(propsRoot.transform, "Drips_Z5_CanopySanctum", new Vector2(128.0f, 25.8f), 20f, 1.2f, 2.8f, 14f, dripSprite, assets.AdditiveVFXMaterial);

            // =========================================================================
            // BIDIRECTIONAL METROIDVANIA AREA ZONE TRIGGERS (Zero Gap, Clean Seamless Follow)
            // =========================================================================
            CreateAreaZone(levelRoot.transform, "zone_glade", "Sunlit Moss Glade", "Sanctuary Clearing",
                new Vector2(8.0f, 10.0f), new Vector2(40.0f, 24.0f), new Vector2(-10.5f, -3.5f), new Vector2(42.0f, 22.0f));
            CreateAreaZone(levelRoot.transform, "zone_cavern", "Sunken Root Cavern", "Secret Alcove Beneath the Glade",
                new Vector2(20.0f, -6.0f), new Vector2(16.0f, 8.0f), new Vector2(11.0f, -9.5f), new Vector2(74.0f, 22.0f));
            CreateAreaZone(levelRoot.transform, "zone_cavern", "Sunken Root Cavern", "Forgotten Roots & Secret Weaves",
                new Vector2(45.0f, 6.0f), new Vector2(34.0f, 32.0f), new Vector2(11.0f, -9.5f), new Vector2(74.0f, 22.0f));
            CreateAreaZone(levelRoot.transform, "zone_spire", "Overgrown Spire", "Vertical Canopy Ascent",
                new Vector2(72.0f, 8.0f), new Vector2(20.0f, 34.0f), new Vector2(50.0f, -9.0f), new Vector2(94.0f, 25.0f));
            CreateAreaZone(levelRoot.transform, "zone_thornbridge", "Thornbridge Passage", "Chasm of Silk & Briars",
                new Vector2(97.0f, 16.0f), new Vector2(30.0f, 22.0f), new Vector2(70.0f, 5.0f), new Vector2(124.0f, 27.0f));
            CreateAreaZone(levelRoot.transform, "zone_arena", "Ancient Canopy Arena", "Domain of the Crimson Captain",
                new Vector2(128.5f, 19.0f), new Vector2(33.0f, 24.0f), new Vector2(100.0f, 8.0f), new Vector2(144.5f, 29.0f));

            // =========================================================================
            // CAVE ENTRANCE DOORWAY PORTALS (Paused until a new cave/area is confirmed)
            // =========================================================================
            // CreateBidirectionalCavePortalPair helpers remain ready for future zones/caverns.
        }

        private static void CreateBidirectionalCavePortalPair(
            Transform parent,
            string baseName,
            Sprite glowSprite,
            Material additiveMat,
            Vector3 aPos, Vector3 aReturnSpawnOffset, int aEnterDir,
            string aRoomId, string aTitle, string aSub, Vector2 aCamMin, Vector2 aCamMax, bool aDarkAcoustics,
            Vector3 bPos, Vector3 bReturnSpawnOffset, int bEnterDir,
            string bRoomId, string bTitle, string bSub, Vector2 bCamMin, Vector2 bCamMax, bool bDarkAcoustics)
        {
            CaveEntrancePortal portalA = CreatePortalEndpoint(parent, baseName + "_A", aPos, glowSprite, additiveMat, aEnterDir);
            CaveEntrancePortal portalB = CreatePortalEndpoint(parent, baseName + "_B", bPos, glowSprite, additiveMat, bEnterDir);

            portalA.Configure(
                portalB, aReturnSpawnOffset, aEnterDir, -aEnterDir,
                aRoomId, aTitle, aSub, aCamMin, aCamMax, aDarkAcoustics,
                portalA.GetComponentInChildren<SpriteRenderer>()
            );
            portalB.Configure(
                portalA, bReturnSpawnOffset, bEnterDir, -bEnterDir,
                bRoomId, bTitle, bSub, bCamMin, bCamMax, bDarkAcoustics,
                portalB.GetComponentInChildren<SpriteRenderer>()
            );
        }

        private static CaveEntrancePortal CreatePortalEndpoint(
            Transform parent, string name, Vector3 pos, Sprite glowSprite, Material additiveMat, int enterDir)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            BoxCollider2D trig = go.AddComponent<BoxCollider2D>();
            trig.isTrigger = true;
            trig.offset = new Vector2(0f, 1.85f);
            trig.size = new Vector2(1.25f, 3.7f);

            // Luminous radial doorway cue (#D9F2A8 -> #F2FFD4 at ~45% pulsing opacity bleeding inward from edge)
            GameObject glowGo = new GameObject("DoorwayRadialGlow");
            glowGo.transform.SetParent(go.transform, false);
            glowGo.transform.localPosition = new Vector3(enterDir >= 0 ? -0.35f : 0.35f, 1.95f, 0f);
            glowGo.transform.localScale = new Vector3(enterDir >= 0 ? 1.55f : -1.55f, 1.15f, 1f);
            SpriteRenderer sr = glowGo.AddComponent<SpriteRenderer>();
            sr.sprite = glowSprite;
            sr.color = new Color(0.92f, 0.99f, 0.78f, 0.45f);
            sr.sortingLayerName = GameConstants.SortVFX;
            sr.sortingOrder = 4;
            if (additiveMat != null) sr.sharedMaterial = additiveMat;

            return go.AddComponent<CaveEntrancePortal>();
        }

        private static void CreateMossPlatform(
            Transform parent, string name, Vector2 center, Vector2 size,
            Sprite sprite, Sprite hangingTendrilsSprite, bool addHangingTendrils)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;

            // Separate Collider2D strictly decoupled from background art!
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            if (sprite != null)
            {
                float unitW = Mathf.Max(2.2f, sprite.bounds.size.x);
                float unitH = Mathf.Max(1.0f, sprite.bounds.size.y);
                int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / unitW));
                float segW = size.x / cols;

                // Tall vertical boundary walls tile cleanly in Y too so moss blobs aren't stretched vertically
                int rows = size.y > 3.5f ? Mathf.Max(2, Mathf.CeilToInt(size.y / unitH)) : 1;
                float segH = size.y / rows;
                // Env_MossGroundTile has tall bulbous background moss bushes occupying the top 25.5% above the
                // flat walkable front moss deck. Align the flat walkable deck to (colliderTop + 0.085f) so
                // player boots, enemy feet, benches, and ground props plant firmly onto the mossy turf!
                const float flatTopRatio = 0.255f;

                for (int r = 0; r < rows; r++)
                {
                    bool isTopRow = (r == rows - 1);
                    float rowBot;
                    float rowTop;
                    if (rows == 1)
                    {
                        rowBot = -size.y * 0.5f;
                        float desiredWalkY = size.y * 0.5f + 0.085f;
                        float calibratedH = (desiredWalkY - rowBot) / (1f - flatTopRatio);
                        rowTop = rowBot + calibratedH;
                    }
                    else
                    {
                        rowBot = -size.y * 0.5f + segH * r - (r > 0 ? segH * 0.34f : 0f);
                        rowTop = -size.y * 0.5f + segH * (r + 1) + (isTopRow ? 0.35f : 0f);
                    }
                    float localY = (rowBot + rowTop) * 0.5f;
                    float tileH = rowTop - rowBot;
                    Color rowTint = isTopRow ? Color.white : new Color(0.62f, 0.68f, 0.66f, 1f);

                    for (int c = 0; c < cols; c++)
                    {
                        GameObject tile = new GameObject("Tile_" + r + "_" + c);
                        tile.transform.SetParent(go.transform, false);
                        float localX = -size.x * 0.5f + segW * (c + 0.5f);
                        float flipX = (c % 2 == 1 && cols > 1) ? -1f : 1f;
                        tile.transform.localPosition = new Vector3(localX, localY, 0f);
                        tile.transform.localScale = new Vector3(flipX * ((segW * 1.07f) / unitW), tileH / unitH, 1f);

                        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                        sr.sprite = sprite;
                        sr.color = rowTint;
                        sr.sortingLayerName = GameConstants.SortEnvironment;
                        sr.sortingOrder = isTopRow ? (rows + 2) : (r + 1);
                    }
                }

                // Irregular hanging moss & vine tendrils beneath ledge underside (strictly visual, zero collider change)
                if (addHangingTendrils && hangingTendrilsSprite != null && size.y <= 3.5f)
                {
                    float tendUnitW = Mathf.Max(1.8f, hangingTendrilsSprite.bounds.size.x);
                    int tCols = Mathf.Max(1, Mathf.CeilToInt(size.x / (tendUnitW * 1.15f)));
                    float tSegW = size.x / tCols;
                    for (int c = 0; c < tCols; c++)
                    {
                        GameObject tend = new GameObject("HangingMoss_" + c);
                        tend.transform.SetParent(go.transform, false);
                        float lx = -size.x * 0.5f + tSegW * (c + 0.5f);
                        tend.transform.localPosition = new Vector3(lx, -size.y * 0.5f + 0.08f, 0f);
                        float flipX = (c % 2 == 1) ? -1f : 1f;
                        float varScaleY = (c % 3 == 0) ? 0.92f : ((c % 3 == 1) ? 0.78f : 1.04f);
                        tend.transform.localScale = new Vector3(flipX * (tSegW * 0.98f / tendUnitW), varScaleY, 1f);

                        SpriteRenderer tsr = tend.AddComponent<SpriteRenderer>();
                        tsr.sprite = hangingTendrilsSprite;
                        tsr.color = new Color(0.92f, 0.96f, 0.90f, 0.96f);
                        tsr.sortingLayerName = GameConstants.SortEnvironment;
                        tsr.sortingOrder = 1;
                    }
                }
            }
        }

        private static void CreateCarvedStonePlatform(
            Transform parent, string name, Vector2 center, Vector2 size, Sprite stoneSprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;

            // Exact same Collider2D dimensions preserved!
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            if (stoneSprite != null)
            {
                float unitW = Mathf.Max(2.2f, stoneSprite.bounds.size.x);
                float unitH = Mathf.Max(0.9f, stoneSprite.bounds.size.y);
                int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / unitW));
                float segW = size.x / cols;

                int rows = size.y > 3.5f ? Mathf.Max(2, Mathf.CeilToInt(size.y / unitH)) : 1;
                float segH = size.y / rows;
                // Env_CarvedStonePlatform has small corner moss rising 12.5% above the flat carved stone top.
                // Align the flat chiseled stone surface to (colliderTop + 0.085f) for grounded foot contact.
                const float stoneFlatTopRatio = 0.125f;

                for (int r = 0; r < rows; r++)
                {
                    bool isTopRow = (r == rows - 1);
                    float rowBot;
                    float rowTop;
                    if (rows == 1)
                    {
                        rowBot = -size.y * 0.5f;
                        float desiredWalkY = size.y * 0.5f + 0.085f;
                        float calibratedH = (desiredWalkY - rowBot) / (1f - stoneFlatTopRatio);
                        rowTop = rowBot + calibratedH;
                    }
                    else
                    {
                        rowBot = -size.y * 0.5f + segH * r - (r > 0 ? segH * 0.28f : 0f);
                        rowTop = -size.y * 0.5f + segH * (r + 1) + (isTopRow ? 0.22f : 0f);
                    }
                    float localY = (rowBot + rowTop) * 0.5f;
                    float tileH = rowTop - rowBot;
                    Color rowTint = isTopRow
                        ? new Color(0.96f, 0.98f, 1f, 1f)
                        : new Color(0.60f, 0.64f, 0.66f, 1f);

                    for (int c = 0; c < cols; c++)
                    {
                        GameObject tile = new GameObject("StoneSlab_" + r + "_" + c);
                        tile.transform.SetParent(go.transform, false);
                        float localX = -size.x * 0.5f + segW * (c + 0.5f);
                        tile.transform.localPosition = new Vector3(localX, localY, 0f);
                        float flipX = (c % 2 == 1 && cols > 1) ? -1f : 1f;
                        tile.transform.localScale = new Vector3(flipX * ((segW * 1.07f) / unitW), tileH / unitH, 1f);

                        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                        sr.sprite = stoneSprite;
                        sr.color = rowTint;
                        sr.sortingLayerName = GameConstants.SortEnvironment;
                        sr.sortingOrder = isTopRow ? (rows + 2) : (r + 1);
                    }
                }
            }
        }

        private static void CreateCarvedStoneCeiling(
            Transform parent, string name, Vector2 center, Vector2 size, Sprite stoneSprite, Color tint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            if (stoneSprite != null)
            {
                float unitW = Mathf.Max(2.2f, stoneSprite.bounds.size.x);
                float unitH = Mathf.Max(0.9f, stoneSprite.bounds.size.y);
                int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / unitW));
                int rows = Mathf.Max(2, Mathf.CeilToInt(size.y / unitH));
                float segW = size.x / cols;
                float segH = size.y / rows;

                for (int r = 0; r < rows; r++)
                {
                    bool isBottomEdge = (r == 0);
                    float localY = -size.y * 0.5f + segH * (r + 0.5f);
                    Color rowTint = isBottomEdge
                        ? tint
                        : new Color(tint.r * 0.70f, tint.g * 0.70f, tint.b * 0.72f, 1f);

                    for (int c = 0; c < cols; c++)
                    {
                        GameObject tile = new GameObject("CeilingSlab_" + r + "_" + c);
                        tile.transform.SetParent(go.transform, false);
                        float localX = -size.x * 0.5f + segW * (c + 0.5f);
                        tile.transform.localPosition = new Vector3(localX, localY, 0f);
                        float flipX = (c % 2 == 1 && cols > 1) ? -1f : 1f;
                        // Invert Y on bottom edge so carved bevel rim faces down into the cavern cleanly
                        float flipY = isBottomEdge ? -1f : 1f;
                        tile.transform.localScale = new Vector3(flipX * ((segW * 1.07f) / unitW), flipY * ((segH * 1.18f) / unitH), 1f);

                        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                        sr.sprite = stoneSprite;
                        sr.color = rowTint;
                        sr.sortingLayerName = GameConstants.SortEnvironment;
                        sr.sortingOrder = isBottomEdge ? 6 : 5;
                    }
                }
            }
        }

        private static void SpawnBreakableProp(
            Transform parent,
            string name,
            Vector2 surfacePos,
            Sprite sprite,
            BreakablePropKind kind,
            int maxHits,
            int essenceReward,
            float silkReward,
            bool flipX,
            bool solidObstacle,
            float scale)
        {
            if (sprite == null) return;
            bool isBackgroundBarrelOrUrn = (kind == BreakablePropKind.WoodenBarrel || kind == BreakablePropKind.AncientUrn);
            bool blocksBody = solidObstacle && !isBackgroundBarrelOrUrn;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(surfacePos.x, surfacePos.y, 0f);
            go.tag = GameConstants.TagBreakable;
            go.layer = blocksBody ? GameConstants.LayerBreakableWall : GameConstants.LayerDestructibleFoliage;

            GameObject vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);

            SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = (blocksBody || isBackgroundBarrelOrUrn) ? GameConstants.SortInteractables : GameConstants.SortEnvironment;
            sr.sortingOrder = blocksBody ? 4 : (isBackgroundBarrelOrUrn ? 3 : 9);

            float colW;
            float colH;
            switch (kind)
            {
                case BreakablePropKind.WoodenBarrel:
                    colW = 0.72f * scale;
                    colH = 0.80f * scale;
                    break;
                case BreakablePropKind.AncientUrn:
                    colW = 0.60f * scale;
                    colH = 0.76f * scale;
                    break;
                case BreakablePropKind.BriarBramble:
                    colW = 0.96f * scale;
                    colH = 0.84f * scale;
                    break;
                default:
                    colW = 1.08f * scale;
                    colH = 0.96f * scale;
                    break;
            }

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = !blocksBody;
            col.offset = new Vector2(0f, colH * 0.5f);
            col.size = new Vector2(colW, colH);

            BreakableProp prop = go.AddComponent<BreakableProp>();
            prop.Configure(kind, maxHits, essenceReward, silkReward, blocksBody, sr);
        }

        private static void CreateSolidStoneBridge(
            Transform parent, string name, Vector2 center, float width, Sprite sprite, Color tint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            go.layer = GameConstants.LayerGround;

            // Fully solid BoxCollider2D (top, bottom, and vertical side faces) so floating platforms
            // feel tangible from every angle without pass-through and support Ledge Mantle cleanly.
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, 0.55f);

            if (sprite != null)
            {
                GameObject vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform, false);
                // Env_StoneBridgeWalkway has top-center pivot (0.5, 0.96). Raise +0.08f above collider top (+0.275f)
                // so heels plant squarely onto the 2.5D top bevel of the carved stone walkway!
                vis.transform.localPosition = new Vector3(0f, 0.355f, 0f);
                vis.transform.localScale = new Vector3(width / Mathf.Max(1f, sprite.bounds.size.x), 0.94f, 1f);
                SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
                sr.sortingLayerName = GameConstants.SortEnvironment;
                sr.sortingOrder = 3;
            }
        }

        private static void CreateBridgeSupportArch(
            Transform parent, string name, Vector2 topAnchorPos, Sprite archSprite, float scaleX, float scaleY)
        {
            if (archSprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(topAnchorPos.x, topAnchorPos.y, 0f);
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = archSprite;
            sr.color = new Color(0.86f, 0.90f, 0.86f, 0.96f);
            sr.sortingLayerName = GameConstants.SortNearBackground;
            sr.sortingOrder = 8;
        }

        private static void CreateReflectiveWaterPool(
            Transform parent, string name, Vector2 bottomCenterPos, float width,
            Sprite waterSprite, Sprite shimmerSprite)
        {
            if (waterSprite == null) return;
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(bottomCenterPos.x, bottomCenterPos.y, 0f);

            float nativeW = Mathf.Max(1f, waterSprite.bounds.size.x);
            float scaleX = width / nativeW;

            GameObject poolVis = new GameObject("WaterPoolSurface");
            poolVis.transform.SetParent(root.transform, false);
            // Nest water surface & lily pads right at walkable ground level so characters wade cleanly across it
            poolVis.transform.localPosition = new Vector3(0f, -0.44f, 0f);
            poolVis.transform.localScale = new Vector3(scaleX, 0.62f, 1f);
            SpriteRenderer psr = poolVis.AddComponent<SpriteRenderer>();
            psr.sprite = waterSprite;
            psr.color = new Color(0.92f, 0.98f, 0.95f, 0.92f);
            psr.sortingLayerName = GameConstants.SortEnvironment;
            psr.sortingOrder = 6;

            WaterSurfaceShimmer poolBob = poolVis.AddComponent<WaterSurfaceShimmer>();
            poolBob.Configure(1.35f, 0.04f, 0.008f, 0.012f, bottomCenterPos.x * 0.43f);

            if (shimmerSprite != null)
            {
                GameObject glint = new GameObject("WaterSurfaceGlint");
                glint.transform.SetParent(root.transform, false);
                glint.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                glint.transform.localScale = new Vector3(scaleX * 0.88f, 0.55f, 1f);
                SpriteRenderer gsr = glint.AddComponent<SpriteRenderer>();
                gsr.sprite = shimmerSprite;
                gsr.color = new Color(0.88f, 1f, 0.86f, 0.52f);
                gsr.sortingLayerName = GameConstants.SortEnvironment;
                gsr.sortingOrder = 7;

                WaterSurfaceShimmer shimmer = glint.AddComponent<WaterSurfaceShimmer>();
                shimmer.Configure(2.15f, 0.16f, 0.032f, 0.014f, bottomCenterPos.x * 0.71f + 1.2f);
            }
        }

        private static void CreateCavernProp(
            Transform parent, string name, Vector2 pos, Sprite sprite, float scale,
            bool flipX, string sortLayer, int sortOrder, Color tint)
        {
            if (sprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingLayerName = sortLayer;
            sr.sortingOrder = sortOrder;
        }

        private static void CreateHangingBronzeCage(
            Transform parent, string name, Vector2 bottomPos, Sprite cageSprite, float scale, float chainHeight)
        {
            if (cageSprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(bottomPos.x, bottomPos.y, 0f);

            GameObject cageVis = new GameObject("Cage");
            cageVis.transform.SetParent(go.transform, false);
            cageVis.transform.localScale = Vector3.one * scale;
            SpriteRenderer sr = cageVis.AddComponent<SpriteRenderer>();
            sr.sprite = cageSprite;
            sr.color = new Color(0.80f, 0.85f, 0.80f, 0.92f);
            sr.sortingLayerName = GameConstants.SortNearBackground;
            sr.sortingOrder = 6;

            // Subtle dark weathered hanging chain up toward cavern ceiling using thin LineRenderer
            GameObject chain = new GameObject("Chain");
            chain.transform.SetParent(go.transform, false);
            LineRenderer lr = chain.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            float cageTopY = cageSprite.bounds.size.y * scale * 0.94f;
            lr.SetPosition(0, new Vector3(0f, cageTopY, 0f));
            lr.SetPosition(1, new Vector3(0f, cageTopY + chainHeight, 0f));
            lr.startWidth = 0.055f;
            lr.endWidth = 0.045f;
            lr.sortingLayerName = GameConstants.SortNearBackground;
            lr.sortingOrder = 5;
            Material lineMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            if (lineMat != null) lr.sharedMaterial = lineMat;
            Color chainCol = new Color(0.10f, 0.14f, 0.13f, 0.88f);
            lr.startColor = chainCol;
            lr.endColor = chainCol;
        }

        private static void CreateCeilingDripZone(
            Transform parent, string name, Vector2 origin, float spanWidth,
            float minInterval, float maxInterval, float maxDropDist, Sprite dripSprite, Material addMat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            CeilingWaterDripEmitter emitter = go.AddComponent<CeilingWaterDripEmitter>();
            emitter.Configure(spanWidth, minInterval, maxInterval, maxDropDist);
            SilkenGrottoProjectBuilder.SetField(emitter, "waterDripSprite", dripSprite);
            SilkenGrottoProjectBuilder.SetField(emitter, "additiveMaterial", addMat);
        }

        private static void CreateDecorativePillar(Transform parent, Vector2 bottomPos, Sprite sprite, float scale)
        {
            if (sprite == null) return;
            GameObject go = new GameObject("RuinedStonePillar");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(bottomPos.x, bottomPos.y, 0f);
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.82f, 0.88f, 0.85f, 0.92f);
            sr.sortingLayerName = GameConstants.SortNearBackground;
            sr.sortingOrder = 4;
        }

        private static GameObject SpawnPrefabAt(GameObject prefab, Transform parent, string name, Vector3 pos)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = name;
            inst.transform.SetParent(parent, true);
            inst.transform.position = pos;
            return inst;
        }

        private static void SpawnAbilityPedestal(GameObject prefab, Transform parent, string name, Vector3 pos, SkillData skill)
        {
            GameObject inst = SpawnPrefabAt(prefab, parent, name, pos);
            AbilityPedestal ped = inst.GetComponent<AbilityPedestal>();
            if (ped != null)
            {
                SilkenGrottoProjectBuilder.SetField(ped, "skillToUnlock", skill);
            }
        }

        private static void SpawnEnemy(GameObject prefab, Transform parent, string name, string enemyId, Vector3 pos, EnemyStatsData stats)
        {
            GameObject inst = SpawnPrefabAt(prefab, parent, name, pos);
            EnemyController ec = inst.GetComponent<EnemyController>();
            if (ec != null)
            {
                SerializedObject so = new SerializedObject(ec);
                SerializedProperty idProp = so.FindProperty("enemyId");
                if (idProp != null) idProp.stringValue = enemyId;
                SerializedProperty stProp = so.FindProperty("statsData");
                if (stProp != null) stProp.objectReferenceValue = stats;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SpawnAknid(GameObject prefab, Transform parent, string name, string enemyId, Vector3 pos, EnemyStatsData stats, AknidCarryType carryPayload)
        {
            if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Shellwood/Aknid.prefab");
            if (stats == null) stats = AssetDatabase.LoadAssetAtPath<EnemyStatsData>("Assets/ScriptableObjects/Enemies/Enemy_Aknid.asset");
            if (prefab == null) return;

            GameObject inst = SpawnPrefabAt(prefab, parent, name, pos);
            EnemyController ec = inst.GetComponent<EnemyController>();
            if (ec != null)
            {
                SerializedObject so = new SerializedObject(ec);
                SerializedProperty idProp = so.FindProperty("enemyId");
                if (idProp != null) idProp.stringValue = enemyId;
                SerializedProperty stProp = so.FindProperty("statsData");
                if (stProp != null) stProp.objectReferenceValue = stats;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AknidAI ai = inst.GetComponent<AknidAI>();
            if (ai != null)
            {
                SerializedObject aiSo = new SerializedObject(ai);
                SerializedProperty carryProp = aiSo.FindProperty("carryType");
                if (carryProp != null) carryProp.intValue = (int)carryPayload;
                aiSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SpawnOrbRow(GameObject orbPrefab, Transform parent, Vector3 startPos, int count, float spacing, int valueEach)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject orb = SpawnPrefabAt(orbPrefab, parent, "SilkOrb_" + i, startPos + new Vector3(i * spacing, 0f, 0f));
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

        private static void SpawnInventoryPickup(
            SilkenGrottoProjectBuilder.BuiltAssets assets,
            Transform parent,
            string objName,
            string pickupId,
            string itemId,
            int count,
            Vector3 worldPos)
        {
            if (assets == null || assets.InventoryPickupPrefab == null) return;
            GameObject go = SpawnPrefabAt(assets.InventoryPickupPrefab, parent, objName, worldPos);
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

        private static void CreateAreaZone(Transform parent, string id, string title, string subtitle, Vector2 center, Vector2 size, Vector2 camMin, Vector2 camMax)
        {
            GameObject go = new GameObject("AreaZone_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            AreaZoneTrigger trig = go.AddComponent<AreaZoneTrigger>();
            trig.Configure(id, title, subtitle, camMin, camMax);
            EditorUtility.SetDirty(trig);
        }

        // =========================================================================
        // UI PREFAB & OVERLAY BUILDERS (1920x1080 Responsive CanvasScaler)
        // =========================================================================

        public static GameObject BuildHUDCanvasPrefab(string prefabPath)
        {
            GameObject canvasGo = CreateScaledCanvas("GameplayHUDCanvas");
            Font font = HUDController.LoadSafeFont();
            Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_PanelFrame.png");
            Sprite maskFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MaskHealthFull.png");
            Sprite maskEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_MaskHealthEmpty.png");
            Sprite crestFrame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_HealthCrestFrame.png");
            Sprite crestGlow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_HealthCrestGlow.png");
            Sprite silkSpoolBase = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_SilkSpoolBase.png");
            Sprite silkThreadFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_SilkThreadFill.png");

            // --- CAVE TRANSITION BLACK FADE OVERLAY (Beneath HUD_GameplayDimGroup so dimmed 35% HUD glows cleanly over darkness) ---
            GameObject caveFadeGo = CreateUIImage("CaveFadeOverlay", canvasGo.transform, null, new Color(0f, 0f, 0f, 0f));
            SetRect(caveFadeGo, Vector2.zero, Vector2.one);
            caveFadeGo.GetComponent<Image>().raycastTarget = false;

            // --- HUD GAMEPLAY DIM GROUP (Gently dims health masks, Silk bar, & skills to ~35% during room transitions) ---
            GameObject hudDimGo = new GameObject("HUD_GameplayDimGroup");
            hudDimGo.transform.SetParent(canvasGo.transform, false);
            SetRect(hudDimGo, Vector2.zero, Vector2.one);
            CanvasGroup hudDimCG = hudDimGo.AddComponent<CanvasGroup>();
            hudDimCG.alpha = 1f;

            // --- TOP-LEFT: Silksong-Style Crest Medallion, Needle Spine, Tight Teardrop Masks & Clean Silk Spool ---
            GameObject topLeft = new GameObject("HUD_TopLeft");
            topLeft.transform.SetParent(hudDimGo.transform, false);
            SetRect(topLeft, new Vector2(0.02f, 0.75f), new Vector2(0.42f, 0.97f));

            GameObject crestGlowGo = CreateUIImage("CrestBindGlow", topLeft.transform, crestGlow, new Color(0.95f, 1.0f, 0.92f, 0f));
            RectTransform crestGlowRt = crestGlowGo.GetComponent<RectTransform>();
            crestGlowRt.anchorMin = new Vector2(0.080f, 0.740f);
            crestGlowRt.anchorMax = new Vector2(0.080f, 0.740f);
            crestGlowRt.pivot = new Vector2(0.5f, 0.5f);
            crestGlowRt.sizeDelta = new Vector2(158f, 158f);
            Image crestGlowImg = crestGlowGo.GetComponent<Image>();
            crestGlowImg.raycastTarget = false;

            GameObject crestGo = CreateUIImage("CrestNeedleFrame", topLeft.transform, crestFrame, Color.white);
            SetRect(crestGo, new Vector2(0f, 0.48f), new Vector2(0.662f, 1.00f));
            crestGo.GetComponent<Image>().raycastTarget = false;

            GameObject maskRow = new GameObject("HealthMasks");
            maskRow.transform.SetParent(topLeft.transform, false);
            SetRect(maskRow, new Vector2(0.156f, 0.56f), new Vector2(0.98f, 0.92f));
            HorizontalLayoutGroup hlg = maskRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            GameObject spoolBaseGo = CreateUIImage("SilkSpoolBase", topLeft.transform, silkSpoolBase, Color.white);
            SetRect(spoolBaseGo, new Vector2(0.012f, 0.110f), new Vector2(0.347f, 0.420f));
            spoolBaseGo.GetComponent<Image>().raycastTarget = false;

            GameObject silkFillGo = CreateUIImage("SilkSpoolThreadFill", spoolBaseGo.transform, silkThreadFill, Color.white);
            SetRect(silkFillGo, new Vector2(0.109f, 0f), new Vector2(0.790f, 1f));
            Image silkFillImg = silkFillGo.GetComponent<Image>();
            silkFillImg.raycastTarget = false;
            silkFillImg.preserveAspect = false;
            silkFillImg.type = Image.Type.Filled;
            silkFillImg.fillMethod = Image.FillMethod.Horizontal;
            silkFillImg.fillOrigin = 0;

            // Active Combat Crest Badge right beside Silk Spool (Hunter / Reaper / Witch live switcher!)
            GameObject crestBadgeGo = CreateUIImage("ActiveCrestBadge", topLeft.transform, panelSprite, new Color(0.08f, 0.14f, 0.16f, 0.86f));
            SetRect(crestBadgeGo, new Vector2(0.368f, 0.145f), new Vector2(0.840f, 0.385f));
            Image crestBadgeBg = crestBadgeGo.GetComponent<Image>();
            crestBadgeBg.raycastTarget = false;
            Text crestBadgeTxt = CreateUIText("CrestBadgeText", crestBadgeGo.transform, "CREST: HUNTER [1]", font, 14, TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.80f, 1f));
            crestBadgeTxt.raycastTarget = false;
            SetRect(crestBadgeTxt.gameObject, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));

            // --- BOTTOM-LEFT: Skills Indicator Bar ---
            GameObject skillBar = new GameObject("HUD_SkillsBar");
            skillBar.transform.SetParent(hudDimGo.transform, false);
            SetRect(skillBar, new Vector2(0.02f, 0.025f), new Vector2(0.46f, 0.13f));
            HorizontalLayoutGroup slg = skillBar.AddComponent<HorizontalLayoutGroup>();
            slg.spacing = 8f;
            slg.childAlignment = TextAnchor.LowerLeft;
            slg.childControlWidth = false;
            slg.childControlHeight = false;

            // --- BOTTOM-CENTER: Interact Prompt ---
            GameObject interactRoot = CreateUIImage("InteractPrompt", canvasGo.transform, panelSprite, new Color(0.06f, 0.12f, 0.14f, 0.88f));
            SetRect(interactRoot, new Vector2(0.32f, 0.11f), new Vector2(0.68f, 0.17f));
            Text interactTxt = CreateUIText("PromptText", interactRoot.transform, "[E] Interact", font, 19, TextAnchor.MiddleCenter, new Color(0.95f, 0.94f, 0.86f, 1f));
            SetRect(interactTxt.gameObject, Vector2.zero, Vector2.one);

            // --- TOP-CENTER: Area Banner & Checkpoint/Unlock Notification ---
            GameObject areaRoot = CreateUIImage("AreaBanner", canvasGo.transform, null, new Color(0.04f, 0.08f, 0.10f, 0.65f));
            SetRect(areaRoot, new Vector2(0.28f, 0.82f), new Vector2(0.72f, 0.94f));
            Text areaTitle = CreateUIText("AreaTitle", areaRoot.transform, "SUNLIT MOSS GLADE", font, 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.93f, 0.85f, 1f));
            SetRect(areaTitle.gameObject, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.94f));
            Text areaSub = CreateUIText("AreaSub", areaRoot.transform, "Ancient Sanctuary", font, 17, TextAnchor.MiddleCenter, new Color(0.58f, 0.90f, 0.78f, 0.95f));
            SetRect(areaSub.gameObject, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.42f));

            GameObject notifyRoot = CreateUIImage("NotificationBanner", canvasGo.transform, panelSprite, new Color(0.08f, 0.14f, 0.16f, 0.92f));
            SetRect(notifyRoot, new Vector2(0.24f, 0.68f), new Vector2(0.76f, 0.80f));
            Text notifyTitle = CreateUIText("NotifyTitle", notifyRoot.transform, "CHECKPOINT REACHED", font, 24, TextAnchor.MiddleCenter, new Color(0.96f, 0.78f, 0.45f, 1f));
            SetRect(notifyTitle.gameObject, new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.94f));
            Text notifySub = CreateUIText("NotifySub", notifyRoot.transform, "", font, 16, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 0.92f, 0.95f));
            SetRect(notifySub.gameObject, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.48f));

            HUDController hud = canvasGo.AddComponent<HUDController>();
            SilkenGrottoProjectBuilder.SetField(hud, "hudDimGroup", hudDimCG);
            SilkenGrottoProjectBuilder.SetField(hud, "healthMaskContainer", maskRow.transform);
            SilkenGrottoProjectBuilder.SetField(hud, "maskFullSprite", maskFull);
            SilkenGrottoProjectBuilder.SetField(hud, "maskEmptySprite", maskEmpty);
            SilkenGrottoProjectBuilder.SetField(hud, "crestReadyGlowImage", crestGlowImg);
            SilkenGrottoProjectBuilder.SetField(hud, "silkFillImage", silkFillImg);
            SilkenGrottoProjectBuilder.SetField(hud, "crestBadgeBackground", crestBadgeBg);
            SilkenGrottoProjectBuilder.SetField(hud, "crestBadgeText", crestBadgeTxt);
            SilkenGrottoProjectBuilder.SetField(hud, "skillSlotContainer", skillBar.transform);
            SilkenGrottoProjectBuilder.SetField(hud, "interactPromptRoot", interactRoot);
            SilkenGrottoProjectBuilder.SetField(hud, "interactPromptText", interactTxt);
            SilkenGrottoProjectBuilder.SetField(hud, "notificationRoot", notifyRoot);
            SilkenGrottoProjectBuilder.SetField(hud, "notificationTitleText", notifyTitle);
            SilkenGrottoProjectBuilder.SetField(hud, "notificationSubText", notifySub);
            SilkenGrottoProjectBuilder.SetField(hud, "areaBannerRoot", areaRoot);
            SilkenGrottoProjectBuilder.SetField(hud, "areaBannerTitleText", areaTitle);
            SilkenGrottoProjectBuilder.SetField(hud, "areaBannerSubText", areaSub);

            // --- PAUSE MENU ---
            GameObject pauseRoot = CreateUIImage("PauseMenuOverlay", canvasGo.transform, null, new Color(0.02f, 0.05f, 0.06f, 0.72f));
            SetRect(pauseRoot, Vector2.zero, Vector2.one);
            pauseRoot.AddComponent<CanvasGroup>();
            GameObject pauseCard = CreateUIImage("PauseCard", pauseRoot.transform, panelSprite, new Color(0.07f, 0.12f, 0.14f, 0.96f));
            SetRect(pauseCard, new Vector2(0.34f, 0.08f), new Vector2(0.66f, 0.92f));
            Text pTitle = CreateUIText("PauseTitle", pauseCard.transform, "PAUSED", font, 36, TextAnchor.MiddleCenter, new Color(0.95f, 0.93f, 0.86f, 1f));
            SetRect(pTitle.gameObject, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f));

            Button pResume = CreateMenuButton("Btn_Resume", pauseCard.transform, "RESUME", font, new Color(0.66f, 0.22f, 0.30f, 0.95f));
            SetRect(pResume.gameObject, new Vector2(0.15f, 0.76f), new Vector2(0.85f, 0.84f));
            Button pInventory = CreateMenuButton("Btn_Inventory", pauseCard.transform, "INVENTORY (I)", font, new Color(0.18f, 0.36f, 0.30f, 0.95f));
            SetRect(pInventory.gameObject, new Vector2(0.15f, 0.65f), new Vector2(0.85f, 0.73f));
            Button pUpgrades = CreateMenuButton("Btn_Upgrades", pauseCard.transform, "UPGRADES & SKILLS", font, new Color(0.16f, 0.34f, 0.32f, 0.95f));
            SetRect(pUpgrades.gameObject, new Vector2(0.15f, 0.54f), new Vector2(0.85f, 0.62f));
            Button pControls = CreateMenuButton("Btn_Controls", pauseCard.transform, "CONTROLS", font, new Color(0.14f, 0.26f, 0.30f, 0.95f));
            SetRect(pControls.gameObject, new Vector2(0.15f, 0.43f), new Vector2(0.85f, 0.51f));
            Button pSettings = CreateMenuButton("Btn_Settings", pauseCard.transform, "SETTINGS", font, new Color(0.14f, 0.24f, 0.26f, 0.95f));
            SetRect(pSettings.gameObject, new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.40f));
            Button pSave = CreateMenuButton("Btn_Save", pauseCard.transform, "SAVE PROGRESS", font, new Color(0.14f, 0.28f, 0.24f, 0.95f));
            SetRect(pSave.gameObject, new Vector2(0.15f, 0.21f), new Vector2(0.85f, 0.29f));
            Button pMenu = CreateMenuButton("Btn_MainMenu", pauseCard.transform, "MAIN MENU", font, new Color(0.16f, 0.18f, 0.20f, 0.95f));
            SetRect(pMenu.gameObject, new Vector2(0.15f, 0.10f), new Vector2(0.85f, 0.18f));
            Text pFeedback = CreateUIText("SaveFeedback", pauseCard.transform, "", font, 15, TextAnchor.MiddleCenter, new Color(0.58f, 0.95f, 0.78f, 1f));
            SetRect(pFeedback.gameObject, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.08f));

            // --- UPGRADE & SKILL MENU ---
            GameObject upgRoot = CreateUIImage("UpgradeMenuOverlay", canvasGo.transform, null, new Color(0.02f, 0.05f, 0.06f, 0.78f));
            SetRect(upgRoot, Vector2.zero, Vector2.one);
            GameObject upgCard = CreateUIImage("UpgradeCard", upgRoot.transform, panelSprite, new Color(0.06f, 0.11f, 0.13f, 0.97f));
            SetRect(upgCard, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));

            Text upgTitle = CreateUIText("UpgradeTitle", upgCard.transform, "SANCTUARY ATTUNEMENT — UPGRADES & SKILLS", font, 30, TextAnchor.MiddleLeft, new Color(0.96f, 0.93f, 0.85f, 1f));
            SetRect(upgTitle.gameObject, new Vector2(0.04f, 0.87f), new Vector2(0.68f, 0.96f));
            Text upgEssence = CreateUIText("UpgradeEssence", upgCard.transform, "SILK ESSENCE: 0", font, 24, TextAnchor.MiddleRight, new Color(0.58f, 0.96f, 0.82f, 1f));
            SetRect(upgEssence.gameObject, new Vector2(0.68f, 0.87f), new Vector2(0.96f, 0.96f));

            GameObject upgList = new GameObject("UpgradeList");
            upgList.transform.SetParent(upgCard.transform, false);
            SetRect(upgList, new Vector2(0.04f, 0.14f), new Vector2(0.54f, 0.84f));
            VerticalLayoutGroup vlg = upgList.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            Text statsSummary = CreateUIText("StatsSummary", upgCard.transform, "ATTRIBUTES", font, 17, TextAnchor.UpperLeft, new Color(0.88f, 0.94f, 0.90f, 1f));
            SetRect(statsSummary.gameObject, new Vector2(0.57f, 0.52f), new Vector2(0.96f, 0.84f));

            Text skillSummary = CreateUIText("SkillSummary", upgCard.transform, "AWAKENED ABILITIES", font, 15, TextAnchor.UpperLeft, new Color(0.76f, 0.90f, 0.86f, 0.95f));
            SetRect(skillSummary.gameObject, new Vector2(0.57f, 0.14f), new Vector2(0.96f, 0.50f));

            Button upgClose = CreateMenuButton("Btn_CloseUpgrades", upgCard.transform, "RETURN TO GAME (TAB / ESC)", font, new Color(0.64f, 0.22f, 0.30f, 0.95f));
            SetRect(upgClose.gameObject, new Vector2(0.34f, 0.03f), new Vector2(0.66f, 0.11f));

            UpgradeMenuController upgCtrl = canvasGo.AddComponent<UpgradeMenuController>();
            SilkenGrottoProjectBuilder.SetField(upgCtrl, "panelRoot", upgRoot);
            SilkenGrottoProjectBuilder.SetField(upgCtrl, "essenceHeaderText", upgEssence);
            SilkenGrottoProjectBuilder.SetField(upgCtrl, "statsSummaryText", statsSummary);
            SilkenGrottoProjectBuilder.SetField(upgCtrl, "upgradeListContainer", upgList.transform);
            SilkenGrottoProjectBuilder.SetField(upgCtrl, "skillStatusSummaryText", skillSummary);
            SilkenGrottoProjectBuilder.SetField(upgCtrl, "closeButton", upgClose);

            // --- GAME OVER OVERLAY ---
            GameObject goRoot = CreateUIImage("GameOverOverlay", canvasGo.transform, null, new Color(0.06f, 0.01f, 0.02f, 0.82f));
            SetRect(goRoot, Vector2.zero, Vector2.one);
            GameObject goCard = CreateUIImage("GameOverCard", goRoot.transform, panelSprite, new Color(0.10f, 0.05f, 0.07f, 0.96f));
            SetRect(goCard, new Vector2(0.30f, 0.28f), new Vector2(0.70f, 0.72f));
            Text goTitle = CreateUIText("GameOverTitle", goCard.transform, "VEIL SHATTERED", font, 42, TextAnchor.MiddleCenter, new Color(0.94f, 0.32f, 0.38f, 1f));
            SetRect(goTitle.gameObject, new Vector2(0.06f, 0.64f), new Vector2(0.94f, 0.88f));
            Text goSub = CreateUIText("GameOverSub", goCard.transform, "Awaken once more at your last sanctuary bench.", font, 18, TextAnchor.MiddleCenter, new Color(0.86f, 0.84f, 0.82f, 0.92f));
            SetRect(goSub.gameObject, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.62f));

            Button goRespawn = CreateMenuButton("Btn_Respawn", goCard.transform, "RESPAWN AT CHECKPOINT", font, new Color(0.72f, 0.22f, 0.30f, 0.96f));
            SetRect(goRespawn.gameObject, new Vector2(0.16f, 0.28f), new Vector2(0.84f, 0.42f));
            Button goMenu = CreateMenuButton("Btn_GameOverMenu", goCard.transform, "MAIN MENU", font, new Color(0.18f, 0.20f, 0.22f, 0.95f));
            SetRect(goMenu.gameObject, new Vector2(0.16f, 0.10f), new Vector2(0.84f, 0.24f));

            GameOverController goCtrl = canvasGo.AddComponent<GameOverController>();
            SilkenGrottoProjectBuilder.SetField(goCtrl, "panelRoot", goRoot);
            SilkenGrottoProjectBuilder.SetField(goCtrl, "respawnButton", goRespawn);
            SilkenGrottoProjectBuilder.SetField(goCtrl, "mainMenuButton", goMenu);

            // --- INVENTORY, CONTROLS & SETTINGS OVERLAYS (Placed LAST so overlays ALWAYS render cleanly on top!) ---
            InventoryMenuController inventoryCtrl = BuildInventoryPanel(canvasGo.transform, font, panelSprite);
            ControlsMenuController controlsCtrl = BuildControlsPanel(canvasGo.transform, font, panelSprite);
            SettingsMenuController settingsCtrl = BuildSettingsPanel(canvasGo.transform, font, panelSprite);

            PauseMenuController pauseCtrl = canvasGo.AddComponent<PauseMenuController>();
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "panelRoot", pauseRoot);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "inventoryMenu", inventoryCtrl);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "settingsMenu", settingsCtrl);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "controlsMenu", controlsCtrl);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "resumeButton", pResume);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "inventoryButton", pInventory);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "upgradesButton", pUpgrades);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "controlsButton", pControls);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "settingsButton", pSettings);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "saveButton", pSave);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "mainMenuButton", pMenu);
            SilkenGrottoProjectBuilder.SetField(pauseCtrl, "statusFeedbackText", pFeedback);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, prefabPath);
            Object.DestroyImmediate(canvasGo);
            return prefab;
        }

        private static InventoryMenuController BuildInventoryPanel(Transform canvasParent, Font font, Sprite panelSprite)
        {
            Sprite essenceIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Item_EssenceVessel.png");

            GameObject root = CreateUIImage("InventoryOverlay", canvasParent, null, new Color(0.02f, 0.04f, 0.06f, 0.80f));
            SetRect(root, Vector2.zero, Vector2.one);
            root.AddComponent<CanvasGroup>();
            GameObject card = CreateUIImage("InventoryCard", root.transform, panelSprite, new Color(0.06f, 0.11f, 0.13f, 0.98f));
            SetRect(card, new Vector2(0.11f, 0.10f), new Vector2(0.89f, 0.90f));

            Text title = CreateUIText("InventoryTitle", card.transform, "TRAVELER'S POUCH — INVENTORY", font, 32, TextAnchor.MiddleLeft, new Color(0.95f, 0.93f, 0.86f, 1f));
            SetRect(title.gameObject, new Vector2(0.04f, 0.87f), new Vector2(0.54f, 0.96f));

            // Centralized Silk Essence Vessel card inside Inventory
            GameObject essenceBox = CreateUIImage("EssenceVesselBox", card.transform, null, new Color(0.04f, 0.09f, 0.10f, 0.88f));
            SetRect(essenceBox, new Vector2(0.56f, 0.85f), new Vector2(0.96f, 0.96f));
            if (essenceIcon != null)
            {
                GameObject eIcon = CreateUIImage("EssenceIcon", essenceBox.transform, essenceIcon, Color.white);
                SetRect(eIcon, new Vector2(0.03f, 0.12f), new Vector2(0.17f, 0.88f));
                eIcon.GetComponent<Image>().raycastTarget = false;
            }
            Text essenceTxt = CreateUIText("InventoryEssenceText", essenceBox.transform, "SILK ESSENCE: 0", font, 22, TextAnchor.MiddleLeft, new Color(0.60f, 0.96f, 0.82f, 1f));
            SetRect(essenceTxt.gameObject, new Vector2(0.20f, 0.34f), new Vector2(0.96f, 0.92f));
            Text essenceNote = CreateUIText("InventoryEssenceNote", essenceBox.transform, "Used at Sanctuary Altars & Upgrades [Tab / U]", font, 12, TextAnchor.MiddleLeft, new Color(0.76f, 0.88f, 0.84f, 0.85f));
            SetRect(essenceNote.gameObject, new Vector2(0.20f, 0.06f), new Vector2(0.96f, 0.40f));

            Text pouchCount = CreateUIText("PouchCountText", card.transform, "CARRIED ITEMS: 0", font, 15, TextAnchor.MiddleLeft, new Color(0.78f, 0.90f, 0.86f, 0.92f));
            SetRect(pouchCount.gameObject, new Vector2(0.04f, 0.80f), new Vector2(0.54f, 0.86f));

            // Left Items List Box
            GameObject listBox = CreateUIImage("ItemListBox", card.transform, null, new Color(0.04f, 0.08f, 0.09f, 0.72f));
            SetRect(listBox, new Vector2(0.04f, 0.14f), new Vector2(0.52f, 0.79f));

            GameObject listContent = new GameObject("ItemListContent");
            listContent.transform.SetParent(listBox.transform, false);
            SetRect(listContent, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            VerticalLayoutGroup vlg = listContent.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Text emptyHint = CreateUIText("EmptyHint", listBox.transform, "Pouch is empty.\nExplore Moss Grotto or smash urns to collect items.", font, 16, TextAnchor.MiddleCenter, new Color(0.72f, 0.80f, 0.78f, 0.85f));
            SetRect(emptyHint.gameObject, new Vector2(0.08f, 0.25f), new Vector2(0.92f, 0.75f));

            // Right Inspector Box
            GameObject detailBox = CreateUIImage("ItemInspectorBox", card.transform, null, new Color(0.04f, 0.08f, 0.09f, 0.78f));
            SetRect(detailBox, new Vector2(0.54f, 0.14f), new Vector2(0.96f, 0.83f));

            GameObject dIconGo = CreateUIImage("DetailIcon", detailBox.transform, essenceIcon, Color.white);
            SetRect(dIconGo, new Vector2(0.06f, 0.68f), new Vector2(0.32f, 0.93f));
            Image dIconImg = dIconGo.GetComponent<Image>();
            dIconImg.raycastTarget = false;

            Text dTitle = CreateUIText("DetailTitle", detailBox.transform, "NO ITEM SELECTED", font, 22, TextAnchor.MiddleLeft, new Color(0.96f, 0.92f, 0.82f, 1f));
            SetRect(dTitle.gameObject, new Vector2(0.35f, 0.76f), new Vector2(0.95f, 0.93f));
            Text dCat = CreateUIText("DetailCategory", detailBox.transform, "", font, 14, TextAnchor.MiddleLeft, new Color(0.62f, 0.90f, 0.82f, 0.92f));
            SetRect(dCat.gameObject, new Vector2(0.35f, 0.67f), new Vector2(0.95f, 0.77f));

            Text dDesc = CreateUIText("DetailDesc", detailBox.transform, "", font, 16, TextAnchor.UpperLeft, new Color(0.88f, 0.92f, 0.89f, 0.95f));
            dDesc.lineSpacing = 1.25f;
            SetRect(dDesc.gameObject, new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.64f));

            Text dEff = CreateUIText("DetailEffect", detailBox.transform, "", font, 15, TextAnchor.MiddleLeft, new Color(0.96f, 0.82f, 0.50f, 1f));
            SetRect(dEff.gameObject, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.37f));

            Text dFeedback = CreateUIText("DetailFeedback", detailBox.transform, "", font, 14, TextAnchor.MiddleCenter, new Color(0.62f, 0.96f, 0.80f, 1f));
            SetRect(dFeedback.gameObject, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.24f));

            Button useBtn = CreateMenuButton("Btn_UseInventoryItem", detailBox.transform, "USE ITEM", font, new Color(0.18f, 0.42f, 0.34f, 0.96f));
            SetRect(useBtn.gameObject, new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.14f));
            Text useBtnTxt = useBtn.GetComponentInChildren<Text>();

            Button closeBtn = CreateMenuButton("Btn_CloseInventory", card.transform, "CLOSE INVENTORY (I / ESC)", font, new Color(0.64f, 0.22f, 0.30f, 0.95f));
            SetRect(closeBtn.gameObject, new Vector2(0.32f, 0.03f), new Vector2(0.68f, 0.11f));

            InventoryMenuController ctrl = canvasParent.gameObject.AddComponent<InventoryMenuController>();
            SilkenGrottoProjectBuilder.SetField(ctrl, "panelRoot", root);
            SilkenGrottoProjectBuilder.SetField(ctrl, "essenceAmountText", essenceTxt);
            SilkenGrottoProjectBuilder.SetField(ctrl, "pouchCountText", pouchCount);
            SilkenGrottoProjectBuilder.SetField(ctrl, "itemListContainer", listContent.transform);
            SilkenGrottoProjectBuilder.SetField(ctrl, "emptyListHintText", emptyHint);
            SilkenGrottoProjectBuilder.SetField(ctrl, "detailIconImage", dIconImg);
            SilkenGrottoProjectBuilder.SetField(ctrl, "detailTitleText", dTitle);
            SilkenGrottoProjectBuilder.SetField(ctrl, "detailCategoryText", dCat);
            SilkenGrottoProjectBuilder.SetField(ctrl, "detailDescriptionText", dDesc);
            SilkenGrottoProjectBuilder.SetField(ctrl, "detailEffectText", dEff);
            SilkenGrottoProjectBuilder.SetField(ctrl, "feedbackText", dFeedback);
            SilkenGrottoProjectBuilder.SetField(ctrl, "useItemButton", useBtn);
            SilkenGrottoProjectBuilder.SetField(ctrl, "useButtonText", useBtnTxt);
            SilkenGrottoProjectBuilder.SetField(ctrl, "closeButton", closeBtn);
            return ctrl;
        }

        private static ControlsMenuController BuildControlsPanel(Transform canvasParent, Font font, Sprite panelSprite)
        {
            GameObject root = CreateUIImage("ControlsOverlay", canvasParent, null, new Color(0.02f, 0.04f, 0.06f, 0.86f));
            SetRect(root, Vector2.zero, Vector2.one);
            root.AddComponent<CanvasGroup>();
            GameObject card = CreateUIImage("ControlsCard", root.transform, panelSprite, new Color(0.07f, 0.12f, 0.14f, 0.98f));
            SetRect(card, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.95f));

            Text title = CreateUIText("ControlsTitle", card.transform, "CONTROLS & KEYBINDINGS", font, 32, TextAnchor.MiddleCenter, new Color(0.95f, 0.93f, 0.86f, 1f));
            SetRect(title.gameObject, new Vector2(0.06f, 0.90f), new Vector2(0.94f, 0.97f));
            Text sub = CreateUIText("ControlsSub", card.transform, "Click any binding button to customize Keyboard or Mouse controls", font, 15, TextAnchor.MiddleCenter, new Color(0.58f, 0.90f, 0.78f, 0.95f));
            SetRect(sub.gameObject, new Vector2(0.06f, 0.855f), new Vector2(0.94f, 0.905f));

            // Left column: Movement & Mobility (8 rebindable actions populated by ControlsMenuController)
            GameObject leftBox = CreateUIImage("Section_Movement", card.transform, null, new Color(0.04f, 0.08f, 0.10f, 0.72f));
            SetRect(leftBox, new Vector2(0.03f, 0.255f), new Vector2(0.49f, 0.845f));
            Text leftHdr = CreateUIText("HeaderLeft", leftBox.transform, "MOVEMENT & MOBILITY", font, 16, TextAnchor.MiddleLeft, new Color(0.96f, 0.78f, 0.45f, 1f));
            SetRect(leftHdr.gameObject, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f));
            GameObject leftList = new GameObject("LeftBindingsContainer");
            leftList.transform.SetParent(leftBox.transform, false);
            leftList.AddComponent<RectTransform>();
            SetRect(leftList, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.88f));

            // Right column: Combat, Silk Skills & Menus (8 rebindable actions populated by ControlsMenuController)
            GameObject rightBox = CreateUIImage("Section_Combat", card.transform, null, new Color(0.04f, 0.08f, 0.10f, 0.72f));
            SetRect(rightBox, new Vector2(0.51f, 0.255f), new Vector2(0.97f, 0.845f));
            Text rightHdr = CreateUIText("HeaderRight", rightBox.transform, "COMBAT, SILK SKILLS & MENUS", font, 16, TextAnchor.MiddleLeft, new Color(0.96f, 0.78f, 0.45f, 1f));
            SetRect(rightHdr.gameObject, new Vector2(0.04f, 0.89f), new Vector2(0.96f, 0.98f));
            GameObject rightList = new GameObject("RightBindingsContainer");
            rightList.transform.SetParent(rightBox.transform, false);
            rightList.AddComponent<RectTransform>();
            SetRect(rightList, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.88f));

            // Dynamic Moveset Combos & 6/6 Silk Skills reference bar (updates live as keys change!)
            GameObject comboBox = CreateUIImage("ComboReferenceBox", card.transform, null, new Color(0.04f, 0.09f, 0.11f, 0.80f));
            SetRect(comboBox, new Vector2(0.03f, 0.140f), new Vector2(0.97f, 0.242f));
            Text comboTips = CreateUIText("ComboTipsText", comboBox.transform,
                ControlBindingsManager.BuildDynamicComboTips(),
                font, 13, TextAnchor.MiddleCenter, new Color(0.86f, 0.94f, 0.90f, 0.96f));
            comboTips.lineSpacing = 1.25f;
            SetRect(comboTips.gameObject, new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.94f));

            // Live rebind listening / confirmation banner
            Text statusTxt = CreateUIText("StatusFeedbackText", card.transform,
                "Click any key button to rebind  |  Press ESC while listening to cancel",
                font, 14, TextAnchor.MiddleCenter, new Color(0.74f, 0.88f, 0.84f, 0.94f));
            SetRect(statusTxt.gameObject, new Vector2(0.04f, 0.095f), new Vector2(0.96f, 0.136f));

            Button resetBtn = CreateMenuButton("Btn_ResetControls", card.transform, "RESET DEFAULTS", font, new Color(0.16f, 0.30f, 0.34f, 0.95f));
            SetRect(resetBtn.gameObject, new Vector2(0.18f, 0.025f), new Vector2(0.47f, 0.088f));

            Button closeBtn = CreateMenuButton("Btn_CloseControls", card.transform, "BACK", font, new Color(0.66f, 0.22f, 0.30f, 0.95f));
            SetRect(closeBtn.gameObject, new Vector2(0.53f, 0.025f), new Vector2(0.82f, 0.088f));

            ControlsMenuController ctrl = canvasParent.gameObject.AddComponent<ControlsMenuController>();
            SilkenGrottoProjectBuilder.SetField(ctrl, "panelRoot", root);
            SilkenGrottoProjectBuilder.SetField(ctrl, "leftColumnContainer", leftList.transform);
            SilkenGrottoProjectBuilder.SetField(ctrl, "rightColumnContainer", rightList.transform);
            SilkenGrottoProjectBuilder.SetField(ctrl, "comboTipsText", comboTips);
            SilkenGrottoProjectBuilder.SetField(ctrl, "statusFeedbackText", statusTxt);
            SilkenGrottoProjectBuilder.SetField(ctrl, "resetDefaultsButton", resetBtn);
            SilkenGrottoProjectBuilder.SetField(ctrl, "closeButton", closeBtn);
            return ctrl;
        }

        private static SettingsMenuController BuildSettingsPanel(Transform canvasParent, Font font, Sprite panelSprite)
        {
            GameObject root = CreateUIImage("SettingsOverlay", canvasParent, null, new Color(0.02f, 0.04f, 0.06f, 0.82f));
            SetRect(root, Vector2.zero, Vector2.one);
            root.AddComponent<CanvasGroup>();
            GameObject card = CreateUIImage("SettingsCard", root.transform, panelSprite, new Color(0.07f, 0.12f, 0.14f, 0.98f));
            SetRect(card, new Vector2(0.28f, 0.16f), new Vector2(0.72f, 0.84f));

            Text title = CreateUIText("SettingsTitle", card.transform, "SETTINGS", font, 34, TextAnchor.MiddleCenter, new Color(0.95f, 0.93f, 0.86f, 1f));
            SetRect(title.gameObject, new Vector2(0.10f, 0.82f), new Vector2(0.90f, 0.94f));

            Slider master = CreateLabeledSlider(card.transform, "Master Volume", font, 0.70f);
            Slider music = CreateLabeledSlider(card.transform, "Music Volume", font, 0.58f);
            Slider sfx = CreateLabeledSlider(card.transform, "SFX Volume", font, 0.46f);

            Toggle fullToggle = CreateLabeledToggle(card.transform, "Fullscreen Display", font, 0.35f);
            Toggle shakeToggle = CreateLabeledToggle(card.transform, "Combat Screen Shake", font, 0.26f);

            Button resBtn = CreateMenuButton("Btn_Resolution", card.transform, "Resolution: 1920 x 1080", font, new Color(0.16f, 0.28f, 0.30f, 0.95f));
            SetRect(resBtn.gameObject, new Vector2(0.16f, 0.15f), new Vector2(0.84f, 0.23f));
            Text resText = resBtn.GetComponentInChildren<Text>();

            Button closeBtn = CreateMenuButton("Btn_CloseSettings", card.transform, "CLOSE", font, new Color(0.66f, 0.22f, 0.30f, 0.95f));
            SetRect(closeBtn.gameObject, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.12f));

            SettingsMenuController ctrl = canvasParent.gameObject.AddComponent<SettingsMenuController>();
            SilkenGrottoProjectBuilder.SetField(ctrl, "panelRoot", root);
            SilkenGrottoProjectBuilder.SetField(ctrl, "masterVolumeSlider", master);
            SilkenGrottoProjectBuilder.SetField(ctrl, "musicVolumeSlider", music);
            SilkenGrottoProjectBuilder.SetField(ctrl, "sfxVolumeSlider", sfx);
            SilkenGrottoProjectBuilder.SetField(ctrl, "fullscreenToggle", fullToggle);
            SilkenGrottoProjectBuilder.SetField(ctrl, "screenShakeToggle", shakeToggle);
            SilkenGrottoProjectBuilder.SetField(ctrl, "resolutionCycleButton", resBtn);
            SilkenGrottoProjectBuilder.SetField(ctrl, "resolutionButtonText", resText);
            SilkenGrottoProjectBuilder.SetField(ctrl, "closeButton", closeBtn);
            return ctrl;
        }

        private static void WireAudioManager(AudioManager am)
        {
            SilkenGrottoProjectBuilder.SetField(am, "bgmMossGrotto", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/BGM_MossGrotto.wav"));
            AudioClip[] stageBgms = new AudioClip[SilkenGrottoStagesBuilder.StageBgmFiles.Length];
            for (int i = 0; i < stageBgms.Length; i++)
            {
                stageBgms[i] = SilkenGrottoStagesBuilder.LoadStageBgm(i);
            }
            SilkenGrottoProjectBuilder.SetObjectArray(am, "stageBgmClips", stageBgms);
            SilkenGrottoProjectBuilder.SetField(am, "ambientCavernLoop", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/AMB_CavernDripsStream.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "ambientDarkStoneLoop", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/AMB_DarkStoneCavern.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxFootstep", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Footstep.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxFootstepMoss", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_FootstepMoss.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxFootstepStone", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_FootstepStone.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxJump", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Jump.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxDoubleJump", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_DoubleJump.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxLand", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Land.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxDash", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Dash.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxLedgeMantle", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_LedgeMantle.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxAttackSlash1", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_AttackSlash1.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxAttackSlash2", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_AttackSlash2.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxAttackSlash3", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_AttackSlash3.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxSilkSpear", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_SilkSpear.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxThreadGuard", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_ThreadGuard.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxBindCharge", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_BindCharge.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxBindBurst", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_BindBurst.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxEnemyHit", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_EnemyHit.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxArmorClink", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_ArmorClink.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxEnemyChirp", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_EnemyChirp.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxEnemyWindup", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_EnemyWindup.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxEnemyDeath", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_EnemyDeath.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxPlayerHit", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_PlayerHit.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxPlayerDeath", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_PlayerDeath.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxWaterDrip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_WaterDrip.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxBreakWood", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_BreakWood.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxBreakUrn", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_BreakUrn.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxBreakFoliage", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_BreakFoliage.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxBellChime", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_BellChime.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxIceShatter", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_IceShatter.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxCheckpoint", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Checkpoint.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxSkillUnlock", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_SkillUnlock.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxUpgrade", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Upgrade.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxUIClick", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_UIClick.wav"));
            SilkenGrottoProjectBuilder.SetField(am, "sfxUIHover", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_UIHover.wav"));
        }

        private static void WireVFXManager(VFXManager vfx, SilkenGrottoProjectBuilder.BuiltAssets assets)
        {
            PooledParticleSystem2D pool = vfx.gameObject.GetComponent<PooledParticleSystem2D>();
            if (pool == null) pool = vfx.gameObject.AddComponent<PooledParticleSystem2D>();
            SilkenGrottoProjectBuilder.SetField(pool, "defaultSpriteMaterial", assets.DefaultSpriteMaterial);
            SilkenGrottoProjectBuilder.SetField(pool, "additiveMaterial", assets.AdditiveVFXMaterial);

            SilkenGrottoProjectBuilder.SetField(vfx, "particlePool", pool);
            SilkenGrottoProjectBuilder.SetField(vfx, "defaultSpriteMaterial", assets.DefaultSpriteMaterial);
            SilkenGrottoProjectBuilder.SetField(vfx, "additiveVfxMaterial", assets.AdditiveVFXMaterial);

            SilkenGrottoProjectBuilder.SetField(vfx, "slashArcSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SlashArc.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "impactStarburstSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_ImpactStarburst.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "hitSparkSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_HitSpark.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "windStreakSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_WindStreak.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "threadGuardSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_ThreadGuard.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "healRibbonSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_HealRibbon.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "softParticleSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SoftParticle.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "mossLeafSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_MossLeaf.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "organicDropletSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_OrganicDroplet.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "shellShardSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_ShellShard.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "dustPuffSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_DustPuff.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "mossSpeckSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_MossSpeck.png"));
            SilkenGrottoProjectBuilder.SetField(vfx, "splashRingSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/VFX_SplashRing.png"));

            // Wire 2x8 Hand-Drawn Silksong Dash Flash Burst, Floor Dash Dust & Hero Landing Shockwave
            string vfxSheet = "Assets/Art/Effects/player_vfx_sheet.png";
            Sprite[] dashBurst = SilkenGrottoProjectBuilder.LoadSheetSprites(vfxSheet, "VFX_DashBurst", 8);
            Sprite[] groundImpact = SilkenGrottoProjectBuilder.LoadSheetSprites(vfxSheet, "VFX_GroundImpact", 8);
            if (dashBurst != null && dashBurst.Length == 8 && dashBurst[0] != null)
            {
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "dashBurstFrames", dashBurst);
            }
            if (groundImpact != null && groundImpact.Length == 8 && groundImpact[0] != null)
            {
                Sprite[] dashDust = new Sprite[4];
                Sprite[] heroWave = new Sprite[4];
                System.Array.Copy(groundImpact, 0, dashDust, 0, 4);
                System.Array.Copy(groundImpact, 4, heroWave, 0, 4);
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "floorDashDustFrames", dashDust);
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "heroLandingWaveFrames", heroWave);
            }

            // Wire 5x6 Shared Hand-Drawn Enemy VFX Sheet (Servitor Boran Laser, Mnemonid/Mnemonord Spikes, Driznit, Bellbearer, Winged Furm)
            string enemyVfxSheet = ENEMY_VFX_CONFIG.SheetPath;
            Sprite[] laserVfx = SilkenGrottoProjectBuilder.LoadSheetSprites(enemyVfxSheet, ENEMY_VFX_CONFIG.PrefixLaser, 6);
            Sprite[] spikesVfx = SilkenGrottoProjectBuilder.LoadSheetSprites(enemyVfxSheet, ENEMY_VFX_CONFIG.PrefixIceSpikes, 6);
            Sprite[] driznitVfx = SilkenGrottoProjectBuilder.LoadSheetSprites(enemyVfxSheet, ENEMY_VFX_CONFIG.PrefixDriznitProj, 6);
            Sprite[] bellVfx = SilkenGrottoProjectBuilder.LoadSheetSprites(enemyVfxSheet, ENEMY_VFX_CONFIG.PrefixBellProj, 6);
            Sprite[] dizzyVfx = SilkenGrottoProjectBuilder.LoadSheetSprites(enemyVfxSheet, ENEMY_VFX_CONFIG.PrefixHelmetDizzy, 6);
            if (laserVfx != null && laserVfx.Length == 6 && laserVfx[0] != null)
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "enemyLaserVfxFrames", laserVfx);
            if (spikesVfx != null && spikesVfx.Length == 6 && spikesVfx[0] != null)
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "enemyIceSpikesVfxFrames", spikesVfx);
            if (driznitVfx != null && driznitVfx.Length == 6 && driznitVfx[0] != null)
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "enemyDriznitVfxFrames", driznitVfx);
            if (bellVfx != null && bellVfx.Length == 6 && bellVfx[0] != null)
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "enemyBellVfxFrames", bellVfx);
            if (dizzyVfx != null && dizzyVfx.Length == 6 && dizzyVfx[0] != null)
                SilkenGrottoProjectBuilder.SetObjectArray(vfx, "enemyHelmetDizzyVfxFrames", dizzyVfx);
        }

        private static void CreateEventSystem()
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateScaledCanvas(string name)
        {
            GameObject go = new GameObject(name);
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreateUIImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return go;
        }

        private static Text CreateUIText(string name, Transform parent, string text, Font font, int size, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            return t;
        }

        private static Button CreateMenuButton(string name, Transform parent, string label, Font font, Color bgColor)
        {
            GameObject go = CreateUIImage(name, parent, null, bgColor);
            Button btn = go.AddComponent<Button>();
            go.AddComponent<UIButtonAudioFeedback>();
            Text t = CreateUIText("Label", go.transform, label, font, 20, TextAnchor.MiddleCenter, Color.white);
            SetRect(t.gameObject, Vector2.zero, Vector2.one);
            return btn;
        }

        private static Slider CreateLabeledSlider(Transform parent, string label, Font font, float yAnchor)
        {
            Text lbl = CreateUIText(label + "_Lbl", parent, label, font, 18, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 0.90f, 1f));
            SetRect(lbl.gameObject, new Vector2(0.10f, yAnchor), new Vector2(0.45f, yAnchor + 0.08f));

            GameObject sGo = new GameObject(label + "_Slider");
            sGo.transform.SetParent(parent, false);
            SetRect(sGo, new Vector2(0.46f, yAnchor + 0.015f), new Vector2(0.88f, yAnchor + 0.065f));
            Slider slider = sGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            GameObject bg = CreateUIImage("Background", sGo.transform, null, new Color(0.16f, 0.22f, 0.24f, 1f));
            SetRect(bg, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sGo.transform, false);
            SetRect(fillArea, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));
            GameObject fill = CreateUIImage("Fill", fillArea.transform, null, new Color(0.55f, 0.92f, 0.78f, 1f));
            SetRect(fill, Vector2.zero, Vector2.one);

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sGo.transform, false);
            SetRect(handleArea, Vector2.zero, Vector2.one);
            GameObject handle = CreateUIImage("Handle", handleArea.transform, null, Color.white);
            RectTransform hrt = handle.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(18f, 26f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = hrt;
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        private static Toggle CreateLabeledToggle(Transform parent, string label, Font font, float yAnchor)
        {
            GameObject tGo = new GameObject(label + "_Toggle");
            tGo.transform.SetParent(parent, false);
            SetRect(tGo, new Vector2(0.12f, yAnchor), new Vector2(0.88f, yAnchor + 0.07f));
            Toggle toggle = tGo.AddComponent<Toggle>();

            GameObject box = CreateUIImage("Box", tGo.transform, null, new Color(0.16f, 0.24f, 0.26f, 1f));
            SetRect(box, new Vector2(0f, 0.15f), new Vector2(0.08f, 0.85f));
            GameObject check = CreateUIImage("Checkmark", box.transform, null, new Color(0.55f, 0.95f, 0.80f, 1f));
            SetRect(check, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f));

            Text lbl = CreateUIText("Label", tGo.transform, label, font, 18, TextAnchor.MiddleLeft, new Color(0.90f, 0.94f, 0.92f, 1f));
            SetRect(lbl.gameObject, new Vector2(0.12f, 0f), new Vector2(1f, 1f));

            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }

        private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
