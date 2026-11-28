using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ตัวควบคุม Showcase Scene 05 (GameManager, All 5 Real Cutscenes, Stage Switching & Bonus Stages):
/// 1. All 5 Real In-Game Cutscenes (StageCutsceneController.cs) เล่นพร้อมกันทุกกรอบบนฉากจริงที่ Copy มาพร้อม SpriteMask:
///    - MainMenu Attract (Boss Wolf + Mama + 2 Piglets + ลอยลูกโป่งไปเกาะ UI Select Cursor)
///    - Stage 1 Intro Kidnap (Autumn: ลูกหมูเดินกลับบ้าน -> หมาป่าจับอุ้ม CutScene_StealPiglet -> แม่หมูวิ่งไล่)
///    - Stage 2 Intro Chase (Spring: หมาป่าหนี -> ลูกหมู 1 -> ลูกหมู 2 -> แม่หมู)
///    - Stage 4 Intro Chase (Rainy: หมาป่าฝน -> ลูกหมูฝน 1 & 2 -> แม่หมูฝน + ฝนตก)
///    - Stage 2/4 Clear Reunion (Spring: กระเช้า + เชือกจริงเลื่อนลงพื้น -> แม่หมูเดินออก -> ลูกหมูวิ่งเข้าขบวนเข้าโบนัส)
/// 2. Stage Switching & Difficulty Progression (GameManager.cs + LevelManager.cs):
///    - Timeline 8 ช่อง (Stage 1..5 + Bonus 1 & 2 + Loop 2+ Random Pool) พร้อมกรอบไฮไลต์วิ่งตาม
///    - จอพรีวิวฉากจริงทั้ง 7 ฉากสลับไปมาพร้อม Fade Curtain + แบนเนอร์แบบ NES + แม่หมู/หมาป่า/ธนู/เนื้อประจำฤดู + สูตร Scaling สด
/// 3. Bonus Stages Showcase (BonusStageManager.cs + BonusBossWolf.cs):
///    - Meat Bonus (Spring_Bonus): แม่หมูโหมดเนื้อล้วนปาเนื้อวิถีโค้งสอยหมาป่าลูกโป่งฟ้า 1-HP -> คอมโบ 400..6,400
///    - Fruit Bonus (Rainy_Bonus): บอสหมาป่าวิ่งบนยอดไม้ทิ้งผลไม้ (Strawberry/Cherry/Peach) -> แม่หมูยิงสอยทำ Nakamoto Chain 100..8,000
/// </summary>
public class ShowcaseCoreSystemsDemo : MonoBehaviour
{
    public enum CutsceneCardMode
    {
        MainMenuAttract,
        Stage1KidnapAutumn,
        Stage2ChaseSpring,
        Stage4ChaseRainy,
        StageClearReunion
    }

    [Serializable]
    public class CutsceneCardRig
    {
        public CutsceneCardMode mode;
        public Transform actorsRoot;
        public float viewportHalfWidth = 3.02f;
        public float groundLocalY = -1.18f;

        public SpriteRenderer mamaSr;
        public Animator mamaAnim;
        public string mamaWalkState;
        public float mamaYOffset = 0.30f;

        public SpriteRenderer piglet1Sr;
        public Animator piglet1Anim;
        public SpriteRenderer piglet2Sr;
        public Animator piglet2Anim;
        public string pigletWalkState;
        public float pigletYOffset = 0.20f;

        public SpriteRenderer wolfSr;
        public Animator wolfAnim;
        public string wolfRunState;
        public string wolfStealState;
        public float wolfYOffset = 0.30f;

        // สำหรับ MainMenu Attract: ลูกโป่งลอยขึ้นไปเกาะตำแหน่ง Select Cursor
        public SpriteRenderer balloonSr;
        public Transform balloonDockTarget;

        // สำหรับ Stage Clear Reunion: กระเช้าแม่หมูจริง + เชือก LineRenderer เลื่อนลงพื้น
        public Transform reunionBasketRig;
        public Transform reunionInBasketMama;
        public LineRenderer reunionRopeLine;
        public float reunionRopeTopLocalY = 1.48f;
        public float reunionBasketTopLocalY = 0.92f;
        public float reunionBasketGroundLocalY = -0.90f;

        public TextMesh phaseLabel;
    }

    [Serializable]
    public class StageMonitorRig
    {
        // 0=Autumn, 1=Spring, 2=Winter, 3=Rainy, 4=Summer, 5=Spring_Bonus, 6=Rainy_Bonus
        public GameObject[] stageEnvRoots = new GameObject[7];
        public SpriteRenderer fadeCurtainSr;
        public GameObject nesBannerRoot;
        public TextMesh nesBannerLabel;

        public SpriteRenderer previewMamaSr;
        public Animator previewMamaAnim;
        public SpriteRenderer previewWolfSr;
        public Animator previewWolfAnim;
        public SpriteRenderer previewWolfBalloonSr;
        public SpriteRenderer previewArrowSr;
        public SpriteRenderer previewMeatSr;
        public TextMesh directionBadgeLabel;

        // 8 Timeline Nodes (0:Stg1, 1:Stg2, 2:Bonus1, 3:Stg3, 4:Stg4, 5:Bonus2, 6:Stg5, 7:Loop2+)
        public Transform[] timelineNodes = new Transform[8];
        public Transform timelineHighlightFrame;

        public TextMesh stageTitleLabel;
        public TextMesh scalingFormulaLabel;
        public TextMesh gameStateFlowLabel;
    }

    [Serializable]
    public class MeatBonusRig
    {
        public Transform stageRoot;
        public Transform mamaBasketRig;
        public Animator mamaAnim;
        public Transform firePoint;
        public GameObject meatPrefab;
        public Sprite wolfSprite;
        public RuntimeAnimatorController wolfCtrl;
        public Sprite balloonSprite;
        public Material balloonMat;
        public GameObject fxBalloonPopPrefab;
        public TextMesh popupScoreLabel;
        public TextMesh statusLabel;
        public GameObject perfectBannerRoot;
        public TextMesh perfectBannerLabel;
    }

    [Serializable]
    public class FruitBonusRig
    {
        public Transform stageRoot;
        public SpriteRenderer bossWolfSr;
        public Animator bossWolfAnim;
        public string bossRunState = "Run";
        public string bossThrowState = "ThrowFruit";
        public float bossMinX = -2.45f;
        public float bossMaxX = 0.65f;
        public float bossTopY = 1.18f;

        public Transform mamaBasketRig;
        public Animator mamaAnim;
        public Transform firePoint;
        public GameObject arrowPrefab;
        public GameObject fruitPrefab;
        public Sprite[] fruitSprites = new Sprite[3]; // 0=Strawberry, 1=Cherry, 2=Peach
        public GameObject fxPopSparklePrefab;
        public TextMesh popupScoreLabel;
        public TextMesh statusLabel;
        public GameObject perfectBannerRoot;
        public TextMesh perfectBannerLabel;
    }

    [Header("1. Five Real In-Game Cutscene Rigs")]
    public CutsceneCardRig[] cutsceneRigs = new CutsceneCardRig[5];

    [Header("2. Stage Switcher & Level Scaling Monitor")]
    public StageMonitorRig stageMonitor = new StageMonitorRig();

    [Header("Seasonal Visual Assets (0=Autumn, 1=Spring, 2=Winter, 3=Rainy, 4=Summer)")]
    public Sprite[] seasonMamaSprites = new Sprite[5];
    public RuntimeAnimatorController[] seasonMamaControllers = new RuntimeAnimatorController[5];
    public Sprite[] seasonWolfSprites = new Sprite[5];
    public RuntimeAnimatorController[] seasonWolfControllers = new RuntimeAnimatorController[5];
    public Sprite[] seasonArrowSprites = new Sprite[5];
    public Sprite[] seasonMeatSprites = new Sprite[5];

    [Header("3. Bonus Stage Live Rigs")]
    public MeatBonusRig meatBonus = new MeatBonusRig();
    public FruitBonusRig fruitBonus = new FruitBonusRig();

