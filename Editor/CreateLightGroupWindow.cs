using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{
    public class CreateLightGroupWindow : EditorWindow
    {
        public static string lightGroupName;

        static CreateLightGroupWindow createLightGroupWindow;

        public static void ShowWindow()
        {
            createLightGroupWindow = ScriptableObject.CreateInstance<CreateLightGroupWindow>();

            createLightGroupWindow.titleContent = new GUIContent("Create Light Prefab");

            createLightGroupWindow.position = new Rect(new Vector2(SceneView.lastActiveSceneView.position.x, SceneView.lastActiveSceneView.position.y), new Vector2(300, 100));

            createLightGroupWindow.minSize = createLightGroupWindow.position.size;
            createLightGroupWindow.maxSize = createLightGroupWindow.minSize;

            createLightGroupWindow.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            EditorGUILayout.Space();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal("Box");
            {
                GUILayout.Label("Prefab Name :", GUILayout.Width(80));
                lightGroupName = GUILayout.TextField(lightGroupName, GUILayout.Width(200));
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Save Light Prefab"))
            {
                if (Selection.gameObjects.Length == 0)
                {
                    Debug.LogError("No Selected Lights to be Prefab");
                    return;
                }

                if (string.IsNullOrEmpty(lightGroupName))
                {
                    Debug.LogError("Group name is empty");
                    return;
                }

                string newGoName = lightGroupName;

                GameObject lightRootGo = GameObject.FindGameObjectWithTag("LightRig");
                GameObject rootGoInstance = new GameObject(newGoName);
                rootGoInstance.transform.SetParent(lightRootGo.transform);

                foreach (GameObject go in Selection.gameObjects)
                {
                    if (go.GetComponentInChildren<Light>() != null)
                        go.transform.SetParent(rootGoInstance.transform);
                    else
                        Debug.LogWarning($"{go.name} does not have any Light component.");
                }

                PrefabUtility.SaveAsPrefabAssetAndConnect(rootGoInstance, $"Assets/LookDev/Lights/{newGoName}.prefab", InteractionMode.AutomatedAction);

                if (AssetDatabase.LoadAssetAtPath<Texture>("Packages/com.unity.lookdevstudio/Editor/Resources/Icon_LgtGrp.png") != null)
                    AssetDatabase.CopyAsset("Packages/com.unity.lookdevstudio/Editor/Resources/Icon_LgtGrp.png", $"Assets/LookDev/Lights/{newGoName}.png");

                LookDevSearchHelpers.SwitchCurrentProvider(4);

                createLightGroupWindow.Close();
            }

            GUILayout.EndVertical();
        }
    }

}