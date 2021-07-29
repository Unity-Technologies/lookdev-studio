using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

// -----------------------------------------------------------------------------
//
// Use this editor example C# file to develop editor (non-runtime) code.
//
// -----------------------------------------------------------------------------

namespace LookDev.Editor
{
    /// <summary>
    /// Packages require documentation for ALL public Package APIs.
    /// 
    /// The summary tags are where you put all basic descriptions.
    /// For example, this is where you would normally provide a general description of the class.
    ///
    /// Inside these tags, you can use normal markdown, such as **bold**, *italics*, and `code` formatting.
    /// </summary>
    /// <remarks>
    /// For more information on using the XML Documentation comments and the supported tags,
    /// see the [Microsoft documentation](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments).
    /// </remarks>
    [InitializeOnLoad]
    public static class LookDevStudioEditor
    {
#if !LOOKDEV_DISABLE_AUTOTOGGLE
        static LookDevStudioEditor()
        {
            IEnumerator LoadLookDevOnReload()
            {
                yield return new WaitForEndOfFrame();
                EnableLookDev();
            }

            GameObject owner = new GameObject("CoroutineOwner");
            EditorCoroutineUtility.StartCoroutine(LoadLookDevOnReload(), owner);

            //Activating LookDev should change the scene, effectively destroying the 'owner'.
        }
#endif

        static bool _loadingCameraPosition;

        static float _helpUiAspect;
        static int _helpPage;

        static VisualElement _sceneOverlayRoot;
        static VisualElement _sceneHelpUiRoot;
        static Slider _rotationSlider;

        const string ScreencaptureFolder = "Screenshots";

        static LookDevPreferences _lookDevPreferences;

        [MenuItem("LookDev Studio/Enable")]
        public static void EnableLookDev()
        {
            if (_lookDevPreferences == null)
                _lookDevPreferences = LookDevPreferences.instance;

            _lookDevPreferences.EnableOrbit = false;
            _lookDevPreferences.EnableTurntable = false;

            EditorApplication.update += OnLookDevModeUpdate;
            SceneView.duringSceneGui += OnDuringSceneGuiForDragDrop;
            LightingPresetSceneChanger.OnLightSceneChangedEvent += LoadSceneViewOverlay;
            LightingPresetSceneChanger.OnLightSceneChangedEvent += LoadCurrentCameraPosition;

            ModeService.ChangeModeById("lookdevstudio");

            LightingPresetSceneChanger.Initialize();

            LookDevSearchHelpers.Initialize();

            //Hide the LightRig components
            var lightRigs = GameObject.FindObjectsOfType<LightRig>();
            foreach (var lightRig in lightRigs)
            {
                lightRig.hideFlags = HideFlags.HideInInspector;
            }

            /*
            it's to set the SceneView as the initial view when user opens the lookDev at the first time.
            Otherwise, the GameView is usually set to the first view. 
            */
            var sceneView = SceneView.sceneViews[0] as SceneView;
            sceneView.ResetCameraSettings();
            sceneView.showGrid = false;
            sceneView.drawGizmos = false;
            sceneView.sceneViewState.alwaysRefresh = true;
            if (sceneView?.hasFocus == false)
                sceneView.Focus();
        }

        static void OnLookDevModeUpdate()
        {
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

            SaveCurrentCameraPosition();
        }

        public static void LoadSceneViewOverlay()
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

                var fogToggle = new Toggle("Fog");
                var enableFog = _lookDevPreferences.EnableFog;
                ToggleFog(enableFog);
                fogToggle.SetValueWithoutNotify(enableFog);
                fogToggle.RegisterValueChangedCallback(evt =>
                {
                    ToggleFog(evt.newValue);
                    _lookDevPreferences.EnableFog = evt.newValue;
                });
                _sceneOverlayRoot.Add(fogToggle);

                var hdriSkyToggle = new Toggle("HDRI Sky");
                var enableHdriSky = _lookDevPreferences.EnableHDRISky;
                ToggleHdriSky(enableHdriSky);
                hdriSkyToggle.SetValueWithoutNotify(enableHdriSky);
                hdriSkyToggle.RegisterValueChangedCallback(evt =>
                {
                    ToggleHdriSky(evt.newValue);
                    _lookDevPreferences.EnableHDRISky = evt.newValue;
                });
                _sceneOverlayRoot.Add(hdriSkyToggle);

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

                var lightRig = GameObject.FindObjectOfType<LightRig>();
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
                        var lightRig = GameObject.FindObjectOfType<LightRig>();
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

                var saveCameraButton = new Button(SaveCameraPosition);
                saveCameraButton.text = "Save Camera Position";
                _sceneOverlayRoot.Add(saveCameraButton);

