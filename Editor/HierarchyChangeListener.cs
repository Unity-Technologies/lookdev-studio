using System;
using System.Linq;
using LookDev.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HierarchyChangeListener
{
    static Transform m_Camera;
    static Transform CameraRootTransform
    {
        get
        {
            if (m_Camera == null)
            {
                var objs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                m_Camera = objs.First(x => string.Equals(x.name, "Camera", StringComparison.Ordinal)).transform;
            }

            return m_Camera;
        }
    }

    static Transform m_PostProcess;
    static Transform PostProcessRootTransform
    {
        get
        {
            if (m_PostProcess == null)
            {
                var objs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                m_Models = objs.First(x => string.Equals(x.name, "Models", StringComparison.Ordinal)).transform;
            }

            return m_PostProcess;
        }
    }

    static Transform m_Lights;
    static Transform LightRootTransform
    {
        get
        {
            if (m_Lights == null)
            {
                var objs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                m_Lights = objs.First(x => string.Equals(x.name, "Lights", StringComparison.Ordinal)).transform;
            }

            return m_Lights;
        }
    }

    static Transform m_Models;
    static Transform ModelsRootTransform
    {
        get
        {
            if (m_Models == null)
            {
                var objs = EditorSceneManager.GetActiveScene().GetRootGameObjects();
                m_Models = objs.First(x => string.Equals(x.name, "Models", StringComparison.Ordinal)).transform;
            }

            return m_Models;
        }
    }

    static HierarchyChangeListener()
    {
        // Switching modes does not invoke domain reload, so we cannot depend
        // on static constructors to actively subscribe/unsubscribe from events.

        // To avoid duplicate listeners,
        // unsubscribe first, if previously subscribed.
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;

        EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
    }

    static void OnActiveSceneChanged(Scene current, Scene next)
    {
        if (!LookDevStudioEditor.lookDevEnabled)
            return;

        // Rescan all the root references.
        var objs = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        if (objs.Length == 0)
            return;

        m_Lights = objs.First(x => string.Equals(x.name, "Lights", StringComparison.Ordinal)).transform;
        m_Models = objs.First(x => string.Equals(x.name, "Models", StringComparison.Ordinal)).transform;
        m_Camera = objs.First(x => string.Equals(x.name, "Camera", StringComparison.Ordinal)).transform;
        m_PostProcess = objs.First(x => string.Equals(x.name, "PostProcess", StringComparison.Ordinal)).transform;
    }

    static void OnHierarchyChanged()
    {
        //NOTE(Bronson): This callback is also called from the Preview Window because it has its own
        // scene. As a result, we have to check if the selected object is in the active scene, otherwise selecting
        // an object in the asset browser throws error when it tries to re-parent.
        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.scene != EditorSceneManager.GetActiveScene())
            return;

        if (!LookDevStudioEditor.lookDevEnabled) return;
        if (!Selection.activeGameObject) return;
        if (Validate<Light>(LightRootTransform)) return;
        if (Validate<Camera>(CameraRootTransform)) return;
        if (Validate<Volume>(PostProcessRootTransform)) return;
        if (Validate<MeshRenderer>(ModelsRootTransform)) return;
    }

    static bool Validate<T>(Transform reference) where T : Component
    {
        if (IsOfType<T>(Selection.activeGameObject))
        {
            Transform root = Selection.activeGameObject.transform.root;
            if (root != reference)
            {
                Selection.activeGameObject.transform.parent = reference;
                Debug.LogWarning(
                    $"Incorrect hierarchy location for '{Selection.activeGameObject.name}', under [{root.name}]. Re-sorting under [{reference.name}] parent.");
            }

            return true;
        }

        return false;
    }

    static bool IsOfType<T>(GameObject gameObject) where T : Component
    {
        return gameObject.GetComponent<T>() || gameObject.GetComponentInChildren<T>();
    }
}