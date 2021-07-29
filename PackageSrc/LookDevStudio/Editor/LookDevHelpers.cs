using System;
using System.IO;
using System.Collections.Generic;
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
            new List<string>() {".fbx", ".obj", ".tga", ".png", ".exr", ".tif", ".tiff", ".psd"};
        
        public const string LookDevSubdirectoryForModel = "LookDev/Models";
        public const string LookDevSubdirectoryForTexture = "LookDev/Textures";
        public const string LookDevSubdirectoryForMaterial = "LookDev/Materials";

        public static event Action<string> OnDragAndDropPreImportEvent;
        public static event Action<string> OnDragAndDropPostImportEvent;
        public static event Action OnHeroAssetReplacedEvent;

        public static void RecordLastHeroAsset(UnityEngine.Object obj)
        {
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
            EditorPrefs.SetString(LastHeroAssetGUIDKey, guid);
        }

        public static UnityEngine.Object GetLastHeroAsset()
        {
            var guid = EditorPrefs.GetString(LastHeroAssetGUIDKey, String.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static void ReplaceHeroAsset(GameObject target)
        {
            SetHeroAsset(target);
            ResetLookDevCamera(target);
            RecordLastHeroAsset(target);
        }

        public static Camera GetLookDevCam()
        {
            var cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.tag == "MainCamera")
                {
                    return cam;
                }
            }

            Debug.Assert(false, "No lookdev cam exists in the scene");

            return null;
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
        public static GameObject GetLookDevContainer(Scene correspondingScene)
        {
            foreach (GameObject GO in correspondingScene.GetRootGameObjects())
            {
                if (GO.tag.Equals("AssetHolder"))
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

        public static void ResetLookDevCamera(GameObject basis = null)
        {
            if (basis == null)
            {
                basis = GetLookDevContainer();

                // Try to get the loaded model, if available.
                if (basis.transform.childCount > 0)
                    basis = basis.transform.GetChild(0).gameObject;
            }

            var vCam = GetLookDevCam();
            float resetDist = CalculateMinimumDistance(vCam, basis, out Vector3 centroid);

            //Turntable.Controls.Reset(resetDist);
            //Turntable.Controls.Origin = centroid;
            vCam.transform.position = centroid + Vector3.forward * resetDist;
            
            SceneView sv = SceneView.lastActiveSceneView;
            sv.Frame(GetBoundsWithChildren(basis));
        }

        private static float CalculateMinimumDistance(Camera vCam, GameObject basis, out Vector3 centroidWorldPos)
        {
            // https://forum.unity.com/threads/fit-object-exactly-into-perspective-cameras-field-of-view-focus-the-object.496472/
            Bounds b = GetBoundsWithChildren(basis);
            centroidWorldPos = b.center;
            const float marginPercentage = 0.8f;
            float maxExtent = b.extents.magnitude;
            float minDistance = (maxExtent * marginPercentage) / Mathf.Sin(Mathf.Deg2Rad * vCam.fieldOfView / 2f);
            return minDistance;
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

                if (sampleObj.GetType() != typeof(UnityEngine.Material) && sampleObj.GetType() != typeof(UnityEngine.Texture))
                {
                    return true;
                }
                else
                    return false;
            }

            return false;
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
                    Event.current.Use(); // Hides the ScenView's raycast-snap model preview. but want to see the preview when the material is dragged
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
                        Debug.Log("- " + obj);
                        Transform assetHolder = GetLookDevContainer().transform;
                        var instantiatedObject = Object.Instantiate(obj, Vector3.zero, Quaternion.identity, assetHolder);
                        Selection.SetActiveObjectWithContext(instantiatedObject, null);
                        SceneView.lastActiveSceneView.FrameSelected();
                    }

                    if (IsDraggedObjectModel())
                        Event.current.Use(); // Consumes the event to prevent downstream errors with the Editor's core DragDrop logic.
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
                                string[] files = Directory.GetFiles(path, $"*{SupportedFormatExtension}", SearchOption.AllDirectories);

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

                if (IsModel(sourceFile.Extension))
                {
                    assetPath = Path.Combine("Assets/", LookDevSubdirectoryForModel, Path.GetFileName(sourcePath));
                    foundModel = true;
                }
                else
                {
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

            AssetDatabase.StopAssetEditing();


            if (foundModel && DragDropModelPostProcessor.latestImportedAssets.Count != 0)
            {
                if (EditorUtility.DisplayDialog("Material set-up", $"Do you want to open the Texture Allocator to set up materials now?\nThe total number of imported Assets is: {DragDropModelPostProcessor.latestImportedAssets.Count.ToString()}, including newly generated Materials.", "Yes", "No"))
                {
                    Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(TextureLinkBrowser.Inst.InitTextureLinkBrowserOnImportingWithDelay(), TextureLinkBrowser.Inst);
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
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type type = assembly.GetType("UnityEditor.GameView");
            return EditorWindow.GetWindow(type);
        }

        public static void UpdateRotation(Transform t, float deltaTime, float speed)
        {
            var rotation = Quaternion.AngleAxis(speed * deltaTime, Vector3.up);
            t.rotation *= rotation;
        }
    }
}