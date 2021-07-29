using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace LookDev.Editor
{
    public class DragDropModelPostProcessor : AssetPostprocessor
    {
        public static List<string> latestImportedAssets = new List<string>();

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
                if (!imported.ToLower().Contains(("LookDev/").ToLower()))
                    continue;

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
                LookDevHelpers.SetHeroAsset(prop);

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
            var meshFilters = AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(x => x.GetType() == typeof(Mesh)).ToArray();
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
    }
}