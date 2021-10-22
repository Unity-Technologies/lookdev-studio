using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;


[EditorTool("LDS Animation Tool")]
public class AnimationToolOverlay : EditorTool
{
    GUIContent m_IconContent;

    void OnEnable()
    {
        m_IconContent = new GUIContent()
        {
            image = Resources.Load<Texture>("Icon_Anim"),
            text = "LDS Animation Tool",
            tooltip = "Tool for Animation Previews"
        };
    }

    public override GUIContent toolbarIcon
    {
        get { return m_IconContent; }
    }

    public override void OnActivated()
    {
    }

    public override void OnWillBeDeactivated()
    {
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
            return;

        Handles.BeginGUI();

        if (AnimationTool.loadedClips.Count > 0)
        {
            GUILayout.BeginArea(new Rect(window.position.width - 200, (window.position.height - 200) - (25 * AnimationTool.loadedClips.Count), 180, 115 + (25 * AnimationTool.loadedClips.Count)));
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Animation List", EditorStyles.boldLabel);

                    // Show the animation list

                    for (int i=0;i<AnimationTool.loadedClips.Count;i++)
                    {
                        AnimationClip loadedClip = AnimationTool.loadedClips[i];

                        if (loadedClip == null)
                            continue;

                        GUILayout.BeginHorizontal("Box");
                        {
                            if (GUILayout.Button(loadedClip.name, GUILayout.Width(145)))
                            {
                                AnimationTool.LoadAnimation(loadedClip);
                                AnimationTool.PlayAnimator();
                            }

                            if (GUILayout.Button("X"))
                            {
                                AnimationTool.UnregisterLoadedClip(loadedClip);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();

        }


        GUILayout.BeginArea(new Rect(window.position.width - 200, window.position.height - 125, 180, 115));
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Label("Animation Tool", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal("Box");

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimRewind"), GUILayout.Width(32), GUILayout.Height(32)))
                    AnimationTool.RewindAnimator();

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimPlay"), GUILayout.Width(32), GUILayout.Height(32)))
                    AnimationTool.PlayAnimator();

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimPause"), GUILayout.Width(32), GUILayout.Height(32)))
                    AnimationTool.PauseAnimator();

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimStop"), GUILayout.Width(32), GUILayout.Height(32)))
                    AnimationTool.StopAnimator();

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimReset"), GUILayout.Width(32), GUILayout.Height(32)))
                    AnimationTool.ResetAnimator();

                GUILayout.EndHorizontal();

                AnimationTool.autoRewind = GUILayout.Toggle(AnimationTool.autoRewind, "Loop Animation");
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();

        /*
        GUILayout.BeginArea(new Rect(window.position.width - 200, window.position.height - 40, 200, 40));
        {
            if (AnimationTool.GetLatestPlayedClipName() != string.Empty)
                GUILayout.Label($"Playing \"{AnimationTool.GetLatestPlayedClipName()}\"", EditorStyles.boldLabel);
        }
        GUILayout.EndArea();
        */

        Handles.EndGUI();
    }



}
