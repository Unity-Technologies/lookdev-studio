using UnityEngine;
using UnityEditor;
using System.IO;

namespace LookDev.Editor
{
    public class PrefabSaveAsWindow : EditorWindow
    {
        GameObject targetGo;

        string targetDir = string.Empty;
        string targetPath = string.Empty;
        string prefabName;


        public void InitPrefabSaveAsWindow(GameObject prefabGo, string prefabPath)
        {
            targetGo = prefabGo;

            targetPath = prefabPath;
            targetDir = prefabPath.Replace(Path.GetFileName(prefabPath), string.Empty);

            prefabName = Path.GetFileNameWithoutExtension(targetPath);


            if (targetGo == null || string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("Prefab is null or Prefab path is empty.");
                return;
            }

            minSize = new Vector2(330, 90);
            maxSize = minSize;

            var assembly = typeof(UnityEditor.EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.SceneHierarchyWindow");

            EditorWindow sceneHierarchyWindow = EditorWindow.GetWindow(type);

            Vector2 pos = new Vector2(sceneHierarchyWindow.position.x + sceneHierarchyWindow.position.width + 10, sceneHierarchyWindow.position.y + sceneHierarchyWindow.position.height);
            position = new Rect(pos, minSize);


        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            EditorGUILayout.Space();

            GUILayout.Label("File name", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            prefabName = GUILayout.TextField(prefabName);

            if (GUILayout.Button("Save"))
            {
                if (SavePrefab())
                    Close();
            }

            EditorGUILayout.Space();

            GUILayout.EndVertical();

        }

        bool SavePrefab()
        {
            if (AssetDatabase.IsValidFolder(targetDir) == false)
            {
                Debug.LogError($"Could not find the target path : {targetDir}");
                return false;
            }

            if (prefabName.Trim() == string.Empty)
            {
                Debug.LogWarning("You cannot input an Empty prefab name.");
                return false;
            }

            string outputPath = $"{targetDir}{prefabName}.prefab";

            outputPath = AssetDatabase.GenerateUniqueAssetPath(outputPath);

            targetGo.name = prefabName;

            PrefabUtility.SaveAsPrefabAssetAndConnect(targetGo, outputPath, InteractionMode.AutomatedAction);

            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Prefab Saved : {outputPath}"), 4f);

            LookDevSearchHelpers.SwitchCurrentProvider(2);

            AssetDatabase.SaveAssets();

            LookDevSearchHelpers.RefreshWindow();

            return true;
        }
    }

}