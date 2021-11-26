using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{

    public class LookDevSceneMenu
    {
        static bool isIsolationMode;


        public static void RegisterSceneMenu()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            isIsolationMode = false;
        }

        public static void UnregisterSceneMenu()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        static GameObject currentGo;
        static int currentGoMaterialIndex;

        static bool CheckRenderableObjectSelected()
        {
            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.GetComponent<Renderer>() != null)
                    return true;
            }

            return false;
        }

        static Vector2 startMousePos; 

        static void OnSceneGUI(SceneView sceneView)
        {
            bool isRenderObjectSelected = CheckRenderableObjectSelected();

            Event e = Event.current;
            
            if (e.type == EventType.MouseMove && e.type != EventType.MouseDrag)
            {
                if (e.keyCode == KeyCode.LeftAlt || e.keyCode == KeyCode.RightAlt)
                    currentGo = null;
                else
                    currentGo = HandleUtility.PickGameObject(Event.current.mousePosition, out currentGoMaterialIndex);
            }

            if (e != null && e.button == 1 && e.type == EventType.MouseDown)
                startMousePos = e.mousePosition;

            if (e != null && e.button == 1 && e.type == EventType.MouseUp && currentGo != null && e.keyCode != KeyCode.LeftAlt && e.keyCode != KeyCode.RightAlt)
            {
                Vector2 endMousePos = e.mousePosition;

                if (Vector2.Distance(startMousePos, endMousePos) > 1f)
                    return;

                currentGo = HandleUtility.PickGameObject(Event.current.mousePosition, out currentGoMaterialIndex);

                if (currentGo == null)
                    return;


                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Object Menu"), false, null);
                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Edit"), false, null);
                //menu.AddItem(new GUIContent("\tNew Material for mesh part(TBD)"), false, null, "menu_1");

                if (isRenderObjectSelected)
                {
                    if (isIsolationMode == false)
                        menu.AddItem(new GUIContent("\tIsolate Selected"), false, OnIsolateSelection, null);
                    else
                        menu.AddItem(new GUIContent("\tExit Isolate Selected"), false, OnIsolateSelection, null);
                }
                else
                {
                    if (isIsolationMode == false)
                        menu.AddItem(new GUIContent("\tIsolate Selected"), false, null);
                    if (isIsolationMode == true)
                        menu.AddItem(new GUIContent("\tExit Isolate Selected"), false, null);
                }

                menu.AddSeparator("");

                if (isRenderObjectSelected)
                {
                    menu.AddItem(new GUIContent("Tool"), false, null);
                    //menu.AddItem(new GUIContent("\tEdit Pivot(TBD)"), false, null, "menu_1");
                    //menu.AddItem(new GUIContent("\tMake Lods(TBD)"), false, null, "menu_1");
                    //menu.AddItem(new GUIContent("\tMake Collision(TBD)"), false, null, "menu_1");
                    menu.AddItem(new GUIContent("\tGo to material"), false, OnOpenAssocatedMaterial, null);
                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("External"), false, null);
                    menu.AddItem(new GUIContent("\tEdit Mesh in DCC"), false, OnEditMeshinDCC, null);
                    //menu.AddItem(new GUIContent("\tEdit in Painter(TBD)"), false, null, "menu_1");
                    //menu.AddItem(new GUIContent("\tExport Package(TBD)"), false, null, "menu_1");
                    menu.AddItem(new GUIContent("\tExport Fbx"), false, OnExportFbxOnSelection, null);

                    menu.AddItem(new GUIContent("\tOpen in Explorer"), false, OnOpenInExplorer, null);
                }
                else
                {
                    menu.AddItem(new GUIContent("Tool"), false, null);
                    //menu.AddItem(new GUIContent("\tEdit Pivot(TBD)"), false, null, null);
                    //menu.AddItem(new GUIContent("\tMake Lods(TBD)"), false, null, null);
                    //menu.AddItem(new GUIContent("\tMake Collision(TBD)"), false, null, null);
                    menu.AddItem(new GUIContent("\tGo to material"), false, OnOpenAssocatedMaterial, null);
                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("External"), false, null);
                    menu.AddItem(new GUIContent("\tEdit Mesh in DCC"), false, OnEditMeshinDCC, null);
                    //menu.AddItem(new GUIContent("\tEdit in Painter(TBD)"), false, null);
                    //menu.AddItem(new GUIContent("\tExport Package(TBD)"), false, null);
                    menu.AddItem(new GUIContent("\tExport Fbx"), false, null);
                    menu.AddItem(new GUIContent("\tOpen in Explorer"), false, null);
                }
                /*
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("XXX/menu item 2"), false, OnMenuClick, "menu_2");
                menu.AddItem(new GUIContent("XXX/menu item 3"), false, OnMenuClick, "menu_3");
                */
                menu.ShowAsContext();
                Event.current.Use();
            }
            else if (e != null && e.button == 1 && e.type == EventType.MouseUp && currentGo == null && e.keyCode != KeyCode.LeftAlt && e.keyCode != KeyCode.RightAlt)
            {
                currentGo = HandleUtility.PickGameObject(Event.current.mousePosition, out currentGoMaterialIndex);

                if (currentGo != null)
                    return;

                Vector2 endMousePos = e.mousePosition;

                if (Vector2.Distance(startMousePos, endMousePos) > 1f)
                    return;

                /*
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Background Menu"), false, null);
                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Background Color(TBD)"), false, null, "menu_1");
                menu.AddItem(new GUIContent("Show Ground(TBD)"), false, null, "menu_1");
                menu.AddItem(new GUIContent("Select Hdr(TBD)"), false, null, "menu_1");
                menu.AddItem(new GUIContent("Show All(TBD)"), false, null, "menu_1");
                
                menu.ShowAsContext();
                Event.current.Use();
                */
            }
        }

        static void OnEditMeshinDCC(object userData)
        {
            if (Selection.activeGameObject != null)
            {
                Renderer renderer = Selection.activeGameObject.GetComponent<Renderer>();

                if (renderer == null)
                    renderer = Selection.activeGameObject.GetComponentInChildren<Renderer>();

                string path = string.Empty;

                if (renderer.GetType() == typeof(SkinnedMeshRenderer))
                {
                    SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;

                    if (skinnedRenderer.sharedMesh != null)
                        path = AssetDatabase.GetAssetPath(skinnedRenderer.sharedMesh);
                }
                else if (renderer.GetType() == typeof(MeshRenderer))
                {
                    MeshFilter meshFilter = renderer.gameObject.GetComponent<MeshFilter>();

                    if (meshFilter.sharedMesh != null)
                        path = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                }

                if (string.IsNullOrEmpty(path) == false)
                {
                    AssetManageHelpers.LoadModelOnDCC(path);
                }
            }
        }



        static void OnMenuClick(object userData)
        {
            EditorUtility.DisplayDialog("Tip", "OnMenuClick" + userData.ToString(), "Ok");
        }

        static void OnOpenAssocatedMaterial(object userData)
        {
            if (currentGo != null)
            {
                Renderer renderer = currentGo.GetComponent<Renderer>();

                if (renderer != null)
                {
                    Material associatedMaterial = renderer.sharedMaterials[currentGoMaterialIndex];
                    AssetManageHelpers.OpenToAsset(AssetDatabase.GetAssetPath(associatedMaterial));
                }

            }

        }


        static void OnExportFbxOnSelection(object userData)
        {
            FbxTool.ExportGameObjects(Selection.gameObjects);
        }


        static void OnOpenInExplorer(object userData)
        {
            if (Selection.activeGameObject != null)
            {
                Renderer renderer = Selection.activeGameObject.GetComponent<Renderer>();

                string path = string.Empty;

                if (renderer.GetType() == typeof(SkinnedMeshRenderer))
                {
                    SkinnedMeshRenderer skinnedRenderer = renderer as SkinnedMeshRenderer;

                    if (skinnedRenderer.sharedMesh != null)
                        path = AssetDatabase.GetAssetPath(skinnedRenderer.sharedMesh);
                }
                else if (renderer.GetType() == typeof(MeshRenderer))
                {
                    MeshFilter meshFilter = renderer.gameObject.GetComponent<MeshFilter>();

                    if (meshFilter.sharedMesh != null)
                        path = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                }

                if (string.IsNullOrEmpty(path) == false)
                {
                    EditorUtility.RevealInFinder(path);
                }
            }
        }


        static void OnIsolateSelection(object userData)
        {
            if (Selection.gameObjects.Length != 0)
            {
                int rendererCount = 0;

                foreach(GameObject go in Selection.gameObjects)
                {
                    if (go.GetComponent<Renderer>() != null)
                        rendererCount++;
                }

                if (rendererCount == 0)
                    return;

                if (isIsolationMode == false)
                {
                    OnSetAllVisible(false);

                    foreach(GameObject selectedGo in Selection.gameObjects)
                        selectedGo.SetActive(true);
                }
                else
                {
                    OnSetAllVisible(true);
                }

                isIsolationMode = !isIsolationMode;
            }
        }

        static void OnSetAllVisible(bool isVisible)
        {
            GameObject modelRoot = GameObject.Find("/Models");

            if (modelRoot != null)
            {
                if (modelRoot.transform.childCount == 0)
                    return;

                for (int i=0;i<modelRoot.transform.childCount;i++)
                {
                    Renderer[] rendereres = modelRoot.transform.GetChild(i).gameObject.GetComponentsInChildren<Renderer>(true);

                    foreach(Renderer renderer in rendereres)
                    {
                        renderer.gameObject.SetActive(isVisible);
                    }
                }
            }
        }


    }
}