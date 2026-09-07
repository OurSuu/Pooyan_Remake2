using UnityEngine;

/// <summary>
/// ลูกพี่ใหญ่จัดการเรื่องเสียงทั้งหมดในเกม ทั้งเพลงประกอบ (BGM) แล้วก็พวกเสียงเอฟเฟกต์ (SFX) ต่างๆ
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip bgmMain; // เพลงหน้าด่านปกติ
    [SerializeField] private AudioClip bgmBonus; // เพลงด่านโบนัส
    [SerializeField] private AudioClip bgmStageClear; // เสียงตอนเคลียร์ด่าน
    [SerializeField] private AudioClip bgmGameOver; // เสียงแพ้เกมโอเวอร์

    [Header("SFX")]
    [SerializeField] private AudioClip seArrowFire; // ยิงธนูฟิ้วๆ
    [SerializeField] private AudioClip seBalloonPop; // ลูกโป่งแตกปุ้ง
    [SerializeField] private AudioClip seWolfFall; // เสียงหมาป่าร่วง
    [SerializeField] private AudioClip seMeatThrow; // โยนเนื้อ
    [SerializeField] private AudioClip seMeatHit; // เนื้อโดนหัวหมา
    [SerializeField] private AudioClip seRockHit; // ก้อนหินกระทบ
    [SerializeField] private AudioClip seRockDestroy; // ก้อนหินแตกกระจาย
    [SerializeField] private AudioClip seShieldBlock; // หมาป่าเอาโล่บล็อค
    [SerializeField] private AudioClip seLadderClimb; // หมาปีนบันได
    [SerializeField] private AudioClip seWolfBite; // หมางับหมู
    [SerializeField] private AudioClip seBoulderFall; // หินถล่มใส่หน้าผา

    [SerializeField] private AudioSource bgmSource; // ลำโพงเพลง BGM
    [SerializeField] private AudioSource sfxSource; // ลำโพงเสียงเอฟเฟกต์

    private void Awake()
    {
        // จัดการ Singleton แบบง่ายๆ มีได้แค่ตัวเดียว
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ถ้าลืมแปะ AudioSource มาก็ใส่ให้มันเลย ออโต้!
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true; // เพลงให้มันวนเรื่อยๆ
        sfxSource.loop = false; // เสียงเอฟเฟกต์อย่าวนดิวะ 555
    }

    private void Start()
    {
        // ตามติดชีวิต GameManager ไว้ จะได้เปิดเพลงให้เข้ากับฉาก
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnStageClear += HandleStageClear;
        }
    }

    private void OnDestroy()
    {
        // ลบ Listener ทิ้งตอนโดนทำลายด้วย
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnStageClear -= HandleStageClear;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                PlayBGM(bgmMain, true);
                break;
            case GameState.BonusStage:
                PlayBGM(bgmBonus, true);
                break;
        }
    }

    private void HandleGameOver()
    {
        PlayBGM(bgmGameOver, false); // เพลงเกมโอเวอร์เล่นรอบเดียวพอแล้วเพื่อน
    }

    private void HandleStageClear()
    {
        PlayBGM(bgmStageClear, false); // ชนะด่านก็เล่นครั้งเดียวเหมือนกัน
    }

    public void PlayBGM(AudioClip clip, bool loop)
    {
        if (clip == null || bgmSource == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    // แค่เรียก PlayOneShot เอฟเฟกต์ก็น่าจะพอแล้ว
    public void PlaySFX(AudioClip clip) => PlayOneShot(clip);

    public void PlayArrowFire() => PlayOneShot(seArrowFire);
    public void PlayBalloonPop() => PlayOneShot(seBalloonPop);
    public void PlayWolfFall() => PlayOneShot(seWolfFall);
    public void PlayMeatThrow() => PlayOneShot(seMeatThrow);
    public void PlayMeatHit() => PlayOneShot(seMeatHit);
    public void PlayRockHit() => PlayOneShot(seRockHit);
    public void PlayRockDestroy() => PlayOneShot(seRockDestroy);
    public void PlayShieldBlock() => PlayOneShot(seShieldBlock);
    public void PlayLadderClimb() => PlayOneShot(seLadderClimb);
    public void PlayWolfBite() => PlayOneShot(seWolfBite);
    public void PlayBoulderFall() => PlayOneShot(seBoulderFall);
    public void PlayExtraLife() => PlayOneShot(seBalloonPop); // TODO: เดี๋ยวค่อยหาเสียงจิงเกิ้ลตอนได้ 1UP มาใส่นะ ขี้เกียจหา

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip); // สาดเสียงเอฟเฟกต์ไปเลยเพื่อน!
    }
}
