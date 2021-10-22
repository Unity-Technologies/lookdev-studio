using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace LookDev.Editor
{
    public class LookDevSearchHelpers
    {
        public static ISearchView searchView;
        public static EditorWindow searchViewEditorWindow;
        public static IMGUIContainer searchViewContainer;

        public static string[] lookDevProviderNames =
            new string[] { "LookDev_Material", "LookDev_Texture", "LookDev_Model", "LookDev_Shader", "LookDev_Light", "LookDev_HDRI", "LookDev_Animation" };

        public static Texture[] lookDevSearchIcons =
            new Texture[] { Resources.Load<Texture>("Icon_Mat"), Resources.Load<Texture>("Icon_Tex"), Resources.Load<Texture>("Icon_Mod"), Resources.Load<Texture>("Icon_Sha"), Resources.Load<Texture>("Icon_Lgt"), Resources.Load<Texture>("Icon_Hdr"), Resources.Load<Texture>("Icon_Anim") };

        public static string[] lookDevSearchIconDesc =
            new string[] { "Material", "Texture", "Model", "Shader", "Light", "HDRi", "Animation" };

        public static List<SearchContext> lookDevSearchContext;

        public static int currentContextId;

        static Color rectColor = Color.green;

        public static void OnGUI(Rect position)
        {
            GUILayout.BeginArea(new Rect(1, position.height - 40, Screen.width, 40));
            GUILayout.BeginHorizontal("Box");
            GUILayout.FlexibleSpace();

            for (int i=0;i<lookDevProviderNames.Length;i++)
            {
                GUILayout.Space(4);
                if (GUILayout.Button(new GUIContent(lookDevSearchIcons[i], lookDevSearchIconDesc[i]), GUILayout.Width(32), GUILayout.Height(32)))
                    SwitchCurrentProvider(i);
                if (i == currentContextId)
                {
                    Rect rect = GUILayoutUtility.GetLastRect();
                    EditorGUI.DrawRect(new Rect(rect.x, rect.height + 3, rect.width, 3), rectColor);
                }
                GUILayout.Space(4);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public static void SwitchCurrentProvider(int index)
        {
            if (index == -1)
            {
                rectColor = Color.gray;
                SearchContext newContext = new SearchContext(new List<SearchProvider>() { SearchService.GetProvider("LookDev_Correlation") });
                searchView = SearchService.ShowWindow(context: newContext, reuseExisting: true, multiselect: true);
            }
            else
            {
                rectColor = Color.green;
                currentContextId = index;
                searchView = SearchService.ShowWindow(context: lookDevSearchContext[currentContextId], reuseExisting: true, multiselect: true);
            }
            RefreshWindow();
        }

        static void SetNullContext()
        {
            searchView = SearchService.ShowWindow(reuseExisting: true, multiselect: true);
        }

        public static void Initialize()
        {
            currentContextId = 0;
            lookDevSearchContext = new List<SearchContext>();

            if (LookDevStudioEditor.IsHDRP())
            {
                lookDevProviderNames = new string[] { "LookDev_Material", "LookDev_Texture", "LookDev_Model", "LookDev_Shader", "LookDev_Light", "LookDev_HDRI", "LookDev_Animation" };
                lookDevSearchIcons = new Texture[] { Resources.Load<Texture>("Icon_Mat"), Resources.Load<Texture>("Icon_Tex"), Resources.Load<Texture>("Icon_Mod"), Resources.Load<Texture>("Icon_Sha"), Resources.Load<Texture>("Icon_Lgt"), Resources.Load<Texture>("Icon_Hdr"), Resources.Load<Texture>("Icon_Anim") };
                lookDevSearchIconDesc = new string[] { "Material", "Texture", "Model", "Shader", "Light", "HDRi", "Animation" };
    }
            else // if URP
            {
                lookDevProviderNames = new string[] { "LookDev_Material", "LookDev_Texture", "LookDev_Model", "LookDev_Shader", "LookDev_Light", "LookDev_Skybox", "LookDev_Animation" };
                lookDevSearchIcons = new Texture[] { Resources.Load<Texture>("Icon_Mat"), Resources.Load<Texture>("Icon_Tex"), Resources.Load<Texture>("Icon_Mod"), Resources.Load<Texture>("Icon_Sha"), Resources.Load<Texture>("Icon_Lgt"), Resources.Load<Texture>("Icon_Hdr"), Resources.Load<Texture>("Icon_Anim") };
                lookDevSearchIconDesc = new string[] { "Material", "Texture", "Model", "Shader", "Light", "Skybox", "Animation" };
            }


            foreach (string lookDevProviderName in lookDevProviderNames)
            {
                lookDevSearchContext.Add(new SearchContext(new List<SearchProvider>() { SearchService.GetProvider(lookDevProviderName) }));
            }

            SetDisableAllProviders();

            SwitchCurrentProvider(currentContextId);

            searchViewEditorWindow = (EditorWindow)LookDevSearchHelpers.searchView;


            // Force to have a gridview as default
            searchView.itemIconSize = (float)DisplayMode.Grid;

            VisualElement rootVisualElement = searchViewEditorWindow.rootVisualElement;

            if (rootVisualElement != null)
                searchViewContainer = rootVisualElement.parent.Q<IMGUIContainer>(className: "unity-imgui-container");

            RegisterCallbacks();
            
        }

        public static void RegisterCallbacks()
        {
            if (searchViewContainer != null)
            {
                searchViewContainer.RegisterCallback<MouseUpEvent>(OnMouseUp);
                //searchViewContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
                searchViewContainer.RegisterCallback<DragUpdatedEvent>(OnMouseDragUpdate);
                searchViewContainer.RegisterCallback<DragPerformEvent>(OnMouseDragPerform);
            }
            else
                Debug.LogError("Could not find IMGUIContainer of the Quick search.");
        }

        public static void UnregisterCallbacks()
        {
            if (searchViewContainer != null)
            {
                searchViewContainer.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                //searchViewContainer.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                searchViewContainer.UnregisterCallback<DragUpdatedEvent>(OnMouseDragUpdate);
                searchViewContainer.UnregisterCallback<DragPerformEvent>(OnMouseDragPerform);
            }
            else
                Debug.LogError("Could not find IMGUIContainer of the Quick search.");
        }

        public static void RefreshWindow()
        {
            if (searchView != null)
            {
                searchView.Refresh();
                searchView.Repaint();
            }
        }

        public static void SetDisableAllProviders()
        {
            foreach (SearchProvider searchProvider in SearchService.Providers)
            {
                searchProvider.active = false;
            }
        }


        static void OnMouseDragUpdate(DragUpdatedEvent evt)
        {
            LookDevHelpers.DragDrop();
        }

        static void OnMouseDragPerform(DragPerformEvent evt)
        {
            LookDevHelpers.DragDrop();
        }

        static void OnMouseDown(MouseDownEvent evt)
        {
            // For testing
        }

        static void OnMouseUp(MouseUpEvent evt)
        {
            // To do : use the postion for the context menu with multi-selection
            /*
            Debug.LogError(Event.current.type);
            Debug.LogError("Mouse UP!!");
            Debug.LogError("Button ID :" + evt.button.ToString());
            Debug.LogError("\tLocal : " + evt.localMousePosition);
            Debug.LogError("\tPos :" + evt.mousePosition);
            Debug.LogError("\tOri Pos :" + evt.originalMousePosition);
            */
            if (evt.button == 1 && Selection.objects.Length >= 2)
            {
                SearchPopup searchPopup = new SearchPopup();

                UnityEditor.PopupWindow.Show(new Rect(new Vector2(evt.localMousePosition.x, evt.localMousePosition.y), searchPopup.GetWindowSize()), searchPopup);

                Event.current.Use();
            }

        }


    }
}
