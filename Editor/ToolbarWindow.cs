using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LookDev.Editor
{
    public class ToolbarWindow : EditorWindow
    {
        bool _savePosition;

        Button _screenshotButton;
        Button _saveCameraButton;
        Toggle _autoSaveCameraToggle;

        public static TemplateContainer ToolbarContainer;

        void CreateGUI()
        {
            var uxmlTemplate =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    $"Packages/com.unity.lookdevstudio/UI/LookDevStudioToolbar.uxml");
            ToolbarContainer = uxmlTemplate.CloneTree();
            rootVisualElement.Add(ToolbarContainer);

            for (int i = 0; i < LookDevCamera.NUM_CAMERA_INDICES; ++i)
            {
                var cameraButton = rootVisualElement.Q<Button>($"cameraButton{i}");
                var index = i;
                cameraButton.clicked += () => { LookDevShortcutsOverlay.LoadCameraPosition(index); };
            }

            _screenshotButton = rootVisualElement.Q<Button>("ScreenshotButton");
            _screenshotButton.clicked += () =>
            {
                LookDevHelpers.GetGameview();
                var fullPath = $"{Directory.GetCurrentDirectory()}/{LookDevPreferences.ScreencaptureFolder}";
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                var screenshotName =
                    $"Screenshot_{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}.png";
                ScreenCapture.CaptureScreenshot($"{LookDevPreferences.ScreencaptureFolder}/{screenshotName}", 2);
                Debug.Log($"Screenshot Captured: {fullPath}/{screenshotName}");
            };


            _saveCameraButton = rootVisualElement.Q<Button>("saveCameraButton");
            _saveCameraButton.clicked += () => { LookDevShortcutsOverlay.SaveCameraPosition(); };

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
            for (int i = 0; i < LookDevCamera.NUM_CAMERA_INDICES; ++i)
            {
                var cameraButton = rootVisualElement.Q<Button>($"cameraButton{i}");
                var lookDevCamera = LookDevHelpers.GetLookDevCam();

                if (i == lookDevCamera.CurrentCameraIndex)
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