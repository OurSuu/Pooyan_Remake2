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
        if (rubberShards != null)
        {
            var main = rubberShards.main;
            main.startColor = balloonColor;
            rubberShards.Play();
        }

        if (airRing != null)
        {
            var main = airRing.main;
            Color ringColor = Color.Lerp(Color.white, balloonColor, 0.25f);
            ringColor.a = 0.85f;
            main.startColor = ringColor;
            airRing.Play();
        }

        if (sparkles != null)
        {
            sparkles.Play();
        }

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
