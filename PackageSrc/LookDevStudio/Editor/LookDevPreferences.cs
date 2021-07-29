using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    [FilePath("Assets/LookDevPreferences.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LookDevPreferences : ScriptableSingleton<LookDevPreferences>
    {
        [Serializable]
        public struct CameraPositionPreset
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public bool CurrentCameraPositionIsInitialized = false;
        public CameraPositionPreset CurrentCameraPosition;
        public List<CameraPositionPreset> SavedCameraPositions = new List<CameraPositionPreset>();
        public bool EnableHDRISky = true;
        public bool EnableGroundPlane = true;
        public bool SnapGroundToObject = true;
        public bool EnableFog = true;
        public bool EnableOrbit = false;
        public bool EnableTurntable = false;

        void OnDisable()
        {
            Save(true);
        }
    }
}