using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;


namespace LookDev.Editor
{
    static class SearchProviderForMaterials
    {
        internal static string id = "LookDev_Material";
        internal static string name = "Material";

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
                    // That provider searches for tree prefabs in the project
                    //var results = AssetDatabase.FindAssets("t:Prefab tree" + context.searchQuery);
                    var results = AssetDatabase.FindAssets("t:Material " + context.searchQuery, new string[]
                    {
                        "Assets/LookDev/Materials",
                        "Packages/com.unity.lookdevstudio/DefaultAssets/Materials"
                    });

                    foreach (var guid in results)
                    {
                        items.Add(provider.CreateItem(context, AssetDatabase.GUIDToAssetPath(guid), null, null, null, null));
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

        /*
        public static void Init()
        {
            //SearchService.ShowContextual(id);
            //SearchService.GetProvider("LookDev_Material").active = true;
            //SearchService.GetProvider("LookDev_Texture").active = true;
            SearchService.GetProvider("LookDev_Model").active = true;

            SearchService.ShowWindow(reuseExisting: true, multiselect: true);
        }

        [MenuItem("DEBUG/SearchProvider/Open the searcher by the generated SearchProvider")]
        static void InitSearcherBytheProvider()
        {
            TestSearchService.SetAllSearchProvidersDisabled();
            Init();
        }
        */
    }

}
