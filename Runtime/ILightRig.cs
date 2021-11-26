using UnityEngine;
using UnityEngine.Rendering;

namespace LookDev
{
    public abstract class ILightRig : MonoBehaviour
    {
        public Volume GlobalVolume;

        public abstract void ToggleFog(bool isEnabled);
        public abstract void SetRotation(float previousRotation, float newRotation);
    }
}