    // Progression Step Sequence:
    // 0: Stage 1 (Autumn)
    // 1: Stage 2 (Spring)
    // 2: Bonus 1 (Spring_Bonus - Meat Bonus)
    // 3: Stage 3 (Winter)
    // 4: Stage 4 (Rainy)
    // 5: Bonus 2 (Rainy_Bonus - Fruit Bonus)
    // 6: Stage 5 (Summer)
    // 7: Stage 6 (Loop 2 Even Random -> Spring/Rainy)
    // 8: Stage 7 (Loop 2 Odd Random -> Autumn/Winter/Summer)
    private int flowStepIndex = 0;
    private bool autoCycleStages = true;
    private float autoCycleTimer = 3.4f;
    private float stageFadeAlpha = 0f;
    private float nesBannerTimer = 0f;
    private float wolfDriftPhase = 0f;
    private bool currentMonitorIsDescending = true;
    private bool currentMonitorIsBonus = false;

    private readonly List<Coroutine> cutsceneCoroutines = new();
    private Coroutine meatBonusCoroutine;
    private Coroutine fruitBonusCoroutine;
    private Coroutine perfectBannerCoroutine;

    // Flat Dark Studio Toolbar Styles
    private bool stylesReady = false;
    private readonly List<Texture2D> createdTextures = new();
    private Texture2D texBarBg;
    private Texture2D texBtnNormal;
    private Texture2D texBtnHover;
    private Texture2D texBtnActive;
    private Texture2D texBtnAccent;
    private GUIStyle styleBarBg;
    private GUIStyle styleLabel;
    private GUIStyle styleBtn;
    private GUIStyle styleBtnActive;
    private GUIStyle styleBtnAccent;

    private static readonly string[] SeasonNames = { "Autumn", "Spring", "Winter", "Rainy", "Summer" };
    private static readonly int[] NakamotoScores = { 100, 200, 400, 800, 1000, 2000, 4000, 8000 };
    private static readonly string[] FruitNames = { "Strawberry", "Cherry", "Peach" };
    private static readonly Color[] FruitGlowColors =
    {
        new Color(1.00f, 0.24f, 0.34f, 0.90f), // Strawberry Ruby
        new Color(1.00f, 0.16f, 0.56f, 0.90f), // Cherry Crimson
        new Color(1.00f, 0.66f, 0.18f, 0.90f)  // Peach Amber
    };

    private void Start()
    {
        StartAllCutscenes();
        ApplyFlowStep(0, playTransition: false);
        StartBonusDemos();
    }

    private void OnDestroy()
    {
        foreach (var t in createdTextures)
        {
            if (t != null) Destroy(t);
        }
        createdTextures.Clear();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // 1. Auto-Cycle Stage Progression
        if (autoCycleStages)
        {
            autoCycleTimer -= Time.deltaTime;
            if (autoCycleTimer <= 0f)
            {
                autoCycleTimer = 3.4f;
                StepFlowForward();
            }
        }

        // 2. Smooth Stage Transition Curtain Fade + NES Banner Timer
        if (stageFadeAlpha > 0f)
        {
            stageFadeAlpha = Mathf.Max(0f, stageFadeAlpha - Time.deltaTime * 2.8f);
            if (stageMonitor.fadeCurtainSr != null)
            {
                Color c = stageMonitor.fadeCurtainSr.color;
                c.a = stageFadeAlpha;
                stageMonitor.fadeCurtainSr.color = c;
            }
        }

        if (nesBannerTimer > 0f)
        {
            nesBannerTimer -= Time.deltaTime;
            if (nesBannerTimer <= 0f && stageMonitor.nesBannerRoot != null)
            {
                stageMonitor.nesBannerRoot.SetActive(false);
            }
        }

        // 3. Animate Live Wolf Floating Direction inside Stage Monitor (Odd = Down ↓, Even = Up ↑)
        if (stageMonitor.previewWolfSr != null)
        {
            wolfDriftPhase += Time.deltaTime * 0.65f;
            float u = Mathf.Repeat(wolfDriftPhase, 1f);
            float y = currentMonitorIsBonus
                ? Mathf.Lerp(-0.75f, 0.75f, u)
                : (currentMonitorIsDescending ? Mathf.Lerp(0.85f, -0.82f, u) : Mathf.Lerp(-0.82f, 0.85f, u));
            float swayX = Mathf.Sin(Time.time * 2.6f) * 0.22f;
            Vector3 wolfPos = new Vector3(-2.15f + swayX, y, 0f);
            stageMonitor.previewWolfSr.transform.localPosition = wolfPos;
            if (stageMonitor.previewWolfBalloonSr != null)
            {
                stageMonitor.previewWolfBalloonSr.transform.localPosition = wolfPos + new Vector3(0.03f, 0.72f, 0f);
            }
        }

        if (stageMonitor.previewMamaSr != null)
        {
            float bobY = Mathf.Sin(Time.time * 2.1f) * 0.42f;
            stageMonitor.previewMamaSr.transform.localPosition = new Vector3(2.35f, bobY, 0f);
        }
    }

    // =========================================================================================
    // SECTION 1: ALL 5 REAL IN-GAME CUTSCENES (RUNNING SIMULTANEOUSLY ON REAL COPIED STAGES)
    // =========================================================================================
    public void StartAllCutscenes()
    {
        foreach (var c in cutsceneCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        cutsceneCoroutines.Clear();

        if (cutsceneRigs == null) return;
        for (int i = 0; i < cutsceneRigs.Length; i++)
        {
            var rig = cutsceneRigs[i];
            if (rig == null || rig.actorsRoot == null) continue;
            Coroutine co = rig.mode switch
            {
                CutsceneCardMode.MainMenuAttract => StartCoroutine(RunMainMenuAttractLoop(rig)),
                CutsceneCardMode.Stage1KidnapAutumn => StartCoroutine(RunStage1KidnapLoop(rig)),
                CutsceneCardMode.Stage2ChaseSpring => StartCoroutine(RunStageChaseLoop(rig, "Stage 2 (Spring)")),
                CutsceneCardMode.Stage4ChaseRainy => StartCoroutine(RunStageChaseLoop(rig, "Stage 4 (Rainy)")),
                CutsceneCardMode.StageClearReunion => StartCoroutine(RunStageClearReunionLoop(rig)),
                _ => null
            };
            if (co != null) cutsceneCoroutines.Add(co);
        }
    }

    private IEnumerator RunMainMenuAttractLoop(CutsceneCardRig rig)
    {
        float halfW = rig.viewportHalfWidth;
        float exitL = -halfW - 0.35f;
        float enterR = halfW + 0.35f;

        while (true)
        {
            SetPhase(rig, "1) Boss Wolf นำหน้า -> แม่หมู & ลูกหมู 2 ถือลูกโป่งวิ่งเข้าจอ");
            PlayState(rig.wolfAnim, rig.wolfRunState);
            PlayState(rig.mamaAnim, rig.mamaWalkState);
            PlayState(rig.piglet1Anim, rig.pigletWalkState);
            PlayState(rig.piglet2Anim, rig.pigletWalkState);

            bool released = false;
            bool docked = false;
            float floatElapsed = 0f;
            Vector3 startBalloonPos = Vector3.zero;
            Vector3 dockPos = rig.balloonDockTarget != null
                ? rig.balloonDockTarget.localPosition
                : new Vector3(-0.65f, 0.58f, 0f);

            if (rig.balloonSr != null)
            {
                rig.balloonSr.gameObject.SetActive(true);
                rig.balloonSr.transform.localRotation = Quaternion.identity;
            }

            float mamaX = enterR + 0.65f;
            const float wolfLead = 0.85f;
            const float gap = 0.54f;
            float elapsed = 0f;

            while (elapsed < 6.4f)
            {
                elapsed += Time.deltaTime;
                mamaX -= 1.55f * Time.deltaTime;
                float wolfX = mamaX - wolfLead;
                float pig1X = mamaX + gap;
                float pig2X = mamaX + gap * 2f;
                float bob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.025f;

                PlaceCutsceneActor(rig.wolfSr, wolfX, rig.groundLocalY + rig.wolfYOffset + bob, faceLeft: true, exitL, enterR + 1.8f);
                PlaceCutsceneActor(rig.mamaSr, mamaX, rig.groundLocalY + rig.mamaYOffset + bob, faceLeft: true, exitL, enterR + 1.8f);
                PlaceCutsceneActor(rig.piglet1Sr, pig1X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: true, exitL, enterR + 1.8f);
                PlaceCutsceneActor(rig.piglet2Sr, pig2X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: true, exitL, enterR + 1.8f);

                if (rig.balloonSr != null)
                {
                    if (!released)
                    {
                        Vector3 handPos = new Vector3(pig2X - 0.14f, rig.groundLocalY + rig.pigletYOffset + 0.14f + bob, 0f);
                        rig.balloonSr.transform.localPosition = handPos;
                        rig.balloonSr.transform.localRotation = Quaternion.Euler(0f, 0f, -12f + Mathf.Sin(elapsed * 10f) * 3f);
                        rig.balloonSr.gameObject.SetActive(pig2X <= enterR + 0.2f);

                        if (handPos.x <= dockPos.x + 0.18f)
                        {
                            released = true;
                            startBalloonPos = handPos;
                            floatElapsed = 0f;
                            SetPhase(rig, "2) ลูกหมู 2 ปล่อยลูกโป่ง -> ลอยขึ้นเกาะเป็น UI Select Cursor!");
                        }
                    }
                    else if (!docked)
                    {
                        floatElapsed += Time.deltaTime;
                        float u = Mathf.Clamp01(floatElapsed / 1.35f);
                        float lift = Mathf.SmoothStep(0f, 1f, u);
                        float sway = Mathf.Sin(u * Mathf.PI * 2f) * 0.12f * (1f - u);
                        rig.balloonSr.transform.localPosition = Vector3.Lerp(startBalloonPos, dockPos, lift) + new Vector3(sway, 0f, 0f);
                        rig.balloonSr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-12f, 0f, lift));
                        if (u >= 1f)
                        {
                            docked = true;
                            SetPhase(rig, "3) ลูกโป่งจอดที่ ▶ 1 PLAYER พร้อมเล่น (Loop รอบใหม่)");
                        }
                    }
                    else
                    {
                        // ลอยดึ๋งเบาๆ ที่ตำแหน่ง Select Cursor
                        rig.balloonSr.transform.localPosition = dockPos + new Vector3(0f, Mathf.Sin(elapsed * 5f) * 0.03f, 0f);
                    }
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.55f);
        }
    }

