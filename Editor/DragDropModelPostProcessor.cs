using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LookDev.Editor
{
    public class DragDropModelPostProcessor : AssetPostprocessor
    {
        public static List<string> latestImportedAssets = new List<string>();


        ColliderNameRules GetColliderNameRule()
        {
            ColliderNameRules colliderNameRules = AssetDatabase.LoadAssetAtPath<ColliderNameRules>(ColliderNameRules.DefaultColliderAssetPath);

            if (colliderNameRules == null)
            {
                try
                {
                    colliderNameRules = new ColliderNameRules();
                    AssetDatabase.CreateAsset(colliderNameRules, ColliderNameRules.DefaultColliderAssetPath);
                }
                catch
                {
                    Debug.LogError($"Failed to generate a Collider NameRule Asset : {ColliderNameRules.DefaultColliderAssetPath}");
                    return null;
                }
            }

            return colliderNameRules;
        }

        static void RegisterImportedAsset(string importedAsset)
        {
            if (!latestImportedAssets.Contains(importedAsset))
            {
                latestImportedAssets.Add(importedAsset);
            }
        }

        public static bool AreReadyImportedAssets()
        {
            foreach (string path in latestImportedAssets)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) == null)
                    return false;
            }

            return true;
        }

        public static List<string> GetLatestImportedAssets()
        {
            return latestImportedAssets;
        }

        void OnPreprocessTexture()
        {
            if (!LookDevStudioEditor.lookDevEnabled)
                return;

            var sanitizedAssetPath = assetPath.ToLower().Replace("_", "");
            var extension = Path.GetExtension(assetPath);

            /*
            var baseColorMapSuffixes = new[]
            {
                "base", "basecolor", "basecolormap", "diffuse", "diffusecolor", "diffusecolormap", "albedo",
                "albedomap", "_a", "color", "_dc", "_ac", "albedotransparency", "diffuseglossiness"
            };
            var maskMapSuffixes = new[]
            {
                "mask", "maskmap", "_m", "_mm"
            };
            */

            //Automatic Normal Map Importer
            {
                var normalMapSuffixes = new[] {"normal", "normalmap", "norm", "bump", "bumpmap"};
                var normalMapLetterSuffixes = new[] {"_n", "_nm"};

                bool isNormalMap = false;
                foreach (var suffix in normalMapSuffixes)
                {
                    if (sanitizedAssetPath.EndsWith($"{suffix.ToLower()}{extension}"))
                    {
                        isNormalMap = true;
                        break;
                    }
                }

                if (!isNormalMap)
                {
                    foreach (var suffix in normalMapLetterSuffixes)
                    {
                        if (assetPath.ToLower().EndsWith($"{suffix.ToLower()}{extension}"))
                        {
                            isNormalMap = true;
                            break;
                        }
                    }
                }

                if (isNormalMap)
                {
                    var textureImporter = (TextureImporter) assetImporter;
                    textureImporter.textureType = TextureImporterType.NormalMap;
                }
            }


            /*
            var bentNormalMapSuffixes = new[]
            {
                "bentnormalmap", "bentnormal", "_bnm"
            };
            var subsurfaceMapSuffixes = new[]
            {
                "subsurfacemaskmap", "_sss", "subsurface", "subsurfacemask"
            };
            var thicknessMapSuffixes = new[]
            {
                "thicknessmap", "thickness"
            };
            var coatMaskMapSuffixes = new[]
            {
                "coat", "coatmask", "coatmap", "coatmaskmap"
            };
            var detailMapSuffixes = new[]
            {
                "detail", "detailmap"
            };
            var emissiveMapSuffixes = new[]
            {
                "emissive", "emissivecolor", "emissivemap", "emissivecolormap", "_e", "illumination",
                "selfillumination", "emission", "emissionMap", "emissioncolormap"
            };
            var anisotropyMapSuffixes = new[]
            {
                "anisotropy", "anisotropymap"
            };
            var tangentMapSuffixes = new[]
            {
                "tangent", "tangentmap"
            };
            */
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!LookDevStudioEditor.lookDevEnabled)
                return;

#if LOOKDEV_LOGGING
            Debug.Log("DragDropModelPostProcessor::OnPostprocessAllAssets");
