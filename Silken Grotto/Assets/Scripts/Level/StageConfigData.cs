using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    public enum WeatherProfileType
    {
        MossGrottoSunSpores,
        WeavenestSilkDust,
        ShellwoodBiolumFog,
        KarakSandstorm,
        BellhartColdMist,
        MountFayBlizzard,
        CourtyardLightSnowWind,
        ChoralGoldenDust
    }

    public enum TransitionGateVisualType
    {
        MossyCaveMouth,
        CaveLightOpening,
        AncientStoneWallGate,
        RuinedSandWallGate,
        OldIronGate,
        RoyalGrandGate,
        GrandInteriorDoors
    }

    public enum StagePlatformKind
    {
        SolidCliff,
        OneWayPlatform,
        SlipperyIceCliff,
        SlipperyIceOneWay
    }

    public enum StageAmbientPropKind
    {
        WarmLantern,
        CrimsonBanner,
        SwayingWeb,
        CathedralGodRay,
        SwayingFlora,
        HangingBellCluster
    }

    [Serializable]
    public class StageDecorSpec
    {
        public string label;
        public Vector2 localPos;
        public string assetFileName;
        public string sortLayer = GameConstants.SortNearBackground;
        public int sortOrder = 5;
        public float scale = 1f;
        public bool flipX;
        public Color tint = Color.white;
        public bool swayWithWind;
    }

    [Serializable]
    public class StagePoolSpec
    {
        public string label;
        public Vector2 localBottomCenter;
        public float width = 5.4f;
        public Color waterTint = new Color(0.75f, 0.92f, 0.96f, 0.90f);
        public Color glintTint = new Color(0.88f, 0.98f, 1.0f, 0.54f);
    }

    [Serializable]
    public class StageFaunaSwarmSpec
    {
        public string label;
        public Vector2 localCenter;
        public Vector2 roamRadius = new Vector2(2.6f, 1.3f);
        public int count = 5;
        public Color colorA = new Color(0.88f, 0.96f, 1f, 0.85f);
        public Color colorB = new Color(0.72f, 0.90f, 1f, 0.72f);
    }

    [Serializable]
    public class StagePlatformSpec
    {
        public string label;
        public Vector2 localCenter;
        public Vector2 size;
        public StagePlatformKind kind;
        public Color tint = Color.white;
    }

    [Serializable]
    public class StageDestructibleSpec
    {
        public string label;
        public Vector2 localSurfacePos;
        public BreakablePropKind kind;
        public int maxHits = 1;
        public int orbDropCount = 1;
        public int essencePerOrb = 6;
        public float rewardSilk = 6f;
        public bool blocksMovement;
        public bool flipX;
        public float scale = 1f;
    }

    [Serializable]
    public class StageHazardSpec
    {
        public string label;
        public Vector2 localPos;
        public float width = 2.8f;
    }

    [Serializable]
    public class StageAmbientPropSpec
    {
        public string label;
        public Vector2 localPos;
        public StageAmbientPropKind kind;
        public float scale = 1f;
        public bool flipX;
    }

    /// <summary>
    /// Modular ScriptableObject definition encapsulating one complete progression stage:
    /// background art, floor/camera bounds, platforms, destructible obstacles, hazards,
    /// dynamic weather profile, and bidirectional transition gates.
    /// </summary>
    [CreateAssetMenu(fileName = "Stage_00_New", menuName = "Silken Grotto/Stage Config Data")]
    public class StageConfigData : ScriptableObject
    {
        [Header("Stage Identity")]
        public string stageId = "stage_00_moss_grotto";
        public int stageIndex;
        public string displayName = "Moss Grotto";
        public string subtitle = "Overgrown Sanctuary";

        [Header("Visuals & Atmosphere")]
        public Sprite backgroundSprite;
        public Sprite cliffSprite;
        public Sprite platformSprite;
        public Sprite supportArchSprite;
        public Sprite ceilingStalactiteSprite;
        public Sprite foregroundFrameSprite;
        public Color foregroundFrameTint = new Color(1f, 1f, 1f, 0.92f);
        public Sprite exitGateSprite;
        public Sprite entryGateSprite;
        public Color cameraClearColor = new Color(0.06f, 0.14f, 0.12f, 1f);
        public Color godRayTint = new Color(0.95f, 1f, 0.85f, 0.26f);
        public bool showGodRays = true;
        public WeatherProfileType weatherType = WeatherProfileType.MossGrottoSunSpores;
        public bool useDarkStoneAcoustics = true;
        public AudioClip stageBgm;

        [Header("Geometry & Camera Bounds")]
        public float originX;
        public float stageWidth = 56f;
        public float groundY;
        public bool slipperyGround;
        public Vector2 cameraMinBounds;
        public Vector2 cameraMaxBounds;
        public Vector2 leftSpawnOffset = new Vector2(4.2f, 0.02f);
        public Vector2 rightExitOffset = new Vector2(52.5f, 0f);

        [Header("Progression Links")]
        public TransitionGateVisualType exitGateVisual = TransitionGateVisualType.MossyCaveMouth;
        public string nextStageId;
        public TransitionGateVisualType entryGateVisual = TransitionGateVisualType.MossyCaveMouth;
        public string previousStageId;

        [Header("Modular Level Content")]
        public List<StagePlatformSpec> platforms = new List<StagePlatformSpec>();
        public List<StageDestructibleSpec> destructibles = new List<StageDestructibleSpec>();
        public List<StageHazardSpec> hazards = new List<StageHazardSpec>();
        public List<StageAmbientPropSpec> ambientProps = new List<StageAmbientPropSpec>();
        public List<StageDecorSpec> decors = new List<StageDecorSpec>();
        public List<StagePoolSpec> pools = new List<StagePoolSpec>();
        public List<StageFaunaSwarmSpec> faunaSwarms = new List<StageFaunaSwarmSpec>();
    }
}
