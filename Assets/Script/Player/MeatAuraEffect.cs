using UnityEngine;

/// <summary>
/// เอฟเฟกต์ออร่าประกายดาวและหางดาวตกของก้อนเนื้อ (Meat Aura & Comet Trail FX)
/// เพิ่มความเด่น สะดุดตา และความอลังการให้กับอาวุธเนื้อไม้ตายประจำฤดูกาล
/// </summary>
public class MeatAuraEffect : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ParticleSystem sparkles;
    [SerializeField] private TrailRenderer cometTrail;

    [Header("Color Settings")]
    [SerializeField] private Color themeColor = new Color(1f, 0.78f, 0.28f, 1f);

    private void Awake()
    {
        if (sparkles == null) sparkles = GetComponent<ParticleSystem>();
        if (cometTrail == null) cometTrail = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// สั่งกระเพื่อมประกายดาวพิเศษตอนเนื้อชนลูกโป่งแล้วเด้งกระดอน (Bounce)
    /// </summary>
    public void OnBounce()
    {
        if (sparkles != null)
        {
            sparkles.Emit(12); // ปล่อยละอองดาวระเบิดกระจายช็อตหนึ่งตอนเด้งกระดอน
        }
    }
}