    private IEnumerator RunStage1KidnapLoop(CutsceneCardRig rig)
    {
        float halfW = rig.viewportHalfWidth;
        float exitL = -halfW - 0.35f;
        float exitR = halfW + 0.35f;

        while (true)
        {
            SetPhase(rig, "1) ลูกหมู 2 ตัวเดินกลับบ้านทางขวา | หมาป่าชาร์จจากซ้าย");
            PlayState(rig.piglet1Anim, rig.pigletWalkState);
            PlayState(rig.piglet2Anim, rig.pigletWalkState);
            PlayState(rig.wolfAnim, rig.wolfRunState);
            if (rig.mamaSr != null) rig.mamaSr.gameObject.SetActive(false);

            bool stole = false;
            bool mamaStarted = false;
            float elapsed = 0f;
            const float totalDur = 7.4f;

            while (elapsed < totalDur)
            {
                elapsed += Time.deltaTime;
                float bob = Mathf.Abs(Mathf.Sin(elapsed * 12f)) * 0.025f;

                if (elapsed >= 2.4f && !stole)
                {
                    stole = true;
                    SetPhase(rig, "2) หมาป่าจับลูกหมู 2 ใส่ถุง [CutScene_StealPiglet] แล้วหันหนี!");
                    PlayState(rig.wolfAnim, rig.wolfStealState);
                    if (rig.piglet2Sr != null) rig.piglet2Sr.gameObject.SetActive(false);
                }

                if (elapsed < 2.4f)
                {
                    float u = elapsed / 2.4f;
                    float pig1X = Mathf.Lerp(-0.45f, 1.45f, u);
                    float pig2X = Mathf.Lerp(-1.25f, 0.45f, u);
                    float wolfX = Mathf.Lerp(-halfW - 0.25f, 0.30f, u);

                    PlaceCutsceneActor(rig.piglet1Sr, pig1X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: false, exitL, exitR);
                    PlaceCutsceneActor(rig.piglet2Sr, pig2X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: false, exitL, exitR);
                    PlaceCutsceneActor(rig.wolfSr, wolfX, rig.groundLocalY + rig.wolfYOffset + bob, faceLeft: false, exitL, exitR);
                }
                else if (elapsed < 3.3f)
                {
                    float u = (elapsed - 2.4f) / 0.9f;
                    float pig1X = Mathf.Lerp(1.45f, exitR + 0.1f, u);
                    PlaceCutsceneActor(rig.piglet1Sr, pig1X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: false, exitL, exitR);
                    PlaceCutsceneActor(rig.wolfSr, 0.35f, rig.groundLocalY + rig.wolfYOffset + bob, faceLeft: true, exitL, exitR);
                }
                else
                {
                    if (rig.piglet1Sr != null) rig.piglet1Sr.gameObject.SetActive(false);
                    float uWolf = Mathf.Clamp01((elapsed - 3.3f) / 2.0f);
                    float wolfX = Mathf.Lerp(0.35f, exitL - 0.15f, uWolf);
                    PlaceCutsceneActor(rig.wolfSr, wolfX, rig.groundLocalY + rig.wolfYOffset + bob, faceLeft: true, exitL, exitR);

                    if (elapsed >= 4.25f)
                    {
                        if (!mamaStarted)
                        {
                            mamaStarted = true;
                            SetPhase(rig, "3) แม่หมูพุ่งออกจากบ้าน [CutScene_Walk] วิ่งไล่กวดหมาป่า!");
                            PlayState(rig.mamaAnim, rig.mamaWalkState);
                        }
                        float uMama = Mathf.Clamp01((elapsed - 4.25f) / 2.3f);
                        float mamaX = Mathf.Lerp(exitR - 0.1f, exitL - 0.15f, uMama);
                        PlaceCutsceneActor(rig.mamaSr, mamaX, rig.groundLocalY + rig.mamaYOffset + bob, faceLeft: true, exitL, exitR);
                    }
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.45f);
        }
    }

    private IEnumerator RunStageChaseLoop(CutsceneCardRig rig, string tag)
    {
        float halfW = rig.viewportHalfWidth;
        float exitL = -halfW - 0.35f;
        float enterR = halfW + 0.35f;

        while (true)
        {
            SetPhase(rig, $"{tag} Chase: หมาป่าหนี -> ลูกหมู 1 -> ลูกหมู 2 -> แม่หมู");
            PlayState(rig.wolfAnim, rig.wolfRunState);
            PlayState(rig.piglet1Anim, rig.pigletWalkState);
            PlayState(rig.piglet2Anim, rig.pigletWalkState);
            PlayState(rig.mamaAnim, rig.mamaWalkState);

            float elapsed = 0f;
            const float dur = 4.6f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float u = elapsed / dur;
                float bob = Mathf.Abs(Mathf.Sin(elapsed * 13f)) * 0.025f;

                float wolfX = Mathf.Lerp(enterR - 0.2f, exitL - 0.25f, Mathf.Clamp01(u * 1.18f));
                float pig1X = Mathf.Lerp(enterR + 1.05f, exitL - 0.25f, Mathf.Clamp01(u * 1.12f));
                float pig2X = Mathf.Lerp(enterR + 1.62f, exitL - 0.25f, Mathf.Clamp01(u * 1.06f));
                float mamaX = Mathf.Lerp(enterR + 2.25f, exitL - 0.25f, Mathf.Clamp01(u * 1.00f));

                PlaceCutsceneActor(rig.wolfSr, wolfX, rig.groundLocalY + rig.wolfYOffset + bob, faceLeft: true, exitL, enterR + 2.4f);
                PlaceCutsceneActor(rig.piglet1Sr, pig1X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: true, exitL, enterR + 2.4f);
                PlaceCutsceneActor(rig.piglet2Sr, pig2X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: true, exitL, enterR + 2.4f);
                PlaceCutsceneActor(rig.mamaSr, mamaX, rig.groundLocalY + rig.mamaYOffset + bob, faceLeft: true, exitL, enterR + 2.4f);
                yield return null;
            }
            yield return new WaitForSeconds(0.45f);
        }
    }

