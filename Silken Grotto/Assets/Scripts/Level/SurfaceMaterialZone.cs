using UnityEngine;

namespace SilkenGrotto
{
    /// <summary>
    /// Optional surface property modifier attached to icy/snowy cliffs and platforms
    /// (e.g., Mount Fay) to impart subtle slippery traction inertia without altering core jump physics.
    /// </summary>
    public class SurfaceMaterialZone : MonoBehaviour
    {
        [SerializeField] private bool isSlipperyIce = true;
        [SerializeField] private float accelerationMultiplier = 0.38f;
        [SerializeField] private float decelerationMultiplier = 0.22f;
        [SerializeField] private bool isBareStoneAudio = true;

        public bool IsSlipperyIce { get { return isSlipperyIce; } }
        public float AccelerationMultiplier { get { return Mathf.Clamp(accelerationMultiplier, 0.15f, 1f); } }
        public float DecelerationMultiplier { get { return Mathf.Clamp(decelerationMultiplier, 0.10f, 1f); } }
        public bool IsBareStoneAudio { get { return isBareStoneAudio; } }

        public void Configure(bool ice, float accelMult, float decelMult, bool stoneAudio)
        {
            isSlipperyIce = ice;
            accelerationMultiplier = accelMult;
            decelerationMultiplier = decelMult;
            isBareStoneAudio = stoneAudio;
        }
    }
}
