
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace LookDev.Editor
{

    [Serializable]
    public class TextureRule
    {
        public string TextureProperty;
        public string TexturePostfixes;
    }

    public class TexturePopulationRules : ScriptableObject
    {
        [Header("Name rules for populating Textures")]
        public List<TextureRule> textureRules;

        /*
        [MenuItem("DEBUG/Make")]
        static void Make()
        {
            TexturePopulationRules newOne = new TexturePopulationRules();
            AssetDatabase.CreateAsset(newOne, "Assets/LookDev/Settings/TextureRule/TextureAutoPopulate.asset");
        }
        */
    }

}
