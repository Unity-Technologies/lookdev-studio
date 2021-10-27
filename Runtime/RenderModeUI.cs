using System;
using System.Collections;
using System.IO;
using LookDev;
using UnityEngine;
using UnityEngine.UIElements;

public class RenderModeUI : MonoBehaviour
{
    [SerializeField] UIDocument _uiDocumentPrefab;
    UIDocument _uiDocument;

    [SerializeField] StyleSheet _baseStylesheet;
    [SerializeField] StyleSheet _uiStylesheet;
    [SerializeField] StyleSheet _runtimeUiStylesheet;
    const string ScreencaptureFolder = "Screenshots";

    void OnEnable()
    {
        _uiDocument = Instantiate(_uiDocumentPrefab, transform);
        _uiDocument.name = "RenderModeUIDocument";

        var screenshotButton = _uiDocument.rootVisualElement.Q<Button>("ScreenshotButton");
        screenshotButton.visible = true;
        screenshotButton.clicked += () => { StartCoroutine(TakeScreenshot(_uiDocument)); };

        var rootVisualElement = _uiDocument.rootVisualElement;

        for (int i = 0; i < LookDevCamera.NUM_CAMERA_INDICES; ++i)
        {
            var cameraButton = rootVisualElement.Q<Button>($"cameraButton{i}");
            var index = i;
            cameraButton.clicked += () => { LoadCameraPosition(index); };
        }

        rootVisualElement.Q<Button>("saveCameraButton").RemoveFromHierarchy();
        rootVisualElement.Q<Toggle>("autoSaveCameraToggle").RemoveFromHierarchy();
        rootVisualElement.Q<Label>("autoSaveCameraToggleLabel").RemoveFromHierarchy();

        rootVisualElement.styleSheets.Clear();
        rootVisualElement.styleSheets.Add(_baseStylesheet);
        rootVisualElement.styleSheets.Add(_uiStylesheet);
        rootVisualElement.styleSheets.Add(_runtimeUiStylesheet);

        var lookDevCamera = FindObjectOfType<LookDevCamera>();
        lookDevCamera.OnCreated();
        LoadCameraPosition(0);
    }

    void LoadCameraPosition(int index)
    {
        var lookDevCamera = FindObjectOfType<LookDevCamera>();
        lookDevCamera.LoadCameraPreset(index);

        for (int i = 0; i < LookDevCamera.NUM_CAMERA_INDICES; ++i)
        {
            var cameraButton = _uiDocument.rootVisualElement.Q<Button>($"cameraButton{i}");
            cameraButton.RemoveFromClassList("selectedCameraButton");
        }

        var selectedButton = _uiDocument.rootVisualElement.Q<Button>($"cameraButton{index}");
        selectedButton.AddToClassList("selectedCameraButton");
    }


    static IEnumerator TakeScreenshot(UIDocument uiDocument)
    {
        var fullPath = $"{Directory.GetCurrentDirectory()}/{ScreencaptureFolder}";
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        uiDocument.rootVisualElement.visible = false;
        yield return null;
        var screenshotName =
            $"Screenshot_{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}.png";
        ScreenCapture.CaptureScreenshot($"{ScreencaptureFolder}/{screenshotName}", 2);
        Debug.Log($"Screenshot Captured: {fullPath}/{screenshotName}");
        yield return null;
        uiDocument.rootVisualElement.visible = true;
    }
}