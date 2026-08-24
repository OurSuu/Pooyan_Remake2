using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;

public class FixAnimatorController
{
    [MenuItem("Tools/Fix Wolf Animator")]
    public static void FixAnimator()
    {
        string controllerPath = "Assets/Animation/Gpx.controller";
        string runAnimPath = "Assets/Animation/Run.anim";
        string dropAnimPath = "Assets/Animation/Drop.anim";

        // 1. Delete the corrupted controller
        AssetDatabase.DeleteAsset(controllerPath);

        // 2. Create a brand new clean controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // 3. Load the animations you made
        AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(runAnimPath);
        AnimationClip dropClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(dropAnimPath);

        // 4. Add them as States to the clean controller
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
        
        if (runClip != null)
        {
            AnimatorState runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;
            rootStateMachine.defaultState = runState; // Make Run the default state
        }
        else
        {
            Debug.LogError("Could not find Run animation at " + runAnimPath);
        }

        if (dropClip != null)
        {
            AnimatorState dropState = rootStateMachine.AddState("Drop");
            dropState.motion = dropClip;
        }
        else
        {
            Debug.LogError("Could not find Drop animation at " + dropAnimPath);
        }

        // Save everything
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ Gpx.controller fixed successfully! Run and Drop states have been added.");
    }
}
