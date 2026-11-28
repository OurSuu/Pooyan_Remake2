using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// จัดการเอฟเฟกต์บรรยากาศแบบ Shader Graph สำหรับด่าน Autumn (Stage 1):
/// - SunbeamsLayer: แสงแดดอุ่นๆ เฉียงลงมาพร้อมละอองแสงระยิบระยับ (Additive Blending)
/// - WindStreaksLayer: เส้นสายลมอ่อนๆ พัดพริ้วผ่านอากาศอย่างต่อเนื่อง (Soft Alpha Blending)
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class AutumnAtmosphereFX : MonoBehaviour
{
    public const string SunbeamsLayerName = "SunbeamsLayer";
    public const string WindStreaksLayerName = "WindStreaksLayer";

    private const string SunbeamsMaterialPath = "Assets/Autumn/Material/Shadergraph/AutumnSunbeams.mat";
    private const string SunbeamsShaderPath = "Assets/Autumn/Material/Shadergraph/AutumnSunbeams.shadergraph";
    private const string WindStreaksMaterialPath = "Assets/Autumn/Material/Shadergraph/AutumnWindStreaks.mat";
    private const string WindStreaksShaderPath = "Assets/Autumn/Material/Shadergraph/AutumnWindStreaks.shadergraph";

    [Header("1. Warm Sunbeams (แสงแดดอุ่นๆ - Shader Graph)")]
    [Tooltip("เปิด/ปิด เลเยอร์แสงแดดอุ่นๆ")]
    public bool enableSunbeams = true;

    [Tooltip("Sorting Order ของแสงแดด (-2 = อยู่หน้าฉากหลัง/ต้นไม้ -3 แต่อยู่หลังพื้นและตัวละคร 0..2)")]
    public int sunbeamsSortingOrder = -2;

    [Tooltip("Material จาก AutumnSunbeams.shadergraph")]
    public Material sunbeamsMaterial;

    [Header("2. Flowing Wind Streaks (เส้นลมพัดพริ้ว - Shader Graph)")]
    [Tooltip("เปิด/ปิด เลเยอร์เส้นลมพัดพริ้ว")]
    public bool enableWindStreaks = false;

    [Tooltip("Sorting Order ของเส้นลม (-1 = อยู่หน้าแสงแดด -2 แต่อยู่หลังพื้นและตัวละคร 0..2)")]
    public int windStreaksSortingOrder = -1;

    [Tooltip("Material จาก AutumnWindStreaks.shadergraph")]
    public Material windStreaksMaterial;

    [Header("Overlay Transform (พื้นที่ครอบคลุมหน้าจอ)")]
    [Tooltip("ตำแหน่งจุดกึ่งกลางของแผ่น Overlay ใน World Space")]
    public Vector2 overlayCenter = new Vector2(0f, 0.25f);

    [Tooltip("ขนาดกว้าง x สูงของแผ่น Overlay (ครอบคลุมกล้อง Ortho 16:9 ขนาด 17.78 x 10 เผื่อขอบเล็กน้อย)")]
    public Vector2 overlaySize = new Vector2(20f, 10.6f);

    private static Mesh cachedQuadMesh;

#if UNITY_EDITOR
    private bool editorSyncQueued;
#endif

    private void OnEnable()
    {
        SyncLayers();
    }

    private void OnValidate()
    {
        overlaySize.x = Mathf.Max(1f, overlaySize.x);
        overlaySize.y = Mathf.Max(1f, overlaySize.y);

        if (!isActiveAndEnabled) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!editorSyncQueued)
            {
                editorSyncQueued = true;
                EditorApplication.delayCall += DelayedEditorValidate;
            }
            return;
        }
#endif
        SyncLayers();
    }

#if UNITY_EDITOR
    private void DelayedEditorValidate()
    {
        editorSyncQueued = false;
        if (this == null || !isActiveAndEnabled) return;
        SyncLayers();
    }
#endif

    /// <summary>
    /// สร้างและอัปเดตเลเยอร์ Quad สำหรับแสงอุ่นๆ และเส้นลมพัดพริ้ว
    /// (ไม่มี Update loop จึงไม่สร้างภาระ CPU/GC ต่อเฟรม)
    /// </summary>
    public bool SyncLayers()
    {
        bool changed = false;

        if (transform.localPosition != Vector3.zero)
        {
            transform.localPosition = Vector3.zero;
            changed = true;
        }
        if (transform.localRotation != Quaternion.identity)
        {
            transform.localRotation = Quaternion.identity;
            changed = true;
        }
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
            changed = true;
        }

#if UNITY_EDITOR
        if (enableSunbeams && EnsureEditorMaterial(ref sunbeamsMaterial, SunbeamsMaterialPath, SunbeamsShaderPath))
        {
            changed = true;
        }
        if (enableWindStreaks && EnsureEditorMaterial(ref windStreaksMaterial, WindStreaksMaterialPath, WindStreaksShaderPath))
        {
            changed = true;
        }
