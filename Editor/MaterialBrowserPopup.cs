using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace LookDev.Editor
{
    public class MaterialBrowserPopup : PopupWindowContent
    {
        // Opaque (Lit), Transparent (Lit), Skin (Lit), Foliage (Lit), Hair, Eye
        public static List<string> m_MaterialPresets = new List<string>();
        public static List<Preset> m_Presets = new List<Preset>();

        const string materialPresetPath = "Assets/LookDev/Setup/Settings/MaterialPreset";

        public override void OnGUI(Rect rect)
        {
            if (GUILayout.Button("Default"))
            {
                CreateDefaultMaterial();
                editorWindow.Close();
            }

            for (int i=0;i<m_MaterialPresets.Count;i++)
            {

                if (GUILayout.Button(m_MaterialPresets[i]))
                {
                    Object genMaterial = AssetManageHelpers.CreateMaterialByPresetName(AssetManageHelpers.GetDefaultLitShader?.Invoke().name, m_MaterialPresets[i]);

                    if (genMaterial != null)
                        m_Presets[i].ApplyTo(genMaterial);

                    editorWindow.Close();
                }

                /*
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    Debug.Log($"Mouse Over : {i}");
                }
                */

            }
            
        }

        Shader GetDefaultShaderFromProjectSetting()
        {
            Shader defaultShader = null;

            if (ProjectSettingWindow.projectSetting != null)
            {
                if (ProjectSettingWindow.projectSetting.defaultShader != null)
                {
                    defaultShader = ProjectSettingWindow.projectSetting.defaultShader;
                }
            }

            return defaultShader;
        }

        void CreateDefaultMaterial()
        {
            Shader defaultShader = GetDefaultShaderFromProjectSetting();

            if (defaultShader != null)
            {
                Object genMaterial = AssetManageHelpers.CreateMaterialByPresetName(defaultShader.name, System.IO.Path.GetFileNameWithoutExtension(defaultShader.name));
            }
            else
            {
                AssetManageHelpers.CreateDefaultMaterial();
            }

        }

        void RefreshMaterialPresetList()
        {
            m_MaterialPresets.Clear();
            m_Presets.Clear();

            string[] guids = AssetDatabase.FindAssets("t:preset", new string[] { materialPresetPath });

            foreach(string guid in guids)
            {
                string presetPath = AssetDatabase.GUIDToAssetPath(guid);
                string presetName = System.IO.Path.GetFileNameWithoutExtension(presetPath);
                Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(presetPath);

                if (string.IsNullOrEmpty(presetName) == false && preset != null)
                {
                    if (!m_MaterialPresets.Contains(presetName))
                    {
                        m_MaterialPresets.Add(presetName);
                        m_Presets.Add(preset);
                    }
                }

            }
        }

        public override void OnOpen()
        {
            RefreshMaterialPresetList();
        }

        public override void OnClose()
        {
            //base.OnClose();
        }

        
        public override Vector2 GetWindowSize()
        {
            if (LookDevStudioEditor.IsHDRP())
                return new Vector2(115, 150);
            else
                return new Vector2(115, 70);
        }
        
    }
}