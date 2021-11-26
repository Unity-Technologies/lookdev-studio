using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace LookDev
{
    [CreateAssetMenu(fileName = "New CameraPreset", menuName = "LookDev Studio/Create CameraPreset")]
    public class CameraPreset : ScriptableObject
    {
        public Vector3 Position => _position;
        [SerializeField] Vector3 _position = new Vector3(0, 2f, -5f);

        public Quaternion Rotation => _rotation;
        [SerializeField] Quaternion _rotation = Quaternion.Euler(19, 0, 0);

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}