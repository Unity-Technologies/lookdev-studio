using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LookDev.Editor
{
    public class AnimatorSelectionWindow : EditorWindow
    {
        public List<Animator> animators = new List<Animator>();

        Vector2 scrollPos;
        Animator selectedAnimator = null;

        private void OnGUI()
        {
            GUILayout.BeginScrollView(scrollPos);

            GUILayout.BeginVertical();

            for (int i=0;i< animators.Count;i++)
            {
                if (GUILayout.Button($"{animators[i].name}"))
                {
                    selectedAnimator = animators[i];
                    this.Close();
                }
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    Selection.activeGameObject = animators[i].gameObject;
                }

            }

            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        public void Reset()
        {
            selectedAnimator = null;
            animators.Clear();
            scrollPos = Vector2.zero;
        }


        public Animator GetSelectedAnimator()
        {
            return selectedAnimator;
        }

    }
}