using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.IO;

namespace LookDev.Editor
{
    static class SearchProviderForLight
    {
        internal static string id = "LookDev_Light";
        internal static string name = "Light";

        public static List<string> folders = new List<string>();
        public static string[] defaultFolders = new string[] { "Assets/LookDev/Scenes", "Assets/LookDev/Lights" };


        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "lgt:",
                priority = 14,
                fetchItems = (context, items, provider) =>
                {
                    string projectPath = ProjectSettingWindow.projectSetting.GetImportAssetPath();
                    if (string.IsNullOrEmpty(projectPath) == false)
                        defaultFolders = new string[] { projectPath };

                    string[] results;

                    if (folders.Count == 0)
                        results = AssetDatabase.FindAssets("t:Scene t:Prefab" + context.searchQuery, defaultFolders);
                    else
                        results = AssetDatabase.FindAssets("t:Scene t:Prefab" + context.searchQuery, folders.ToArray());

                    foreach (var guid in results)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        Object assetObj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                        if (PrefabUtility.GetPrefabAssetType(assetObj) != PrefabAssetType.NotAPrefab)
                        {
                            if ((assetObj as GameObject).GetComponentInChildren<Light>() == null)
                                continue;
                        }

                        items.Add(provider.CreateItem(context, assetPath, null, null, null, null));
                    }
                    return null;

                },
#pragma warning disable UNT0008 // Null propagation on Unity objects
                // Use fetch to load the asset asynchronously on display
                fetchThumbnail = (item, context) => AssetDatabase.GetCachedIcon(item.id) as Texture2D,
                fetchPreview = (item, context, size, options) =>
                {
                    string itemPath = item.id;
                    string previewPath = itemPath.Replace(Path.GetFileName(itemPath), Path.GetFileNameWithoutExtension(itemPath) + ".png");

                    Texture previewTex = AssetDatabase.LoadAssetAtPath<Texture>(previewPath);

                    if (previewTex != null)
                    {
                        item.preview = AssetPreview.GetAssetPreview(previewTex) as Texture2D;
                    }
                    else
                        item.preview = AssetPreview.GetAssetPreview(item.ToObject()) as Texture2D;
                    return null;
                },
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
            return new[]
            {
            new SearchAction(id, "Edit", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.OpenAsset(item.ToObject());
                }
            },
            new SearchAction(id, "Show in Explorer", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.ShowinExplorer(item.id);
                }
            },
            new SearchAction(id, "Duplicate", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DuplicateSelectedAssets();

                    // To do : duplicate the preview Image as well.
                }
            },
            new SearchAction(id, "Rename", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.RenameSelectedAssets();

                    // To do : rename the preview Image as well.
                }
            },
            new SearchAction(id, "Delete", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DeleteSelectedAssets();

                    // To do : Delete the preview Image as well.
                    string fileExtension = Path.GetExtension(item.id);
                    string previewImgPath = item.id.Replace(fileExtension, ".png");

                    if (AssetDatabase.LoadAssetAtPath<Texture>(previewImgPath) != null)
                        AssetDatabase.DeleteAsset(previewImgPath);
                }
            }
        };
        }
    }

}
