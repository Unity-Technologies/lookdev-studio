using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.IO;
using System.Linq;

namespace LookDev.Editor
{
    public static class SearchProviderForTextures
    {
        internal static string id = "LookDev_Texture";
        internal static string name = "Texture";

        public static List<string> folders = new List<string>();
        public static List<string> objectsGUID = new List<string>();

        static readonly string defaultLookdevFolder = "Assets/LookDev/Textures";
        public static string defaultFolder = "Assets/LookDev/Textures";

        static string[] results;
        static List<string> resultList = new List<string>();


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
                    if (ProjectSettingWindow.projectSetting != null)
                    {
                        string projectPath = ProjectSettingWindow.projectSetting.GetImportAssetPath();
                        if (string.IsNullOrEmpty(projectPath) == false)
                            defaultFolder = projectPath;
                        else
                            defaultFolder = defaultLookdevFolder;
                    }
                    else
                        defaultFolder = defaultLookdevFolder;

                    resultList.Clear();

                    if (folders.Count == 0 && objectsGUID.Count == 0)
                    {
                        results = AssetDatabase.FindAssets("t:Texture " + context.searchQuery, new string[] { defaultFolder });
                        resultList = results.ToList<string>();
                        results.Initialize();
                    }
                    else
                    {
                        if (folders.Count != 0)
                        {
                            results = AssetDatabase.FindAssets("t:Texture " + context.searchQuery, folders.ToArray());
                            resultList = results.ToList<string>();
                            results.Initialize();
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
                fetchDescription = (item, context) => item.id,
                toObject = (item, type) => AssetDatabase.LoadMainAssetAtPath(item.id),
#pragma warning restore UNT0008 // Null propagation on Unity objects
                // Shows handled actions in the preview inspector
                // Shows inspector view in the preview inspector (uses toObject)
                showDetails = false,
                showDetailsOptions = ShowDetailsOptions.None,
                trackSelection = (item, context) =>
                {
                    var obj = AssetDatabase.LoadMainAssetAtPath(item.id);
                    if (obj != null)
                    {
                        if (context.selection.Count == 1)
                        {
                            //EditorGUIUtility.PingObject(obj.GetInstanceID());
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
                            instantFilter.filterName = $"FROM_TEXTURE ({System.IO.Path.GetFileNameWithoutExtension(item.id)})";
                            instantFilter.objectGuid.Add(AssetDatabase.AssetPathToGUID(item.id));
                            instantFilter.showModel = true;
                            instantFilter.showPrefab = true;

                            LookDevSearchFilters.SaveFilter(instantFilter);

                            LookDevSearchFilters.RefreshFilters();

                            lookDevSearchFilters.OnRemoveAllFilters();

                            if (LookDevSearchFilters.filters.ContainsKey(instantFilter.filterName))
                                LookDevSearchFilters.filters[instantFilter.filterName].enabled = true;

                            lookDevSearchFilters.OnChangedFilters();
                        }
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
