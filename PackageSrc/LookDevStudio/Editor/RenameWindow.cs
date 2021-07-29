using UnityEngine;

using UnityEditor;
using System.IO;


namespace LookDev.Editor
{
    

    public class RenameWindow : EditorWindow
    {
        static string sourceFullPath;
        static string targetFullPath;

        static string sourceFile;
        static string targetFile;

        static RenameWindow m_renameWindow;


        public static void InitRenameWindow(string sourceFilePath, bool isPopup)
        {

            Object source = AssetDatabase.LoadAssetAtPath<Object>(sourceFilePath);

            if (source == null)
            {
                Debug.LogError($"Could not find the file : {sourceFilePath}");
                return;
            }

            if (m_renameWindow == null)
                m_renameWindow = CreateInstance<RenameWindow>();

            m_renameWindow.titleContent = new GUIContent("Rename");
            m_renameWindow.minSize = new Vector2(330, 95);
            m_renameWindow.maxSize = m_renameWindow.minSize;

            
            if (isPopup)
            {
                m_renameWindow.position = EditorWindow.GetWindow<QuickControl>().GetCenterPositionFromWindow(m_renameWindow.minSize);

            }

            sourceFullPath = sourceFilePath;

            sourceFile = Path.GetFileNameWithoutExtension(sourceFullPath);
            targetFile = sourceFile;

            m_renameWindow.ShowModalUtility();
        }

        public static string GetTargetFullPath()
        {
            return targetFullPath;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("Source File Name:", GUILayout.Width(150));
            GUILayout.Label($"{sourceFile}", GUILayout.Width(160));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("New File Name:", GUILayout.Width(150));
            targetFile = GUILayout.TextField(targetFile, GUILayout.Width(160));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Ok", GUILayout.Width(150)))
            {
                if (sourceFile != targetFile)
                {
                    AssetDatabase.RenameAsset(sourceFullPath, targetFile);
                    targetFullPath = sourceFullPath.Replace($"/{sourceFile}", $"/{targetFile}");

                    Object target = AssetDatabase.LoadAssetAtPath<Object>(targetFullPath);

                    if (target != null)
                    {
                        Selection.activeObject = target;
                        if (SceneView.lastActiveSceneView != null)
                            SceneView.lastActiveSceneView.ShowNotification(new GUIContent($"Renamed to \"{targetFullPath}\""), 4f);
                    }

                }
                m_renameWindow?.Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(160)))
            {
                m_renameWindow?.Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }


}