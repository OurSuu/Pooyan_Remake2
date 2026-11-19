using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared pooled comic/cartoon VFX spawner backed by Phase 1 ShaderGraph materials
/// (BalloonPopRing, BalloonHitRing, ShieldParryRing, CartoonSparkleHDR, CartoonDustPuff).
/// Zero per-frame coroutine allocations and safe across scene reloads.
/// </summary>
public static class CartoonFXHelper
{
    private static readonly Queue<CartoonFXElement> pool = new();
    private static int poolSceneHandle = -1;

    private static Sprite cachedQuadSprite;
    private static Material cachedPopRingMaterial;
    private static Material cachedSparkleMaterial;
    private static Material cachedDustMaterial;
    private static Material cachedWindWispMaterial;

    private sealed class CartoonFXElement : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Vector3 startScale;
        private Vector3 endScale;
        private Color startColor;
        private Color endColor;
        private float duration;
        private float elapsed;
        private float spinSpeed;
        private Vector3 moveVelocity;
        private bool isFlashPop;

        public void CacheRenderer()
        {
            if (sr == null)
                sr = GetComponent<SpriteRenderer>();
        }

        public void Play(
            Sprite sprite,
            Material material,
            Vector3 position,
            float angleDeg,
            Vector3 startScale,
            Vector3 endScale,
            Color startColor,
            Color endColor,
            float duration,
            int sortingOrder,
            float spinSpeed,
            Vector3 moveVelocity,
            bool isFlashPop)
        {
            CacheRenderer();
            this.startScale = startScale;
            this.endScale = endScale;
            this.startColor = startColor;
            this.endColor = endColor;
            this.duration = Mathf.Max(0.02f, duration);
            this.elapsed = 0f;
            this.spinSpeed = spinSpeed;
            this.moveVelocity = moveVelocity;
            this.isFlashPop = isFlashPop;

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDeg));
            transform.localScale = startScale;

            if (sr != null)
            {
                sr.sprite = sprite;
                sr.sharedMaterial = material;
                sr.color = startColor;
                sr.sortingOrder = sortingOrder;
                sr.enabled = true;
            }

            gameObject.SetActive(true);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (moveVelocity.sqrMagnitude > 0.0001f)
            {
                float drag = 1f - 0.55f * t;
                transform.position += moveVelocity * (drag * Time.deltaTime);
            }

            if (Mathf.Abs(spinSpeed) > 0.01f)
            {
                transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            }

            if (isFlashPop)
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.LerpUnclamped(startScale, endScale, pulse);
                if (sr != null)
                {
                    Color col = Color.Lerp(startColor, endColor, t);
                    col.a = Mathf.Lerp(startColor.a, endColor.a, t * t);
                    sr.color = col;
                }
            }
            else
            {
                float ease = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.LerpUnclamped(startScale, endScale, ease);
                if (sr != null)
                {
                    sr.color = Color.Lerp(startColor, endColor, t);
                }
            }

            if (elapsed >= duration)
            {
                gameObject.SetActive(false);
                ReturnToPool(this);
            }
        }
    }

    private static Texture2D cachedQuadTexture;

    private static Sprite GetQuadSprite()
    {
        if (cachedQuadSprite != null) return cachedQuadSprite;

        // Soft circular alpha mask so even emergency/uncompiled draws never appear as a hard square
        const int size = 32;
        if (cachedQuadTexture == null)
        {
            cachedQuadTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float invRadius = 1f / center;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) * invRadius;
                    float dy = (y - center) * invRadius;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - Mathf.SmoothStep(0.62f, 0.98f, r);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            }
            cachedQuadTexture.SetPixels32(pixels);
            cachedQuadTexture.Apply(false, true);
        }

        cachedQuadSprite = Sprite.Create(
            cachedQuadTexture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect);
        cachedQuadSprite.hideFlags = HideFlags.HideAndDontSave;
        return cachedQuadSprite;
    }

    public static Material GetSharedRingMaterial(Material overrideMat = null)
    {
        if (overrideMat != null) return overrideMat;
        if (cachedPopRingMaterial != null) return cachedPopRingMaterial;

        var popPrefab = Resources.Load<GameObject>("FX/BalloonPopFX");
        if (popPrefab != null)
        {
            var airRing = popPrefab.transform.Find("AirRing");
            var psr = airRing != null ? airRing.GetComponent<ParticleSystemRenderer>() : null;
            if (psr != null && psr.sharedMaterial != null)
                cachedPopRingMaterial = psr.sharedMaterial;
        }

        if (cachedPopRingMaterial == null)
        {
            Shader s = Shader.Find("Shader Graphs/BalloonPopRing");
            if (s != null)
            {
                cachedPopRingMaterial = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            }
        }
        return cachedPopRingMaterial;
    }

    public static Material GetSharedSparkleMaterial(Material overrideMat = null)
    {
        if (overrideMat != null) return overrideMat;
        if (cachedSparkleMaterial != null) return cachedSparkleMaterial;

        var hitPrefab = Resources.Load<GameObject>("FX/BalloonHitFX");
        if (hitPrefab != null)
        {
            var sparks = hitPrefab.transform.Find("Sparks");
            var psr = sparks != null ? sparks.GetComponent<ParticleSystemRenderer>() : null;
            if (psr != null && psr.sharedMaterial != null)
                cachedSparkleMaterial = psr.sharedMaterial;
        }

        if (cachedSparkleMaterial == null)
        {
            Shader s = Shader.Find("Shader Graphs/CartoonSparkleHDR");
            if (s != null)
            {
                cachedSparkleMaterial = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            }
        }
        return cachedSparkleMaterial;
    }

    public static Material GetSharedDustMaterial(Material overrideMat = null)
    {
        if (overrideMat != null) return overrideMat;
        if (cachedDustMaterial != null) return cachedDustMaterial;

        var dustPrefab = Resources.Load<GameObject>("FX/GroundDustImpactFX");
        if (dustPrefab != null)
        {
            var psr = dustPrefab.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sharedMaterial != null)
                cachedDustMaterial = psr.sharedMaterial;
        }
        return cachedDustMaterial;
    }

    public static Material GetSharedWindWispMaterial(Material overrideMat = null)
    {
        if (overrideMat != null) return overrideMat;
        if (cachedWindWispMaterial != null) return cachedWindWispMaterial;

        var bitePrefab = Resources.Load<GameObject>("FX/WolfBiteArrowFX");
        if (bitePrefab != null)
        {
            var psr = bitePrefab.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sharedMaterial != null)
                cachedWindWispMaterial = psr.sharedMaterial;
        }

        if (cachedWindWispMaterial == null)
        {
            var windPrefab = Resources.Load<GameObject>("FX/Arrow/ArrowWind_Autumn");
            if (windPrefab != null)
            {
                var psr = windPrefab.GetComponent<ParticleSystemRenderer>();
                if (psr != null && psr.sharedMaterial != null)
                    cachedWindWispMaterial = psr.sharedMaterial;
            }
        }

        if (cachedWindWispMaterial == null)
        {
            Shader s = Shader.Find("Shader Graphs/CartoonWindWisp");
            if (s != null)
            {
                cachedWindWispMaterial = new Material(s) { hideFlags = HideFlags.HideAndDontSave };
            }
        }
        return cachedWindWispMaterial;
    }

    private static void SyncScenePool()
    {
        int currentScene = SceneManager.GetActiveScene().handle;
        if (poolSceneHandle != currentScene)
        {
            poolSceneHandle = currentScene;
            pool.Clear();
        }
    }

    private static CartoonFXElement CreateElement()
    {
        var go = new GameObject("CartoonFXElement");
        go.AddComponent<SpriteRenderer>();
        var created = go.AddComponent<CartoonFXElement>();
        created.CacheRenderer();
        return created;
    }

    private static CartoonFXElement RentElement()
    {
        SyncScenePool();
        while (pool.Count > 0)
        {
            var elem = pool.Dequeue();
            if (elem != null) return elem;
        }

        return CreateElement();
    }

    private static int warmupSceneHandle = -1;

    public static void Prewarm(int count = 12)
    {
        SyncScenePool();
        int needed = count - pool.Count;
        for (int i = 0; i < needed; i++)
        {
            var elem = CreateElement();
            elem.gameObject.SetActive(false);
            pool.Enqueue(elem);
        }
    }

    /// <summary>
    /// Forces active ShaderGraph materials to compile during scene startup before the player fires Shot #1,
    /// avoiding Unity Editor's cyan async-shader-compilation placeholder quad.
    /// </summary>
    public static void WarmupShaders(params Material[] extraMaterials)
    {
        SyncScenePool();
        int activeHandle = SceneManager.GetActiveScene().handle;
        if (warmupSceneHandle == activeHandle) return;
        warmupSceneHandle = activeHandle;

        Shader.WarmupAllShaders();

        var unique = new HashSet<Material>();
        Material ring = GetSharedRingMaterial();
        Material sparkle = GetSharedSparkleMaterial();
        Material dust = GetSharedDustMaterial();
        Material windWisp = GetSharedWindWispMaterial();
        if (ring != null) unique.Add(ring);
        if (sparkle != null) unique.Add(sparkle);
        if (dust != null) unique.Add(dust);
        if (windWisp != null) unique.Add(windWisp);

        if (extraMaterials != null)
        {
            for (int i = 0; i < extraMaterials.Length; i++)
            {
                if (extraMaterials[i] != null) unique.Add(extraMaterials[i]);
            }
        }

        Camera cam = Camera.main;
        Vector3 pos = cam != null ? new Vector3(cam.transform.position.x, cam.transform.position.y, 0f) : Vector3.zero;
        Sprite quad = GetQuadSprite();

        foreach (Material mat in unique)
        {
            RentElement().Play(
                quad,
                mat,
                pos,
                0f,
                new Vector3(0.0002f, 0.0002f, 1f),
                new Vector3(0.0002f, 0.0002f, 1f),
                new Color(1f, 1f, 1f, 0.004f),
                new Color(1f, 1f, 1f, 0f),
                duration: 0.25f,
                sortingOrder: -100,
                spinSpeed: 0f,
                moveVelocity: Vector3.zero,
                isFlashPop: false);
        }
    }

    private static void ReturnToPool(CartoonFXElement elem)
    {
        if (elem == null) return;
        if (SceneManager.GetActiveScene().handle != poolSceneHandle) return;
        pool.Enqueue(elem);
    }

    public static void SpawnRing(
        Vector3 position,
        Color color,
        Vector3 startScale,
        Vector3 endScale,
        float duration = 0.22f,
        int sortingOrder = 14,
        Material overrideMat = null)
    {
        Material mat = GetSharedRingMaterial(overrideMat);
        if (mat == null) return;

        Color endCol = color;
        endCol.a = 0f;

        RentElement().Play(
            GetQuadSprite(),
            mat,
            position,
            Random.Range(-15f, 15f),
            startScale,
            endScale,
            color,
            endCol,
            duration,
            sortingOrder,
            0f,
            Vector3.zero,
            isFlashPop: false);
    }

    public static void SpawnFlashStar(
        Vector3 position,
        Color color,
        float startScale,
        float peakScale,
        float duration = 0.16f,
        float angleDeg = 0f,
        float spinSpeed = 120f,
        int sortingOrder = 15,
        Material overrideMat = null)
    {
        Material mat = GetSharedSparkleMaterial(overrideMat);
        if (mat == null) return;

        Color endCol = color;
        endCol.a = 0f;

        RentElement().Play(
            GetQuadSprite(),
            mat,
            position,
            angleDeg,
            Vector3.one * startScale,
            Vector3.one * peakScale,
            color,
            endCol,
            duration,
            sortingOrder,
            spinSpeed,
            Vector3.zero,
            isFlashPop: true);
    }

    public static void SpawnStreaks(
        Vector3 position,
        Color color,
        int count,
        float minSpeed,
        float maxSpeed,
        float width,
        float length,
        float duration = 0.16f,
        int sortingOrder = 15,
        Material overrideMat = null,
        float baseAngleDeg = 0f,
        float spreadDeg = 360f)
    {
        Material mat = GetSharedSparkleMaterial(overrideMat);
        if (mat == null || count <= 0) return;

        Sprite quad = GetQuadSprite();
        Color endCol = color;
        endCol.a = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / count : 0.5f;
            float jitter = Random.Range(-0.5f, 0.5f) * (spreadDeg / Mathf.Max(1, count));
            float deg = spreadDeg >= 359f
                ? baseAngleDeg + t * 360f + jitter
                : baseAngleDeg + Random.Range(-spreadDeg * 0.5f, spreadDeg * 0.5f);
            float rad = deg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            float spd = Random.Range(minSpeed, maxSpeed);

            RentElement().Play(
                quad,
                mat,
                position + dir * 0.08f,
                deg,
                new Vector3(length, width, 1f),
                new Vector3(length * 0.35f, width * 0.25f, 1f),
                color,
                endCol,
                duration,
                sortingOrder,
                0f,
                dir * spd,
                isFlashPop: false);
        }
    }

    public static void SpawnShieldParry(Vector3 position, Material parryRingMat = null, Material sparkleMat = null)
    {
        Color parryGold = new Color(1f, 0.9f, 0.38f, 1f);
        Color flashWhite = new Color(1f, 0.98f, 0.88f, 1f);

        SpawnRing(
            position,
            parryGold,
            new Vector3(0.28f, 0.28f, 1f),
            new Vector3(1.55f, 1.55f, 1f),
            duration: 0.19f,
            sortingOrder: 16,
            overrideMat: parryRingMat);

        SpawnFlashStar(position, flashWhite, 0.25f, 1.25f, 0.14f, angleDeg: 18f, spinSpeed: 180f, sortingOrder: 17, overrideMat: sparkleMat);
        SpawnFlashStar(position, parryGold, 0.2f, 1.0f, 0.16f, angleDeg: 63f, spinSpeed: -140f, sortingOrder: 16, overrideMat: sparkleMat);
        SpawnStreaks(position, parryGold, count: 6, minSpeed: 4.5f, maxSpeed: 8.5f, width: 0.18f, length: 0.65f, duration: 0.18f, sortingOrder: 17, overrideMat: sparkleMat);
    }

    public static void SpawnMuzzleGust(Vector3 position, float shootDirection, Color? tintColor = null)
    {
        Color baseTint = tintColor ?? new Color(0.88f, 0.78f, 0.52f, 0.72f);
        // Clamp luminance below post-processing Bloom threshold so shots fit 2D painted cartoon style cleanly
        float maxCh = Mathf.Max(baseTint.r, Mathf.Max(baseTint.g, baseTint.b));
        if (maxCh > 0.86f)
        {
            float scale = 0.86f / maxCh;
            baseTint = new Color(baseTint.r * scale, baseTint.g * scale, baseTint.b * scale, baseTint.a);
        }

        float sign = shootDirection >= 0f ? 1f : -1f;
        Material puffMat = GetSharedWindWispMaterial() ?? GetSharedDustMaterial();
        if (puffMat != null)
        {
            Color puffCol = Color.Lerp(baseTint, new Color(0.9f, 0.88f, 0.84f, 1f), 0.25f);
            puffCol.a = 0.48f;
            Color endCol = puffCol;
            endCol.a = 0f;

            RentElement().Play(
                GetQuadSprite(),
                puffMat,
                position + new Vector3(sign * 0.14f, 0f, 0f),
                0f,
                new Vector3(0.16f, 0.26f, 1f),
                new Vector3(0.36f, 0.56f, 1f),
                puffCol,
                endCol,
                duration: 0.11f,
                sortingOrder: 14,
                spinSpeed: 0f,
                moveVelocity: new Vector3(sign * 2.4f, 0f, 0f),
                isFlashPop: false);
        }

        Material wispMat = GetSharedWindWispMaterial();
        if (wispMat != null)
        {
            Color streakCol = baseTint;
            streakCol.a = 0.55f;
            float baseDeg = shootDirection >= 0f ? 0f : 180f;
            SpawnStreaks(
                position,
                streakCol,
                count: 2,
                minSpeed: 3.2f,
                maxSpeed: 5.0f,
                width: 0.08f,
                length: 0.30f,
                duration: 0.10f,
                sortingOrder: 14,
                overrideMat: wispMat,
                baseAngleDeg: baseDeg,
                spreadDeg: 44f);
        }
    }

    public static void SpawnGroundShockwave(Vector3 position, Color dustTint)
    {
        Color brightRing = Color.Lerp(dustTint, Color.white, 0.35f);
        brightRing.a = 0.95f;

        SpawnRing(
            position,
            brightRing,
            new Vector3(0.45f, 0.15f, 1f),
            new Vector3(2.85f, 0.78f, 1f),
            duration: 0.28f,
            sortingOrder: 6);

        Color innerRing = Color.Lerp(dustTint, Color.white, 0.65f);
        innerRing.a = 0.85f;

        SpawnRing(
            position + new Vector3(0f, 0.08f, 0f),
            innerRing,
            new Vector3(0.3f, 0.1f, 1f),
            new Vector3(1.85f, 0.48f, 1f),
            duration: 0.2f,
            sortingOrder: 7);
    }
}
