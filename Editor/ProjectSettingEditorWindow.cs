using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.IO;

namespace LookDev.Editor
{
    public class ProjectSettingEditorWindow : EditorWindow
    {
        ProjectSetting projectSetting;

        public string exportAssetPath = string.Empty;
        public string importAssetPath = string.Empty;


        public void SetCurrentProjectSetting(ProjectSetting inProjectSetting)
        {
            projectSetting = inProjectSetting;
            exportAssetPath = inProjectSetting.GetExportAssetPath();
            importAssetPath = inProjectSetting.GetImportAssetPath();
        }

        public void ResetWindowPosition()
        {
            minSize = new Vector2(400, 400);
            maxSize = minSize;

            float posX = LookDevSearchHelpers.searchViewEditorWindow.position.x - minSize.x - 10;
            float posY = LookDevSearchHelpers.searchViewEditorWindow.position.y + LookDevSearchHelpers.searchViewEditorWindow.position.height - minSize.y;

            position = new Rect(new Vector2(posX, posY), minSize);
        }

        private void OnDisable()
        {
            EditorUtility.SetDirty(projectSetting);
            AssetDatabase.SaveAssets();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.Label($"LDS Project : {Path.GetFileNameWithoutExtension(projectSetting.GetObjectPath(projectSetting))}", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Make Prefabs for all meshes", EditorStyles.boldLabel, GUILayout.Width(200));
            projectSetting.MakePrefabsForAllMeshes = GUILayout.Toggle(projectSetting.MakePrefabsForAllMeshes, string.Empty);
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            */

            GUILayout.Label($"DCC Editor of choice", EditorStyles.boldLabel, GUILayout.Width(200));


            EditorGUILayout.Space();


            GUILayout.BeginVertical("Box");


            EditorGUI.BeginChangeCheck();
            projectSetting.meshDccs = (MeshDCCs)EditorGUILayout.EnumPopup(new GUIContent("Editing Mesh"), projectSetting.meshDccs);
            if (EditorGUI.EndChangeCheck())
            {
                projectSetting.meshDccPath = string.Empty;
            }

            if (projectSetting.meshDccs != MeshDCCs.None)
            {
                GUILayout.Label($"Path ({projectSetting.meshDccs.ToString()})", EditorStyles.boldLabel, GUILayout.Width(200));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                projectSetting.meshDccPath = GUILayout.TextField(projectSetting.meshDccPath, GUILayout.Width(300));
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string file = EditorUtility.OpenFilePanel($"Select {projectSetting.meshDccs.ToString()}'s path", Application.dataPath, "exe");
                    if (File.Exists(file))
                        projectSetting.meshDccPath = file;
                }
                if (GUILayout.Button("Detect", GUILayout.Width(55)))
                {
                    projectSetting.meshDccPath = DCCLauncher.GetMeshDCCPath(projectSetting.meshDccs);
                    if (File.Exists(projectSetting.meshDccPath) == false)
                        Debug.LogWarning($"Could not find the path of {projectSetting.meshDccs.ToString()}");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            GUILayout.EndVertical();



            GUILayout.BeginVertical("Box");

            EditorGUI.BeginChangeCheck();
            projectSetting.paintingMeshDccs = (PaintingMeshDCCs)EditorGUILayout.EnumPopup(new GUIContent("Painting Mesh"), projectSetting.paintingMeshDccs);
            if (EditorGUI.EndChangeCheck())
            {
                projectSetting.paintingMeshDccPath = string.Empty;
            }

            if (projectSetting.paintingMeshDccs != PaintingMeshDCCs.None)
            {
                GUILayout.Label($"Path ({projectSetting.paintingMeshDccs.ToString()})", EditorStyles.boldLabel, GUILayout.Width(200));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                projectSetting.paintingMeshDccPath = GUILayout.TextField(projectSetting.paintingMeshDccPath, GUILayout.Width(300));
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string file = EditorUtility.OpenFilePanel($"Select {projectSetting.paintingMeshDccs.ToString()}'s path", Application.dataPath, "exe");
                    if (File.Exists(file))
                        projectSetting.paintingMeshDccPath = file;
                }
                if (GUILayout.Button("Detect", GUILayout.Width(55)))
                {
                    projectSetting.paintingMeshDccPath = DCCLauncher.GetPaintingMeshDCCPath(projectSetting.paintingMeshDccs);
                    if (File.Exists(projectSetting.paintingMeshDccPath) == false)
                        Debug.LogWarning($"Could not find the path of {projectSetting.paintingMeshDccs.ToString()}");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            GUILayout.EndVertical();



            GUILayout.BeginVertical("Box");

            EditorGUI.BeginChangeCheck();
            projectSetting.paintingTexDccs = (PaintingTexDCCs)EditorGUILayout.EnumPopup(new GUIContent("Painting Texture"), projectSetting.paintingTexDccs);
            if (EditorGUI.EndChangeCheck())
            {
                projectSetting.paintingTexDccPath = string.Empty;
            }

            if (projectSetting.paintingTexDccs != PaintingTexDCCs.None)
            {
                GUILayout.Label($"Path ({projectSetting.paintingTexDccs.ToString()})", EditorStyles.boldLabel, GUILayout.Width(200));
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                projectSetting.paintingTexDccPath = GUILayout.TextField(projectSetting.paintingTexDccPath, GUILayout.Width(300));
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string file = EditorUtility.OpenFilePanel($"Select {projectSetting.paintingTexDccs.ToString()}'s path", Application.dataPath, "exe");
                    if (File.Exists(file))
                        projectSetting.paintingTexDccPath = file;
                }
                if (GUILayout.Button("Detect", GUILayout.Width(55)))
                {
                    projectSetting.paintingTexDccPath = DCCLauncher.GetTextureDCCPath(projectSetting.paintingTexDccs);
                    if (File.Exists(projectSetting.paintingTexDccPath) == false)
                        Debug.LogWarning($"Could not find the path of {projectSetting.paintingTexDccs.ToString()}");
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            GUILayout.EndVertical();

            //if (EditorGUI.EndChangeCheck())
            //{
            //    EditorUtility.SetDirty(projectSetting);
            //    AssetDatabase.SaveAssets();
            //}

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            projectSetting.defaultShader = (Shader)EditorGUILayout.ObjectField(new GUIContent("Default Shader"), projectSetting.defaultShader, typeof(Shader), false);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label("Import Asset Path", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            exportAssetPath = GUILayout.TextField(exportAssetPath, GUILayout.Width(230));
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string path = EditorUtility.OpenFolderPanel("Import Asset Path", Application.dataPath, string.Empty);
                DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/../");
                string projDir = directoryInfo.FullName.Replace("\\", "/");
                path = path.Replace(projDir, string.Empty);

                if (AssetDatabase.IsValidFolder(path))
                {
                    exportAssetPath = path;
                    projectSetting.exportAssetPath = AssetDatabase.LoadAssetAtPath<Object>(exportAssetPath);
                    AssetDatabase.SaveAssets();
                }

            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            */

            GUILayout.BeginHorizontal();
            GUILayout.Label("External Reference", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            importAssetPath = GUILayout.TextField(importAssetPath, GUILayout.Width(230));
            if (EditorGUI.EndChangeCheck())
            {
                if (AssetDatabase.IsValidFolder(importAssetPath))
                {
                    projectSetting.importAssetPath = AssetDatabase.LoadAssetAtPath<Object>(importAssetPath);
                    AssetDatabase.SaveAssets();
                }
                else
                {
                    projectSetting.importAssetPath = null;
                    AssetDatabase.SaveAssets();
                }
            }

            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string path = EditorUtility.OpenFolderPanel("Select a folder as External Reference", Application.dataPath, string.Empty);
                DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/../");
                string projDir = directoryInfo.FullName.Replace("\\", "/");
                path = path.Replace(projDir, string.Empty);

                if (AssetDatabase.IsValidFolder(path))
                {
                    importAssetPath = path;
                    projectSetting.importAssetPath = AssetDatabase.LoadAssetAtPath<Object>(importAssetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            GUILayout.EndVertical();
        }
    }

}