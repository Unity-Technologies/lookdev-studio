using UnityEngine;
using System.IO;
using LookDev.Editor;
using UnityEditor;
using Object = UnityEngine.Object;

namespace LookDev
{
    public class AssetBrowser : EditorWindow
    {
        public GUISkin m_GUISkin;
        GUIStyle m_UIStyleDefault;
        GUIStyle m_UIStyleSelected;

        const int m_columeCount = 4;

        string m_selectedModelPath;

        public string GetSelectedModelPath()
        {
            return m_selectedModelPath;
        } 

        public AssetBrowser()
        {
            titleContent.text = "Models";

            LookDevHelpers.OnDragAndDropPostImportEvent += (x) =>
            {
                m_selectedModelPath = x;
            };
        }

        void OnEnable()
        {
            m_UIStyleDefault = new GUIStyle(m_GUISkin.box);
            m_UIStyleSelected = new GUIStyle(m_UIStyleDefault);
            m_UIStyleSelected.normal.background = null;
        }

        private void OnGUI()
        {
            AssetHolder holder = FindObjectOfType<AssetHolder>();
            Debug.Assert(holder != null);
            if (holder.Owner != GetType().Name)
            {
                Object lastHeroAsset = LookDevHelpers.GetLastHeroAsset();

                LookDevHelpers.SetHeroAsset(lastHeroAsset);

                if (lastHeroAsset != null)
                {
                    m_selectedModelPath = AssetDatabase.GetAssetPath(lastHeroAsset);
                }
                holder.Owner = GetType().Name;
            }
            

            LookDevHelpers.DragDrop();

            Buttons();
        }

        public void DuplicateModel()
        {
            if (string.IsNullOrEmpty(m_selectedModelPath))
                return;

            GameObject targetGo = AssetDatabase.LoadAssetAtPath<GameObject>(m_selectedModelPath);

            string sourcePath = m_selectedModelPath;

            if (targetGo != null)
            {
                string targetPath = AssetDatabase.GenerateUniqueAssetPath(m_selectedModelPath);

                AssetDatabase.CopyAsset(sourcePath, targetPath);

                m_selectedModelPath = targetPath;
            }
        }

        public void DeleteModel()
        {
            if (string.IsNullOrEmpty(m_selectedModelPath))
                return;

            GameObject targetGo = AssetDatabase.LoadAssetAtPath<GameObject>(m_selectedModelPath);

            if (targetGo != null)
            {

                if (EditorUtility.DisplayDialog("Delete the Model", string.Format("\"{0}\"\nAre you sure to delete the model?", m_selectedModelPath), "Yes", "No"))
                {
                    Selection.activeObject = null;
                    AssetDatabase.DeleteAsset(m_selectedModelPath);
                    m_selectedModelPath = string.Empty;
                }
            }
        }
        
        public void ImportModel()
        {
            string targetFile = EditorUtility.OpenFilePanel("Select the target FBX to be imported", Application.dataPath, "fbx");


            if (!File.Exists(targetFile))
            {
                Debug.LogError($"File does not exist: {targetFile}. Aborting.");
                return;
            }

            LookDevHelpers.Import(targetFile);
        }


        private void Buttons()
        {
            var filePaths = AssetDatabase.FindAssets("t:Model", new string[]
            {
                Path.Combine("Assets", LookDevHelpers.LookDevSubdirectoryForModel),
            });

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal("Box", GUILayout.Width(position.width - 8f));


            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_NewMat"), "Import Model"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                ImportModel();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DupMat"), "Duplicate Model"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                DuplicateModel();
            }

            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("Icon_DelMat"), "Delete Model"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                DeleteModel();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical("Box");
            // Account for partial rows.
            int maxRows = Mathf.CeilToInt(filePaths.Length / (float)m_columeCount);

            for (int curRow=0; curRow < maxRows; curRow++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int curColumn = 0; curColumn < m_columeCount; curColumn++)
                {
                    int pathIndex = m_columeCount * curRow + curColumn;

                    // For the partial final row, fill up as far as we can, then exit.
                    if (pathIndex >= filePaths.Length)
                        return;

                    string path = AssetDatabase.GUIDToAssetPath(filePaths[pathIndex]);
                    
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

                    GUIStyle style = (path != m_selectedModelPath)
                        ? m_UIStyleSelected
                        : m_UIStyleDefault;
                    
                    EditorGUILayout.BeginVertical(style);
                    GuiButton(obj);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void GuiButton(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(target)), GUILayout.Width((position.width - 64) / m_columeCount), GUILayout.Height((position.width - 64) / m_columeCount)))
            {
                m_selectedModelPath = AssetDatabase.GetAssetPath(target);
                LookDevHelpers.ReplaceHeroAsset(target as GameObject);
            }

            GUIStyle bStyle = new GUIStyle("Button");
            bStyle.fontSize = bStyle.fontSize - 2;
            bStyle.alignment = TextAnchor.MiddleLeft;

            GUILayout.Label(new GUIContent(target.name), bStyle, GUILayout.Width((position.width - 64) / m_columeCount));


        }
    }
}