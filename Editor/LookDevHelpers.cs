using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LookDev.Editor
{
    public class LookDevHelpers
    {
        public const string LastHeroAssetGUIDKey = "LookDev_LastHeroAssetGUID";
        public const string CurrentSceneSelectionKey = "LookDev_CurrentPresetScene";

        private static readonly List<string> SupportedFormatExtensions =
            new List<string>() {".fbx", ".obj", ".tga", ".png", ".exr", ".tif", ".tiff", ".psd", ".hdr"};

        private static readonly List<string> SupportedFormatNames =
            new List<string>() { "FBX", "OBJ", "TGA", "PNG", "EXR", "TIF", "TIFF", "PSD", "HDR" };


        public const string LookDevSubdirectoryForModel = "LookDev/Models";
        public const string LookDevSubdirectoryForTexture = "LookDev/Textures";
        public const string LookDevSubdirectoryForMaterial = "LookDev/Materials";
        public const string LookDevSubdirectoryForHdri = "LookDev/Setup/Skybox/UnityHDRI";


        public static event Action<string> OnDragAndDropPreImportEvent;
        public static event Action<string> OnDragAndDropPostImportEvent;
        public static event Action OnHeroAssetReplacedEvent;
        public static event Action<int> OnCameraPositionLoaded;

        public static Action<Cubemap> OnSwapHDRi;
        public static Action<Material> OnSwapSkybox;

        public static void RecordLastHeroAsset(Object obj)
        {
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            EditorPrefs.SetString(LastHeroAssetGUIDKey, guid);
        }

        public static Object GetLastHeroAsset()
        {
            var guid = EditorPrefs.GetString(LastHeroAssetGUIDKey, String.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static List<string> GetSupportedFormatExtension()
        {
            return SupportedFormatExtensions;
        }

        public static List<string> GetSupportedFormatPairs()
        {
            List<string> supportedFormatPairs = new List<string>(SupportedFormatExtensions.Count * 2);
            for (int i = 0; i < supportedFormatPairs.Count; ++i)
            {
                supportedFormatPairs.Add(SupportedFormatNames[i]);
                supportedFormatPairs.Add(SupportedFormatExtensions[i]);
            }

            return supportedFormatPairs;
        }

        public static void ReplaceHeroAsset(GameObject target)
        {
            SetHeroAsset(target);
            ResetLookDevCamera(target);
            RecordLastHeroAsset(target);
        }


        public static bool GetGroundPlane(out GameObject groundPlane)
        {
            groundPlane = GameObject.FindGameObjectWithTag("GroundPlane");
            return groundPlane != null;
        }

        public static GameObject GetLookDevContainer()
        {
            return GameObject.FindWithTag("AssetHolder");
        }

        public static GameObject GetLookDevLightContainer()
        {
            return GameObject.FindWithTag("LightRig");
        }

        public static GameObject GetLookDevContainer(Scene correspondingScene)
        {
            foreach (GameObject GO in correspondingScene.GetRootGameObjects())
            {
                if (GO.tag.Equals("AssetHolder"))
                    return GO;
            }

            return null;
        }

        public static GameObject GetLookDevLightContainer(Scene correspondingScene)
        {
            foreach (GameObject GO in correspondingScene.GetRootGameObjects())
            {
                if (GO.tag.Equals("LightRig"))
                    return GO;
            }

            return null;
        }

        public static GameObject SetHeroAsset(Object target, bool usePrefabs = true)
        {
            if (target == null)
                return null;

            if (PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.NotAPrefab ||
                PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.MissingAsset)
                return null;

            var container = GetLookDevContainer();
            if (container == null) //NOTE: The first time the project loads container comes back null
                return null;

            for (int i = container.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(container.transform.GetChild(i).gameObject);
            }

            GameObject o;

            if (PrefabUtility.IsPartOfPrefabAsset(target) && usePrefabs)
            {
                o = PrefabUtility.InstantiatePrefab(target, container.transform) as GameObject;
            }
            else
            {
                o = GameObject.Instantiate(target, Vector3.zero, Quaternion.identity,
                    container.transform) as GameObject;
            }

            OnHeroAssetReplacedEvent?.Invoke();

            // Silly hack to fix offset rotation that happens when swapping scenes and respawning the asset holder
            for (int i = 0; i < container.transform.childCount; i++)
            {
                container.transform.GetChild(i).localRotation = Quaternion.identity;
            }

            return o;
        }

        public static Bounds GetBoundsWithChildren(GameObject gameObject)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(false);

            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();

            // Start from 1 because we've already encapsulated renderers[0]
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        static bool IsDraggedObjectModel()
        {
            if (DragAndDrop.paths.Length == 0 && DragAndDrop.objectReferences.Length > 0)
            {
                Object sampleObj = DragAndDrop.objectReferences[0];
                // Since there is no possibility that users multi-select different types from the Search Browser

                if (sampleObj.GetType() != typeof(Material) &&
                    sampleObj.GetType() != typeof(Texture))
                {
                    return true;
                }
                else
                    return false;
            }

            return false;
        }

        static bool IsLightScene(Object obj)
        {
            if (obj.GetType() == typeof(SceneAsset))
                return true;
            else
                return false;
        }

        public static bool IsHdri(Object obj)
        {
            if (obj.GetType() == typeof(Cubemap))
                return true;
            else
                return false;
        }

        public static bool IsHdri(string extension)
        {
            string ext = extension.ToLower();

            if (ext == ".hdr" || ext == ".exr")
                return true;

            return false;
        }

        public static bool IsSkybox(Object obj)
        {
            if (obj.GetType() == typeof(Material))
            {
                Material mat = obj as Material;

                if (mat.shader.name.Equals("Skybox/Cubemap"))
                    return true;
            }

            return false;
        }

        static bool IsLightPrefab(Object obj)
        {
            if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.MissingAsset &&
                PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(assetPath))
                    return false;

                if (assetPath.ToLower().StartsWith("Assets/LookDev/Lights".ToLower()))
                    return true;
            }

            return false;
        }


        static bool IsAnimationClip(Object obj)
        {
            if (obj.GetType() == typeof(AnimationClip))
                return true;
            return false;
        }


        static bool ValidateDirectionalLights()
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>(true);

            if (lights.Length > 1)
            {
                int castShadowCount = 0;

                foreach (Light lgt in lights)
                {
                    if (lgt.type == LightType.Directional)
                    {
                        if (lgt.shadows != LightShadows.None)
                            castShadowCount++;
                    }
                }

                if (castShadowCount > 1)
                {
                    Debug.LogWarning(
                        "You can only have shadows on one directional light. The second one will have shadows disabled.");
                    return false;
                }
            }

            return true;
        }

        static bool CheckNoShadowCasting()
        {
            bool noShadowCasting = true;

            Light[] lights = GameObject.FindObjectsOfType<Light>(true);

            foreach (Light lgt in lights)
            {
                if (lgt.type == LightType.Directional)
                {
                    if (lgt.shadows != LightShadows.None)
                    {
                        noShadowCasting = false;
                    }
                }
            }

            return noShadowCasting;
        }


        static void DisableCastShadow(GameObject targetGo)
        {
            if (targetGo == null)
                return;

            Light[] lgts = targetGo.GetComponentsInChildren<Light>(true);

            foreach (Light lgt in lgts)
            {
                if (lgt.type == LightType.Directional && lgt.shadows != LightShadows.None)
                {
                    lgt.shadows = LightShadows.None;
                    break;
                }
            }
        }

        static void EnableCastShadow(GameObject targetGo)
        {
            if (targetGo == null)
                return;

            Light[] lgts = targetGo.GetComponentsInChildren<Light>(true);

            foreach (Light lgt in lgts)
            {
                if (lgt.type == LightType.Directional && lgt.shadows == LightShadows.None)
                {
                    lgt.shadows = LightShadows.Soft;
                    break;
                }
            }
        }

        /// <summary>
        /// Tracks Drag n Drop over a UnityEditor, must be called explicitly inside OnGUI().
        /// </summary>
        /// <returns>True, if drag was consumed</returns>
        public static void DragDrop()
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (IsDraggedObjectModel())
                    Event.current
                        .Use(); // Hides the ScenView's raycast-snap model preview. but want to see the preview when the material is dragged
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                // To consume drag data.
                DragAndDrop.AcceptDrag();

                // GameObjects from hierarchy & Quicksearch.
                if (DragAndDrop.paths.Length == 0 && DragAndDrop.objectReferences.Length > 0)
                {
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (IsLightScene(obj)) // if it's sceneAsset
                        {
                            string scenePath = AssetDatabase.GetAssetPath(obj);
                            LightingPresetSceneChanger.TransitionToScene(scenePath);
                            int currentLightIndex = LightingPresetSceneChanger.GetLightSceneIndex(scenePath);
                            LightingPresetSceneChanger.SetLightSceneIndex(currentLightIndex);
                        }
                        else if (IsHdri(obj)) // if it's Hdri
                        {
                            // it seems be commented for supporting both of RPs
                            //HdriTool.SwapCurrentHDRISky(obj as Cubemap);
                            OnSwapHDRi?.Invoke(obj as Cubemap);
                        }
                        else if (IsSkybox(obj)) // if it's skybox on URP
                        {
                            OnSwapSkybox?.Invoke(obj as Material);
                        }
                        else if (IsLightPrefab(obj))
                        {
                            Transform lightHolder = GetLookDevLightContainer().transform;
                            var instantiatedLgtObject = PrefabUtility.InstantiatePrefab(obj, lightHolder);

                            GameObject instantiatedLgtGo = instantiatedLgtObject as GameObject;

                            Vector3 initPos = (obj as GameObject).transform.position;

                            instantiatedLgtGo.transform.position = initPos;

                            if (ValidateDirectionalLights() == false)
                                DisableCastShadow(instantiatedLgtGo);
                            else if (CheckNoShadowCasting() == true)
                                EnableCastShadow(instantiatedLgtGo);

                            Selection.SetActiveObjectWithContext(instantiatedLgtObject, null);
                        }
                        else if (IsAnimationClip(obj))
                        {
                            AnimationTool.AddAnimator();

                            AnimationTool.StopAnimator();

                            AnimationTool.LoadAnimation(obj as AnimationClip);

                            AnimationTool.PlayAnimator();
                        }
                        else if (PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab &&
                                 PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.MissingAsset)
                        {
                            Transform assetHolder = GetLookDevContainer().transform;
                            var instantiatedObject = PrefabUtility.InstantiatePrefab(obj);

                            GameObject target = (instantiatedObject as GameObject);

                            target.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                            target.transform.SetParent(assetHolder);
                            Selection.SetActiveObjectWithContext(instantiatedObject, null);
                            SceneView.lastActiveSceneView.FrameSelected();

                        }
                        else
                        {
                            Transform assetHolder = GetLookDevContainer().transform;
                            var instantiatedObject =
                                Object.Instantiate(obj, Vector3.zero, Quaternion.identity, assetHolder);
                            Selection.SetActiveObjectWithContext(instantiatedObject, null);
                            SceneView.lastActiveSceneView.FrameSelected();
                        }
                    }

                    if (IsDraggedObjectModel())
                        Event.current
                            .Use(); // Consumes the event to prevent downstream errors with the Editor's core DragDrop logic.
                }
                // Object outside project. It mays from File Explorer (Finder in OSX).
                else if (DragAndDrop.paths.Length > 0 && DragAndDrop.objectReferences.Length == 0)
                {
                    // Clear a List of the imported Assets 
                    DragDropModelPostProcessor.latestImportedAssets.Clear();

                    List<string> fullAssetList = new List<string>();

                    foreach (string path in DragAndDrop.paths)
                    {
                        // what if the path is folder?
                        if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            foreach (string SupportedFormatExtension in SupportedFormatExtensions)
                            {
                                string[] files = Directory.GetFiles(path, $"*{SupportedFormatExtension}",
                                    SearchOption.AllDirectories);

                                foreach (string file in files)
                                    fullAssetList.Add(file);
                            }
                        }
                        else
                            fullAssetList.Add(path);
                    }

                    if (fullAssetList.Count != 0)
                        Import(fullAssetList.ToArray());
                }
                // Unity Assets including folder.
                else if (DragAndDrop.paths.Length == DragAndDrop.objectReferences.Length)
                {
                    Debug.Log("UnityAsset");
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        var obj = DragAndDrop.objectReferences[i];
                        string path = DragAndDrop.paths[i];
                        Debug.Log(obj.GetType().Name);

                        // Folder.
                        if (obj is DefaultAsset)
                        {
                            Debug.Log(path);
                        }
                        // C# or JavaScript.
                        else if (obj is MonoScript)
                        {
                            Debug.Log(path + "\n" + obj);
                        }
                        else if (obj is Texture2D)
                        {
                        }
                    }
                }
                // Log to make sure we cover all cases.
                else
                {
                    Debug.Log("Out of reach");
                    Debug.Log("Paths:");
                    foreach (string path in DragAndDrop.paths)
                    {
                        Debug.Log("- " + path);
                    }

                    Debug.Log("ObjectReferences:");
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        Debug.Log("- " + obj);
                    }
                }
            }
        }

        public static bool IsSupportedExtension(string extension)
        {
            return (SupportedFormatExtensions.Contains(extension.ToLower()));
        }

        public static bool IsModel(string extension)
        {
            if (extension.ToLower() == ".fbx" || extension.ToLower() == ".obj")
                return true;
            else
                return false;
        }

        /// <summary>
        /// Import external files into the project. All files will be copied to the Assets root directory.
        /// </summary>
        /// <param name="paths">File path(s) to import.</param>
        public static List<string> Import(params string[] paths)
        {
            bool foundModel = false;

            var imports = new List<string>(paths.Length);

            // You can use StartAssetEditing() and StopAssetEditing(), to group any imports that happen inbetween:
            // This will speed up import considerably, when importing a large number of assets.
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string sourcePath in paths)
                {
                    var sourceFile = new FileInfo(sourcePath);
                    // Ignore invalid paths
                    if (!sourceFile.Exists)
                    {
                        continue;
                    }

                    if (!IsSupportedExtension(sourceFile.Extension))
                    {
                        Debug.LogError($"Files with extension [{sourceFile.Extension}] are not supported.");
                        continue;
                    }

                    // Path to import file to, in the Assets root directory

                    string assetPath = string.Empty;
                    string assetDir = string.Empty;

                    if (IsModel(sourceFile.Extension)) // Model
                    {
                        assetPath = Path.Combine("Assets/", LookDevSubdirectoryForModel, Path.GetFileName(sourcePath));
                        foundModel = true;
                    }
                    else // Texture
                    {
                        /*
                        if (IsHdri(sourceFile.Extension))
                            assetPath = Path.Combine("Assets/", LookDevSubdirectoryForHdri, Path.GetFileName(sourcePath));
                        */

                    	assetPath = Path.Combine("Assets/", LookDevSubdirectoryForTexture, Path.GetFileName(sourcePath));
                    }

                    // Normalize path separators due to differing operating systems.
                    assetPath = assetPath.Replace('\\', '/');

                    assetDir = assetPath.Replace(Path.GetFileName(assetPath), string.Empty);


                    // Check Directory existence
                    if (!Directory.Exists(assetDir))
                    {
                        Directory.CreateDirectory(assetDir);
                    }

                    // Copy the file from the source path to the target asset path
                    sourceFile.CopyTo(assetPath, true);

                    // Notify we are about to import a specific path
                    OnDragAndDropPreImportEvent?.Invoke(assetPath);

                    // Manually trigger an import for the new asset
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                    // Notify we have finalized a single specific import
                    OnDragAndDropPostImportEvent?.Invoke(assetPath);

                    // Collect the import paths.
                    imports.Add(assetPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (foundModel && DragDropModelPostProcessor.latestImportedAssets.Count != 0)
            {
                /*
                if (EditorUtility.DisplayDialog("Material set-up",
                    $"Do you want to open the Texture Allocator to set up materials now?\nThe total number of imported Assets is: {DragDropModelPostProcessor.latestImportedAssets.Count.ToString()}, including newly generated Materials.",
                    "Yes", "No"))
                    */
                if (ProjectSettingWindow.projectSetting.AutoPopulateTextures == true)
                {
                    EditorCoroutineUtility.StartCoroutine(
                        TextureLinkBrowser.Inst.InitTextureLinkBrowserOnImportingWithDelay(), TextureLinkBrowser.Inst);
                }
                else
                {
                    DragDropModelPostProcessor.latestImportedAssets.Clear();
                }
            }

            return imports;
        }

        public static TextElement CreateTextHeader(string text)
        {
            var header = new TextElement();
            header.text = text;
            return header;
        }

        //TMP_EditorUtility.cs
        public static EditorWindow GetGameview()
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            return EditorWindow.GetWindow(type);
        }


        public static EditorWindow GetSceneView()
        {
            return EditorWindow.GetWindow<SceneView>();
        }


        public static void UpdateRotation(Transform t, float deltaTime, float speed)
        {
            var rotation = Quaternion.AngleAxis(speed * deltaTime, Vector3.up);
            t.rotation *= rotation;
        }

        #region Camera

        public static void ResetLookDevCamera(GameObject basis = null)
        {
            if (basis == null)
            {
                basis = GetLookDevContainer();
            }

            var vCam = GetLookDevCam();
            float resetDist = CalculateMinimumDistance(vCam.Camera, basis, out Vector3 centroid);

            vCam.Camera.transform.position = centroid + Vector3.forward * resetDist;

            SceneView sv = SceneView.lastActiveSceneView;
            sv.Frame(GetBoundsWithChildren(basis));
        }

        static float CalculateMinimumDistance(Camera vCam, GameObject basis, out Vector3 centroidWorldPos)
        {
            // https://forum.unity.com/threads/fit-object-exactly-into-perspective-cameras-field-of-view-focus-the-object.496472/
            Bounds b = GetBoundsWithChildren(basis);
            centroidWorldPos = b.center;
            const float marginPercentage = 0.8f;
            float maxExtent = b.extents.magnitude;
            float minDistance = (maxExtent * marginPercentage) / Mathf.Sin(Mathf.Deg2Rad * vCam.fieldOfView / 2f);
            return minDistance;
        }

        public static LookDevCamera GetLookDevCam()
        {
            var lookDevCameras = GameObject.FindObjectsOfType<LookDevCamera>();
            foreach (var cam in lookDevCameras)
            {
                if (cam.Camera.CompareTag("MainCamera"))
                {
                    return cam;
                }
            }

            Debug.Assert(false, "No lookdev cam exists in the scene");

            return null;
        }

        public static void CopyCameraTransformToSceneViewCamera(Camera camera, SceneView sceneView)
        {
            const float kSqrt2 = 1.41421356f;

            var sceneViewAspect = sceneView.camera.aspect;

            var hFov = Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect);
            var vFov = Camera.HorizontalToVerticalFieldOfView(hFov, sceneViewAspect);

            sceneView.camera.fieldOfView = vFov;
            var cameraTransform = camera.transform;

            var newSize = 0.0f;
            if (camera.orthographic)
                newSize = (camera.aspect > sceneViewAspect ? camera.aspect / sceneViewAspect : 1.0f) *
                          kSqrt2 * Mathf.Sqrt(sceneViewAspect) * camera.orthographicSize;
            sceneView.LookAt(cameraTransform.position, cameraTransform.rotation, newSize, false, true);
        }

        public static void CopySceneViewToCameraTransform(SceneView sceneView, Camera camera)
        {
            var targetCameraTransform = camera.transform;
            var sceneViewCameraTransform = sceneView.camera.transform;
            targetCameraTransform.position = sceneViewCameraTransform.position;
            targetCameraTransform.rotation = sceneViewCameraTransform.rotation;
            camera.orthographic = sceneView.camera.orthographic;
            camera.orthographicSize = sceneView.camera.orthographicSize;

            var sceneViewAspect = sceneView.camera.aspect;
            var hFov = Camera.VerticalToHorizontalFieldOfView(sceneView.camera.fieldOfView, sceneViewAspect);
            var vFov = Camera.HorizontalToVerticalFieldOfView(hFov, camera.aspect);
            camera.fieldOfView = vFov;
        }

        public static void LoadCameraPosition(int index)
        {
            var lookDevCamera = GetLookDevCam();
            lookDevCamera.LoadCameraPreset(index);
            CopyCameraTransformToSceneViewCamera(lookDevCamera.Camera, SceneView.lastActiveSceneView);
            OnCameraPositionLoaded?.Invoke(index);
        }

        public static void SaveCameraPosition(int cameraIndex = -1)
        {
            var lookDevCamera = GetLookDevCam();
            if (cameraIndex == -1)
            {
                lookDevCamera.SaveCameraPosition();
            }
            else
            {
                lookDevCamera.SaveCameraPosition(cameraIndex);
            }
        }

        public static void ShowSavedCameraPositionNotification(int index = -1)
        {
            if (index == -1)
            {
                index = GetLookDevCam().CurrentCameraIndex + 1;
            }

            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Saved Camera {index} Position"), 2);
        }

        #endregion

        #region ToolMode

        public static void SetToolMode(Tool toolMode)
        {
            var unityEditorAssembly = typeof(EditorWindow).Assembly;
            Type toolbarType = unityEditorAssembly.GetType("UnityEditor.Toolbar");
            Type toolsType = unityEditorAssembly.GetType("UnityEditor.Tools");

            var repaintToolbarMethodInfo =
                toolbarType.GetMethod("RepaintToolbar", BindingFlags.Static | BindingFlags.NonPublic);
            var resetGlobalHandleRotationMethodInfo = toolsType.GetMethod("ResetGlobalHandleRotation",
                BindingFlags.Static | BindingFlags.NonPublic);

            Tools.current = toolMode;
            repaintToolbarMethodInfo.Invoke(null, new object[] { });
            resetGlobalHandleRotationMethodInfo.Invoke(null, new object[] { });
        }

        #endregion

        public static Rect GetMainWindowRect()
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.ContainerWindow");
            FieldInfo pixelRectField = type.GetField("m_PixelRect", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo showModeField = type.GetField("m_ShowMode", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var containerWindow in Resources.FindObjectsOfTypeAll(type))
            {
                int showMode = (int)showModeField.GetValue(containerWindow);
                if (showMode == 4)
                {
                    return (Rect)pixelRectField.GetValue(containerWindow);
                }
            }

            Debug.LogError("LookDevHelpers GetMainWindowRect: MainWindow not found");
            return new Rect(-1, -1, -1, -1);
        }

        public static void SetMainWindowRect(Rect rect)
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.ContainerWindow");
            FieldInfo showModeField = type.GetField("m_ShowMode", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo positionProperty = type.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);

            foreach (var containerWindow in Resources.FindObjectsOfTypeAll(type))
            {
                int showMode = (int)showModeField.GetValue(containerWindow);
                if (showMode == 4)
                {
                    positionProperty.SetValue(containerWindow, rect);
                }
            }
        }
    }
}