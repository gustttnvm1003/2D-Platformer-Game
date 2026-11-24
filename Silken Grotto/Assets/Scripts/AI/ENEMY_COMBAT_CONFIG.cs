using UnityEngine;

namespace SilkenGrotto
{
    public enum AknidCarryType
    {
        None,
        Berry,
        Flea
    }

    public enum PharlidAmbushMode
    {
        Ground,
        CeilingDrop
    }

    /// <summary>
    /// Centralized tuning & animation FPS table for Choral Chambers (6 enemies), Moss Grotto (3 enemies),
    /// and Sands of Karak & Bellhart (4 enemies: Driznit, Conchfly, Pharlid, Coral Furm).
    /// Movement/physics run at 60 FPS (FixedUpdate + Rigidbody2D interpolation);
    /// sprite animation playback stays strictly within 12-24 FPS.
    /// </summary>
    public static class ENEMY_COMBAT_CONFIG
    {
        public struct KarakProfile
        {
            public KarakArchetype Archetype;
            public string DisplayName;
            public string SheetStem;
            public string SpritePath;       // Existing reference image path (DO NOT MODIFY/DELETE!)
            public string SpriteSheetPath;  // Existing sprite sheet path (DO NOT MODIFY/DELETE!)
            public bool IsFlying;

            // Vitals & Rewards
            public int MaxHealth;
            public int ContactDamage;
            public float ContactKnockback;
            public float KnockbackResistance;
            public float StaggerDuration;
            public int SilkEssenceReward;
            public int OrbDropCount;

            // Movement & Detection (60 FPS physics)
            public float PatrolSpeed;
            public float AggroSpeed;
            public float PatrolSpanX;
            public float DetectRange;
            public float LoseTargetRange;
            public float AttackRange;
            public float IdealMinRange;
            public float IdealMaxRange;
            public float HoverAmplitude;
            public float HoverFrequency;

            // Driznit Bouncing Projectile
            public float ProjectileSpeed;
            public float ProjectileGravity;
            public int MaxBounces;
            public float BounceRestitution;
            public bool CanDeflectProjectile;
            public float ThrowWindupDuration;
            public float AttackCooldown;

            // Conchfly Precision Aerial Dart
            public float AimTelegraphDuration;
            public float DartSpeed;
            public float DartDuration;
            public float BrakeRecoverDuration;

            // Pharlid Skitterer & Ceiling Ambush
            public PharlidAmbushMode DefaultAmbushMode;
            public float DropTriggerWidth;
            public float DropWarningDuration;
            public float AmbushDropSpeed;
            public float LungeWindupDuration;
            public float QuickLungeSpeed;
            public float QuickLungeHopY;
            public float SkitterAwaySpeed;
            public float SkitterAwayDuration;

            // Coral Furm Crunchy Coral Impact
            public float AlertHopSpeed;
            public float AlertHopY;
            public float AlertHopDuration;
            public float ShellBraceDuration;
            public int CoralShardCountHit;
            public int CoralShardCountDeath;
            public float CoralCrunchHitstop;

            // Sprite Animation Playback FPS (12-24 FPS)
            public float IdleFps;
            public float MoveFps;
            public float WindupFps;
            public float AttackFps;
            public float HurtFps;
            public float DeathFps;
        }

