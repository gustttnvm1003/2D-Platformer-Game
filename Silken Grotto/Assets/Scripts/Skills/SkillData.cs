using UnityEngine;

namespace SilkenGrotto
{
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "Silken Grotto/Skills/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "skill_dash";
        public string displayName = "Silk Dash";
        [TextArea(2, 4)]
        public string description = "Burst swiftly forward along woven silk threads, evading incoming strikes.";
        public SkillType skillType = SkillType.Dash;
        public Sprite icon;

        [Header("Activation Requirements")]
        public bool unlockedByDefault = false;
        [Min(0f)] public float silkCost = 0f;
        [Min(0f)] public float cooldown = 0.65f;
        public string inputHint = "RMB / K";

        [Header("Effect Parameters")]
        public float powerValue = 16f;
        public float duration = 0.22f;
        public Color accentColor = new Color(0.92f, 0.36f, 0.44f, 1f);
    }
}
