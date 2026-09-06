using UnityEngine;
using UnityEditor;

public class CheckBgLighting
{
    public static void Run()
    {
        SpriteRenderer[] srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (var sr in srs)
        {
            if (sr.gameObject.name.ToLower().Contains("bg") || sr.gameObject.name.ToLower().Contains("background"))
            {
                Debug.Log($"Found BG: {sr.gameObject.name}, Z: {sr.transform.position.z}, Material: {sr.sharedMaterial.name}");
            }
        }
    }
}
