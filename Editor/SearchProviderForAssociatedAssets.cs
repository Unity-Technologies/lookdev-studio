using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;


namespace LookDev.Editor
{
    static class SearchProviderForAssociatedAssets
    {
        internal static string id = "LookDev_Correlation";
        internal static string name = "Associated Assets";

        public static List<string> associatedAssetPaths = new List<string>();

        public static void AddAssetPath(string path)
        {
            if (associatedAssetPaths.Contains(path) == false)
                associatedAssetPaths.Add(path);
        }

        public static void RemoveAssetPath(string path)
        {
            if (associatedAssetPaths.Contains(path) == true)
                associatedAssetPaths.Remove(path);
            else
                Debug.LogError($"Remove failed. Could not find \"{path}\"");
        }

        public static void DisposeAssetPaths()
        {
            associatedAssetPaths.Clear();
        }

        public static void FindAssociatedAssets(string assetPath)
        {
            // if Material? -> Find Textures, Find Model

            // if Texture? -> Find Material

            // if Model or Prefab?

        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "rel:",
                priority = 20,
                fetchItems = (context, items, provider) =>
                {
                    foreach (var assetPath in associatedAssetPaths)
                    {
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
            return new[]
            {
            new SearchAction(id, "Find Associated Assets", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.GoToAsset(item.id);
                }
            },
            new SearchAction(id, "Show in Explorer", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.ShowinExplorer(item.id);
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
        }

        /*
        public static void Init()
        {
            SearchService.ShowContextual(id);
        }

        [MenuItem("DEBUG/SearchProvider/Open the searcher by the generated SearchProvider : Texture")]
        static void InitMaterialSearcherBytheProvider()
        {
            TestSearchService.SetAllSearchProvidersDisabled();
            Init();
        }
        */
    }

}
