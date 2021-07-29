using System;
using System.Collections.Generic;
using System.IO;
using LookDev.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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

    private static void TransitionToScene(string scenePath)
    {
        var originalScene = SceneManager.GetActiveScene();
        Transform originalRoot = LookDevHelpers.GetLookDevContainer(originalScene)?.transform;

        if (originalRoot is null || originalRoot.childCount == 0)
        {
            EditorSceneManager.OpenScene(scenePath);
        }
        else
        {
            // 1) Move root object to a temp scene.
            var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(originalRoot.gameObject, tempScene);

            // 2) Close the original scene to prevent loading conflicts.
            EditorSceneManager.CloseScene(originalScene, true);

            // 3) Replace root from temp into the final new scene.
            var newScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            GameObject.DestroyImmediate(LookDevHelpers.GetLookDevContainer(newScene));
            SceneManager.MoveGameObjectToScene(tempScene.GetRootGameObjects()[0], newScene);

            // 4) Dispose of the temp scene.
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

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal(GUI.skin.box);

        GUILayout.FlexibleSpace();
        GUILayout.Label("Lighting Preset:", GUILayout.Width(100));
        var newSelection = EditorGUILayout.Popup(m_sceneSelection, sceneNames, GUILayout.Width(200));
        if (newSelection != m_sceneSelection)
        {
            TransitionToScene(scenePaths[newSelection]);
            m_sceneSelection = newSelection;
            SetLastLightingPreset(m_sceneSelection);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    
    private static int GetLastLightingPreset(out string scenePath, out string[] allSceneNames, out string[] allScenePaths)
    {
        scenePath = string.Empty;
        allSceneNames = null;
        allScenePaths = null;

        var packageScenes = AssetDatabase.FindAssets("t:scene", new string[] { "Packages/com.unity.lookdevstudio/Setup/Scenes" });
        var userScenes = AssetDatabase.FindAssets("t:scene", new string[] { "Assets/LookDev/Scenes" });

        int totalLength = packageScenes.Length + userScenes.Length;
        if (totalLength == 0)
            return -1;
        
        var scenes = new List<string>(totalLength);
        scenes.AddRange(packageScenes);
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
}