    private IEnumerator RunStageClearReunionLoop(CutsceneCardRig rig)
    {
        float halfW = rig.viewportHalfWidth;
        float exitL = -halfW - 0.35f;
        float enterR = halfW + 0.35f;
        float basketX = 1.42f;

        while (true)
        {
            // เฟส 1: กระเช้า + เชือกจริงเลื่อนลงมาจอดที่พื้นล่าง
            SetPhase(rig, "1) กระเช้าแม่หมู + เชือกรอกเลื่อนลงมาแตะพื้นล่าง");
            if (rig.mamaSr != null) rig.mamaSr.gameObject.SetActive(false);
            if (rig.piglet1Sr != null) rig.piglet1Sr.gameObject.SetActive(false);
            if (rig.piglet2Sr != null) rig.piglet2Sr.gameObject.SetActive(false);
            if (rig.reunionBasketRig != null) rig.reunionBasketRig.gameObject.SetActive(true);
            if (rig.reunionInBasketMama != null) rig.reunionInBasketMama.gameObject.SetActive(true);
            if (rig.reunionRopeLine != null) rig.reunionRopeLine.enabled = true;

            float elapsed = 0f;
            const float lowerDur = 1.85f;
            while (elapsed < lowerDur)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / lowerDur));
                float by = Mathf.Lerp(rig.reunionBasketTopLocalY, rig.reunionBasketGroundLocalY, u);
                if (rig.reunionBasketRig != null)
                {
                    rig.reunionBasketRig.localPosition = new Vector3(basketX, by, 0f);
                }
                UpdateReunionRope(rig, basketX, by);
                yield return null;
            }

            // เฟส 2 & 3: แม่หมูลงจากกระเช้าเดินนำช้าๆ -> ลูกหมู 2 ตัววิ่งตามเข้าขบวน -> วิ่งเร็วพร้อมกันเข้าด่านโบนัส!
            SetPhase(rig, "2) แม่หมูก้าวลงจากกระเช้า -> ลูกหมู 2 ตัววิ่งตามเข้าขบวน");
            if (rig.reunionInBasketMama != null) rig.reunionInBasketMama.gameObject.SetActive(false);

            float mamaX = basketX - 0.22f;
            float pig1X = enterR;
            float pig2X = enterR + 0.45f;
            bool pig1Spawned = false, pig2Spawned = false;
            bool pig1Caught = false, pig2Caught = false;
            bool sprinting = false;
            const float gap = 0.56f;

            PlayState(rig.mamaAnim, rig.mamaWalkState);
            elapsed = 0f;

            while (elapsed < 5.6f)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float bob = Mathf.Abs(Mathf.Sin(elapsed * (sprinting ? 17f : 11f))) * 0.025f;

                if (!pig1Spawned && elapsed >= 0.30f)
                {
                    pig1Spawned = true;
                    pig1X = enterR - 0.05f;
                    PlayState(rig.piglet1Anim, rig.pigletWalkState);
                }
                if (!pig2Spawned && elapsed >= 0.80f)
                {
                    pig2Spawned = true;
                    pig2X = enterR - 0.05f;
                    PlayState(rig.piglet2Anim, rig.pigletWalkState);
                }

                float mamaSpeed = sprinting ? 2.45f : 0.62f;
                mamaX -= mamaSpeed * dt;
                PlaceCutsceneActor(rig.mamaSr, mamaX, rig.groundLocalY + rig.mamaYOffset + bob, faceLeft: true, exitL, enterR + 1f);

                if (pig1Spawned)
                {
                    float target1 = mamaX + gap;
                    if (!pig1Caught)
                    {
                        pig1X = Mathf.MoveTowards(pig1X, target1, 1.38f * dt);
                        if (pig1X <= target1 + 0.02f) { pig1X = target1; pig1Caught = true; }
                    }
                    else pig1X = target1;
                    PlaceCutsceneActor(rig.piglet1Sr, pig1X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: true, exitL, enterR + 1f);
                }
                if (pig2Spawned)
                {
                    float target2 = mamaX + gap * 2f;
                    if (!pig2Caught)
                    {
                        pig2X = Mathf.MoveTowards(pig2X, target2, 1.38f * dt);
                        if (pig2X <= target2 + 0.02f) { pig2X = target2; pig2Caught = true; }
                    }
                    else pig2X = target2;
                    PlaceCutsceneActor(rig.piglet2Sr, pig2X, rig.groundLocalY + rig.pigletYOffset + bob, faceLeft: true, exitL, enterR + 1f);
                }

                if (!sprinting && ((pig1Caught && pig2Caught) || mamaX <= -1.25f))
                {
                    sprinting = true;
                    pig1Caught = true;
                    pig2Caught = true;
                    SetPhase(rig, "3) รวมขบวนครบ! วิ่งเร็วพร้อมกันเข้าสู่ BONUS STAGE!");
                }

                if (sprinting && pig2Spawned && pig2X <= exitL) break;
                yield return null;
            }

            yield return new WaitForSeconds(0.50f);
        }
    }

    private static void UpdateReunionRope(CutsceneCardRig rig, float basketLocalX, float basketLocalY)
    {
        if (rig.reunionRopeLine == null || rig.actorsRoot == null) return;
        Vector3 topW = rig.actorsRoot.TransformPoint(new Vector3(basketLocalX, rig.reunionRopeTopLocalY, 0f));
        Vector3 botW = rig.actorsRoot.TransformPoint(new Vector3(basketLocalX, basketLocalY + 0.22f, 0f));
        rig.reunionRopeLine.positionCount = 2;
        rig.reunionRopeLine.SetPosition(0, topW);
        rig.reunionRopeLine.SetPosition(1, botW);
    }

    private static void PlaceCutsceneActor(SpriteRenderer sr, float localX, float localY, bool faceLeft, float minX, float maxX)
    {
        if (sr == null) return;
        bool visible = localX > minX && localX < maxX;
        if (sr.gameObject.activeSelf != visible) sr.gameObject.SetActive(visible);
        if (!visible) return;
        sr.transform.localPosition = new Vector3(localX, localY, 0f);
        sr.flipX = faceLeft;
    }

    private static void SetPhase(CutsceneCardRig rig, string text)
    {
        if (rig != null && rig.phaseLabel != null) rig.phaseLabel.text = text;
    }

    // =========================================================================================
    // SECTION 2: STAGE SWITCHING, PROGRESSION TIMELINE & DIFFICULTY SCALING
    // =========================================================================================
    private void StepFlowForward()
    {
        flowStepIndex = (flowStepIndex + 1) % 9;
        ApplyFlowStep(flowStepIndex, playTransition: true);
    }

    private void StepFlowBackward()
    {
        flowStepIndex = (flowStepIndex + 8) % 9;
        ApplyFlowStep(flowStepIndex, playTransition: true);
    }

    private void ApplyFlowStep(int stepIdx, bool playTransition)
    {
        flowStepIndex = Mathf.Clamp(stepIdx, 0, 8);

        int stageNum;
        int seasonIdx;
        int envIdx;
        int timelineIdx;
        bool isBonus = false;
        string sceneName;
        string ruleText;
        string stateHighlight;

        switch (flowStepIndex)
        {
            case 0:
                stageNum = 1; seasonIdx = 0; envIdx = 0; timelineIdx = 0;
                sceneName = "Autumn"; ruleText = "Cycle 1..5 -> GetShowcaseSceneName(1)"; stateHighlight = "Cutscene -> Playing";
                break;
            case 1:
                stageNum = 2; seasonIdx = 1; envIdx = 1; timelineIdx = 1;
                sceneName = "Spring"; ruleText = "Cycle 1..5 -> GetShowcaseSceneName(2)"; stateHighlight = "Cutscene -> Playing";
                break;
            case 2:
                stageNum = 2; seasonIdx = 1; envIdx = 5; timelineIdx = 2; isBonus = true;
                sceneName = "Spring_Bonus"; ruleText = "Stage 2 Clear (Even) -> Meat Bonus Stage"; stateHighlight = "StageClear -> BonusStage";
                break;
            case 3:
                stageNum = 3; seasonIdx = 2; envIdx = 2; timelineIdx = 3;
                sceneName = "Winter"; ruleText = "Cycle 1..5 -> GetShowcaseSceneName(3) [No Intro Cutscene]"; stateHighlight = "BonusComplete -> Playing";
                break;
            case 4:
                stageNum = 4; seasonIdx = 3; envIdx = 3; timelineIdx = 4;
                sceneName = "Rainy"; ruleText = "Cycle 1..5 -> GetShowcaseSceneName(4)"; stateHighlight = "Cutscene -> Playing";
                break;
            case 5:
                stageNum = 4; seasonIdx = 3; envIdx = 6; timelineIdx = 5; isBonus = true;
                sceneName = "Rainy_Bonus"; ruleText = "Stage 4 Clear (Even) -> Fruit Bonus Stage"; stateHighlight = "StageClear -> BonusStage";
                break;
            case 6:
                stageNum = 5; seasonIdx = 4; envIdx = 4; timelineIdx = 6;
                sceneName = "Summer"; ruleText = "Cycle 1..5 -> GetShowcaseSceneName(5) [No Intro Cutscene]"; stateHighlight = "BonusComplete -> Playing";
                break;
            case 7:
                stageNum = 6; seasonIdx = 3; envIdx = 3; timelineIdx = 7;
                sceneName = "Rainy (Random Even)"; ruleText = "Loop 2+ Random Even Pool {Spring, Rainy}"; stateHighlight = "Loop 2+ -> Playing";
                break;
            default:
                stageNum = 7; seasonIdx = 2; envIdx = 2; timelineIdx = 7;
                sceneName = "Winter (Random Odd)"; ruleText = "Loop 2+ Random Odd Pool {Autumn, Winter, Summer}"; stateHighlight = "Loop 2+ -> Playing";
                break;
        }

        bool isOdd = (stageNum % 2 == 1);
        int loopCount = 1 + ((stageNum - 1) / 5);
        currentMonitorIsDescending = isOdd;
        currentMonitorIsBonus = isBonus;

        // 1. สลับฉากจริงที่ Copy มาแสดงในจอ Stage Monitor
        if (stageMonitor.stageEnvRoots != null)
        {
            for (int i = 0; i < stageMonitor.stageEnvRoots.Length; i++)
            {
                if (stageMonitor.stageEnvRoots[i] != null)
                {
                    stageMonitor.stageEnvRoots[i].SetActive(i == envIdx);
                }
            }
        }

        // 2. ขยับกรอบสีทองบน Timeline 8 ช่องให้ตรงด่านปัจจุบัน
        if (stageMonitor.timelineHighlightFrame != null
            && stageMonitor.timelineNodes != null
            && timelineIdx < stageMonitor.timelineNodes.Length
            && stageMonitor.timelineNodes[timelineIdx] != null)
        {
            stageMonitor.timelineHighlightFrame.localPosition = stageMonitor.timelineNodes[timelineIdx].localPosition;
        }

        // 3. เล่น Fade Transition + แบนเนอร์แบบ NES สั้นๆ
        if (playTransition)
        {
            stageFadeAlpha = 0.78f;
            if (stageMonitor.nesBannerRoot != null && stageMonitor.nesBannerLabel != null)
            {
                stageMonitor.nesBannerRoot.SetActive(true);
                stageMonitor.nesBannerLabel.text = isBonus
                    ? $"★ BONUS STAGE ({sceneName}) ★"
                    : $"STAGE {stageNum} — {SeasonNames[seasonIdx].ToUpperInvariant()}";
                nesBannerTimer = 1.05f;
            }
        }

        // 4. สลับตัวละคร & อาวุธประจำฤดูในจอ Stage Monitor
        ApplySeasonVisual(stageMonitor.previewMamaSr, stageMonitor.previewMamaAnim, seasonMamaSprites, seasonMamaControllers, seasonIdx, "Idle");
        ApplySeasonVisual(stageMonitor.previewWolfSr, stageMonitor.previewWolfAnim, seasonWolfSprites, seasonWolfControllers, seasonIdx, "Fly");
        if (stageMonitor.previewArrowSr != null && seasonIdx < seasonArrowSprites.Length && seasonArrowSprites[seasonIdx] != null)
            stageMonitor.previewArrowSr.sprite = seasonArrowSprites[seasonIdx];
        if (stageMonitor.previewMeatSr != null && seasonIdx < seasonMeatSprites.Length && seasonMeatSprites[seasonIdx] != null)
            stageMonitor.previewMeatSr.sprite = seasonMeatSprites[seasonIdx];
        if (stageMonitor.previewWolfBalloonSr != null)
        {
            stageMonitor.previewWolfBalloonSr.color = isBonus
                ? new Color(0.45f, 0.85f, 1f, 1f)
                : (isOdd ? new Color(1f, 0.36f, 0.56f, 1f) : new Color(0.32f, 0.68f, 1f, 1f));
        }

        if (stageMonitor.directionBadgeLabel != null)
        {
            stageMonitor.directionBadgeLabel.text = isBonus
                ? (envIdx == 5 ? "★ MEAT BONUS (ปาเนื้อล้วน)" : "★ FRUIT BONUS (บอสทิ้งผลไม้)")
                : (isOdd ? "↓ ODD STAGE: ลอยลง (Descending)" : "↑ EVEN STAGE: ลอยขึ้น (Ascending)");
        }

        // 5. คำนวณสูตร LevelManager.ApplyScaling() จริง
        int wolfCount = Mathf.Min(99, 32 + (stageNum - 1) * 4);
        int scaleSteps = Mathf.Min(14, Mathf.Max(0, stageNum - 4));
        float minSpd = 0.75f + 0.05f * scaleSteps;
        float maxSpd = 1.03f + 0.06f * scaleSteps;
        float spawnInterval = Mathf.Max(0.95f, 1.65f - 0.12f * scaleSteps);
        int balloonHp = isOdd ? 1 : (stageNum >= 4 ? 5 : 4);
        bool hasTreeBoss = stageNum >= 3 && isOdd;
        bool hasBossWolf = !isOdd && seasonIdx != 1;

        if (stageMonitor.stageTitleLabel != null)
        {
            stageMonitor.stageTitleLabel.text =
                $"STAGE {stageNum} (Loop {loopCount})  |  Scene: {sceneName}\n" +
                $"Rule: {ruleText}";
        }

        if (stageMonitor.scalingFormulaLabel != null)
        {
            stageMonitor.scalingFormulaLabel.text = isBonus
                ? (envIdx == 5
                    ? "BonusStageManager (NES Order #1 — Meat Bonus):\n• 16 Wolves (1-HP LightBlue) + Standalone Balloons\n• SetMeatBonusMode(true) | Combo: 400 × 2^(n-1)\n• เก็บครบ 16 ตัวไม่หลุด = PERFECT +10,000 PT"
                    : "BonusStageManager (NES Order #2 — Fruit Bonus):\n• BonusBossWolf วิ่งยอดไม้ทิ้งผลไม้ 20 ลูก\n• Nakamoto Chain: 100->200->400->800->1k->2k->4k->8k\n• ยิงครบ 20 ลูก = PERFECT +10,000 PT")
                : $"LevelManager.ApplyScaling(stage = {stageNum}):\n" +
                  $"• Wolves: min(99, 32+({stageNum}-1)×4) = {wolfCount} ตัว\n" +
                  $"• Balloon HP: {balloonHp} HP  |  Spawn: {spawnInterval:F2}s\n" +
                  $"• Speed: {minSpd:F2}..{maxSpd:F2}  |  TreeBoss: {(hasTreeBoss ? "ON" : "OFF")}  |  Boss: {(hasBossWolf ? "ON" : "OFF")}";
        }

        if (stageMonitor.gameStateFlowLabel != null)
        {
            stageMonitor.gameStateFlowLabel.text = $"GameState: MainMenu -> [{stateHighlight}] -> Next Scene";
        }
    }

    private static void ApplySeasonVisual(SpriteRenderer sr, Animator anim, Sprite[] sprites, RuntimeAnimatorController[] ctrls, int idx, string preferredState)
    {
        if (sr != null && sprites != null && idx < sprites.Length && sprites[idx] != null)
            sr.sprite = sprites[idx];
        if (anim != null && ctrls != null && idx < ctrls.Length && ctrls[idx] != null)
        {
            anim.runtimeAnimatorController = ctrls[idx];
            if (!PlayState(anim, preferredState))
            {
                PlayState(anim, "Run");
            }
        }
    }

    // =========================================================================================
    // SECTION 3: LIVE BONUS STAGE DEMOS (MEAT BONUS & FRUIT BONUS ON REAL COPIED SCENES)
    // =========================================================================================
    private void StartBonusDemos()
    {
        if (meatBonusCoroutine != null) StopCoroutine(meatBonusCoroutine);
        if (fruitBonusCoroutine != null) StopCoroutine(fruitBonusCoroutine);

        if (meatBonus != null && meatBonus.stageRoot != null)
            meatBonusCoroutine = StartCoroutine(RunMeatBonusDemoLoop());
        if (fruitBonus != null && fruitBonus.stageRoot != null)
            fruitBonusCoroutine = StartCoroutine(RunFruitBonusDemoLoop());
    }

    private IEnumerator RunMeatBonusDemoLoop()
    {
        // สร้างหมาป่าลูกโป่งสีฟ้า 1-HP 3 ตัวแบบ NES Staircase + ชิ้นเนื้อจริง (MeatSpring)
        var wolves = new List<(GameObject root, Animator anim, GameObject balloon, Vector3 basePos, bool hit)>();
        float[] laneX = { -0.45f, -1.35f, -2.25f };
        float[] startY = { -0.55f, -0.85f, -1.10f };

        for (int i = 0; i < 3; i++)
        {
            GameObject wRoot = new GameObject($"MeatBonusWolf_{i}");
            wRoot.transform.SetParent(meatBonus.stageRoot, false);
            wRoot.transform.localPosition = new Vector3(laneX[i], startY[i], 0f);

            GameObject wBody = new GameObject("WolfBody");
            wBody.transform.SetParent(wRoot.transform, false);
            wBody.transform.localScale = Vector3.one * 0.145f;
            var wSr = wBody.AddComponent<SpriteRenderer>();
            wSr.sprite = meatBonus.wolfSprite;
            wSr.sortingOrder = 17;
            wSr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            var wAnim = wBody.AddComponent<Animator>();
            if (meatBonus.wolfCtrl != null) wAnim.runtimeAnimatorController = meatBonus.wolfCtrl;

            GameObject bal = new GameObject("LightBlueBalloon");
            bal.transform.SetParent(wRoot.transform, false);
            bal.transform.localPosition = new Vector3(0.02f, 0.44f, 0f);
            bal.transform.localScale = Vector3.one * 0.22f;
            var bSr = bal.AddComponent<SpriteRenderer>();
            bSr.sprite = meatBonus.balloonSprite;
            bSr.color = new Color(0.45f, 0.86f, 1f, 1f); // LightBlue 1-HP ตาม BonusStageManager.cs!
            if (meatBonus.balloonMat != null) bSr.sharedMaterial = meatBonus.balloonMat;
            bSr.sortingOrder = 18;
            bSr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            wolves.Add((wRoot, wAnim, bal, new Vector3(laneX[i], startY[i], 0f), false));
        }

        GameObject meatObj = null;
        if (meatBonus.meatPrefab != null)
        {
            meatObj = Instantiate(meatBonus.meatPrefab, meatBonus.stageRoot);
            meatObj.name = "LiveBonusMeat";
            StripGameplayOnly(meatObj);
            meatObj.transform.localScale *= 0.52f;
            SetMaskInside(meatObj, 19);
            meatObj.SetActive(false);
        }

        int[] comboPts = { 400, 800, 1600 };

        while (true)
        {
            // รีเซ็ตหมาป่า 3 ตัวลอยขึ้นจากด้านล่างในเลนขั้นบันได NES
            for (int i = 0; i < wolves.Count; i++)
            {
                var w = wolves[i];
                w.hit = false;
                w.root.SetActive(true);
                w.root.transform.localPosition = w.basePos;
                if (w.balloon != null) w.balloon.SetActive(true);
                PlayState(w.anim, "Fly");
                wolves[i] = w;
            }
            if (meatBonus.popupScoreLabel != null) meatBonus.popupScoreLabel.text = "";
            if (meatBonus.statusLabel != null) meatBonus.statusLabel.text = "1) แม่หมูถือเนื้อ [HoldingMeat] — หมาป่า 1-HP ลอยขึ้นเลนขั้นบันได";

            PlayState(meatBonus.mamaAnim, "HoldingMeat");

            // เฟส 1: แม่หมูเลื่อนขึ้น-ลงนิดหน่อยพร้อมถือเนื้อ ขณะหมาป่าลอยขึ้น
            float prepT = 0f;
            while (prepT < 0.85f)
            {
                prepT += Time.deltaTime;
                for (int i = 0; i < wolves.Count; i++)
                {
                    var w = wolves[i];
                    w.root.transform.localPosition += Vector3.up * (0.58f * Time.deltaTime);
                }
                yield return null;
            }

            // เฟส 2: แม่หมูปาเนื้อวิถีโค้งทะลุหมาป่าทั้ง 3 ตัวทำคอมโบ 400 -> 800 -> 1,600!
            PlayState(meatBonus.mamaAnim, "Shoot");
            if (meatBonus.statusLabel != null) meatBonus.statusLabel.text = "2) ปาเนื้อวิถีโค้ง! สอยต่อเนื่อง Combo 400 -> 800 -> 1,600!";

            Vector3 startPos = meatBonus.firePoint != null
                ? meatBonus.stageRoot.InverseTransformPoint(meatBonus.firePoint.position)
                : new Vector3(1.55f, 0.20f, 0f);
            Vector3 endPos = new Vector3(-2.95f, -0.55f, 0f);

            if (meatObj != null)
            {
                meatObj.SetActive(true);
                meatObj.transform.localPosition = startPos;
                RestartParticles(meatObj);
            }

            int comboHit = 0;
            float throwT = 0f;
            const float throwDur = 1.35f;

            while (throwT < throwDur)
            {
                throwT += Time.deltaTime;
                float u = Mathf.Clamp01(throwT / throwDur);
                Vector3 meatPos = Vector3.Lerp(startPos, endPos, u);
                meatPos.y += Mathf.Sin(u * Mathf.PI) * 0.88f;

                if (meatObj != null)
                {
                    meatObj.transform.localPosition = meatPos;
                    meatObj.transform.Rotate(0f, 0f, 460f * Time.deltaTime);
                }

                for (int i = 0; i < wolves.Count; i++)
                {
                    var w = wolves[i];
                    if (!w.hit)
                    {
                        w.root.transform.localPosition += Vector3.up * (0.48f * Time.deltaTime);
                        // เช็คเนื้อพุ่งผ่านตัวหมาป่า!
                        if (meatPos.x <= w.root.transform.localPosition.x + 0.22f)
                        {
                            w.hit = true;
                            if (w.balloon != null) w.balloon.SetActive(false);
                            PlayState(w.anim, "Fall");
                            SpawnPopBurst(meatBonus.fxBalloonPopPrefab, meatBonus.stageRoot, w.root.transform.localPosition + Vector3.up * 0.35f, 0.28f);

                            int pts = comboPts[Mathf.Min(comboHit, comboPts.Length - 1)];
                            comboHit++;
                            if (meatBonus.popupScoreLabel != null)
                            {
                                meatBonus.popupScoreLabel.transform.localPosition = w.root.transform.localPosition + new Vector3(0f, 0.65f, 0f);
                                meatBonus.popupScoreLabel.text = $"+{pts:N0}! (×{comboHit})";
                            }
                            wolves[i] = w;
                        }
                    }
                    else
                    {
                        // หมาป่าร่วงลงด้านล่าง!
                        w.root.transform.localPosition += Vector3.down * (2.6f * Time.deltaTime);
                    }
                }
                yield return null;
            }

            if (meatObj != null) meatObj.SetActive(false);
            if (meatBonus.statusLabel != null) meatBonus.statusLabel.text = "3) 400 -> 800 -> 1,600 (สูตร 400 × 2^(n-1)) • เก็บครบ +10,000!";

            // ปล่อยให้หมาป่าร่วงพ้นพื้น
            float fallWait = 0f;
            while (fallWait < 0.75f)
            {
                fallWait += Time.deltaTime;
                for (int i = 0; i < wolves.Count; i++)
                {
                    if (wolves[i].hit)
                        wolves[i].root.transform.localPosition += Vector3.down * (2.8f * Time.deltaTime);
                }
                yield return null;
            }
        }
    }

    private IEnumerator RunFruitBonusDemoLoop()
    {
        // เตรียม Fruit.prefab จริง + Rainy Arrow จริง
        GameObject fruitObj = null;
        if (fruitBonus.fruitPrefab != null)
        {
            fruitObj = Instantiate(fruitBonus.fruitPrefab, fruitBonus.stageRoot);
            fruitObj.name = "LiveBonusFruit";
            StripGameplayOnly(fruitObj);
            fruitObj.transform.localScale *= 0.75f;
            SetMaskInside(fruitObj, 19);
            fruitObj.SetActive(false);
        }

        GameObject arrowObj = null;
        if (fruitBonus.arrowPrefab != null)
        {
            arrowObj = Instantiate(fruitBonus.arrowPrefab, fruitBonus.stageRoot);
            arrowObj.name = "LiveRainyArrow";
            StripGameplayOnly(arrowObj);
            arrowObj.transform.localScale *= 0.58f;
            SetMaskInside(arrowObj, 19);
            arrowObj.SetActive(false);
        }

        float bossX = (fruitBonus.bossMinX + fruitBonus.bossMaxX) * 0.5f;
        int bossDir = 1;
        int chainIdx = 0;

        PlayState(fruitBonus.bossWolfAnim, fruitBonus.bossRunState);

        while (true)
        {
            int fruitType = chainIdx == 0 ? 0 : (chainIdx == 1 ? 1 : 2); // Strawberry -> Cherry -> Peach ตาม BonusStageManager.GetFamicomFruitType()!
            int points = NakamotoScores[Mathf.Clamp(chainIdx, 0, NakamotoScores.Length - 1)];

            if (fruitBonus.statusLabel != null)
            {
                fruitBonus.statusLabel.text = $"บอสวิ่งยอดไม้ [Run] -> ทิ้ง {FruitNames[fruitType]} (Chain #{chainIdx + 1})";
            }

            // เฟส 1: บอสวิ่งบนยอดไม้สักครู่แล้วเล่นท่า ThrowFruit หย่อนผลไม้ลงมา
            float patrolT = 0f;
            while (patrolT < 0.72f)
            {
                patrolT += Time.deltaTime;
                StepBossPatrol(ref bossX, ref bossDir, Time.deltaTime);
                yield return null;
            }

            PlayState(fruitBonus.bossWolfAnim, fruitBonus.bossThrowState);
            float dropX = Mathf.Clamp(bossX + bossDir * 0.12f, fruitBonus.bossMinX + 0.15f, fruitBonus.bossMaxX - 0.15f);
            float dropY = fruitBonus.bossTopY - 0.25f;
            float hitY = -0.15f;

            if (fruitObj != null)
            {
                ConfigureBonusFruitVisual(fruitObj, fruitType);
                fruitObj.SetActive(true);
                fruitObj.transform.localPosition = new Vector3(dropX, dropY, 0f);
                RestartParticles(fruitObj);
            }

            // แม่หมูเลื่อนกระเช้ามาเล็งระดับเดียวกับจุดยิงผลไม้
            Vector3 mamaBase = fruitBonus.mamaBasketRig != null ? fruitBonus.mamaBasketRig.localPosition : new Vector3(1.85f, 0f, 0f);

            // เฟส 2: ผลไม้หล่นลงมา + แม่หมูยิงธนู Rainy Arrow สอยกลางอากาศ!
            bool arrowFired = false;
            bool hit = false;
            float arrowX = 1.55f;
            float fruitY = dropY;
            float elapsed = 0f;

            while (elapsed < 1.35f)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= 0.28f) PlayState(fruitBonus.bossWolfAnim, fruitBonus.bossRunState);
                StepBossPatrol(ref bossX, ref bossDir, Time.deltaTime);

                if (fruitBonus.mamaBasketRig != null)
                {
                    float targetMY = Mathf.Lerp(mamaBase.y, hitY, Mathf.Clamp01(elapsed / 0.35f));
                    fruitBonus.mamaBasketRig.localPosition = new Vector3(mamaBase.x, targetMY, 0f);
                }

                if (!hit)
                {
                    fruitY -= 1.48f * Time.deltaTime;
                    if (fruitObj != null)
                    {
                        fruitObj.transform.localPosition = new Vector3(dropX, fruitY, 0f);
                        fruitObj.transform.Rotate(0f, 0f, 180f * Time.deltaTime);
                    }

                    // ยิงลูกธนูออกมาดักให้โดนตรง hitY พอดี!
                    if (!arrowFired && fruitY <= hitY + 0.42f)
                    {
                        arrowFired = true;
                        PlayState(fruitBonus.mamaAnim, "Shoot");
                        arrowX = fruitBonus.firePoint != null
                            ? fruitBonus.stageRoot.InverseTransformPoint(fruitBonus.firePoint.position).x
                            : 1.42f;
                        if (arrowObj != null)
                        {
                            arrowObj.SetActive(true);
                            arrowObj.transform.localPosition = new Vector3(arrowX, hitY, 0f);
                            RestartParticles(arrowObj);
                        }
                    }

                    if (arrowFired && arrowObj != null)
                    {
                        arrowX -= 8.5f * Time.deltaTime;
                        arrowObj.transform.localPosition = new Vector3(arrowX, hitY, 0f);

                        if (arrowX <= dropX + 0.16f)
                        {
                            hit = true;
                            if (fruitObj != null) fruitObj.SetActive(false);
                            if (arrowObj != null) arrowObj.SetActive(false);
                            SpawnPopBurst(fruitBonus.fxPopSparklePrefab, fruitBonus.stageRoot, new Vector3(dropX, fruitY, 0f), 0.30f);

                            string tag = chainIdx >= 7 ? " [NAKAMOTO!]" : "";
                            if (fruitBonus.popupScoreLabel != null)
                            {
                                fruitBonus.popupScoreLabel.transform.localPosition = new Vector3(dropX, fruitY + 0.35f, 0f);
                                fruitBonus.popupScoreLabel.text = $"+{points:N0}{tag}";
                            }
                            if (fruitBonus.statusLabel != null)
                            {
                                fruitBonus.statusLabel.text = $"โดน {FruitNames[fruitType]}! Nakamoto Chain #{chainIdx + 1} = +{points:N0}{tag}";
                            }
                        }
                    }
                }
                yield return null;
            }

            if (fruitObj != null) fruitObj.SetActive(false);
            if (arrowObj != null) arrowObj.SetActive(false);

            chainIdx = (chainIdx + 1) % NakamotoScores.Length;
            PlayState(fruitBonus.mamaAnim, "Idle");
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void StepBossPatrol(ref float bossX, ref int bossDir, float dt)
    {
        if (fruitBonus.bossWolfSr == null) return;
        bossX += bossDir * 1.35f * dt;
        if (bossX >= fruitBonus.bossMaxX)
        {
            bossX = fruitBonus.bossMaxX;
            bossDir = -1;
        }
        else if (bossX <= fruitBonus.bossMinX)
        {
            bossX = fruitBonus.bossMinX;
            bossDir = 1;
        }
        fruitBonus.bossWolfSr.transform.localPosition = new Vector3(bossX, fruitBonus.bossTopY, 0f);
        fruitBonus.bossWolfSr.flipX = (bossDir < 0);
    }

    private void ConfigureBonusFruitVisual(GameObject fruitObj, int typeIdx)
    {
        if (fruitObj == null) return;
        Sprite spr = (fruitBonus.fruitSprites != null && typeIdx < fruitBonus.fruitSprites.Length)
            ? fruitBonus.fruitSprites[typeIdx]
            : null;
        Color glow = FruitGlowColors[Mathf.Clamp(typeIdx, 0, FruitGlowColors.Length - 1)];

        foreach (var sr in fruitObj.GetComponentsInChildren<SpriteRenderer>(true))
        {
            string n = sr.gameObject.name.ToLowerInvariant();
            if (n.Contains("glow") || n.Contains("aura"))
            {
                sr.color = glow;
            }
            else if (spr != null)
            {
                sr.sprite = spr;
                sr.color = Color.white;
            }
        }
        foreach (var ps in fruitObj.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
            main.startColor = glow;
        }
    }

    private void TriggerPerfectBonusBannerDemo()
    {
        if (perfectBannerCoroutine != null) StopCoroutine(perfectBannerCoroutine);
        perfectBannerCoroutine = StartCoroutine(ShowPerfectBannerRoutine());
    }

    private IEnumerator ShowPerfectBannerRoutine()
    {
        SetPerfectBanner(meatBonus.perfectBannerRoot, meatBonus.perfectBannerLabel, true, "PERFECT BONUS\n10000 PT");
        SetPerfectBanner(fruitBonus.perfectBannerRoot, fruitBonus.perfectBannerLabel, true, "PERFECT BONUS\n10000 PT");
        yield return new WaitForSeconds(1.6f);
        SetPerfectBanner(meatBonus.perfectBannerRoot, meatBonus.perfectBannerLabel, true, "STAGE 3\nWINTER");
        SetPerfectBanner(fruitBonus.perfectBannerRoot, fruitBonus.perfectBannerLabel, true, "STAGE 5\nSUMMER");
        yield return new WaitForSeconds(1.25f);
        SetPerfectBanner(meatBonus.perfectBannerRoot, meatBonus.perfectBannerLabel, false, "");
        SetPerfectBanner(fruitBonus.perfectBannerRoot, fruitBonus.perfectBannerLabel, false, "");
        perfectBannerCoroutine = null;
    }

    private static void SetPerfectBanner(GameObject root, TextMesh lbl, bool active, string txt)
    {
        if (root != null) root.SetActive(active);
        if (lbl != null && active) lbl.text = txt;
    }

    // =========================================================================================
    // HELPERS
    // =========================================================================================
    private static bool PlayState(Animator anim, string stateName)
    {
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName)) return false;
        int hash = Animator.StringToHash(stateName);
        if (anim.HasState(0, hash))
        {
            anim.Play(hash, 0, 0f);
            return true;
        }
        return false;
    }

    private static void StripGameplayOnly(GameObject go)
    {
        if (go == null) return;
        foreach (var col in go.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            if (mb is ArrowTrailEffect || mb is MeatAuraEffect) continue;
            mb.enabled = false;
        }
    }

    private static void SetMaskInside(GameObject go, int minSortOrder)
    {
        if (go == null) return;
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (sr.sortingOrder < minSortOrder) sr.sortingOrder = minSortOrder;
        }
        foreach (var psr in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            psr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (psr.sortingOrder < minSortOrder + 1) psr.sortingOrder = minSortOrder + 1;
        }
        foreach (var tr in go.GetComponentsInChildren<TrailRenderer>(true))
        {
            if (tr.sortingOrder < minSortOrder) tr.sortingOrder = minSortOrder;
        }
    }

    private static void RestartParticles(GameObject go)
    {
        if (go == null) return;
        foreach (var tr in go.GetComponentsInChildren<TrailRenderer>(true)) tr.Clear();
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void SpawnPopBurst(GameObject prefab, Transform parent, Vector3 localPos, float scale)
    {
        if (prefab == null || parent == null) return;
        GameObject fx = Instantiate(prefab, parent);
        fx.transform.localPosition = localPos;
        fx.transform.localScale = Vector3.one * scale;
        StripGameplayOnly(fx);
        SetMaskInside(fx, 21);
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ps.Play(true);
        }
        Destroy(fx, 1.2f);
    }

    // =========================================================================================
    // MODERN FLAT DARK STUDIO BOTTOM CONTROL BAR
    // =========================================================================================
    private void EnsureToolbarStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        texBarBg = MakeBorderedTex(new Color(0.075f, 0.10f, 0.155f, 0.96f), new Color(0.24f, 0.34f, 0.50f, 1f));
        texBtnNormal = MakeBorderedTex(new Color(0.14f, 0.18f, 0.26f, 1f), new Color(0.28f, 0.36f, 0.50f, 1f));
        texBtnHover = MakeBorderedTex(new Color(0.20f, 0.28f, 0.42f, 1f), new Color(0.46f, 0.64f, 0.88f, 1f));
        texBtnActive = MakeBorderedTex(new Color(0.16f, 0.45f, 0.66f, 1f), new Color(0.48f, 0.88f, 1f, 1f));
        texBtnAccent = MakeBorderedTex(new Color(0.56f, 0.34f, 0.12f, 1f), new Color(1f, 0.78f, 0.32f, 1f));

        styleBarBg = new GUIStyle
        {
            normal = { background = texBarBg },
            border = new RectOffset(2, 2, 2, 2)
        };

        styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.86f, 0.92f, 1f, 1f) }
        };

        styleBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(6, 6, 3, 3)
        };
        styleBtn.normal.background = texBtnNormal;
        styleBtn.normal.textColor = new Color(0.92f, 0.96f, 1f, 1f);
        styleBtn.hover.background = texBtnHover;
        styleBtn.hover.textColor = Color.white;
        styleBtn.active.background = texBtnActive;
        styleBtn.active.textColor = Color.white;

        styleBtnActive = new GUIStyle(styleBtn);
        styleBtnActive.normal.background = texBtnActive;
        styleBtnActive.normal.textColor = Color.white;
        styleBtnActive.hover.background = texBtnActive;

        styleBtnAccent = new GUIStyle(styleBtn);
        styleBtnAccent.normal.background = texBtnAccent;
        styleBtnAccent.normal.textColor = Color.white;
        styleBtnAccent.hover.background = texBtnHover;
    }

    private Texture2D MakeBorderedTex(Color fill, Color border)
    {
        const int sz = 16;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color[] px = new Color[sz * sz];
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                px[y * sz + x] = (x == 0 || y == 0 || x == sz - 1 || y == sz - 1) ? border : fill;
        tex.SetPixels(px);
        tex.Apply();
        createdTextures.Add(tex);
        return tex;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        EnsureToolbarStyles();

        float barW = Mathf.Min(Screen.width - 24f, 1320f);
        float barX = (Screen.width - barW) * 0.5f;
        Rect barRect = new Rect(barX, Screen.height - 62f, barW, 50f);

        GUI.Box(barRect, GUIContent.none, styleBarBg);

        GUILayout.BeginArea(new Rect(barRect.x + 10f, barRect.y + 10f, barRect.width - 20f, 30f));
        GUILayout.BeginHorizontal();

        GUILayout.Label("Stage Flow:", styleLabel, GUILayout.Width(72f), GUILayout.Height(28f));
        if (GUILayout.Button("◀ Prev", styleBtn, GUILayout.Width(62f), GUILayout.Height(28f))) { autoCycleStages = false; StepFlowBackward(); }
        if (GUILayout.Button("Next ▶", styleBtn, GUILayout.Width(62f), GUILayout.Height(28f))) { autoCycleStages = false; StepFlowForward(); }
        if (GUILayout.Button(autoCycleStages ? "Auto: ON" : "Auto: OFF", autoCycleStages ? styleBtnActive : styleBtn, GUILayout.Width(74f), GUILayout.Height(28f)))
        {
            autoCycleStages = !autoCycleStages;
        }

        GUILayout.Space(8f);
        string[] stepLabels = { "1:Aut", "2:Spr", "★Meat", "3:Win", "4:Rain", "★Fruit", "5:Sum", "L2:Even", "L2:Odd" };
        for (int i = 0; i < stepLabels.Length; i++)
        {
            GUIStyle st = (flowStepIndex == i) ? styleBtnActive : styleBtn;
            if (GUILayout.Button(stepLabels[i], st, GUILayout.Height(28f)))
            {
                autoCycleStages = false;
                ApplyFlowStep(i, playTransition: true);
            }
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("↻ Replay 5 Cutscenes", styleBtnAccent, GUILayout.Width(148f), GUILayout.Height(28f)))
        {
            StartAllCutscenes();
        }
        if (GUILayout.Button("★ NES Perfect Banner", styleBtnAccent, GUILayout.Width(152f), GUILayout.Height(28f)))
        {
            TriggerPerfectBonusBannerDemo();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
