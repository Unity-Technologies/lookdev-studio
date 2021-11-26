using System.IO;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Rendering;

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
#if UNITY_EDITOR_OSX
            //NOTE: This block fixes the editor resizing in MacOS inside the LookDev mode.
            if (!EditorPrefs.HasKey("UnityEditor.Search.QuickSearchw"))
                EditorPrefs.SetInt("UnityEditor.Search.QuickSearchw", 0);

            if (!EditorPrefs.HasKey("UnityEditor.Search.QuickSearchx"))
                EditorPrefs.SetInt("UnityEditor.Search.QuickSearchx", 0);

            if (!EditorPrefs.HasKey("UnityEditor.Search.QuickSearchy"))
                EditorPrefs.SetInt("UnityEditor.Search.QuickSearchy", 0);

            if (!EditorPrefs.HasKey("UnityEditor.Search.QuickSearchh"))
                EditorPrefs.SetInt("UnityEditor.Search.QuickSearchh", 0);

            if (!EditorPrefs.HasKey("UnityEditor.Search.QuickSearchz"))
                EditorPrefs.SetInt("UnityEditor.Search.QuickSearchz", 0);

            if (!EditorPrefs.HasKey("MainView_lookdevstudiow"))
                EditorPrefs.SetInt("MainView_lookdevstudiow", 0);

            if (!EditorPrefs.HasKey("MainView_lookdevstudiox"))
                EditorPrefs.SetInt("MainView_lookdevstudiox", 0);

            if (!EditorPrefs.HasKey("MainView_lookdevstudioy"))
                EditorPrefs.SetInt("MainView_lookdevstudioy", 0);

            if (!EditorPrefs.HasKey("MainView_lookdevstudioz"))
                EditorPrefs.SetInt("MainView_lookdevstudioz", 0);

            if (!EditorPrefs.HasKey("MainView_lookdevstudioh"))
                EditorPrefs.SetInt("MainView_lookdevstudioh", 0);

            if (!EditorPrefs.HasKey("UnityEditor.MainVieww"))
                EditorPrefs.SetInt("UnityEditor.MainVieww", 0);

            if (!EditorPrefs.HasKey("UnityEditor.MainViewh"))
                EditorPrefs.SetInt("UnityEditor.MainViewh", 0);

            if (!EditorPrefs.HasKey("UnityEditor.MainViewx"))
                EditorPrefs.SetInt("UnityEditor.MainViewx", 0);

            if (!EditorPrefs.HasKey("UnityEditor.MainViewy"))
                EditorPrefs.SetInt("UnityEditor.MainViewy", 53);

            if (!EditorPrefs.HasKey("UnityEditor.MainViewz"))
                EditorPrefs.SetInt("UnityEditor.MainViewz", 0);

            if (!EditorPrefs.HasKey("LookDev.Editor.FeedbackWindoww"))
                EditorPrefs.SetInt("LookDev.Editor.FeedbackWindoww", 0);

            if (!EditorPrefs.HasKey("LookDev.Editor.FeedbackWindowh"))
                EditorPrefs.SetInt("LookDev.Editor.FeedbackWindowh", 0);

            if (!EditorPrefs.HasKey("LookDev.Editor.FeedbackWindowx"))
                EditorPrefs.SetInt("LookDev.Editor.FeedbackWindowx", 0);
            
            if (!EditorPrefs.HasKey("LookDev.Editor.FeedbackWindowy"))
                EditorPrefs.SetInt("LookDev.Editor.FeedbackWindowy", 0);
            
            if (!EditorPrefs.HasKey("LookDev.Editor.FeedbackWindowz"))
                EditorPrefs.SetInt("LookDev.Editor.FeedbackWindowz", 0);
             
            if (!EditorPrefs.HasKey($"mode-current-id-{Application.productName}"))
                EditorPrefs.SetString($"mode-current-id-{Application.productName}", "default");
#endif
#if !DISABLE_LOOKDEV_WELCOME_WINDOW
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
#else
            if (File.Exists(PathToLookDevDisableAutoLaunchFile))
                return;

            // Enable LookDev on the next Editor tick.
            void LoadLookDevOnReload()
            {
                EditorApplication.update -= LoadLookDevOnReload;
                EnableLookDev();
            }

            EditorApplication.update += LoadLookDevOnReload;