#endif

        if (SyncQuadLayer(SunbeamsLayerName, enableSunbeams, sunbeamsSortingOrder, sunbeamsMaterial))
        {
            changed = true;
        }
        if (SyncQuadLayer(WindStreaksLayerName, enableWindStreaks, windStreaksSortingOrder, windStreaksMaterial))
        {
            changed = true;
        }

        return changed;
    }

    private bool SyncQuadLayer(string layerName, bool enabledLayer, int sortingOrder, Material material)
    {
        if (!enabledLayer)
        {
            return RemoveLayerChildren(layerName);
        }

        bool changed = false;
        Transform child = FindOrCleanChild(layerName, out bool childCreatedOrCleaned);
        if (childCreatedOrCleaned) changed = true;

        GameObject go = child.gameObject;
        if (!go.activeSelf)
        {
            go.SetActive(true);
            changed = true;
        }

        Vector3 targetPos = new Vector3(overlayCenter.x, overlayCenter.y, 0f);
        if (child.localPosition != targetPos)
        {
            child.localPosition = targetPos;
            changed = true;
        }
        if (child.localRotation != Quaternion.identity)
        {
            child.localRotation = Quaternion.identity;
            changed = true;
        }

        Vector3 targetScale = new Vector3(overlaySize.x, overlaySize.y, 1f);
        if (child.localScale != targetScale)
        {
            child.localScale = targetScale;
            changed = true;
        }

        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = go.AddComponent<MeshFilter>();
            changed = true;
        }
        if (mf.sharedMesh == null)
        {
            mf.sharedMesh = GetQuadMesh();
            changed = true;
        }

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = go.AddComponent<MeshRenderer>();
            changed = true;
        }

        if (mr.shadowCastingMode != ShadowCastingMode.Off)
        {
            mr.shadowCastingMode = ShadowCastingMode.Off;
            changed = true;
        }
        if (mr.receiveShadows)
        {
            mr.receiveShadows = false;
            changed = true;
        }
        if (mr.lightProbeUsage != LightProbeUsage.Off)
        {
            mr.lightProbeUsage = LightProbeUsage.Off;
            changed = true;
        }
        if (mr.reflectionProbeUsage != ReflectionProbeUsage.Off)
        {
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            changed = true;
        }
        if (mr.sortingLayerID != 0)
        {
            mr.sortingLayerID = 0;
            changed = true;
        }
        if (mr.sortingOrder != sortingOrder)
        {
            mr.sortingOrder = sortingOrder;
            changed = true;
        }
        if (material != null && mr.sharedMaterial != material)
        {
            mr.sharedMaterial = material;
            changed = true;
        }

        // SortingGroup ทำให้ Quad ของ MeshRenderer จัดลำดับร่วมกับ SpriteRenderer (2D) ได้แม่นยำและเห็นค่าใน Inspector
        SortingGroup sg = go.GetComponent<SortingGroup>();
        if (sg == null)
        {
            sg = go.AddComponent<SortingGroup>();
            changed = true;
        }
        if (sg.sortingLayerID != 0)
        {
            sg.sortingLayerID = 0;
            changed = true;
        }
        if (sg.sortingOrder != sortingOrder)
        {
            sg.sortingOrder = sortingOrder;
            changed = true;
        }

        return changed;
    }

    private bool RemoveLayerChildren(string layerName)
    {
        bool removed = false;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (!string.Equals(c.name, layerName, StringComparison.Ordinal)) continue;
            DestroyObjectSafe(c.gameObject);
            removed = true;
        }
        return removed;
    }

    private Transform FindOrCleanChild(string layerName, out bool modified)
    {
        modified = false;
        Transform first = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (!string.Equals(c.name, layerName, StringComparison.Ordinal)) continue;

            if (first == null)
            {
                first = c;
            }
            else
            {
                DestroyObjectSafe(c.gameObject);
                modified = true;
            }
        }

        if (first != null) return first;

        GameObject go = new GameObject(layerName);
        go.transform.SetParent(transform, false);
        modified = true;
        return go.transform;
    }

    private static Mesh GetQuadMesh()
    {
        if (cachedQuadMesh != null) return cachedQuadMesh;

        GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        if (tempQuad != null)
        {
            MeshFilter tempFilter = tempQuad.GetComponent<MeshFilter>();
            if (tempFilter != null && tempFilter.sharedMesh != null)
            {
                cachedQuadMesh = tempFilter.sharedMesh;
            }
            DestroyObjectSafe(tempQuad);
        }

        if (cachedQuadMesh == null)
        {
            cachedQuadMesh = CreateFallbackQuadMesh();
        }

        return cachedQuadMesh;
    }

    private static Mesh CreateFallbackQuadMesh()
    {
        var mesh = new Mesh
        {
            name = "AutumnAtmosphereQuad"
        };

        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f)
        };

        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };

        mesh.normals = new[]
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back
        };

        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void DestroyObjectSafe(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

#if UNITY_EDITOR
    private bool EnsureEditorMaterial(ref Material matRef, string materialPath, string shaderGraphPath)
    {
        bool changed = false;

        if (matRef == null)
        {
            matRef = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (matRef != null)
            {
                if (!Application.isPlaying) EditorUtility.SetDirty(this);
                changed = true;
            }
        }

        if (matRef != null && (matRef.shader == null || matRef.shader.name == "Hidden/InternalErrorShader"))
        {
            Shader sgShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderGraphPath);
            if (sgShader != null)
            {
                matRef.shader = sgShader;
                if (!Application.isPlaying) EditorUtility.SetDirty(matRef);
                changed = true;
            }
        }

        return changed;
    }

    // ปิดการสร้าง AutumnAtmosphereFX อัตโนมัติใน Editor / Runtime แล้ว
    // หากต้องการใช้งาน ให้วาง Component นี้ใน Scene ด้วยตัวเองและติ๊กเปิดเลเยอร์ที่ต้องการใน Inspector
#endif
}
