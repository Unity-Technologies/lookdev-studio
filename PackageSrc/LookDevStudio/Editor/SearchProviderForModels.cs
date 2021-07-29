using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.Linq;

namespace LookDev.Editor
{
    internal static class SearchProviderForModels
    {
        internal static string id = "LookDev_Model";
        internal static string name = "Model";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "mod:",
                priority = 12, // put example provider at a low priority
                fetchItems = (context, items, provider) =>
                {
                    // That provider searches for tree prefabs in the project
                    //var results = AssetDatabase.FindAssets("t:Prefab tree" + context.searchQuery);
                    var results = AssetDatabase.FindAssets("t:Model t:Prefab " + context.searchQuery, new string[]
                    {
                        "Assets/LookDev/Models",
                        "Packages/com.unity.lookdevstudio/DefaultAssets/Models"
                    });

                    foreach (var guid in results)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        var firstRenderer = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponentInChildren<Renderer>();

                        // It's to hide a model which does not have renderers
                        // If the model just has only animation without having any renderers, the file will be deleted.
                        if (firstRenderer != null)
                            items.Add(provider.CreateItem(context, AssetDatabase.GUIDToAssetPath(guid), null, null, null, null));
                        else
                            AssetDatabase.DeleteAsset(assetPath);

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
            
            new SearchAction(id, "Show in Explorer", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.ShowinExplorer(item.id);
                }
            },
            new SearchAction(id, "Duplicate Model", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DuplicateSelectedAssets();
                }
            },
            new SearchAction(id, "Rename Model", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.RenameSelectedAssets();
                }
            },
            new SearchAction(id, "Delete Model", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DeleteSelectedAssets();
                }
            },
        };
        }

        /*
        public static void Init()
        {
            ISearchView searchView = SearchService.ShowContextual(id);
        }

        static void InitMaterialSearcherBytheProvider()
        {
            TestSearchService.SetAllSearchProvidersDisabled();
            Init();
        }

        public static void InitAllLookDevSearcher()
        {

            TestSearchService.SetAllSearchProvidersDisabled();
            ISearchView searchView = SearchService.ShowContextual("LookDev_Material", "LookDev_Texture", "LookDev_Model");

            Debug.Log(searchView.context.focusedWindow);
            Debug.Log(searchView.context.filterId);

        }
        */
    }
}

