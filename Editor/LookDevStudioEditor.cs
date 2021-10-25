using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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
    public static class LookDevStudioEditor
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            if (!LookDevPreferences.instance.IsRenderPipelineInitialized)
            {
                LookDevWelcomeWindow.ShowWindow();
            }
            else
            {
                if (File.Exists(PathToLookDevDisableAutoLaunchFile))
                    return;


                // Enable LookDev on the next Editor tick.
                void LoadLookDevOnReload()
                {
                    EditorApplication.update -= LoadLookDevOnReload;
                    EnableLookDev();
                }

                EditorApplication.update += LoadLookDevOnReload;
            }
        }

        static LookDevPreferences _lookDevPreferences;

        public const string PathToLookDevDisableAutoLaunchFile = "Library/LookDevDisableAutoLaunch";


        public static bool IsHDRP()
        {
            string currentRP = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.GetType().Name;

            // UniversalRenderPipelineAsset
            // HDRenderPipelineAsset

            if (currentRP.ToLower() == "HDRenderPipelineAsset".ToLower())
                return true;
            else
                return false;
        }


        [MenuItem("LookDev Studio/Enable")]
        public static void EnableLookDev()
        {
            if (_lookDevPreferences == null)
                _lookDevPreferences = LookDevPreferences.instance;

            if (!_lookDevPreferences.IsRenderPipelineInitialized)
            {
                Debug.LogError("LookDevStudio hasn't been initialized yet");
                return;
            }

            _lookDevPreferences.EnableOrbit = false;
            _lookDevPreferences.EnableTurntable = false;
            _lookDevPreferences.CurrentCameraIndex = -1;

            EditorApplication.update += OnLookDevModeUpdate;
            SceneView.duringSceneGui += OnDuringSceneGuiForDragDrop;

            ModeService.ChangeModeById("lookdevstudio");

            LookDevShortcutsOverlay.Enable();

            LightingPresetSceneChanger.Initialize();

            LookDevSearchHelpers.Initialize();

            LookDevSceneMenu.RegisterSceneMenu();

            //Hide the LightRig components
            var lightRigs = GameObject.FindObjectsOfType<ILightRig>();
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
            LookDevShortcutsOverlay.Update();
        }

        private static void OnDuringSceneGuiForDragDrop(SceneView sceneView)
        {
            LookDevShortcutsOverlay.DragDrop();
            LookDevHelpers.DragDrop();
        }

        [CommandHandler("Commands/Return to Unity", CommandHint.Menu | CommandHint.Validate)]
        private static void DisableLookDev(CommandExecuteContext context)
        {
            ModeService.ChangeModeById("default");
            LightingPresetSceneChanger.Shutdown();

            EditorApplication.update -= OnLookDevModeUpdate;
            SceneView.duringSceneGui -= OnDuringSceneGuiForDragDrop;

            LookDevShortcutsOverlay.Disable();

            LookDevSearchHelpers.UnregisterCallbacks();

            LookDevSceneMenu.UnregisterSceneMenu();
        }

        [CommandHandler("Commands/ResetLookDevView", CommandHint.Menu | CommandHint.Validate)]
        private static void ResetLookDevView(CommandExecuteContext context)
        {
            LookDevHelpers.ResetLookDevCamera();
        }


        [CommandHandler("Commands/Create Lighting Preset", CommandHint.Menu | CommandHint.Validate)]
        public static void CreateLightingPreset(CommandExecuteContext context)
        {
            string scenePath = LightingPresetSceneChanger.GetCurrentLightScenePath();

            if (!string.IsNullOrEmpty(scenePath))
            {
                if (GameObject.FindObjectsOfType<Light>().Length == 0)
                {
                    Debug.LogError("Could not find Lights in the scene.");
                    return;
                }

                CreateLightPresetWindow.ShowWindow(scenePath);
            }
            else
                Debug.LogError("Could not find the current Lighting Scene.");
        }


        [CommandHandler("Commands/Create Light Group", CommandHint.Menu | CommandHint.Validate)]
        public static void CreateLightGroup(CommandExecuteContext context)
        {
            bool isFoundLightComp = false;

            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogError("Need to select GameObjects to be groupped.");
                return;
            }

            foreach (GameObject go in Selection.gameObjects)
            {
                Light lgtComp = go.GetComponentInChildren<Light>();
                if (lgtComp != null)
                {
                    isFoundLightComp = true;
                    break;
                }
            }

            if (isFoundLightComp)
                CreateLightGroupWindow.ShowWindow();
            else
                Debug.LogError("Could not find GameObject with Light Components.");
        }


        [CommandHandler("Commands/Set as Main Directional Light", CommandHint.Menu | CommandHint.Validate)]
        public static void SetAsMainDirectionLight(CommandExecuteContext context)
        {
            if (Selection.gameObjects.Length == 0 || Selection.gameObjects.Length > 1)
                return;

            // Check about whether the selected light is the directional light
            Light selectedLight = Selection.gameObjects[0].GetComponent<Light>();

            if (selectedLight == null)
            {
                Debug.LogError("You need to select the GameObject that has Light Component.");
                return;
            }

            if (selectedLight.type != LightType.Directional)
            {
                Debug.LogError("the type of the selected Light is not Directional Light");
                return;
            }

            // Hide All ShadowCasting of the directional Lights
            Light[] allLights = GameObject.FindObjectsOfType<Light>();
            foreach (Light currentLight in allLights)
            {
                if (currentLight.type == LightType.Directional && currentLight.shadows != LightShadows.None)
                    currentLight.shadows = LightShadows.None;
            }

            // Enable shadowCasting on the selected light
            selectedLight.shadows = LightShadows.Soft;

            // Link the light to LightRig
            ILightRig lRigInfo = GameObject.FindGameObjectWithTag("LightRig")?.GetComponent<ILightRig>();

            if (lRigInfo != null)
                lRigInfo.LightDirection = selectedLight.gameObject.transform;
        }


        [CommandHandler("Commands/New LDS Project", CommandHint.Menu | CommandHint.Validate)]
        public static void NewLdsProject(CommandExecuteContext context)
        {
            string projPath = EditorUtility.SaveFilePanelInProject("New LDS Project", "ProjectName", "asset", "Create new Project setting file", Path.GetFullPath(ProjectSettingWindow.GetLookDevProjectFolder()));

            if (string.IsNullOrEmpty(projPath))
                return;

            projPath = AssetDatabase.GenerateUniqueAssetPath(projPath);

            ProjectSettingWindow.CreateNewProjectSettings(Path.GetFileNameWithoutExtension(projPath));

        }

        [MenuItem("LookDev Studio/Import Unity Package", editorModes = new[] {"lookdevstudio"})]
        public static void ImportUnityPackage()
        {
            var lastAssetImportDir = EditorPrefs.GetString("LastAssetImportDir", Application.dataPath);
            var unityPackage = EditorUtility.OpenFilePanel("Import Unity Package", lastAssetImportDir, "unitypackage");
            if (!string.IsNullOrEmpty(unityPackage))
            {
                AssetDatabase.ImportPackage(unityPackage, true);
            }
        }

        [CommandHandler("Commands/Load LDS Project", CommandHint.Menu | CommandHint.Validate)]
        public static void LoadLdsProject(CommandExecuteContext context)
        {
            string projPath = EditorUtility.OpenFilePanel("Load LDS Project", Path.GetFullPath(ProjectSettingWindow.GetLookDevProjectFolder()), "asset");

            string rootPath = (new DirectoryInfo(Application.dataPath + "/../")).FullName.Replace("\\", "/");

            projPath = projPath.Replace(rootPath, string.Empty);

            if (AssetDatabase.LoadAssetAtPath<Object>(projPath) != null)
            {
                EditorWindow.GetWindow<ProjectSettingWindow>()?.ApplyProjectSettings(projPath);
            }
        }

        public static void MakePrefab()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Need to select GameObjects in the Hierarchy");
                return;
            }

            // Filter Groups "Lights", "Camera", "PostProcess" and "Models"
            if (Selection.gameObjects[0].transform?.parent == null)
            {
                Debug.LogError($"Could not save the GameObject : {Selection.gameObjects[0].name}");
                return;
            }

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.gameObjects[0]);

            string savePath = $"{assetPath.Replace(Path.GetExtension(assetPath), string.Empty)}.prefab";


            if (ProjectSettingWindow.projectSetting.MakePrefabsForAllMeshes)
            {
                if (Path.GetFileName(savePath).ToUpper().StartsWith(ProjectSettingWindow.projectSetting.PrefabPrefix.ToUpper()) == false && ProjectSettingWindow.projectSetting.PrefabPrefix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath), $"{ProjectSettingWindow.projectSetting.PrefabPrefix}{Path.GetFileNameWithoutExtension(savePath)}.prefab");

                if (Path.GetFileName(savePath).ToUpper().EndsWith(ProjectSettingWindow.projectSetting.PrefabPostfix.ToUpper()) == false && ProjectSettingWindow.projectSetting.PrefabPostfix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath), $"{Path.GetFileNameWithoutExtension(savePath)}{ProjectSettingWindow.projectSetting.PrefabPostfix}.prefab");
            }


            if (string.IsNullOrEmpty(assetPath) == false)
            {
                savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
                Selection.gameObjects[0].name = Path.GetFileNameWithoutExtension(savePath);

                PrefabUtility.SaveAsPrefabAssetAndConnect(Selection.gameObjects[0], savePath, InteractionMode.AutomatedAction);

                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Prefab Saved : {savePath}"), 4f);

                LookDevSearchHelpers.SwitchCurrentProvider(2);

                AssetDatabase.SaveAssets();
            }

        }

        public static void MakePrefabByUserInput()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Need to select GameObjects in the Hierarchy");
                return;
            }

            // Filter Groups "Lights", "Camera", "PostProcess" and "Models"
            if (Selection.gameObjects[0].transform?.parent == null)
            {
                Debug.LogError($"Could not save the GameObject : {Selection.gameObjects[0].name}");
                return;
            }

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.gameObjects[0]);

            string savePath = $"{assetPath.Replace(Path.GetExtension(assetPath), string.Empty)}.prefab";

            if (ProjectSettingWindow.projectSetting.MakePrefabsForAllMeshes)
            {
                if (Path.GetFileName(savePath).ToUpper().StartsWith(ProjectSettingWindow.projectSetting.PrefabPrefix.ToUpper()) == false && ProjectSettingWindow.projectSetting.PrefabPrefix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath), $"{ProjectSettingWindow.projectSetting.PrefabPrefix}{Path.GetFileNameWithoutExtension(savePath)}.prefab");

                if (Path.GetFileName(savePath).ToUpper().EndsWith(ProjectSettingWindow.projectSetting.PrefabPostfix.ToUpper()) == false && ProjectSettingWindow.projectSetting.PrefabPostfix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath), $"{Path.GetFileNameWithoutExtension(savePath)}{ProjectSettingWindow.projectSetting.PrefabPostfix}.prefab");
            }

            if (string.IsNullOrEmpty(assetPath) == false)
            {
                savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

                PrefabSaveAsWindow prefabSaveAsWindow = EditorWindow.GetWindow<PrefabSaveAsWindow>("Save As (Prefab)", true);
                prefabSaveAsWindow.InitPrefabSaveAsWindow(Selection.gameObjects[0], savePath);

                prefabSaveAsWindow.ShowUtility();
                prefabSaveAsWindow.Focus();

            }

        }



        [CommandHandler("Commands/Save", CommandHint.Menu | CommandHint.Validate)]
        public static void SavePrefab(CommandExecuteContext context)
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Need to select GameObjects in the Hierarchy");
                return;
            }

            // Filter Groups "Lights", "Camera", "PostProcess" and "Models"
            if (Selection.gameObjects[0].transform?.parent == null)
            {
                Debug.LogError($"Could not save the GameObject : {Selection.gameObjects[0].name}");
                return;
            }
            
            if (PrefabUtility.GetPrefabAssetType(Selection.gameObjects[0]) == PrefabAssetType.Model)
            {
                // Make this as a Prefab
                if (EditorUtility.DisplayDialog("Convert to Prefab", "Do you want to convert the selected GameObjec to the Prefab?", "Yes", "No"))
                {
                    MakePrefab();
                }
                else
                {
                    Debug.LogWarning("Could not save changes on the model. Need to convert the model to Prefab.");
                }

                return;
            }

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.gameObjects[0]);

            if (string.IsNullOrEmpty(assetPath) == false)
            {
                if (EditorUtility.DisplayDialog("Overwrite Prefab", "Do you want to overwirte the existing Prefab?", "Yes", "No") == false)
                    return;

                PrefabUtility.ApplyPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(Selection.gameObjects[0]), InteractionMode.AutomatedAction);
                LookDevSearchHelpers.SwitchCurrentProvider(2);

                AssetDatabase.SaveAssets();

                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Prefab Saved : {assetPath}"), 4f);
            }

        }

        [CommandHandler("Commands/Save As", CommandHint.Menu | CommandHint.Validate)]
        public static void SaveAsPrefab(CommandExecuteContext context)
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Need to select GameObjects in the Hierarchy");
                return;
            }

            // Filter Groups "Lights", "Camera", "PostProcess" and "Models"
            if (Selection.gameObjects[0].transform?.parent == null)
            {
                Debug.LogError($"Could not save the GameObject : {Selection.gameObjects[0].name}");
                return;
            }

            MakePrefabByUserInput();

        }

    }
}