using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

        const string nameRuleCsv = "Packages/com.unity.lookdevstudio/Settings/TextureNameRule/LookDevNameConvention.csv";


        public List<NameSet> TextureNameSet = new List<NameSet>();


        public LookDevNameRules()
        {
            RefreshTextureNameRules();
        }


        public void RefreshTextureNameRules()
        {
            // Load Texture Name convention

            string csvFullPath = Path.GetFullPath(nameRuleCsv);

            if (File.Exists(csvFullPath))
            {
                TextureNameSet.Clear();

                string[] AllLines = File.ReadAllLines(nameRuleCsv);

                foreach (string currentLine in AllLines)
                {
                    string[] tokens = currentLine.Split(',');

                    if (tokens.Length == 1)
                        continue;

                    NameSet nameSet = new NameSet();
                    nameSet.propertyName = tokens[0].Trim();
                    nameSet.postfixes = new List<string>();

                    for (int i = 1; i < tokens.Length; i++)
                    {
                        string currentPostFix = tokens[i].ToLower().Trim();

                        if (!nameSet.postfixes.Contains(currentPostFix))
                        {
                            nameSet.postfixes.Add(currentPostFix);
                        }
                    }

                    TextureNameSet.Add(nameSet);
                }

            }
            else
            {
                Debug.LogError($"Could not find CSV : {nameRuleCsv}");
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