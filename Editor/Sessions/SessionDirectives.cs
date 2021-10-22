using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace LookDev.Editor
{
    [InitializeOnLoad]
    public static class CurrentRenderPipelineDirective
    {
        public static event Action<LookDevSession> OnDirectivesCreate;
        public static event Action<LookDevSession> OnDirectivesLoad;
        
        const string DIRECTIVE_KEY = "CurrentRenderPipelineIndex";
        static CurrentRenderPipelineDirective()
        {
            RenderSettingsBundler.OnDirectivesCreate += OnDirectivesCreateCallback;
            RenderSettingsBundler.OnDirectivesLoad += OnDirectivesLoadCallback;
        }

        static void OnDirectivesCreateCallback(LookDevSession session)
        {
            string curRpPath = AssetDatabase.GetAssetPath(GraphicsSettings.renderPipelineAsset);

            int assetIndex = Array.IndexOf(session.Assets, curRpPath);

            session.Directives.Add(new SerializableDictionary<string, string>.Pair(
                DIRECTIVE_KEY,
                assetIndex.ToString()
                ));
            
            OnDirectivesCreate?.Invoke(session);
        }

        static void OnDirectivesLoadCallback(LookDevSession session)
        {
            int assetPathIndex = Int32.Parse(session.Directives[DIRECTIVE_KEY]);

            string pkgRpPath = session.Assets[assetPathIndex];

            var rpa = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pkgRpPath);

            GraphicsSettings.renderPipelineAsset = rpa;
            
            OnDirectivesLoad?.Invoke(session);
        }
    }
}
