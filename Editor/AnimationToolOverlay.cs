using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;


[EditorTool("LDS Animation Tool")]
public class AnimationToolOverlay : EditorTool
{
    GUIContent m_IconContent;
    float speed = 1f;

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
            GUILayout.BeginArea(new Rect(window.position.width - 200, (window.position.height - 250) - (25 * AnimationTool.loadedClips.Count), 180, 115 + (25 * AnimationTool.loadedClips.Count)));
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
                                GUIUtility.ExitGUI();
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


        GUILayout.BeginArea(new Rect(window.position.width - 200, window.position.height - 175, 180, 145));
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Animation Tool", EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(18)))
                {
                    Tools.current = Tool.None;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal("Box");


                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimRewind"), GUILayout.Width(32), GUILayout.Height(32)))
                    AnimationTool.RewindAnimator();

                if (AnimationTool.isPlayed == false)
                {
                    if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimPlay"), GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        if (AnimationTool.targetAnimator != null)
                        {
                            AnimationTool.PlayAnimator();
                            AnimationTool.isPlayed = true;
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimPause"), GUILayout.Width(32), GUILayout.Height(32)))
                    {
                        AnimationTool.PauseAnimator();
                        AnimationTool.isPlayed = false;
                    }
                }

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimStop"), GUILayout.Width(32), GUILayout.Height(32)))
                {
                    AnimationTool.StopAnimator();
                    AnimationTool.isPlayed = false;
                }

                if (GUILayout.Button(Resources.Load<Texture>("Icon_AnimReset"), GUILayout.Width(32), GUILayout.Height(32)))
                {
                    AnimationTool.ResetAnimator();
                    AnimationTool.isPlayed = false;
                }

                GUILayout.EndHorizontal();

                GUILayout.Label("Speed", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                speed = EditorGUILayout.Slider(speed, 0f, 3f);
                if (EditorGUI.EndChangeCheck())
                {
                    AnimationTool.SetSpeed(speed);
                }

                EditorGUILayout.Space();
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
