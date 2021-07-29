using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace LookDev.Editor
{
    public class LookDevSearchHelpers
    {
        public static ISearchView searchView;
        public static EditorWindow searchViewEditorWindow;
        public static IMGUIContainer searchViewContainer;

        public static void Initialize()
        {
            SetDisableAllProviders();

            SearchService.GetProvider("LookDev_Material").active = true;
            SearchService.GetProvider("LookDev_Texture").active = true;
            SearchService.GetProvider("LookDev_Model").active = true;


            searchView = SearchService.ShowWindow(reuseExisting: true, multiselect: true);
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
        }


    }
}
