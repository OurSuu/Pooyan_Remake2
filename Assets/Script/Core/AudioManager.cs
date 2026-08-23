using UnityEngine;

/// <summary>
/// Centralized audio playback for SE and BGM.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] private AudioClip bgmMain;
    [SerializeField] private AudioClip bgmBonus;
    [SerializeField] private AudioClip bgmStageClear;
    [SerializeField] private AudioClip bgmGameOver;

    [Header("SFX")]
    [SerializeField] private AudioClip seArrowFire;
    [SerializeField] private AudioClip seBalloonPop;
    [SerializeField] private AudioClip seWolfFall;
    [SerializeField] private AudioClip seMeatThrow;
    [SerializeField] private AudioClip seMeatHit;
    [SerializeField] private AudioClip seRockHit;
    [SerializeField] private AudioClip seRockDestroy;
    [SerializeField] private AudioClip seShieldBlock;
    [SerializeField] private AudioClip seLadderClimb;
    [SerializeField] private AudioClip seWolfBite;
    [SerializeField] private AudioClip seBoulderFall;

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        sfxSource.loop = false;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
            GameManager.Instance.OnStageClear += HandleStageClear;
        }
    }

    private void OnDestroy()
    {
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
        PlayBGM(bgmGameOver, false);
    }

    private void HandleStageClear()
    {
        PlayBGM(bgmStageClear, false);
    }

    public void PlayBGM(AudioClip clip, bool loop)
    {
        if (clip == null || bgmSource == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

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
    public void PlayExtraLife() => PlayOneShot(seBalloonPop); // TODO: assign proper 1UP jingle

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
