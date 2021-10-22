using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class CameraSync : ScriptableSingleton<CameraSync>
    {
        [SerializeField] List<EditorViewCameraDriver> m_EditorViewCameraDrivers = new List<EditorViewCameraDriver>();

        void OnEnable()
        {
            SceneView.duringSceneGui += instance.SceneViewOnDuringSceneGui;
            Internals.ReflectInternals();
        }

#if UNITY_2020_1_OR_NEWER
        [InitializeOnLoadMethod]
        static void InitializeSingleton()
        {
            instance.Init();
        }

#endif
        void Init()
        {
        }

        public class Internals
        {
            public static void ReflectInternals()
            {
                var engineTypes = typeof(Transform).Assembly.GetTypes();
                var drivenPropertyManagerType = engineTypes.FirstOrDefault(t => t.Name == "DrivenPropertyManager");

                {
                    var mi = drivenPropertyManagerType.GetMethod("RegisterProperty");
                    DrivenPropertyManager_RegisterProperty =
                        (DPM_RegisterPropertyDelegate) Delegate.CreateDelegate(typeof(DPM_RegisterPropertyDelegate),
                            mi);
                }
                {
                    var mi = drivenPropertyManagerType.GetMethod("UnregisterProperties");
                    DrivenPropertyManager_UnregisterProperties =
                        (DPM_UnregisterPropertiesDelegate) Delegate.CreateDelegate(
                            typeof(DPM_UnregisterPropertiesDelegate), mi);
                }
            }

            internal delegate void DPM_RegisterPropertyDelegate(Object driver, Object target, string propertyPath);
            internal delegate void DPM_UnregisterPropertiesDelegate(Object driver);

            internal static DPM_RegisterPropertyDelegate DrivenPropertyManager_RegisterProperty;
            internal static DPM_UnregisterPropertiesDelegate DrivenPropertyManager_UnregisterProperties;
        }
        
        void SceneViewOnDuringSceneGui(SceneView sceneView)
        {
            m_EditorViewCameraDrivers.RemoveAll(x => x == null);
            var cleanup = m_EditorViewCameraDrivers.FindAll(x => x.sceneView == null);
            foreach (var editorViewCameraDriver in cleanup)
            {
                m_EditorViewCameraDrivers.Remove(editorViewCameraDriver);
                DestroyImmediate(editorViewCameraDriver);
            }

            var driver = GetDriver(sceneView);
            driver.DuringSceneGUI();
        }

        public EditorViewCameraDriver GetDriver(SceneView sceneView)
        {
            var driver = m_EditorViewCameraDrivers.Find(x => x.sceneView == sceneView);
            if (driver == null)
            {
                driver = CreateInstance<EditorViewCameraDriver>();
                driver.Initialize(sceneView);
                m_EditorViewCameraDrivers.Add(driver);
            }

            return driver;
        }
    }
}