using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LookDev.Editor
{
    public class FeedbackWindow : EditorWindow
    {
        void OnEnable()
        {
            GenerateVisualElement(this);
        }

        static string _videoLink = "https://www.youtube.com/watch?v=aTD7eoEiNQ0";
        static string _forumLink = "https://forum.unity.com/threads/lookdev-studio.1148474/";
        static string _feedbackLink = "https://unitysoftware.co1.qualtrics.com/jfe/form/SV_b2Bu8eT7dnWtx7U";

        private static void GenerateVisualElement(FeedbackWindow instance)
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.lookdevstudio/UI/Feedback.uxml");
            uxml.CloneTree(instance.rootVisualElement);

            instance.rootVisualElement.Q<Button>("LinkOne").clicked += () =>
            {
                Application.OpenURL(_feedbackLink);
            };
            instance.rootVisualElement.Q<Button>("LinkTwo").clicked += () =>
            {
                Application.OpenURL(_forumLink);
            };
            instance.rootVisualElement.Q<Button>("LinkThree").clicked += () =>
            {
                Application.OpenURL(_videoLink);
            };

            instance.rootVisualElement.Q<Toggle>("ToggleBtn").RegisterValueChangedCallback(OnToggleBtnChanged);

            if (LookDevPreferences.instance != null)
                instance.rootVisualElement.Q<Toggle>("ToggleBtn").value = LookDevPreferences.instance.DoNotShowFeedbackWinOnStart;

        }

        static void OnToggleBtnChanged(ChangeEvent<bool> toggleValue)
        {
            LookDevPreferences.instance.DoNotShowFeedbackWinOnStart = toggleValue.newValue;
        }
    }
}
