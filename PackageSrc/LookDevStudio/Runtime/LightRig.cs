using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LookDev
{
    public class LightRig : MonoBehaviour
    {
        public Volume GlobalVolume;
        public Transform LightDirection;

        public float StartingSkyRotation;
        public Quaternion StartingLightRotation;

        void OnValidate()
        {
            if (GlobalVolume.sharedProfile.TryGet(out HDRISky skySettings))
            {
                StartingSkyRotation = skySettings.rotation.GetValue<float>();
                StartingLightRotation = LightDirection.rotation;
            }
        }

        public void SetRotation(float rotation)
        {
            if (GlobalVolume.sharedProfile.TryGet(out HDRISky skySettings))
            {
                var newRotation =
                    new ClampedFloatParameter(rotation, skySettings.rotation.min, skySettings.rotation.max);
                skySettings.rotation.SetValue(newRotation);

                LightDirection.rotation = Quaternion.AngleAxis(rotation, Vector3.down) * StartingLightRotation;
            }
        }

        public void ToggleFog(bool enabled)
        {
            if (GlobalVolume.sharedProfile.TryGet(out Fog fogSettings))
            {
                fogSettings.enabled.value = enabled;
            }
        }
    }
}