using UnityEngine;

namespace SilkenGrotto
{
    public enum CrestType
    {
        Hunter = 0,
        Reaper = 1,
        Witch = 2
    }

    /// <summary>
    /// Immutable combat & Bind profile defining each selectable Silksong Combat Crest:
    /// - Hunter (default balanced needle crest)
    /// - Reaper (heavy wide scythe cleave + Reaper's Harvest bonus Silk post-Bind)
    /// - Witch  (long-range thorny silk whip + Root Leech offensive lifesteal Bind)
    /// </summary>
    public struct CrestProfile
    {
        public CrestType Type;
        public string DisplayName;
        public string ShortBadge;
        public string Description;
        public Color AccentColor;
        public Color SlashTint;
        public float DamageMultiplier;
        public float DurationMultiplier;
        public float CooldownMultiplier;
        public float HitboxWidthMultiplier;
        public float HitboxHeightMultiplier;
        public float OffsetXMultiplier;
        public float KnockbackMultiplier;
        public float SlashScaleMultiplier;
        public int CustomMeleeComboStep; // 0 = standard Hunter combo steps 1..3; 11 = ReaperSlash; 12 = WitchWhip
    }

    /// <summary>
    /// Runtime Combat Crest selector & buff tracker (Hunter / Reaper / Witch).
    /// </summary>
    public static class PlayerCrestSystem
    {
        private static CrestType _activeCrest = CrestType.Hunter;
        private static float _reaperHarvestEndTime = -10f;

        public static CrestType ActiveCrest { get { return _activeCrest; } }
        public static bool IsReaperHarvestActive { get { return _activeCrest == CrestType.Reaper && Time.time < _reaperHarvestEndTime; } }
        public static float ReaperHarvestRemaining { get { return Mathf.Max(0f, _reaperHarvestEndTime - Time.time); } }

        public static CrestProfile GetProfile(CrestType type)
        {
            switch (type)
            {
                case CrestType.Reaper:
                    return new CrestProfile
                    {
                        Type = CrestType.Reaper,
                        DisplayName = "Reaper Crest",
                        ShortBadge = "REAPER",
                        Description = "Heavy wide scythe sweeps. Bind triggers Reaper's Harvest (+14 bonus Silk per melee hit for 10s).",
                        AccentColor = new Color(0.98f, 0.78f, 0.34f, 1f),
                        SlashTint = new Color(1.0f, 0.88f, 0.56f, 1f),
                        DamageMultiplier = 1.24f,
                        DurationMultiplier = 1.18f,
                        CooldownMultiplier = 1.12f,
                        HitboxWidthMultiplier = 1.48f,
                        HitboxHeightMultiplier = 1.32f,
                        OffsetXMultiplier = 1.18f,
                        KnockbackMultiplier = 1.28f,
                        SlashScaleMultiplier = 1.36f,
                        CustomMeleeComboStep = 11
                    };

                case CrestType.Witch:
                    return new CrestProfile
                    {
                        Type = CrestType.Witch,
                        DisplayName = "Witch Crest",
                        ShortBadge = "WITCH",
                        Description = "Long-range thorny silk whip lashes. Bind erupts Root Leech tendrils (damages nearby foes to steal up to +3 Masks).",
                        AccentColor = new Color(0.46f, 0.94f, 0.68f, 1f),
                        SlashTint = new Color(0.72f, 1.0f, 0.84f, 1f),
                        DamageMultiplier = 1.08f,
                        DurationMultiplier = 1.06f,
                        CooldownMultiplier = 1.04f,
                        HitboxWidthMultiplier = 1.56f,
                        HitboxHeightMultiplier = 1.10f,
                        OffsetXMultiplier = 1.32f,
                        KnockbackMultiplier = 1.06f,
                        SlashScaleMultiplier = 1.28f,
                        CustomMeleeComboStep = 12
                    };

                default:
                    return new CrestProfile
                    {
                        Type = CrestType.Hunter,
                        DisplayName = "Hunter Crest",
                        ShortBadge = "HUNTER",
                        Description = "Balanced, agile needle cadence. Bind restores +2 Masks cleanly on ground or mid-air.",
                        AccentColor = new Color(0.92f, 0.88f, 0.80f, 1f),
                        SlashTint = Color.white,
                        DamageMultiplier = 1.0f,
                        DurationMultiplier = 1.0f,
                        CooldownMultiplier = 1.0f,
                        HitboxWidthMultiplier = 1.0f,
                        HitboxHeightMultiplier = 1.0f,
                        OffsetXMultiplier = 1.0f,
                        KnockbackMultiplier = 1.0f,
                        SlashScaleMultiplier = 1.0f,
                        CustomMeleeComboStep = 0
                    };
            }
        }

        public static CrestProfile ActiveProfile { get { return GetProfile(_activeCrest); } }

        public static void SetActiveCrest(CrestType next, bool notify = true)
        {
            _activeCrest = next;
            if (_activeCrest != CrestType.Reaper)
            {
                _reaperHarvestEndTime = -10f;
            }
            GameEvents.RaiseCrestChanged(_activeCrest);
            if (notify)
            {
                CrestProfile p = GetProfile(_activeCrest);
                GameEvents.RaiseNotification("CREST EQUIPPED: " + p.DisplayName.ToUpper(), p.Description);
            }
        }

        public static CrestType CycleNextCrest()
        {
            int nextIdx = ((int)_activeCrest + 1) % 3;
            SetActiveCrest((CrestType)nextIdx, true);
            return _activeCrest;
        }

        public static void ActivateReaperHarvest(float durationSeconds = 10.0f)
        {
            if (_activeCrest != CrestType.Reaper) return;
            _reaperHarvestEndTime = Time.time + Mathf.Max(2f, durationSeconds);
        }

        public static void ResetToDefault()
        {
            _activeCrest = CrestType.Hunter;
            _reaperHarvestEndTime = -10f;
            GameEvents.RaiseCrestChanged(_activeCrest);
        }

        public static void ImportFromSave(string savedCrestName)
        {
            if (string.IsNullOrEmpty(savedCrestName))
            {
                ResetToDefault();
                return;
            }
            try
            {
                CrestType parsed = (CrestType)System.Enum.Parse(typeof(CrestType), savedCrestName, true);
                SetActiveCrest(parsed, false);
            }
            catch
            {
                ResetToDefault();
            }
        }
    }
}
