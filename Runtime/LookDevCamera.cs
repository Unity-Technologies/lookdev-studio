using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace LookDev
{
    public class LookDevCamera : MonoBehaviour
    {
        public const int NUM_CAMERA_INDICES = 5;

        int _currentCameraIndex;

        public int CurrentCameraIndex => _currentCameraIndex;

        CameraPreset[] _cameraPreset = new CameraPreset[NUM_CAMERA_INDICES];

        [SerializeField] Camera _camera;
        public Camera Camera => _camera;

        public void OnCreated()
        {
            Reset();
        }

        void OnValidate()
        {
            if (_cameraPreset.Length != NUM_CAMERA_INDICES)
            {
                _cameraPreset = new CameraPreset[NUM_CAMERA_INDICES];
            }
        }

        void Reset()
        {
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }

            LoadCameraPresetsFromDisk();
        }

        public void LoadCameraPresetsFromDisk()
        {
#if UNITY_EDITOR
            var lookDevSettingsFolderPath = "LookDevStudioSettings";
            if (!AssetDatabase.IsValidFolder($"Assets/{lookDevSettingsFolderPath}"))
            {
                AssetDatabase.CreateFolder("Assets", lookDevSettingsFolderPath);
            }

            if (!AssetDatabase.IsValidFolder($"Assets/{lookDevSettingsFolderPath}/Resources"))
            {
                AssetDatabase.CreateFolder($"Assets/{lookDevSettingsFolderPath}", "Resources");
            }

            for (int i = 0; i < NUM_CAMERA_INDICES; ++i)
            {
                var cameraPreset =
                    AssetDatabase.LoadAssetAtPath<CameraPreset>(
                        $"Assets/{lookDevSettingsFolderPath}/Resources/CameraPreset {i}.asset");

                if (cameraPreset == null)
                {
                    cameraPreset = ScriptableObject.CreateInstance<CameraPreset>();
                    cameraPreset.name = $"CameraPreset {i}";
                    AssetDatabase.CreateAsset(cameraPreset,
                        $"Assets/{lookDevSettingsFolderPath}/Resources/CameraPreset {i}.asset");
                }

                _cameraPreset[i] = cameraPreset;
            }
#else
            for (int i = 0; i < NUM_CAMERA_INDICES; ++i)
            {
                var cameraPreset = Resources.Load<CameraPreset>($"CameraPreset {i}");
                if (cameraPreset == null)
                {
                    cameraPreset = ScriptableObject.CreateInstance<CameraPreset>();
                    cameraPreset.name = $"CameraPreset {i}";
                }
                _cameraPreset[i] = cameraPreset;
            }
#endif
        }

        public void LoadCameraPreset(int index)
        {
            var savedPosition = _cameraPreset[index];
            Camera.transform.SetPositionAndRotation(savedPosition.Position, savedPosition.Rotation);
            _currentCameraIndex = index;
        }

        public void SaveCameraPosition(Vector3 position, Quaternion rotation)
        {
            _cameraPreset[_currentCameraIndex].SetPositionAndRotation(position, rotation);
        }
    }
}