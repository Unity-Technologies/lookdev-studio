using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    public class TextureBrowser : EditorWindow
    {
        public GUISkin m_GUISkin;

        private LookDevConfig m_config;

        private int m_columeCount = 5;

        private string m_selectedTexturePath;

        private Vector2 m_scrollVector;

        public void OnFocus()
        {
            titleContent.text = "Textures";
            
            if (m_config == null)
            {
                m_config = AssetDatabase.LoadAssetAtPath<LookDevConfig>("Assets/LookDev/Setup/LookDevConfig.asset");
            }
            
        }




        public void DuplicateTexture()
        {
            if (string.IsNullOrEmpty(m_selectedTexturePath))
                return;

            Texture targetMat = AssetDatabase.LoadAssetAtPath<Texture>(m_selectedTexturePath);

            string sourcePath = m_selectedTexturePath;

            if (targetMat != null)
            {
                string targetPath = AssetDatabase.GenerateUniqueAssetPath(m_selectedTexturePath);

                AssetDatabase.CopyAsset(sourcePath, targetPath);

                m_selectedTexturePath = targetPath;
                SelectCurrentTexture();
            }

        }

        public void DeleteTexture()
        {
            if (string.IsNullOrEmpty(m_selectedTexturePath))
                return;

            Texture targetMat = AssetDatabase.LoadAssetAtPath<Texture>(m_selectedTexturePath);

            if (targetMat != null)
            {
                if (EditorUtility.DisplayDialog("Delete the Texture", string.Format("\"{0}\"\nAre you sure to delete the texture?", m_selectedTexturePath), "Yes", "No"))
                {
                    Selection.activeObject = null;
                    AssetDatabase.DeleteAsset(m_selectedTexturePath);
                    m_selectedTexturePath = string.Empty;
                }
            }

            
        }


        void SelectCurrentTexture()
        {
            if (!string.IsNullOrEmpty(m_selectedTexturePath))
            {
                Texture targetMat = AssetDatabase.LoadAssetAtPath<Texture>(m_selectedTexturePath);

                if (targetMat != null)
                {
                    Selection.activeObject = targetMat;
                }
            }
        }


        public void ImportTexture()
        {
            string targetFile = EditorUtility.OpenFilePanelWithFilters("Select the target texture to be imported", Application.dataPath, new string[] { "png", "png", "tga", "tga", "tif", "tif" });
            //string targetFile = EditorUtility.OpenFilePanel("Select the target texture to be imported", Application.dataPath, "fbx");

            if (!File.Exists(targetFile))
            {
                Debug.LogError($"File does not exist: {targetFile}. Aborting.");
                return;
            }

            LookDevHelpers.Import(targetFile);

        }



        private void OnGUI()
        {
            // Yuck, hack
            AssetHolder holder = FindObjectOfType<AssetHolder>();
            Debug.Assert(holder != null);
            if (holder.Owner != GetType().Name || LookDevHelpers.GetLookDevContainer().transform.childCount == 0)
            {
                holder.Owner = GetType().Name;
            }

            LookDevHelpers.DragDrop();


            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal("Box", GUILayout.Width(position.width - 8f));

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMat"), "Import Texture"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                ImportTexture();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DupMat"), "Duplicate Texture"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                DuplicateTexture();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DelMat"), "Delete Texture"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                DeleteTexture();
            }

            EditorGUILayout.EndHorizontal();


            var texs = AssetDatabase.FindAssets("t:texture", new string[] {"Assets/LookDev/Textures"});
                if (texs.Length == 0)
                    return;


            EditorGUILayout.BeginVertical("Box");

            m_scrollVector = EditorGUILayout.BeginScrollView(m_scrollVector, false, false, GUILayout.Height(position.height/2), GUILayout.Width(position.width));

                for (int i=0;i<texs.Length/ m_columeCount + 1;i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int j=0;j< m_columeCount; j++)
                    {
                        if (m_columeCount * i + j >= texs.Length)
                            continue;

                        string path = AssetDatabase.GUIDToAssetPath(texs[m_columeCount * i + j]);

                        GUIStyle gUIStyle;

                        gUIStyle = new GUIStyle(m_GUISkin.box);

                        if (path != m_selectedTexturePath)
                            gUIStyle.normal.background = null;

                        EditorGUILayout.BeginVertical(gUIStyle);
                        if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Texture>(path)), Path.GetFileNameWithoutExtension(path)), GUILayout.Width((position.width-64) / m_columeCount), GUILayout.Height((position.width-64) / m_columeCount)))
                        {
                            m_selectedTexturePath = path;
                            SelectCurrentTexture();
                            if (Event.current.button == 1)
                            {
                                Vector2 vec = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                                MaterialPopupMenu.OpenPopup(new Rect(vec.x, vec.y, 120, 46));
                            }
                        }

                        GUIStyle bStyle = new GUIStyle("Button");
                        bStyle.fontSize = bStyle.fontSize - 2;
                        bStyle.alignment = TextAnchor.MiddleLeft;

                        GUILayout.Label(new GUIContent(Path.GetFileNameWithoutExtension(path)), bStyle, GUILayout.Width((position.width - 64) / m_columeCount));
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();
                
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

        }
    }
}