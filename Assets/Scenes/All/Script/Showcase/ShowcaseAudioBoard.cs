using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// หน้า Soundboard Studio สำหรับโชว์ระบบเสียงของ Pooyan Remake (Scene 04):
/// จัดวางแบบ 6 การ์ดแบ่งกลุ่มชัดเจน (3 คอลัมน์ × 2 แถว) พร้อมกรอบสีแยกหมวดหมู่, ปุ่มกดสไตล์ Studio,
/// ตัวแสดงสเปกตรัมเสียง 24 แถบแบบ Real-Time, Stereo Panning (-7.5..+7.5) และเสียงสังเคราะห์ C# DSP 44.1kHz
///   1. BGM
///   2. Cutscene
///   3. เสียงของพวกหมาป่า & ลูกโป่ง
///   4. เสียงของพวกหมู (แม่หมู & ลูกหมู)
///   5. เสียง Particles, สภาพอากาศ & พื้นผิว
///   6. เสียง UI & Procedural C# DSP (RetroSFXSynth)
/// </summary>
public class ShowcaseAudioBoard : MonoBehaviour
{
    [Header("Real Game Audio References")]
    public AudioManager audioManagerRef;
    public AudioClip mainMenuBgmOverride;

    [Header("Edit-Mode World Backdrop (จะถูกซ่อนอัตโนมัติใน Play Mode ไม่ให้ซ้อนทะลุ UI)")]
    public GameObject editModeBackdropRoot;
    public GameObject stageHeaderRoot;

    private AudioSource bgmPreviewSource;
    private AudioSource sfxPreviewSource;
    private AudioSource loopPreviewSource;

    private string nowPlayingTitle = "เลือกกดปุ่มเสียงจากการ์ดทั้ง 6 กลุ่มด้านล่างเพื่อทดสอบฟังสด";
    private string nowPlayingSourceTag = "AudioManager.prefab + RetroSFXSynth.cs — Ready";
    private string activeBgmKey = "";
    private string lastTriggeredKey = "";
    private float lastTriggeredTime = -10f;

    private float masterVolume = 0.85f;
    private float stereoPanX = 0f; // -7.5 .. +7.5 เหมือนระบบจริงใน AudioManager
    private bool isRopeLooping = false;
    private bool isRainLooping = false;
    private bool isFootstepLooping = false;
    private int selectedSurfaceIndex = 0; // 0=Grass, 1=Rainy Mud, 2=Winter Snow
    private int selectedRoleIndex = 0;    // 0=Wolf, 1=Mama, 2=Piglet1, 3=Piglet2
    private float footstepTimer = 0f;

    private readonly float[] spectrumRaw = new float[64];
    private readonly float[] spectrumBars = new float[24];
    private readonly Vector2[] cardScrolls = new Vector2[6];

    private struct ClipEntry
    {
        public string key;
        public string label;
        public string sourceTag;
        public AudioClip clip;
        public bool isLoopBgm;
    }

    private readonly List<ClipEntry> group1Bgm = new();
    private readonly List<ClipEntry> group2Cutscene = new();
    private readonly List<ClipEntry> group3Wolf = new();
    private readonly List<ClipEntry> group4Pig = new();
    private readonly List<ClipEntry> group5ParticleFx = new();
    private readonly List<ClipEntry> group6Ui = new();
    private readonly List<ClipEntry> group6Synth = new();

    // Custom Flat Studio UI Textures & Styles
    private bool stylesReady = false;
    private readonly List<Texture2D> createdTextures = new();
    private Texture2D texCanvasBg;
    private Texture2D texPadNormal;
    private Texture2D texPadHover;
    private Texture2D texPadActive;
    private Texture2D texPadPlaying;
    private Texture2D texStopNormal;
    private Texture2D texStopHover;
    private Texture2D texPillOff;
    private Texture2D texPillOn;
    private Texture2D texSubPanel;

    private GUIStyle styleDeckTitle;
    private GUIStyle styleDeckNowPlaying;
    private GUIStyle styleDeckSource;
    private GUIStyle styleDeckLabel;
    private GUIStyle styleCardHeader;
    private GUIStyle styleCardCount;
    private GUIStyle styleSubLabel;
    private GUIStyle styleSubPanel;
    private GUIStyle stylePad;
    private GUIStyle stylePadPlaying;
    private GUIStyle styleStopBtn;
    private GUIStyle stylePillOff;
    private GUIStyle stylePillOn;

    // 6 Group Accent Colors
    private static readonly Color ColBgm = new Color(0.24f, 0.54f, 0.96f, 1f);      // Royal Blue
    private static readonly Color ColCutscene = new Color(0.66f, 0.36f, 0.94f, 1f); // Violet
    private static readonly Color ColWolf = new Color(0.92f, 0.32f, 0.30f, 1f);     // Crimson Coral
    private static readonly Color ColPig = new Color(0.14f, 0.74f, 0.48f, 1f);      // Emerald
    private static readonly Color ColParticle = new Color(0.08f, 0.72f, 0.84f, 1f); // Cyan Teal
    private static readonly Color ColUi = new Color(0.96f, 0.65f, 0.14f, 1f);       // Amber Gold