#endif
        }

        static LookDevPreferences _lookDevPreferences;

        public const string PathToLookDevDisableAutoLaunchFile = "Library/LookDevDisableAutoLaunch";

        public static bool lookDevEnabled = false;

        public static bool IsHDRP()
        {
            string currentRP = GraphicsSettings.renderPipelineAsset.GetType().Name;

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
#if LOOKDEV_DISABLE_LOGS
            Debug.unityLogger.logEnabled = false;
#endif

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

            lookDevEnabled = true;

            EnableLookDevShortcuts();
            EditorApplication.quitting += DisableLookDevShortcuts;

            //Show Welcome Window of LDS (= Feedback window)
            if (LookDevPreferences.instance != null)
            {
                if (LookDevPreferences.instance.DoNotShowFeedbackWinOnStart == false)
                    ShowWelcomeWindowOnStart();
            }

            // Open Model-tab at First
            LookDevSearchHelpers.SwitchCurrentProvider(2);

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

        static void EnableLookDevShortcuts()
        {
            bool hasLookDevProfile = false;
            foreach (var profileId in ShortcutManager.instance.GetAvailableProfileIds())
            {
                if (profileId == "lookdevstudio")
                {
                    hasLookDevProfile = true;
                    break;
                }
            }

            if (!hasLookDevProfile)
            {
                ShortcutManager.instance.CreateProfile("lookdevstudio");
            }

            ShortcutManager.instance.activeProfileId = "lookdevstudio";
            ShortcutManager.instance.RebindShortcut("Scene View/Toggle 2D Mode", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Tools/View", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Tools/Move", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Tools/Rotate", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Tools/Scale", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Tools/Rect", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Tools/Transform", ShortcutBinding.empty);
            ShortcutManager.instance.RebindShortcut("Window/Maximize View", ShortcutBinding.empty);
        }

        static void DisableLookDevShortcuts()
        {
            ShortcutManager.instance.DeleteProfile("lookdevstudio");
        }

        [CommandHandler("Commands/Return to Unity", CommandHint.Menu | CommandHint.Validate)]
        private static void DisableLookDev(CommandExecuteContext context)
        {
            DisableLookDevShortcuts();
            EditorApplication.quitting -= DisableLookDevShortcuts;

            ModeService.ChangeModeById("default");
            LightingPresetSceneChanger.Shutdown();

            EditorApplication.update -= OnLookDevModeUpdate;
            SceneView.duringSceneGui -= OnDuringSceneGuiForDragDrop;

            LookDevShortcutsOverlay.Disable();

            LookDevSearchHelpers.UnregisterCallbacks();

            LookDevSceneMenu.UnregisterSceneMenu();

#if LOOKDEV_DISABLE_LOGS
            Debug.unityLogger.logEnabled = true;
#endif
            lookDevEnabled = false;
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


        [CommandHandler("Commands/Create Light Prefab", CommandHint.Menu | CommandHint.Validate)]
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



        [CommandHandler("Commands/New LDS Project", CommandHint.Menu | CommandHint.Validate)]
        public static void NewLdsProject(CommandExecuteContext context)
        {
            string projPath = EditorUtility.SaveFilePanelInProject("New LDS Project", "ProjectName", "asset",
                "Create new Project setting file", Path.GetFullPath(ProjectSettingWindow.GetLookDevProjectFolder()));

            if (string.IsNullOrEmpty(projPath))
                return;

            projPath = AssetDatabase.GenerateUniqueAssetPath(projPath);

            ProjectSettingWindow.CreateNewProjectSettings(Path.GetFileNameWithoutExtension(projPath));
        }

        [MenuItem("LookDev Studio/Import Unity Package", editorModes = new[] {"lookdevstudio"})]
        static void ImportUnityPackage()
        {
            var lastAssetImportDir = EditorPrefs.GetString("LastAssetImportDir", Application.dataPath);
            var unityPackage = EditorUtility.OpenFilePanel("Import Unity Package", lastAssetImportDir, "unitypackage");
            if (!string.IsNullOrEmpty(unityPackage))
            {
                AssetDatabase.ImportPackage(unityPackage, true);
            }
        }


        [MenuItem("LookDev Studio/Show Welcome Window", editorModes = new[] {"lookdevstudio"})]
        static void ShowWelcomeWindowOnStart()
        {
            FeedbackWindow feedbackWindow = EditorWindow.GetWindow<FeedbackWindow>();

            feedbackWindow.titleContent = new GUIContent("LookDev Studio");
            feedbackWindow.maxSize = new Vector2(800, 680);
            feedbackWindow.minSize = feedbackWindow.maxSize;

            float posX = (Screen.currentResolution.width - feedbackWindow.maxSize.x) * 0.5f;
            float posY = (Screen.currentResolution.height - feedbackWindow.maxSize.y) * 0.5f;

            feedbackWindow.position = new Rect(new Vector2(posX, posY), feedbackWindow.maxSize);

            feedbackWindow.Show();
        }


        [CommandHandler("Commands/Load LDS Project", CommandHint.Menu | CommandHint.Validate)]
        public static void LoadLdsProject(CommandExecuteContext context)
        {
            string projPath = EditorUtility.OpenFilePanel("Load LDS Project",
                Path.GetFullPath(ProjectSettingWindow.GetLookDevProjectFolder()), "asset");

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
                if (Path.GetFileName(savePath).ToUpper()
                        .StartsWith(ProjectSettingWindow.projectSetting.PrefabPrefix.ToUpper()) == false &&
                    ProjectSettingWindow.projectSetting.PrefabPrefix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath),
                        $"{ProjectSettingWindow.projectSetting.PrefabPrefix}{Path.GetFileNameWithoutExtension(savePath)}.prefab");

                if (Path.GetFileName(savePath).ToUpper()
                        .EndsWith(ProjectSettingWindow.projectSetting.PrefabPostfix.ToUpper()) == false &&
                    ProjectSettingWindow.projectSetting.PrefabPostfix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath),
                        $"{Path.GetFileNameWithoutExtension(savePath)}{ProjectSettingWindow.projectSetting.PrefabPostfix}.prefab");
            }


            if (string.IsNullOrEmpty(assetPath) == false)
            {
                savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
                Selection.gameObjects[0].name = Path.GetFileNameWithoutExtension(savePath);

                PrefabUtility.SaveAsPrefabAssetAndConnect(Selection.gameObjects[0], savePath,
                    InteractionMode.AutomatedAction);

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
                if (Path.GetFileName(savePath).ToUpper()
                        .StartsWith(ProjectSettingWindow.projectSetting.PrefabPrefix.ToUpper()) == false &&
                    ProjectSettingWindow.projectSetting.PrefabPrefix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath),
                        $"{ProjectSettingWindow.projectSetting.PrefabPrefix}{Path.GetFileNameWithoutExtension(savePath)}.prefab");

                if (Path.GetFileName(savePath).ToUpper()
                        .EndsWith(ProjectSettingWindow.projectSetting.PrefabPostfix.ToUpper()) == false &&
                    ProjectSettingWindow.projectSetting.PrefabPostfix.Trim() != string.Empty)
                    savePath = savePath.Replace(Path.GetFileName(savePath),
                        $"{Path.GetFileNameWithoutExtension(savePath)}{ProjectSettingWindow.projectSetting.PrefabPostfix}.prefab");
            }

            if (string.IsNullOrEmpty(assetPath) == false)
            {
                savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);

                PrefabSaveAsWindow prefabSaveAsWindow =
                    EditorWindow.GetWindow<PrefabSaveAsWindow>("Save As (Prefab)", true);
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
                if (EditorUtility.DisplayDialog("Convert to Prefab",
                    "Do you want to convert the selected GameObjec to the Prefab?", "Yes", "No"))
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
                if (EditorUtility.DisplayDialog("Overwrite Prefab", "Do you want to overwirte the existing Prefab?",
                    "Yes", "No") == false)
                    return;

                PrefabUtility.ApplyPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(Selection.gameObjects[0]),
                    InteractionMode.AutomatedAction);
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

        #region Camera Shortcuts

        [MenuItem("LookDev Studio/Camera/Load Camera 1 _1", editorModes = new[] {"lookdevstudio"})]
        static void LoadCameraOne()
        {
            LookDevHelpers.LoadCameraPosition(0);
        }

        [MenuItem("LookDev Studio/Camera/Load Camera 2 _2", editorModes = new[] {"lookdevstudio"})]
        static void LoadCameraTwo()
        {
            LookDevHelpers.LoadCameraPosition(1);
        }

        [MenuItem("LookDev Studio/Camera/Load Camera 3 _3", editorModes = new[] {"lookdevstudio"})]
        static void LoadCameraThree()
        {
            LookDevHelpers.LoadCameraPosition(2);
        }

        [MenuItem("LookDev Studio/Camera/Load Camera 4 _4", editorModes = new[] {"lookdevstudio"})]
        static void LoadCameraFour()
        {
            LookDevHelpers.LoadCameraPosition(3);
        }

        [MenuItem("LookDev Studio/Camera/Load Camera 5 _5", editorModes = new[] {"lookdevstudio"})]
        static void LoadCameraFive()
        {
            LookDevHelpers.LoadCameraPosition(4);
        }

        [MenuItem("LookDev Studio/Camera/Save Camera 1 %1", editorModes = new[] {"lookdevstudio"})]
        static void SaveCameraOne()
        {
            LookDevHelpers.SaveCameraPosition(0);
            LookDevHelpers.ShowSavedCameraPositionNotification(1);
        }

        [MenuItem("LookDev Studio/Camera/Save Camera 2 %2", editorModes = new[] {"lookdevstudio"})]
        static void SaveCameraTwo()
        {
            LookDevHelpers.SaveCameraPosition(1);
            LookDevHelpers.ShowSavedCameraPositionNotification(2);
        }

        [MenuItem("LookDev Studio/Camera/Save Camera 3 %3", editorModes = new[] {"lookdevstudio"})]
        static void SaveCameraThree()
        {
            LookDevHelpers.SaveCameraPosition(2);
            LookDevHelpers.ShowSavedCameraPositionNotification(3);
        }

        [MenuItem("LookDev Studio/Camera/Save Camera 4 %4", editorModes = new[] {"lookdevstudio"})]
        static void SaveCameraFour()
        {
            LookDevHelpers.SaveCameraPosition(3);
            LookDevHelpers.ShowSavedCameraPositionNotification(4);
        }

        [MenuItem("LookDev Studio/Camera/Save Camera 5 %5", editorModes = new[] {"lookdevstudio"})]
        static void SaveCameraFive()
        {
            LookDevHelpers.SaveCameraPosition(4);
            LookDevHelpers.ShowSavedCameraPositionNotification(5);
        }

        #endregion

        #region Set Tool Mode

        [MenuItem("LookDev Studio/Tools/View _q", editorModes = new[] {"lookdevstudio"})]
        static void SetToolModeView()
        {
            LookDevHelpers.SetToolMode(Tool.View);
        }

        [MenuItem("LookDev Studio/Tools/Move _w", editorModes = new[] {"lookdevstudio"})]
        static void SetToolModeMove()
        {
            LookDevHelpers.SetToolMode(Tool.Move);
        }

        [MenuItem("LookDev Studio/Tools/Rotate _e", editorModes = new[] {"lookdevstudio"})]
        static void SetToolModeRotate()
        {
            LookDevHelpers.SetToolMode(Tool.Rotate);
        }

        [MenuItem("LookDev Studio/Tools/Scale _r", editorModes = new[] {"lookdevstudio"})]
        static void SetToolModeScale()
        {
            LookDevHelpers.SetToolMode(Tool.Scale);
        }

        [MenuItem("LookDev Studio/Tools/Rect _t", editorModes = new[] {"lookdevstudio"})]
        static void SetToolModeRect()
        {
            LookDevHelpers.SetToolMode(Tool.Rect);
        }

        [MenuItem("LookDev Studio/Tools/Transform _y", editorModes = new[] {"lookdevstudio"})]
        static void SetToolModeTransform()
        {
            LookDevHelpers.SetToolMode(Tool.Transform);
        }

        #endregion

        // [MenuItem("LookDev Studio DEBUG/Log MainView Size")]
        // static void LogMainViewSize()
        // {
        //     Debug.Log($"UnityEditor.MainViewx: {EditorPrefs.GetFloat("UnityEditor.MainViewx")}");
        //     Debug.Log($"UnityEditor.MainViewy: {EditorPrefs.GetFloat("UnityEditor.MainViewy")}");
        //     Debug.Log($"UnityEditor.MainVieww: {EditorPrefs.GetFloat("UnityEditor.MainVieww")}");
        //     Debug.Log($"UnityEditor.MainViewh: {EditorPrefs.GetFloat("UnityEditor.MainViewh")}");
        // }
        //
        // [MenuItem("LookDev Studio DEBUG/Find Container Window")]
        // static void GetContainerWindow()
        // {
        //     Assembly assembly = typeof(EditorWindow).Assembly;
        //     Type type = assembly.GetType("UnityEditor.ContainerWindow");
        //     FieldInfo pixelRectField = type.GetField("m_PixelRect", BindingFlags.NonPublic | BindingFlags.Instance);
        //     PropertyInfo positionProperty = type.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
        //     FieldInfo showModeField = type.GetField("m_ShowMode", BindingFlags.Instance | BindingFlags.NonPublic);
        //
        //     foreach (var containerWindow in Resources.FindObjectsOfTypeAll(type))
        //     {
        //         Debug.Log($"PixelRect: {pixelRectField.GetValue(containerWindow)}");
        //         Debug.Log($"ShowMode: {showModeField.GetValue(containerWindow)}");
        //         //positionProperty.SetValue(containerWindow, new Rect(0, 100, 200, 300));
        //     }
        // }
    }
}