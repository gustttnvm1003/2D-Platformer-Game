using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Manages ScriptableObject-driven player abilities: unlock states, cooldown timers,
    /// Silk resource validation, and HUD cooldown broadcasts.
    /// Locked skills can never be activated.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        [Header("Configured Skills (ScriptableObjects)")]
        [SerializeField] private List<SkillData> allSkills = new List<SkillData>();

        private readonly Dictionary<SkillType, SkillData> _skillLookup = new Dictionary<SkillType, SkillData>();
        private readonly HashSet<SkillType> _unlockedSkills = new HashSet<SkillType>();
        private readonly Dictionary<SkillType, float> _cooldownTimers = new Dictionary<SkillType, float>();

        public IList<SkillData> AllSkills { get { return allSkills; } }

        private void Awake()
        {
            RebuildLookup();
            ResetToDefaults();
        }

        public void SetSkillCatalog(List<SkillData> catalog)
        {
            allSkills = catalog != null ? catalog : new List<SkillData>();
            RebuildLookup();
            ResetToDefaults();
        }

        private void RebuildLookup()
        {
            _skillLookup.Clear();
            _cooldownTimers.Clear();
            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillData s = allSkills[i];
                if (s == null) continue;
                _skillLookup[s.skillType] = s;
                _cooldownTimers[s.skillType] = 0f;
            }
        }

        public void ResetToDefaults()
        {
            _unlockedSkills.Clear();
            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillData s = allSkills[i];
                if (s == null) continue;
                _cooldownTimers[s.skillType] = 0f;
                if (s.unlockedByDefault)
                {
                    _unlockedSkills.Add(s.skillType);
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < allSkills.Count; i++)
            {
                SkillData s = allSkills[i];
                if (s == null) continue;
                float rem;
                if (_cooldownTimers.TryGetValue(s.skillType, out rem) && rem > 0f)
                {
                    rem = Mathf.Max(0f, rem - dt);
                    _cooldownTimers[s.skillType] = rem;
                    GameEvents.RaiseSkillCooldownUpdated(s.skillType, rem, s.cooldown);
                }
            }
        }

        public SkillData GetSkillData(SkillType type)
        {
            SkillData data;
            if (_skillLookup.TryGetValue(type, out data)) return data;
            return null;
        }

        public bool IsUnlocked(SkillType type)
        {
            return _unlockedSkills.Contains(type);
        }

        public float GetCooldownRemaining(SkillType type)
        {
            float rem;
            return _cooldownTimers.TryGetValue(type, out rem) ? rem : 0f;
        }

        public bool CanUseSkill(SkillType type, PlayerStats stats)
        {
            if (!IsUnlocked(type)) return false;

            SkillData data = GetSkillData(type);
            if (data == null) return false;

            if (GetCooldownRemaining(type) > 0f) return false;

            if (data.silkCost > 0f && (stats == null || stats.CurrentSilk < data.silkCost))
            {
                return false;
            }

            return true;
        }

        public bool TryConsumeSkill(SkillType type, PlayerStats stats)
        {
            if (!CanUseSkill(type, stats)) return false;

            SkillData data = GetSkillData(type);
            if (data.silkCost > 0f && stats != null)
            {
                if (!stats.TrySpendSilk(data.silkCost)) return false;
            }

            if (data.cooldown > 0f)
            {
                _cooldownTimers[type] = data.cooldown;
                GameEvents.RaiseSkillCooldownUpdated(type, data.cooldown, data.cooldown);
            }

            return true;
        }

        public bool UnlockSkill(SkillType type, bool silent = false)
        {
            if (_unlockedSkills.Contains(type)) return false;
            _unlockedSkills.Add(type);

            SkillData data = GetSkillData(type);
            if (!silent && data != null)
            {
                GameEvents.RaiseSkillUnlocked(data);
                GameEvents.RaiseNotification("ABILITY AWAKENED: " + data.displayName.ToUpper(), data.description + " (" + data.inputHint + ")");
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySkillUnlock();
            }
            return true;
        }

        public bool UnlockSkill(SkillData data, bool silent = false)
        {
            if (data == null) return false;
            if (!_skillLookup.ContainsKey(data.skillType))
            {
                allSkills.Add(data);
                _skillLookup[data.skillType] = data;
                _cooldownTimers[data.skillType] = 0f;
            }
            return UnlockSkill(data.skillType, silent);
        }

        public List<string> ExportUnlockedSkillIds()
        {
            List<string> ids = new List<string>();
            foreach (SkillType type in _unlockedSkills)
            {
                ids.Add(type.ToString());
            }
            return ids;
        }

        public void ImportUnlockedSkillIds(List<string> ids)
        {
            ResetToDefaults();
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                try
                {
                    SkillType parsed = (SkillType)System.Enum.Parse(typeof(SkillType), ids[i], true);
                    _unlockedSkills.Add(parsed);
                }
                catch
                {
                    // Ignore unknown skill IDs cleanly for forward compatibility
                }
            }
        }
    }
}
