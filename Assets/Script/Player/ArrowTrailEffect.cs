using UnityEngine;

/// <summary>
/// เอฟเฟกต์หางลมและอนุภาคความเร็วสูงตามหลังลูกธนู (Arrow Wind Trail & Speed FX)
/// ช่วยให้ลูกธนูดูพุ่งแรง มีน้ำหนัก และมีมิติตอนบินตัดอากาศ
/// </summary>
public class ArrowTrailEffect : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private ParticleSystem particleSys;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    private bool isDetached = false;

    private void Awake()
    {
        if (trailRenderer == null)
            trailRenderer = GetComponent<TrailRenderer>();
        if (particleSys == null)
            particleSys = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// ปลดเอฟเฟกต์ออกจากลูกธนู เพื่อให้หางลมและอนุภาคค่อยๆ สลายตัวไปตามธรรมชาติ ไม่หายไปดื้อๆ ตอนลูกธนูชนเป้าหมาย
    /// </summary>
    public void DetachAndFade()
    {
        if (isDetached) return;
        isDetached = true;

        transform.SetParent(null);

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        if (particleSys != null)
        {
            particleSys.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, fadeDuration);
    }
}
