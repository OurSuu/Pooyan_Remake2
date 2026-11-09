using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// ระบบสร้างบรรยากาศฝนตกสำหรับด่าน Rainy (Stage 4):
/// - RainDrops: เม็ดฝนเส้นเฉียงๆ ตกหนักต่อเนื่อง มี Collision 2D ชนกับพื้นและวัตถุในฉาก
/// - RainSplashes (Sub-Emitter): เป็น Sub-Emitter ของ RainDrops เมื่อเม็ดฝนชนสิ่งใด (Collision) จะแตกกระจายเป็นละอองน้ำตรงจุดที่ชนทันที
/// - ไม่มีหมอก (ลบ RainGroundMist ออกตามที่ผู้ใช้ต้องการ)
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class RainWeatherFX : MonoBehaviour
{
    private const int RainColliderLayer = 4; // Water Layer (แยกเฉพาะสำหรับรับการชนของเม็ดฝน ไม่กวนฟิสิกส์เกมเพลย์)

    [Header("1. Heavy Slanted Rain (เม็ดฝนเฉียงๆ ตกหนัก)")]
    [Tooltip("จำนวนเม็ดฝนที่ตกลงมาต่อวินาที (ยิ่งมากยิ่งตกหนัก)")]
    [Range(20f, 300f)]
    public float rainEmissionRate = 125f;

    [Tooltip("ความเร็วเม็ดฝนตกแนวดิ่ง")]
    [Range(8f, 35f)]
    public float fallSpeed = 20.5f;

    [Tooltip("ความแรงลมที่พัดเม็ดฝนให้ตกเฉียง (ค่าติดลบ = เฉียงไปทางซ้าย, ค่าบวก = เฉียงไปทางขวา)")]
    [Range(-12f, 12f)]
    public float windSpeedX = -4.6f;

    [Tooltip("ความโปร่งใสของเส้นเม็ดฝน (แนะนำ 0.20 - 0.32 เพื่อไม่ให้รกสายตาผู้เล่น)")]
    [Range(0.05f, 0.75f)]
    public float rainAlpha = 0.26f;

    [Tooltip("ความหนาของเส้นเม็ดฝน")]
    [Range(0.015f, 0.10f)]
    public float rainWidth = 0.042f;

    [Tooltip("ความยาวของเส้นเม็ดฝนตอนพุ่งลงมา")]
    [Range(1.0f, 5.0f)]
    public float rainStretchScale = 2.65f;

    [Header("2. Sub-Emitter Splash on Collision (เม็ดฝนชนสิ่งใดแล้วแตกกระจาย)")]
    [Tooltip("เลเยอร์ของ Collider2D ที่เม็ดฝนสามารถชนแล้วเกิด Splash ได้")]
    public LayerMask collisionLayers = ~0;

    [Tooltip("ระดับความสูงแกน Y ของพื้นล่างที่เม็ดฝนตกกระทบ")]
    public float groundY = -3.85f;

    [Tooltip("สร้าง Collider รับเม็ดฝนที่พื้นและหลังคากระเช้าผู้เล่นให้อัตโนมัติ")]
    public bool autoCreateRainColliders = true;

    [Tooltip("จำนวนเม็ดน้ำกระเซ็นขั้นต่ำต่อเม็ดฝน 1 หยดที่ชน")]
    [Range(1, 6)]
    public int splashDropsMin = 2;

    [Tooltip("จำนวนเม็ดน้ำกระเซ็นสูงสุดต่อเม็ดฝน 1 หยดที่ชน")]
    [Range(1, 8)]
    public int splashDropsMax = 4;

    [Tooltip("ขนาดของเม็ดน้ำกระเซ็นตอนชน")]
    [Range(0.02f, 0.15f)]
    public float splashSize = 0.055f;

    [Tooltip("ความชัดของเม็ดน้ำกระเซ็นตอนชน")]
    [Range(0.05f, 0.90f)]
    public float splashAlpha = 0.48f;

    [Header("Sorting & Colors")]
    [Tooltip("ลำดับ Sorting Order ของเอฟเฟกต์ฝน")]
    public int sortingOrder = 2;

    [Tooltip("สีโทนของเม็ดฝนและละอองน้ำ")]
    public Color rainTint = new Color(0.78f, 0.89f, 1.0f, 1.0f);

    private ParticleSystem rainDropsPS;
    private ParticleSystem rainSplashesPS;

    private static Texture2D cachedStreakTexture;
    private static Texture2D cachedSplashTexture;

    private static Material cachedStreakMaterial;
    private static Material cachedSplashMaterial;

    private void OnEnable()
    {
        SetupAndApplyRainSystems();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (splashDropsMax < splashDropsMin) splashDropsMax = splashDropsMin;
        SetupAndApplyRainSystems();
    }

    /// <summary>
    /// สร้างและอัปเดตค่าให้กับ RainDrops และ Sub-Emitter RainSplashes (พร้อมลบหมอกเก่าออก)
    /// </summary>
    public void SetupAndApplyRainSystems()
    {
        EnsureProceduralMaterials();
        ConfigureRainPhysicsLayerIgnore();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // ลบหมอก (RainGroundMist) ออกทั้งหมดตามที่ผู้ใช้ต้องการ
        RemoveObsoleteChild(transform, "RainGroundMist");

        // 1. สร้าง/ดึง RainDrops (Particle หลัก)
        rainDropsPS = GetOrCreateChildPS(transform, "RainDrops", new Vector3(1.8f, 6.6f, 0f));
        RemoveObsoleteChild(rainDropsPS.transform, "RainGroundMist");

        // หากมี RainSplashes ที่เคยอยู่นอก RainDrops ให้ย้ายเข้ามาเป็นลูกของ RainDrops เพื่อให้เป็น Sub-Emitter ที่ถูกต้องตามโครงสร้าง Unity
        Transform oldSiblingSplash = transform.Find("RainSplashes");
        if (oldSiblingSplash != null && oldSiblingSplash.parent != rainDropsPS.transform)
        {
            if (rainDropsPS.transform.Find("RainSplashes") == null)
            {
                oldSiblingSplash.SetParent(rainDropsPS.transform, false);
            }
            else
            {
                DestroyObjectSafe(oldSiblingSplash.gameObject);
            }
        }

        // 2. สร้าง/ดึง RainSplashes เป็นลูกของ RainDrops (Sub-Emitter)
        rainSplashesPS = GetOrCreateChildPS(rainDropsPS.transform, "RainSplashes", Vector3.zero);

        // 3. สร้าง Collider รับเม็ดฝนที่พื้นล่างและหลังคากระเช้าแม่หมู (อยู่บน Water Layer ที่ไม่กวนฟิสิกส์เกมเพลย์)
        EnsureRainSurfaceColliders();

        // 4. ตั้งค่า RainSplashes (Sub-Emitter) ก่อน แล้วผูกเข้ากับ SubEmittersModule ของ RainDrops
        ConfigureRainSplashesSubEmitter(rainSplashesPS);
        ConfigureRainDropsWithSubEmitter(rainDropsPS, rainSplashesPS);

        if (Application.isPlaying)
        {
            if (!rainDropsPS.isPlaying) rainDropsPS.Play(true);
        }
    }

    private static void ConfigureRainPhysicsLayerIgnore()
    {
        // ป้องกันไม่ให้ Collider สำหรับรับเม็ดฝน (Layer 4: Water) ไปชนหรือทริกเกอร์กับตัวละคร ลูกธนู หมาป่า หรือก้อนหินในเกมเพลย์
        for (int i = 0; i < 32; i++)
        {
            if (i != RainColliderLayer)
            {
                Physics2D.IgnoreLayerCollision(RainColliderLayer, i, true);
            }
        }
    }

    private void EnsureRainSurfaceColliders()
    {
        Transform groundColTr = transform.Find("RainGroundCollider");
        if (!autoCreateRainColliders)
        {
            if (groundColTr != null) DestroyObjectSafe(groundColTr.gameObject);
            return;
        }

        if (groundColTr == null)
        {
            GameObject groundGo = new GameObject("RainGroundCollider");
            groundGo.transform.SetParent(transform, false);
            groundColTr = groundGo.transform;
        }

        groundColTr.gameObject.layer = RainColliderLayer;
        groundColTr.localPosition = new Vector3(0f, groundY - 0.15f, 0f);
        groundColTr.localRotation = Quaternion.identity;
        groundColTr.localScale = Vector3.one;

        BoxCollider2D groundBox = groundColTr.GetComponent<BoxCollider2D>();
        if (groundBox == null)
        {
            groundBox = groundColTr.gameObject.AddComponent<BoxCollider2D>();
        }
        groundBox.isTrigger = false;
        groundBox.size = new Vector2(26f, 0.30f);

        // เพิ่ม Collider รับเม็ดฝนบนหลังคากระเช้าผู้เล่น (เพื่อให้เม็ดฝนตกลงบนหลังคากระเช้าแล้วแตก Splash สวยงาม)
        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            Transform roofColTr = player.transform.Find("RainRoofCollider");
            if (roofColTr == null)
            {
                GameObject roofGo = new GameObject("RainRoofCollider");
                roofGo.transform.SetParent(player.transform, false);
                roofColTr = roofGo.transform;
            }
            roofColTr.gameObject.layer = RainColliderLayer;
            roofColTr.localPosition = new Vector3(0f, 0.58f, 0f);
            roofColTr.localRotation = Quaternion.identity;
            roofColTr.localScale = Vector3.one;

            BoxCollider2D roofBox = roofColTr.GetComponent<BoxCollider2D>();
            if (roofBox == null)
            {
                roofBox = roofColTr.gameObject.AddComponent<BoxCollider2D>();
            }
            roofBox.isTrigger = false;
            roofBox.size = new Vector2(0.95f, 0.12f);
        }
    }

    private static void RemoveObsoleteChild(Transform parent, string childName)
    {
        if (parent == null) return;
        Transform child = parent.Find(childName);
        if (child != null)
        {
            DestroyObjectSafe(child.gameObject);
        }
    }

    private static void DestroyObjectSafe(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }

    private ParticleSystem GetOrCreateChildPS(Transform parent, string childName, Vector3 localPos)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }
        child.localPosition = localPos;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = child.gameObject.AddComponent<ParticleSystem>();
        }
        return ps;
    }

    // =========================================================================================
    // 1. เลเยอร์เม็ดฝนเฉียงๆ ตกหนัก + Collision 2D + Sub-Emitter
    // =========================================================================================
    private void ConfigureRainDropsWithSubEmitter(ParticleSystem ps, ParticleSystem splashSubEmitter)
    {
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 350;

        // ให้มีอายุมากพอที่จะพุ่งลงมาชน RainGroundCollider หรือวัตถุในฉากเสมอ
        float verticalDistance = Mathf.Max(9f, 6.6f - (groundY - 1.5f));
        float spdY = Mathf.Max(5f, fallSpeed);
        float baseLifetime = verticalDistance / spdY;
        main.startLifetime = new ParticleSystem.MinMaxCurve(baseLifetime * 1.05f, baseLifetime * 1.20f);
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(rainWidth * 0.78f, rainWidth * 1.22f);

        Color minCol = new Color(rainTint.r, rainTint.g, rainTint.b, Mathf.Clamp01(rainAlpha * 0.70f));
        Color maxCol = new Color(rainTint.r, rainTint.g, rainTint.b, Mathf.Clamp01(rainAlpha * 1.15f));
        main.startColor = new ParticleSystem.MinMaxGradient(minCol, maxCol);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rainEmissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(23.5f, 0.45f, 0.1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(windSpeedX * 0.92f, windSpeedX * 1.08f);
        vel.y = new ParticleSystem.MinMaxCurve(-fallSpeed * 1.06f, -fallSpeed * 0.94f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(1f, 0.95f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        // เปิดระบบ Collision 2D: เมื่อเม็ดฝนชน Collider2D ใดๆ เม็ดฝนจะหายไปทันที (lifetimeLoss = 1) และสั่งเล่น Sub-Emitter
        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision2D;
        collision.dampen = 0f;
        collision.bounce = 0f;
        collision.lifetimeLoss = 1.0f;
        collision.collidesWith = collisionLayers;
        collision.enableDynamicColliders = true;
        collision.quality = ParticleSystemCollisionQuality.High;
        collision.maxCollisionShapes = 256;
        collision.radiusScale = 0.25f;

        // ผูก RainSplashes เป็น Sub-Emitter ประเภท Collision (ชนสิ่งใดแล้ว Splash ตรงจุดนั้นทันที)
        var subEmitters = ps.subEmitters;
        subEmitters.enabled = true;
        while (subEmitters.subEmittersCount > 0)
        {
            subEmitters.RemoveSubEmitter(0);
        }
        if (splashSubEmitter != null)
        {
            subEmitters.AddSubEmitter(
                splashSubEmitter,
                ParticleSystemSubEmitterType.Collision,
                ParticleSystemSubEmitterProperties.InheritNothing,
                1.0f
            );
        }

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0.032f;
            renderer.lengthScale = rainStretchScale;
            renderer.sortingOrder = sortingOrder;
            if (cachedStreakMaterial != null)
            {
                renderer.sharedMaterial = cachedStreakMaterial;
            }
        }
    }

    // =========================================================================================
    // 2. เลเยอร์ Sub-Emitter: เม็ดฝนแตกกระจายตรงจุดที่ชน (Collision Burst Splash)
    // =========================================================================================
    private void ConfigureRainSplashesSubEmitter(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.35f, 2.75f);
        main.gravityModifier = 0.95f;
        main.startSize = new ParticleSystem.MinMaxCurve(splashSize * 0.75f, splashSize * 1.25f);

        Color minSplash = new Color(rainTint.r, rainTint.g, rainTint.b, Mathf.Clamp01(splashAlpha * 0.75f));
        Color maxSplash = new Color(1f, 1f, 1f, Mathf.Clamp01(splashAlpha * 1.10f));
        main.startColor = new ParticleSystem.MinMaxGradient(minSplash, maxSplash);

        // Sub-Emitter ใช้ Burst ตอนเกิดเหตุการณ์ชน (rateOverTime = 0)
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        short minCount = (short)Mathf.Clamp(splashDropsMin, 1, 10);
        short maxCount = (short)Mathf.Clamp(Mathf.Max(splashDropsMin, splashDropsMax), 1, 10);
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, minCount, maxCount)
        });

        // กระจายออกเป็นทรงกรวยหงายขึ้นจากจุดที่เม็ดฝนตกกระทบ แล้วโค้งตกลงตามแรงโน้มถ่วง
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 52f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        shape.scale = Vector3.one;

        var vel = ps.velocityOverLifetime;
        vel.enabled = false;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.80f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.cameraVelocityScale = 0f;
            renderer.velocityScale = 0.024f;
            renderer.lengthScale = 1.45f;
            renderer.sortingOrder = sortingOrder;
            if (cachedSplashMaterial != null)
            {
                renderer.sharedMaterial = cachedSplashMaterial;
            }
        }
    }

    // =========================================================================================
    // สร้าง Texture & Material แบบ Procedural ครั้งเดียวในหน่วยความจำ
    // =========================================================================================
    private static void EnsureProceduralMaterials()
    {
        if (cachedStreakTexture == null)
        {
            cachedStreakTexture = CreateRainStreakTexture(16, 64);
        }
        if (cachedSplashTexture == null)
        {
            cachedSplashTexture = CreateSplashDropTexture(32);
        }

        if (cachedStreakMaterial == null)
        {
            cachedStreakMaterial = CreateParticleMaterial("RainStreak_Mat", cachedStreakTexture);
        }
        if (cachedSplashMaterial == null)
        {
            cachedSplashMaterial = CreateParticleMaterial("RainSplash_Mat", cachedSplashTexture);
        }
    }

    private static Material CreateParticleMaterial(string matName, Texture2D tex)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        Material mat = new Material(shader)
        {
            name = matName,
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = tex
        };
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        return mat;
    }

    private static Texture2D CreateRainStreakTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "ProceduralRainStreak",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            float verticalProfile = Mathf.Sin(v * Mathf.PI);
            verticalProfile = Mathf.Pow(verticalProfile, 0.75f);

            for (int x = 0; x < width; x++)
            {
                float u = ((float)x / (width - 1)) * 2f - 1f;
                float horizontalProfile = Mathf.Clamp01(1f - Mathf.Abs(u));
                horizontalProfile = horizontalProfile * horizontalProfile;

                float alpha = horizontalProfile * verticalProfile;
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D CreateSplashDropTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ProceduralRainSplash",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float maxRadius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / maxRadius;
                float dy = (y - center) / maxRadius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    // =========================================================================================
    // สร้าง FallingRainFX อัตโนมัติเมื่ออยู่ในฉาก Rainy ทั้งใน Runtime และใน Editor
    // =========================================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsureOnSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInActiveSceneIfNeeded();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInActiveSceneIfNeeded();
    }

    public static void EnsureInActiveSceneIfNeeded()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.name)) return;
        if (activeScene.name.IndexOf("Rainy", StringComparison.OrdinalIgnoreCase) < 0) return;

        RainWeatherFX existing = FindFirstObjectByType<RainWeatherFX>();
        if (existing == null)
        {
            GameObject go = new GameObject("FallingRainFX");
            existing = go.AddComponent<RainWeatherFX>();
        }
        existing.SetupAndApplyRainSystems();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void InitEditorSync()
    {
        EditorApplication.delayCall += SyncOpenRainySceneInEditor;
        EditorSceneManager.sceneOpened += (scene, mode) => SyncOpenRainySceneInEditor();
    }

    private static void SyncOpenRainySceneInEditor()
    {
        if (Application.isPlaying) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrEmpty(activeScene.name)) return;
        if (activeScene.name.IndexOf("Rainy", StringComparison.OrdinalIgnoreCase) < 0) return;

        bool modified = false;

        // 1. ปิด Cutscene ในด่าน Rainy ทันที
        var cutscene = FindFirstObjectByType<StageCutsceneController>(FindObjectsInactive.Include);
        if (cutscene != null && cutscene.enableCutscenes)
        {
            cutscene.enableCutscenes = false;
            EditorUtility.SetDirty(cutscene);
            modified = true;
        }

        // 2. อัปเดต GameManager.currentStage ให้เป็น 4
        var gm = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm != null)
        {
            SerializedObject soGm = new SerializedObject(gm);
            SerializedProperty stageProp = soGm.FindProperty("currentStage");
            if (stageProp != null && stageProp.intValue != 4)
            {
                stageProp.intValue = 4;
                soGm.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gm);
                modified = true;
            }
        }

        // 2.1 อัปเดต LevelManager ให้ชี้ไปที่ Assets/Config/Rainy.asset
        var lm = FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
        var rainyConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Config/Rainy.asset");
        if (lm != null && rainyConfig != null)
        {
            SerializedObject soLm = new SerializedObject(lm);
            SerializedProperty defProp = soLm.FindProperty("defaultConfig");
            SerializedProperty overridesProp = soLm.FindProperty("stageOverrides");
            bool lmChanged = false;
            if (defProp != null && defProp.objectReferenceValue != rainyConfig)
            {
                defProp.objectReferenceValue = rainyConfig;
                lmChanged = true;
            }
            if (overridesProp != null && overridesProp.isArray && overridesProp.arraySize >= 1)
            {
                var elem0 = overridesProp.GetArrayElementAtIndex(0);
                if (elem0 != null && elem0.objectReferenceValue != rainyConfig)
                {
                    elem0.objectReferenceValue = rainyConfig;
                    lmChanged = true;
                }
            }
            if (lmChanged)
            {
                soLm.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(lm);
                modified = true;
            }
        }

        // 2.2 อัปเดต WolfSpawner ให้ชี้ไปที่ RainyWolf.prefab และ RainyBossWolf.prefab
        var spawner = FindFirstObjectByType<WolfSpawner>(FindObjectsInactive.Include);
        var rainyWolf = AssetDatabase.LoadAssetAtPath<Wolf>("Assets/Prefab/Rainy/RainyWolf.prefab");
        var rainyBoss = AssetDatabase.LoadAssetAtPath<BossWolf>("Assets/Prefab/Rainy/RainyBossWolf.prefab");
        if (spawner != null && (rainyWolf != null || rainyBoss != null))
        {
            SerializedObject soSpawner = new SerializedObject(spawner);
            SerializedProperty wolfProp = soSpawner.FindProperty("wolfPrefab");
            SerializedProperty bossProp = soSpawner.FindProperty("bossWolfPrefab");
            bool spawnerChanged = false;
            if (rainyWolf != null && wolfProp != null && wolfProp.objectReferenceValue != rainyWolf)
            {
                wolfProp.objectReferenceValue = rainyWolf;
                spawnerChanged = true;
            }
            if (rainyBoss != null && bossProp != null && bossProp.objectReferenceValue != rainyBoss)
            {
                bossProp.objectReferenceValue = rainyBoss;
                spawnerChanged = true;
            }
            if (spawnerChanged)
            {
                soSpawner.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(spawner);
                modified = true;
            }
        }

        // 3. สร้าง/อัปเดต FallingRainFX ในฉาก Rainy
        RainWeatherFX rainFX = FindFirstObjectByType<RainWeatherFX>(FindObjectsInactive.Include);
        if (rainFX == null)
        {
            GameObject go = new GameObject("FallingRainFX");
            rainFX = go.AddComponent<RainWeatherFX>();
            EditorUtility.SetDirty(go);
            modified = true;
        }
        rainFX.SetupAndApplyRainSystems();

        if (modified && !Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }
#endif
}
