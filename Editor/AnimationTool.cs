using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;


[InitializeOnLoad]
public class AnimationTool
{
    public static readonly int maxLoadedClip = 15;

    public static List<AnimationClip> loadedClips = new List<AnimationClip>();

    public static bool isPause = false;

    public static bool autoRewind = false;

    public static Animator targetAnimator;

    static readonly string defaultAnimatorStateName = "DefaultAnim";

    static readonly string defaultAnimatorControllerPath;

    static string recentlyPlayedClipName;

    static AnimationTool()
    {
        string animatorControllerPath = "Assets/LookDev/AnimatorControllers";

        if (AssetDatabase.IsValidFolder(animatorControllerPath) == false)
            AssetDatabase.CreateFolder("Assets/LookDev", "AnimatorControllers");

        // Generate Default Animator at first
        string defaultAnimatorController = $"{animatorControllerPath}/DefaultAnimatorController.controller";

        if (AssetDatabase.LoadAssetAtPath<Object>(defaultAnimatorController) == null)
        {
            GenerateAnimatorController(defaultAnimatorController);
        }

        defaultAnimatorControllerPath = defaultAnimatorController;
    }


    public static void RegisterLoadedClip(AnimationClip target)
    {
        if (target == null)
            return;

        if (loadedClips.Count == maxLoadedClip)
            loadedClips.RemoveAt(0);

        if (loadedClips.Contains(target) == false)
            loadedClips.Add(target);
    }


    public static void UnregisterLoadedClip(AnimationClip target)
    {
        if (target == null)
            return;

        if (loadedClips.Contains(target))
            loadedClips.Remove(target);
    }


    public static string GetLatestPlayedClipName()
    {
        return recentlyPlayedClipName;
    }


    static List<AnimationClip> GetAnimationClips(string targetPath)
    {
        List<AnimationClip> AnimClips = new List<AnimationClip>();

        if (AssetDatabase.LoadAssetAtPath<Object>(targetPath) == null)
            return AnimClips;

        Object[] allObjs = AssetDatabase.LoadAllAssetRepresentationsAtPath(targetPath);

        foreach(Object obj in allObjs)
        {
            if (obj.GetType() == typeof(AnimationClip))
                AnimClips.Add(obj as AnimationClip);
        }

        return AnimClips;
    }


    static void GetCurrentAnimator(out Animator animator)
    {
        animator = GameObject.FindObjectOfType<Animator>();
    }


    static void UpdateAnimationOnEditor()
    {
        if (targetAnimator != null)
        {
            targetAnimator.Play(defaultAnimatorStateName);
            targetAnimator.Update(Time.deltaTime);

            if (targetAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                if (autoRewind)
                    PlayAnimator();
                else
                    StopAnimator();
            }

            if (targetAnimator.GetCurrentAnimatorStateInfo(0).IsName(defaultAnimatorStateName) == false)
            {
                Debug.LogError("Done!!!");
            }
        }
    }


    public static void LoadAnimation(string targetFBX, int motionID)
    {
        List<AnimationClip> animationClips = GetAnimationClips(targetFBX);

        if (targetAnimator == null)
            GetCurrentAnimator(out targetAnimator);

        string controllerPath = AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController);

        if (string.IsNullOrEmpty(controllerPath))
            return;

        AnimatorController animControl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        AnimatorState defaultState = animControl.layers[0].stateMachine.states[0].state;

