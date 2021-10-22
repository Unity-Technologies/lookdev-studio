using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace LookDev.Editor
{
    public class ProjectSettingWindow : EditorWindow
    {
        const string lookdevProjectFolder = "Assets/LookDevProjects";
        readonly string defaultProjectSettingPath = $"{lookdevProjectFolder}/DefaultProjectSetting.asset";

        public static string currentProjectSettingPath;
        public static ProjectSetting projectSetting;

        ProjectSettingEditorWindow ProjectSettingEditorWindow;


        public void ApplyProjectSettings(string projectSettingPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(projectSettingPath) == true)
            {
                projectSetting = AssetDatabase.LoadAssetAtPath<ScriptableObject>(projectSettingPath) as ProjectSetting;

                if (projectSetting != null)
                {
                    currentProjectSettingPath = projectSettingPath;

                    // Refresh Tabs
                    LookDevSearchHelpers.RefreshWindow();

                    // Apply post-processing on importing

                    // Apply DCCs settings
                    switch(projectSetting.meshDccs)
                    {
                        case MeshDCCs.Maya:
                            DCCLauncher.mayaPath = projectSetting.meshDccPath;
                            break;
                        case MeshDCCs.Max:
                            DCCLauncher.maxPath = projectSetting.meshDccPath;
                            break;
                        /*
                        case MeshDCCs.Blender:
                            DCCLauncher.blenderPath = projectSetting.meshDccPath;
                            break;
                        */
                    }

                    switch(projectSetting.paintingMeshDccs)
                    {
                        case PaintingMeshDCCs.Substance_Painter:
                            DCCLauncher.substancePainterPath = projectSetting.paintingMeshDccPath;
                            break;
                    }

                    switch(projectSetting.paintingTexDccs)
                    {
                        case PaintingTexDCCs.Photoshop:
                            DCCLauncher.photoShopPath = projectSetting.paintingTexDccPath;
                            break;
                    }
                    
                }
            }
        }

        public static string GetLookDevProjectFolder()
        {
            return lookdevProjectFolder;
        }

        public static string GetCurrentProjectName()
        {
            return System.IO.Path.GetFileNameWithoutExtension(currentProjectSettingPath);
        }


        public static void CreateNewProjectSettings(string fileName)
        {
            if (AssetDatabase.IsValidFolder(lookdevProjectFolder) == false)
                AssetDatabase.CreateFolder("Assets", "LookDevProjects");

            ProjectSetting newProjectSetting = ScriptableObject.CreateInstance<ProjectSetting>();

            string targetFilePath = $"{lookdevProjectFolder}/{fileName}.asset";

            targetFilePath = AssetDatabase.GenerateUniqueAssetPath(targetFilePath);

            AssetDatabase.CreateAsset(newProjectSetting, targetFilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        private void OnEnable()
        {
            // Apply default project setting
            if (projectSetting == null)
                projectSetting = ScriptableObject.CreateInstance<ProjectSetting>();

            if (AssetDatabase.IsValidFolder(lookdevProjectFolder) == false)
                AssetDatabase.CreateFolder("Assets", "LookDevProjects");

            if (AssetDatabase.LoadAssetAtPath<Object>(defaultProjectSettingPath) == false)
            {
                // Generate LDS Default project setting
                AssetDatabase.CreateAsset(projectSetting, defaultProjectSettingPath);
                AssetDatabase.SaveAssets();
            }

            ApplyProjectSettings(defaultProjectSettingPath);
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Resources.Load<Texture>("Icon_Setting"), GUILayout.Width(30), GUILayout.Height(30)))
            {
                ProjectSettingEditorWindow = EditorWindow.GetWindow<ProjectSettingEditorWindow>("Project Settings", true);
                ProjectSettingEditorWindow.SetCurrentProjectSetting(projectSetting);
                ProjectSettingEditorWindow.ResetWindowPosition();
                ProjectSettingEditorWindow.ShowModalUtility();

                GUIUtility.ExitGUI();
                ApplyProjectSettings(currentProjectSettingPath);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}