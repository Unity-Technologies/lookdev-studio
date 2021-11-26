using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.IO;

namespace LookDev.Editor
{
    public class CreateNewFilterWindow : EditorWindow
    {
        string previousFilterName = string.Empty;

        LookDevFilter newFilter = new LookDevFilter();

        Vector2 scrollVector;

        
        public void SetPreviousFilterName(string inputPath)
        {
            previousFilterName = inputPath;
        }
        

        public void SetFilter(LookDevFilter inputFilter)
        {
            if (inputFilter != null)
            {
                newFilter = inputFilter;
            }
        }

        public LookDevFilter GetFilter()
        {
            return newFilter;
        }


        private void OnEnable()
        {
            if (newFilter == null)
            {
                newFilter = new LookDevFilter();
                newFilter.filterName = "New Filter Name";
            }

        }

        private void OnDisable()
        {
            if (newFilter == null)
            {
                LookDevSearchFilters.SaveFilter(newFilter);
                LookDevSearchFilters.RefreshFilters();
            }

            previousFilterName = string.Empty;
            LookDevSearchFilters.Inst.OnChangedFilters();
        }


        public void ResetWindowPosition()
        {
            minSize = new Vector2(400, 350);
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


        void ShowFilterByObjectInfo(string filterItemName, ref List<string> objGuidList)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{filterItemName} :", EditorStyles.boldLabel, GUILayout.Width(75));
            if (GUILayout.Button(new GUIContent("+", "Add Object"), GUILayout.Width(20)))
            {
                if (objGuidList != null)
                    objGuidList.Add(string.Empty);
                else
                    Debug.LogError($"The filter's Object List in null");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            for (int i = 0; i < objGuidList.Count; i++)
            {
                GUILayout.BeginHorizontal("Box");
                GUILayout.FlexibleSpace();

                string assetPath = AssetDatabase.GUIDToAssetPath(objGuidList[i]);
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                EditorGUI.BeginChangeCheck();
                obj = EditorGUILayout.ObjectField(obj, typeof(Object), true, GUILayout.Width(340));
                if (EditorGUI.EndChangeCheck())
                {
                    string curAssetPath = AssetDatabase.GetAssetPath(obj);
                    objGuidList[i] = AssetDatabase.AssetPathToGUID(curAssetPath);
                }

                if (GUILayout.Button(new GUIContent("X", "Remove Object"), GUILayout.Width(20)))
                {
                    objGuidList.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        private void OnGUI()
        {
            scrollVector = GUILayout.BeginScrollView(scrollVector, GUILayout.Width(minSize.x), GUILayout.Height(minSize.y));

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("Filter Name : ", GUILayout.Width(100));
            newFilter.filterName = GUILayout.TextField(newFilter.filterName, 30, GUILayout.Width(200));
            GUILayout.EndHorizontal();


            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            ShowFilterInfo("Paths", ref newFilter.paths);

            ShowFilterByObjectInfo("Objects", ref newFilter.objectGuid);

            /*
            ShowFilterInfo("Materials", ref newFilter.pathForMaterial);
            ShowFilterInfo("Textures", ref newFilter.pathForTexture);
            ShowFilterInfo("Models", ref newFilter.pathForModel);
            ShowFilterInfo("Shaders", ref newFilter.pathForShader);
            ShowFilterInfo("Lights", ref newFilter.pathForLight);
            ShowFilterInfo("Skyboxes", ref newFilter.pathForSkybox);
            ShowFilterInfo("Animations", ref newFilter.pathForAnimation);
            */

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label("Visible types", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            GUILayout.BeginVertical("Box");
            newFilter.showModel = GUILayout.Toggle(newFilter.showModel, new GUIContent("Show Models", string.Empty));
            newFilter.showPrefab = GUILayout.Toggle(newFilter.showPrefab, new GUIContent("Show Prefabs", string.Empty));
            GUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Save Filter"))
            {
                LookDevSearchFilters.SaveFilter(newFilter);

                if (newFilter.filterName != previousFilterName)
                    LookDevSearchFilters.RemovePreviousFilter(previousFilterName);

                LookDevSearchFilters.RefreshFilters();

                previousFilterName = string.Empty;
                Close();
            }

            EditorGUILayout.Space();

            GUILayout.EndScrollView();

        }
    }
}