#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LightingSetupTool
{
    [MenuItem("Tools/1-Click Lighting Setup")]
    public static void CreateLights()
    {
        // 1. Create Natural Light (Directional Light = แสงอาทิตย์/แสงจันทร์)
        GameObject dirLightObj = new GameObject("Natural Sun Light");
        Light dirLight = dirLightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(1f, 0.9f, 0.75f); // สีส้มอ่อนๆ โทนอุ่นเข้ากับ Autumn
        dirLight.intensity = 0.8f;
        dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 2. Create Point Light 1 (ดวงไฟเฉพาะจุด)
        GameObject pointLightObj = new GameObject("Firefly Point Light 1");
        Light pLight = pointLightObj.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.5f, 0f); // สีส้มไฟตะเกียง
        pLight.intensity = 2.5f;
        pLight.range = 8f;
        // วางไฟไว้หน้ากล้องนิดนึง (Z = -2) แสงจะได้ตกกระทบ Sprite 2D
        pointLightObj.transform.position = new Vector3(-3f, 2f, -2f); 

        GameObject pointLightObj2 = new GameObject("Firefly Point Light 2");
        Light pLight2 = pointLightObj2.AddComponent<Light>();
        pLight2.type = LightType.Point;
        pLight2.color = new Color(1f, 0.8f, 0.2f); // สีเหลืองนวล
        pLight2.intensity = 2f;
        pLight2.range = 6f;
        pointLightObj2.transform.position = new Vector3(3f, -1f, -2f);

        // 3. ปลดล็อคให้รูป 2D รับแสงได้ (เปลี่ยน Material จาก Default เป็น Diffuse)
        Material diffuseMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Diffuse.mat");
        if (diffuseMat == null)
        {
            Debug.LogError("หา Material Sprites-Diffuse ไม่เจอ! ทำให้ Sprite ไม่รับแสงครับ");
            return;
        }

        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        int changedCount = 0;
        foreach (var sr in renderers)
        {
            if (sr.sharedMaterial != null && sr.sharedMaterial.name == "Sprites-Default")
            {
                sr.sharedMaterial = diffuseMat;
                changedCount++;
            }
        }

        Debug.Log($"จัดแสงให้เรียบร้อยแล้วครับ! เปลี่ยน SpriteRenderer ให้รับแสงไปทั้งหมด {changedCount} ตัว");
        Selection.activeGameObject = dirLightObj;
    }
}
#endif
