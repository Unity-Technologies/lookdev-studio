using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.Linq;

namespace LookDev.Editor
{
    static class SearchProviderForShader
    {
        internal static string id = "LookDev_Shader";
        internal static string name = "Shader";

        public static List<string> folders = new List<string>();
        public static List<string> objectsGUID = new List<string>();

        public static string defaultFolder = "Assets/LookDev/Shaders";


        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "sha:",
                priority = 13,
                fetchItems = (context, items, provider) =>
                {
                    string projectPath = ProjectSettingWindow.projectSetting.GetImportAssetPath();
                    if (string.IsNullOrEmpty(projectPath) == false)
                        defaultFolder = projectPath;

                    string[] results;
                    List<string> resultList = new List<string>();

                    if (folders.Count == 0 && objectsGUID.Count == 0)
                    {
                        results = AssetDatabase.FindAssets("t:Shader " + context.searchQuery, new string[] { defaultFolder });
                        resultList = results.ToList<string>();
                    }
                    else
                    {
                        if (folders.Count != 0)
                        {
                            results = AssetDatabase.FindAssets("t:Shader " + context.searchQuery, folders.ToArray());
                            resultList = results.ToList<string>();
                        }

                        if (objectsGUID.Count != 0)
                        {
                            for (int i = 0; i < objectsGUID.Count; i++)
                            {
                                if (resultList.Contains(objectsGUID[i]) == false)
                                    resultList.Add(objectsGUID[i]);
                            }
                        }
                    }

                    foreach (var guid in resultList)
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
            new SearchAction(id, "Duplicate Shader", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DuplicateSelectedAssets();
                }
            },
            new SearchAction(id, "Rename Shader", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.RenameSelectedAssets();
                }
            },
            new SearchAction(id, "Delete Shader", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.DeleteSelectedAssets();
                }
            },
            new SearchAction(id, "Show related Assets", null, "Description")
            {
                handler = (item) =>
                {
                    LookDevSearchFilters lookDevSearchFilters = EditorWindow.GetWindow<LookDevSearchFilters>();

                    if (lookDevSearchFilters != null)
                    {
                        LookDevFilter instantFilter = new LookDevFilter();
                        instantFilter.enabled = true;
                        instantFilter.filterName = $"FROM_SHADER ({System.IO.Path.GetFileNameWithoutExtension(item.id)})";
                        instantFilter.objectGuid.Add(AssetDatabase.AssetPathToGUID(item.id));
                        instantFilter.showModel = true;
                        instantFilter.showPrefab = true;

                        lookDevSearchFilters.OnRemoveAllFilters();

                        LookDevSearchFilters.SaveFilter(instantFilter);

                        LookDevSearchFilters.RefreshFilters();
                        lookDevSearchFilters.OnChangedFilters();
                    }
                }
            }
        };
        }
    }

}
