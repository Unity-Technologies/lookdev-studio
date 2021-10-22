using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.IO;

namespace LookDev.Editor
{
    public static class SearchProviderForTextures
    {
        internal static string id = "LookDev_Texture";
        internal static string name = "Texture";

        public static List<string> folders = new List<string>();
        public static string defaultFolder = "Assets/LookDev/Textures";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "tex:",
                priority = 11, // put example provider at a low priority
                fetchItems = (context, items, provider) =>
                {
                    string projectPath = ProjectSettingWindow.projectSetting.GetImportAssetPath();
                    if (string.IsNullOrEmpty(projectPath) == false)
                        defaultFolder = projectPath;

                    string[] results;
                    
                    if (folders.Count == 0)
                        results = AssetDatabase.FindAssets("t:Texture " + context.searchQuery, new string[]{ defaultFolder });
                    else
                        results = AssetDatabase.FindAssets("t:Texture " + context.searchQuery, folders.ToArray());


                    foreach (var guid in results)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);

                        if (LookDevHelpers.IsSupportedExtension(Path.GetExtension(assetPath)))
                            items.Add(provider.CreateItem(context, assetPath, null, null, null, null));
                    }
                    return null;

                },
#pragma warning disable UNT0008 // Null propagation on Unity objects
                // Use fetch to load the asset asynchronously on display
                fetchThumbnail = (item, context) => AssetDatabase.GetCachedIcon(item.id) as Texture2D,
                fetchPreview = (item, context, size, options) => AssetPreview.GetAssetPreview(item.ToObject()) as Texture2D,
                fetchLabel = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id)?.name,
                fetchDescription = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id)?.name,
                toObject = (item, type) => AssetDatabase.LoadMainAssetAtPath(item.id),
#pragma warning restore UNT0008 // Null propagation on Unity objects
                // Shows handled actions in the preview inspector
                // Shows inspector view in the preview inspector (uses toObject)
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions | ShowDetailsOptions.Preview,
                trackSelection = (item, context) =>
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(item.id);
                    if (obj != null)
                    {
                        if (context.selection.Count == 1)
                        {
                            EditorGUIUtility.PingObject(obj.GetInstanceID());
                            Selection.activeInstanceID = obj.GetInstanceID();
                        }
                        else if (context.selection.Count > 1)
                        {
                            List<Object> objList = new List<Object>();
                            foreach (SearchItem sItem in context.selection)
                            {
                                var targetObj = AssetDatabase.LoadMainAssetAtPath(sItem.id);

                                if (targetObj != null)
                                    objList.Add(targetObj);
                            }
                            Selection.objects = objList.ToArray();
                        }
                    }
                },
                startDrag = (item, context) =>
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(item.id);
                    if (obj != null)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { obj };

                        if (Selection.objects.Length > 1)
                        {
                            DragAndDrop.objectReferences = Selection.objects;
                        }

                        DragAndDrop.StartDrag(item.label);
                    }
                }
            };
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            List<SearchAction> action = new List<SearchAction>()
            {
                new SearchAction(id, "Edit in Dcc", null, "Description")
                {
                    handler = (item) =>
                    {
                        switch(ProjectSettingWindow.projectSetting.paintingTexDccs)
                        {
                            case PaintingTexDCCs.Photoshop:
                                AssetManageHelpers.LoadTextureOnDCC(item.id);
                                break;
                        }
                    }
                },
                new SearchAction(id, "Show in Explorer", null, "Description")
                {
                    handler = (item) =>
                    {
                        AssetManageHelpers.ShowinExplorer(item.id);
                    }
                },
                new SearchAction(id, "Go to Material", null, "Description")
                {
                    handler = (item) =>
                    {
                        AssetManageHelpers.GoToAsset(item.id);
                    }
                },
                new SearchAction(id, "Duplicate Texture", null, "Description")
                {
                    handler = (item) =>
                    {
                        AssetManageHelpers.DuplicateSelectedAssets();
                    }
                },
                new SearchAction(id, "Rename Texture", null, "Description")
                {
                    handler = (item) =>
                    {
                        AssetManageHelpers.RenameSelectedAssets();
                    }
                },
                new SearchAction(id, "Delete Texture", null, "Description")
                {
                    handler = (item) =>
                    {
                        AssetManageHelpers.DeleteSelectedAssets();
                    }
                }
            };

            if (LookDevStudioEditor.IsHDRP())
            {
                action.Add(
                    new SearchAction(id, "Switch to Hdri Map", null, "Description")
                    {
                        handler = (item) =>
                        {
                            AssetManageHelpers.ApplyHdriPresetOnSelectedTextures("Assets/LookDev/Setup/Settings/TexturePreset/HDRi.preset");
                        }
                    });
            }
            else
            {
                action.Add(
                    new SearchAction(id, "Switch to Skybox", null, "Description")
                    {
                        handler = (item) =>
                        {
                            AssetManageHelpers.ApplySkyboxPresetOnSelectedTextures("Assets/LookDev/Setup/Settings/TexturePreset/Skybox.preset");
                        }
                    });

                /*
                action.Add(
                    new SearchAction(id, "Switch to Default", null, "Description")
                    {
                        handler = (item) =>
                        {
                            foreach (Object currentGo in Selection.objects)
                            {
                                string assetPath = AssetDatabase.GetAssetPath(currentGo);
                                if (string.IsNullOrEmpty(assetPath) == false)
                                {
                                    TextureImporter currentImporter = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                                    AssetManageHelpers.ApplyPresetToObject("Assets/LookDev/Setup/Settings/TexturePreset/Default.preset", currentImporter);
                                }
                            }
                        }
                    });
                    */
            }

            return action;
        }

    }

}