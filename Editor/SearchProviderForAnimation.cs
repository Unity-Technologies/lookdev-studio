using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using System.IO;


namespace LookDev.Editor
{
    internal static class SearchProviderForAnimation
    {
        internal static string id = "LookDev_Animation";
        internal static string name = "Animation";

        public static List<string> folders = new List<string>();

        static readonly string defaultLookdevFolder = "Assets/LookDev";
        public static string defaultFolder = "Assets/LookDev";

        static string[] results;


        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(id, name)
            {
                active = false,
                filterId = "anim:",
                priority = 20, // put example provider at a low priority
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


                    if (folders.Count == 0)
                        results = AssetDatabase.FindAssets("t:Animationclip " + context.searchQuery, new string[] { defaultFolder });
                    else
                        results = AssetDatabase.FindAssets("t:Animationclip " + context.searchQuery, folders.ToArray());

                    foreach (var guid in results)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                        List<AnimationClip> AnimClips = new List<AnimationClip>();

                        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) == null)
                            continue;

                        Object[] allObjs = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

                        foreach (Object obj in allObjs)
                        {
                            if (obj == null)
                                continue;

                            if (obj.GetType() == typeof(AnimationClip))
                                AnimClips.Add(obj as AnimationClip);
                        }

                        if (AnimClips.Count == 0)
                            continue;


                        foreach(AnimationClip animationClip in AnimClips)
                        {
                            items.Add(provider.CreateItem(context, assetPath, $"{Path.GetFileNameWithoutExtension(assetPath)}@{animationClip.name}", null, null, null));
                        }

                    }

                    results.Initialize();


                    return null;

                },
#pragma warning disable UNT0008 // Null propagation on Unity objects
                // Use fetch to load the asset asynchronously on display
                fetchThumbnail = (item, context) => AssetPreview.GetAssetPreview(Resources.Load<Texture2D>("Icon_Clip")),
                fetchPreview = (item, context, size, options) => AssetPreview.GetAssetPreview(Resources.Load<Texture2D>("Icon_Clip")),
                //fetchLabel = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id)?.name,
                fetchDescription = (item, context) => AssetDatabase.LoadMainAssetAtPath(item.id)?.name,
                toObject = (item, type) =>
                {
                    Object[] allObjs = AssetDatabase.LoadAllAssetRepresentationsAtPath(item.id);

                    foreach (Object obj in allObjs)
                    {
                        if (obj.GetType() == typeof(AnimationClip))
                        {
                            string[] tokens = item.label.Split('@');

                            if (obj.name == tokens[tokens.Length-1])
                            {
                                return obj;
                            }
                        }
                    }

                    return null;
                    //return AssetDatabase.LoadMainAssetAtPath(item.id);
                },
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
                        Object target = item.ToObject();

                        if (target != null)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new Object[] { target };

                            DragAndDrop.StartDrag(item.label);

                        }
                    }
                    
                }
            };
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {

            new SearchAction(id, "Load Animation", null, "Description")
            {
                handler = (item) =>
                {
                    AnimationTool.AddAnimator();
                    AnimationTool.StopAnimator();

                    AnimationTool.LoadAnimation(item.ToObject<AnimationClip>());

                    AnimationTool.PlayAnimator();
                }
            },
            new SearchAction(id, "Show in Explorer", null, "Description")
            {
                handler = (item) =>
                {
                    AssetManageHelpers.ShowinExplorer(item.id);
                }
            },
            /*
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
            */
        };
        }

    }
}

