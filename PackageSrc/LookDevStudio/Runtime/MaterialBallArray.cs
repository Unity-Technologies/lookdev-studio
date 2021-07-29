using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LookDev
{
    public class MaterialBallArray : MonoBehaviour
    {
        private static readonly string MaterialBallAssetPrefixKey = "LookDev_ViewMaterialPath_";
        
        public List<Renderer> MaterialBallRenderers;
        public Texture2D DisplayIcon;

        private int m_currentMaterial = 0;

        public Material LoadMaterialAt(int i)
        {
            string key = $"{MaterialBallAssetPrefixKey}{i}";
            
            #if UNITY_EDITOR
            var assetPath = UnityEditor.EditorPrefs.GetString(key, string.Empty);
            try
            {
                Material m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                SetMaterial(i, m);
                return m;
            }
            catch (System.Exception)
            {
                // noop
            }
            #else
            // Code to load materials from a save at runtime
            #endif

            return null;
        }

        public void SetMaterial(int i, Material m)
        {
            if(i < 0 || i >= MaterialBallRenderers.Count)
            {
                return;
            }

            m_currentMaterial = (m_currentMaterial + 1) % MaterialBallRenderers.Count;

            MaterialBallRenderers[i].material = m;
            
            #if UNITY_EDITOR
            string key = $"{MaterialBallAssetPrefixKey}{i}";
            string path = AssetDatabase.GetAssetPath(m);
            UnityEditor.EditorPrefs.SetString(key, path);
            #endif
        }

        public void SetMaterial(Material m)
        {
            SetMaterial(m_currentMaterial, m);
        }
    }
}