    private void Start()
    {
        if (audioManagerRef == null)
        {
            audioManagerRef = FindFirstObjectByType<AudioManager>();
        }

        // ซ่อนการ์ดพื้นหลังของ Scene View ออกให้สนิทใน Play Mode เพื่อไม่ให้ซ้อนทะลุหน้า Soundboard
        if (editModeBackdropRoot != null) editModeBackdropRoot.SetActive(false);
        if (stageHeaderRoot != null) stageHeaderRoot.SetActive(false);

        bgmPreviewSource = gameObject.AddComponent<AudioSource>();
        bgmPreviewSource.loop = true;
        bgmPreviewSource.playOnAwake = false;

        sfxPreviewSource = gameObject.AddComponent<AudioSource>();
        sfxPreviewSource.loop = false;
        sfxPreviewSource.playOnAwake = false;

        loopPreviewSource = gameObject.AddComponent<AudioSource>();
        loopPreviewSource.loop = true;
        loopPreviewSource.playOnAwake = false;

        if (audioManagerRef != null)
        {
            audioManagerRef.StopBGM();
            audioManagerRef.StopCutsceneAudio();
            audioManagerRef.StopGroundRunAudio();
        }

        PopulateAllSixGroups();
    }

    private void OnDestroy()
    {
        foreach (var t in createdTextures)
        {
            if (t != null) Destroy(t);
        }
        createdTextures.Clear();
    }

    private void PopulateAllSixGroups()
    {
        group1Bgm.Clear();
        group2Cutscene.Clear();
        group3Wolf.Clear();
        group4Pig.Clear();
        group5ParticleFx.Clear();
        group6Ui.Clear();
        group6Synth.Clear();

        // 1. BGM
        AudioClip menuClip = GetPrivateClip("bgmMainMenu") ?? mainMenuBgmOverride;
        if (menuClip != null)
        {
            group1Bgm.Add(new ClipEntry { key = "bgmMainMenu", label = "MainMenu BGM", sourceTag = "AudioManager.bgmMainMenu", clip = menuClip, isLoopBgm = true });
        }
        AddRealClip(group1Bgm, "Autumn (Stage 1)", "bgmAutumn", isLoopBgm: true);
        AddRealClip(group1Bgm, "Spring (Stage 2)", "bgmSpring", isLoopBgm: true);
        AddRealClip(group1Bgm, "Winter (Stage 3)", "bgmWinter", isLoopBgm: true);
        AddRealClip(group1Bgm, "Rainy (Stage 4)", "bgmRainy", isLoopBgm: true);
        AddRealClip(group1Bgm, "Summer (Stage 5)", "bgmSummer", isLoopBgm: true);
        AddRealClip(group1Bgm, "Bonus Main", "bgmBonus", isLoopBgm: true);
        AddRealClip(group1Bgm, "Bonus Meat", "bgmBonusMeat", isLoopBgm: true);
        AddRealClip(group1Bgm, "Bonus Fruit", "bgmBonusFruit", isLoopBgm: true);
        AddRealClip(group1Bgm, "Stage Clear", "bgmStageClear", isLoopBgm: false);
        AddRealClip(group1Bgm, "Game Over", "bgmGameOver", isLoopBgm: false);

        // 2. Cutscene
        AddRealClip(group2Cutscene, "Stage 1 Kidnap", "bgmStageIntro", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Stage 2 Chase", "bgmStage2Intro", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Stage 4 Chase", "bgmStage4Intro", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Stage 2 Reunion", "bgmStage2Clear", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Stage 4 Reunion", "bgmStage4Clear", isLoopBgm: false);
        AddRealClip(group2Cutscene, "MainMenu Attract", "bgmMainMenuCutscene", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Default Cutscene", "bgmDefaultCutscene", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Balloon Release", "seMainMenuBalloonRelease", isLoopBgm: false);
        AddRealClip(group2Cutscene, "Balloon Dock UI", "seMainMenuBalloonDock", isLoopBgm: false);

        // 3. เสียงของพวกหมาป่า & ลูกโป่ง
        AddRealClip(group3Wolf, "Balloon Inflate", "seBalloonInflate");
        AddRealClip(group3Wolf, "Balloon Pop", "seBalloonPop");
        AddRealClip(group3Wolf, "Balloon Damage", "seBalloonDamage");
        AddRealClip(group3Wolf, "Wolf Fall", "seWolfFall");
        AddRealClip(group3Wolf, "Shield Block", "seShieldBlock");
        AddRealClip(group3Wolf, "Ladder Climb", "seLadderClimb");
        AddRealClip(group3Wolf, "Wolf Bite", "seWolfBite");
        AddRealClip(group3Wolf, "Rock Throw", "seRockThrow");
        AddRealClip(group3Wolf, "Rock Hit Basket", "seRockHit");
        AddRealClip(group3Wolf, "Rock Destroy", "seRockDestroy");
        AddRealClip(group3Wolf, "Boulder Push", "seBoulderPush");
        AddRealClip(group3Wolf, "Boulder Fall", "seBoulderFall");
        AddRealClip(group3Wolf, "Boulder Hit", "seBoulderHit");
        AddRealClip(group3Wolf, "Boss Fruit Drop", "seFruitDrop");
        AddRealClip(group3Wolf, "Boss Fruit Hit", "seFruitHit");

        // 4. เสียงของพวกหมู (แม่หมู & ลูกหมู)
        AddRealClip(group4Pig, "Arrow Fire", "seArrowFire");
        AddRealClip(group4Pig, "Meat Pickup", "seMeatPickup");
        AddRealClip(group4Pig, "Meat Throw", "seMeatThrow");
        AddRealClip(group4Pig, "Meat Hit Wolf", "seMeatHit");
        AddRealClip(group4Pig, "Meat Bounce", "seMeatBounce");
        AddRealClip(group4Pig, "Rope Pull (1-Shot)", "seRopePull");
        AddRealClip(group4Pig, "Mama Pig Fall", "sePlayerFall");
        AddRealClip(group4Pig, "Extra Life (1-UP)", "seExtraLife");

        // 5. เสียง Particles, สภาพอากาศ & ผลกระทบฉาก
        AddRealClip(group5ParticleFx, "Pop FX Burst", "seBalloonPop");
        AddRealClip(group5ParticleFx, "Rock Break FX", "seRockDestroy");
        AddRealClip(group5ParticleFx, "Armor Spark FX", "seRockHit");
        AddRealClip(group5ParticleFx, "Fruit Sparkle FX", "seFruitHit");
        AddRealClip(group5ParticleFx, "Boulder Impact", "seBoulderHit");
        AddRealClip(group5ParticleFx, "Ground Run Grass", "seGroundRun");
        AddRealClip(group5ParticleFx, "Ground Run Rainy", "seGroundRunRainy");
        AudioClip snowClip = GetPrivateClip("seGroundRunWinter") ?? RetroSFXSynth.GetSnowRun();
        if (snowClip != null)
        {
            group5ParticleFx.Add(new ClipEntry { key = "fx_snow", label = "Snow Crunch", sourceTag = "Surface: Winter Snow", clip = snowClip });
        }

        // 6. เสียง UI + Procedural C# DSP
        AddRealClip(group6Ui, "UI Button Click", "seButtonClick");
        group6Ui.Add(new ClipEntry { key = "ui_synth_click", label = "Synth UI Click", sourceTag = "RetroSFXSynth.GetButtonClick()", clip = RetroSFXSynth.GetButtonClick() });
        AudioClip extraLifeClip = GetPrivateClip("seExtraLife") ?? RetroSFXSynth.GetExtraLife();
        group6Ui.Add(new ClipEntry { key = "ui_1up", label = "1-UP Fanfare", sourceTag = "ScoreManager 1-UP", clip = extraLifeClip });
        AudioClip clearClip = GetPrivateClip("bgmStageClear");
        if (clearClip != null)
        {
            group6Ui.Add(new ClipEntry { key = "ui_clear", label = "Stage Clear SFX", sourceTag = "AudioManager.bgmStageClear", clip = clearClip });
        }

        AddSynthClip(group6Synth, "Meat Throw", RetroSFXSynth.GetMeatThrow());
        AddSynthClip(group6Synth, "Meat Hit", RetroSFXSynth.GetMeatHit());
        AddSynthClip(group6Synth, "Meat Bounce", RetroSFXSynth.GetMeatBounce());
        AddSynthClip(group6Synth, "Balloon Inflate", RetroSFXSynth.GetBalloonInflate());
        AddSynthClip(group6Synth, "Boulder Fall", RetroSFXSynth.GetBoulderFall());
        AddSynthClip(group6Synth, "Fruit Drop", RetroSFXSynth.GetFruitDrop());
        AddSynthClip(group6Synth, "Fruit Hit", RetroSFXSynth.GetFruitHit());
        AddSynthClip(group6Synth, "Ground Run", RetroSFXSynth.GetGroundRun());
        AddSynthClip(group6Synth, "Snow Crunch", RetroSFXSynth.GetSnowRun());
        AddSynthClip(group6Synth, "Rain Ambience", RetroSFXSynth.GetRainAmbience());
    }

