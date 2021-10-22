using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LookDev.Editor
{
    public static class LookDevShortcutsOverlay
    {
        static VisualElement _sceneOverlayRoot;
        static VisualElement _sceneHelpUiRoot;
        static LookDevPreferences _lookDevPreferences;
        static Slider _rotationSlider;
        static float _helpUiAspect;
        static int _helpPage;
        static bool _loadingCameraPosition;

        public static bool AutoSaveCameraPosition;

        static readonly List<LoadExtensionDelegate> _extensions = new List<LoadExtensionDelegate>();

        public delegate bool LoadExtensionDelegate(VisualElement overlayRoot);

        const string ScreencaptureFolder = "Screenshots";

        static double _toastLastFrameTime;
        static string _toastMessage;
        static float _toastElapsed;
        static float _toastDuration;

        public static event Action<int> OnCameraPositionLoaded;

        public static void RenderExtension(LoadExtensionDelegate loadExtensionMethod)
        {
            _extensions.Add(loadExtensionMethod);
        }

        public static void Enable()
        {
            _lookDevPreferences = LookDevPreferences.instance;
            LightingPresetSceneChanger.OnLightSceneChangedEvent += LoadSceneViewOverlay;
            SceneView.beforeSceneGui += CameraPresetShortcut;
        }

        public static void Disable()
        {
            LightingPresetSceneChanger.OnLightSceneChangedEvent -= LoadSceneViewOverlay;
            SceneView.beforeSceneGui -= CameraPresetShortcut;
        }

        public static void Update()
        {
            if (_lookDevPreferences.CurrentCameraIndex == -1)
            {
                LoadCameraPosition(0);
            }

            if (_loadingCameraPosition)
            {
                _loadingCameraPosition = false;
            }
            else if (_lookDevPreferences.EnableOrbit)
            {
                var cameraRig = GameObject.FindWithTag("LookDevCam");
                LookDevHelpers.UpdateRotation(cameraRig.transform, Time.deltaTime, 20f);
                SceneView sv = SceneView.lastActiveSceneView;

                var driver = CameraSync.instance.GetDriver(sv);
                driver.targetCamera = cameraRig.gameObject.GetComponentInChildren<Camera>();
                driver.syncMode = SyncMode.GameViewToSceneView;
                driver.syncing = true;
            }
            else if (_lookDevPreferences.EnableTurntable)
            {
                var assetHolder = LookDevHelpers.GetLookDevContainer();
                LookDevHelpers.UpdateRotation(assetHolder.transform, Time.deltaTime, 20f);
            }
            else
            {
                SceneView sv = SceneView.lastActiveSceneView;
                var driver = CameraSync.instance.GetDriver(sv);
                driver.targetCamera = LookDevHelpers.GetLookDevCam();
                driver.syncMode = SyncMode.SceneViewToGameView;
                driver.syncing = true;

                if (AutoSaveCameraPosition)
                {
                    SaveCameraPosition(_lookDevPreferences.CurrentCameraIndex);
                }
            }

            //Update ground plane
            if (_lookDevPreferences.SnapGroundToObject &&
                LookDevHelpers.GetGroundPlane(out GameObject groundPlane))
            {
                var assetHolder = LookDevHelpers.GetLookDevContainer();
                var containerBounds = LookDevHelpers.GetBoundsWithChildren(assetHolder);
                var newPosition = containerBounds.center - new Vector3(0, containerBounds.extents.y, 0);
                groundPlane.transform.position = newPosition;
            }

            if (_sceneHelpUiRoot.visible)
            {
                _sceneHelpUiRoot.style.width = _sceneHelpUiRoot.parent.layout.width * 0.8f;
                _sceneHelpUiRoot.style.height = _sceneHelpUiRoot.style.width.value.value * _helpUiAspect;
            }
        }

        private static void LoadSceneViewOverlay()
        {
            _sceneOverlayRoot?.RemoveFromHierarchy();
            _sceneHelpUiRoot?.RemoveFromHierarchy();

            var sv = SceneView.lastActiveSceneView;
            if (sv.rootVisualElement[0].name.StartsWith("unity-scene-view-camera-rect"))
            {
                sv.rootVisualElement[0].style.flexDirection = FlexDirection.ColumnReverse;

                _sceneOverlayRoot = new VisualElement();
                var backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.2f, 0.2f, 0.2f)
                    : new Color(0.8f, 0.8f, 0.8f);
                _sceneOverlayRoot.style.backgroundColor = backgroundColor;
                _sceneOverlayRoot.style.width = 180;
                _sceneOverlayRoot.style.marginLeft = 10;
                _sceneOverlayRoot.style.marginBottom = 10;
                _sceneOverlayRoot.style.paddingLeft = 5;
                _sceneOverlayRoot.style.paddingTop = 5;
                _sceneOverlayRoot.style.opacity = 0.95f;
                _sceneOverlayRoot.name = "look-dev-scene-view-overlay-root";

                var header = LookDevHelpers.CreateTextHeader("LookDev");
                header.style.height = 25;
                _sceneOverlayRoot.Add(header);

                var turntableToggle = new Toggle("Turntable");
                turntableToggle.SetValueWithoutNotify(_lookDevPreferences.EnableTurntable);

                var orbitToggle = new Toggle("Orbit Camera");
                orbitToggle.SetValueWithoutNotify(_lookDevPreferences.EnableOrbit);
                orbitToggle.RegisterValueChangedCallback(evt =>
                {
                    _lookDevPreferences.EnableOrbit = evt.newValue;
                    if (_lookDevPreferences.EnableTurntable)
                    {
                        _lookDevPreferences.EnableTurntable = false;
                        turntableToggle.SetValueWithoutNotify(false);
                    }

                    // When entering orbit, set the camera rig position to the camera position before starting
                    if (_lookDevPreferences.EnableOrbit)
                    {
                        SetCameraRigToCameraPosition();
                    }
                });
                _sceneOverlayRoot.Add(orbitToggle);

                turntableToggle.RegisterValueChangedCallback(evt =>
                {
                    _lookDevPreferences.EnableTurntable = evt.newValue;
                    if (_lookDevPreferences.EnableOrbit)
                    {
                        _lookDevPreferences.EnableOrbit = false;
                        orbitToggle.SetValueWithoutNotify(false);
                    }
                });
                _sceneOverlayRoot.Add(turntableToggle);


                var enableGroundPlane = _lookDevPreferences.EnableGroundPlane;
                var groundPlaneToggle = new Toggle("Show Ground");
                bool foundGroundPlane = false;

                //Init ground plane toggle
                if (LookDevHelpers.GetGroundPlane(out GameObject groundPlane))
                {
                    if (groundPlane.TryGetComponent(out Renderer renderer))
                    {
                        renderer.enabled = enableGroundPlane;
                        groundPlaneToggle.SetValueWithoutNotify(enableGroundPlane);
                        foundGroundPlane = true;
                    }
                }

                var snapGroundToObject = new Toggle("Snap Ground To Object");
                if (foundGroundPlane)
                {
                    groundPlaneToggle.RegisterValueChangedCallback(evt =>
                    {
                        if (LookDevHelpers.GetGroundPlane(out GameObject groundPlane))
                        {
                            if (groundPlane.TryGetComponent(out Renderer renderer))
                            {
                                renderer.enabled = evt.newValue;
                            }
                        }

                        _lookDevPreferences.EnableGroundPlane = evt.newValue;
                    });
                    _sceneOverlayRoot.Add(groundPlaneToggle);

                    snapGroundToObject.SetValueWithoutNotify(_lookDevPreferences.SnapGroundToObject);
                    snapGroundToObject.RegisterValueChangedCallback(evt =>
                    {
                        _lookDevPreferences.SnapGroundToObject = evt.newValue;

                        if (!evt.newValue && LookDevHelpers.GetGroundPlane(out GameObject groundPlane))
                        {
                            groundPlane.transform.position = Vector3.zero;
                        }
                    });
                    _sceneOverlayRoot.Add(snapGroundToObject);
                }

                var lightRig = GameObject.FindObjectOfType<ILightRig>();
                if (lightRig)
                {
                    _rotationSlider = new Slider()
                    {
                        lowValue = 0,
                        highValue = 360,
                        value = lightRig.StartingSkyRotation
                    };
                    _rotationSlider.RegisterValueChangedCallback(evt =>
                    {
                        var lightRig = GameObject.FindObjectOfType<ILightRig>();
                        lightRig.SetRotation(evt.newValue);
                    });
                    _sceneOverlayRoot.Add(new Label("Rotate Light (Shift+Alt+Move)"));
                    _sceneOverlayRoot.Add(_rotationSlider);
                }
                else
                {
                    Debug.LogError("Lighting Rig not found");
                }

                var screenshotButton = new Button(() =>
                {
                    LookDevHelpers.GetGameview();
                    var fullPath = $"{Directory.GetCurrentDirectory()}/{ScreencaptureFolder}";
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    var screenshotName =
                        $"Screenshot_{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}.png";
                    ScreenCapture.CaptureScreenshot($"{ScreencaptureFolder}/{screenshotName}", 2);
                    Debug.Log($"Screenshot Captured: {fullPath}/{screenshotName}");
                });
                var screenshotIcon = Resources.Load<Texture2D>("EditorIcons/ScreenshotIcon");
                screenshotButton.style.backgroundImage = new StyleBackground(screenshotIcon);
                screenshotButton.style.width = 40;
                screenshotButton.style.height = 40;
                screenshotButton.style.maxWidth = 40;
                screenshotButton.style.maxHeight = 40;
                _sceneOverlayRoot.Add(screenshotButton);

                _sceneHelpUiRoot = new VisualElement();
                var helpUiOne = Resources.Load<Texture2D>("LookdevHelp");
                var helpUiTwo = Resources.Load<Texture2D>("LookdevHelp_02");

                _helpUiAspect = helpUiOne.height / (float) helpUiOne.width;
                _sceneHelpUiRoot.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                _sceneHelpUiRoot.style.position = Position.Absolute;
                _sceneHelpUiRoot.style.marginBottom = 20;
                _sceneHelpUiRoot.style.paddingBottom = 10;
                _sceneHelpUiRoot.style.visibility = Visibility.Hidden;
                _sceneHelpUiRoot.transform.position = new Vector3(10, 10, 0);
                _sceneHelpUiRoot.style.backgroundImage = new StyleBackground(helpUiOne);

                var showHelpButton = new Button(() => { _sceneHelpUiRoot.style.visibility = Visibility.Visible; });
                showHelpButton.style.position = Position.Absolute;
                showHelpButton.style.alignSelf = Align.FlexEnd;
                showHelpButton.text = "Help";
                showHelpButton.style.top = 5;
                showHelpButton.style.right = 5;
                _sceneOverlayRoot.Add(showHelpButton);

                var closeHelpButton = new Button(() => { _sceneHelpUiRoot.style.visibility = Visibility.Hidden; });
                closeHelpButton.style.top = 5;
                closeHelpButton.style.right = 5;
                closeHelpButton.style.position = Position.Absolute;
                closeHelpButton.style.alignSelf = Align.FlexEnd;
                closeHelpButton.text = "Close";
                _sceneHelpUiRoot.Add(closeHelpButton);

                _sceneHelpUiRoot.style.flexDirection = FlexDirection.ColumnReverse;
                var helpButtonRoot = new VisualElement();
                helpButtonRoot.name = "lookdev-helpbutton-root";
                helpButtonRoot.style.alignSelf = Align.Center;
                helpButtonRoot.style.flexDirection = FlexDirection.Row;
                _sceneHelpUiRoot.Add(helpButtonRoot);

                var pageButton = new Button();
                pageButton.text = "Next Page";
                pageButton.clicked += () =>
                {
                    if (_helpPage == 0)
                    {
                        _sceneHelpUiRoot.style.backgroundImage = new StyleBackground(helpUiTwo);
                        pageButton.text = "Previous Page";
                    }
                    else
                    {
                        _sceneHelpUiRoot.style.backgroundImage = new StyleBackground(helpUiOne);
                        pageButton.text = "Next Page";
                    }

                    _helpPage = (_helpPage + 1) % 2;
                };

                pageButton.style.maxHeight = 80;
                pageButton.style.maxWidth = 120;
                helpButtonRoot.Add(pageButton);

                sv.rootVisualElement[0].Add(_sceneOverlayRoot);
                sv.rootVisualElement[0].Add(_sceneHelpUiRoot);

                foreach (var extension in _extensions)
                {
                    if (!extension.Invoke(_sceneOverlayRoot))
                    {
                        Debug.LogError($"Failed to load extension: {extension.Method.Name}");
                    }
                }
            }
        }

        static void SetCameraRigToCameraPosition()
        {
            var cameraRig = GameObject.FindWithTag("LookDevCam");
            var camera = cameraRig.gameObject.GetComponentInChildren<Camera>();
            var cameraTransform = camera.transform;

            //Should Orbit Lock the LookAt position to the model?
            /*
            var assetHolder = LookDevHelpers.GetLookDevContainer();
            var bounds = LookDevHelpers.GetBoundsWithChildren(assetHolder);
            cameraTransform.LookAt(bounds.center);
            */

            cameraTransform.parent.SetPositionAndRotation(cameraTransform.position, cameraTransform.rotation);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }

        public static void LoadCameraPosition(int index)
        {
            var cameraRig = GameObject.FindWithTag("LookDevCam");
            var camera = cameraRig.gameObject.GetComponentInChildren<Camera>();

            SceneView sv = SceneView.lastActiveSceneView;
            var driver = CameraSync.instance.GetDriver(sv);
            driver.syncMode = SyncMode.GameViewToSceneView;
            driver.targetCamera = camera;
            driver.syncing = true;

            var savedPosition = _lookDevPreferences.SavedCameraPositions[index];
            camera.transform.position = savedPosition.Position;
            camera.transform.rotation = savedPosition.Rotation;

            _loadingCameraPosition = true;

            _lookDevPreferences.CurrentCameraIndex = index;
            OnCameraPositionLoaded?.Invoke(index);
        }

        public static void SaveCameraPosition(int index)
        {
            var sv = SceneView.lastActiveSceneView;
            var driver = CameraSync.instance.GetDriver(sv);
            var camera = driver.targetCamera;
            _lookDevPreferences.SavedCameraPositions[index] = new LookDevPreferences.CameraPositionPreset
            {
                Position = camera.transform.position,
                Rotation = camera.transform.rotation
            };
        }

        static void CameraPresetShortcut(SceneView sv)
        {
            var current = Event.current;
            if (current != null && current.type == EventType.KeyDown)
            {
                if (EditorWindow.focusedWindow == sv)
                {
                    int keyIndex = -1;
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Alpha1:
                            keyIndex = 0;
                            break;
                        case KeyCode.Alpha2:
                            keyIndex = 1;
                            break;
                        case KeyCode.Alpha3:
                            keyIndex = 2;
                            break;
                        case KeyCode.Alpha4:
                            keyIndex = 3;
                            break;
                        case KeyCode.Alpha5:
                            keyIndex = 4;
                            break;
                        case KeyCode.Alpha6:
                            keyIndex = 5;
                            break;
                        case KeyCode.Alpha7:
                            keyIndex = 6;
                            break;
                        case KeyCode.Alpha8:
                            keyIndex = 7;
                            break;
                        case KeyCode.Alpha9:
                            keyIndex = 8;
                            break;
                        case KeyCode.Alpha0:
                            keyIndex = 9;
                            break;
                    }

                    if (keyIndex > -1)
                    {
                        LoadCameraPosition(keyIndex);
                        current.Use();
                    }
                }
            }
        }

        public static void DragDrop()
        {
            //Light Rotation Controls
            var evt = Event.current;
            if (evt.type == EventType.MouseMove)
            {
                if (evt.shift && evt.alt)
                {
                    _rotationSlider.value += evt.delta.x;
                    evt.Use();
                }
            }
        }
    }
}