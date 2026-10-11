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

        Destroy(gameObject, autoDestroyDelay);
    }
}