        // SPRITE_PATH: Assets/Art/Characters/Enemies/driznit.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/driznit_sheet.png)
        public static readonly KarakProfile Driznit = new KarakProfile
        {
            Archetype = KarakArchetype.Driznit,
            DisplayName = "Driznit",
            SheetStem = "driznit",
            SpritePath = "Assets/Art/Characters/Enemies/driznit.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/driznit_sheet.png",
            IsFlying = true,

            MaxHealth = 28,             // Low HP (2-3 hits), pogo-friendly
            ContactDamage = 1,
            ContactKnockback = 5.8f,
            KnockbackResistance = 0.06f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 18,
            OrbDropCount = 2,

            PatrolSpeed = 3.1f,
            AggroSpeed = 5.2f,
            PatrolSpanX = 2.6f,
            DetectRange = 10.6f,
            LoseTargetRange = 15.0f,
            AttackRange = 9.4f,
            IdealMinRange = 4.2f,       // Backs off slightly if player pushes too close
            IdealMaxRange = 7.4f,
            HoverAmplitude = 0.34f,
            HoverFrequency = 3.2f,

            ProjectileSpeed = 12.6f,
            ProjectileGravity = 4.2f,   // Slight diagonal/gravity arc aimed at player's face
            MaxBounces = 3,             // Shatters after 3 wall/floor/ceiling bounces
            BounceRestitution = 0.94f,
            CanDeflectProjectile = true, // Deflectable by player's melee slash!
            ThrowWindupDuration = 0.35f,
            AttackCooldown = 1.40f,

            AimTelegraphDuration = 0f,
            DartSpeed = 0f,
            DartDuration = 0f,
            BrakeRecoverDuration = 0.28f,

            DefaultAmbushMode = PharlidAmbushMode.Ground,
            DropTriggerWidth = 0f,
            DropWarningDuration = 0f,
            AmbushDropSpeed = 0f,
            LungeWindupDuration = 0f,
            QuickLungeSpeed = 0f,
            QuickLungeHopY = 0f,
            SkitterAwaySpeed = 0f,
            SkitterAwayDuration = 0f,

            AlertHopSpeed = 0f,
            AlertHopY = 0f,
            AlertHopDuration = 0f,
            ShellBraceDuration = 0f,
            CoralShardCountHit = 0,
            CoralShardCountDeath = 0,
            CoralCrunchHitstop = 0f,

            IdleFps = 18f,                  // 16-22 FPS hand-drawn playback
            MoveFps = 20f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 18f,
            DeathFps = 16f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/conchfly.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/conchfly_sheet.png)
        public static readonly KarakProfile Conchfly = new KarakProfile
        {
            Archetype = KarakArchetype.Conchfly,
            DisplayName = "Conchfly",
            SheetStem = "conchfly",
            SpritePath = "Assets/Art/Characters/Enemies/conchfly.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/conchfly_sheet.png",
            IsFlying = true,

            MaxHealth = 36,             // Low-medium HP (3 hits), air-pogo friendly during BrakeAndRecover
            ContactDamage = 1,
            ContactKnockback = 6.8f,
            KnockbackResistance = 0.14f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 24,
            OrbDropCount = 3,

            PatrolSpeed = 2.8f,
            AggroSpeed = 4.6f,
            PatrolSpanX = 2.8f,
            DetectRange = 11.0f,
            LoseTargetRange = 16.0f,
            AttackRange = 9.6f,
            IdealMinRange = 4.2f,
            IdealMaxRange = 8.0f,
            HoverAmplitude = 0.56f,
            HoverFrequency = 2.1f,

            ProjectileSpeed = 0f,
            ProjectileGravity = 0f,
            MaxBounces = 0,
            BounceRestitution = 0f,
            CanDeflectProjectile = false,
            ThrowWindupDuration = 0.35f,
            AttackCooldown = 1.05f,

            AimTelegraphDuration = 0.35f, // Abrupt stop + pull-back + thin aim flash line
            DartSpeed = 18.2f,            // Fast locked straight diagonal dart (non-homing!)
            DartDuration = 0.42f,
            BrakeRecoverDuration = 0.55f, // Air-friction braking + squash -> easy pogo window

            DefaultAmbushMode = PharlidAmbushMode.Ground,
            DropTriggerWidth = 0f,
            DropWarningDuration = 0f,
            AmbushDropSpeed = 0f,
            LungeWindupDuration = 0f,
            QuickLungeSpeed = 0f,
            QuickLungeHopY = 0f,
            SkitterAwaySpeed = 0f,
            SkitterAwayDuration = 0f,

            AlertHopSpeed = 0f,
            AlertHopY = 0f,
            AlertHopDuration = 0f,
            ShellBraceDuration = 0f,
            CoralShardCountHit = 0,
            CoralShardCountDeath = 0,
            CoralCrunchHitstop = 0f,

            IdleFps = 18f,              // 18-24 FPS hand-drawn playback
            MoveFps = 20f,
            WindupFps = 18f,
            AttackFps = 24f,
            HurtFps = 18f,
            DeathFps = 18f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/pharlid.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/pharlid_sheet.png)
        public static readonly KarakProfile Pharlid = new KarakProfile
        {
            Archetype = KarakArchetype.Pharlid,
            DisplayName = "Pharlid",
            SheetStem = "pharlid",
            SpritePath = "Assets/Art/Characters/Enemies/pharlid.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/pharlid_sheet.png",
            IsFlying = false,

            MaxHealth = 32,             // Low-medium HP (2-3 hits), 1 contact damage
            ContactDamage = 1,
            ContactKnockback = 6.2f,
            KnockbackResistance = 0.08f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 20,
            OrbDropCount = 2,

            PatrolSpeed = 5.4f,         // Snappy stop-and-dart skitter speed
            AggroSpeed = 7.6f,
            PatrolSpanX = 4.6f,
            DetectRange = 8.6f,
            LoseTargetRange = 13.0f,
            AttackRange = 4.4f,
            IdealMinRange = 1.6f,
            IdealMaxRange = 4.4f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            ProjectileSpeed = 0f,
            ProjectileGravity = 0f,
            MaxBounces = 0,
            BounceRestitution = 0f,
            CanDeflectProjectile = false,
            ThrowWindupDuration = 0.25f,
            AttackCooldown = 0.72f,

            AimTelegraphDuration = 0f,
            DartSpeed = 0f,
            DartDuration = 0f,
            BrakeRecoverDuration = 0.34f,

            DefaultAmbushMode = PharlidAmbushMode.Ground,
            DropTriggerWidth = 2.65f,   // Overhead horizontal ambush column
            DropWarningDuration = 0.18f, // Brief tremor + falling dust before plunge
            AmbushDropSpeed = 16.5f,
            LungeWindupDuration = 0.25f, // Quick low lunge telegraph
            QuickLungeSpeed = 12.2f,
            QuickLungeHopY = 4.3f,
            SkitterAwaySpeed = 7.6f,    // Short agile retreat scuttle after lunge
            SkitterAwayDuration = 0.34f,

            AlertHopSpeed = 0f,
            AlertHopY = 0f,
            AlertHopDuration = 0f,
            ShellBraceDuration = 0f,
            CoralShardCountHit = 0,
            CoralShardCountDeath = 0,
            CoralCrunchHitstop = 0f,

            IdleFps = 18f,              // 18-24 FPS hand-drawn playback
            MoveFps = 24f,
            WindupFps = 18f,
            AttackFps = 24f,
            HurtFps = 18f,
            DeathFps = 18f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/coral_furm.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/coral_furm_sheet.png)
        public static readonly KarakProfile CoralFurm = new KarakProfile
        {
            Archetype = KarakArchetype.CoralFurm,
            DisplayName = "Coral Furm",
            SheetStem = "coral_furm",
            SpritePath = "Assets/Art/Characters/Enemies/coral_furm.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/coral_furm_sheet.png",
            IsFlying = false,

            MaxHealth = 36,             // Low-medium HP (3 hits), 1 contact damage, pogo-friendly!
            ContactDamage = 1,
            ContactKnockback = 6.8f,
            KnockbackResistance = 0.32f, // Measured knockback on ShellBrace
            StaggerDuration = 0.26f,
            SilkEssenceReward = 24,
            OrbDropCount = 3,

            PatrolSpeed = 2.0f,         // Steady grounded crawl
            AggroSpeed = 3.2f,
            PatrolSpanX = 4.0f,
            DetectRange = 7.2f,
            LoseTargetRange = 11.0f,
            AttackRange = 4.2f,
            IdealMinRange = 1.4f,
            IdealMaxRange = 4.2f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            ProjectileSpeed = 0f,
            ProjectileGravity = 0f,
            MaxBounces = 0,
            BounceRestitution = 0f,
            CanDeflectProjectile = false,
            ThrowWindupDuration = 0.24f,
            AttackCooldown = 1.05f,

            AimTelegraphDuration = 0f,
            DartSpeed = 0f,
            DartDuration = 0f,
            BrakeRecoverDuration = 0.38f,

            DefaultAmbushMode = PharlidAmbushMode.Ground,
            DropTriggerWidth = 0f,
            DropWarningDuration = 0f,
            AmbushDropSpeed = 0f,
            LungeWindupDuration = 0.24f,
            QuickLungeSpeed = 0f,
            QuickLungeHopY = 0f,
            SkitterAwaySpeed = 0f,
            SkitterAwayDuration = 0f,

            AlertHopSpeed = 6.8f,       // Forward crawl surge / short hop on mid-close approach
            AlertHopY = 4.1f,
            AlertHopDuration = 0.36f,
            ShellBraceDuration = 0.28f, // Brief compressed recoil shedding coral shards
            CoralShardCountHit = 8,
            CoralShardCountDeath = 16,
            CoralCrunchHitstop = 0.036f, // Tactile 36ms crunch hitstop (30-40ms target)

            IdleFps = 14f,              // 14-20 FPS hand-drawn playback
            MoveFps = 16f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        public static KarakProfile GetKarak(KarakArchetype kind)
        {
            switch (kind)
            {
                case KarakArchetype.Driznit: return Driznit;
                case KarakArchetype.Conchfly: return Conchfly;
                case KarakArchetype.CoralFurm: return CoralFurm;
                default: return Pharlid;
            }
        }

        public static bool TryGetKarakByStem(string stem, out KarakProfile profile)
        {
            if (stem == "driznit") { profile = Driznit; return true; }
            if (stem == "conchfly") { profile = Conchfly; return true; }
            if (stem == "pharlid" || stem == "pharlid_diver") { profile = Pharlid; return true; }
            if (stem == "coral_furm") { profile = CoralFurm; return true; }
            profile = Pharlid;
            return false;
        }

        public struct MountFayProfile
        {
            public MountFayArchetype Archetype;
            public string DisplayName;
            public string SheetStem;
            public string SpritePath;       // Existing reference image path (DO NOT MODIFY/DELETE!)
            public string SpriteSheetPath;  // Existing sprite sheet path (DO NOT MODIFY/DELETE!)
            public bool IsFlying;
            public bool IsElite;

            // Vitals & Rewards
            public int MaxHealth;
            public int ContactDamage;
            public float ContactKnockback;
            public float KnockbackResistance;
            public float StaggerDuration;
            public int SilkEssenceReward;
            public int OrbDropCount;
            public float RespawnAfterSeconds; // > 0 preserves parkour pogo pads (Driftlin = 3s)

            // Hover / Turret Detection (60 FPS physics)
            public float DetectRange;
            public float LoseTargetRange;
            public float AttackRange;
            public float HoverAmplitude;
            public float HoverFrequency;
            public float AnchorReturnSpeed;

            // Servitor Boran Tracking Turret (2-Damage Burst Laser)
            public float LanternConeLength;
            public float LanternConeHalfAngleDeg;
            public float LaserChargeDuration;
            public float LaserFireDuration;
            public int LaserDamage;
            public float LaserKnockback;
            public float LaserLength;
            public int LaserBurstCount;
            public float LaserBurstInterval;
            public float LaserCooldown;

            // Driftlin / Mnemonid / Mnemonord Pogo Pad Recoil
            public float PogoRecoilOffsetY;
            public float PogoRecoilDuration;
            public Vector2 BodyColliderSize;
            public Vector2 BodyHurtboxSize;

            // Mnemonid & Mnemonord Retaliation Spikes
            public bool HasRetaliationSpikes;
            public float SpikeTriggerDelay;
            public float SpikeActiveDuration;
            public float SpikeInnerRadius;
            public float SpikeOuterRadius;
            public int SpikeCount;
            public int SpikeContactDamage;

            // Sprite Animation Playback FPS (12-24 FPS)
            public float IdleFps;
            public float MoveFps;
            public float WindupFps;
            public float AttackFps;
            public float HurtFps;
            public float DeathFps;
        }

        // SPRITE_PATH: Assets/Art/Characters/Enemies/servitor_boran.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/servitor_boran_sheet.png)
        public static readonly MountFayProfile ServitorBoran = new MountFayProfile
        {
            Archetype = MountFayArchetype.ServitorBoran,
            DisplayName = "Servitor Boran",
            SheetStem = "servitor_boran",
            SpritePath = "Assets/Art/Characters/Enemies/servitor_boran.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/servitor_boran_sheet.png",
            IsFlying = false,
            IsElite = true,

            MaxHealth = 68,                 // Medium HP pressure turret
            ContactDamage = 1,
            ContactKnockback = 7.4f,
            KnockbackResistance = 0.88f,    // Heavily knockback resistant to stay planted as turret
            StaggerDuration = 0.10f,
            SilkEssenceReward = 40,
            OrbDropCount = 4,
            RespawnAfterSeconds = 0f,

            DetectRange = 12.2f,
            LoseTargetRange = 16.5f,
            AttackRange = 11.4f,
            HoverAmplitude = 0.08f,
            HoverFrequency = 1.9f,
            AnchorReturnSpeed = 2.5f,

            LanternConeLength = 11.2f,
            LanternConeHalfAngleDeg = 15f,
            LaserChargeDuration = 0.55f,    // Narrowing locked aim sightline telegraph
            LaserFireDuration = 0.35f,      // Bright straight 2-damage laser beam
            LaserDamage = 2,
            LaserKnockback = 9.6f,
            LaserLength = 13.0f,
            LaserBurstCount = 3,            // 2-3 sequential rapid laser shots while player stays in range
            LaserBurstInterval = 0.60f,
            LaserCooldown = 1.65f,

            PogoRecoilOffsetY = 0f,
            PogoRecoilDuration = 0f,
            BodyColliderSize = new Vector2(1.16f, 1.68f),
            BodyHurtboxSize = new Vector2(1.36f, 2.35f),

            HasRetaliationSpikes = false,
            SpikeTriggerDelay = 0f,
            SpikeActiveDuration = 0f,
            SpikeInnerRadius = 0f,
            SpikeOuterRadius = 0f,
            SpikeCount = 0,
            SpikeContactDamage = 0,

            IdleFps = 14f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 14f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/driftlin.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/driftlin_sheet.png)
        public static readonly MountFayProfile Driftlin = new MountFayProfile
        {
            Archetype = MountFayArchetype.Driftlin,
            DisplayName = "Driftlin",
            SheetStem = "driftlin",
            SpritePath = "Assets/Art/Characters/Enemies/driftlin.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/driftlin_sheet.png",
            IsFlying = true,
            IsElite = false,

            MaxHealth = 90,                 // High HP + 3s respawn so pogo parkour never breaks!
            ContactDamage = 0,              // Passive pogo pad (configurable 0 or 1 via config)
            ContactKnockback = 4.5f,
            KnockbackResistance = 0.72f,
            StaggerDuration = 0.14f,
            SilkEssenceReward = 14,
            OrbDropCount = 2,
            RespawnAfterSeconds = 3.0f,

            DetectRange = 0f,
            LoseTargetRange = 0f,
            AttackRange = 0f,
            HoverAmplitude = 0.24f,
            HoverFrequency = 2.2f,
            AnchorReturnSpeed = 5.4f,

            LanternConeLength = 0f,
            LanternConeHalfAngleDeg = 0f,
            LaserChargeDuration = 0f,
            LaserFireDuration = 0f,
            LaserDamage = 0,
            LaserKnockback = 0f,
            LaserLength = 0f,
            LaserBurstCount = 0,
            LaserBurstInterval = 0f,
            LaserCooldown = 0f,

            PogoRecoilOffsetY = 0.58f,      // Elastic downward bounce pad dip
            PogoRecoilDuration = 0.30f,     // Matches 6-frame PogoBounce @ 18 FPS smoothly
            BodyColliderSize = new Vector2(0.92f, 0.88f),
            BodyHurtboxSize = new Vector2(1.48f, 1.32f), // Generous pogo pad hurtbox

            HasRetaliationSpikes = false,
            SpikeTriggerDelay = 0f,
            SpikeActiveDuration = 0f,
            SpikeInnerRadius = 0f,
            SpikeOuterRadius = 0f,
            SpikeCount = 0,
            SpikeContactDamage = 0,

            IdleFps = 14f,                  // 12-18 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 18f,
            AttackFps = 18f,
            HurtFps = 14f,
            DeathFps = 12f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/mnemonid.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/mnemonid_sheet.png)
        public static readonly MountFayProfile Mnemonid = new MountFayProfile
        {
            Archetype = MountFayArchetype.Mnemonid,
            DisplayName = "Mnemonid",
            SheetStem = "mnemonid",
            SpritePath = "Assets/Art/Characters/Enemies/mnemonid.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/mnemonid_sheet.png",
            IsFlying = true,
            IsElite = false,

            MaxHealth = 46,                 // Smaller crystal pogo platform with retaliation spikes
            ContactDamage = 1,
            ContactKnockback = 6.2f,
            KnockbackResistance = 0.50f,
            StaggerDuration = 0.14f,
            SilkEssenceReward = 20,
            OrbDropCount = 2,
            RespawnAfterSeconds = 4.0f,

            DetectRange = 0f,
            LoseTargetRange = 0f,
            AttackRange = 0f,
            HoverAmplitude = 0.20f,
            HoverFrequency = 2.4f,
            AnchorReturnSpeed = 4.8f,

            LanternConeLength = 0f,
            LanternConeHalfAngleDeg = 0f,
            LaserChargeDuration = 0f,
            LaserFireDuration = 0f,
            LaserDamage = 0,
            LaserKnockback = 0f,
            LaserLength = 0f,
            LaserBurstCount = 0,
            LaserBurstInterval = 0f,
            LaserCooldown = 0f,

            PogoRecoilOffsetY = 0.42f,
            PogoRecoilDuration = 0.18f,
            BodyColliderSize = new Vector2(0.88f, 0.88f),
            BodyHurtboxSize = new Vector2(1.30f, 1.24f),

            HasRetaliationSpikes = true,
            SpikeTriggerDelay = 0.12f,      // Brief 0.12s window so player bounces away cleanly from pogo
            SpikeActiveDuration = 1.20f,    // 1.2s full-body retaliation spike ring
            SpikeInnerRadius = 0.56f,
            SpikeOuterRadius = 1.08f,
            SpikeCount = 10,
            SpikeContactDamage = 1,

            IdleFps = 14f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 14f,
            WindupFps = 20f,
            AttackFps = 18f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/mnemonord.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/mnemonord_sheet.png)
        public static readonly MountFayProfile Mnemonord = new MountFayProfile
        {
            Archetype = MountFayArchetype.Mnemonord,
            DisplayName = "Mnemonord",
            SheetStem = "mnemonord",
            SpritePath = "Assets/Art/Characters/Enemies/mnemonord.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/mnemonord_sheet.png",
            IsFlying = true,
            IsElite = false,

            MaxHealth = 78,                 // Larger HP, larger hitbox, longer (+0.3s) spike ring!
            ContactDamage = 1,
            ContactKnockback = 7.4f,
            KnockbackResistance = 0.65f,
            StaggerDuration = 0.14f,
            SilkEssenceReward = 34,
            OrbDropCount = 3,
            RespawnAfterSeconds = 4.5f,

            DetectRange = 0f,
            LoseTargetRange = 0f,
            AttackRange = 0f,
            HoverAmplitude = 0.18f,
            HoverFrequency = 1.9f,
            AnchorReturnSpeed = 4.2f,

            LanternConeLength = 0f,
            LanternConeHalfAngleDeg = 0f,
            LaserChargeDuration = 0f,
            LaserFireDuration = 0f,
            LaserDamage = 0,
            LaserKnockback = 0f,
            LaserLength = 0f,
            LaserBurstCount = 0,
            LaserBurstInterval = 0f,
            LaserCooldown = 0f,

            PogoRecoilOffsetY = 0.34f,
            PogoRecoilDuration = 0.18f,
            BodyColliderSize = new Vector2(1.76f, 1.76f),
            BodyHurtboxSize = new Vector2(2.12f, 2.06f),

            HasRetaliationSpikes = true,
            SpikeTriggerDelay = 0.12f,
            SpikeActiveDuration = 1.50f,    // 1.2s + 0.3s longer spike retaliation!
            SpikeInnerRadius = 0.96f,
            SpikeOuterRadius = 1.68f,       // Wider spike reach matching large Mnemonord scale
            SpikeCount = 14,
            SpikeContactDamage = 1,

            IdleFps = 13f,                  // 12-18 FPS hand-drawn playback
            MoveFps = 13f,
            WindupFps = 18f,
            AttackFps = 16f,
            HurtFps = 14f,
            DeathFps = 12f
        };

        public static MountFayProfile GetMountFay(MountFayArchetype kind)
        {
            switch (kind)
            {
                case MountFayArchetype.ServitorBoran: return ServitorBoran;
                case MountFayArchetype.Mnemonord: return Mnemonord;
                case MountFayArchetype.Mnemonid: return Mnemonid;
                default: return Driftlin;
            }
        }

        public static bool TryGetMountFayByStem(string stem, out MountFayProfile profile)
        {
            if (stem == "servitor_boran") { profile = ServitorBoran; return true; }
            if (stem == "driftlin") { profile = Driftlin; return true; }
            if (stem == "mnemonid") { profile = Mnemonid; return true; }
            if (stem == "mnemonord") { profile = Mnemonord; return true; }
            profile = Driftlin;
            return false;
        }

        public struct Batch2Profile
        {
            public string DisplayName;
            public string SheetStem;
            public string SpritePath;       // Existing reference image path (DO NOT MODIFY/DELETE!)
            public string SpriteSheetPath;  // Existing sprite sheet path (DO NOT MODIFY/DELETE!)
            public bool IsFlying;

            // Vitals & Rewards
            public int MaxHealth;
            public int ContactDamage;
            public float ContactKnockback;
            public float KnockbackResistance;
            public float StaggerDuration;
            public int SilkEssenceReward;
            public int OrbDropCount;

            // Core Movement & Detection (60 FPS physics)
            public float PatrolSpeed;
            public float AggroSpeed;
            public float PatrolSpanX;
            public float DetectRange;
            public float LoseTargetRange;
            public float AttackRange;
            public float HoverAmplitude;
            public float HoverFrequency;

            // Drapemite (Aggressive Leaping Slasher)
            public float CrouchTelegraphDuration;
            public float LeapSpeedX;
            public float LeapVelY;
            public float LeapSlashDuration;
            public float LandRecoverDuration;

            // Drapefly (Side-to-Side Sweeper)
            public float SweepVertTolerance;
            public float SweepWindupDuration;
            public float SweepSpeed;
            public float SweepMaxDistance;
            public float TurnRecoverDuration;

            // Pondcatcher (Spear Hunter)
            public float IdlePokeInterval;
            public float SpearWindupDuration;
            public float SpearReach;
            public float SpearThrustDuration;
            public float PullBackRecoverDuration;
            public float TooCloseRepositionRange;
            public float RepositionBackSpeed;
            public float RepositionDuration;

            // Furm (Underground Ambush Jumper)
            public float BurrowTriggerRange;
            public float EruptWarningDuration;
            public float PopJumpSpeedX;
            public float PopJumpVelY;
            public float VulnerableWindowDuration;
            public float BurrowReenterDelay;

            // Winged Furm (Vertical Bobber — Dizzy Helmet Reaction)
            public float VerticalAmplitude;
            public float VerticalBobSpeed;
            public float DizzyDuration;
            public float HelmetPushbackForce;

            public float AttackCooldown;
            public Vector2 BodyColliderSize;
            public Vector2 BodyHurtboxSize;

            // Sprite Animation Playback FPS (12-24 FPS)
            public float IdleFps;
            public float MoveFps;
            public float WindupFps;
            public float AttackFps;
            public float HurtFps;
            public float DeathFps;
        }

        // SPRITE_PATH: Assets/Art/Characters/Enemies/drapemite.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/drapemite_sheet.png)
        public static readonly Batch2Profile Drapemite = new Batch2Profile
        {
            DisplayName = "Drapemite",
            SheetStem = "drapemite",
            SpritePath = "Assets/Art/Characters/Enemies/drapemite.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/drapemite_sheet.png",
            IsFlying = false,

            MaxHealth = 32,                 // Low-medium HP (2-3 hits), 1 damage
            ContactDamage = 1,
            ContactKnockback = 6.2f,
            KnockbackResistance = 0.08f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 18,
            OrbDropCount = 2,

            PatrolSpeed = 2.1f,
            AggroSpeed = 5.4f,
            PatrolSpanX = 4.0f,
            DetectRange = 8.6f,
            LoseTargetRange = 13.0f,
            AttackRange = 6.4f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            CrouchTelegraphDuration = 0.28f, // Vertical squash crouch telegraph
            LeapSpeedX = 11.4f,              // Fast parabolic SlashLeap toward player
            LeapVelY = 7.4f,
            LeapSlashDuration = 0.36f,
            LandRecoverDuration = 0.45f,

            AttackCooldown = 0.55f,
            BodyColliderSize = new Vector2(1.06f, 0.86f),
            BodyHurtboxSize = new Vector2(1.26f, 1.04f),

            IdleFps = 16f,                  // 16-24 FPS hand-drawn playback
            MoveFps = 20f,
            WindupFps = 18f,
            AttackFps = 24f,
            HurtFps = 18f,
            DeathFps = 16f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/drapefly.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/drapefly_sheet.png)
        public static readonly Batch2Profile Drapefly = new Batch2Profile
        {
            DisplayName = "Drapefly",
            SheetStem = "drapefly",
            SpritePath = "Assets/Art/Characters/Enemies/drapefly.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/drapefly_sheet.png",
            IsFlying = true,

            MaxHealth = 22,                 // Low HP (2 hits), 1 damage, easy to air-pogo from above!
            ContactDamage = 1,
            ContactKnockback = 5.8f,
            KnockbackResistance = 0.06f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 16,
            OrbDropCount = 2,

            PatrolSpeed = 2.4f,
            AggroSpeed = 3.8f,
            PatrolSpanX = 2.6f,
            DetectRange = 11.5f,
            LoseTargetRange = 16.0f,
            AttackRange = 10.4f,
            HoverAmplitude = 0.34f,
            HoverFrequency = 3.0f,

            SweepVertTolerance = 2.15f,     // Triggers horizontal sweep when aligned with player
            SweepWindupDuration = 0.30f,    // Slight pull-back before sweeping across
            SweepSpeed = 14.8f,
            SweepMaxDistance = 12.2f,
            TurnRecoverDuration = 0.50f,

            AttackCooldown = 0.85f,
            BodyColliderSize = new Vector2(0.92f, 0.92f),
            BodyHurtboxSize = new Vector2(1.26f, 1.24f),

            IdleFps = 18f,                  // 16-24 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 18f,
            AttackFps = 24f,
            HurtFps = 18f,
            DeathFps = 16f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/pondcatcher.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/pondcatcher_sheet.png)
        public static readonly Batch2Profile Pondcatcher = new Batch2Profile
        {
            DisplayName = "Pondcatcher",
            SheetStem = "pondcatcher",
            SpritePath = "Assets/Art/Characters/Enemies/pondcatcher.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/pondcatcher_sheet.png",
            IsFlying = false,

            MaxHealth = 48,                 // Medium HP (4 hits), 1 damage spear hunter
            ContactDamage = 1,
            ContactKnockback = 7.0f,
            KnockbackResistance = 0.18f,
            StaggerDuration = 0.18f,
            SilkEssenceReward = 26,
            OrbDropCount = 3,

            PatrolSpeed = 2.2f,
            AggroSpeed = 4.2f,
            PatrolSpanX = 3.2f,
            DetectRange = 9.6f,
            LoseTargetRange = 14.0f,
            AttackRange = 4.4f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            IdlePokeInterval = 2.55f,       // Periodic downward pond-hunting spear poke
            SpearWindupDuration = 0.35f,    // Pulls spear back aiming horizontal OR diagonal downward
            SpearReach = 3.95f,
            SpearThrustDuration = 0.24f,
            PullBackRecoverDuration = 0.50f,
            TooCloseRepositionRange = 2.25f,
            RepositionBackSpeed = 6.2f,
            RepositionDuration = 0.28f,

            AttackCooldown = 0.72f,
            BodyColliderSize = new Vector2(1.00f, 1.40f),
            BodyHurtboxSize = new Vector2(1.18f, 1.56f),

            IdleFps = 14f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/furm.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/furm_sheet.png)
        public static readonly Batch2Profile Furm = new Batch2Profile
        {
            DisplayName = "Furm",
            SheetStem = "furm",
            SpritePath = "Assets/Art/Characters/Enemies/furm.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/furm_sheet.png",
            IsFlying = false,

            MaxHealth = 28,                 // Low HP (2-3 hits), 1 damage underground ambusher
            ContactDamage = 1,
            ContactKnockback = 6.2f,
            KnockbackResistance = 0.12f,
            StaggerDuration = 0.18f,
            SilkEssenceReward = 20,
            OrbDropCount = 2,

            PatrolSpeed = 0f,
            AggroSpeed = 0f,
            PatrolSpanX = 0f,
            DetectRange = 4.8f,
            LoseTargetRange = 9.0f,
            AttackRange = 4.8f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            BurrowTriggerRange = 4.6f,      // Triggers EruptWarning when player walks nearby
            EruptWarningDuration = 0.22f,   // Soil shake + dust puff telegraph
            PopJumpSpeedX = 8.4f,           // Explosive fast pop jump toward player
            PopJumpVelY = 9.2f,
            VulnerableWindowDuration = 0.65f, // Exposed landing punish window before re-burrowing
            BurrowReenterDelay = 0.26f,

            AttackCooldown = 0.90f,
            BodyColliderSize = new Vector2(1.02f, 0.96f),
            BodyHurtboxSize = new Vector2(1.24f, 1.12f),

            IdleFps = 16f,                  // 16-24 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 20f,
            AttackFps = 24f,
            HurtFps = 18f,
            DeathFps = 16f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/winged_furm.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/winged_furm_sheet.png)
        public static readonly Batch2Profile WingedFurm = new Batch2Profile
        {
            DisplayName = "Winged Furm",
            SheetStem = "winged_furm",
            SpritePath = "Assets/Art/Characters/Enemies/winged_furm.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/winged_furm_sheet.png",
            IsFlying = true,

            MaxHealth = 36,                 // Medium-low HP (3 hits), 1 contact damage, pogo-friendly!
            ContactDamage = 1,
            ContactKnockback = 6.0f,
            KnockbackResistance = 0.18f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 24,
            OrbDropCount = 3,

            PatrolSpeed = 2.4f,
            AggroSpeed = 0f,
            PatrolSpanX = 0f,
            DetectRange = 0f,
            LoseTargetRange = 0f,
            AttackRange = 0f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            VerticalAmplitude = 2.65f,      // Smooth vertical up-down bobbing on fixed X axis
            VerticalBobSpeed = 1.65f,
            DizzyDuration = 0.80f,          // Helmet clang -> 0.80s dizzy wobble + orbiting stars!
            HelmetPushbackForce = 4.2f,

            AttackCooldown = 0f,
            BodyColliderSize = new Vector2(1.16f, 1.16f),
            BodyHurtboxSize = new Vector2(1.42f, 1.40f),

            IdleFps = 18f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 18f,
            AttackFps = 16f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        public static bool TryGetBatch2ByStem(string stem, out Batch2Profile profile)
        {
            if (stem == "drapemite") { profile = Drapemite; return true; }
            if (stem == "drapefly") { profile = Drapefly; return true; }
            if (stem == "pondcatcher") { profile = Pondcatcher; return true; }
            if (stem == "furm") { profile = Furm; return true; }
            if (stem == "winged_furm") { profile = WingedFurm; return true; }
            profile = Drapemite;
            return false;
        }

        public struct PilgrimProfile
        {
            public ShellwoodArchetype Archetype;
            public string DisplayName;
            public string SheetStem;
            public string SpritePath;       // Existing reference image path (DO NOT MODIFY/DELETE!)
            public string SpriteSheetPath;  // Existing sprite sheet path (DO NOT MODIFY/DELETE!)
            public bool IsFlying;
            public bool IsElite;

            // Vitals, Damage & Rosary Rewards
            public int MaxHealth;
            public int ContactDamage;
            public int AttackDamage;
            public float ContactKnockback;
            public float KnockbackResistance;
            public float StaggerDuration;
            public int RosaryDropCount;     // Orb count dropped on defeat (Covetous = 5x-6x standard!)
            public int SilkEssenceReward;
            public bool DropRosariesOnHit;  // Optional gleaming hit-drop rosaries for Covetous Pilgrim
            public int HitDropRosaryCount;

            // Movement & Detection (60 FPS physics)
            public float PatrolSpeed;
            public float AggroSpeed;
            public float PatrolSpanX;
            public float DetectRange;
            public float LoseTargetRange;
            public float AttackRange;
            public float IdealMinRange;
            public float IdealMaxRange;
            public float HoverAmplitude;
            public float HoverFrequency;

            // Pilgrim Groveller (Basic Floor Jumper)
            public float JumpTelegraphDuration;
            public float HopSpeedX;
            public float HopVelY;
            public float LandPauseDuration;

            // Winged Pilgrim (Diving Aerial Pilgrim)
            public float AirCircleRadiusX;
            public float DiveTelegraphDuration;
            public float DiveSpeed;
            public float DiveDuration;
            public float BrakeRiseDuration;

            // Pilgrim Bellbearer (Ranged Bell Thrower)
            public float BellWindupDuration;
            public float BellThrowSpeed;
            public float BellGravity;
            public bool CanDeflectBell;

            // Covetous Pilgrim (High-Reward Multi-Move Brawler)
            public float ChainSwingWindup;
            public float HeavySlamWindup;
            public float BackstepSpeed;
            public float BackstepDuration;

            // Pilgrim Hulk (Rolling Charger)
            public float RollCurlTelegraphDuration;
            public float RollSpeed;
            public float RollMaxDuration;
            public float WallBonkRecoverDuration;
            public int RollingChargeDamage;

            public float RecoverDuration;
            public float AttackCooldown;
            public Vector2 BodyColliderSize;
            public Vector2 BodyHurtboxSize;

            // Sprite Animation Playback FPS (12-24 FPS)
            public float IdleFps;
            public float MoveFps;
            public float WindupFps;
            public float AttackFps;
            public float HurtFps;
            public float DeathFps;
        }

        // SPRITE_PATH: Assets/Art/Characters/Enemies/pilgrim_groveller.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/pilgrim_groveller_sheet.png)
        public static readonly PilgrimProfile PilgrimGroveller = new PilgrimProfile
        {
            Archetype = ShellwoodArchetype.PilgrimGroveller,
            DisplayName = "Pilgrim Groveller",
            SheetStem = "pilgrim_groveller",
            SpritePath = "Assets/Art/Characters/Enemies/pilgrim_groveller.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/pilgrim_groveller_sheet.png",
            IsFlying = false,
            IsElite = false,

            MaxHealth = 22,                 // Low HP (2 hits), 1 damage
            ContactDamage = 1,
            AttackDamage = 1,
            ContactKnockback = 5.8f,
            KnockbackResistance = 0.05f,
            StaggerDuration = 0.16f,
            RosaryDropCount = 2,
            SilkEssenceReward = 14,
            DropRosariesOnHit = false,
            HitDropRosaryCount = 0,

            PatrolSpeed = 1.85f,            // SlowWalkPatrol
            AggroSpeed = 3.2f,
            PatrolSpanX = 3.8f,
            DetectRange = 7.6f,
            LoseTargetRange = 11.5f,
            AttackRange = 6.2f,

            JumpTelegraphDuration = 0.20f,  // Brief 0.2s crouch telegraph
            HopSpeedX = 7.8f,               // Short lunging hop toward player
            HopVelY = 6.8f,
            LandPauseDuration = 0.45f,

            RecoverDuration = 0.45f,
            AttackCooldown = 0.60f,
            BodyColliderSize = new Vector2(0.90f, 0.96f),
            BodyHurtboxSize = new Vector2(1.14f, 1.12f),

            IdleFps = 14f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 16f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/winged_pilgrim.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/winged_pilgrim_sheet.png)
        public static readonly PilgrimProfile WingedPilgrim = new PilgrimProfile
        {
            Archetype = ShellwoodArchetype.WingedPilgrim,
            DisplayName = "Winged Pilgrim",
            SheetStem = "winged_pilgrim",
            SpritePath = "Assets/Art/Characters/Enemies/winged_pilgrim.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/winged_pilgrim_sheet.png",
            IsFlying = true,
            IsElite = false,

            MaxHealth = 30,                 // Low HP (2-3 hits), 1 damage, pogo-friendly
            ContactDamage = 1,
            AttackDamage = 1,
            ContactKnockback = 6.2f,
            KnockbackResistance = 0.10f,
            StaggerDuration = 0.16f,
            RosaryDropCount = 2,
            SilkEssenceReward = 18,
            DropRosariesOnHit = false,
            HitDropRosaryCount = 0,

            PatrolSpeed = 2.6f,
            AggroSpeed = 3.8f,
            PatrolSpanX = 2.6f,
            DetectRange = 9.4f,
            LoseTargetRange = 14.0f,
            AttackRange = 8.4f,
            HoverAmplitude = 0.48f,
            HoverFrequency = 2.4f,

            AirCircleRadiusX = 2.6f,
            DiveTelegraphDuration = 0.30f,  // Stops 0.3s to lock player coordinate
            DiveSpeed = 13.6f,              // BodyThrowDive angled straight at locked target
            DiveDuration = 0.48f,
            BrakeRiseDuration = 0.52f,      // Decelerates then rises smoothly back to patrol height

            RecoverDuration = 0.52f,
            AttackCooldown = 0.90f,
            BodyColliderSize = new Vector2(0.80f, 0.96f),
            BodyHurtboxSize = new Vector2(1.14f, 1.24f),

            IdleFps = 18f,                  // 16-22 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 18f,
            DeathFps = 16f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/pilgrim_bellbearer.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/pilgrim_bellbearer_sheet.png)
        public static readonly PilgrimProfile PilgrimBellbearer = new PilgrimProfile
        {
            Archetype = ShellwoodArchetype.PilgrimBellbearer,
            DisplayName = "Pilgrim Bellbearer",
            SheetStem = "pilgrim_bellbearer",
            SpritePath = "Assets/Art/Characters/Enemies/pilgrim_bellbearer.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/pilgrim_bellbearer_sheet.png",
            IsFlying = false,
            IsElite = false,

            MaxHealth = 36,                 // Low-medium HP (3 hits), 1 damage
            ContactDamage = 1,
            AttackDamage = 1,
            ContactKnockback = 6.2f,
            KnockbackResistance = 0.16f,
            StaggerDuration = 0.18f,
            RosaryDropCount = 3,
            SilkEssenceReward = 22,
            DropRosariesOnHit = false,
            HitDropRosaryCount = 0,

            PatrolSpeed = 1.9f,
            AggroSpeed = 2.6f,
            PatrolSpanX = 3.4f,
            DetectRange = 10.6f,
            LoseTargetRange = 14.5f,
            AttackRange = 9.0f,
            IdealMinRange = 4.0f,           // Slowly backs away if player crowds too close
            IdealMaxRange = 8.4f,

            BellWindupDuration = 0.35f,     // 0.35s BellThrowWindup
            BellThrowSpeed = 10.6f,
            BellGravity = 14.5f,
            CanDeflectBell = true,          // Player can deflect incoming bronze bells!

            RecoverDuration = 0.34f,
            AttackCooldown = 1.30f,         // 1.3s throw cooldown
            BodyColliderSize = new Vector2(0.92f, 1.08f),
            BodyHurtboxSize = new Vector2(1.16f, 1.26f),

            IdleFps = 14f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 16f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/covetous_pilgrim.png (Hand-Drawn 5x6 Sheet: Assets/Sprites/Enemies/covetous_pilgrim_sheet.png)
        public static readonly PilgrimProfile CovetousPilgrim = new PilgrimProfile
        {
            Archetype = ShellwoodArchetype.CovetousPilgrim,
            DisplayName = "Covetous Pilgrim",
            SheetStem = "covetous_pilgrim",
            SpritePath = "Assets/Art/Characters/Enemies/covetous_pilgrim.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/covetous_pilgrim_sheet.png",
            IsFlying = false,
            IsElite = true,

            MaxHealth = 78,                 // Medium-high HP (6-7 hits), moderate knockback resistance
            ContactDamage = 1,
            AttackDamage = 2,               // 1-2 damage configurable (2 on heavy rosary chain/slam!)
            ContactKnockback = 7.8f,
            KnockbackResistance = 0.44f,
            StaggerDuration = 0.16f,
            RosaryDropCount = 12,           // 5x-6x standard Pilgrim rosary jackpot!
            SilkEssenceReward = 120,
            DropRosariesOnHit = true,       // Jingling mini rosaries scatter on every hit!
            HitDropRosaryCount = 2,

            PatrolSpeed = 2.0f,
            AggroSpeed = 3.5f,
            PatrolSpanX = 3.8f,
            DetectRange = 9.2f,
            LoseTargetRange = 13.5f,
            AttackRange = 3.55f,

            ChainSwingWindup = 0.32f,
            HeavySlamWindup = 0.42f,
            BackstepSpeed = 6.8f,
            BackstepDuration = 0.24f,

            RecoverDuration = 0.46f,
            AttackCooldown = 0.80f,
            BodyColliderSize = new Vector2(1.08f, 1.38f),
            BodyHurtboxSize = new Vector2(1.30f, 1.56f),

            IdleFps = 14f,                  // 14-20 FPS hand-drawn playback
            MoveFps = 16f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/pilgrim_hulk.png (Hand-Drawn 4x6 Sheet: Assets/Sprites/Enemies/pilgrim_hulk_sheet.png)
        public static readonly PilgrimProfile PilgrimHulk = new PilgrimProfile
        {
            Archetype = ShellwoodArchetype.PilgrimHulk,
            DisplayName = "Pilgrim Hulk",
            SheetStem = "pilgrim_hulk",
            SpritePath = "Assets/Art/Characters/Enemies/pilgrim_hulk.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/pilgrim_hulk_sheet.png",
            IsFlying = false,
            IsElite = true,

            MaxHealth = 58,                 // Medium HP (4-5 hits), heavy roll weight, pogo-friendly!
            ContactDamage = 1,
            AttackDamage = 2,
            ContactKnockback = 9.0f,
            KnockbackResistance = 0.72f,
            StaggerDuration = 0.14f,
            RosaryDropCount = 4,
            SilkEssenceReward = 36,
            DropRosariesOnHit = false,
            HitDropRosaryCount = 0,

            PatrolSpeed = 1.45f,            // HeavyWalk
            AggroSpeed = 2.25f,
            PatrolSpanX = 4.0f,
            DetectRange = 9.4f,
            LoseTargetRange = 13.5f,
            AttackRange = 7.4f,

            RollCurlTelegraphDuration = 0.35f, // Body curls/compresses + kicking foot dust
            RollSpeed = 11.4f,                 // Continuous 360-deg rolling charge!
            RollMaxDuration = 1.35f,
            WallBonkRecoverDuration = 0.60f,   // Rebounds slightly on wall impact then uncurls
            RollingChargeDamage = 2,           // 2 damage during RollingCharge (1 on normal walk contact)

            RecoverDuration = 0.60f,
            AttackCooldown = 1.05f,
            BodyColliderSize = new Vector2(1.38f, 1.78f),
            BodyHurtboxSize = new Vector2(1.60f, 1.94f),

            IdleFps = 14f,                  // 14-22 FPS hand-drawn playback
            MoveFps = 16f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        public static PilgrimProfile GetPilgrim(ShellwoodArchetype kind)
        {
            switch (kind)
            {
                case ShellwoodArchetype.WingedPilgrim: return WingedPilgrim;
                case ShellwoodArchetype.PilgrimBellbearer: return PilgrimBellbearer;
                case ShellwoodArchetype.CovetousPilgrim: return CovetousPilgrim;
                case ShellwoodArchetype.PilgrimHulk: return PilgrimHulk;
                default: return PilgrimGroveller;
            }
        }

        public static bool TryGetPilgrimByStem(string stem, out PilgrimProfile profile)
        {
            if (stem == "pilgrim_groveller") { profile = PilgrimGroveller; return true; }
            if (stem == "winged_pilgrim") { profile = WingedPilgrim; return true; }
            if (stem == "pilgrim_bellbearer") { profile = PilgrimBellbearer; return true; }
            if (stem == "covetous_pilgrim") { profile = CovetousPilgrim; return true; }
            if (stem == "pilgrim_hulk") { profile = PilgrimHulk; return true; }
            profile = PilgrimGroveller;
            return false;
        }

        public struct MossGrottoProfile
        {
            public string DisplayName;
            public string SheetStem;
            public string SpritePath;       // Existing reference image path (DO NOT MODIFY/DELETE!)
            public string SpriteSheetPath;  // Existing sprite sheet path (DO NOT MODIFY/DELETE!)
            public bool IsFlying;

            // Vitals & Rewards
            public int MaxHealth;
            public int ContactDamage;
            public float ContactKnockback;
            public float KnockbackResistance;
            public float StaggerDuration;
            public int SilkEssenceReward;
            public int OrbDropCount;

            // Ground / Aerial Movement (60 FPS physics)
            public float PatrolSpeed;
            public float AggroSpeed;
            public float PatrolSpanX;
            public float HoverAmplitude;
            public float HoverFrequency;
            public float DetectRange;
            public float LoseTargetRange;
            public float AnchorReturnSpeed;

            // Specialized Moss Grotto Mechanics
            public float ProvokedAlertDuration;
            public float CalmDownDuration;
            public bool AllowEdgeHopOnCharge;

            public float PogoRecoilOffsetY;
            public float PogoRecoilDuration;
            public float TopPogoHurtboxRadius;
            public float LightRetaliationRange;
            public float LightRetaliationVertTolerance;
            public float LightRetaliationSpeed;
            public float LightRetaliationDuration;
            public float LightRetaliationCooldown;

            public AknidCarryType DefaultCarryType;
            public bool DropPayloadOnFirstHit;
            public bool DropPayloadOnDeath;
            public float EvadeDetectRange;
            public float EvadeSpeed;
            public float DiveNudgeRange;
            public float DiveNudgeSpeed;
            public float DiveNudgeDuration;
            public float DiveNudgeCooldown;

            // Sprite Animation Playback FPS (12-24 FPS)
            public float IdleFps;
            public float MoveFps;
            public float WindupFps;
            public float AttackFps;
            public float HurtFps;
            public float DeathFps;
        }

        // SPRITE_PATH: Assets/Art/Characters/Enemies/mossgrub.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/moss_grub_sheet.png)
        public static readonly MossGrottoProfile MossGrub = new MossGrottoProfile
        {
            DisplayName = "Mossgrub",
            SheetStem = "bellhart_mossgrub",
            SpritePath = "Assets/Art/Characters/Enemies/mossgrub.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/moss_grub_sheet.png",
            IsFlying = false,

            MaxHealth = 28,             // Low HP (2-3 standard hits)
            ContactDamage = 1,
            ContactKnockback = 6.0f,
            KnockbackResistance = 0.05f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 14,
            OrbDropCount = 2,

            PatrolSpeed = 2.0f,         // Calm passive crawl
            AggroSpeed = 5.4f,          // Fast provoked charge
            PatrolSpanX = 4.5f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,
            DetectRange = 6.2f,
            LoseTargetRange = 9.0f,
            AnchorReturnSpeed = 0f,

            ProvokedAlertDuration = 0.25f,  // Exact 0.25s surprised/angry telegraph on hit
            CalmDownDuration = 4.5f,        // Returns to PassivePatrol after undisturbed pause
            AllowEdgeHopOnCharge = false,

            PogoRecoilOffsetY = 0f,
            PogoRecoilDuration = 0f,
            TopPogoHurtboxRadius = 0f,
            LightRetaliationRange = 0f,
            LightRetaliationVertTolerance = 0f,
            LightRetaliationSpeed = 0f,
            LightRetaliationDuration = 0f,
            LightRetaliationCooldown = 0f,

            DefaultCarryType = AknidCarryType.None,
            DropPayloadOnFirstHit = false,
            DropPayloadOnDeath = false,
            EvadeDetectRange = 0f,
            EvadeSpeed = 0f,
            DiveNudgeRange = 0f,
            DiveNudgeSpeed = 0f,
            DiveNudgeDuration = 0f,
            DiveNudgeCooldown = 0f,

            IdleFps = 12f,              // 12-18 FPS hand-drawn playback
            MoveFps = 14f,
            WindupFps = 14f,
            AttackFps = 18f,
            HurtFps = 14f,
            DeathFps = 12f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/mossmir.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/mossmir_sheet.png)
        public static readonly MossGrottoProfile Mossmir = new MossGrottoProfile
        {
            DisplayName = "Mossmir",
            SheetStem = "bellhart_mossmir",
            SpritePath = "Assets/Art/Characters/Enemies/mossmir.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/mossmir_sheet.png",
            IsFlying = true,

            MaxHealth = 42,             // Medium-low HP (3-4 hits -> survives 2-3 chained pogos!)
            ContactDamage = 1,
            ContactKnockback = 6.0f,
            KnockbackResistance = 0.12f,
            StaggerDuration = 0.14f,
            SilkEssenceReward = 18,
            OrbDropCount = 2,

            PatrolSpeed = 1.15f,
            AggroSpeed = 5.8f,
            PatrolSpanX = 1.45f,        // Short predictable horizontal sway around anchor
            HoverAmplitude = 0.32f,
            HoverFrequency = 2.4f,
            DetectRange = 4.2f,
            LoseTargetRange = 7.5f,
            AnchorReturnSpeed = 4.2f,

            ProvokedAlertDuration = 0f,
            CalmDownDuration = 0f,
            AllowEdgeHopOnCharge = false,

            PogoRecoilOffsetY = 0.52f,  // Gentle downward elastic dip on pogo
            PogoRecoilDuration = 0.18f, // ~0.18s recoil then smooth anchor return
            TopPogoHurtboxRadius = 0.68f, // Widened responsive top pogo hurtbox
            LightRetaliationRange = 3.8f,
            LightRetaliationVertTolerance = 0.95f,
            LightRetaliationSpeed = 5.8f,
            LightRetaliationDuration = 0.24f,
            LightRetaliationCooldown = 2.2f,

            DefaultCarryType = AknidCarryType.None,
            DropPayloadOnFirstHit = false,
            DropPayloadOnDeath = false,
            EvadeDetectRange = 0f,
            EvadeSpeed = 0f,
            DiveNudgeRange = 0f,
            DiveNudgeSpeed = 0f,
            DiveNudgeDuration = 0f,
            DiveNudgeCooldown = 0f,

            IdleFps = 18f,              // 16-22 FPS hand-drawn playback
            MoveFps = 18f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 16f,
            DeathFps = 16f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/aknid.png (Hand-Drawn 3x6 Sheet: Assets/Sprites/Enemies/aknid_sheet.png)
        public static readonly MossGrottoProfile Aknid = new MossGrottoProfile
        {
            DisplayName = "Aknid",
            SheetStem = "aknid",
            SpritePath = "Assets/Art/Characters/Enemies/aknid.png",
            SpriteSheetPath = "Assets/Sprites/Enemies/aknid_sheet.png",
            IsFlying = true,

            MaxHealth = 28,             // Low HP (2-3 hits), secondary pogo target
            ContactDamage = 1,
            ContactKnockback = 6.0f,
            KnockbackResistance = 0.08f,
            StaggerDuration = 0.16f,
            SilkEssenceReward = 18,
            OrbDropCount = 2,

            PatrolSpeed = 2.6f,
            AggroSpeed = 4.8f,
            PatrolSpanX = 3.2f,         // Wider dynamic flight path than Mossmir
            HoverAmplitude = 0.55f,
            HoverFrequency = 2.8f,
            DetectRange = 8.4f,
            LoseTargetRange = 12.5f,
            AnchorReturnSpeed = 3.6f,

            ProvokedAlertDuration = 0f,
            CalmDownDuration = 0f,
            AllowEdgeHopOnCharge = false,

            PogoRecoilOffsetY = 0.44f,
            PogoRecoilDuration = 0.16f,
            TopPogoHurtboxRadius = 0.62f,
            LightRetaliationRange = 0f,
            LightRetaliationVertTolerance = 0f,
            LightRetaliationSpeed = 0f,
            LightRetaliationDuration = 0f,
            LightRetaliationCooldown = 0f,

            DefaultCarryType = AknidCarryType.None,
            DropPayloadOnFirstHit = true,
            DropPayloadOnDeath = true,
            EvadeDetectRange = 4.6f,    // Subtly evades/weaves when player approaches
            EvadeSpeed = 3.1f,
            DiveNudgeRange = 4.8f,      // Light DiveNudge ONLY when CarryType == None
            DiveNudgeSpeed = 7.6f,
            DiveNudgeDuration = 0.28f,
            DiveNudgeCooldown = 2.1f,

            IdleFps = 18f,              // 16-22 FPS hand-drawn playback
            MoveFps = 20f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 16f,
            DeathFps = 16f
        };

        public static bool TryGetMossByStem(string stem, out MossGrottoProfile profile)
        {
            if (stem == "bellhart_mossgrub" || stem == "mossgrub" || stem == "moss_grub") { profile = MossGrub; return true; }
            if (stem == "bellhart_mossmir" || stem == "mossmir") { profile = Mossmir; return true; }
            if (stem == "aknid") { profile = Aknid; return true; }
            profile = Mossmir;
            return false;
        }

        public struct Profile
        {
            public ChoralArchetype Archetype;
            public string DisplayName;
            public string SheetStem;
            public string SpritePath;       // Existing reference image path (DO NOT MODIFY/DELETE!)
            public string SpriteSheetPath;  // Existing 12x4 sprite sheet path (DO NOT MODIFY/DELETE!)
            public bool IsElite;
            public bool IsFlying;

            // Vitals, Damage & Rewards
            public int MaxHealth;
            public int ContactDamage;
            public int PrimaryAttackDamage;
            public int SecondaryAttackDamage;
            public float PrimaryKnockback;
            public float SecondaryKnockback;
            public float ContactKnockback;
            public float KnockbackResistance;
            public float StaggerDuration;
            public int SilkEssenceReward;
            public int OrbDropCount;

            // Movement & Detection (60 FPS physics)
            public float PatrolSpeed;
            public float ChaseSpeed;
            public float PatrolDistance;
            public float IdlePauseDuration;
            public float DetectRange;
            public float LoseTargetRange;
            public float AttackRange;
            public float IdealMinRange;
            public float IdealMaxRange;
            public float LungeSpeed;
            public float RetreatHopSpeed;
            public float ProjectileSpeed;
            public float MaxChargeDistance;
            public float HoverAmplitude;
            public float HoverFrequency;

            // Timings & Cadence
            public float TelegraphDuration;
            public float SecondaryTelegraphDuration;
            public float AttackActiveDuration;
            public float RecoveryDuration;
            public float AttackCooldown;
            public float GuardDuration;
            public float OverheadPauseMin;
            public float OverheadPauseMax;
            public float SkidDuration;
            public float LockOnDuration;

            // Hitbox / Hurtbox / FrontBlockBox Geometry
            public Vector2 BodyColliderSize;
            public Vector2 BodyHurtboxSize;
            public bool HasFrontBlockBox;
            public Vector2 FrontBlockBoxOffset;
            public Vector2 FrontBlockBoxSize;
            public Vector2 PrimaryAttackOffset;
            public Vector2 PrimaryAttackSize;
            public Vector2 SecondaryAttackOffset;
            public Vector2 SecondaryAttackSize;

            // Sprite Animation Playback FPS (12-24 FPS Silksong cadence)
            public float IdleFps;
            public float MoveFps;
            public float WindupFps;
            public float AttackFps;
            public float HurtFps;
            public float DeathFps;
        }

        // SPRITE_PATH: Assets/Art/Characters/Enemies/grand_reed.png (Sheet: Assets/Art/Characters/Enemies/Sheets/grand_reed_Sheet.png)
        public static readonly Profile GrandReed = new Profile
        {
            Archetype = ChoralArchetype.GrandReed,
            DisplayName = "Grand Reed",
            SheetStem = "grand_reed",
            SpritePath = "Assets/Art/Characters/Enemies/grand_reed.png",
            SpriteSheetPath = "Assets/Art/Characters/Enemies/Sheets/grand_reed_Sheet.png",
            IsElite = true,
            IsFlying = false,

            MaxHealth = 110,
            ContactDamage = 1,          // Body contact = 1 damage
            PrimaryAttackDamage = 2,    // WideHeavySlash = 2 damage
            SecondaryAttackDamage = 2,  // DoubleThrustCombo = 2 damage
            PrimaryKnockback = 10.6f,
            SecondaryKnockback = 11.2f,
            ContactKnockback = 7.4f,
            KnockbackResistance = 0.68f,
            StaggerDuration = 0.14f,
            SilkEssenceReward = 110,    // 4x-5x standard reward
            OrbDropCount = 10,

            PatrolSpeed = 1.85f,
            ChaseSpeed = 3.25f,         // Dignified heavy Citadel march
            PatrolDistance = 4.6f,
            IdlePauseDuration = 0.70f,
            DetectRange = 10.8f,
            LoseTargetRange = 15.5f,
            AttackRange = 4.15f,
            IdealMinRange = 1.8f,
            IdealMaxRange = 4.15f,
            LungeSpeed = 11.4f,
            RetreatHopSpeed = 5.4f,
            ProjectileSpeed = 0f,
            MaxChargeDistance = 0f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            TelegraphDuration = 0.45f,          // Unmistakable 0.45s telegraph
            SecondaryTelegraphDuration = 0.45f,
            AttackActiveDuration = 0.34f,
            RecoveryDuration = 0.54f,
            AttackCooldown = 0.82f,
            GuardDuration = 0.78f,
            OverheadPauseMin = 0f,
            OverheadPauseMax = 0f,
            SkidDuration = 0f,
            LockOnDuration = 0f,

            BodyColliderSize = new Vector2(1.18f, 2.38f),
            BodyHurtboxSize = new Vector2(1.36f, 2.55f),
            HasFrontBlockBox = true,
            FrontBlockBoxOffset = new Vector2(0.86f, 1.05f),
            FrontBlockBoxSize = new Vector2(0.82f, 1.65f),
            PrimaryAttackOffset = new Vector2(1.45f, 1.15f),
            PrimaryAttackSize = new Vector2(3.95f, 2.35f),
            SecondaryAttackOffset = new Vector2(1.75f, 1.02f),
            SecondaryAttackSize = new Vector2(4.05f, 1.25f),

            IdleFps = 14f,
            MoveFps = 16f,
            WindupFps = 14f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/reed.png (Sheet: Assets/Art/Characters/Enemies/Sheets/reed_Sheet.png)
        public static readonly Profile Reed = new Profile
        {
            Archetype = ChoralArchetype.Reed,
            DisplayName = "Reed",
            SheetStem = "reed",
            SpritePath = "Assets/Art/Characters/Enemies/reed.png",
            SpriteSheetPath = "Assets/Art/Characters/Enemies/Sheets/reed_Sheet.png",
            IsElite = false,
            IsFlying = false,

            MaxHealth = 48,
            ContactDamage = 1,
            PrimaryAttackDamage = 1,    // QuickSlash
            SecondaryAttackDamage = 1,  // ForwardLunge
            PrimaryKnockback = 7.0f,
            SecondaryKnockback = 8.2f,
            ContactKnockback = 6.4f,
            KnockbackResistance = 0.18f,
            StaggerDuration = 0.22f,
            SilkEssenceReward = 26,
            OrbDropCount = 3,

            PatrolSpeed = 3.2f,
            ChaseSpeed = 5.8f,
            PatrolDistance = 4.2f,
            IdlePauseDuration = 0.25f,
            DetectRange = 9.4f,
            LoseTargetRange = 13.8f,
            AttackRange = 3.45f,
            IdealMinRange = 1.6f,
            IdealMaxRange = 3.45f,
            LungeSpeed = 12.2f,
            RetreatHopSpeed = 8.4f,     // Nimble duelist Backstep
            ProjectileSpeed = 0f,
            MaxChargeDistance = 0f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            TelegraphDuration = 0.24f,
            SecondaryTelegraphDuration = 0.30f,
            AttackActiveDuration = 0.24f,
            RecoveryDuration = 0.32f,
            AttackCooldown = 0.50f,
            GuardDuration = 0.72f,      // Clear BlockStance window
            OverheadPauseMin = 0f,
            OverheadPauseMax = 0f,
            SkidDuration = 0f,
            LockOnDuration = 0f,

            BodyColliderSize = new Vector2(0.95f, 1.75f),
            BodyHurtboxSize = new Vector2(1.12f, 1.92f),
            HasFrontBlockBox = true,
            FrontBlockBoxOffset = new Vector2(0.68f, 0.86f),
            FrontBlockBoxSize = new Vector2(0.72f, 1.32f),
            PrimaryAttackOffset = new Vector2(1.12f, 0.90f),
            PrimaryAttackSize = new Vector2(2.45f, 1.28f),
            SecondaryAttackOffset = new Vector2(1.42f, 0.92f),
            SecondaryAttackSize = new Vector2(2.85f, 1.15f),

            IdleFps = 16f,
            MoveFps = 20f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/choristor.png (Sheet: Assets/Art/Characters/Enemies/Sheets/choristor_Sheet.png)
        public static readonly Profile Choristor = new Profile
        {
            Archetype = ChoralArchetype.Choristor,
            DisplayName = "Choristor",
            SheetStem = "choristor",
            SpritePath = "Assets/Art/Characters/Enemies/choristor.png",
            SpriteSheetPath = "Assets/Art/Characters/Enemies/Sheets/choristor_Sheet.png",
            IsElite = false,
            IsFlying = false,

            MaxHealth = 38,
            ContactDamage = 1,
            PrimaryAttackDamage = 1,    // CastThread projectile
            SecondaryAttackDamage = 1,
            PrimaryKnockback = 6.6f,
            SecondaryKnockback = 6.4f,
            ContactKnockback = 6.0f,
            KnockbackResistance = 0.15f,
            StaggerDuration = 0.22f,
            SilkEssenceReward = 22,
            OrbDropCount = 3,

            PatrolSpeed = 2.5f,
            ChaseSpeed = 4.4f,
            PatrolDistance = 3.8f,
            IdlePauseDuration = 0.30f,
            DetectRange = 11.2f,
            LoseTargetRange = 15.0f,
            AttackRange = 10.6f,        // Mid-long range thread caster
            IdealMinRange = 3.65f,      // Too close -> RetreatHop!
            IdealMaxRange = 10.6f,
            LungeSpeed = 4.0f,
            RetreatHopSpeed = 8.6f,
            ProjectileSpeed = 15.2f,    // Crisp white silk thread launch
            MaxChargeDistance = 0f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            TelegraphDuration = 0.35f,  // Exact 0.35s ThreadWindup
            SecondaryTelegraphDuration = 0.35f,
            AttackActiveDuration = 0.22f,
            RecoveryDuration = 0.34f,
            AttackCooldown = 0.98f,
            GuardDuration = 0f,
            OverheadPauseMin = 0f,
            OverheadPauseMax = 0f,
            SkidDuration = 0f,
            LockOnDuration = 0f,

            BodyColliderSize = new Vector2(0.94f, 1.48f),
            BodyHurtboxSize = new Vector2(1.12f, 1.62f),
            HasFrontBlockBox = false,
            FrontBlockBoxOffset = Vector2.zero,
            FrontBlockBoxSize = Vector2.zero,
            PrimaryAttackOffset = new Vector2(0.82f, 0.92f),
            PrimaryAttackSize = new Vector2(1.45f, 0.90f),
            SecondaryAttackOffset = Vector2.zero,
            SecondaryAttackSize = Vector2.zero,

            IdleFps = 14f,
            MoveFps = 18f,
            WindupFps = 16f,
            AttackFps = 20f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/choir_hornhead.png (Sheet: Assets/Art/Characters/Enemies/Sheets/choir_hornhead_Sheet.png)
        public static readonly Profile ChoirHornhead = new Profile
        {
            Archetype = ChoralArchetype.ChoirHornhead,
            DisplayName = "Choir Hornhead",
            SheetStem = "choir_hornhead",
            SpritePath = "Assets/Art/Characters/Enemies/choir_hornhead.png",
            SpriteSheetPath = "Assets/Art/Characters/Enemies/Sheets/choir_hornhead_Sheet.png",
            IsElite = false,
            IsFlying = false,

            MaxHealth = 50,
            ContactDamage = 1,
            PrimaryAttackDamage = 1,    // ChargeDash horn strike
            SecondaryAttackDamage = 1,
            PrimaryKnockback = 8.8f,
            SecondaryKnockback = 8.0f,
            ContactKnockback = 7.2f,
            KnockbackResistance = 0.48f,
            StaggerDuration = 0.18f,
            SilkEssenceReward = 24,
            OrbDropCount = 3,

            PatrolSpeed = 2.15f,
            ChaseSpeed = 3.2f,
            PatrolDistance = 4.2f,
            IdlePauseDuration = 0.35f,
            DetectRange = 9.5f,
            LoseTargetRange = 13.8f,
            AttackRange = 8.2f,         // Triggers ChargeWindup when horizontally aligned in detectRange
            IdealMinRange = 0f,
            IdealMaxRange = 8.2f,
            LungeSpeed = 12.8f,         // High-speed bull ChargeDash
            RetreatHopSpeed = 0f,
            ProjectileSpeed = 0f,
            MaxChargeDistance = 7.8f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            TelegraphDuration = 0.42f,  // Horn lowering + foot dust
            SecondaryTelegraphDuration = 0.42f,
            AttackActiveDuration = 0.68f,
            RecoveryDuration = 0.34f,
            AttackCooldown = 0.92f,
            GuardDuration = 0f,
            OverheadPauseMin = 0.62f,   // PlayerOverhead pause 0.6-0.9s for smooth diagonal pogo chains
            OverheadPauseMax = 0.88f,
            SkidDuration = 0.46f,       // Braking SkidStop
            LockOnDuration = 0f,

            BodyColliderSize = new Vector2(1.02f, 1.28f),
            BodyHurtboxSize = new Vector2(1.24f, 1.44f),
            HasFrontBlockBox = false,
            FrontBlockBoxOffset = Vector2.zero,
            FrontBlockBoxSize = Vector2.zero,
            PrimaryAttackOffset = new Vector2(0.86f, 0.72f),
            PrimaryAttackSize = new Vector2(1.85f, 1.22f),
            SecondaryAttackOffset = Vector2.zero,
            SecondaryAttackSize = Vector2.zero,

            IdleFps = 14f,
            MoveFps = 18f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/choir_elder.png (Sheet: Assets/Art/Characters/Enemies/Sheets/choir_elder_Sheet.png)
        public static readonly Profile ChoirElder = new Profile
        {
            Archetype = ChoralArchetype.ChoirElder,
            DisplayName = "Choir Elder",
            SheetStem = "choir_elder",
            SpritePath = "Assets/Art/Characters/Enemies/choir_elder.png",
            SpriteSheetPath = "Assets/Art/Characters/Enemies/Sheets/choir_elder_Sheet.png",
            IsElite = true,
            IsFlying = false,

            MaxHealth = 84,
            ContactDamage = 1,
            PrimaryAttackDamage = 2,    // HeavySwing = 2 damage
            SecondaryAttackDamage = 2,  // StepStrike = 2 damage
            PrimaryKnockback = 11.4f,
            SecondaryKnockback = 10.6f,
            ContactKnockback = 8.4f,
            KnockbackResistance = 0.78f,
            StaggerDuration = 0.12f,
            SilkEssenceReward = 48,
            OrbDropCount = 5,

            PatrolSpeed = 1.45f,        // Slow deliberate patrol
            ChaseSpeed = 2.15f,
            PatrolDistance = 3.8f,
            IdlePauseDuration = 0.55f,
            DetectRange = 9.4f,
            LoseTargetRange = 13.8f,
            AttackRange = 5.15f,        // StepStrike closes mid-range; HeavySwing triggers close
            IdealMinRange = 2.65f,
            IdealMaxRange = 5.15f,
            LungeSpeed = 8.8f,          // StepStrike forward burst
            RetreatHopSpeed = 0f,
            ProjectileSpeed = 0f,
            MaxChargeDistance = 0f,
            HoverAmplitude = 0f,
            HoverFrequency = 0f,

            TelegraphDuration = 0.52f,          // HeavySwing slow clear telegraph
            SecondaryTelegraphDuration = 0.42f, // StepStrike telegraph
            AttackActiveDuration = 0.34f,
            RecoveryDuration = 0.66f,
            AttackCooldown = 1.15f,
            GuardDuration = 0f,
            OverheadPauseMin = 0f,
            OverheadPauseMax = 0f,
            SkidDuration = 0f,
            LockOnDuration = 0f,

            BodyColliderSize = new Vector2(1.36f, 1.88f),
            BodyHurtboxSize = new Vector2(1.54f, 2.02f),
            HasFrontBlockBox = false,
            FrontBlockBoxOffset = Vector2.zero,
            FrontBlockBoxSize = Vector2.zero,
            PrimaryAttackOffset = new Vector2(1.25f, 0.96f),
            PrimaryAttackSize = new Vector2(2.85f, 1.85f),
            SecondaryAttackOffset = new Vector2(1.35f, 0.92f),
            SecondaryAttackSize = new Vector2(2.65f, 1.55f),

            IdleFps = 12f,
            MoveFps = 14f,
            WindupFps = 13f,
            AttackFps = 18f,
            HurtFps = 14f,
            DeathFps = 12f
        };

        // SPRITE_PATH: Assets/Art/Characters/Enemies/choir_flyer.png (Sheet: Assets/Art/Characters/Enemies/Sheets/choir_flyer_Sheet.png)
        public static readonly Profile ChoirFlyer = new Profile
        {
            Archetype = ChoralArchetype.ChoirFlyer,
            DisplayName = "Choir Flyer",
            SheetStem = "choir_flyer",
            SpritePath = "Assets/Art/Characters/Enemies/choir_flyer.png",
            SpriteSheetPath = "Assets/Art/Characters/Enemies/Sheets/choir_flyer_Sheet.png",
            IsElite = false,
            IsFlying = true,

            MaxHealth = 28,             // Low HP (2-3 standard 12-dmg player hits)
            ContactDamage = 1,
            PrimaryAttackDamage = 1,    // SwoopDive
            SecondaryAttackDamage = 1,
            PrimaryKnockback = 6.4f,
            SecondaryKnockback = 6.0f,
            ContactKnockback = 5.8f,
            KnockbackResistance = 0.06f, // Super light & aerial pogo friendly!
            StaggerDuration = 0.24f,
            SilkEssenceReward = 18,
            OrbDropCount = 2,

            PatrolSpeed = 2.8f,
            ChaseSpeed = 4.4f,
            PatrolDistance = 3.4f,
            IdlePauseDuration = 0.20f,
            DetectRange = 9.8f,
            LoseTargetRange = 14.5f,
            AttackRange = 8.6f,         // Direct SwoopDive once player enters detect radius
            IdealMinRange = 0f,
            IdealMaxRange = 8.6f,
            LungeSpeed = 13.4f,         // Fast direct SwoopDive velocity
            RetreatHopSpeed = 0f,
            ProjectileSpeed = 0f,
            MaxChargeDistance = 9.2f,
            HoverAmplitude = 0.36f,
            HoverFrequency = 3.0f,

            TelegraphDuration = 0.25f,  // Exact 0.25s LockOn
            SecondaryTelegraphDuration = 0.25f,
            AttackActiveDuration = 0.56f,
            RecoveryDuration = 0.65f,   // RecoverClimb back to safe hover height
            AttackCooldown = 0.85f,
            GuardDuration = 0f,
            OverheadPauseMin = 0f,
            OverheadPauseMax = 0f,
            SkidDuration = 0f,
            LockOnDuration = 0.25f,

            BodyColliderSize = new Vector2(0.96f, 0.96f),
            BodyHurtboxSize = new Vector2(1.24f, 1.24f),
            HasFrontBlockBox = false,
            FrontBlockBoxOffset = Vector2.zero,
            FrontBlockBoxSize = Vector2.zero,
            PrimaryAttackOffset = new Vector2(0.42f, 0f),
            PrimaryAttackSize = new Vector2(1.48f, 1.22f),
            SecondaryAttackOffset = Vector2.zero,
            SecondaryAttackSize = Vector2.zero,

            IdleFps = 16f,
            MoveFps = 20f,
            WindupFps = 16f,
            AttackFps = 22f,
            HurtFps = 16f,
            DeathFps = 14f
        };

        public static Profile Get(ChoralArchetype archetype)
        {
            switch (archetype)
            {
                case ChoralArchetype.GrandReed: return GrandReed;
                case ChoralArchetype.Reed: return Reed;
                case ChoralArchetype.Choristor: return Choristor;
                case ChoralArchetype.ChoirHornhead: return ChoirHornhead;
                case ChoralArchetype.ChoirElder: return ChoirElder;
                case ChoralArchetype.ChoirFlyer: return ChoirFlyer;
                default: return Choristor;
            }
        }

        public static bool TryGetByStem(string stem, out Profile profile)
        {
            if (stem == "grand_reed") { profile = GrandReed; return true; }
            if (stem == "reed") { profile = Reed; return true; }
            if (stem == "choristor") { profile = Choristor; return true; }
            if (stem == "choir_hornhead") { profile = ChoirHornhead; return true; }
            if (stem == "choir_elder") { profile = ChoirElder; return true; }
            if (stem == "choir_flyer") { profile = ChoirFlyer; return true; }
            profile = Choristor;
            return false;
        }
    }
}
