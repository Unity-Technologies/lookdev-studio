using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    public class MaterialBrowser : EditorWindow
    {
        public GUISkin m_GUISkin;

        private static readonly string LastNumBallsShownKey = "LookDev_LastNumBalls";
        

        private int m_numBalls;
        private Renderer[] m_materialBalls;
        private LookDevConfig m_config;
        private bool m_instantiated;

        private int m_columeCount = 3;

        private string m_selectedMaterialPath;

        private Vector2 m_scrollVector;

        private static MaterialBrowser inst; 

        public static MaterialBrowser Inst
        {
            get
            {
                return inst ?? EditorWindow.GetWindow<MaterialBrowser>();
            }
        }

        public void SetLatestlySelectedMaterialPath(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                if (m_selectedMaterialPath != path)
                {
                    m_selectedMaterialPath = path;
                    SelectCurrentMaterial();
                }
            }
        }

        public string GetLatestlySelectedMaterialPath()
        {
            return m_selectedMaterialPath;
        }

        public void OnFocus()
        {
            titleContent.text = "Materials";
            
            if (m_config == null)
            {
                m_config = AssetDatabase.LoadAssetAtPath<LookDevConfig>("Assets/LookDev/Setup/LookDevConfig.asset");
            }
            
        }

 

        public void SetNumBalls(int numBalls)
        {
            EditorPrefs.GetInt(LastNumBallsShownKey, numBalls);
            SetupBalls(numBalls);
        }

        public void LoadMaterial(string assetPath)
        {
            try
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                //GetArray().SetMaterial(m);
            }
            catch (System.Exception)
            {
                // noop
            }
        }


        public void CreateDefaultMaterial()
        {
            string defaultMaterialName = "Default";

            string newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial, defaultMaterialName);
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            Material newMaterial = new Material(Shader.Find("HDRP/Autodesk Interactive/AutodeskInteractive"));

            AssetDatabase.CreateAsset(newMaterial, newAssetPath);

            m_selectedMaterialPath = newAssetPath;
            SelectCurrentMaterial();

            /*
            AssetDatabase.WriteImportSettingsIfDirty(newAssetPath);
            AssetDatabase.ImportAsset(newAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            */
        }

        public void CreateMaterialByShaderName(string shaderName)
        {
            string[] tokens = shaderName.Split('/');

            string defaultMaterialName = tokens[tokens.Length - 1];

            string newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial, defaultMaterialName);
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            Shader targetShader = Shader.Find(shaderName);

            if (targetShader != null)
            {
                Material newMaterial = new Material(targetShader);
                AssetDatabase.CreateAsset(newMaterial, newAssetPath);

                m_selectedMaterialPath = newAssetPath;
                SelectCurrentMaterial();

            }
        }


        public void DuplicateMaterial()
        {
            if (string.IsNullOrEmpty(m_selectedMaterialPath))
                return;

            Material targetMat = AssetDatabase.LoadAssetAtPath<Material>(m_selectedMaterialPath);

            string sourcePath = m_selectedMaterialPath;

            if (targetMat != null)
            {
                string targetPath = AssetDatabase.GenerateUniqueAssetPath(m_selectedMaterialPath);

                AssetDatabase.CopyAsset(sourcePath, targetPath);

                m_selectedMaterialPath = targetPath;
                SelectCurrentMaterial();
            }

        }

        public void DeleteMaterial()
        {
            if (string.IsNullOrEmpty(m_selectedMaterialPath))
                return;

            Material targetMat = AssetDatabase.LoadAssetAtPath<Material>(m_selectedMaterialPath);

            if (targetMat != null)
            {
                if (EditorUtility.DisplayDialog("Delete the Material", string.Format("\"{0}\"\nAre you sure to delete the material?", m_selectedMaterialPath), "Yes", "No"))
                {
                    Selection.activeObject = null;
                    AssetDatabase.DeleteAsset(m_selectedMaterialPath);
                    m_selectedMaterialPath = string.Empty;
                }
            }
        }


        MaterialBallArray GetArray()
        {
            return FindObjectOfType<MaterialBallArray>();
        }

        void SetupBalls(int numBalls)
        {
            EditorPrefs.SetInt(LastNumBallsShownKey, numBalls);

            MaterialBallArray prefab = null;
            foreach (var layout in m_config.Layouts)
            {
                if (layout.MaterialBallRenderers.Count == numBalls)
                {
                    prefab = layout;
                    break;
                }
            }

            if (prefab == null)
                return;

            LookDevHelpers.SetHeroAsset(prefab.gameObject, false); 

            // retrieve materials
            for (int i = 0; i < numBalls; i++)
            {
                GetArray().LoadMaterialAt(i);
            }
        }

        void SelectCurrentMaterial()
        {
            if (!string.IsNullOrEmpty(m_selectedMaterialPath))
            {
                Material targetMat = AssetDatabase.LoadAssetAtPath<Material>(m_selectedMaterialPath);

                if (targetMat != null)
                {
                    Selection.activeObject = targetMat;
                    //EditorPrefs.SetString("LATEST_SELECTION", m_selectedMaterialPath);
                }
            }
        }



        private void OnGUI()
        {
            // Yuck, hack
            AssetHolder holder = FindObjectOfType<AssetHolder>();
            Debug.Assert(holder != null);
            if (holder.Owner != GetType().Name || LookDevHelpers.GetLookDevContainer().transform.childCount == 0)
            {
                //SetupBalls(EditorPrefs.GetInt(LastNumBallsShownKey, 6));
                holder.Owner = GetType().Name;
            }



            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal("Box", GUILayout.Width(position.width - 8f));

            
            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMat"), "New Material"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                CreateDefaultMaterial();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DupMat"), "Duplicate Material"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                DuplicateMaterial();
            }

            
            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMatPreset"), "Create Material by the Presets"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                //Vector2 vec = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                //MaterialBrowserPopup.OpenPopup(new Rect(vec.x, vec.y, 95, 110));
            }
            //GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMatGrp"), "Create New Material Group"), GUILayout.Width(32), GUILayout.Height(32));


            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DelMat"), "Delete Material"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                DeleteMaterial();
            }


            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_AssignMat"), "Assign Material on Selection"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                LoadMaterial(m_selectedMaterialPath);
            }

            
            EditorGUILayout.EndHorizontal();



            var mats = AssetDatabase.FindAssets("t:material", new string[] {"Assets/LookDev/Materials"});
                if (mats.Length == 0)
                    return;


            m_scrollVector = EditorGUILayout.BeginScrollView(m_scrollVector, false, false, GUILayout.Height(position.height/2), GUILayout.Width(position.width));

                for (int i=0;i<mats.Length/ m_columeCount + 1;i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int j=0;j< m_columeCount; j++)
                    {
                        if (m_columeCount * i + j >= mats.Length)
                            continue;

                        string path = AssetDatabase.GUIDToAssetPath(mats[m_columeCount * i + j]);

                        GUIStyle gUIStyle;

                        gUIStyle = new GUIStyle(m_GUISkin.box);

                        if (path != m_selectedMaterialPath)
                            gUIStyle.normal.background = null;

                        EditorGUILayout.BeginVertical(gUIStyle);
                        if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Material>(path)), Path.GetFileNameWithoutExtension(path)), GUILayout.Width((position.width-64) / m_columeCount), GUILayout.Height((position.width-64) / m_columeCount)))
                        {
                            m_selectedMaterialPath = path;
                            SelectCurrentMaterial();
                            if (Event.current.button == 1)
                            {
                                Vector2 vec = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                                MaterialPopupMenu.OpenPopup(new Rect(vec.x, vec.y, 120, 46));
                            }
                        }

                        /*
                        if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                        {
                            Debug.LogError("Over : " + i.ToString());
                        }
                        */
                        

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

        }
    }
}