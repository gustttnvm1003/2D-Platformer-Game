using UnityEngine;

namespace SilkenGrotto
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        UpgradeMenu,
        InventoryMenu,
        GameOver
    }

    public enum SkillType
    {
        Dash,
        DoubleJump,
        AirDash,
        SilkSpear,
        EnhancedAttack,
        ThreadGuard
    }

    public enum UpgradeType
    {
        MaxHealth,
        AttackDamage,
        MovementSpeed,
        JumpControl,
        MaxSilkEnergy,
        Defense
    }

    public enum AttackDirection
    {
        Forward,
        Up,
        Down,
        AirForward
    }

    public static class GameConstants
    {
        // Scene names
        public const string SceneMainMenu = "MainMenu";
        public const string SceneGameplay = "MossGrotto";

        // Tags
        public const string TagPlayer = "Player";
        public const string TagEnemy = "Enemy";
        public const string TagCheckpoint = "Checkpoint";
        public const string TagHazard = "Hazard";
        public const string TagInteractable = "Interactable";
        public const string TagSecretArea = "SecretArea";
        public const string TagBreakable = "Breakable";

        // Layers (matching TagManager.asset)
        public const int LayerGround = 6;
        public const int LayerOneWayPlatform = 7;
        public const int LayerPlayer = 8;
        public const int LayerEnemy = 9;
        public const int LayerHazard = 10;
        public const int LayerInteractable = 11;
        public const int LayerBreakableWall = 12;
        public const int LayerPlayerAttack = 13;
        public const int LayerEnemyAttack = 14;
        public const int LayerDestructibleFoliage = 15;

        public const string LayerNameGround = "Ground";
        public const string LayerNameOneWayPlatform = "OneWayPlatform";
        public const string LayerNamePlayer = "Player";
        public const string LayerNameEnemy = "Enemy";
        public const string LayerNameHazard = "Hazard";
        public const string LayerNameInteractable = "Interactable";
        public const string LayerNameBreakableWall = "BreakableWall";
        public const string LayerNameDestructibleFoliage = "DestructibleFoliage";

        // Sorting Layers
        public const string SortFarBackground = "FarBackground";
        public const string SortMidBackground = "MidBackground";
        public const string SortNearBackground = "NearBackground";
        public const string SortEnvironment = "Environment";
        public const string SortInteractables = "Interactables";
        public const string SortEnemies = "Enemies";
        public const string SortPlayer = "Player";
        public const string SortVFX = "VFX";
        public const string SortForeground = "Foreground";

        // Player Animator Parameter Hashes
        public static readonly int AnimSpeed = Animator.StringToHash("Speed");
        public static readonly int AnimIsRunning = Animator.StringToHash("IsRunning");
        public static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int AnimVelocityY = Animator.StringToHash("VelocityY");
        public static readonly int AnimIsDashing = Animator.StringToHash("IsDashing");
        public static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");
        public static readonly int AnimComboStep = Animator.StringToHash("ComboStep");
        public static readonly int AnimIsAirAttacking = Animator.StringToHash("IsAirAttacking");
        public static readonly int AnimIsHurt = Animator.StringToHash("IsHurt");
        public static readonly int AnimIsDead = Animator.StringToHash("IsDead");
        public static readonly int AnimIsSitting = Animator.StringToHash("IsSitting");
        public static readonly int AnimIsWallSliding = Animator.StringToHash("IsWallSliding");
        public static readonly int AnimIdleVariant = Animator.StringToHash("IdleVariant");
        public static readonly int AnimTriggerLand = Animator.StringToHash("Land");
        public static readonly int AnimTriggerDoubleJump = Animator.StringToHash("DoubleJump");
        public static readonly int AnimTriggerWallJump = Animator.StringToHash("WallJump");
        public static readonly int AnimTriggerAttack = Animator.StringToHash("Attack");
        public static readonly int AnimTriggerSitDown = Animator.StringToHash("SitDown");
        public static readonly int AnimTriggerGraceIdle = Animator.StringToHash("GraceIdle");
        public static readonly int AnimIsMantling = Animator.StringToHash("IsMantling");
        public static readonly int AnimIsHealing = Animator.StringToHash("IsHealing");
        public static readonly int AnimIsAirDashing = Animator.StringToHash("IsAirDashing");
        public static readonly int AnimIsDownwardDashing = Animator.StringToHash("IsDownwardDashing");
        public static readonly int AnimIsHeroLanding = Animator.StringToHash("IsHeroLanding");
        public static readonly int AnimTriggerRunStart = Animator.StringToHash("RunStart");
        public static readonly int AnimTriggerLedgeSomersault = Animator.StringToHash("LedgeSomersault");
        public static readonly int AnimTriggerRestAlert = Animator.StringToHash("RestAlert");
        public static readonly int AnimTriggerRestJumpOff = Animator.StringToHash("RestJumpOff");
        public static readonly int AnimIsGliding = Animator.StringToHash("IsGliding");
        public static readonly int AnimIsSkidding = Animator.StringToHash("IsSkidding");
        public static readonly int AnimIsParrying = Animator.StringToHash("IsParrying");
        public static readonly int AnimIsTaunting = Animator.StringToHash("IsTaunting");
        public static readonly int AnimIsHarpooning = Animator.StringToHash("IsHarpooning");
        public static readonly int AnimIsSilksoaring = Animator.StringToHash("IsSilksoaring");
        public static readonly int AnimIsPlayingNeedolin = Animator.StringToHash("IsPlayingNeedolin");
        public static readonly int AnimIsWitchBind = Animator.StringToHash("IsWitchBind");

        // Enemy Animator Parameter Hashes
        public static readonly int EnemyAnimSpeed = Animator.StringToHash("Speed");
        public static readonly int EnemyAnimIsWindup = Animator.StringToHash("IsWindup");
        public static readonly int EnemyAnimIsAttacking = Animator.StringToHash("IsAttacking");
        public static readonly int EnemyAnimIsHurt = Animator.StringToHash("IsHurt");
        public static readonly int EnemyAnimIsDead = Animator.StringToHash("IsDead");
        public static readonly int EnemyAnimIsTurning = Animator.StringToHash("IsTurning");
        public static readonly int EnemyAnimIsCharging = Animator.StringToHash("IsCharging");

        // PlayerPrefs keys for settings
        public const string PrefMasterVolume = "SG_MasterVolume";
        public const string PrefMusicVolume = "SG_MusicVolume";
        public const string PrefSfxVolume = "SG_SfxVolume";
        public const string PrefScreenShake = "SG_ScreenShake";
        public const string PrefFullscreen = "SG_Fullscreen";
    }
}
