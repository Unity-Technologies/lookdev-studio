// using System;
// using UnityEditor;
// using UnityEngine;
//
// internal class CameraRootEditor : Editor
// {
//     // This function registers a predicate that tells the Inspector that we
//     // want a specific Editor for any GameObject containing a Camera:
//     [RootEditor]
//     static System.Type CameraRootEditorPredicate(UnityEngine.Object[] objects)
//     {
//         return typeof(CameraRootEditor);
//     }
//
//     // // This function should handle the drawing of all components of the selected game object:
//     // public override void OnInspectorGUI()
//     // {
//     //     DrawDefaultInspector();
//     // }
//     //
//     // protected override void OnHeaderGUI()
//     // {
//     //     GUILayout.Button("OnHeaderGUI");
//     // }
//
//     public override bool UseDefaultMargins()
//     {
//         return false;
//     }
// }

// using UnityEditor;
// using UnityEngine;
//
// [CustomEditor(typeof(GameObject))]
// [CanEditMultipleObjects]
// public class GameObjectEditor : Editor
// {
//     Color darkSkinHeaderColor = (Color)new Color32(62, 62, 62, 255);
//     Color lightSkinHeaderColor = (Color)new Color32(194, 194, 194, 255);
//  
//     protected override void OnHeaderGUI()
//     {
//         var rect = EditorGUILayout.GetControlRect(false, 0f);
//         rect.height = EditorGUIUtility.singleLineHeight * 1.4f;
//         rect.y -= rect.height;
//         rect.x = 60;
//         rect.xMax -= rect.x * 2f;
//  
//         EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? darkSkinHeaderColor : lightSkinHeaderColor);
//  
//         string header = (target as GenericAIBehaviour).DisplayName + " (AI Behaviour)";
//         if (string.IsNullOrEmpty(header))
//             header = target.ToString();
//  
//         EditorGUI.LabelField(rect, header, EditorStyles.boldLabel);
//     }
// }