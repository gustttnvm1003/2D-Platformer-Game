using UnityEngine;

namespace SilkenGrotto
{
    [CreateAssetMenu(fileName = "NewAttackData", menuName = "Silken Grotto/Combat/Attack Data")]
    public class AttackData : ScriptableObject
    {
        [Header("Identity")]
        public string attackName = "Needle Slash";
        public int comboStep = 1;
        public AttackDirection direction = AttackDirection.Forward;

        [Header("Combat Values")]
        [Min(1)] public int baseDamage = 12;
        [Min(0.05f)] public float animationDuration = 0.28f;
        [Range(0f, 1f)] public float hitboxActiveStart = 0.18f;
        [Range(0f, 1f)] public float hitboxActiveEnd = 0.65f;
        [Min(0f)] public float cooldown = 0.08f;

        [Header("Hitbox Geometry (Local to Player facing right)")]
        public Vector2 hitboxOffset = new Vector2(1.25f, 1.05f);
        public Vector2 hitboxSize = new Vector2(1.85f, 1.15f);

        [Header("Impact & Feel")]
        public float knockbackForce = 6.5f;
        public float staggerDuration = 0.20f;
        public float hitStopDuration = 0.045f;
        public float silkGainOnHit = 12f;
        public bool locksHorizontalMovement = false;

        [Header("Visual & Audio")]
        public float slashAngleDegrees = 0f;
        public float slashScale = 0.85f;
        public Color slashTint = new Color(0.98f, 0.94f, 0.90f, 0.95f);
        public AudioClip overrideSfx;
    }
}
