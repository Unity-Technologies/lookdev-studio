using System.Collections.Generic;
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

        public static bool AutoSaveCameraPosition;

        static readonly List<LoadExtensionDelegate> _extensions = new List<LoadExtensionDelegate>();

        public delegate bool LoadExtensionDelegate(VisualElement overlayRoot, VisualElement toolbarRoot);

        public static void RenderExtension(LoadExtensionDelegate loadExtensionMethod)
        {
            _extensions.Add(loadExtensionMethod);
        }

        public static void Enable()
        {
            _lookDevPreferences = LookDevPreferences.instance;
            LightingPresetSceneChanger.OnLightSceneChangedEvent += LoadSceneViewOverlay;
        }

        public static void Disable()
        {
            LightingPresetSceneChanger.OnLightSceneChangedEvent -= LoadSceneViewOverlay;
        }

        public static void Update()
        {
            if (_lookDevPreferences.EnableOrbit)
            {
                var lookDevCamera = LookDevHelpers.GetLookDevCam();
                var lookDevCameraTransform = lookDevCamera.Camera.transform;
                lookDevCameraTransform.RotateAround(Vector3.zero, new Vector3(0f, 1f, 0f), Time.deltaTime * 20f);
                LookDevHelpers.CopyCameraTransformToSceneViewCamera(lookDevCamera.Camera,
                    SceneView.lastActiveSceneView);
            }
            else if (_lookDevPreferences.EnableTurntable)
            {
                var lookDevCamera = LookDevHelpers.GetLookDevCam();
                var assetHolder = LookDevHelpers.GetLookDevContainer();
                LookDevHelpers.UpdateRotation(assetHolder.transform, Time.deltaTime, 20f);
                LookDevHelpers.CopySceneViewToCameraTransform(SceneView.lastActiveSceneView, lookDevCamera.Camera);
            }
            else
            {
                var lookDevCamera = LookDevHelpers.GetLookDevCam();
                LookDevHelpers.CopySceneViewToCameraTransform(SceneView.lastActiveSceneView, lookDevCamera.Camera);
                if (AutoSaveCameraPosition)
                {
                    LookDevHelpers.SaveCameraPosition();
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
                turntableToggle.style.marginBottom = 8;

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
                snapGroundToObject.style.marginBottom = 8;
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
                        value = 0
                    };
                    _rotationSlider.style.paddingBottom = 8;
                    _rotationSlider.RegisterValueChangedCallback(evt =>
                    {
                        var lightRig = GameObject.FindObjectOfType<ILightRig>();
                        lightRig.SetRotation(evt.previousValue, evt.newValue);
                    });
                    _sceneOverlayRoot.Add(new Label("Rotate Light (Shift+Alt+Move)"));
                    _sceneOverlayRoot.Add(_rotationSlider);
                }
                else
                {
                    Debug.LogError("Lighting Rig not found");
                }

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
                    if (!extension.Invoke(_sceneOverlayRoot, ToolbarWindow.ToolbarContainer))
                    {
                        Debug.LogError($"Failed to load extension: {extension.Method.Name}");
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