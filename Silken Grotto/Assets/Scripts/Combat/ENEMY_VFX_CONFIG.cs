using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Centralized metadata & playback tuning for the shared 2D hand-drawn Silksong-style
    /// enemy VFX sprite sheet (`Assets/Sprites/VFX/common_enemies_vfx_sheet.png`).
    /// Grid: 5 rows x 6 cols @ 192x192px/cell (1152x960px total), FilterMode.Bilinear smoothing.
    /// Playback runs at 18-24 FPS independent of 60 FPS combat physics!
    /// </summary>
    public static class ENEMY_VFX_CONFIG
    {
        public const string SheetPath = "Assets/Sprites/VFX/common_enemies_vfx_sheet.png";
        public const int CellSize = 192;
        public const int Rows = 5;
        public const int Cols = 6;
        public const int TotalWidth = 1152;
        public const int TotalHeight = 960;
        public const float PixelsPerUnit = 128.0f;
        public const float CellWorldSize = CellSize / PixelsPerUnit; // 1.50 world units per 192px cell
        public const bool ImageSmoothingEnabled = true;              // FilterMode.Bilinear

        // Sprite slice row prefixes configured by SilkenGrottoProjectBuilder
        public const string PrefixLaser = "EnemyVFX_Laser";
        public const string PrefixIceSpikes = "EnemyVFX_IceSpikes";
        public const string PrefixDriznitProj = "EnemyVFX_DriznitProj";
        public const string PrefixBellProj = "EnemyVFX_BellProj";
        public const string PrefixHelmetDizzy = "EnemyVFX_HelmetDizzy";

        public struct VfxClipMeta
        {
            public string Name;
            public int Row;
            public int StartCol;
            public int FrameCount;
            public float Fps;           // Strictly within 18-24 FPS (independent of 60 FPS physics)
            public bool Loop;
            public float DefaultScale;
            public int SortingOrder;
        }

        // Row 1: Servitor Boran — Laser Charge Flare (F1-2), Stretchable Laser Beam (F3-5), Beam Dissipate (F6)
        public static readonly VfxClipMeta ServitorLaserCharge = new VfxClipMeta
        {
            Name = "ServitorLaserCharge",
            Row = 0, StartCol = 0, FrameCount = 2,
            Fps = 20f, Loop = true, DefaultScale = 0.85f, SortingOrder = 18
        };

        public static readonly VfxClipMeta ServitorLaserBeam = new VfxClipMeta
        {
            Name = "ServitorLaserBeam",
            Row = 0, StartCol = 2, FrameCount = 3,
            Fps = 24f, Loop = true, DefaultScale = 0.72f, SortingOrder = 17
        };

        public static readonly VfxClipMeta ServitorLaserDissipate = new VfxClipMeta
        {
            Name = "ServitorLaserDissipate",
            Row = 0, StartCol = 5, FrameCount = 1,
            Fps = 18f, Loop = false, DefaultScale = 0.72f, SortingOrder = 17
        };

        // Row 2: Mnemonid (1.0x) & Mnemonord (1.62x) — Ice Spikes Emerging (F1-2), Active Ring (F3-4), Shatter (F5-6)
        public const float MnemonidSpikeScale = 1.00f;
        public const float MnemonordSpikeScale = 1.62f;

        public static readonly VfxClipMeta MnemonSpikesEmerging = new VfxClipMeta
        {
            Name = "MnemonSpikesEmerging",
            Row = 1, StartCol = 0, FrameCount = 2,
            Fps = 20f, Loop = false, DefaultScale = 1.00f, SortingOrder = 14
        };

        public static readonly VfxClipMeta MnemonSpikesActive = new VfxClipMeta
        {
            Name = "MnemonSpikesActive",
            Row = 1, StartCol = 2, FrameCount = 2,
            Fps = 20f, Loop = true, DefaultScale = 1.00f, SortingOrder = 14
        };

        public static readonly VfxClipMeta MnemonIceShatter = new VfxClipMeta
        {
            Name = "MnemonIceShatter",
            Row = 1, StartCol = 4, FrameCount = 2,
            Fps = 18f, Loop = false, DefaultScale = 1.05f, SortingOrder = 16
        };

        // Row 3: Driznit — Spinning Bouncing Projectile (F1-4) & Wall Ricochet Spark (F5-6)
        public static readonly VfxClipMeta DriznitProjectileSpin = new VfxClipMeta
        {
            Name = "DriznitProjectileSpin",
            Row = 2, StartCol = 0, FrameCount = 4,
            Fps = 24f, Loop = true, DefaultScale = 0.48f, SortingOrder = 17
        };

        public static readonly VfxClipMeta DriznitRicochetSpark = new VfxClipMeta
        {
            Name = "DriznitRicochetSpark",
            Row = 2, StartCol = 4, FrameCount = 2,
            Fps = 22f, Loop = false, DefaultScale = 0.68f, SortingOrder = 19
        };

        // Row 4: Pilgrim Bellbearer — Spinning Thrown Bell (F1-4) & Impact Sonic Ring (F5-6)
        public static readonly VfxClipMeta BellbearerBellSpin = new VfxClipMeta
        {
            Name = "BellbearerBellSpin",
            Row = 3, StartCol = 0, FrameCount = 4,
            Fps = 22f, Loop = true, DefaultScale = 0.52f, SortingOrder = 17
        };

        public static readonly VfxClipMeta BellbearerImpactSonic = new VfxClipMeta
        {
            Name = "BellbearerImpactSonic",
            Row = 3, StartCol = 4, FrameCount = 2,
            Fps = 20f, Loop = false, DefaultScale = 0.82f, SortingOrder = 19
        };

        // Row 5: Winged Furm — Helmet Clang Spark (F1-2) & Dizzy Stars Spiral Halo Loop (F3-6)
        public static readonly VfxClipMeta WingedFurmHelmetClang = new VfxClipMeta
        {
            Name = "WingedFurmHelmetClang",
            Row = 4, StartCol = 0, FrameCount = 2,
            Fps = 22f, Loop = false, DefaultScale = 0.78f, SortingOrder = 19
        };

        public static readonly VfxClipMeta WingedFurmDizzyLoop = new VfxClipMeta
        {
            Name = "WingedFurmDizzyLoop",
            Row = 4, StartCol = 2, FrameCount = 4,
            Fps = 18f, Loop = true, DefaultScale = 0.88f, SortingOrder = 16
        };
    }
}
