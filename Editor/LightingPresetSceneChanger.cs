using System;
using System.Collections.Generic;
using System.IO;
using LookDev.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;

using LookDev;

public static class LightingPresetSceneChanger
{
    private static int m_sceneSelection;
    public static Action OnLightSceneChangedEvent;

    public static void Initialize()
    {
        m_sceneSelection = GetLastLightingPreset(out string _, out string[] _, out string[] scenePaths);
        TransitionToScene(scenePaths[m_sceneSelection]);
    }

    public static void Shutdown()
    {
        UndoSceneGameObjectConfiguration();
    }

    public static string GetCurrentLightScenePath()
    {
        string scenePath = string.Empty;

        var originalScene = SceneManager.GetActiveScene();

        if (originalScene != null)
            scenePath = originalScene.path;

        return scenePath;
    }


    public static void SaveSceneAsLightPreset(string dstScenePath)
    {
        var originalScene = SceneManager.GetActiveScene();
        Transform originalRoot = LookDevHelpers.GetLookDevContainer(originalScene)?.transform;

        // 1) Move root object to a temp scene.
        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(originalRoot.gameObject, tempScene);

        // 1.5) Disable Hide Flags & Make Empty Models

        GameObject newModels = new GameObject("Models");
        newModels.tag = "AssetHolder";
        newModels.AddComponent<AssetHolder>();
        SceneManager.MoveGameObjectToScene(newModels, originalScene);

        GameObject[] allGameObjects = originalScene.GetRootGameObjects();

        foreach (var obj in allGameObjects)
        {
            obj.hideFlags = HideFlags.None;
        }

        // 2) Save the original Scene without models
        EditorSceneManager.SaveScene(originalScene, dstScenePath);

        // 2.5) Delete empty Models
        GameObject.DestroyImmediate(newModels);

        // 3) Back models to the original Scene
        SceneManager.MoveGameObjectToScene(LookDevHelpers.GetLookDevContainer(tempScene).gameObject, originalScene);

        // 4) Dispose of the temp scene.
        EditorSceneManager.CloseScene(tempScene, true);

        ConfigureSceneGameObjects();
        // 5) Trasition to the new Scene
        //TransitionToScene(dstScenePath);
    }

    public static void TransitionToScene(string scenePath)
    {
        var originalScene = SceneManager.GetActiveScene();
        Transform originalRoot = LookDevHelpers.GetLookDevContainer(originalScene)?.transform;
        Transform lightRoot = LookDevHelpers.GetLookDevLightContainer(originalScene)?.transform;

        if (originalRoot is null || originalRoot.childCount == 0)
        {
            EditorSceneManager.OpenScene(scenePath);
        }
        else
        {
            // 1) Move root object to a temp scene.
            var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            SceneManager.MoveGameObjectToScene(originalRoot.gameObject, tempScene);

            //SceneManager.MoveGameObjectToScene(lightRoot.gameObject, tempScene); /////////////////////////////////

            // 2) Close the original scene to prevent loading conflicts.
            EditorSceneManager.CloseScene(originalScene, true);

            // 3) Replace root from temp into the final new scene.
            var newScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            GameObject.DestroyImmediate(LookDevHelpers.GetLookDevContainer(newScene));

            //GameObject.DestroyImmediate(LookDevHelpers.GetLookDevLightContainer(newScene)); ////////////////////////////////////////

            SceneManager.MoveGameObjectToScene(tempScene.GetRootGameObjects()[0], newScene);

            //SceneManager.MoveGameObjectToScene(tempScene.GetRootGameObjects()[0], newScene); ///////////////////////////////////////

            // 4) Reassign if LightRig's volume reference is null.
            
            ILightRig targetLightRig = GameObject.FindObjectOfType<ILightRig>();

            if (targetLightRig.GlobalVolume == null)
                targetLightRig.GlobalVolume = GameObject.FindObjectOfType<Volume>();
            
            // 5) Dispose of the temp scene.
            EditorSceneManager.CloseScene(tempScene, true);
        }

        // Configure/Lock scene GameObjects for LookDev modes.
        ConfigureSceneGameObjects();

        // Signal completion.
        OnLightSceneChangedEvent?.Invoke();
    }

