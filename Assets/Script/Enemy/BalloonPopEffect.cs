using UnityEngine;

/// <summary>
/// ตัวควบคุม Particle Effect ตอนลูกโป่งแตก (Balloon Pop Particle FX)
/// รองรับการเปลี่ยนสีเศษยางตามสีของลูกโป่งที่แตกแบบไดนามิก (ชมพู / เหลือง / แดง / บอส)
/// </summary>
public class BalloonPopEffect : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem rubberShards;
    [SerializeField] private ParticleSystem airRing;
    [SerializeField] private ParticleSystem sparkles;

    [Header("Settings")]
    [SerializeField] private float autoDestroyDelay = 0.8f;

    private static readonly System.Collections.Generic.Queue<BalloonPopEffect> pool = new();
    private static int prewarmedSceneHandle = -1;

    public static void PrewarmPool(GameObject prefab, int count = 4)
    {
        if (prefab == null) return;
        CartoonFXHelper.Prewarm(16);
        int currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (prewarmedSceneHandle == currentScene && pool.Count > 0) return;
        prewarmedSceneHandle = currentScene;

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, new Vector3(0f, -100f, 0f), Quaternion.identity);
            var fx = go.GetComponent<BalloonPopEffect>();
            if (fx != null)
            {
                go.SetActive(false);
                pool.Enqueue(fx);
            }
        }
    }

    public static void SpawnPooled(GameObject prefab, Vector3 position, Color balloonColor)
    {
        if (prefab == null) return;

        BalloonPopEffect fx = null;
        while (pool.Count > 0 && fx == null)
        {
            fx = pool.Dequeue();
        }

        if (fx == null)
        {
            var go = Instantiate(prefab, position, Quaternion.identity);
            fx = go.GetComponent<BalloonPopEffect>();
        }
        else
        {
            fx.transform.position = position;
            fx.gameObject.SetActive(true);
        }

        if (fx != null)
        {
            fx.InitAndPlay(balloonColor);
        }
    }

    private void Awake()
    {
        if (rubberShards == null)
        {
            var t = transform.Find("RubberShards");
            if (t != null) rubberShards = t.GetComponent<ParticleSystem>();
            else rubberShards = GetComponent<ParticleSystem>();
        }

        if (airRing == null)
        {
            var t = transform.Find("AirRing");
            if (t != null) airRing = t.GetComponent<ParticleSystem>();
        }

        if (sparkles == null)
        {
            var t = transform.Find("Sparkles");
            if (t != null) sparkles = t.GetComponent<ParticleSystem>();
        }
    }

    /// <summary>
    /// สั่งเล่นเอฟเฟกต์ลูกโป่งแตก พร้อมส่งสีของลูกโป่งเข้ามาให้เศษยางสีตรงกัน
    /// </summary>
    public void InitAndPlay(Color balloonColor)
    {
        Color vibrantColor = balloonColor;
        vibrantColor.a = 1f;

        if (rubberShards != null)
        {
            var main = rubberShards.main;
            main.startColor = vibrantColor;
            rubberShards.Play();
        }

        Material ringMat = null;
        if (airRing != null)
        {
            var main = airRing.main;
            Color ringColor = Color.Lerp(Color.white, vibrantColor, 0.35f);
            ringColor.a = 1f;
            main.startColor = ringColor;
            var psr = airRing.GetComponent<ParticleSystemRenderer>();
            if (psr != null) ringMat = psr.sharedMaterial;
            airRing.Play();
        }

        Material sparkleMat = null;
        if (sparkles != null)
        {
            var main = sparkles.main;
            Color sparkColor = Color.Lerp(vibrantColor, Color.white, 0.55f);
            sparkColor.a = 1f;
            main.startColor = sparkColor;
            var psr = sparkles.GetComponent<ParticleSystemRenderer>();
            if (psr != null) sparkleMat = psr.sharedMaterial;
            sparkles.Play();
        }

        // ประกายดาวการ์ตูนตรงกลาง + วงแหวนสะท้อนชั้นนอกให้เห็นชัดทุกฉาก
        CartoonFXHelper.SpawnFlashStar(
            transform.position,
            Color.Lerp(vibrantColor, Color.white, 0.72f),
            0.35f,
            1.95f,
            duration: 0.16f,
            angleDeg: 22f,
            spinSpeed: 150f,
            sortingOrder: 14,
            overrideMat: sparkleMat);

        CartoonFXHelper.SpawnRing(
            transform.position,
            Color.Lerp(vibrantColor, Color.white, 0.2f),
            new Vector3(0.45f, 0.45f, 1f),
            new Vector3(2.45f, 2.45f, 1f),
            duration: 0.32f,
            sortingOrder: 12,
            overrideMat: ringMat);

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), autoDestroyDelay);
    }

    private void ReturnToPool()
    {
        if (rubberShards != null) rubberShards.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (airRing != null) airRing.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparkles != null) sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
        pool.Enqueue(this);
    }
}