    private void AddRealClip(List<ClipEntry> list, string label, string fieldName, bool isLoopBgm = false)
    {
        AudioClip clip = GetPrivateClip(fieldName);
        if (clip != null)
        {
            list.Add(new ClipEntry
            {
                key = $"{fieldName}_{label}",
                label = label,
                sourceTag = $"AudioManager.{fieldName}",
                clip = clip,
                isLoopBgm = isLoopBgm
            });
        }
    }

    private void AddSynthClip(List<ClipEntry> list, string label, AudioClip clip)
    {
        if (clip == null) return;
        list.Add(new ClipEntry
        {
            key = $"synth_{label}",
            label = "⚡ " + label,
            sourceTag = "RetroSFXSynth.cs (44.1kHz PCM C#)",
            clip = clip
        });
    }

    private AudioClip GetPrivateClip(string fieldName)
    {
        if (audioManagerRef == null) return null;
        FieldInfo fi = typeof(AudioManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return fi != null ? fi.GetValue(audioManagerRef) as AudioClip : null;
    }

    private void Update()
    {
        if (bgmPreviewSource != null) bgmPreviewSource.volume = masterVolume * 0.72f;
        if (loopPreviewSource != null) loopPreviewSource.volume = masterVolume * 0.65f;

        if (isFootstepLooping)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                footstepTimer = 0.21f;
                PlaySurfaceStepOnce();
            }
        }

