using UnityEngine;
using System;
using Object = UnityEngine.Object;
using UnityEditor;

namespace LookDev.Editor
{
    public enum MeshDCCs
    {
        None,
        Maya,
        Max,
        //Blender
    }

    public enum PaintingMeshDCCs
    {
        None,
        Substance_Painter
    }

    public enum PaintingTexDCCs
    {
        None,
        Photoshop
    }


    public class ProjectSetting : ScriptableObject
    {
        [Header("DCC Tools")]
        public MeshDCCs meshDccs;
        public string meshDccPath;

        public PaintingMeshDCCs paintingMeshDccs;
        public string paintingMeshDccPath;

        public PaintingTexDCCs paintingTexDccs;
        public string paintingTexDccPath;


        [Header("Default Assets")]
        public Shader defaultShader;


        [Header("Asset Post-processing"), Tooltip("Description")]
        public bool MakePrefabsForAllMeshes;
        public string PrefabPrefix;
        public string PrefabPostfix;


        [Header("Project Folders")]
        public Object importAssetPath;
        public Object exportAssetPath;


        /*
        [Header("Asset Folders")]
        public Object materialDefaultPath;
        public Object textureDefaultPath;
        public Object modelDefaultPath;
        public Object shaderDefaultPath;
        public Object lightDefaultPath;
        public Object skyboxDefaultPath;
        public Object animationDefaultPath;
        */

        public string GetImportAssetPath()
        {
            return GetObjectPath(importAssetPath);
        }

        public string GetExportAssetPath()
        {
            return GetObjectPath(exportAssetPath);
        }

        public string GetObjectPath(Object obj)
        {
            if (obj == null)
                return string.Empty;

            return AssetDatabase.GetAssetPath(obj);
        }

    }
}