    public static void OnGUI()
    {
        GetLastLightingPreset(out string _, out string[] sceneNames, out string[] scenePaths);

        if (m_sceneSelection < 0 || m_sceneSelection >= sceneNames.Length)
        {
            m_sceneSelection = 0;
            TransitionToScene(scenePaths[m_sceneSelection]);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("Lighting Preset:", GUILayout.Width(100));
        var newSelection = EditorGUILayout.Popup(m_sceneSelection, sceneNames, GUILayout.Width(170));
        if (newSelection != m_sceneSelection)
        {
            TransitionToScene(scenePaths[newSelection]);
            m_sceneSelection = newSelection;
            SetLastLightingPreset(m_sceneSelection);
        }

        EditorGUILayout.EndHorizontal();
    }
    
    public static int GetLightSceneIndex(string scenePathToBeChecked)
    {
        GetLastLightingPreset(out string scenePath, out string[] allSceneNames, out string[] allScenePaths);

        for (int i=0;i<allScenePaths.Length;i++)
        {
            if (allScenePaths[i] == scenePathToBeChecked)
            {
                return i;
            }
        }

        return 0;
    }


    public static void SetLightSceneIndex(int index)
    {
        m_sceneSelection = index;
        SetLastLightingPreset(m_sceneSelection);
    }


    private static int GetLastLightingPreset(out string scenePath, out string[] allSceneNames, out string[] allScenePaths)
    {
        scenePath = string.Empty;
        allSceneNames = null;
        allScenePaths = null;

        var userScenes = AssetDatabase.FindAssets("t:scene", new string[] { "Assets/LookDev/Scenes" });

        int totalLength = userScenes.Length;
        if (totalLength == 0)
            return -1;
        
        var scenes = new List<string>(totalLength);
        scenes.AddRange(userScenes);

        allSceneNames = new string[totalLength];
        allScenePaths = new string[totalLength];
        for (int i = 0; i < totalLength; i++)
        {
            allScenePaths[i] = AssetDatabase.GUIDToAssetPath(scenes[i]);
            allSceneNames[i] = Path.GetFileNameWithoutExtension(allScenePaths[i]).Replace("aa-", "");
        }

        var lastSceneIndexPref = EditorPrefs.GetInt(LookDevHelpers.CurrentSceneSelectionKey, 0);
        var lastSceneIndex = Mathf.Clamp(lastSceneIndexPref, 0, allSceneNames.Length - 1);

        scenePath = allScenePaths[lastSceneIndex];

        return lastSceneIndex;
    }

    private static void SetLastLightingPreset(int sceneIndex)
    {
        EditorPrefs.SetInt(LookDevHelpers.CurrentSceneSelectionKey, sceneIndex);
    }
    
    private static void ConfigureSceneGameObjects()
    {
        GameObject[] allGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var obj in allGameObjects)
        {
            if (!obj.CompareTag("LookDevCam") && !obj.CompareTag("LightRig") && !obj.CompareTag("AssetHolder") && !obj.CompareTag("PostProcess"))
            {
                obj.hideFlags |= HideFlags.HideInHierarchy;
            }
        }

        //Key objects
        var flags = (HideFlags.HideInInspector | HideFlags.NotEditable);
        LookDevHelpers.GetLookDevContainer().hideFlags |= flags;
        GameObject.FindGameObjectWithTag("LookDevCam").hideFlags |= flags;
        GameObject.FindGameObjectWithTag("LightRig").hideFlags |= flags;
        GameObject.FindGameObjectWithTag("PostProcess").hideFlags |= flags;
    }

    private static void UndoSceneGameObjectConfiguration()
    {
        GameObject[] allGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var obj in allGameObjects)
        {
            if (!obj.CompareTag("LookDevCam") && !obj.CompareTag("LightRig") && !obj.CompareTag("AssetHolder") && !obj.CompareTag("PostProcess"))
            {
                obj.hideFlags &= ~HideFlags.HideInHierarchy;
            }
        }
        
        //Key objects
        var flags = ~(HideFlags.HideInInspector | HideFlags.NotEditable);
        LookDevHelpers.GetLookDevContainer().hideFlags &= flags;
        GameObject.FindGameObjectWithTag("LookDevCam").hideFlags &= flags;
        GameObject.FindGameObjectWithTag("LightRig").hideFlags &= flags;
        GameObject.FindGameObjectWithTag("PostProcess").hideFlags &= flags;
    }

    [MenuItem("LookDev Studio/Set Previous Light Preset _-", editorModes = new[] { "lookdevstudio" })]
    static void SetPreviousLightPreset()
    {
        GetLastLightingPreset(out string _, out string[] sceneNames, out string[] scenePaths);

        if (m_sceneSelection == 0)
            m_sceneSelection = sceneNames.Length - 1;
        else
            m_sceneSelection -= 1;

        TransitionToScene(scenePaths[m_sceneSelection]);
        SetLastLightingPreset(m_sceneSelection);
    }

    [MenuItem("LookDev Studio/Set Next Light Preset _=", editorModes = new[] { "lookdevstudio" })]
    static void SetNextLightPreset()
    {
        GetLastLightingPreset(out string _, out string[] sceneNames, out string[] scenePaths);

        if (m_sceneSelection == sceneNames.Length - 1)
            m_sceneSelection = 0;
        else
            m_sceneSelection += 1;

        TransitionToScene(scenePaths[m_sceneSelection]);
        SetLastLightingPreset(m_sceneSelection);
    }

}