        // อัปเดต 24-Bar Live Spectrum Visualizer
        AudioListener.GetSpectrumData(spectrumRaw, 0, FFTWindow.BlackmanHarris);
        for (int i = 0; i < spectrumBars.Length; i++)
        {
            int bin = Mathf.Min(spectrumRaw.Length - 1, i + 1);
            float target = Mathf.Clamp01(Mathf.Sqrt(spectrumRaw[bin]) * 4.2f);
            spectrumBars[i] = target > spectrumBars[i]
                ? Mathf.Lerp(spectrumBars[i], target, Time.deltaTime * 28f)
                : Mathf.Lerp(spectrumBars[i], target, Time.deltaTime * 8.5f);
        }
    }

    private void PlayBGMClip(ClipEntry e)
    {
        if (e.clip == null) return;
        bgmPreviewSource.Stop();
        bgmPreviewSource.clip = e.clip;
        bgmPreviewSource.loop = e.isLoopBgm;
        bgmPreviewSource.volume = masterVolume * 0.72f;
        bgmPreviewSource.Play();
        activeBgmKey = e.key;
        lastTriggeredKey = e.key;
        lastTriggeredTime = Time.unscaledTime;
        nowPlayingTitle = $"{e.label}   [{e.clip.name}  •  {e.clip.length:F1}s]";
        nowPlayingSourceTag = e.sourceTag + (e.isLoopBgm ? " (Loop)" : " (One-Shot Track)");
    }

    private void PlayOneShotClip(string key, string title, AudioClip clip, string tag, float pitch = 1f)
    {
        if (clip == null) return;
        sfxPreviewSource.panStereo = Mathf.Clamp(stereoPanX / 7.5f, -1f, 1f) * 0.85f;
        sfxPreviewSource.pitch = pitch;
        sfxPreviewSource.PlayOneShot(clip, masterVolume);
        lastTriggeredKey = key;
        lastTriggeredTime = Time.unscaledTime;
        nowPlayingTitle = $"{title}   [{clip.name}  •  {clip.length:F2}s]   |   Pan X = {stereoPanX:F1}";
        nowPlayingSourceTag = tag;
    }

    private void PlaySurfaceStepOnce()
    {
        AudioClip clip = selectedSurfaceIndex switch
        {
            1 => GetPrivateClip("seGroundRunRainy") ?? GetPrivateClip("seGroundRun"),
            2 => GetPrivateClip("seGroundRunWinter") ?? RetroSFXSynth.GetSnowRun(),
            _ => GetPrivateClip("seGroundRun") ?? RetroSFXSynth.GetGroundRun()
        };

        float[] rolePitches = { 0.94f, 1.02f, 1.18f, 1.29f };
        string[] surfaceNames = { "Grass", "Rainy Mud", "Winter Snow" };
        string[] roleNames = { "Wolf", "Mama", "Piglet 1", "Piglet 2" };
        float pitch = rolePitches[Mathf.Clamp(selectedRoleIndex, 0, rolePitches.Length - 1)] * Random.Range(0.96f, 1.04f);

        PlayOneShotClip("surface_step", $"Footstep [{surfaceNames[selectedSurfaceIndex]} / {roleNames[selectedRoleIndex]}]", clip, "AudioManager Multi-Voice Footstep Mixer", pitch);
    }

    private void ToggleRopeLoop()
    {
        isRopeLooping = !isRopeLooping;
        if (isRopeLooping)
        {
            isRainLooping = false;
            AudioClip rope = GetPrivateClip("seRopePull");
            if (rope != null)
            {
                loopPreviewSource.clip = rope;
                loopPreviewSource.Play();
                nowPlayingTitle = $"Rope Pull Loop   [{rope.name}]";
                nowPlayingSourceTag = "AudioManager.ropeLoopSource (Continuous Loop)";
            }
            else isRopeLooping = false;
        }
        else
        {
            loopPreviewSource.Stop();
        }
    }

    private void ToggleRainLoop()
    {
        isRainLooping = !isRainLooping;
        if (isRainLooping)
        {
            isRopeLooping = false;
            AudioClip rain = GetPrivateClip("seRainAmbience") ?? RetroSFXSynth.GetRainAmbience();
            if (rain != null)
            {
                loopPreviewSource.clip = rain;
                loopPreviewSource.Play();
                nowPlayingTitle = $"Rainy Ambience Loop   [{rain.name}]";
                nowPlayingSourceTag = "AudioManager RainAmbience + Procedural Synth";
            }
            else isRainLooping = false;
        }
        else
        {
            loopPreviewSource.Stop();
        }
    }

    private void StopAllAudio()
    {
        bgmPreviewSource?.Stop();
        sfxPreviewSource?.Stop();
        loopPreviewSource?.Stop();
        isRopeLooping = false;
        isRainLooping = false;
        isFootstepLooping = false;
        activeBgmKey = "";
        nowPlayingTitle = "หยุดเล่นเสียงทั้งหมดแล้ว — กดเลือกปุ่มจากการ์ดด้านล่างได้เลย";
        nowPlayingSourceTag = "Idle";
    }

    // =========================================================================================
    // MODERN STUDIO UI STYLING & 6-CARD LAYOUT
    // =========================================================================================
    private void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        texCanvasBg = MakeSolidTex(new Color(0.055f, 0.075f, 0.115f, 1f));
        texSubPanel = MakeBorderedTex(new Color(0.065f, 0.088f, 0.135f, 1f), new Color(0.16f, 0.21f, 0.30f, 1f));

        texPadNormal = MakeBorderedTex(new Color(0.135f, 0.175f, 0.255f, 1f), new Color(0.24f, 0.31f, 0.44f, 1f));
        texPadHover = MakeBorderedTex(new Color(0.20f, 0.26f, 0.38f, 1f), new Color(0.42f, 0.56f, 0.78f, 1f));
        texPadActive = MakeBorderedTex(new Color(0.24f, 0.34f, 0.50f, 1f), new Color(0.65f, 0.82f, 1f, 1f));
        texPadPlaying = MakeBorderedTex(new Color(0.15f, 0.42f, 0.62f, 1f), new Color(0.45f, 0.85f, 1f, 1f));

        texStopNormal = MakeBorderedTex(new Color(0.62f, 0.17f, 0.20f, 1f), new Color(0.88f, 0.36f, 0.38f, 1f));
        texStopHover = MakeBorderedTex(new Color(0.76f, 0.22f, 0.25f, 1f), new Color(1f, 0.52f, 0.52f, 1f));

        texPillOff = MakeBorderedTex(new Color(0.11f, 0.145f, 0.21f, 1f), new Color(0.22f, 0.28f, 0.38f, 1f));
        texPillOn = MakeBorderedTex(new Color(0.12f, 0.44f, 0.54f, 1f), new Color(0.35f, 0.85f, 0.96f, 1f));

        styleDeckTitle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.88f, 0.45f, 1f) }
        };
        styleDeckNowPlaying = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        styleDeckSource = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.66f, 0.78f, 0.92f, 1f) }
        };
        styleDeckLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.84f, 0.90f, 0.98f, 1f) }
        };
        styleCardHeader = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 6, 0, 0),
            normal = { textColor = Color.white }
        };
        styleCardCount = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            padding = new RectOffset(0, 10, 0, 0),
            normal = { textColor = new Color(0.82f, 0.90f, 1f, 0.85f) }
        };
        styleSubLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.70f, 0.82f, 0.94f, 1f) }
        };
        styleSubPanel = new GUIStyle
        {
            normal = { background = texSubPanel },
            border = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 2, 2)
        };

        stylePad = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 8, 4, 4),
            margin = new RectOffset(2, 2, 2, 2),
            border = new RectOffset(2, 2, 2, 2),
            clipping = TextClipping.Clip
        };
        stylePad.normal.background = texPadNormal;
        stylePad.normal.textColor = new Color(0.92f, 0.96f, 1f, 1f);
        stylePad.hover.background = texPadHover;
        stylePad.hover.textColor = Color.white;
        stylePad.active.background = texPadActive;
        stylePad.active.textColor = Color.white;

        stylePadPlaying = new GUIStyle(stylePad);
        stylePadPlaying.normal.background = texPadPlaying;
        stylePadPlaying.normal.textColor = Color.white;
        stylePadPlaying.hover.background = texPadPlaying;

        styleStopBtn = new GUIStyle(stylePad)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter
        };
        styleStopBtn.normal.background = texStopNormal;
        styleStopBtn.normal.textColor = Color.white;
        styleStopBtn.hover.background = texStopHover;
        styleStopBtn.hover.textColor = Color.white;
        styleStopBtn.active.background = texStopNormal;

        stylePillOff = new GUIStyle(stylePad)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };
        stylePillOff.normal.background = texPillOff;
        stylePillOff.normal.textColor = new Color(0.78f, 0.86f, 0.96f, 1f);
        stylePillOff.hover.background = texPadHover;

        stylePillOn = new GUIStyle(stylePillOff);
        stylePillOn.normal.background = texPillOn;
        stylePillOn.normal.textColor = Color.white;
        stylePillOn.hover.background = texPillOn;
    }

    private Texture2D MakeSolidTex(Color col)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { col, col, col, col });
        tex.Apply();
        createdTextures.Add(tex);
        return tex;
    }

    private Texture2D MakeBorderedTex(Color fill, Color border)
    {
        const int sz = 16;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color[] px = new Color[sz * sz];
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                bool isEdge = x == 0 || y == 0 || x == sz - 1 || y == sz - 1;
                px[y * sz + x] = isEdge ? border : fill;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        createdTextures.Add(tex);
        return tex;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        EnsureStyles();

        float sw = Screen.width;
        float sh = Screen.height;
        const float outerPad = 16f;
        const float deckH = 88f;
        const float gap = 12f;

        // พื้นหลัง Studio เข้มเต็มจอ (ไม่มีข้อความ Scene View ทะลุขึ้นมาทับ)
        GUI.DrawTexture(new Rect(0f, 0f, sw, sh), texCanvasBg);

        // =====================================================================
        // 1. TOP MASTER AUDIO STUDIO DECK
        // =====================================================================
        Rect deckRect = new Rect(outerPad, outerPad, sw - outerPad * 2f, deckH);
        DrawMasterDeck(deckRect);

        // =====================================================================
        // 2. 6-CARD STUDIO GRID (3 COLUMNS × 2 ROWS)
        // =====================================================================
        float gridTop = deckRect.yMax + gap;
        float gridW = sw - outerPad * 2f;
        float gridH = sh - gridTop - outerPad;
        float cardW = (gridW - gap * 2f) / 3f;
        float cardH = (gridH - gap) / 2f;

        Rect r1 = new Rect(outerPad, gridTop, cardW, cardH);
        Rect r2 = new Rect(outerPad + cardW + gap, gridTop, cardW, cardH);
        Rect r3 = new Rect(outerPad + (cardW + gap) * 2f, gridTop, cardW, cardH);

        Rect r4 = new Rect(outerPad, gridTop + cardH + gap, cardW, cardH);
        Rect r5 = new Rect(outerPad + cardW + gap, gridTop + cardH + gap, cardW, cardH);
        Rect r6 = new Rect(outerPad + (cardW + gap) * 2f, gridTop + cardH + gap, cardW, cardH);

        DrawClipCard(0, r1, "1. BGM — เพลงประกอบด่าน & โบนัส", $"{group1Bgm.Count} Tracks", ColBgm, group1Bgm, isBgmGroup: true, columns: 2);
        DrawClipCard(1, r2, "2. CUTSCENE — เพลง & เสียงคัทซีน", $"{group2Cutscene.Count} Clips", ColCutscene, group2Cutscene, isBgmGroup: true, columns: 2);
        DrawClipCard(2, r3, "3. เสียงของพวกหมาป่า & ลูกโป่ง", $"{group3Wolf.Count} SFX", ColWolf, group3Wolf, isBgmGroup: false, columns: 2);

        DrawPigCard(3, r4);
        DrawParticleSurfaceCard(4, r5);
        DrawUiAndSynthCard(5, r6);
    }

    private void DrawMasterDeck(Rect rect)
    {
        DrawFramedBox(rect, new Color(0.095f, 0.125f, 0.185f, 1f), new Color(0.22f, 0.30f, 0.44f, 1f), 1.5f);
        // แถบสีทองด้านบน Deck
        DrawColorRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(1f, 0.80f, 0.28f, 0.95f));

        float pad = 14f;
        float leftW = Mathf.Min(rect.width * 0.46f, 640f);

        // ซ้าย: ชื่อระบบ + Now Playing + Source
        bool isPlayingAny = (bgmPreviewSource != null && bgmPreviewSource.isPlaying)
            || (loopPreviewSource != null && loopPreviewSource.isPlaying)
            || isFootstepLooping
            || (Time.unscaledTime - lastTriggeredTime < 0.75f);

        DrawColorRect(new Rect(rect.x + pad, rect.y + 16f, 10f, 10f), isPlayingAny ? new Color(0.22f, 0.94f, 0.54f) : new Color(0.42f, 0.48f, 0.58f));
        GUI.Label(new Rect(rect.x + pad + 18f, rect.y + 10f, leftW, 22f), "POOYAN REMAKE — AUDIO ARCHITECTURE & SOUNDBOARD STUDIO", styleDeckTitle);
        GUI.Label(new Rect(rect.x + pad, rect.y + 36f, leftW + 40f, 22f), "NOW PLAYING: " + nowPlayingTitle, styleDeckNowPlaying);
        GUI.Label(new Rect(rect.x + pad, rect.y + 58f, leftW + 40f, 20f), "Source: " + nowPlayingSourceTag, styleDeckSource);

        // ขวา: Stop Button + Sliders + Live 24-Bar Equalizer
        float stopW = 108f;
        Rect stopRect = new Rect(rect.xMax - pad - stopW, rect.y + 14f, stopW, 34f);
        if (GUI.Button(stopRect, "■ STOP ALL", styleStopBtn))
        {
            StopAllAudio();
        }

        float slidersRight = stopRect.x - 14f;
        float slidersW = Mathf.Min(360f, Mathf.Max(220f, rect.width * 0.28f));
        float slidersX = slidersRight - slidersW;

        // Master Vol Slider
        GUI.Label(new Rect(slidersX, rect.y + 12f, 105f, 20f), $"Master Vol: {(int)(masterVolume * 100)}%", styleDeckLabel);
        masterVolume = GUI.HorizontalSlider(new Rect(slidersX + 108f, rect.y + 17f, slidersW - 108f, 16f), masterVolume, 0f, 1f);

        // Stereo Pan X Slider
        string panDir = stereoPanX < -0.5f ? "L" : (stereoPanX > 0.5f ? "R" : "C");
        GUI.Label(new Rect(slidersX, rect.y + 34f, 105f, 20f), $"Stereo Pan: {stereoPanX:F1} ({panDir})", styleDeckLabel);
        stereoPanX = GUI.HorizontalSlider(new Rect(slidersX + 108f, rect.y + 39f, slidersW - 108f, 16f), stereoPanX, -7.5f, 7.5f);

        // 24-Bar Live Equalizer Spectrum Meter
        Rect eqRect = new Rect(slidersX, rect.y + 58f, rect.xMax - pad - slidersX, 20f);
        DrawSpectrumVisualizer(eqRect);
    }

    private void DrawSpectrumVisualizer(Rect rect)
    {
        DrawColorRect(rect, new Color(0.05f, 0.07f, 0.10f, 1f));
        float barGap = 2f;
        float barW = (rect.width - 6f - barGap * (spectrumBars.Length - 1)) / spectrumBars.Length;
        for (int i = 0; i < spectrumBars.Length; i++)
        {
            float v = Mathf.Clamp(spectrumBars[i], 0.06f, 1f);
            float h = (rect.height - 4f) * v;
            float bx = rect.x + 3f + i * (barW + barGap);
            float by = rect.yMax - 2f - h;
            Color col = Color.Lerp(new Color(0.12f, 0.80f, 0.92f), new Color(1f, 0.52f, 0.22f), (float)i / spectrumBars.Length);
            DrawColorRect(new Rect(bx, by, barW, h), col);
        }
    }

    private Rect BeginStudioCardFrame(Rect rect, string title, string badgeText, Color accent)
    {
        // กรอบการ์ดคมชัดทุกความละเอียด + แถบสีด้านบน + หัวการ์ดตกแต่งสวยงาม
        Color borderCol = Color.Lerp(new Color(0.20f, 0.27f, 0.38f, 1f), accent, 0.38f);
        DrawFramedBox(rect, new Color(0.085f, 0.112f, 0.168f, 1f), borderCol, 1.5f);
        DrawColorRect(new Rect(rect.x, rect.y, rect.width, 4f), accent);
        DrawColorRect(new Rect(rect.x + 1.5f, rect.y + 4f, rect.width - 3f, 30f), new Color(accent.r * 0.24f, accent.g * 0.24f, accent.b * 0.24f, 0.96f));
        DrawColorRect(new Rect(rect.x + 1.5f, rect.y + 34f, rect.width - 3f, 1f), new Color(accent.r, accent.g, accent.b, 0.50f));

        GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 80f, 30f), title, styleCardHeader);
        GUI.Label(new Rect(rect.xMax - 82f, rect.y + 4f, 78f, 30f), badgeText, styleCardCount);

        return new Rect(rect.x + 10f, rect.y + 42f, rect.width - 20f, rect.height - 52f);
    }

    private void DrawClipCard(int scrollIdx, Rect rect, string title, string badge, Color accent, List<ClipEntry> entries, bool isBgmGroup, int columns)
    {
        Rect inner = BeginStudioCardFrame(rect, title, badge, accent);
        GUILayout.BeginArea(inner);
        cardScrolls[scrollIdx] = GUILayout.BeginScrollView(cardScrolls[scrollIdx], false, false);
        DrawPadGrid(entries, isBgmGroup, columns, accent, inner.width - 18f);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawPigCard(int scrollIdx, Rect rect)
    {
        Rect inner = BeginStudioCardFrame(rect, "4. เสียงของพวกหมู (แม่หมู & ลูกหมู)", $"{group4Pig.Count + 1} SFX", ColPig);
        GUILayout.BeginArea(inner);
        cardScrolls[scrollIdx] = GUILayout.BeginScrollView(cardScrolls[scrollIdx], false, false);

        DrawPadGrid(group4Pig, isBgmGroup: false, columns: 2, accent: ColPig, availWidth: inner.width - 18f);

        GUILayout.Space(6f);
        GUILayout.BeginVertical(styleSubPanel);
        GUILayout.Label("ElevatorRope Loop Channel (ลูกหมูดึงเชือกต่อเนื่อง):", styleSubLabel);
        GUILayout.Space(3f);
        GUIStyle ropeStyle = isRopeLooping ? stylePadPlaying : stylePad;
        if (GUILayout.Button(isRopeLooping ? "■ กำลังเล่นเสียงเชือก (กดหยุด)" : "▶ ทดสอบ Rope Pull Loop (ElevatorRope.cs)", ropeStyle, GUILayout.Height(30f)))
        {
            ToggleRopeLoop();
        }
        GUILayout.EndVertical();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawParticleSurfaceCard(int scrollIdx, Rect rect)
    {
        Rect inner = BeginStudioCardFrame(rect, "5. เสียง PARTICLES, สภาพอากาศ & พื้นผิว", "FX & Weather", ColParticle);
        GUILayout.BeginArea(inner);
        cardScrolls[scrollIdx] = GUILayout.BeginScrollView(cardScrolls[scrollIdx], false, false);

        // A. Weather Ambience Loop & Multi-Voice Surface Footstep Mixer
        GUILayout.BeginVertical(styleSubPanel);
        GUILayout.Label("Weather & Multi-Voice Surface Footstep Mixer:", styleSubLabel);
        GUILayout.Space(3f);

        GUILayout.BeginHorizontal();
        DrawPillButton("Grass", selectedSurfaceIndex == 0, () => selectedSurfaceIndex = 0);
        DrawPillButton("Rainy Mud", selectedSurfaceIndex == 1, () => selectedSurfaceIndex = 1);
        DrawPillButton("Winter Snow", selectedSurfaceIndex == 2, () => selectedSurfaceIndex = 2);
        GUILayout.EndHorizontal();

        GUILayout.Space(3f);
        GUILayout.BeginHorizontal();
        DrawPillButton("Wolf", selectedRoleIndex == 0, () => selectedRoleIndex = 0);
        DrawPillButton("Mama", selectedRoleIndex == 1, () => selectedRoleIndex = 1);
        DrawPillButton("Piglet 1", selectedRoleIndex == 2, () => selectedRoleIndex = 2);
        DrawPillButton("Piglet 2", selectedRoleIndex == 3, () => selectedRoleIndex = 3);
        GUILayout.EndHorizontal();

        GUILayout.Space(4f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(isFootstepLooping ? "■ Stop Run" : "▶ Footstep Loop", isFootstepLooping ? stylePadPlaying : stylePad, GUILayout.Height(28f)))
        {
            isFootstepLooping = !isFootstepLooping;
            if (isFootstepLooping) PlaySurfaceStepOnce();
        }
        if (GUILayout.Button("▶ 1 Step", stylePad, GUILayout.Width(78f), GUILayout.Height(28f)))
        {
            PlaySurfaceStepOnce();
        }
        if (GUILayout.Button(isRainLooping ? "■ Stop Rain" : "☂ Rain Loop", isRainLooping ? stylePadPlaying : stylePad, GUILayout.Height(28f)))
        {
            ToggleRainLoop();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.Space(6f);
        GUILayout.Label("Particle Burst & Impact SFX:", styleSubLabel);
        DrawPadGrid(group5ParticleFx, isBgmGroup: false, columns: 2, accent: ColParticle, availWidth: inner.width - 18f);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawUiAndSynthCard(int scrollIdx, Rect rect)
    {
        Rect inner = BeginStudioCardFrame(rect, "6. เสียง UI & PROCEDURAL C# DSP", $"{group6Ui.Count + group6Synth.Count} Sounds", ColUi);
        GUILayout.BeginArea(inner);
        cardScrolls[scrollIdx] = GUILayout.BeginScrollView(cardScrolls[scrollIdx], false, false);

        GUILayout.Label("UI & Menu Feedback:", styleSubLabel);
        DrawPadGrid(group6Ui, isBgmGroup: false, columns: 2, accent: ColUi, availWidth: inner.width - 18f);

        GUILayout.Space(6f);
        GUILayout.Label("RetroSFXSynth.cs — สังเคราะห์คลื่นเสียง 44.1kHz PCM จากโค้ด C# ล้วน:", styleSubLabel);
        DrawPadGrid(group6Synth, isBgmGroup: false, columns: 2, accent: ColUi, availWidth: inner.width - 18f);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawPillButton(string label, bool selected, System.Action onClick)
    {
        if (GUILayout.Button(label, selected ? stylePillOn : stylePillOff, GUILayout.Height(23f)))
        {
            onClick?.Invoke();
        }
    }

    private void DrawPadGrid(List<ClipEntry> entries, bool isBgmGroup, int columns, Color accent, float availWidth)
    {
        if (entries == null || entries.Count == 0)
        {
            GUILayout.Label("— ไม่มีคลิปเสียงในหมวดนี้ —", styleSubLabel);
            return;
        }

        float padW = Mathf.Max(90f, (availWidth - (columns - 1) * 6f) / columns);

        for (int i = 0; i < entries.Count; i += columns)
        {
            GUILayout.BeginHorizontal();
            for (int c = 0; c < columns; c++)
            {
                int idx = i + c;
                if (idx < entries.Count)
                {
                    var e = entries[idx];
                    bool isActiveBgm = isBgmGroup && bgmPreviewSource != null && bgmPreviewSource.isPlaying && activeBgmKey == e.key;
                    bool isRecentFlash = !isBgmGroup && lastTriggeredKey == e.key && (Time.unscaledTime - lastTriggeredTime < 0.35f);
                    GUIStyle btnStyle = (isActiveBgm || isRecentFlash) ? stylePadPlaying : stylePad;

                    string icon = isActiveBgm ? "🔊 " : (isBgmGroup ? "♫ " : (e.label.StartsWith("⚡") ? "" : "▶ "));
                    if (GUILayout.Button(icon + e.label, btnStyle, GUILayout.Width(padW), GUILayout.Height(30f)))
                    {
                        if (isBgmGroup)
                        {
                            PlayBGMClip(e);
                        }
                        else
                        {
                            PlayOneShotClip(e.key, e.label, e.clip, e.sourceTag);
                        }
                    }

                    // แถบสีหมวดเล็กๆ ด้านซ้ายปุ่มเพื่อความสวยงามสไตล์ Soundboard Studio
                    if (Event.current.type == EventType.Repaint)
                    {
                        Rect lastR = GUILayoutUtility.GetLastRect();
                        DrawColorRect(new Rect(lastR.x + 2f, lastR.y + 4f, 3f, lastR.height - 8f), isActiveBgm ? Color.white : accent);
                    }
                }
                else
                {
                    GUILayout.Space(padW);
                }

                if (c < columns - 1) GUILayout.Space(4f);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }
    }

    private static void DrawFramedBox(Rect rect, Color fill, Color border, float t)
    {
        DrawColorRect(rect, fill);
        DrawColorRect(new Rect(rect.x, rect.y, rect.width, t), border);
        DrawColorRect(new Rect(rect.x, rect.yMax - t, rect.width, t), border);
        DrawColorRect(new Rect(rect.x, rect.y, t, rect.height), border);
        DrawColorRect(new Rect(rect.xMax - t, rect.y, t, rect.height), border);
    }

    private static void DrawColorRect(Rect rect, Color col)
    {
        Color prev = GUI.color;
        GUI.color = col;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }
}
