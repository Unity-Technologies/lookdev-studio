using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.IO;

namespace LookDev.Editor
{
    static class SearchProviderForMaterials
    {
        internal static string id = "LookDev_Material";
        internal static string name = "Material";


        public static List<string> folders = new List<string>();
        public static string defaultFolder = "Assets/LookDev/Materials";


        public static string[] GetAllMaterialPaths()
        {
            string[] guids;
            
            if (folders.Count == 0)
                guids = AssetDatabase.FindAssets("t:Material", new string[] { defaultFolder });
            else
                guids = AssetDatabase.FindAssets("t:Material", folders.ToArray());

            List<string> allMaterials = new List<string>();

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetExtension(assetPath).ToLower() == ".mat")
                    allMaterials.Add(assetPath);
            }

            return allMaterials.ToArray();
        }


        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "mat:",
                priority = 10, // put example provider at a low priority
                fetchItems = (context, items, provider) =>
                {
                    string projectPath = ProjectSettingWindow.projectSetting.GetImportAssetPath();
                    if (string.IsNullOrEmpty(projectPath) == false)
                        defaultFolder = projectPath;

                    string[] results;

                    if (folders.Count == 0)
                        results = AssetDatabase.FindAssets("t:Material " + context.searchQuery, new string[] { defaultFolder });
                    else
                        results = AssetDatabase.FindAssets("t:Material " + context.searchQuery, folders.ToArray());

                    foreach (string guid in results)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (Path.GetExtension(assetPath).ToLower() == ".mat")
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
                        DragAndDrop.StartDrag(item.label);
                    }
                }
            };
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
            new SearchAction(id, "New Material", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.CreateDefaultMaterial();
                }
            },
            new SearchAction(id, "Create Material by Presets", null, "Description")
            {
                handler = (item) =>
                {
                    MaterialBrowserPopup contentPopup = new MaterialBrowserPopup();
                    var activatorRect = EditorWindow.GetWindow<QuickControl>().GetCenterPositionOfTheBrowser(contentPopup.GetWindowSize());
                    PopupWindow.Show(activatorRect, contentPopup);
                }
            },
            new SearchAction(id, "Show in Explorer", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.ShowinExplorer(item.id);
                }
            },
            new SearchAction(id, "Go to Textures", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.GoToAsset(item.id);
                }
            },
            new SearchAction(id, "Duplicate Material", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DuplicateSelectedAssets();
                }
            },
            new SearchAction(id, "Rename Material", null, "Insert a forest in scene")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.RenameSelectedAssets();
                }
            },
            new SearchAction(id, "Delete Material", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DeleteSelectedAssets();
                }
            }

        };
        }

    }

}