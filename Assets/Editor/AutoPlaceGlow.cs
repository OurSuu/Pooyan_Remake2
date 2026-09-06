using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoPlaceGlow
{
    [MenuItem("Tools/2-Place Glowing Windows")]
    public static void PlaceWindows()
    {
        // 1. Create Soft Glow Texture
        string texPath = "Assets/SoftGlow.png";
        if (!File.Exists(texPath))
        {
            Texture2D tex = new Texture2D(128, 128, TextureFormat.ARGB32, false);
            for (int y = 0; y < 128; y++) {
                for (int x = 0; x < 128; x++) {
                    float d = Vector2.Distance(new Vector2(64, 64), new Vector2(x, y)) / 64f;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // Smoothstep
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            }
            tex.Apply();
            File.WriteAllBytes(Application.dataPath + "/SoftGlow.png", tex.EncodeToPNG());
            AssetDatabase.Refresh();
            
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        // 2. Load Sprite and Material
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        Material addMat = new Material(Shader.Find("Mobile/Particles/Additive"));

        // 3. Define Window Positions (Approximate based on user screenshot)
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-8.8f, -2.8f, -1f), // Left Tree Bottom
            new Vector3(-9.1f, -1f, -1f),   // Left Tree Middle
            new Vector3(6.5f, 0.4f, -1f),   // Right Mushroom 1
            new Vector3(7.8f, -0.9f, -1f),  // Right Mushroom 2
            new Vector3(8.5f, -2.5f, -1f)   // Right Mushroom 3
        };

        // 4. Create Glow Objects
        GameObject glowGroup = new GameObject("WindowGlows");
        foreach (Vector3 pos in positions)
        {
            GameObject glow = new GameObject("WindowGlow");
            glow.transform.parent = glowGroup.transform;
            glow.transform.position = pos;
            glow.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            SpriteRenderer sr = glow.AddComponent<SpriteRenderer>();
            sr.sprite = glowSprite;
            sr.material = addMat;
            sr.color = new Color(1f, 0.6f, 0.1f, 0.8f); // Warm Orange Glow
            sr.sortingOrder = 10;
        }

        Debug.Log("🟢 วางแสงหน้าต่าง 2D แบบ Additive ให้เรียบร้อยแล้วครับ! (ใช้ Move Tool ขยับตำแหน่งให้ตรงหน้าต่างได้เลย)");
    }
}
