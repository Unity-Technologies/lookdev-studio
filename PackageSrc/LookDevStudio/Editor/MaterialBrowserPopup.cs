using System.Collections;
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

        const string materialPresetPath = "Packages/com.unity.lookdevstudio/Setup/Settings/MaterialPreset";

        public override void OnGUI(Rect rect)
        {
            for (int i=0;i<m_MaterialPresets.Count;i++)
            {
                //string[] tokens = m_MaterialPresets[i].Split('/');
                //string materialName = tokens[tokens.Length - 1];


                if (GUILayout.Button(m_MaterialPresets[i]))
                {
                    Object genMaterial = AssetManageHelpers.CreateMaterialByPresetName("HDRP/Lit", m_MaterialPresets[i]);

                    if (genMaterial != null)
                        m_Presets[i].ApplyTo(genMaterial);

                    editorWindow.Close();
                }
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
            return new Vector2(115, 150);
        }
        
    }
}