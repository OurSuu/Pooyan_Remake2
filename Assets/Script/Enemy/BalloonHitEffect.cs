using UnityEngine;

/// <summary>
/// ตัวควบคุม Particle Effect ตอนยิงโดนลูกโป่งแต่ยังไม่แตก (Balloon Hit / Damage FX)
/// กระจายสะเก็ดเศษลูกโป่ง (Rubber Flecks) ตามสีของลูกโป่งที่โดนยิงแบบไดนามิก (ฟ้า / เหลือง / แดง / ม่วงบอส / เขียว)
/// รองรับ Object Pool เพื่อความลื่นไหล ไม่สร้างขยะ GC
/// </summary>
public class BalloonHitEffect : MonoBehaviour
{
    [Header("Particle Systems")]
    [Tooltip("ระบบสะเก็ดเศษยางลูกโป่งที่จะเปลี่ยนสีตามลูกโป่ง")]
    [SerializeField] private ParticleSystem rubberFlecks;
    [Tooltip("ระบบประกายไฟ/แสงวิบวับตอนหัวลูกธนูกระแทกผิวลูกโป่ง")]
    [SerializeField] private ParticleSystem sparks;

    [Header("Settings")]
    [SerializeField] private float autoDestroyDelay = 0.45f;

    private static readonly System.Collections.Generic.Queue<BalloonHitEffect> pool = new();
    private static int prewarmedSceneHandle = -1;

    public static void PrewarmPool(GameObject prefab, int count = 6)
    {
        if (prefab == null) return;
        int currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (prewarmedSceneHandle == currentScene && pool.Count > 0) return;
        prewarmedSceneHandle = currentScene;

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, new Vector3(0f, -100f, 0f), Quaternion.identity);
            var fx = go.GetComponent<BalloonHitEffect>();
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

        BalloonHitEffect fx = null;
        while (pool.Count > 0 && fx == null)
        {
            fx = pool.Dequeue();
        }

        if (fx == null)
        {
            var go = Instantiate(prefab, position, Quaternion.identity);
            fx = go.GetComponent<BalloonHitEffect>();
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
        if (rubberFlecks == null)
        {
            var t = transform.Find("RubberFlecks");
            if (t != null) rubberFlecks = t.GetComponent<ParticleSystem>();
            else rubberFlecks = GetComponent<ParticleSystem>();
        }

        if (sparks == null)
        {
            var t = transform.Find("Sparks");
            if (t != null) sparks = t.GetComponent<ParticleSystem>();
        }
    }

    /// <summary>
    /// สั่งเล่นเอฟเฟกต์สะเก็ดลูกโป่ง พร้อมส่งสีของลูกโป่งเข้ามาให้เปลี่ยนสีแบบไดนามิก
    /// </summary>
    public void InitAndPlay(Color balloonColor)
    {
        // 1. ปรับสีสะเก็ดเศษยางลูกโป่งให้ตรงกับสีลูกโป่ง (มีเฉดเข้ม-อ่อนนิดๆ ให้ดูมีมิติ)
        if (rubberFlecks != null)
        {
            var main = rubberFlecks.main;
            Color darker = balloonColor * 0.75f;
            darker.a = 1f;
            Color lighter = Color.Lerp(balloonColor, Color.white, 0.25f);
            lighter.a = 1f;
            main.startColor = new ParticleSystem.MinMaxGradient(darker, lighter);
            rubberFlecks.Play();
        }

        // 2. ปรับสีประกายกระแทก (สว่าง ขาวปนสีลูกโป่งนิดๆ)
        if (sparks != null)
        {
            var main = sparks.main;
            Color sparkColor = Color.Lerp(balloonColor, Color.white, 0.7f);
            sparkColor.a = 1f;
            main.startColor = sparkColor;
            sparks.Play();
        }

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), autoDestroyDelay);
    }

    private void ReturnToPool()
    {
        if (rubberFlecks != null) rubberFlecks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        gameObject.SetActive(false);
        pool.Enqueue(this);
    }
}
