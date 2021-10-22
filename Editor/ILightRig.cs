using UnityEngine;
using UnityEngine.Rendering;

namespace LookDev
{
    public abstract class ILightRig : MonoBehaviour
    {
        public Volume GlobalVolume;
        public Transform LightDirection;

        public float StartingSkyRotation;
        public Quaternion StartingLightRotation;
        
        public abstract void ToggleFog(bool isEnabled);
        public abstract void SetRotation(float rotation);
        
    }
}
