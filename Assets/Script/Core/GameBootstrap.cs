using UnityEngine;

/// <summary>
/// Entry point ของเกมนะ — เรียกจาก Scene หรือ Main Menu เพื่อเริ่มเกมเพลย์
/// อารมณ์เหมือนเป็นจุดสตาร์ทระบบ
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    // ให้มันเริ่มเกมอัตโนมัติมั้ย? ติ๊กเปิดปิดใน Inspector ได้เลย
    [SerializeField] private bool autoStart = true;

    private void Start()
    {
        // ถ้าไม่ได้เซ็ต autoStart ไว้ ก็ไม่ต้องทำอะไร จบปิ๊ง
        if (!autoStart) return;

        // รีเซ็ตคะแนนก่อนเริ่ม แล้วก็สั่ง GameManager ให้เริ่มเกมโลด!
        ScoreManager.Instance?.ResetScore();
        GameManager.Instance?.StartGame();
    }
}
