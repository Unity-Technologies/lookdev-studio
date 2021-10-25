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

        public bool AreAssetsInstalled;
        public bool IsRenderPipelineInitialized;
        public int CurrentCameraIndex;
        public CameraPositionPreset[] SavedCameraPositions = new CameraPositionPreset[10];
        public bool EnableDeveloperMode = false;
        public bool EnableHDRISky = true;
        public bool EnableGroundPlane = true;
        public bool SnapGroundToObject = true;
        public bool EnableFog = true;
        public bool EnableOrbit = false;
        public bool EnableTurntable = false;
        static string[] _lookDevSessionGuids;
        static GUIStyle _lockedLabelStyle;
        static Texture2D _lockedIcon;

        public static GUIStyle LockedLabelStyle
        {
            get
            {
                if (_lockedLabelStyle == null)
                {
                    _lockedLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleRight, normal = {textColor = Color.red}
                    };
                }

                return _lockedLabelStyle;
            }
        }

        static void ProjectWindowItemOnGui(string guid, Rect rect)
        {
            foreach (var sessionGuid in _lookDevSessionGuids)
            {
                var sessionPath = AssetDatabase.GUIDToAssetPath(sessionGuid);
                LookDevSession session = AssetDatabase.LoadAssetAtPath<LookDevSession>(sessionPath);
                foreach (var assetPath in session.Assets)
                {
                    if (AssetDatabase.GUIDFromAssetPath(assetPath).ToString() == guid)
                    {
                        var iconRect = rect;
                        var aspectRatio = _lockedIcon.height / (float) _lockedIcon.width;

                        iconRect.height = Mathf.Min(rect.height, _lockedIcon.height);
                        iconRect.width = iconRect.height * aspectRatio;
                        GUI.DrawTexture(iconRect, _lockedIcon);
                        GUI.Label(rect, "In LookDev", LockedLabelStyle);
                    }
                }
            }
        }

        void OnEnable()
        {
            //Initialize the position presets to 10 always
            if (SavedCameraPositions.Length < 10)
            {
                SavedCameraPositions = new CameraPositionPreset[10];
            }
            
            _lookDevSessionGuids = AssetDatabase.FindAssets("t:LookDevSession");

            _lockedIcon =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.unity.lookdevstudio/Editor/Resources/Icon_Lock.png");
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGui;
        }

        void OnDisable()
        {
            EditorApplication.projectWindowItemOnGUI -= ProjectWindowItemOnGui;
            Save(true);
        }
    }
}