                var loadCameraButton = new Button(LoadCameraPosition);
                loadCameraButton.text = "Load Camera Position";
                _sceneOverlayRoot.Add(loadCameraButton);


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
                //pageButton.style.marginBottom = -15;
                helpButtonRoot.Add(pageButton);

                sv.rootVisualElement[0].Add(_sceneOverlayRoot);
                sv.rootVisualElement[0].Add(_sceneHelpUiRoot);
            }
        }

        private static void OnDuringSceneGuiForDragDrop(SceneView sceneView)
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

            LookDevHelpers.DragDrop();
        }


        [CommandHandler("Commands/Return to Unity", CommandHint.Menu | CommandHint.Validate)]
        private static void DisableLookDev(CommandExecuteContext context)
        {
            ModeService.ChangeModeById("default");
            LightingPresetSceneChanger.Shutdown();

            EditorApplication.update -= OnLookDevModeUpdate;
            SceneView.duringSceneGui -= OnDuringSceneGuiForDragDrop;
            LightingPresetSceneChanger.OnLightSceneChangedEvent -= LoadSceneViewOverlay;
            LightingPresetSceneChanger.OnLightSceneChangedEvent -= LoadCurrentCameraPosition;

            LookDevSearchHelpers.UnregisterCallbacks();
        }

        static void SaveCurrentCameraPosition()
        {
            var sv = SceneView.lastActiveSceneView;
            var driver = CameraSync.instance.GetDriver(sv);
            var camera = driver.targetCamera;

            _lookDevPreferences.CurrentCameraPosition =
                new LookDevPreferences.CameraPositionPreset
                {
                    Position = camera.transform.position,
                    Rotation = camera.transform.rotation
                };

            _lookDevPreferences.CurrentCameraPositionIsInitialized = true;
        }

        static void SaveCameraPosition()
        {
            var sv = SceneView.lastActiveSceneView;
            var driver = CameraSync.instance.GetDriver(sv);
            var camera = driver.targetCamera;

            _lookDevPreferences.SavedCameraPositions = new List<LookDevPreferences.CameraPositionPreset>
            {
                new LookDevPreferences.CameraPositionPreset
                {
                    Position = camera.transform.position,
                    Rotation = camera.transform.rotation
                }
            };
        }

        static void LoadCurrentCameraPosition()
        {
            if (!_lookDevPreferences.CurrentCameraPositionIsInitialized)
                return;

            var cameraRig = GameObject.FindWithTag("LookDevCam");
            var camera = cameraRig.gameObject.GetComponentInChildren<Camera>();

            SceneView sv = SceneView.lastActiveSceneView;
            var driver = CameraSync.instance.GetDriver(sv);
            driver.syncMode = SyncMode.GameViewToSceneView;
            driver.targetCamera = camera;
            driver.syncing = true;

            var savedPosition = _lookDevPreferences.CurrentCameraPosition;
            camera.transform.position = savedPosition.Position;
            camera.transform.rotation = savedPosition.Rotation;

            _loadingCameraPosition = true;
        }

        static void LoadCameraPosition()
        {
            var cameraRig = GameObject.FindWithTag("LookDevCam");
            var camera = cameraRig.gameObject.GetComponentInChildren<Camera>();

            SceneView sv = SceneView.lastActiveSceneView;
            var driver = CameraSync.instance.GetDriver(sv);
            driver.syncMode = SyncMode.GameViewToSceneView;
            driver.targetCamera = camera;
            driver.syncing = true;

            var savedPosition = _lookDevPreferences.SavedCameraPositions[0];
            camera.transform.position = savedPosition.Position;
            camera.transform.rotation = savedPosition.Rotation;

            _loadingCameraPosition = true;
        }

        [CommandHandler("Commands/ResetLookDevView", CommandHint.Menu | CommandHint.Validate)]
        private static void ResetLookDevView(CommandExecuteContext context)
        {
            LookDevHelpers.ResetLookDevCamera();
        }

        static void ToggleHdriSky(bool enabled)
        {
            var camera = LookDevHelpers.GetLookDevCam();
            var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
            additionalCameraData.clearColorMode = enabled
                ? HDAdditionalCameraData.ClearColorMode.Sky
                : HDAdditionalCameraData.ClearColorMode.Color;
            SceneView.lastActiveSceneView.sceneViewState.showSkybox = enabled;
        }

        static void ToggleFog(bool enabled)
        {
            var lightRig = GameObject.FindObjectOfType<LightRig>();
            lightRig.ToggleFog(enabled);
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
    }
}