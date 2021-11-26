using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{
    public struct NameSet
    {
        public string propertyName;
        public List<string> postfixes;
    }

    public class LookDevNameRules
    {
        public static LookDevNameRules nameRuleManager;

        //const string nameRuleCsv = "Packages/com.unity.lookdevstudio/Settings/TextureNameRule/LookDevNameConvention.csv";
        public string nameRuleAsset = "Assets/LookDev/Settings/TextureRule/TextureAutoPopulate.asset";

        public List<NameSet> TextureNameSet = new List<NameSet>();


        public LookDevNameRules()
        {
            RefreshTextureNameRules();
        }


        public void RefreshTextureNameRules()
        {
            // Load Texture Name convention

            TexturePopulationRules currentPopulationRule = AssetDatabase.LoadAssetAtPath<TexturePopulationRules>(nameRuleAsset);

            if (currentPopulationRule != null)
            {
                TextureNameSet.Clear();

                foreach (TextureRule currentRule in currentPopulationRule.textureRules)
                {
                    if (currentRule.TextureProperty == string.Empty || currentRule.TexturePostfixes == string.Empty)
                        continue;

                    string[] tokens = currentRule.TexturePostfixes.Split(',');

                    NameSet nameSet = new NameSet();
                    nameSet.propertyName = currentRule.TextureProperty.Trim();
                    nameSet.postfixes = new List<string>();

                    for (int i = 0; i < tokens.Length; i++)
                    {
                        string currentPostFix = tokens[i].ToLower().Trim();

                        if (!nameSet.postfixes.Contains(currentPostFix))
                        {
                            nameSet.postfixes.Add(currentPostFix);
                        }
                    }

                    TextureNameSet.Add(nameSet);
                }

                /*
                foreach(NameSet nameSet in TextureNameSet)
                {
                    Debug.LogError(nameSet);
                    foreach (string t in nameSet.postfixes)
                        Debug.LogWarning(t);
                }
                */
            }
            else
            {
                Debug.LogError($"Could not find NameRuleAsset : {nameRuleAsset}");
                return;
            }
        }






        public static LookDevNameRules Inst
        {
            get
            {
                if (nameRuleManager == null)
                    nameRuleManager = new LookDevNameRules();

                return nameRuleManager;
            }
        }

    }
}