        if (motionID < animationClips.Count && defaultState != null)
        {
            animControl.SetStateEffectiveMotion(defaultState, animationClips[motionID]);
            recentlyPlayedClipName = defaultState.motion.name;

            RegisterLoadedClip(animationClips[motionID]);
        }
    }


    public static void LoadAnimation(AnimationClip targetClip)
    {
        GetCurrentAnimator(out targetAnimator);

        if (targetAnimator != null)
        {
            ModelImporter mImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(targetClip)) as ModelImporter;

            if (targetAnimator.avatar != null)
            {
                if (mImporter.sourceAvatar != targetAnimator.avatar)
                {
                    mImporter.animationType = ModelImporterAnimationType.Human;
                    mImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    mImporter.sourceAvatar = targetAnimator.avatar;
                    mImporter.SaveAndReimport();
                }

            }
            else
            {
                if (mImporter.sourceAvatar != targetAnimator.avatar)
                {
                    mImporter.animationType = ModelImporterAnimationType.Generic;
                    mImporter.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                    mImporter.sourceAvatar = null;
                    mImporter.SaveAndReimport();
                }
            }

        }

        string controllerPath = AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController);

        if (string.IsNullOrEmpty(controllerPath))
            return;

        AnimatorController animControl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        AnimatorState defaultState = animControl.layers[0].stateMachine.states[0].state;

        animControl.SetStateEffectiveMotion(defaultState, targetClip);
        recentlyPlayedClipName = defaultState.motion.name;

        RegisterLoadedClip(targetClip);
    }


    public static void AddAnimator()
    {
        GameObject modelGroup = GameObject.Find("Models");

        for (int i = 0; i < modelGroup.transform.childCount; i++)
        {
            if (modelGroup.transform.GetChild(i).gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                if (modelGroup.transform.GetChild(i).gameObject.GetComponentInChildren<Animator>(true) == null)
                {
                    Animator genericAnimator = modelGroup.transform.GetChild(i).gameObject.AddComponent<Animator>();

                    genericAnimator.applyRootMotion = true;
                    genericAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(defaultAnimatorControllerPath);
                }
                else
                {
                    Animator exAnimator = modelGroup.transform.GetChild(i).gameObject.GetComponentInChildren<Animator>(true);
                    exAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(defaultAnimatorControllerPath);
                }
            }
            else
                continue;
        }
    }


    static GameObject GetRootGameObjectWithAnimator()
    {
        if (targetAnimator == null)
            GetCurrentAnimator(out targetAnimator);

        if (targetAnimator != null)
        {
            GameObject modelGroup = GameObject.Find("Models");

            for (int i=0;i<modelGroup.transform.childCount;i++)
            {
                if (modelGroup.transform.GetChild(i).gameObject.GetComponentInChildren<Animator>(true) == targetAnimator)
                    return modelGroup.transform.GetChild(i).gameObject;
            }
        }

        return null;
    }


    public static void PlayAnimator()
    {
        if (targetAnimator == null)
            GetCurrentAnimator(out targetAnimator);

        if (targetAnimator.runtimeAnimatorController == null)
            targetAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(defaultAnimatorControllerPath);

        StopAnimator();

        if (isPause != true)
            RewindAnimator();

        EditorApplication.update += UpdateAnimationOnEditor;

        isPause = false;
    }


    public static void StopAnimator()
    {
        if (targetAnimator == null)
            GetCurrentAnimator(out targetAnimator);

        EditorApplication.update -= UpdateAnimationOnEditor;
    }


    public static void PauseAnimator()
    {
        isPause = true;
        StopAnimator();
    }


    public static void ResetAnimator()
    {
        // Reset Pose
        isPause = false;
        StopAnimator();

        GameObject targetGo = GetRootGameObjectWithAnimator();

        SkinnedMeshRenderer sRend = targetGo.GetComponentInChildren<SkinnedMeshRenderer>();

        string assetPath = AssetDatabase.GetAssetPath(sRend.sharedMesh);

        GameObject originalGo = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        SkinnedMeshRenderer originalSRend = originalGo.GetComponentInChildren<SkinnedMeshRenderer>();

        if (sRend.bones.Length == originalSRend.bones.Length)
        {
            for (int i=0;i<sRend.bones.Length;i++)
            {
                if (sRend.bones[i].localPosition != originalSRend.bones[i].localPosition)
                    sRend.bones[i].localPosition = originalSRend.bones[i].localPosition;

                if (sRend.bones[i].localRotation != originalSRend.bones[i].localRotation)
                    sRend.bones[i].localRotation = originalSRend.bones[i].localRotation;

                if (sRend.bones[i].localScale != originalSRend.bones[i].localScale)
                    sRend.bones[i].localScale = originalSRend.bones[i].localScale;
            }
        }

        if (targetAnimator != null)
        {
            targetAnimator.runtimeAnimatorController = null;
        }

    }


    public static void RewindAnimator()
    {
        if (targetAnimator != null)
        {
            targetAnimator.Play(defaultAnimatorStateName, -1, 0f);
            targetAnimator.Update(Time.deltaTime);
        }
    }


    static void GenerateAnimatorController(string controllerPath)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        if (controller != null)
        {
            //controller.AddParameter("PLAY", AnimatorControllerParameterType.Trigger);

            AnimationClip animationClip = new AnimationClip();
            AnimatorState animatorState = controller.AddMotion(animationClip);
            animatorState.name = "DefaultAnim";

            /*
            AnimatorStateTransition animatorTransition = animatorState.AddExitTransition();
            animatorTransition.hasExitTime = true;
            */
        }
    }


    static List<Transform> GetBoneList()
    {
        List<Transform> boneList = new List<Transform>();

        GameObject targetGo = GetRootGameObjectWithAnimator();

        if (targetGo != null)
        {
            SkinnedMeshRenderer[] skinnedMeshRenderers = targetGo.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach(var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                foreach(var bone in skinnedMeshRenderer.bones)
                {
                    //Debug.Log(bone.name);
                    Debug.Log(skinnedMeshRenderer.bones.Length);
                }

                Debug.Log("---------------------------");
            }
        }

        return boneList;
    }

}
