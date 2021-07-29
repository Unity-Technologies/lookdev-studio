using UnityEngine;

namespace UnityEditor
{
    public enum SyncMode
    {
        SceneViewToGameView,
        GameViewToSceneView
    }

    public class EditorViewCameraDriver : ScriptableObject
    {
        [SerializeField] bool m_Syncing;
        [SerializeField] SyncMode m_SyncMode;
        [SerializeField] Camera m_TargetCamera;
        [SerializeField] SceneView m_SceneView;

        public EditorViewCameraDriver(SceneView sv)
        {
            sceneView = sv;
        }

        public bool syncing
        {
            get => m_Syncing;
            set
            {
                if (m_Syncing != value)
                {
                    if (!value && targetCamera != null)
                    {
                        var t = targetCamera.transform;

                        var vFov = Mathf.Atan(2 * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));

                        sceneView.size = 10 * Mathf.Tan(vFov * 0.5f * Mathf.Deg2Rad);
                        sceneView.LookAt(t.position, t.rotation, sceneView.size, sceneView.camera.orthographic, true);
                    }

                    m_Syncing = value;
                }
            }
        }

        public SyncMode syncMode
        {
            get => m_SyncMode;
            set
            {
                if (value != m_SyncMode)
                {
                    if (value == SyncMode.GameViewToSceneView)
                    {
                        UnregesterDrivenProperties();
                    }
                    else if (value == SyncMode.SceneViewToGameView)
                    {
                        RegisterDrivenProperties();
                    }
                }

                m_SyncMode = value;
            }
        }


        public Camera targetCamera
        {
            get => m_TargetCamera;
            set
            {
                if (value == m_TargetCamera) return;

                m_TargetCamera = value;

                UnregesterDrivenProperties();

                if (m_TargetCamera == null) return;

                RegisterDrivenProperties();
            }
        }

        void UnregesterDrivenProperties()
        {
            CameraSync.Internals.DrivenPropertyManager_UnregisterProperties(this);
        }

        void RegisterDrivenProperties()
        {
            if (m_TargetCamera == null) return;

            CameraSync.Internals.DrivenPropertyManager_RegisterProperty(this,
                m_TargetCamera.transform, "m_LocalPosition");
            CameraSync.Internals.DrivenPropertyManager_RegisterProperty(this,
                m_TargetCamera.transform, "m_LocalRotation");
            CameraSync.Internals.DrivenPropertyManager_RegisterProperty(this,
                m_TargetCamera, "orthographic");
        }

        public SceneView sceneView
        {
            get => m_SceneView;
            set => m_SceneView = value;
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            CameraSync.Internals.ReflectInternals();
        }

        const float kSqrt2 = 1.41421356f;

        public void SynchronizeCamera()
        {
            if (!syncing)
                return;

            if (targetCamera == null)
                return;

            if (syncMode == SyncMode.GameViewToSceneView)
            {
                var sceneViewAspect = sceneView.camera.aspect;
                sceneView.camera.orthographic = targetCamera.orthographic;

                var hFov = Camera.VerticalToHorizontalFieldOfView(targetCamera.fieldOfView, targetCamera.aspect);
                var vFov = Camera.HorizontalToVerticalFieldOfView(hFov, sceneViewAspect);
                sceneView.camera.fieldOfView = vFov;

                var newSize = 0.0f;
                if (targetCamera.orthographic)
                    newSize = (targetCamera.aspect > sceneViewAspect ? targetCamera.aspect / sceneViewAspect : 1.0f) *
                              kSqrt2 * Mathf.Sqrt(sceneViewAspect) * targetCamera.orthographicSize;
                var transform = targetCamera.transform;
                sceneView.LookAt(transform.position, transform.rotation, newSize, targetCamera.orthographic, true);
            }
            else if (m_SyncMode == SyncMode.SceneViewToGameView)
            {
                var targetCameraTransform = targetCamera.transform;
                var sceneViewCameraTransform = sceneView.camera.transform;
                targetCameraTransform.position = sceneViewCameraTransform.position;
                targetCameraTransform.rotation = sceneViewCameraTransform.rotation;
                targetCamera.orthographic = sceneView.camera.orthographic;
                targetCamera.orthographicSize = sceneView.camera.orthographicSize;

                var sceneViewAspect = sceneView.camera.aspect;
                var hFov = Camera.VerticalToHorizontalFieldOfView(sceneView.camera.fieldOfView, sceneViewAspect);
                var vFov = Camera.HorizontalToVerticalFieldOfView(hFov, targetCamera.aspect);
                targetCamera.fieldOfView = vFov;
            }
        }

        public void DuringSceneGUI()
        {
            SynchronizeCamera();
        }

        public void Initialize(SceneView sceneView)
        {
            this.sceneView = sceneView;
        }
    }
}