using UnityEngine;

using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace LookDev.Editor
{
    

    public class LoadMeshWindow : EditorWindow
    {
        static List<string> modelList = new List<string>();

        static LoadMeshWindow m_loadMeshWindow;

        static string target = string.Empty;

        Vector2 scrollPos;

        public static void InitLoadMeshWindow(List<string> modelPathListToBeloaded, bool isPopup)
        {
            modelList.Clear();

            if (modelPathListToBeloaded == null)
                return;

            if (modelPathListToBeloaded.Count == 0)
                return;

            foreach(string modelPath in modelPathListToBeloaded)
            {
                Object source = AssetDatabase.LoadAssetAtPath<Object>(modelPath);

                if (source == null)
                {
                    Debug.LogError($"Could not find the file : {modelPath}");
                    continue;
                }

                if (modelList.Contains(modelPath) == false)
                    modelList.Add(modelPath);

            }
            
            if (m_loadMeshWindow == null)
                m_loadMeshWindow = CreateInstance<LoadMeshWindow>();

            m_loadMeshWindow.titleContent = new GUIContent("Select Model in the Prefab");
            m_loadMeshWindow.minSize = new Vector2(330, 95);
            m_loadMeshWindow.maxSize = m_loadMeshWindow.minSize;

            
            if (isPopup)
                m_loadMeshWindow.position = EditorWindow.GetWindow<QuickControl>().GetCenterPositionFromWindow(m_loadMeshWindow.minSize);

            m_loadMeshWindow.ShowModalUtility();
        }

        public static List<string> GetModelList()
        {
            return modelList;
        }

        public static string GetTarget()
        {
            return target;
        }

        private void OnGUI()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.BeginVertical();
            EditorGUILayout.Space();

            foreach(string modelPath in modelList)
            {
                if (GUILayout.Button(modelPath))
                {
                    target = modelPath;
                    m_loadMeshWindow?.Close();
                }
            }

            EditorGUILayout.Space();

            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }
    }


}