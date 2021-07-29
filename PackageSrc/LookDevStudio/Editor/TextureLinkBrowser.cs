using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{
    public class TextureLinkBrowser : EditorWindow
    {

        static TextureLinkBrowser m_TextureLinkBrowser;

        static public List<Rect> m_LoadedTextureCardWindow = new List<Rect>();
        static public List<Rect> m_LoadedTextureSlot = new List<Rect>();
        List<int> TextureSlotIDs = new List<int>();


        static public List<Texture> m_LoadedTextures = new List<Texture>();
        public List<string> m_LoadedTexturePaths = new List<string>();

        public List<GameObject> m_LoadedModels = new List<GameObject>();

        public Material m_currentLoadedMaterial;
        int m_currentSelectedMaterialIndex = -1;

        static public List<Material> m_LoadedMaterials = new List<Material>();
        public List<string> m_LoadedMaterialPaths = new List<string>();

        List<Texture> existingTextures = new List<Texture>();
        List<Rect> existingTextureCardWindow = new List<Rect>();


        // To do : Composite Texture Slots 
        //static public List<Rect> compositeTextureSlot = new List<Rect>();

        public bool isEnalbed = false;

        int latestHandlingWinID = -1;

        UnityEditor.Editor materialPreviewEditor;
        UnityEditor.Editor modelPreviewEditor;
        UnityEditor.Editor texturePreviewEditor;


        static List<string> TextureTypeDescription = new List<string>()
        {
            "Base Map",
            "Normal Map",
            "Mask Map",
            "Bent Normal map",
            "Subsurface Mask Map",
            "Thickness Map"
        };

        static List<string> TextureName = new List<string>()
        {
        "_BaseColorMap",
        "_NormalMap",
        "_MaskMap",
        "_BentNormalMap",
        "_SubsurfaceMaskMap",
        "_ThicknessMap"
        };

        // Place the texture cards on the table
        int texWinSize = 90;
        int texWinOffset = 16;

        public static TextureLinkBrowser Inst
        {
            get
            {
                return m_TextureLinkBrowser ?? GetWindow<TextureLinkBrowser>();
            }
        }

        Dictionary<int, List<string>> LitTextureGroup = new Dictionary<int, List<string>>()
        {   
            // SubSurface Scaterring
            {0, new List<string>() { "_BaseColorMap", "_MaskMap", "_NormalMap", "_BentNormalMap", "_SubsurfaceMaskMap", "_ThicknessMap", "_CoatMaskMap", "_DetailMap", "_EmissiveColorMap" }},

            // Standard
            {1, new List<string>() { "_BaseColorMap", "_MaskMap", "_NormalMap", "_BentNormalMap", "_CoatMaskMap", "_DetailMap", "_EmissiveColorMap" }},

            // Anisotropy
            {2, new List<string>() { "_BaseColorMap", "_MaskMap", "_NormalMap", "_BentNormalMap", "_AnisotropyMap", "_TangentMap", "_CoatMaskMap", "_DetailMap", "_EmissiveColorMap" }},

            // Iridescence
            {3, new List<string>() { "_BaseColorMap", "_MaskMap", "_NormalMap", "_BentNormalMap", "_IridescenceMaskMap", "_IridescenceThicknessMap", "_CoatMaskMap", "_DetailMap", "_EmissiveColorMap" }},

            // Specular Color
            {4, new List<string>() { "_BaseColorMap", "_MaskMap", "_NormalMap", "_BentNormalMap", "_SpecularColorMap", "_CoatMaskMap", "_DetailMap", "_EmissiveColorMap" }},

            // Translucent
            {5, new List<string>() { "_BaseColorMap", "_MaskMap", "_NormalMap", "_BentNormalMap", "_ThicknessMap", "_CoatMaskMap", "_DetailMap", "_EmissiveColorMap" }},
        };

        Dictionary<string, string> LitTextureDesc = new Dictionary<string, string>()
        {
            {"_BaseColorMap", "Base Map"},
            {"_MaskMap", "Mask Map"},
            {"_NormalMap", "Normal Map"},
            {"_BentNormalMap", "Bent normal map"},
            {"_SubsurfaceMaskMap", "Subsurface Mask Map"},
            {"_ThicknessMap", "Thickness Map"},
            {"_CoatMaskMap", "Coat Mask"},
            {"_DetailMap", "Detail Map"},
            {"_EmissiveColorMap", "Emissive Map"},
            {"_AnisotropyMap", "Anisotropy Map"},
            {"_TangentMap", "Tangent Map"},
            {"_IridescenceMaskMap", "Iridescence Mask Map"},
            {"_IridescenceThicknessMap", "Iridescence Thickness Map"},
            {"_SpecularColorMap", "Specular Color Map"},
        };

        Dictionary<string, string> LitKeywordGroup = new Dictionary<string, string>()
        {
            //{"_BaseColorMap", ""},
            {"_MaskMap", "_MASKMAP"},
            {"_NormalMap", "_NORMALMAP"},
            {"_BentNormalMap", "_BENTNORMALMAP"},
            {"_SubsurfaceMaskMap", "_SUBSURFACE_MASK_MAP"},
            {"_ThicknessMap", "_THICKNESSMAP"},
            {"_CoatMaskMap", "_MATERIAL_FEATURE_CLEAR_COAT"},
            {"_DetailMap", "_DETAIL_MAP"},
            {"_EmissiveColorMap", "_EMISSIVE_COLOR_MAP"},
            {"_AnisotropyMap", "_ANISOTROPYMAP"},
            {"_TangentMap", "_TANGENTMAP"},
            {"_IridescenceMaskMap", "_MATERIAL_FEATURE_IRIDESCENCE"},
            {"_IridescenceThicknessMap", "_IRIDESCENCE_THICKNESSMAP"},
            {"_SpecularColorMap", "_SPECULARCOLORMAP"},
        };

        List<string> GetLitTexturePropertiesByID(int materialID)
        {
            return LitTextureGroup[materialID];
        }


        public List<string> GetEssentialTexturePropertyNames(Material targetMat)
        {
            List<string> output = new List<string>();


            if (targetMat.shader.name == "HDRP/Lit" || targetMat.shader.name == "HDRP/LitTessellation")
            {
                int materialID = (int)targetMat.GetFloat("_MaterialID");
                output = GetLitTexturePropertiesByID(materialID);

                if ((int)targetMat.GetFloat("_DisplacementMode") != 0)
                {
                    if (output.Contains("_HeightMap") == false)
                        output.Add("_HeightMap");
                }
                else // if _DisplacementMode == 0
                {
                    if (output.Contains("_HeightMap") == true)
                        output.Remove("_HeightMap");
                }
            }
            
            return output;
        }


        /// <summary>
        /// Extract material properties & names from the targetMaterial
        /// </summary>
        /// <param name="targetMat"></param>
        public void RefreshTextureTypeAndNameOnMaterial(Material targetMat)
        {
            
            TextureTypeDescription.Clear();
            TextureName.Clear();

            if (targetMat == null)
            {
                Debug.LogError("Cound not Refresh Types and names : Null Material");
                return;
            }

            
            List<string> includedPropertyNames = GetEssentialTexturePropertyNames(targetMat);

            for (int i=0;i< targetMat.shader.GetPropertyCount();i++)
            {
                int id = i;

                if (targetMat.shader.GetPropertyType(id) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    string pName = targetMat.shader.GetPropertyName(id);
                    string pTypeDesc = targetMat.shader.GetPropertyDescription(id);

                    // To filter unused texture properties by Lit's material type
                    if (includedPropertyNames.Count != 0)
                    {
                        if (!includedPropertyNames.Contains(pName))
                            continue;
                    }

                    // Seems textures for the Unity internal purpose
                    if (pName.StartsWith("unity_"))
                        continue;

                    pTypeDesc = pTypeDesc.Replace("_", string.Empty);

                    TextureName.Add(pName);
                    TextureTypeDescription.Add(pTypeDesc);
                }
            }

        }


        Rect AllocatorWindowSize = new Rect(160, 21, 1600, 900);

        private void OnEnable()
        {
            isEnalbed = true;

            titleContent = new GUIContent("Texture Allocator", Resources.Load<Texture>("Icon_AssignMat"));

            if (SceneView.lastActiveSceneView != null)
            {
                Inst.position = AllocatorWindowSize;

                Inst.minSize = new Vector2(1280, 720);

            }
        }

        private void OnDisable()
        {
            DragDropModelPostProcessor.latestImportedAssets.Clear();

            isEnalbed = false;
        }

        public void RegisterMaterialPath(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                if (!m_LoadedMaterialPaths.Contains(path))
                    m_LoadedMaterialPaths.Add(path);
            }
            else
            {
                Debug.LogError($"Cound not find the Material : {path}");
            }
        }

        public void RegisterTexturePath(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                if (!m_LoadedTexturePaths.Contains(path))
                    m_LoadedTexturePaths.Add(path);
            }
            else
            {
                Debug.LogError($"Cound not find the Texture : {path}");
            }
        }

        public void ClearAllLoadedTextureAndMaterials()
        {
            // Clean & init arrays
            m_LoadedTextureCardWindow.Clear();
            m_LoadedTextures.Clear();
            m_LoadedTexturePaths.Clear();
            m_LoadedTextureSlot.Clear();
            TextureSlotIDs.Clear();

            existingTextureCardWindow.Clear();
            existingTextures.Clear();

            m_LoadedMaterials.Clear();
            m_LoadedMaterialPaths.Clear();
            m_currentLoadedMaterial = null;

            m_LoadedModels.Clear();


            latestHandlingWinID = -1;
        }

        public void RegisterAllMaterialAndTexturesPathsBySelection()
        {
            // Register Selected Materials

            foreach (Object obj in Selection.objects)
            {
                string objPath = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(objPath))
                    continue;

                if (obj.GetType() == typeof(UnityEngine.Material))
                {
                    RegisterMaterialPath(objPath);
                }
                else if (obj.GetType() == typeof(UnityEngine.Texture2D) || obj.GetType() == typeof(UnityEngine.Texture2D))
                {
                    RegisterTexturePath(objPath);
                }
                else if (obj.GetType() == typeof(UnityEngine.GameObject))
                {
                    GameObject currentGo = (GameObject)obj;
                    Renderer[] rends = currentGo.GetComponentsInChildren<Renderer>();

                    // Add Models
                    if (!m_LoadedModels.Contains(currentGo))
                        m_LoadedModels.Add(currentGo);

                    foreach(Renderer rend in rends)
                    {
                        foreach(Material mat in rend.sharedMaterials)
                        {
                            objPath = AssetDatabase.GetAssetPath(mat);
                            if (string.IsNullOrEmpty(objPath))
                                continue;
                            RegisterMaterialPath(objPath);
                        }
                    }

                }
            }
        }

        public void RegisterAllMaterialAndTexturesPathsByAssetList(List<string> assetPath)
        {
            // Register Selected Materials

            foreach(string currentPath in assetPath)
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(currentPath);
                string objPath = currentPath;

                if (string.IsNullOrEmpty(objPath))
                    continue;

                if (obj.GetType() == typeof(UnityEngine.Material))
                {
                    RegisterMaterialPath(objPath);
                }
                else if (obj.GetType() == typeof(UnityEngine.Texture2D) || obj.GetType() == typeof(UnityEngine.Texture2D))
                {
                    RegisterTexturePath(objPath);
                }
                else if (obj.GetType() == typeof(UnityEngine.GameObject))
                {
                    GameObject currentGo = (GameObject)obj;
                    Renderer[] rends = currentGo.GetComponentsInChildren<Renderer>();

                    // Add Models
                    if (!m_LoadedModels.Contains(currentGo))
                        m_LoadedModels.Add(currentGo);

                    foreach (Renderer rend in rends)
                    {
                        foreach (Material mat in rend.sharedMaterials)
                        {
                            objPath = AssetDatabase.GetAssetPath(mat);
                            if (string.IsNullOrEmpty(objPath))
                                continue;
                            RegisterMaterialPath(objPath);
                        }
                    }

                }
            }
        }


        public void LoadTextureFromTexturePaths()
        {
            // Load Textures from Input
            foreach (string currentTexPath in m_LoadedTexturePaths)
            {
                m_LoadedTextures.Add(AssetDatabase.LoadAssetAtPath<Texture>(currentTexPath));
            }
        }

        public void CreateTextureCardWindowFromInput()
        {
            for (int i = 0; i < m_LoadedTextures.Count; i++)
            {
                m_LoadedTextureCardWindow.Add(new Rect((i * (texWinSize + texWinOffset)), 600, texWinSize, texWinSize));
            }
        }

        public void LoadMaterialFromMaterialPaths()
        {
            // Load Materials from Input
            if (m_LoadedMaterialPaths.Count != 0)
            {
                foreach (string currentMatPath in m_LoadedMaterialPaths)
                {
                    if (AssetDatabase.LoadAssetAtPath<Object>(currentMatPath) != null)
                    {
                        Material curMat = AssetDatabase.LoadAssetAtPath<Material>(currentMatPath);

                        if (!m_LoadedMaterials.Contains(curMat))
                            m_LoadedMaterials.Add(curMat);

                    }
                }

                m_currentSelectedMaterialIndex = 0;
                m_currentLoadedMaterial = m_LoadedMaterials[m_currentSelectedMaterialIndex];

                RefreshTextureTypeAndNameOnMaterial(m_currentLoadedMaterial);
            }
        }

        public void RefreshCardSlot()
        {
            m_LoadedTextureSlot.Clear();

            for (int i = 0; i < TextureTypeDescription.Count; i++)
            {
                m_LoadedTextureSlot.Add(new Rect(i * 170, 450, 160, 160));
            }
        }

        public void ResetTextureSlotIDs()
        {
            TextureSlotIDs.Clear();

            for (int i = 0; i < TextureTypeDescription.Count; i++)
            {
                TextureSlotIDs.Add(-1);
            }
        }

        public void CreateMaterialEditor()
        {
            if (materialPreviewEditor != null)
                materialPreviewEditor.ResetTarget();

            if (modelPreviewEditor != null)
                modelPreviewEditor.ResetTarget();

            if (m_currentLoadedMaterial != null)
            {
                materialPreviewEditor = UnityEditor.Editor.CreateEditor(m_currentLoadedMaterial);
                materialPreviewEditor.Repaint();
            }

            if (m_LoadedModels.Count != 0)
            {
                modelPreviewEditor = UnityEditor.Editor.CreateEditor(m_LoadedModels[0]);
                modelPreviewEditor.Repaint();
            }
        }

        void MakeTextureCardAlreadyConnected()
        {
            if (m_currentLoadedMaterial == null)
                return;

            existingTextures.Clear();
            existingTextureCardWindow.Clear();

            for (int i = 0; i < TextureName.Count; i++)
            {
                string currentTexName = TextureName[i];

                Texture linkedTexture = m_currentLoadedMaterial.GetTexture(currentTexName);

                if (linkedTexture != null)
                {
                    TextureSlotIDs[i] = existingTextures.Count + 100;

                    existingTextures.Add(linkedTexture);

                    Rect existingTextureRect = new Rect(0, 0, texWinSize, texWinSize);

                    
                    Rect dummySlot = new Rect(i * 170, m_LoadedTextureSlot[0].y, m_LoadedTextureSlot[0].width, m_LoadedTextureSlot[0].height);
                    float offsetX = (dummySlot.width - existingTextureRect.width) / 2f + dummySlot.x;
                    float offsetY = (dummySlot.height - existingTextureRect.height) / 2f + dummySlot.y;

                    existingTextureRect.x = offsetX;
                    existingTextureRect.y = offsetY;

                    existingTextureCardWindow.Add(existingTextureRect);

                    
                }
            }
        }

        bool CheckMaterialExistency()
        {
            if (m_LoadedMaterialPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("No Asset selected", "Please select at least one Material or a Model which contains Materials and open the Texture Allocator again.", "Confirm");
                return false;
            }

            return true;
        }

        public void InitTextureLinkBrowserBySelection()
        {

            ClearAllLoadedTextureAndMaterials();

            RegisterAllMaterialAndTexturesPathsBySelection();

            LoadTextureFromTexturePaths();

            CheckMaterialExistency();

            CreateTextureCardWindowFromInput();

            LoadMaterialFromMaterialPaths();

            RefreshCardSlot();

            ResetTextureSlotIDs();

            CreateMaterialEditor();

            MakeTextureCardAlreadyConnected();

            //ExistingTextureToCards();

        }

        void ExistingTextureToCards()
        {
            for (int i=0;i<existingTextures.Count;i++)
            {
                string exTexturePath = AssetDatabase.GetAssetPath(existingTextures[i]);

                if (m_LoadedMaterialPaths.Contains(exTexturePath) == false)
                {
                    m_LoadedMaterialPaths.Add(exTexturePath);
                    m_LoadedTextures.Add(existingTextures[i]);
                    m_LoadedTextureCardWindow.Add(Rect.zero);
                }
            }

            OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);
        }


        public IEnumerator InitTextureLinkBrowserOnImportingWithDelay()
        {
            if (TextureLinkBrowser.Inst != null)
                TextureLinkBrowser.Inst.ShowNotification(new GUIContent("Please, wait for the preparation of the imported assets."), 5f);

            yield return new WaitForSecondsRealtime(5f);

            while(DragDropModelPostProcessor.AreReadyImportedAssets() == false)
                yield return new WaitForSecondsRealtime(1f);

            InitTextureLinkBrowserOnImporting();
        }


        void AutopopulateLatestTexture()
        {
            string swapTexturePath = string.Empty;
            Rect swapTexturePosition = Rect.zero;

            int i = m_LoadedTexturePaths.Count - 1; // Lastest Index that was just put

            string textureFileName = System.IO.Path.GetFileNameWithoutExtension(m_LoadedTexturePaths[i]);

            for (int j = 0; j < m_LoadedTextureSlot.Count; j++)
            {
                string texturePropertyName = TextureName[j];

                NameSet targetNameSet = new NameSet();
                targetNameSet.postfixes = new List<string>();

                foreach (NameSet texNameSet in LookDevNameRules.Inst.TextureNameSet)
                {
                    if (texNameSet.propertyName == texturePropertyName)
                    {
                        targetNameSet = texNameSet;
                        break;
                    }
                }

                if (targetNameSet.postfixes.Count == 0)
                    continue;

                bool postFixFound = false;

                for (int k = 0; k < targetNameSet.postfixes.Count; k++)
                {
                    if (textureFileName.ToLower().Trim().EndsWith(targetNameSet.postfixes[k].ToLower().Trim()))
                    {
                        postFixFound = true;
                        break;
                    }
                }

                if (postFixFound)
                {
                    /*
                    if (EditorUtility.DisplayDialog("Auto-populate Textures", $"Do you want to automatically link the textures to the material,\n according to the file name convention?\n[\"{m_LoadedTextures[i].name}\" will be assigned to \"{texturePropertyName}\"]\n", "Yes", "No") == false)
                        break;
                    */

                    float xOffset = ((m_LoadedTextureSlot[j].width - m_LoadedTextureCardWindow[i].width) / 2f) + m_LoadedTextureSlot[j].x;
                    float yOffset = ((m_LoadedTextureSlot[j].height - m_LoadedTextureCardWindow[i].height) / 2f) + m_LoadedTextureSlot[j].y;

                    m_LoadedTextureCardWindow[i] = new Rect(xOffset, yOffset, m_LoadedTextureCardWindow[i].width, m_LoadedTextureCardWindow[i].height);

                    if (LitKeywordGroup.ContainsKey(texturePropertyName))
                    {
                        string targetKeyword = LitKeywordGroup[texturePropertyName];
                        if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == false)
                            m_currentLoadedMaterial.EnableKeyword(targetKeyword);
                    }

                    // Check existing card on the TextureSlot

                    if (m_currentLoadedMaterial.GetTexture(texturePropertyName) != null && TextureSlotIDs[j] != -1)
                    {
                        int exCardId;

                        if (TextureSlotIDs[j] < 100)
                        {
                            exCardId = TextureSlotIDs[j];
                            m_LoadedTextureCardWindow[exCardId] = new Rect(m_LoadedTextureCardWindow[exCardId].x, m_LoadedTextureCardWindow[exCardId].y + 200, m_LoadedTextureCardWindow[exCardId].width, m_LoadedTextureCardWindow[exCardId].height);

                        }
                        else
                        {
                            exCardId = TextureSlotIDs[j] - 100;

                            swapTexturePosition = new Rect(existingTextureCardWindow[exCardId].x, existingTextureCardWindow[exCardId].y + 200, existingTextureCardWindow[exCardId].width, existingTextureCardWindow[exCardId].height);
                            swapTexturePath = AssetDatabase.GetAssetPath(existingTextures[exCardId]);

                            // remove existing card
                            existingTextures.RemoveAt(exCardId);
                            existingTextureCardWindow.RemoveAt(exCardId);

                            if (!m_LoadedTexturePaths.Contains(swapTexturePath))
                            {
                                m_LoadedTexturePaths.Add(swapTexturePath);
                                m_LoadedTextures.Add(AssetDatabase.LoadAssetAtPath<Texture>(swapTexturePath));
                                m_LoadedTextureCardWindow.Add(swapTexturePosition);

                                TextureSlotIDs[j] = exCardId;
                            }
                        }

                    }

                    m_currentLoadedMaterial.SetTexture(texturePropertyName, m_LoadedTextures[i]);

                    TextureSlotIDs[j] = i;

                    RefreshAllPreviews();

                    break;
                }

            }

            OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);
        }

        void AutopopulateTextures()
        {
            for (int i = 0; i < m_LoadedTexturePaths.Count; i++)
            {

                string textureFileName = System.IO.Path.GetFileNameWithoutExtension(m_LoadedTexturePaths[i]);

                for (int j = 0; j < m_LoadedTextureSlot.Count; j++)
                {
                    string texturePropertyName = TextureName[j];

                    NameSet targetNameSet = new NameSet();
                    targetNameSet.postfixes = new List<string>();

                    foreach (NameSet texNameSet in LookDevNameRules.Inst.TextureNameSet)
                    {
                        if (texNameSet.propertyName == texturePropertyName)
                        {
                            targetNameSet = texNameSet;
                            break;
                        }
                    }

                    if (targetNameSet.postfixes.Count == 0)
                        continue;

                    bool postFixFound = false;

                    for (int k=0;k<targetNameSet.postfixes.Count;k++)
                    {
                        if (textureFileName.ToLower().Trim().EndsWith(targetNameSet.postfixes[k].ToLower().Trim()))
                        {
                            postFixFound = true;
                            break;
                        }
                    }

                    if (postFixFound)
                    {
                        /*
                        if (EditorUtility.DisplayDialog("Auto-populate Textures", $"Do you want to automatically link the textures to the material,\n according to the file name convention?\n[\"{m_LoadedTextures[i].name}\" will be assigned to \"{texturePropertyName}\"]\n", "Yes", "No") == false)
                            break;
                        */

                        float xOffset = ((m_LoadedTextureSlot[j].width - m_LoadedTextureCardWindow[i].width) / 2f) + m_LoadedTextureSlot[j].x;
                        float yOffset = ((m_LoadedTextureSlot[j].height - m_LoadedTextureCardWindow[i].height) / 2f) + m_LoadedTextureSlot[j].y;

                        m_LoadedTextureCardWindow[i] = new Rect(xOffset, yOffset, m_LoadedTextureCardWindow[i].width, m_LoadedTextureCardWindow[i].height);
                        
                        if (LitKeywordGroup.ContainsKey(texturePropertyName))
                        {
                            string targetKeyword = LitKeywordGroup[texturePropertyName];
                            if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == false)
                                m_currentLoadedMaterial.EnableKeyword(targetKeyword);
                        }

                        m_currentLoadedMaterial.SetTexture(texturePropertyName, m_LoadedTextures[i]);

                        TextureSlotIDs[j] = i;

                        RefreshAllPreviews();

                        break;
                    }

                }
            }

            OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);

        }

        public void InitTextureLinkBrowserOnImporting()
        {

            ClearAllLoadedTextureAndMaterials();

            RegisterAllMaterialAndTexturesPathsByAssetList(DragDropModelPostProcessor.GetLatestImportedAssets());

            CheckMaterialExistency(); ///

            LoadTextureFromTexturePaths();

            CreateTextureCardWindowFromInput();

            LoadMaterialFromMaterialPaths();

            RefreshCardSlot();

            ResetTextureSlotIDs();

            CreateMaterialEditor();

            MakeTextureCardAlreadyConnected();

            AutopopulateTextures();

            DragDropModelPostProcessor.latestImportedAssets.Clear();
        }



        private bool RectOverlap(Rect firstRect, Rect secondRect)
        {
            if (firstRect.x + firstRect.width * 0.5f < secondRect.x - secondRect.width * 0.5f)
            {
                return false;
            }
            if (secondRect.x + secondRect.width * 0.5f < firstRect.x - firstRect.width * 0.5f)
            {
                return false;
            }
            if (firstRect.y + firstRect.height * 0.5f < secondRect.y - secondRect.height * 0.5f)
            {
                return false;
            }
            if (secondRect.y + secondRect.height * 0.5f < firstRect.y - firstRect.height * 0.5f)
            {
                return false;
            }
            return true;
        }

        private bool PointOverlapToRect(Vector2 targetPoint, Rect targetRect)
        {
            if (targetPoint.x >= targetRect.x && targetPoint.x <= targetRect.x + targetRect.width)
            {
                if (targetPoint.y >= targetRect.y && targetPoint.y <= targetRect.y + targetRect.height)
                {
                    return true;
                }
            }

            return false;
        }


        // GUI : to make a texture card through dragging assets
        void DragToPutTextures()
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                if (DragAndDrop.objectReferences.Length > 0)
                {

                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        var activatorRect = GUILayoutUtility.GetLastRect();

                        Object obj = DragAndDrop.objectReferences[i];
                        string path = AssetDatabase.GetAssetPath(obj);

                        if (obj.GetType() == typeof(UnityEngine.Texture) || obj.GetType() == typeof(UnityEngine.Texture2D))
                        {
                            m_LoadedTexturePaths.Add(path);
                            m_LoadedTextures.Add(AssetDatabase.LoadAssetAtPath<Texture>(path));

                            activatorRect.x = activatorRect.x + Event.current.mousePosition.x;
                            activatorRect.y = activatorRect.y + Event.current.mousePosition.y;

                            m_LoadedTextureCardWindow.Add(new Rect(new Vector2(activatorRect.x + texWinSize * i, activatorRect.y), new Vector2(texWinSize, texWinSize)));

                            AutopopulateLatestTexture();
                        }
                        else if (obj.GetType() == typeof(UnityEngine.Material))
                        {
                            // Add Material

                            RegisterMaterialPath(path);
                            m_currentSelectedMaterialIndex = m_LoadedMaterialPaths.Count - 1;
                            m_LoadedMaterials.Add(AssetDatabase.LoadAssetAtPath<Material>(path));

                            m_currentLoadedMaterial = m_LoadedMaterials[m_currentSelectedMaterialIndex];

                            OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);
                        }
                    }

                }
            }
        }

        void RefreshAllPreviews()
        {
            if (texturePreviewEditor != null)
                texturePreviewEditor.ReloadPreviewInstances();
            if (modelPreviewEditor != null)
                modelPreviewEditor.ReloadPreviewInstances();
            if (materialPreviewEditor != null)
                materialPreviewEditor.ReloadPreviewInstances();

            if (m_currentLoadedMaterial != null)
                EditorUtility.SetDirty((Object)m_currentLoadedMaterial);

            foreach(GameObject go in m_LoadedModels)
                EditorUtility.SetDirty((Object)go);

            LookDevSearchHelpers.RefreshWindow();
        }


        void ActionForTextureSlots()
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {

                for (int i = 0; i < m_LoadedTextureSlot.Count; i++)
                {
                    if (latestHandlingWinID == -1)
                        break;


                    if (latestHandlingWinID < 100)
                    {
                        if (RectOverlap(m_LoadedTextureCardWindow[latestHandlingWinID], m_LoadedTextureSlot[i]))
                        {

                            // Need to be snapped
                            float offsetX = (m_LoadedTextureSlot[i].width - m_LoadedTextureCardWindow[latestHandlingWinID].width) / 2f + m_LoadedTextureSlot[i].x;
                            float offsetY = (m_LoadedTextureSlot[i].height - m_LoadedTextureCardWindow[latestHandlingWinID].height) / 2f + m_LoadedTextureSlot[i].y;

                            m_LoadedTextureCardWindow[latestHandlingWinID] = new Rect(offsetX, offsetY, m_LoadedTextureCardWindow[latestHandlingWinID].width, m_LoadedTextureCardWindow[latestHandlingWinID].height);

                            if (LitKeywordGroup.ContainsKey(TextureName[i]))
                            {
                                string targetKeyword = LitKeywordGroup[TextureName[i]];
                                if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == false)
                                    m_currentLoadedMaterial.EnableKeyword(targetKeyword);
                            }

                            Texture linkedTexture = m_currentLoadedMaterial.GetTexture(TextureName[i]);

                            if (linkedTexture != null)
                            {
                                
                                for (int j=0;j< m_LoadedTextureCardWindow.Count;j++)
                                {
                                    if (j == latestHandlingWinID)
                                        continue;
                                    if (RectOverlap(m_LoadedTextureCardWindow[j], m_LoadedTextureSlot[i]))
                                    {
                                        m_LoadedTextureCardWindow[j] = new Rect(m_LoadedTextureCardWindow[j].x, m_LoadedTextureCardWindow[j].y - 200, m_LoadedTextureCardWindow[j].width, m_LoadedTextureCardWindow[j].height);
                                        break;
                                    }
                                }
                                

                                for (int j=0;j<existingTextureCardWindow.Count;j++)
                                {
                                    if (RectOverlap(existingTextureCardWindow[j], m_LoadedTextureSlot[i]))
                                    {
                                        Rect exTexRect = new Rect(existingTextureCardWindow[j].x, existingTextureCardWindow[j].y - 200, existingTextureCardWindow[j].width, existingTextureCardWindow[j].height);

                                        string exTexPath = AssetDatabase.GetAssetPath(existingTextures[j]);

                                        if (m_LoadedTexturePaths.Contains(exTexPath) == false)
                                        {
                                            m_LoadedTexturePaths.Add(exTexPath);
                                            m_LoadedTextures.Add(AssetDatabase.LoadAssetAtPath<Texture>(exTexPath));
                                            m_LoadedTextureCardWindow.Add(exTexRect);
                                        }
                                        break;
                                    }
                                }
                            }

                            m_currentLoadedMaterial.SetTexture(TextureName[i], m_LoadedTextures[latestHandlingWinID]);

                            TextureSlotIDs[i] = latestHandlingWinID;
                            RefreshAllPreviews();

                            OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);
                            break;
                        }
                    }
                    else // if existing textures are dragged
                    {
                        int relativeIndex = latestHandlingWinID - 100;

                        if (RectOverlap(existingTextureCardWindow[relativeIndex], m_LoadedTextureSlot[i]))
                        {

                            // Need to be snapped
                            float offsetX = (m_LoadedTextureSlot[i].width - existingTextureCardWindow[relativeIndex].width) / 2f + m_LoadedTextureSlot[i].x;
                            float offsetY = (m_LoadedTextureSlot[i].height - existingTextureCardWindow[relativeIndex].height) / 2f + m_LoadedTextureSlot[i].y;

                            existingTextureCardWindow[relativeIndex] = new Rect(offsetX, offsetY, existingTextureCardWindow[relativeIndex].width, existingTextureCardWindow[relativeIndex].height);


                            if (LitKeywordGroup.ContainsKey(TextureName[i]))
                            {
                                string targetKeyword = LitKeywordGroup[TextureName[i]];
                                if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == false)
                                    m_currentLoadedMaterial.EnableKeyword(targetKeyword);
                            }

                            Texture linkedTexture = m_currentLoadedMaterial.GetTexture(TextureName[i]);

                            if (linkedTexture != null)
                            {
                                /*
                                for (int j = 0; j < m_LoadedTextureCardWindow.Count; j++)
                                {
                                    if (RectOverlap(m_LoadedTextureCardWindow[j], m_LoadedTextureSlot[i]))
                                    {
                                        m_LoadedTextureCardWindow[j] = new Rect(m_LoadedTextureCardWindow[j].x, m_LoadedTextureCardWindow[j].y - 200, m_LoadedTextureCardWindow[j].width, m_LoadedTextureCardWindow[j].height);
                                        break;
                                    }
                                }
                                */

                                //existingTextureCardWindow[relativeIndex]
                                //existingTextures[relativeIndex]

                                //existingTextureCardWindow[previousSlotID]
                                //existingTextures[previousSlotID]

                                if (previousSlotID != -1)
                                {
                                    Texture swapTexture = m_currentLoadedMaterial.GetTexture(TextureName[previousSlotID]);
                                    m_currentLoadedMaterial.SetTexture(TextureName[previousSlotID], linkedTexture);
                                    previousSlotID = -1;
                                }

                                // Ex Card to Ex Card
                                /*
                                for (int j = 0; j < existingTextureCardWindow.Count; j++)
                                {
                                    if (j == relativeIndex)
                                        continue;

                                    if (RectOverlap(existingTextureCardWindow[j], m_LoadedTextureSlot[i]))
                                    {
                                        existingTextureCardWindow[j] = new Rect(m_LoadedTextureSlot[i].x, m_LoadedTextureSlot[i].y, existingTextureCardWindow[j].width, existingTextureCardWindow[j].height);
                                        existingTextures[j] = linkedTexture;
                                        break;
                                    }
                                }
                                */
                            }
                            else
                            {

                                if (previousSlotID >= 0 && previousSlotID < TextureName.Count)
                                {
                                    // Disable Keyword
                                    if (LitKeywordGroup.ContainsKey(TextureName[previousSlotID]))
                                    {
                                        string targetKeyword = LitKeywordGroup[TextureName[previousSlotID]];
                                        if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == true)
                                            m_currentLoadedMaterial.DisableKeyword(targetKeyword);
                                    }

                                    m_currentLoadedMaterial.SetTexture(TextureName[previousSlotID], null);

                                    previousSlotID = -1;
                                }
                            }


                            m_currentLoadedMaterial.SetTexture(TextureName[i], existingTextures[relativeIndex]);

                            TextureSlotIDs[i] = latestHandlingWinID;

                            RefreshAllPreviews();

                            OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);
                            break;
                        }
                    }
                }

                // Check No textures in the slot, no overlap = no texture
                for (int i = 0; i < m_LoadedTextureSlot.Count; i++)
                {
                    if (latestHandlingWinID == -1)
                        break;

                    bool isNoTextureSlot = true;

                    for (int j = 0; j < m_LoadedTextureCardWindow.Count; j++)
                    {
                        if (RectOverlap(m_LoadedTextureCardWindow[j], m_LoadedTextureSlot[i]))
                        {
                            isNoTextureSlot = false;
                            break;
                        }
                    }

                    for (int j = 0; j < existingTextureCardWindow.Count; j++)
                    {
                        if (RectOverlap(existingTextureCardWindow[j], m_LoadedTextureSlot[i]))
                        {
                            isNoTextureSlot = false;
                            break;
                        }
                    }

                    if (isNoTextureSlot)
                    {
                        if (m_currentLoadedMaterial != null)
                        {

                            // Disable Keyword
                            if (LitKeywordGroup.ContainsKey(TextureName[i]))
                            {
                                string targetKeyword = LitKeywordGroup[TextureName[i]];
                                if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == true)
                                    m_currentLoadedMaterial.DisableKeyword(targetKeyword);
                            }

                            m_currentLoadedMaterial.SetTexture(TextureName[i], null);


                            TextureSlotIDs[i] = -1;

                            RefreshAllPreviews();
                        }
                    }

                }


                if (latestHandlingWinID >= 100)
                {
                    int relativeIndex = latestHandlingWinID - 100;

                    bool isOut = true;

                    for (int i = 0; i < m_LoadedTextureSlot.Count; i++)
                    {
                        if (RectOverlap(existingTextureCardWindow[relativeIndex], m_LoadedTextureSlot[i]))
                        {
                            isOut = false;
                            break;
                        }
                    }

                    if (isOut)
                    {
                        Texture swapTexture = existingTextures[relativeIndex];
                        string swapTexturePath = AssetDatabase.GetAssetPath(swapTexture);

                        if (!m_LoadedTexturePaths.Contains(swapTexturePath))
                        {
                            m_LoadedTexturePaths.Add(swapTexturePath);
                            m_LoadedTextures.Add(swapTexture);
                            m_LoadedTextureCardWindow.Add(new Rect(existingTextureCardWindow[relativeIndex].x, existingTextureCardWindow[relativeIndex].y - 200, existingTextureCardWindow[relativeIndex].width, existingTextureCardWindow[relativeIndex].height));
                        }

                        existingTextures.RemoveAt(relativeIndex);
                        existingTextureCardWindow.RemoveAt(relativeIndex);

                        OnChangeCurrentSelectedMaterial(m_currentSelectedMaterialIndex);
                    }
                }
            }
        }

        void OnChangeCurrentSelectedMaterial(int i)
        {
            latestHandlingWinID = -1;

            existingTextureCardWindow.Clear();
            existingTextures.Clear();

            // relocate loaded materials
            for (int j=0;j<m_LoadedTextureCardWindow.Count;j++)
            {
                m_LoadedTextureCardWindow[j] = new Rect((j * (texWinSize + texWinOffset)), 710, texWinSize, texWinSize);
            }

            m_currentSelectedMaterialIndex = i;

            if (i >= 0 && i < m_LoadedMaterials.Count)
            {
                m_currentLoadedMaterial = m_LoadedMaterials[i];
                RefreshTextureTypeAndNameOnMaterial(m_currentLoadedMaterial);
            }

            RefreshCardSlot();

            MakeTextureCardAlreadyConnected();

            CreateMaterialEditor();

            Selection.activeObject = m_currentLoadedMaterial;
        }

        void DisplayLoadedMaterials()
        {
            if (m_LoadedMaterials.Count > 0)
            {

                GUILayout.BeginArea(new Rect(430, 305, position.width, 95));

                GUILayout.Label($"Loaded Materials ({m_LoadedMaterials.Count}) :", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal("Box");

                for (int i = 0; i < m_LoadedMaterials.Count; i++)
                {
                    if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(m_LoadedMaterials[i]), m_LoadedMaterials[i].name), GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        OnChangeCurrentSelectedMaterial(i);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.EndArea();
            }

            
        }

        Vector2 sp;

        Rect materialPreviewLabelArea = new Rect(430, 1, 500, 19);
        Rect modelPreviewLabelArea = new Rect(20, 1, 500, 19);
        Rect texturePreviewLabelArea = new Rect(840, 1, 500, 19);

        Rect materialPreviewArea = new Rect(430, 20, 280, 280);
        Rect modelPreviewArea = new Rect(20, 20, 280, 280);
        Rect texturePreviewArea = new Rect(840, 20, 280, 280);

        Rect scrollRect = new Rect(1200, 20, 280, 280);
        Rect viewRect = new Rect(0, 0, 400, 800);
        void DisplayCurrentMaterialAndModel()
        {
            EditorGUI.LabelField(materialPreviewLabelArea, "Preview (Material currently selected)", EditorStyles.boldLabel);
            EditorGUI.LabelField(modelPreviewLabelArea, "Preview (Model currently selected)", EditorStyles.boldLabel);
            EditorGUI.LabelField(texturePreviewLabelArea, "Preview (Texture currently selected)", EditorStyles.boldLabel);

            // Preview for the current Material
            if (m_currentLoadedMaterial != null)
            {
                if (materialPreviewEditor == null)
                    materialPreviewEditor = UnityEditor.Editor.CreateEditor(m_currentLoadedMaterial);

                if (modelPreviewEditor == null && m_LoadedModels.Count > 0)
                    modelPreviewEditor = UnityEditor.Editor.CreateEditor(m_LoadedModels[0]);

                materialPreviewEditor.OnInteractivePreviewGUI(materialPreviewArea, GUIStyle.none);

                if (m_LoadedModels.Count > 0)
                {
                    modelPreviewEditor.OnInteractivePreviewGUI(modelPreviewArea, GUIStyle.none);
                }
                else
                {
                    GUI.Box(modelPreviewArea, string.Empty, new GUIStyle("Box"));
                }
            }
            else
            {
                GUI.Box(materialPreviewArea, string.Empty, new GUIStyle("Box"));
            }

            
            if (texturePreviewEditor != null)
            {
                texturePreviewEditor.OnInteractivePreviewGUI(texturePreviewArea, new GUIStyle("Box"));
                
                sp = GUI.BeginScrollView(scrollRect, sp, viewRect, false, false);
                GUI.BeginGroup(viewRect, new GUIStyle("box"));
                GUILayout.Label($"Texture name : {texturePreviewEditor.target.name}");
                GUILayout.Label($"Texture path : {AssetDatabase.GetAssetPath(texturePreviewEditor.target)}");
                GUI.EndGroup();
                GUI.EndScrollView();
                
            }
            else
            {
                GUI.Box(texturePreviewArea, string.Empty, new GUIStyle("Box"));
            }
        }


        public void DisplayCurrentTexture()
        {
            if (latestHandlingWinID != -1)
            {
                if (latestHandlingWinID < 100)
                {
                    DisplayCurrentTexture(m_LoadedTextures[latestHandlingWinID]);
                    Selection.activeObject = m_LoadedTextures[latestHandlingWinID];
                }
                else
                {
                    DisplayCurrentTexture(existingTextures[latestHandlingWinID - 100]);
                    Selection.activeObject = existingTextures[latestHandlingWinID - 100];
                }
            }
        }

        public void DisplayCurrentTexture(Texture targetTexture)
        {
            // Preview for the current Texture
            if (texturePreviewEditor == null)
            {
                texturePreviewEditor = UnityEditor.Editor.CreateEditor(targetTexture);
            }
            else
            {
                texturePreviewEditor.ResetTarget();
                texturePreviewEditor = UnityEditor.Editor.CreateEditor(targetTexture);
                texturePreviewEditor.Repaint();
            }

            Selection.activeObject = targetTexture;
        }


        GUIStyle style = new GUIStyle("Box");
        GUIStyle textStyle = EditorStyles.label;

        void DisplaySlots()
        {
            if (m_LoadedTextureSlot.Count != 0)
            {
                EditorGUI.LabelField(new Rect(m_LoadedTextureSlot[0].x, m_LoadedTextureSlot[0].y - 100, m_LoadedTextureSlot[0].width, m_LoadedTextureSlot[0].height), "Assign Textures : ", EditorStyles.boldLabel);
            }

            for (int i = 0; i < m_LoadedTextureSlot.Count; i++)
            {
                style.normal.textColor = Color.cyan;

                GUILayout.BeginArea(m_LoadedTextureSlot[i], new GUIContent(TextureTypeDescription[i]), style);
                GUILayout.BeginVertical("Box");

                textStyle.wordWrap = true;

                if (LitTextureDesc.ContainsKey(TextureName[i]))
                    GUILayout.Label(string.Format("\n\n\n\n\n\nDrag Your \"{0}\" here", LitTextureDesc[TextureName[i]]), textStyle, GUILayout.Width(140));
                else
                    GUILayout.Label(string.Format("\n\n\n\n\n\nDrag Your \"{0}\" here", "MAP"), textStyle, GUILayout.Width(140));

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        void TextureCardControls()
        {
            BeginWindows();
            for (int i = 0; i < m_LoadedTextures.Count; i++)
            {
                m_LoadedTextureCardWindow[i] = GUILayout.Window(i, m_LoadedTextureCardWindow[i], ShowTextureCard, m_LoadedTextures[i].name);
            }

            for (int i = 0; i < existingTextures.Count; i++)
            {
                existingTextureCardWindow[i] = GUILayout.Window(i + 100, existingTextureCardWindow[i], ShowExistingTextureCard, existingTextures[i].name);
            }
            EndWindows();
        }


        void RemoveLatestSelectedTextureCard()
        {
            RemoveTextureCardByID(latestHandlingWinID);
        }

        void RemoveTextureCardByID(int id)
        {
            if (id == -1)
                return;

            int targetCardID = id;
            latestHandlingWinID = -1;

            if (targetCardID < 100)
            {
                m_LoadedTexturePaths.RemoveAt(targetCardID);
                m_LoadedTextures.RemoveAt(targetCardID);
                m_LoadedTextureCardWindow.RemoveAt(targetCardID);
            }
            else
            {
                if (previousSlotID != -1)
                {
                    // Disable Keyword
                    if (LitKeywordGroup.ContainsKey(TextureName[previousSlotID]))
                    {
                        string targetKeyword = LitKeywordGroup[TextureName[previousSlotID]];
                        if (m_currentLoadedMaterial.IsKeywordEnabled(targetKeyword) == true)
                            m_currentLoadedMaterial.DisableKeyword(targetKeyword);
                    }

                    m_currentLoadedMaterial.SetTexture(TextureName[previousSlotID], null);

                    existingTextures.RemoveAt(targetCardID - 100);
                    existingTextureCardWindow.RemoveAt(targetCardID - 100);
                }
            }

            previousSlotID = -1;

        }
        
        //NOTE: Hiding for the sake of the warning
        //float zoomScale = 1f;

        public void DuplicateTexture()
        {
            if (latestHandlingWinID != -1)
            {
                int targetCardID = latestHandlingWinID;
                latestHandlingWinID = -1;

                if (targetCardID < 100)
                {
                    m_LoadedTexturePaths.Add(m_LoadedTexturePaths[targetCardID]);
                    m_LoadedTextures.Add(m_LoadedTextures[targetCardID]);
                    m_LoadedTextureCardWindow.Add(m_LoadedTextureCardWindow[targetCardID]);

                    Rect currentCardRect = m_LoadedTextureCardWindow[targetCardID];
                    currentCardRect = new Rect(currentCardRect.x + currentCardRect.width, currentCardRect.y, currentCardRect.width, currentCardRect.height);

                    m_LoadedTextureCardWindow[m_LoadedTextureCardWindow.Count - 1] = currentCardRect;
                }
                else
                {
                    existingTextures.Add(existingTextures[targetCardID - 100]);
                    existingTextureCardWindow.Add(existingTextureCardWindow[targetCardID - 100]);

                    Rect currentCardRect = existingTextureCardWindow[targetCardID - 100];
                    currentCardRect = new Rect(currentCardRect.x + currentCardRect.width, currentCardRect.y, currentCardRect.width, currentCardRect.height);

                    existingTextureCardWindow[existingTextureCardWindow.Count - 1] = currentCardRect;
                }

            }
        }

        private void OnGUI()
        {

            if (m_currentLoadedMaterial == null)
                return;


            DragToPutTextures();
            ActionForTextureSlots();


            // Zoom Test
            /*
            Event e = Event.current;

            if (e.type == EventType.ScrollWheel)
            {
                var zoomDelta = 0.1f;
                zoomDelta = e.delta.y < 0 ? zoomDelta : -zoomDelta;
                zoomScale += zoomDelta;
                zoomScale = Mathf.Clamp(zoomScale, 0.25f, 1.25f);
                e.Use();
            }

            Vector2 vanishingPoint = new Vector2(0, 21);
            Matrix4x4 Translation = Matrix4x4.TRS(vanishingPoint, Quaternion.identity, Vector3.one);
            Matrix4x4 Scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
            GUI.matrix = Translation * Scale * Translation.inverse;
            */


            GUILayout.BeginVertical();


            DisplayLoadedMaterials();
            DisplayCurrentMaterialAndModel();

            DisplaySlots();

            TextureCardControls();

            DisplayTips();

            GUILayout.EndVertical();

        }


        Rect TipRect = new Rect(1200,640,300,60);
        void DisplayTips()
        {
            EditorGUI.HelpBox(TipRect, "[Tips]\n1. Drag and drop Materials or Textures here.\n2. Drag Textures into the slots to assign them to the selected Material.", MessageType.Info);
        }

        TextureLinkBrowserPopup txLinkPopup = new TextureLinkBrowserPopup();

        void ShowTextureCard(int unusedWindowID)
        {
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                latestHandlingWinID = unusedWindowID;
            }

            GUILayout.BeginVertical("Box");

            float yOffset = 17;

            if (GUI.Button(new Rect(0, 0, m_LoadedTextureCardWindow[unusedWindowID].width, yOffset), "Texture"))
            {
                DisplayCurrentTexture(m_LoadedTextures[unusedWindowID]);
            }

            EditorGUI.DrawPreviewTexture(new Rect(0, yOffset, m_LoadedTextureCardWindow[unusedWindowID].width, m_LoadedTextureCardWindow[unusedWindowID].height - yOffset), m_LoadedTextures[unusedWindowID]);

            GUILayout.Label(m_LoadedTextures[unusedWindowID].name, GUILayout.Width(texWinSize), GUILayout.Height(texWinSize));

            if (GUI.Button(new Rect(m_LoadedTextureCardWindow[unusedWindowID].width - 22, yOffset + 2, 20, 20), new GUIContent("X", "Remove this texture")))
            {
                if (EditorUtility.DisplayDialog("Remove selected Texture?", $"[{m_LoadedTextures[unusedWindowID].name}]\nRemoving the Texture from the Texture Allocator will not delete the Texture from your Project.", "Yes", "No"))
                    RemoveTextureCardByID(unusedWindowID);
            }


            GUILayout.EndVertical();

            if (Event.current.button == 1 && Event.current.type == EventType.MouseUp)
            {
                latestHandlingWinID = unusedWindowID;
                PopupWindow.Show(new Rect(0, 0, m_LoadedTextureCardWindow[unusedWindowID].width, m_LoadedTextureCardWindow[unusedWindowID].height), txLinkPopup);
                Event.current.Use();
            }

            GUI.DragWindow();
        }

        static int previousSlotID;

        void ShowExistingTextureCard(int unusedWindowID)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                bool isOverlap = false;

                // check about whether this card is in the slot
                for (int i=0;i<m_LoadedTextureSlot.Count;i++)
                {
                    if (RectOverlap(existingTextureCardWindow[unusedWindowID - 100], m_LoadedTextureSlot[i]))
                    {
                        previousSlotID = i;
                        isOverlap = true;
                    }
                }

                if (isOverlap == false)
                    previousSlotID = -1;

            }

            if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                latestHandlingWinID = unusedWindowID;
            }

            GUILayout.BeginVertical("Box");

            float yOffset = 17;

            if (GUI.Button(new Rect(0, 0, existingTextureCardWindow[unusedWindowID - 100].width, yOffset), "Texture"))
            {
                DisplayCurrentTexture(existingTextures[unusedWindowID - 100]);
            }
            
            EditorGUI.DrawPreviewTexture(new Rect(0, yOffset, existingTextureCardWindow[unusedWindowID - 100].width, existingTextureCardWindow[unusedWindowID - 100].height - yOffset), existingTextures[unusedWindowID - 100]);
            GUILayout.Label(existingTextures[unusedWindowID-100].name, GUILayout.Width(texWinSize), GUILayout.Height(texWinSize));

            if (GUI.Button(new Rect(existingTextureCardWindow[unusedWindowID - 100].width - 22, yOffset + 2, 20, 20), new GUIContent("X","Remove this texture")))
            {
                if (EditorUtility.DisplayDialog("Remove selected Texture?", $"[{existingTextures[unusedWindowID - 100].name}]\nRemoving the Texture from the Texture Allocator will not delete the Texture from your Project.", "Yes", "No"))
                    RemoveTextureCardByID(unusedWindowID);
            }

            GUILayout.EndVertical();

            if (Event.current.button == 1 && Event.current.type == EventType.MouseUp)
            {
                latestHandlingWinID = unusedWindowID;
                PopupWindow.Show(new Rect(0, 0, existingTextureCardWindow[unusedWindowID-100].width, existingTextureCardWindow[unusedWindowID-100].height), txLinkPopup);
                Event.current.Use();
            }

            GUI.DragWindow();
        }

    }

}