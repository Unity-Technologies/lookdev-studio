using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.IO;

namespace LookDev.Editor
{
    public class CreateNewFilterWindow : EditorWindow
    {
        LookDevFilter newFilter = new LookDevFilter();

        Vector2 scrollVector;


        public void SetFilter(LookDevFilter inputFilter)
        {
            if (inputFilter != null)
            {
                newFilter = inputFilter;

                Debug.Log(inputFilter.filterName);
            }
        }


        private void OnEnable()
        {
            if (newFilter == null)
            {
                newFilter = new LookDevFilter();
                newFilter.filterName = "New Filter Name";
            }

        }


        public void ResetWindowPosition()
        {
            minSize = new Vector2(400, 520);
            maxSize = minSize;

            float posX = LookDevSearchHelpers.searchViewEditorWindow.position.x - minSize.x - 10;
            float posY = LookDevSearchHelpers.searchViewEditorWindow.position.y + LookDevSearchHelpers.searchViewEditorWindow.position.height - minSize.y;

            position = new Rect(new Vector2(posX, posY), minSize);
        }


        void ShowFilterInfo(string filterItemName, ref List<string> pathList)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{filterItemName} :", EditorStyles.boldLabel, GUILayout.Width(75));
            if (GUILayout.Button(new GUIContent("+", "Add Directory"), GUILayout.Width(20)))
            {
                string inputDirectory = EditorUtility.OpenFolderPanel("Add Directory", Application.dataPath, string.Empty);

                string projectRootPath = (new DirectoryInfo(Application.dataPath + "/../").FullName).Replace("\\","/");
                inputDirectory = inputDirectory.Replace(projectRootPath, string.Empty);

                if (AssetDatabase.IsValidFolder(inputDirectory))
                    pathList.Add(inputDirectory);
                else
                    Debug.LogError($"The input directory is not valid : {inputDirectory}");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            for (int i = 0; i < pathList.Count; i++)
            {
                GUILayout.BeginHorizontal("Box");
                GUILayout.FlexibleSpace();
                GUILayout.TextField(pathList[i], GUILayout.Width(340));
                if (GUILayout.Button(new GUIContent("X", "Remove Directory"), GUILayout.Width(20)))
                {
                    pathList.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        private void OnGUI()
        {
            scrollVector = GUILayout.BeginScrollView(scrollVector, GUILayout.Width(400), GUILayout.Height(520));

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("Filter Name : ", GUILayout.Width(100));
            newFilter.filterName = GUILayout.TextField(newFilter.filterName, 30, GUILayout.Width(200));
            GUILayout.EndHorizontal();


            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            ShowFilterInfo("Materials", ref newFilter.pathForMaterial);
            ShowFilterInfo("Textures", ref newFilter.pathForTexture);
            ShowFilterInfo("Models", ref newFilter.pathForModel);
            ShowFilterInfo("Shaders", ref newFilter.pathForShader);
            ShowFilterInfo("Lights", ref newFilter.pathForLight);
            ShowFilterInfo("Skyboxes", ref newFilter.pathForSkybox);
            ShowFilterInfo("Animations", ref newFilter.pathForAnimation);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Save Filter"))
            {
                LookDevSearchFilters.SaveFilter(newFilter);
                LookDevSearchFilters.RefreshFilters();
                Close();
            }

            EditorGUILayout.Space();

            GUILayout.EndScrollView();

        }
    }
}