using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;

using System.IO;

namespace LookDev.Editor
{
    public class FbxTool
    {
        public static void ExportGameObjects(GameObject[] objects)
        {

            List<GameObject> objectsToBeExported = new List<GameObject>();

            foreach (GameObject obj in objects)
            {
                Renderer currentRenderer = obj.GetComponent<Renderer>();

                if (currentRenderer != null)
                {
                    if (currentRenderer.GetType() == typeof(SkinnedMeshRenderer))
                    {
                        SkinnedMeshRenderer skinnedMeshRenderer = currentRenderer as SkinnedMeshRenderer;

                        if (skinnedMeshRenderer.sharedMesh != null)
                            objectsToBeExported.Add(obj);
                        if (skinnedMeshRenderer.rootBone != null)
                        {
                            Transform target;

                            if (skinnedMeshRenderer.rootBone.localPosition != Vector3.zero)
                                target = skinnedMeshRenderer.rootBone.parent;
                            else
                                target = skinnedMeshRenderer.rootBone;

                            if (objectsToBeExported.Contains(target.gameObject) == false)
                                objectsToBeExported.Add(target.gameObject);
                        }
                    }
                    else if (currentRenderer.GetType() == typeof(MeshRenderer))
                    {
                        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

                        if (meshFilter != null)
                        {
                            if (meshFilter.sharedMesh != null)
                                objectsToBeExported.Add(obj);
                        }
                    }
                }
            }


            if (objectsToBeExported.Count == 0)
                return;

            string targetFBXPath = Path.Combine(LookDevHelpers.LookDevSubdirectoryForModel, objects[0].name + ".fbx");
            targetFBXPath = targetFBXPath.Replace("\\", "/");


            targetFBXPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/{targetFBXPath}");


            targetFBXPath = targetFBXPath.Replace("Assets/", string.Empty);

            string targetFBXFullPath = Path.Combine(Application.dataPath, targetFBXPath);
            targetFBXFullPath = targetFBXFullPath.Replace("\\", "/");

            ModelExporter.ExportObjects(targetFBXFullPath, objectsToBeExported.ToArray());

        }

    }
}