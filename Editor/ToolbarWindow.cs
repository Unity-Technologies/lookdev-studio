using UnityEditor;
using UnityEngine.UIElements;

namespace LookDev.Editor
{
    public class ToolbarWindow : EditorWindow
    {
        bool _savePosition;

        Button _saveCameraButton;
        Toggle _autoSaveCameraToggle;

        void CreateGUI()
        {
            var uxmlTemplate =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    $"Packages/com.unity.lookdevstudio/UI/LookDevStudioToolbar.uxml");
            var ui = uxmlTemplate.CloneTree();
            rootVisualElement.Add(ui);

            for (int i = 0; i < 10; ++i)
            {
                var cameraButton = rootVisualElement.Q<Button>($"cameraButton{i}");
                var index = i;
                cameraButton.clicked += () => { LookDevShortcutsOverlay.LoadCameraPosition(index); };
            }

            _saveCameraButton = rootVisualElement.Q<Button>("saveCameraButton");
            _saveCameraButton.clicked += () =>
            {
                LookDevShortcutsOverlay.SaveCameraPosition(LookDevPreferences.instance.CurrentCameraIndex);
            };

            _autoSaveCameraToggle = rootVisualElement.Q<Toggle>("autoSaveCameraToggle");
            _autoSaveCameraToggle.RegisterValueChangedCallback(evt =>
            {
                _saveCameraButton.SetEnabled(!evt.newValue);
                LookDevShortcutsOverlay.AutoSaveCameraPosition = evt.newValue;
            });
        }

        void OnEnable()
        {
            LookDevShortcutsOverlay.OnCameraPositionLoaded += OnCameraLoaded;
        }

        void OnDisable()
        {
            LookDevShortcutsOverlay.OnCameraPositionLoaded -= OnCameraLoaded;
        }

        void OnCameraLoaded(int index)
        {
            for (int i = 0; i < 10; ++i)
            {
                var cameraButton = rootVisualElement.Q<Button>($"cameraButton{i}");

                if (i == LookDevPreferences.instance.CurrentCameraIndex)
                {
                    cameraButton.AddToClassList("selectedCameraButton");
                }
                else
                {
                    cameraButton.RemoveFromClassList("selectedCameraButton");
                }
            }
        }
    }
}