#endif

            foreach (var imported in importedAssets)
            {
#if LOOKDEV_LOGGING
                if(printDebug) Debug.Log("Imported: " + imported);
#endif

                // Anything could have been imported. Check for supported assets.
                if (!LookDevHelpers.IsSupportedExtension(Path.GetExtension(imported)))
                    continue;


                // Check about whether the model imported on LookDev Mode
                /*
                if (imported.ToLower().Contains("lookdev/setup") ||
                    !imported.ToLower().Contains(("LookDev/").ToLower()))
                    continue;
                */

                // Ganerate Material
                if (LookDevHelpers.IsModel(Path.GetExtension(imported)))
                {
                    bool hasMaterial = ExtractMaterials(imported);

                    // To skip the model with no materials
                    if (hasMaterial == false)
                        continue;
                }

                RegisterImportedAsset(imported);

                var prop = AssetDatabase.LoadAssetAtPath<Object>(imported);
                GameObject go = LookDevHelpers.SetHeroAsset(prop);


                // Generating Prefab automatically, if "ProjectSettingWindow.projectSetting.MakePrefabsForAllMeshes" is On
                if (prop != null && LookDevHelpers.IsModel(Path.GetExtension(imported)) &&
                    ProjectSettingWindow.projectSetting != null)
                {
                    if (ProjectSettingWindow.projectSetting.MakePrefabsForAllMeshes)
                    {
                        string savePath = $"{imported.Replace(Path.GetExtension(imported), string.Empty)}.prefab";

                        if (!string.IsNullOrEmpty(ProjectSettingWindow.projectSetting.PrefabPrefix))
                        {
                            if (Path.GetFileName(savePath).ToUpper()
                                    .StartsWith(ProjectSettingWindow.projectSetting.PrefabPrefix.ToUpper()) == false &&
                                ProjectSettingWindow.projectSetting.PrefabPrefix.Trim() != string.Empty)
                            {
                                savePath = savePath.Replace(Path.GetFileName(savePath),
                                    $"{ProjectSettingWindow.projectSetting.PrefabPrefix}{Path.GetFileNameWithoutExtension(savePath)}.prefab");
                            }
                        }

                        if (!string.IsNullOrEmpty(ProjectSettingWindow.projectSetting.PrefabPostfix))
                        {
                            if (Path.GetFileName(savePath).ToUpper()
                                    .EndsWith(ProjectSettingWindow.projectSetting.PrefabPostfix.ToUpper()) == false &&
                                ProjectSettingWindow.projectSetting.PrefabPostfix.Trim() != string.Empty)
                            {
                                savePath = savePath.Replace(Path.GetFileName(savePath),
                                    $"{Path.GetFileNameWithoutExtension(savePath)}{ProjectSettingWindow.projectSetting.PrefabPostfix}.prefab");
                            }
                        }


                        if (savePath != string.Empty)
                        {
                            if (AssetDatabase.LoadAssetAtPath<Object>(savePath) != null)
                            {
                                Debug.LogWarning(
                                    $"Same name of the prefab exists. so Prefab auto-generation was skipped.");
                                continue;
                            }

                            savePath = AssetDatabase.GenerateUniqueAssetPath(savePath);
                            go.name = Path.GetFileNameWithoutExtension(savePath);
                            PrefabUtility.SaveAsPrefabAssetAndConnect(go, savePath, InteractionMode.AutomatedAction);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }

            LookDevSearchHelpers.RefreshWindow();

#if LOOKDEV_LOGGING
            foreach (var deleted in deletedAssets)
                Debug.Log("Deleted: " + deleted);

            foreach (var moved in movedAssets)
                Debug.Log("Moved: " + moved);

            foreach (var movedFromAsset in movedFromAssetPaths)
                Debug.Log("Moved from Asset: " + movedFromAsset);
#endif
        }


        static bool ExtractMaterials(string assetPath)
        {
            var assetsToReload = new HashSet<string>();
            var materials = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x.GetType() == typeof(Material))
                .ToArray();

            // Check MeshFilter's Existence
            var meshFilters = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x.GetType() == typeof(Mesh))
                .ToArray();
            if (meshFilters.Length == 0)
                return false;

            foreach (var material in materials)
            {
                var newAssetPath = string.Format("Assets/{0}/{1}.mat", LookDevHelpers.LookDevSubdirectoryForMaterial,
                    material.name);

                if (AssetDatabase.LoadAssetAtPath<Material>(newAssetPath) != null)
                {
                    if (EditorUtility.DisplayDialog("Reuse Material window",
                        $"[{material.name}] already exists. Do you want to reuse the existing Material? If not, the importer will generate a new Material.",
                        "Yes", "No") == false)
                    {
                        newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);
                    }
                }

                // What if the material already exists
                if (AssetDatabase.LoadAssetAtPath<Material>(newAssetPath) != null)
                {
                    // Remap through existing material
                    Material exMaterial = AssetDatabase.LoadAssetAtPath<Material>(newAssetPath);

                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(material), exMaterial);

                    if (!assetsToReload.Contains(assetPath))
                        assetsToReload.Add(assetPath);

                    AssetManageHelpers.AssignDefaultShaderOnTargetMaterial(newAssetPath);
                }
                else
                {
                    // Remap through newly generated material
                    var clone = Object.Instantiate(material);

                    AssetDatabase.CreateAsset(clone, newAssetPath);

                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(material), clone);

                    if (!assetsToReload.Contains(assetPath))
                        assetsToReload.Add(assetPath);

                    AssetManageHelpers.AssignDefaultShaderOnTargetMaterial(newAssetPath);
                }
            }


            foreach (var path in assetsToReload)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            return true;
        }

        #region Collider Generation

        void OnPostprocessModel(GameObject g)
        {
            if (ProjectSettingWindow.projectSetting == null || !LookDevStudioEditor.lookDevEnabled)
                return;

            if (!ProjectSettingWindow.projectSetting.AutoGenerateColliders)
                return;


            List<Transform> transformsToDestroy = new List<Transform>();

            //Skip the root
            foreach (Transform child in g.transform)
            {
                GenerateCollider(child, transformsToDestroy);
            }

            for (int i = transformsToDestroy.Count - 1; i >= 0; --i)
            {
                if (transformsToDestroy[i] != null)
                    GameObject.DestroyImmediate(transformsToDestroy[i].gameObject);
            }
        }

        bool DetectNamingConvention(Transform t, string convention)
        {
            bool result = false;

            if (t.gameObject.TryGetComponent(out MeshFilter meshFilter))
            {
                var lowercaseMeshName = meshFilter.sharedMesh.name.ToLower();
                result = lowercaseMeshName.StartsWith($"{convention}_");
            }

            if (!result)
            {
                var lowercaseName = t.name.ToLower();
                result = lowercaseName.StartsWith($"{convention}_");
            }

            return result;
        }

        void GenerateCollider(Transform t, List<Transform> transformsToDestroy)
        {
            foreach (Transform child in t.transform)
            {
                GenerateCollider(child, transformsToDestroy);
            }

            ColliderNameRules colliderRule = GetColliderNameRule();

            if (DetectNamingConvention(t, colliderRule.boxCollider))
            {
                AddCollider<BoxCollider>(t);
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t, colliderRule.capsuleCollider))
            {
                AddCollider<CapsuleCollider>(t);
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t, colliderRule.sphereCollider))
            {
                AddCollider<SphereCollider>(t);
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t, colliderRule.meshConvexCollider))
            {
                TranslateSharedMesh(t.GetComponent<MeshFilter>());
                var collider = AddCollider<MeshCollider>(t);
                collider.convex = true;
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t, colliderRule.meshCollider))
            {
                TranslateSharedMesh(t.GetComponent<MeshFilter>());
                AddCollider<MeshCollider>(t);
                transformsToDestroy.Add(t);
            }
        }

        void TranslateSharedMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null)
                return;

            var transform = meshFilter.transform;
            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = transform.TransformPoint(vertices[i]);
                vertices[i] = transform.parent.InverseTransformPoint(vertices[i]);
            }

            mesh.SetVertices(vertices);
        }

        T AddCollider<T>(Transform t) where T : Collider
        {
            T collider = t.gameObject.AddComponent<T>();
            T parentCollider = t.parent.gameObject.AddComponent<T>();
            parentCollider.name = t.name;

            EditorUtility.CopySerialized(collider, parentCollider);
            SerializedObject parentColliderSo = new SerializedObject(parentCollider);
            var parentCenterProperty = parentColliderSo.FindProperty("m_Center");
            if (parentCenterProperty != null)
            {
                SerializedObject colliderSo = new SerializedObject(collider);
                var colliderCenter = colliderSo.FindProperty("m_Center");
                var worldSpaceColliderCenter = t.TransformPoint(colliderCenter.vector3Value);

                parentCenterProperty.vector3Value = t.parent.InverseTransformPoint(worldSpaceColliderCenter);
                parentColliderSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return parentCollider;
        }

        #endregion
    }
}