using UnityEditor;
using UnityEngine;

public class FixWolfPrefab
{
    [MenuItem("Tools/Fix Wolf Prefab")]
    public static void FixPrefab()
    {
        string wolfPath = "Assets/Prefab/Wolf.prefab";
        string balloonPath = "Assets/Prefab/Balloon.prefab";

        GameObject wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(wolfPath);
        GameObject balloonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(balloonPath);

        if (wolfPrefab == null)
        {
            Debug.LogError("Could not find Wolf prefab at " + wolfPath);
            return;
        }
        if (balloonPrefab == null)
        {
            Debug.LogError("Could not find Balloon prefab at " + balloonPath);
            return;
        }

        // Open prefab in memory for editing
        GameObject instanceRoot = (GameObject)PrefabUtility.InstantiatePrefab(wolfPrefab);
        
        // Clean up any weird nested wolves or missing balloons
        foreach (Transform child in instanceRoot.transform)
        {
            if (child.name == "Wolf" || child.name == "Balloon")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Add Balloon prefab instance properly
        GameObject newBalloon = (GameObject)PrefabUtility.InstantiatePrefab(balloonPrefab, instanceRoot.transform);
        newBalloon.name = "Balloon";
        newBalloon.transform.localPosition = new Vector3(0.16f, 2.16f, 0f);

        // Assign to script
        var wolfScript = instanceRoot.GetComponent("Wolf"); // Use string to avoid assembly issues if any
        if (wolfScript != null)
        {
            SerializedObject so = new SerializedObject(wolfScript);
            SerializedProperty balloonProp = so.FindProperty("balloon");
            if (balloonProp != null)
            {
                balloonProp.objectReferenceValue = newBalloon.GetComponent("Balloon");
                so.ApplyModifiedProperties();
            }
        }

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(instanceRoot, wolfPath);
        Object.DestroyImmediate(instanceRoot);

        Debug.Log("Wolf Prefab fixed successfully! The Balloon is now correctly parented and assigned.");
    }
}
