using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShowcaseDemoType
{
    StaticPose,
    AnimLoop,
    MamaIdleGlide,
    MamaShootArrow,
    MamaThrowMeat,
    MamaFallBounce,
    PigletRopePull,
    WolfThrowRock,
    ParticleReplay,
    BalloonColorCycle,
    WolfFormationSingle,
    WolfFormationStack,
    WolfFormationDiagonal,
    WolfFormationHorizontal,
    WolfDirectionOddEven,
    WolfLadderAndBoulder,
    MiniRainBooth,
    MamaShootPopWolf,
    MamaThrowMeatWolves,
    MamaHitByRockDeath,
    BasketArmorBlockDemo,
    MamaArrowStickGroundDemo,
    PigletElevatorStage,
    WolfBalloonDurabilityCompare,
    BonusBossFruitThrow,
    WolfOddEvenCycleStage,
    ShaderPulsePreview,
    VfxWolfBalloonPop,
    VfxWolfRockBreak,
    VfxWolfRockHitBasket,
    VfxWolfFallGroundDust,
    VfxBossFruitThrow,
    VfxMamaWeaponStream
}

/// <summary>
/// ตัวขับเคลื่อนเดโมประจำแต่ละบูธใน Scene Portfolio Showcase (ทำงานเฉพาะใน Scene โชว์ผลงาน):
/// - สาธิตระบบแม่หมู (Idle, Shoot ปล่อย Arrow จริง, HoldingMeat ปาเนื้อวิถีโค้ง, Fall เด้งตกกระเช้า, ลูกหมูดึงเชือกโค้ง + ฝุ่นรอก)
/// - สาธิตระบบเกิดและรูปแบบการโดดของหมาป่าทั้ง 4 แพตเทิร์น (Single, Stack, Diagonal, Horizontal Sync), Odd/Even + Multi-HP, Ladder/Boulder
/// - วนลูป Particle VFX ทุกชิ้นต่อเนื่อง (ทั้ง Looping และ One-Shot Burst) โดยจำกัดขอบเขตให้อยู่ในช่องของตัวเอง
/// </summary>
[ExecuteAlways]
public class ShowcaseStationDemo : MonoBehaviour
{
    [Header("Demo Type")]
    public ShowcaseDemoType demoType = ShowcaseDemoType.AnimLoop;

    [Header("Animator & State")]
    public Animator targetAnimator;
    public Animator secondaryAnimator;
    public string primaryStateName = "Idle";
    public string secondaryStateName = "";
    public float actionInterval = 1.6f;
    public float actionHoldDuration = 0.28f;

    [Header("Visual / Prefab References")]
    public Transform actorTransform;
    public Transform spawnPoint;
    public GameObject projectilePrefab;
    public GameObject heldVisualObject;
    public GameObject indicatorObject;
    public SpriteRenderer tintSpriteRenderer;

    [Header("Wolf Formation Mini-Stage References")]
    public Sprite wolfDemoSprite;
    public Sprite balloonDemoSprite;
    public RuntimeAnimatorController wolfDemoController;
    public Sprite boulderDemoSprite;
    public Transform formationRoot;

    [Header("Rope Demo References")]
    public LineRenderer ropeLineRenderer;
    public Transform ropeGondolaTransform;
    public Transform ropeHandTransform;
    public ParticleSystem pulleyDustFX;
    public float ropePulleyLocalY = 1.15f;
    public float ropeCornerRadius = 0.24f;
    public int ropeCornerSegments = 6;
    public float elevatorMinY = -0.45f;
    public float elevatorMaxY = 1.05f;
    public float ropeGondolaAnchorOffsetY = 0.68f;

    [Header("Particle Demo References")]
    public GameObject particlePrefab;
    public Transform particleMount;
    public Color particleTintColor = new Color(1f, 0.35f, 0.55f, 1f);
    public bool spawnRingAndStar = false;
    public bool isContinuousLoopingParticle = false;
    public bool orbitTrailDemo = false;
    public bool multiChildOrbit = false;
    public Vector2 orbitRadii = new Vector2(0.68f, 0.34f);

    [Header("Strict Particle Containment (ตัดอนุภาคไม่ให้เลยกรอบ)")]
    public bool clampParticlesToBounds = false;
    public Vector2 particleClipHalfSize = new Vector2(2.15f, 1.15f);

    [Header("Scene 03 Authentic Gameplay VFX Mini-Scenarios")]
    public bool multiHitBalloon = false;
    public bool isParabolicMeatThrow = false;
    public Material balloonShaderMat;
    public GameObject fxArrowWindPrefab;
    public GameObject[] seasonalProjectiles;
    public string[] seasonalLabels;
    private int seasonalIndex;

    [Header("Scene 01 & 02 Live Diorama References")]
    public Transform mamaBodyTransform;
    public Transform topArmorTransform;
    public Transform bottomArmorTransform;
    public GameObject rockPrefab;
    public GameObject fxBalloonPopPrefab;
    public GameObject fxBalloonHitPrefab;
    public GameObject fxRockHitBasketPrefab;
    public GameObject fxRockBreakPrefab;
    public GameObject fxGroundDustPrefab;
    public GameObject fxWolfBiteLadderPrefab;
    public RuntimeAnimatorController wolfPushController;
    public Sprite wolfWaitLadderSprite;
    public Sprite wolfPushIdleSprite;
    public GameObject fruitPrefab;
    public Sprite[] realFruitSprites;
    public TextMesh statusLabel;
    public float dioramaScaleRatio = 0.72f;
    public float dioramaFloorLocalY = -1.18f;

    private float timer;
    private float elapsed;
    private Vector3 initialActorLocalPos;
    private Vector3 initialMamaBodyLocalPos;
    private Vector3 initialIndicatorScale = Vector3.one;
    private bool hasSavedInitialPos;
    private Coroutine activeRoutine;
    private Material runtimeRopeMat;
    private float ropeUvOffset;
    private int colorIndex;
    private bool oddEvenToggle = true;

    private readonly List<GameObject> spawnedTemps = new();
    private readonly List<ParticleSystem> cachedBoundParticles = new();
    private static ParticleSystem.Particle[] sharedParticleBuf = new ParticleSystem.Particle[512];
    private Transform trailMover;

    private static readonly Color[] BalloonShowcasePalette =
    {
        new Color(1f, 0.25f, 0.25f, 1f),   // Pink/Red 1 HP
        new Color(1f, 0.82f, 0.16f, 1f),   // Yellow 2 HP
        new Color(0.28f, 0.85f, 1f, 1f),   // Blue 3 HP
        new Color(1f, 0.30f, 0.75f, 1f),   // Boss Pink/Magenta
        new Color(0.55f, 1f, 0.28f, 1f)    // Green Empty Balloon
    };

    private void OnEnable()
    {
        if (actorTransform != null && !hasSavedInitialPos)
        {
            initialActorLocalPos = actorTransform.localPosition;
            if (mamaBodyTransform != null) initialMamaBodyLocalPos = mamaBodyTransform.localPosition;
            if (indicatorObject != null) initialIndicatorScale = indicatorObject.transform.localScale;
            else initialIndicatorScale = actorTransform.localScale;
            hasSavedInitialPos = true;
        }

        if (!Application.isPlaying)
        {
            ApplyEditModePreview();
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        if (actorTransform != null && !hasSavedInitialPos)
        {
            initialActorLocalPos = actorTransform.localPosition;
            if (mamaBodyTransform != null) initialMamaBodyLocalPos = mamaBodyTransform.localPosition;
            if (indicatorObject != null) initialIndicatorScale = indicatorObject.transform.localScale;
            else initialIndicatorScale = actorTransform.localScale;
            hasSavedInitialPos = true;
        }

        if (ropeLineRenderer != null && ropeLineRenderer.sharedMaterial != null)
        {
            runtimeRopeMat = new Material(ropeLineRenderer.sharedMaterial);
            ropeLineRenderer.material = runtimeRopeMat;
        }

        PlayAnimatorSafe(targetAnimator, primaryStateName);
        if (secondaryAnimator != null && !string.IsNullOrEmpty(primaryStateName))
        {
            PlayAnimatorSafe(secondaryAnimator, primaryStateName);
        }

        timer = 0.25f;

        if (demoType == ShowcaseDemoType.ParticleReplay)
        {
            if (orbitTrailDemo && !multiChildOrbit && particleMount != null && particleMount.childCount > 0)
            {
                trailMover = particleMount.GetChild(0);
            }
            RefreshBoundParticleCache();
            TriggerParticleReplay();
        }
        else if (demoType == ShowcaseDemoType.MiniRainBooth)
        {
            BuildMiniRainSystem();
            RefreshBoundParticleCache();
        }
        else if (IsFormationDemo(demoType))
        {
            TriggerFormationWave();
        }
        else if (IsScene01Diorama(demoType))
        {
            TriggerScene01Diorama();
        }
        else if (IsVfxScenarioDemo(demoType))
        {
            TriggerVfxScenario();
        }
    }

    private void OnDisable()
    {
        CleanupSpawnedTemps();
    }

    private void OnDestroy()
    {
        CleanupSpawnedTemps();
        if (runtimeRopeMat != null)
        {
            Destroy(runtimeRopeMat);
        }
    }

    private static bool IsFormationDemo(ShowcaseDemoType t)
    {
        return t == ShowcaseDemoType.WolfFormationSingle
            || t == ShowcaseDemoType.WolfFormationStack
            || t == ShowcaseDemoType.WolfFormationDiagonal
            || t == ShowcaseDemoType.WolfFormationHorizontal
            || t == ShowcaseDemoType.WolfDirectionOddEven
            || t == ShowcaseDemoType.WolfLadderAndBoulder
            || t == ShowcaseDemoType.WolfBalloonDurabilityCompare
            || t == ShowcaseDemoType.BonusBossFruitThrow
            || t == ShowcaseDemoType.WolfOddEvenCycleStage;
    }

    private static bool IsScene01Diorama(ShowcaseDemoType t)
    {
        return t == ShowcaseDemoType.MamaShootPopWolf
            || t == ShowcaseDemoType.MamaThrowMeatWolves
            || t == ShowcaseDemoType.MamaHitByRockDeath
            || t == ShowcaseDemoType.BasketArmorBlockDemo
            || t == ShowcaseDemoType.MamaArrowStickGroundDemo;
    }

    private static bool IsVfxScenarioDemo(ShowcaseDemoType t)
    {
        return t == ShowcaseDemoType.VfxWolfBalloonPop
            || t == ShowcaseDemoType.VfxWolfRockBreak
            || t == ShowcaseDemoType.VfxWolfRockHitBasket
            || t == ShowcaseDemoType.VfxWolfFallGroundDust
            || t == ShowcaseDemoType.VfxBossFruitThrow
            || t == ShowcaseDemoType.VfxMamaWeaponStream;
    }

    private void ApplyEditModePreview()
    {
        if (demoType == ShowcaseDemoType.PigletRopePull || demoType == ShowcaseDemoType.PigletElevatorStage)
        {
            UpdateRopeVisual(0f);
        }

        if (targetAnimator != null && !string.IsNullOrEmpty(primaryStateName))
        {
            PlayAnimatorSafe(targetAnimator, primaryStateName);
        }
    }

    public void RefreshEditModeRopePreview()
    {
        if (actorTransform != null && !hasSavedInitialPos)
        {
            initialActorLocalPos = actorTransform.localPosition;
            hasSavedInitialPos = true;
        }
        UpdateRopeVisual(0f);
    }

    public void TriggerNow()
    {
        if (!Application.isPlaying) return;
        timer = 0f;
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (demoType == ShowcaseDemoType.PigletRopePull || demoType == ShowcaseDemoType.PigletElevatorStage)
            {
                UpdateRopeVisual(0f);
            }
            return;
        }

        elapsed += Time.deltaTime;

        switch (demoType)
        {
            case ShowcaseDemoType.AnimLoop:
                EnsureStatePlaying(targetAnimator, primaryStateName);
                break;

            case ShowcaseDemoType.MamaIdleGlide:
                EnsureStatePlaying(targetAnimator, primaryStateName);
                if (actorTransform != null)
                {
                    float offsetY = Mathf.Sin(elapsed * 2.0f) * 0.42f;
                    actorTransform.localPosition = initialActorLocalPos + new Vector3(0f, offsetY, 0f);
                }
                break;

            case ShowcaseDemoType.MamaShootArrow:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(0.75f, actionInterval);
                    RestartRoutine(MamaShootRoutine());
                }
                break;

            case ShowcaseDemoType.MamaThrowMeat:
                if (indicatorObject != null && indicatorObject.activeSelf)
                {
                    float pulse = 1f + Mathf.Sin(elapsed * 5f) * 0.14f;
                    indicatorObject.transform.localScale = Vector3.one * (0.45f * pulse);
                }
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(1.3f, actionInterval);
                    RestartRoutine(MamaMeatRoutine());
                }
                break;

            case ShowcaseDemoType.MamaFallBounce:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(1.8f, actionInterval);
                    RestartRoutine(MamaFallRoutine());
                }
                break;

            case ShowcaseDemoType.PigletRopePull:
                UpdateRopeDemo();
                break;

            case ShowcaseDemoType.WolfThrowRock:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(1.2f, actionInterval);
                    RestartRoutine(WolfThrowRockRoutine());
                }
                break;

            case ShowcaseDemoType.ParticleReplay:
                if (multiChildOrbit && particleMount != null)
                {
                    int n = particleMount.childCount;
                    for (int idx = 0; idx < n; idx++)
                    {
                        Transform ch = particleMount.GetChild(idx);
                        if (ch == null) continue;
                        float phase = (Mathf.PI * 2f * idx) / Mathf.Max(1, n);
                        float ang = elapsed * 3.1f + phase;
                        ch.localPosition = new Vector3(Mathf.Cos(ang) * orbitRadii.x, Mathf.Sin(ang) * orbitRadii.y, 0f);
                    }
                }
                else if (orbitTrailDemo && trailMover != null)
                {
                    // หมุนวนเป็นวงรีขนาดพอดีในกรอบบูธเพื่อให้เห็น TrailRenderer / หางลม โดยไม่ล้นออกนอกช่อง
                    float ang = elapsed * 3.4f;
                    trailMover.localPosition = new Vector3(Mathf.Cos(ang) * orbitRadii.x, Mathf.Sin(ang) * orbitRadii.y, 0f);
                }

                if (runtimeRopeMat != null)
                {
                    ropeUvOffset -= Time.deltaTime * 1.15f;
                    runtimeRopeMat.mainTextureOffset = new Vector2(ropeUvOffset, 0f);
                }

                if (actorTransform != null && targetAnimator == null)
                {
                    float pulse = 0.90f + Mathf.Sin(elapsed * 3.6f) * 0.12f;
                    actorTransform.localScale = initialIndicatorScale * pulse;
                }
                else if (targetAnimator != null && !string.IsNullOrEmpty(primaryStateName))
                {
                    EnsureStatePlaying(targetAnimator, primaryStateName);
                }

                if (!isContinuousLoopingParticle)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        timer = Mathf.Max(0.95f, actionInterval);
                        if (tintSpriteRenderer != null)
                        {
                            colorIndex = (colorIndex + 1) % BalloonShowcasePalette.Length;
                            tintSpriteRenderer.color = BalloonShowcasePalette[colorIndex];
                            particleTintColor = BalloonShowcasePalette[colorIndex];
                        }
                        TriggerParticleReplay();
                    }
                }
                else
                {
                    EnsureLoopingParticlesPlaying();
                }
                break;

            case ShowcaseDemoType.ShaderPulsePreview:
                if (actorTransform != null)
                {
                    float pulse = 0.72f + Mathf.PingPong(elapsed * 1.15f, 0.45f);
                    actorTransform.localScale = initialIndicatorScale * pulse;
                }
                if (runtimeRopeMat != null)
                {
                    ropeUvOffset -= Time.deltaTime * 1.25f;
                    runtimeRopeMat.mainTextureOffset = new Vector2(ropeUvOffset, 0f);
                }
                break;

            case ShowcaseDemoType.BalloonColorCycle:
                if (actorTransform != null)
                {
                    float sx = 1f + Mathf.Sin(elapsed * 4.6f) * 0.06f;
                    float sy = 1f - Mathf.Sin(elapsed * 4.6f) * 0.08f;
                    actorTransform.localScale = new Vector3(sx, sy, 1f);
                }
                timer -= Time.deltaTime;
                if (timer <= 0f && tintSpriteRenderer != null)
                {
                    timer = Mathf.Max(0.8f, actionInterval);
                    colorIndex = (colorIndex + 1) % BalloonShowcasePalette.Length;
                    tintSpriteRenderer.color = BalloonShowcasePalette[colorIndex];
                }
                break;

            case ShowcaseDemoType.WolfFormationSingle:
            case ShowcaseDemoType.WolfFormationStack:
            case ShowcaseDemoType.WolfFormationDiagonal:
            case ShowcaseDemoType.WolfFormationHorizontal:
            case ShowcaseDemoType.WolfDirectionOddEven:
            case ShowcaseDemoType.WolfLadderAndBoulder:
            case ShowcaseDemoType.WolfBalloonDurabilityCompare:
            case ShowcaseDemoType.WolfOddEvenCycleStage:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(2.8f, actionInterval);
                    TriggerFormationWave();
                }
                break;

            case ShowcaseDemoType.BonusBossFruitThrow:
                UpdateBonusBossPatrolAndThrow();
                break;

            case ShowcaseDemoType.MamaShootPopWolf:
            case ShowcaseDemoType.MamaThrowMeatWolves:
            case ShowcaseDemoType.MamaHitByRockDeath:
            case ShowcaseDemoType.BasketArmorBlockDemo:
            case ShowcaseDemoType.MamaArrowStickGroundDemo:
                if (indicatorObject != null && indicatorObject.activeSelf)
                {
                    float pulse = 1f + Mathf.Sin(elapsed * 5f) * 0.12f;
                    indicatorObject.transform.localScale = initialIndicatorScale * pulse;
                }
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(3.2f, actionInterval);
                    TriggerScene01Diorama();
                }
                break;

            case ShowcaseDemoType.PigletElevatorStage:
                UpdatePigletElevatorStage();
                break;

            case ShowcaseDemoType.VfxWolfBalloonPop:
            case ShowcaseDemoType.VfxWolfRockBreak:
            case ShowcaseDemoType.VfxWolfRockHitBasket:
            case ShowcaseDemoType.VfxWolfFallGroundDust:
            case ShowcaseDemoType.VfxBossFruitThrow:
            case ShowcaseDemoType.VfxMamaWeaponStream:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = Mathf.Max(1.35f, actionInterval);
                    TriggerVfxScenario();
                }
                break;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (clampParticlesToBounds && particleMount != null)
        {
            EnforceParticleBounds();
        }
    }

    private void RefreshBoundParticleCache()
    {
        cachedBoundParticles.Clear();
        if (particleMount == null) return;
        particleMount.GetComponentsInChildren(true, cachedBoundParticles);
    }

    private void EnforceParticleBounds()
    {
        if (cachedBoundParticles.Count == 0)
        {
            RefreshBoundParticleCache();
        }

        float maxX = Mathf.Max(0.25f, particleClipHalfSize.x);
        float maxY = Mathf.Max(0.25f, particleClipHalfSize.y);

        for (int s = 0; s < cachedBoundParticles.Count; s++)
        {
            var ps = cachedBoundParticles[s];
            if (ps == null || !ps.gameObject.activeInHierarchy) continue;

            int alive = ps.particleCount;
            if (alive <= 0) continue;
            if (sharedParticleBuf.Length < alive)
            {
                sharedParticleBuf = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(alive)];
            }

            int count = ps.GetParticles(sharedParticleBuf);
            if (count <= 0) continue;

            bool isWorldSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.World;
            Transform psTr = ps.transform;
            bool modified = false;

            for (int i = 0; i < count; i++)
            {
                Vector3 worldPos = isWorldSpace
                    ? sharedParticleBuf[i].position
                    : psTr.TransformPoint(sharedParticleBuf[i].position);
                Vector3 localInMount = particleMount.InverseTransformPoint(worldPos);

                if (localInMount.x < -maxX || localInMount.x > maxX || localInMount.y < -maxY || localInMount.y > maxY)
                {
                    sharedParticleBuf[i].remainingLifetime = 0f;
                    modified = true;
                }
            }

            if (modified)
            {
                ps.SetParticles(sharedParticleBuf, count);
            }
        }
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(routine);
    }

    // =========================================================================================
    // SCENE 01 LIVE DIORAMAS (5 FULL INTERACTIVE LOOPS FOR MAMA PIG & PIGLETS)
    // =========================================================================================
    private void TriggerScene01Diorama()
    {
        CleanupSpawnedTemps();
        if (actorTransform != null) actorTransform.localPosition = initialActorLocalPos;
        if (mamaBodyTransform != null) mamaBodyTransform.localPosition = initialMamaBodyLocalPos;
        PlayAnimatorSafe(targetAnimator, primaryStateName);

        switch (demoType)
        {
            case ShowcaseDemoType.MamaShootPopWolf:
                RestartRoutine(DioramaShootPopWolfRoutine());
                break;
            case ShowcaseDemoType.MamaThrowMeatWolves:
                RestartRoutine(DioramaThrowMeatWolvesRoutine());
                break;
            case ShowcaseDemoType.MamaHitByRockDeath:
                RestartRoutine(DioramaHitByRockDeathRoutine());
                break;
            case ShowcaseDemoType.BasketArmorBlockDemo:
                RestartRoutine(DioramaBasketArmorBlockRoutine());
                break;
            case ShowcaseDemoType.MamaArrowStickGroundDemo:
                RestartRoutine(DioramaArrowStickGroundRoutine());
                break;
        }
    }

    private GameObject SpawnDioramaWolf(Vector3 localPos, Color balloonColor, out Animator wolfAnim, out GameObject balloonObj)
    {
        GameObject root = new GameObject("DioramaWolf");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = localPos;
        spawnedTemps.Add(root);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = wolfDemoSprite;
        sr.sortingOrder = 16;
        FitSpriteScale(body.transform, wolfDemoSprite, 1.05f, 1.15f);

        wolfAnim = body.AddComponent<Animator>();
        if (wolfDemoController != null) wolfAnim.runtimeAnimatorController = wolfDemoController;
        PlayAnimatorSafe(wolfAnim, "Drop");

        balloonObj = new GameObject("Balloon");
        balloonObj.transform.SetParent(root.transform, false);
        balloonObj.transform.localPosition = new Vector3(-0.04f, 0.84f, 0f);
        var bsr = balloonObj.AddComponent<SpriteRenderer>();
        bsr.sprite = balloonDemoSprite;
        bsr.color = balloonColor;
        bsr.sortingOrder = 17;
        FitSpriteScale(balloonObj.transform, balloonDemoSprite, 0.62f, 0.72f);

        return root;
    }

    private void SpawnDioramaFX(GameObject fxPrefab, Vector3 worldPos, float scale = 0.85f, float life = 1.4f)
    {
        if (fxPrefab == null) return;
        GameObject fx = Instantiate(fxPrefab, worldPos, Quaternion.identity, transform);
        StripGameplayPhysics(fx);
        fx.transform.localScale *= scale;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.playOnAwake = true;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sortingOrder < 22) psr.sortingOrder = 22;
            ps.Play(true);
        }
        spawnedTemps.Add(fx);
        Destroy(fx, life);
    }

    private IEnumerator DropWolfToFloorRoutine(GameObject wolf, Animator anim, float startY)
    {
        if (wolf == null) yield break;
        PlayAnimatorSafe(anim, "Fall");
        float vy = 0.8f;
        const float grav = 14.5f;
        while (wolf != null && wolf.transform.localPosition.y > dioramaFloorLocalY)
        {
            float dt = Time.deltaTime;
            vy -= grav * dt;
            Vector3 p = wolf.transform.localPosition;
            p.y = Mathf.Max(dioramaFloorLocalY, p.y + vy * dt);
            wolf.transform.localPosition = p;
            yield return null;
        }
        if (wolf != null)
        {
            SpawnDioramaFX(fxGroundDustPrefab, wolf.transform.position + new Vector3(0f, -0.22f, 0f), 0.75f, 1.1f);
            PlayAnimatorSafe(anim, "Death");
        }
    }

    private IEnumerator FlashArmorRoutine(Transform armor)
    {
        if (armor == null) yield break;
        var sr = armor.GetComponentInChildren<SpriteRenderer>();
        Vector3 origScale = armor.localScale;
        Color origColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = new Color(1f, 0.94f, 0.55f, 1f);
        armor.localScale = origScale * 1.12f;
        yield return new WaitForSeconds(0.14f);
        if (sr != null) sr.color = origColor;
        if (armor != null) armor.localScale = origScale;
    }

    // ช่อง 1: หมาป่าห้อยลงมา -> แม่หมูเล็งยิงธนู -> ลูกโป่งแตก (BalloonPopFX) -> หมาป่าร่วงพื้น
    private IEnumerator DioramaShootPopWolfRoutine()
    {
        if (actorTransform == null) yield break;

        Vector3 wolfStart = new Vector3(-2.35f, 1.65f, 0f);
        GameObject wolf = SpawnDioramaWolf(wolfStart, new Color(1f, 0.28f, 0.42f, 1f), out Animator wolfAnim, out GameObject balloon);

        float t = 0f;
        const float floatInDuration = 0.95f;
        while (t < floatInDuration && wolf != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / floatInDuration);
            float wy = Mathf.Lerp(1.65f, 0.25f, u);
            float wx = -2.35f + Mathf.Sin(t * 4.5f) * 0.14f;
            wolf.transform.localPosition = new Vector3(wx, wy, 0f);

            // แม่หมูเลื่อนกระเช้าขึ้นให้ตรงระดับลูกโป่งหมาป่า
            float targetMamaY = Mathf.Lerp(initialActorLocalPos.y, wy + 0.82f, u);
            actorTransform.localPosition = new Vector3(initialActorLocalPos.x, targetMamaY, 0f);
            yield return null;
        }
        if (wolf == null || balloon == null) yield break;

        // แม่หมูยิงธนู!
        PlayAnimatorSafe(targetAnimator, "Shoot");
        Vector3 fireWorld = spawnPoint != null ? spawnPoint.position : actorTransform.position + new Vector3(-0.7f, 0f, 0f);
        CartoonFXHelper.SpawnMuzzleGust(fireWorld, -1f, new Color(1f, 0.88f, 0.55f, 0.9f));

        GameObject arrow = null;
        if (projectilePrefab != null)
        {
            arrow = Instantiate(projectilePrefab, fireWorld, Quaternion.identity, transform);
            StripGameplayPhysics(arrow);
            arrow.transform.localScale *= dioramaScaleRatio;
            spawnedTemps.Add(arrow);
        }

        // ธนูพุ่งไปชนลูกโป่งหมาป่า
        float flight = 0f;
        while (arrow != null && balloon != null && flight < 0.75f)
        {
            float dt = Time.deltaTime;
            flight += dt;
            arrow.transform.position += Vector3.left * (11.5f * dt);

            // หมาป่ายังค่อยๆ ลอยลงเบาๆ ระหว่างธนูพุ่ง
            Vector3 wp = wolf.transform.localPosition;
            wp.y -= 0.35f * dt;
            wolf.transform.localPosition = wp;

            if (arrow.transform.position.x <= balloon.transform.position.x + 0.22f)
            {
                break;
            }
            yield return null;
        }

        if (arrow != null)
        {
            spawnedTemps.Remove(arrow);
            Destroy(arrow);
        }

        // ลูกโป่งแตก!
        if (balloon != null)
        {
            Vector3 popPos = balloon.transform.position;
            balloon.SetActive(false);
            SpawnDioramaFX(fxBalloonPopPrefab, popPos, 0.95f, 1.3f);
            CartoonFXHelper.SpawnFlashStar(popPos, new Color(1f, 0.85f, 0.92f, 1f), 0.28f, 1.35f, 0.18f, 22f, 140f, 24);
            CartoonFXHelper.SpawnRing(popPos, new Color(1f, 0.35f, 0.55f, 0.95f), new Vector3(0.32f, 0.32f, 1f), new Vector3(1.65f, 1.65f, 1f), 0.32f, 23);
        }

        PlayAnimatorSafe(targetAnimator, "Idle");
        yield return StartCoroutine(DropWolfToFloorRoutine(wolf, wolfAnim, wolf != null ? wolf.transform.localPosition.y : 0.2f));
    }

    // ช่อง 2: แม่หมูขึ้นไปหยิบเนื้อขนาดเท่าเกมจริง -> ปาเนื้อวิถีโค้งใส่หมาป่าที่ห้อยมา!
    private IEnumerator DioramaThrowMeatWolvesRoutine()
    {
        if (actorTransform == null) yield break;

        if (indicatorObject != null) indicatorObject.SetActive(true);
        if (heldVisualObject != null) heldVisualObject.SetActive(false);

        GameObject wolf1 = SpawnDioramaWolf(new Vector3(-1.45f, 1.60f, 0f), new Color(1f, 0.32f, 0.44f, 1f), out Animator anim1, out GameObject b1);
        GameObject wolf2 = SpawnDioramaWolf(new Vector3(-2.65f, 0.95f, 0f), new Color(1f, 0.82f, 0.20f, 1f), out Animator anim2, out GameObject b2);

        // เฟส 1: แม่หมูเลื่อนขึ้นไปด้านบนสุดหยิบเนื้อ (IsAtTop)
        float t = 0f;
        const float riseTime = 0.65f;
        while (t < riseTime)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / riseTime));
            actorTransform.localPosition = new Vector3(initialActorLocalPos.x, Mathf.Lerp(initialActorLocalPos.y, 1.35f, u), 0f);
            FloatWolfStep(wolf1, 0.35f, 0f);
            FloatWolfStep(wolf2, 0.35f, 1.4f);
            yield return null;
        }

        // หยิบเนื้อติดมือ (HoldingMeat)
        if (indicatorObject != null) indicatorObject.SetActive(false);
        if (heldVisualObject != null) heldVisualObject.SetActive(true);

        // เฟส 2: เลื่อนลงมานิดนึงเพื่อโชว์เนื้อในมือแล้วเตรียมปา
        t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f));
            actorTransform.localPosition = new Vector3(initialActorLocalPos.x, Mathf.Lerp(1.35f, 0.98f, u), 0f);
            FloatWolfStep(wolf1, 0.35f, 0f);
            FloatWolfStep(wolf2, 0.35f, 1.4f);
            yield return null;
        }

        // เฟส 3: ปาเนื้อขนาดจริง (2-Phase Parabolic + Soft Homing)
        if (heldVisualObject != null) heldVisualObject.SetActive(false);
        PlayAnimatorSafe(targetAnimator, "Shoot");

        Vector3 startWorld = spawnPoint != null ? spawnPoint.position : actorTransform.position + new Vector3(-0.6f, 0.05f, 0f);
        GameObject meat = null;
        if (projectilePrefab != null)
        {
            meat = Instantiate(projectilePrefab, startWorld, Quaternion.identity, transform);
            StripGameplayPhysics(meat);
            meat.transform.localScale *= dioramaScaleRatio;
            spawnedTemps.Add(meat);
        }

        bool hit1 = false;
        bool hit2 = false;
        float mt = 0f;
        float vx = -5.6f;
        float vy = 0.35f;
        const float glideDuration = 0.24f;
        const float gravity = 8.8f;

        while (meat != null && mt < 1.35f)
        {
            float dt = Time.deltaTime;
            mt += dt;
            if (mt > 0.22f) PlayAnimatorSafe(targetAnimator, "Idle");

            if (mt > glideDuration)
            {
                vy -= gravity * dt;
                vx = Mathf.MoveTowards(vx, -2.6f, 2.8f * dt);
            }
            meat.transform.position += new Vector3(vx * dt, vy * dt, 0f);
            meat.transform.Rotate(0f, 0f, 240f * dt);

            if (!hit1) FloatWolfStep(wolf1, 0.35f, 0f);
            if (!hit2) FloatWolfStep(wolf2, 0.35f, 1.4f);

            if (!hit1 && wolf1 != null && Vector2.Distance(meat.transform.position, wolf1.transform.position + new Vector3(0f, 0.25f, 0f)) < 0.85f)
            {
                hit1 = true;
                if (b1 != null) b1.SetActive(false);
                SpawnDioramaFX(fxBalloonPopPrefab, wolf1.transform.position + new Vector3(0f, 0.35f, 0f), 0.85f, 1.2f);
                CartoonFXHelper.SpawnFlashStar(wolf1.transform.position, new Color(1f, 0.92f, 0.55f, 1f), 0.3f, 1.45f, 0.2f, 22f, 150f, 24);
                StartCoroutine(DropWolfToFloorRoutine(wolf1, anim1, wolf1.transform.localPosition.y));
            }
            if (!hit2 && wolf2 != null && Vector2.Distance(meat.transform.position, wolf2.transform.position + new Vector3(0f, 0.25f, 0f)) < 0.95f)
            {
                hit2 = true;
                if (b2 != null) b2.SetActive(false);
                SpawnDioramaFX(fxBalloonPopPrefab, wolf2.transform.position + new Vector3(0f, 0.35f, 0f), 0.85f, 1.2f);
                CartoonFXHelper.SpawnFlashStar(wolf2.transform.position, new Color(1f, 0.92f, 0.55f, 1f), 0.3f, 1.45f, 0.2f, 22f, 150f, 24);
                StartCoroutine(DropWolfToFloorRoutine(wolf2, anim2, wolf2.transform.localPosition.y));
            }

            if (meat.transform.localPosition.y < dioramaFloorLocalY - 0.1f || meat.transform.localPosition.x < -4.2f)
                break;

            yield return null;
        }

        if (meat != null)
        {
            spawnedTemps.Remove(meat);
            Destroy(meat);
        }
        yield return new WaitForSeconds(0.55f);
    }

    // ช่อง 3: หมาป่าห้อยมาปาหินใส่ตัวแม่หมูตรงช่องกลาง -> แม่หมูโดนดาเมจกระเด็นตกกระเช้าตาย (กระเช้าค้างบนเชือก)
    private IEnumerator DioramaHitByRockDeathRoutine()
    {
        if (actorTransform == null || mamaBodyTransform == null) yield break;

        mamaBodyTransform.localPosition = initialMamaBodyLocalPos;
        PlayAnimatorSafe(targetAnimator, "Idle");

        GameObject wolf = SpawnDioramaWolf(new Vector3(-2.45f, 1.50f, 0f), new Color(0.32f, 0.84f, 1f, 1f), out Animator wolfAnim, out _);

        // หมาป่าลอยลงมาอยู่ระดับเดียวกับช่องกลางของกระเช้าแม่หมู
        float t = 0f;
        while (t < 0.75f && wolf != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / 0.75f);
            wolf.transform.localPosition = new Vector3(-2.45f, Mathf.Lerp(1.50f, 0.72f, u), 0f);
            yield return null;
        }
        if (wolf == null) yield break;

        // หมาป่าปาหินใส่ตัวแม่หมูตรงๆ
        PlayAnimatorSafe(wolfAnim, "ThrowRock");
        yield return new WaitForSeconds(0.16f);

        Vector3 rockStart = wolf.transform.position + new Vector3(0.42f, 0.12f, 0f);
        Vector3 rockTarget = mamaBodyTransform.position;
        yield return StartCoroutine(FlyRockArcRoutine(rockStart, rockTarget, arcHeight: 0.42f, duration: 0.56f, bounceAfterHit: false, bounceVel: Vector2.zero));

        // หินโดนตัวแม่หมู -> เล่น FX + ท่า Fall + DeathBounceRoutine กระเด็นตกจากกระเช้า!
        SpawnDioramaFX(fxRockBreakPrefab, rockTarget, 0.85f, 1.1f);
        CartoonFXHelper.SpawnFlashStar(rockTarget, new Color(1f, 0.45f, 0.35f, 1f), 0.25f, 1.25f, 0.18f, 22f, 160f, 24);
        PlayAnimatorSafe(wolfAnim, "Drop");
        PlayAnimatorSafe(targetAnimator, "Fall");

        float dtSum = 0f;
        const float vx = -1.65f;
        const float vy = 2.65f;
        const float gravity = 14.5f;
        float minLocalY = (dioramaFloorLocalY - actorTransform.localPosition.y) / Mathf.Max(0.2f, actorTransform.localScale.y);

        while (dtSum < 1.25f && mamaBodyTransform != null)
        {
            dtSum += Time.deltaTime;
            float dx = vx * dtSum;
            float dy = vy * dtSum - 0.5f * gravity * dtSum * dtSum;
            float newY = initialMamaBodyLocalPos.y + dy;
            if (newY <= minLocalY)
            {
                mamaBodyTransform.localPosition = new Vector3(initialMamaBodyLocalPos.x + dx, minLocalY, 0f);
                SpawnDioramaFX(fxGroundDustPrefab, mamaBodyTransform.position + new Vector3(0f, -0.15f, 0f), 0.75f, 1.1f);
                break;
            }
            mamaBodyTransform.localPosition = new Vector3(initialMamaBodyLocalPos.x + dx, newY, 0f);
            yield return null;
        }
        yield return new WaitForSeconds(0.65f);
    }

    // ช่อง 4: ระบบเกราะกระเช้า — หมาป่าบินลงมาปาหินใส่เกราะบน (TopArmor) ก่อน แล้วค่อยลอยลงมาปาใส่เกราะล่าง (BottomArmor)
    private IEnumerator DioramaBasketArmorBlockRoutine()
    {
        if (actorTransform == null) yield break;
        PlayAnimatorSafe(targetAnimator, "Idle");

        GameObject wolf = SpawnDioramaWolf(new Vector3(-2.55f, 1.78f, 0f), new Color(1f, 0.78f, 0.22f, 1f), out Animator wolfAnim, out _);

        // เฟส 1: หมาป่าอยู่ด้านบน -> ปาหินใส่เกราะบน (TopArmor)
        float t = 0f;
        while (t < 0.45f && wolf != null)
        {
            t += Time.deltaTime;
            wolf.transform.localPosition = new Vector3(-2.55f, Mathf.Lerp(1.85f, 1.55f, t / 0.45f), 0f);
            yield return null;
        }
        if (wolf == null) yield break;

        PlayAnimatorSafe(wolfAnim, "ThrowRock");
        yield return new WaitForSeconds(0.14f);

        Vector3 topTarget = topArmorTransform != null ? topArmorTransform.position : actorTransform.position + new Vector3(0f, 0.55f, 0f);
        yield return StartCoroutine(FlyRockArcRoutine(wolf.transform.position + new Vector3(0.4f, 0.1f, 0f), topTarget, arcHeight: 0.45f, duration: 0.58f, bounceAfterHit: true, bounceVel: new Vector2(-2.6f, 2.5f)));

        SpawnDioramaFX(fxRockHitBasketPrefab, topTarget, 0.95f, 1.2f);
        CartoonFXHelper.SpawnRing(topTarget, new Color(0.45f, 0.92f, 1f, 0.95f), new Vector3(0.28f, 0.28f, 1f), new Vector3(1.35f, 1.35f, 1f), 0.28f, 23);
        StartCoroutine(FlashArmorRoutine(topArmorTransform));
        PlayAnimatorSafe(wolfAnim, "Drop");

        // เฟส 2: หมาป่าลอยลงมาด้านล่าง -> ปาหินใส่เกราะล่าง (BottomArmor)
        t = 0f;
        const float descendDur = 1.05f;
        while (t < descendDur && wolf != null)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / descendDur));
            wolf.transform.localPosition = new Vector3(-2.55f + Mathf.Sin(t * 5f) * 0.12f, Mathf.Lerp(1.55f, -0.45f, u), 0f);
            yield return null;
        }
        if (wolf == null) yield break;

        PlayAnimatorSafe(wolfAnim, "ThrowRock");
        yield return new WaitForSeconds(0.14f);

        Vector3 botTarget = bottomArmorTransform != null ? bottomArmorTransform.position : actorTransform.position + new Vector3(0f, -0.45f, 0f);
        yield return StartCoroutine(FlyRockArcRoutine(wolf.transform.position + new Vector3(0.4f, 0.1f, 0f), botTarget, arcHeight: 0.25f, duration: 0.58f, bounceAfterHit: true, bounceVel: new Vector2(-2.6f, -1.8f)));

        SpawnDioramaFX(fxRockHitBasketPrefab, botTarget, 0.95f, 1.2f);
        CartoonFXHelper.SpawnRing(botTarget, new Color(0.45f, 0.92f, 1f, 0.95f), new Vector3(0.28f, 0.28f, 1f), new Vector3(1.35f, 1.35f, 1f), 0.28f, 23);
        StartCoroutine(FlashArmorRoutine(bottomArmorTransform));
        PlayAnimatorSafe(wolfAnim, "Drop");

        yield return new WaitForSeconds(0.55f);
    }

    // ช่อง 6: แม่หมูยิงธนูโดนตัวหมาป่า (BiteArrow/Deflect) -> ธนูหัวทิ่มดิ่งลงปักพื้น (StickIntoGround: Quiver + GroundDust + Fade Out)
    private IEnumerator DioramaArrowStickGroundRoutine()
    {
        if (actorTransform == null) yield break;

        Vector3 wolfStart = new Vector3(-2.15f, 1.55f, 0f);
        GameObject wolf = SpawnDioramaWolf(wolfStart, new Color(0.28f, 0.85f, 1f, 1f), out Animator wolfAnim, out _);

        // เฟส 1: หมาป่าลอยลงมาและแม่หมูเลื่อนกระเช้าให้ตรงระดับตัวหมาป่า (เพื่อยิงโดนตัวหมาป่า ไม่ใช่ลูกโป่ง)
        float t = 0f;
        const float floatInDur = 0.72f;
        while (t < floatInDur && wolf != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / floatInDur);
            float wy = Mathf.Lerp(1.55f, 0.56f, u);
            float wx = -2.15f + Mathf.Sin(t * 4.2f) * 0.12f;
            wolf.transform.localPosition = new Vector3(wx, wy, 0f);

            float targetMamaY = Mathf.Lerp(initialActorLocalPos.y, wy - 0.04f, u);
            actorTransform.localPosition = new Vector3(initialActorLocalPos.x, targetMamaY, 0f);
            yield return null;
        }
        if (wolf == null) yield break;

        // เฟส 2: แม่หมูยิงธนูออกไปทางซ้าย!
        PlayAnimatorSafe(targetAnimator, "Shoot");
        Vector3 fireWorld = spawnPoint != null ? spawnPoint.position : actorTransform.position + new Vector3(-0.7f, 0f, 0f);
        CartoonFXHelper.SpawnMuzzleGust(fireWorld, -1f, new Color(1f, 0.88f, 0.55f, 0.9f));

        GameObject arrow = null;
        if (projectilePrefab != null)
        {
            arrow = Instantiate(projectilePrefab, fireWorld, Quaternion.identity, transform);
            StripGameplayPhysics(arrow);
            arrow.transform.localScale *= dioramaScaleRatio;
            spawnedTemps.Add(arrow);
        }

        float flight = 0f;
        while (arrow != null && wolf != null && flight < 0.75f)
        {
            float dt = Time.deltaTime;
            flight += dt;
            arrow.transform.position += Vector3.left * (11.5f * dt);

            if (arrow.transform.position.x <= wolf.transform.position.x + 0.18f)
            {
                break;
            }
            yield return null;
        }

        PlayAnimatorSafe(targetAnimator, "Idle");
        if (arrow == null || wolf == null) yield break;

        // เฟส 3: โดนตัวหมาป่า (Arrow.DeflectDown) -> หมาป่าเล่นท่างับ BiteArrow + หยุดหางธนู + หมุนหัวทิ่มดิ่งลงพื้น!
        PlayAnimatorSafe(wolfAnim, "BiteArrow");
        CartoonFXHelper.SpawnFlashStar(arrow.transform.position, new Color(1f, 0.88f, 0.45f, 1f), 0.18f, 0.85f, 0.16f, 22f, 140f, 24);

        foreach (var tr in arrow.GetComponentsInChildren<TrailRenderer>(true))
        {
            tr.emitting = false;
        }
        foreach (var ps in arrow.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // จัด X ให้อยู่แนวคอลัมน์หมาป่าแล้วหันหัวลูกธนูดิ่งลง 90 องศาเหมือน Arrow.DeflectDown()
        Vector3 deflectLocal = arrow.transform.localPosition;
        deflectLocal.x = wolf.transform.localPosition.x;
        arrow.transform.localPosition = deflectLocal;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

        // เฟส 4: ลูกธนูดิ่งลงพื้นด้วยแรงโน้มถ่วง (vy เริ่มที่ -3.5f แล้วเร่งลง) ขณะที่หมาป่ากลับไปลอยต่อ
        float vy = -3.5f;
        const float gravity = 16.5f;
        float biteRestoreTimer = 0f;
        bool restoredDrop = false;
        float stickY = dioramaFloorLocalY;

        while (arrow != null && arrow.transform.localPosition.y > stickY)
        {
            float dt = Time.deltaTime;
            biteRestoreTimer += dt;
            if (!restoredDrop && biteRestoreTimer >= 0.28f)
            {
                restoredDrop = true;
                PlayAnimatorSafe(wolfAnim, "Drop");
            }

            vy -= gravity * dt;
            Vector3 p = arrow.transform.localPosition;
            p.y = Mathf.Max(stickY, p.y + vy * dt);
            arrow.transform.localPosition = p;
            yield return null;
        }

        if (arrow == null) yield break;

        // เฟส 5: ปักลงพื้น (Arrow.StickIntoGround) — เอียงมุมสุ่ม + GroundDustImpactFX + สั่น Quiver 0.15s + ค้าง 1.0s + จางหาย 0.5s
        float slant = Random.Range(-8f, 8f);
        Quaternion baseRot = Quaternion.Euler(0f, 0f, 90f + slant);
        arrow.transform.rotation = baseRot;
        SpawnDioramaFX(fxGroundDustPrefab, arrow.transform.position + new Vector3(0f, -0.22f, 0f), 0.55f * dioramaScaleRatio, 1.0f);

        // Quiver / Twang (0.15s)
        float qt = 0f;
        const float quiverDur = 0.15f;
        while (qt < quiverDur && arrow != null)
        {
            qt += Time.deltaTime;
            float prog = qt / quiverDur;
            float qAngle = Mathf.Sin(prog * Mathf.PI * 6f) * 4f * (1f - prog);
            arrow.transform.rotation = baseRot * Quaternion.Euler(0f, 0f, qAngle);
            yield return null;
        }
        if (arrow != null) arrow.transform.rotation = baseRot;

        // ปักคาพื้นให้เห็นชัด (1.0s)
        yield return new WaitForSeconds(1.0f);

        // ค่อยๆ จางหาย (0.5s) เหมือน Arrow.StickAndFadeRoutine()
        if (arrow != null)
        {
            SpriteRenderer[] arrowSrs = arrow.GetComponentsInChildren<SpriteRenderer>(true);
            Color[] baseCols = new Color[arrowSrs.Length];
            for (int i = 0; i < arrowSrs.Length; i++)
            {
                baseCols[i] = arrowSrs[i] != null ? arrowSrs[i].color : Color.white;
            }

            float ft = 0f;
            const float fadeDur = 0.50f;
            while (ft < fadeDur && arrow != null)
            {
                ft += Time.deltaTime;
                float a = Mathf.Clamp01(1f - ft / fadeDur);
                for (int i = 0; i < arrowSrs.Length; i++)
                {
                    if (arrowSrs[i] != null)
                    {
                        Color bc = baseCols[i];
                        arrowSrs[i].color = new Color(bc.r, bc.g, bc.b, bc.a * a);
                    }
                }
                yield return null;
            }

            spawnedTemps.Remove(arrow);
            Destroy(arrow);
        }
    }

    private IEnumerator FlyRockArcRoutine(Vector3 startWorld, Vector3 targetWorld, float arcHeight, float duration, bool bounceAfterHit, Vector2 bounceVel)
    {
        if (rockPrefab == null) yield break;
        GameObject rock = Instantiate(rockPrefab, startWorld, Quaternion.identity, transform);
        StripGameplayPhysics(rock);
        rock.transform.localScale *= dioramaScaleRatio * 0.95f;
        spawnedTemps.Add(rock);

        float t = 0f;
        while (t < duration && rock != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            Vector3 p = Vector3.Lerp(startWorld, targetWorld, u);
            p.y += Mathf.Sin(u * Mathf.PI) * arcHeight;
            rock.transform.position = p;
            rock.transform.Rotate(0f, 0f, -340f * Time.deltaTime);
            yield return null;
        }

        if (bounceAfterHit && rock != null)
        {
            StartCoroutine(BounceDeflectedRockRoutine(rock, bounceVel));
            yield break;
        }

        if (rock != null)
        {
            spawnedTemps.Remove(rock);
            Destroy(rock);
        }
    }

    private IEnumerator BounceDeflectedRockRoutine(GameObject rock, Vector2 vel)
    {
        float t = 0f;
        while (t < 0.42f && rock != null)
        {
            float dt = Time.deltaTime;
            t += dt;
            vel.y -= 11f * dt;
            rock.transform.position += new Vector3(vel.x * dt, vel.y * dt, 0f);
            rock.transform.Rotate(0f, 0f, 480f * dt);
            var sr = rock.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Clamp01(1f - t / 0.42f);
                sr.color = c;
            }
            yield return null;
        }
        if (rock != null)
        {
            spawnedTemps.Remove(rock);
            Destroy(rock);
        }
    }

    private void FloatWolfStep(GameObject wolf, float speed, float phase)
    {
        if (wolf == null) return;
        Vector3 p = wolf.transform.localPosition;
        p.y -= speed * Time.deltaTime;
        p.x += Mathf.Sin(elapsed * 4.2f + phase) * 0.25f * Time.deltaTime;
        wolf.transform.localPosition = p;
    }

    // ช่อง 5: ระบบลูกหมูดึงเชือกรอกแบบฉากจริง (ดึงขึ้น -> หยุด Idle -> ปล่อยลง -> หยุด Idle)
    private void UpdatePigletElevatorStage()
    {
        float period = Mathf.Max(3.6f, actionInterval);
        float phase = elapsed % period;
        float t1 = period * 0.34f; // ดึงขึ้น
        float t2 = period * 0.50f; // พักด้านบน
        float t3 = period * 0.84f; // ปล่อยลง

        float minY = elevatorMinY;
        float maxY = elevatorMaxY;
        bool isMoving;
        float targetY;

        if (phase < t1)
        {
            isMoving = true;
            float u = Mathf.SmoothStep(0f, 1f, phase / t1);
            targetY = Mathf.Lerp(minY, maxY, u);
        }
        else if (phase < t2)
        {
            isMoving = false;
            targetY = maxY;
        }
        else if (phase < t3)
        {
            isMoving = true;
            float u = Mathf.SmoothStep(0f, 1f, (phase - t2) / (t3 - t2));
            targetY = Mathf.Lerp(maxY, minY, u);
        }
        else
        {
            isMoving = false;
            targetY = minY;
        }

        float deltaY = 0f;
        if (ropeGondolaTransform != null)
        {
            float prevY = ropeGondolaTransform.localPosition.y;
            Vector3 p = ropeGondolaTransform.localPosition;
            p.y = targetY;
            ropeGondolaTransform.localPosition = p;
            deltaY = p.y - prevY;
        }

        string desiredState = isMoving
            ? (string.IsNullOrEmpty(secondaryStateName) ? "Piglet_PullRobe" : secondaryStateName)
            : (string.IsNullOrEmpty(primaryStateName) ? "Piglet_Idle" : primaryStateName);

        EnsureStatePlaying(targetAnimator, desiredState);
        EnsureStatePlaying(secondaryAnimator, desiredState);

        if (pulleyDustFX != null)
        {
            var em = pulleyDustFX.emission;
            em.enabled = isMoving;
            if (isMoving && !pulleyDustFX.isPlaying) pulleyDustFX.Play();
            else if (!isMoving && pulleyDustFX.isPlaying) pulleyDustFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        UpdateRopeVisual(deltaY);
    }

    // =========================================================================================
    // WOLF SPAWNER FORMATIONS & JUMP PATTERNS (LIVE MINI-STAGE SIMULATOR)
    // =========================================================================================
    private void TriggerFormationWave()
    {
        CleanupSpawnedTemps();
        switch (demoType)
        {
            case ShowcaseDemoType.WolfFormationSingle:
                RestartRoutine(FormationSingleRoutine());
                break;
            case ShowcaseDemoType.WolfFormationStack:
                RestartRoutine(FormationStackRoutine());
                break;
            case ShowcaseDemoType.WolfFormationDiagonal:
                RestartRoutine(FormationDiagonalRoutine());
                break;
            case ShowcaseDemoType.WolfFormationHorizontal:
                RestartRoutine(FormationHorizontalSyncRoutine());
                break;
            case ShowcaseDemoType.WolfDirectionOddEven:
                RestartRoutine(DirectionOddEvenRoutine());
                break;
            case ShowcaseDemoType.WolfLadderAndBoulder:
                RestartRoutine(LadderAndBoulderRoutine());
                break;
            case ShowcaseDemoType.WolfBalloonDurabilityCompare:
                RestartRoutine(BalloonDurabilityCompareRoutine());
                break;
            case ShowcaseDemoType.BonusBossFruitThrow:
                StartBonusBossFruitDemo();
                break;
            case ShowcaseDemoType.WolfOddEvenCycleStage:
                RestartRoutine(OddEvenCycleStageRoutine());
                break;
        }
    }

    private float GetMiniLaneX(int laneIndex)
    {
        // 5 เลนจำลองในกรอบบูธ (-1.55 .. +1.55)
        float[] lanes = { -1.52f, -0.76f, 0.0f, 0.76f, 1.52f };
        return lanes[Mathf.Clamp(laneIndex, 0, lanes.Length - 1)];
    }

    private GameObject SpawnMiniWolf(Vector3 localPos, Color balloonColor, bool showBalloonInitially, out Animator anim, out GameObject balloonObj)
    {
        Transform parent = formationRoot != null ? formationRoot : transform;
        GameObject root = new GameObject("MiniDemoWolf");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        spawnedTemps.Add(root);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = wolfDemoSprite;
        sr.sortingOrder = 16;
        FitSpriteScale(body.transform, wolfDemoSprite, 0.62f, 0.62f);

        anim = body.AddComponent<Animator>();
        if (wolfDemoController != null) anim.runtimeAnimatorController = wolfDemoController;
        PlayAnimatorSafe(anim, "Run");

        balloonObj = new GameObject("MiniBalloon");
        balloonObj.transform.SetParent(root.transform, false);
        balloonObj.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        var bsr = balloonObj.AddComponent<SpriteRenderer>();
        bsr.sprite = balloonDemoSprite;
        bsr.color = balloonColor;
        bsr.sortingOrder = 17;
        FitSpriteScale(balloonObj.transform, balloonDemoSprite, 0.42f, 0.48f);
        balloonObj.SetActive(showBalloonInitially);

        return root;
    }

    private IEnumerator AnimateMiniWolfDrop(GameObject wolf, Animator anim, GameObject balloon, float targetX, float startY, float endY, float walkSpeed, float floatSpeed, float preDelay)
    {
        if (preDelay > 0f) yield return new WaitForSeconds(preDelay);
        if (wolf == null) yield break;

        // 1. WalkingBeforeDrop: เดินตามกิ่งไม้ไปยังเลนเป้าหมาย
        PlayAnimatorSafe(anim, "Run");
        while (wolf != null && Mathf.Abs(wolf.transform.localPosition.x - targetX) > 0.03f)
        {
            Vector3 p = wolf.transform.localPosition;
            p.x = Mathf.MoveTowards(p.x, targetX, walkSpeed * Time.deltaTime);
            wolf.transform.localPosition = p;
            yield return null;
        }
        if (wolf == null) yield break;

        // 2. BlowBalloon: หยุดเป่าลูกโป่ง
        PlayAnimatorSafe(anim, "BlowBalloon");
        if (balloon != null)
        {
            balloon.SetActive(true);
            Vector3 targetScale = balloon.transform.localScale;
            float blowT = 0f;
            while (wolf != null && blowT < 0.28f)
            {
                blowT += Time.deltaTime;
                balloon.transform.localScale = targetScale * Mathf.Clamp01(blowT / 0.28f);
                yield return null;
            }
            if (balloon != null) balloon.transform.localScale = targetScale;
        }
        if (wolf == null) yield break;

        // 3. Floating: กระโดดลอยลง/ขึ้นพร้อมส่าย Sine-Wave (รองรับทั้ง State "Float" ของ Summer และ "Drop" ของ Spring)
        PlayFloatStateSafe(anim);
        float t = 0f;
        float dir = endY < startY ? -1f : 1f;
        while (wolf != null && ((dir < 0f && wolf.transform.localPosition.y > endY) || (dir > 0f && wolf.transform.localPosition.y < endY)))
        {
            t += Time.deltaTime;
            Vector3 p = wolf.transform.localPosition;
            p.y += dir * floatSpeed * Time.deltaTime;
            p.x = targetX + Mathf.Sin(t * 4.5f) * 0.09f;
            wolf.transform.localPosition = p;
            yield return null;
        }
    }

    private static void PlayFloatStateSafe(Animator anim)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        int floatHash = Animator.StringToHash("Float");
        if (anim.HasState(0, floatHash))
        {
            anim.Play(floatHash, 0, 0f);
            return;
        }
        int dropHash = Animator.StringToHash("Drop");
        if (anim.HasState(0, dropHash))
        {
            anim.Play(dropHash, 0, 0f);
        }
    }

    private IEnumerator FormationSingleRoutine()
    {
        int lane = Random.Range(0, 5);
        float targetX = GetMiniLaneX(lane);
        var w = SpawnMiniWolf(new Vector3(-1.95f, 1.55f, 0f), new Color(1f, 0.3f, 0.45f, 1f), false, out var anim, out var b);
        yield return StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, 1.55f, -0.32f, walkSpeed: 2.3f, floatSpeed: 1.05f, preDelay: 0f));
    }

    private IEnumerator FormationStackRoutine()
    {
        // Pattern 1: ซ้อนเลนเดียวกัน 3 ตัว (stackDelay = stackVerticalGap / sharedSpeed)
        int lane = 2;
        float targetX = GetMiniLaneX(lane);
        float sharedSpeed = 0.92f;
        float stackDelay = 0.52f;
        Color[] cols = { new Color(1f, 0.35f, 0.45f), new Color(1f, 0.82f, 0.2f), new Color(0.35f, 0.88f, 1f) };

        for (int i = 0; i < 3; i++)
        {
            var w = SpawnMiniWolf(new Vector3(-1.95f, 1.55f, 0f), cols[i], false, out var anim, out var b);
            StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, 1.55f, -0.35f, walkSpeed: 2.6f, floatSpeed: sharedSpeed, preDelay: i * stackDelay));
        }
        yield return new WaitForSeconds(3.2f);
    }

    private IEnumerator FormationDiagonalRoutine()
    {
        // Pattern 2: ทแยงมุม (step = +1, ทิ้งช่วง 0.42s)
        float sharedSpeed = 0.98f;
        for (int i = 0; i < 3; i++)
        {
            float targetX = GetMiniLaneX(1 + i);
            var w = SpawnMiniWolf(new Vector3(-1.95f, 1.55f, 0f), new Color(1f, 0.78f, 0.22f), false, out var anim, out var b);
            StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, 1.55f, -0.35f, walkSpeed: 2.8f, floatSpeed: sharedSpeed, preDelay: i * 0.42f));
        }
        yield return new WaitForSeconds(3.2f);
    }

    private IEnumerator FormationHorizontalSyncRoutine()
    {
        // Pattern 3: หน้ากระดานเรียงหนึ่ง — ใช้สูตร spawnX = targetX - uniformWalkDistance เพื่อให้โดดพร้อมกันเป๊ะ!
        int[] lanes = { 1, 2, 3 };
        float uniformWalkDist = 0.95f;
        float sharedSpeed = 1.0f;

        for (int i = 0; i < lanes.Length; i++)
        {
            float targetX = GetMiniLaneX(lanes[i]);
            float spawnX = targetX - uniformWalkDist;
            var w = SpawnMiniWolf(new Vector3(spawnX, 1.55f, 0f), new Color(0.32f, 0.86f, 1f), false, out var anim, out var b);
            StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, 1.55f, -0.35f, walkSpeed: 1.75f, floatSpeed: sharedSpeed, preDelay: 0f));
        }
        yield return new WaitForSeconds(3.2f);
    }

    private IEnumerator DirectionOddEvenRoutine()
    {
        oddEvenToggle = !oddEvenToggle;
        if (oddEvenToggle)
        {
            // ด่านคี่ (Descending 1 HP)
            for (int i = 0; i < 2; i++)
            {
                float targetX = GetMiniLaneX(1 + i * 2);
                var w = SpawnMiniWolf(new Vector3(-1.9f, 1.55f, 0f), new Color(1f, 0.3f, 0.45f), false, out var anim, out var b);
                StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, 1.55f, -0.32f, walkSpeed: 2.6f, floatSpeed: 1.05f, preDelay: i * 0.25f));
            }
        }
        else
        {
            // ด่านคู่ (Ascending Multi-HP สีฟ้า + Boss สีชมพูกระพริบ)
            var w1 = SpawnMiniWolf(new Vector3(-1.9f, -0.32f, 0f), new Color(0.28f, 0.85f, 1f), false, out var a1, out var b1);
            StartCoroutine(AnimateMiniWolfDrop(w1, a1, b1, GetMiniLaneX(1), -0.32f, 1.55f, walkSpeed: 2.5f, floatSpeed: 0.95f, preDelay: 0f));

            var boss = SpawnMiniWolf(new Vector3(-1.9f, -0.32f, 0f), new Color(1f, 0.30f, 0.75f), false, out var a2, out var b2);
            var bsr = boss.GetComponentInChildren<SpriteRenderer>();
            if (bsr != null) bsr.color = new Color(1f, 0.68f, 0.85f, 1f);
            StartCoroutine(AnimateMiniWolfDrop(boss, a2, b2, GetMiniLaneX(3), -0.32f, 1.55f, walkSpeed: 2.9f, floatSpeed: 1.25f, preDelay: 0.35f));
        }
        yield return new WaitForSeconds(3.2f);
    }

    private IEnumerator LadderAndBoulderRoutine()
    {
        // ซ้าย: หมาป่าปีนบันไดเรียงคิว + ค้างท่า WaitLadder เหมือนเกมจริง (และชะโงกงับ BiteRobe + WolfBiteLadderFX)
        // ขวา: หมาป่าช่วยกันดันก้อนหิน (Push) แล้วค้างท่า PushIdle เหมือน Wolf.cs + BoulderSystem.cs
        Transform parent = formationRoot != null ? formationRoot : transform;

        // หมาป่า 2 ตัวที่ยืนจองขั้นบันไดอยู่แล้ว -> ค้างท่า spriteWaitLadder (ปิด Animator เหมือน Wolf.UpdateWaitingOnLadder())
        var lw0 = SpawnMiniWolf(new Vector3(-1.72f, -0.25f, 0f), Color.clear, false, out var la0, out _);
        HoldStaticWolfPose(lw0, la0, wolfWaitLadderSprite);

        var lw1 = SpawnMiniWolf(new Vector3(-1.72f, 0.42f, 0f), Color.clear, false, out var la1, out _);
        HoldStaticWolfPose(lw1, la1, wolfWaitLadderSprite);

        // หมาป่าตัวที่ 3 กำลังปีนขึ้นบันไดด้วยท่า Climb แล้วค่อยหยุดค้างท่า WaitLadder ที่ขั้นบนสุด
        var lw2 = SpawnMiniWolf(new Vector3(-1.72f, -0.78f, 0f), Color.clear, false, out var la2, out _);
        PlayAnimatorSafe(la2, "Climb");

        // ก้อนหินยักษ์ทางขวา (ใช้สไปรท์ก้อนหินจริงจากฉาก) + หมาป่า 2 ตัวช่วยกันดันหิน
        GameObject boulder = new GameObject("DemoBoulder");
        boulder.transform.SetParent(parent, false);
        boulder.transform.localPosition = new Vector3(1.92f, 0.96f, 0f);
        var rockSr = boulder.AddComponent<SpriteRenderer>();
        rockSr.sprite = boulderDemoSprite;
        rockSr.sortingOrder = 16;
        FitSpriteScale(boulder.transform, boulderDemoSprite, 1.35f, 1.35f);
        spawnedTemps.Add(boulder);

        var pw1 = SpawnMiniWolfWithController(new Vector3(0.95f, 0.72f, 0f), wolfPushController != null ? wolfPushController : wolfDemoController, out var pa1);
        var pw2 = SpawnMiniWolfWithController(new Vector3(0.32f, 0.72f, 0f), wolfPushController != null ? wolfPushController : wolfDemoController, out var pa2);
        PlayAnimatorSafe(pa1, "Push");
        PlayAnimatorSafe(pa2, "Push");

        float t = 0f;
        bool lw2Locked = false;
        bool biteTriggered = false;
        bool biteDone = false;
        bool pushHeld = false;

        while (t < 3.4f && boulder != null)
        {
            t += Time.deltaTime;

            // 1. หมาป่าตัวที่ 3 ปีนขึ้นขั้น 3 แล้วหยุดค้างท่า WaitLadder
            if (!lw2Locked && lw2 != null)
            {
                Vector3 lp = lw2.transform.localPosition;
                lp.y = Mathf.MoveTowards(lp.y, 1.08f, 1.45f * Time.deltaTime);
                lw2.transform.localPosition = lp;
                if (lp.y >= 1.07f)
                {
                    lw2Locked = true;
                    HoldStaticWolfPose(lw2, la2, wolfWaitLadderSprite);
                }
            }

            // 2. หมาป่าตัวที่ 2 บนบันไดกะพริบเตือนภัย -> เล่นท่างับ BiteRobe -> กลับไปค้างท่า WaitLadder
            if (lw1 != null)
            {
                var sr1 = lw1.GetComponentInChildren<SpriteRenderer>();
                if (t >= 1.0f && t < 1.35f && sr1 != null)
                {
                    sr1.color = Color.Lerp(Color.white, new Color(1f, 0.68f, 0.12f, 1f), Mathf.PingPong(t * 20f, 1f));
                }
                else if (t >= 1.35f && !biteTriggered)
                {
                    biteTriggered = true;
                    if (sr1 != null) sr1.color = new Color(1f, 0.38f, 0.38f, 1f);
                    if (la1 != null)
                    {
                        la1.enabled = true;
                        la1.speed = 1f;
                        PlayAnimatorSafe(la1, "BiteRobe");
                    }
                    SpawnDioramaFX(fxWolfBiteLadderPrefab, lw1.transform.position + new Vector3(-0.28f, 0.12f, 0f), 0.75f, 0.9f);
                }
                else if (t >= 1.85f && !biteDone)
                {
                    biteDone = true;
                    if (sr1 != null) sr1.color = Color.white;
                    HoldStaticWolfPose(lw1, la1, wolfWaitLadderSprite);
                }
            }

            // 3. หมาป่าฝั่งขวาดันหินขยับ 1.5s แล้วพักค้างท่า PushIdle เหมือนเกมจริง
            if (t <= 1.55f)
            {
                float pushX = Mathf.SmoothStep(0f, 0.55f, Mathf.Clamp01(t / 1.55f));
                boulder.transform.localPosition = new Vector3(1.92f + pushX, 0.96f, 0f);
                if (pw1 != null) pw1.transform.localPosition = new Vector3(0.95f + pushX, 0.72f, 0f);
                if (pw2 != null) pw2.transform.localPosition = new Vector3(0.32f + pushX, 0.72f, 0f);
            }
            else if (!pushHeld)
            {
                pushHeld = true;
                HoldStaticWolfPose(pw1, pa1, wolfPushIdleSprite);
                HoldStaticWolfPose(pw2, pa2, wolfPushIdleSprite);
            }

            yield return null;
        }
    }

    private GameObject SpawnMiniWolfWithController(Vector3 localPos, RuntimeAnimatorController ctrl, out Animator anim)
    {
        Transform parent = formationRoot != null ? formationRoot : transform;
        GameObject root = new GameObject("MiniBoulderWolf");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPos;
        spawnedTemps.Add(root);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = wolfDemoSprite;
        sr.sortingOrder = 16;
        FitSpriteScale(body.transform, wolfDemoSprite, 0.68f, 0.68f);

        anim = body.AddComponent<Animator>();
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        return root;
    }

    private static void HoldStaticWolfPose(GameObject wolfRoot, Animator anim, Sprite holdSprite)
    {
        if (wolfRoot == null) return;
        var sr = wolfRoot.GetComponentInChildren<SpriteRenderer>();
        if (holdSprite != null && sr != null)
        {
            if (anim != null) anim.enabled = false;
            sr.sprite = holdSprite;
        }
        else if (anim != null)
        {
            anim.speed = 0f;
        }
    }

    // ช่อง 7: ระบบลูกโป่ง — ซ้าย: ด่านคี่ (เยอะ แต่ 1 นัดแตก) | ขวา: ด่านคู่ (อึด 3 HP ยิงตอดหดตัว+ช้าลง+เปลี่ยนสี ฟ้า->เหลือง->แดง->แตก!)
    private IEnumerator BalloonDurabilityCompareRoutine()
    {
        Transform parent = formationRoot != null ? formationRoot : transform;

        // ฝั่งซ้าย (ด่านคี่: เยอะ 4 ตัว 1 HP)
        var oddWolves = new List<(GameObject wolf, Animator anim, GameObject balloon, bool popped)>();
        float[] oddXs = { -2.55f, -1.95f, -1.35f, -0.82f };
        float[] oddYs = { 1.45f, 1.05f, 1.55f, 0.85f };
        for (int i = 0; i < oddXs.Length; i++)
        {
            Color col = (i % 2 == 0) ? new Color(1f, 0.28f, 0.42f, 1f) : new Color(0.32f, 0.85f, 1f, 1f);
            var w = SpawnMiniWolf(new Vector3(oddXs[i], oddYs[i], 0f), col, true, out var a, out var b);
            PlayFloatStateSafe(a);
            oddWolves.Add((w, a, b, false));
        }

        // ฝั่งขวา (ด่านคู่: อึด 3 HP เริ่มจากสีฟ้า ลอยขึ้นช้าๆ)
        var toughWolf = SpawnMiniWolf(new Vector3(1.55f, -0.45f, 0f), new Color(0.28f, 0.85f, 1f, 1f), true, out var toughAnim, out var toughBalloon);
        PlayFloatStateSafe(toughAnim);
        Vector3 toughOrigBalloonScale = toughBalloon != null ? toughBalloon.transform.localScale : Vector3.one;
        int toughHp = 3;
        float toughSpeed = 0.72f;

        // ยิงธนูทดสอบฝั่งซ้าย (1 นัดแตกทีละตัว) และฝั่งขวา (ต้องยิง 3 นัดกว่าจะแตก) พร้อมกัน!
        float t = 0f;
        float nextOddShot = 0.45f;
        int oddShotIdx = 0;
        float nextEvenShot = 0.65f;

        while (t < 3.9f)
        {
            float dt = Time.deltaTime;
            t += dt;

            // อัปเดตหมาป่าด่านคี่ (ลอยลง)
            for (int i = 0; i < oddWolves.Count; i++)
            {
                var item = oddWolves[i];
                if (!item.popped && item.wolf != null)
                {
                    Vector3 p = item.wolf.transform.localPosition;
                    p.y -= 0.42f * dt;
                    item.wolf.transform.localPosition = p;
                }
            }

            // ยิงฝั่งซ้ายทีละตัว (1 นัดแตกเลย!)
            if (oddShotIdx < oddWolves.Count && t >= nextOddShot)
            {
                nextOddShot += 0.48f;
                var target = oddWolves[oddShotIdx];
                oddShotIdx++;
                if (target.wolf != null && target.balloon != null)
                {
                    StartCoroutine(ShootMiniArrowAndPopOdd(target.wolf, target.anim, target.balloon));
                    oddWolves[oddShotIdx - 1] = (target.wolf, target.anim, target.balloon, true);
                }
            }

            // อัปเดตหมาป่าด่านคู่ (ลอยขึ้น ความเร็วลดลงทุกครั้งที่โดนตอด)
            if (toughHp > 0 && toughWolf != null)
            {
                Vector3 tp = toughWolf.transform.localPosition;
                tp.y = Mathf.Min(1.35f, tp.y + toughSpeed * dt);
                toughWolf.transform.localPosition = tp;
            }

            // ยิงฝั่งขวา 3 นัด (3 -> 2 -> 1 -> POP!)
            if (toughHp > 0 && t >= nextEvenShot && toughWolf != null && toughBalloon != null)
            {
                nextEvenShot += 0.85f;
                int hpAfterHit = toughHp - 1;
                toughHp = hpAfterHit;
                if (hpAfterHit == 2) toughSpeed = 0.50f;
                else if (hpAfterHit == 1) toughSpeed = 0.34f;
                StartCoroutine(ShootMiniArrowAtToughBalloon(toughWolf, toughAnim, toughBalloon, toughOrigBalloonScale, hpAfterHit));
            }

            yield return null;
        }
    }

    private IEnumerator ShootMiniArrowAndPopOdd(GameObject wolf, Animator anim, GameObject balloon)
    {
        if (balloon == null) yield break;
        Vector3 targetPos = balloon.transform.position;
        Vector3 startPos = targetPos + new Vector3(1.45f, 0f, 0f);
        yield return StartCoroutine(FlyMiniArrow(startPos, targetPos, 0.16f));

        if (balloon != null)
        {
            Vector3 popPos = balloon.transform.position;
            balloon.SetActive(false);
            SpawnDioramaFX(fxBalloonPopPrefab, popPos, 0.62f, 0.9f);
            CartoonFXHelper.SpawnRing(popPos, new Color(1f, 0.35f, 0.55f, 0.9f), new Vector3(0.2f, 0.2f, 1f), new Vector3(0.95f, 0.95f, 1f), 0.24f, 22);
        }
        if (wolf != null)
        {
            StartCoroutine(DropWolfToFloorRoutine(wolf, anim, wolf.transform.localPosition.y));
        }
    }

    private IEnumerator ShootMiniArrowAtToughBalloon(GameObject wolf, Animator anim, GameObject balloon, Vector3 origScale, int hpRemaining)
    {
        if (balloon == null) yield break;
        Vector3 startPos = balloon.transform.position + new Vector3(1.35f, 0f, 0f);
        yield return StartCoroutine(FlyMiniArrow(startPos, balloon.transform.position, 0.18f));
        if (balloon == null) yield break;

        Vector3 hitPos = balloon.transform.position;
        var bsr = balloon.GetComponent<SpriteRenderer>();
        if (hpRemaining > 0)
        {
            // ยังไม่แตก! หดขนาด + เปลี่ยนสี (ฟ้า -> เหลือง -> แดง) + BalloonHitFX
            SpawnDioramaFX(fxBalloonHitPrefab, hitPos, 0.75f, 0.8f);
            CartoonFXHelper.SpawnFlashStar(hitPos, Color.white, 0.18f, 0.85f, 0.14f, 22f, 130f, 23);
            if (bsr != null)
            {
                bsr.color = hpRemaining == 2 ? new Color(1f, 0.82f, 0.16f, 1f) : new Color(1f, 0.28f, 0.35f, 1f);
            }
            balloon.transform.localScale = origScale * (hpRemaining == 2 ? 0.84f : 0.68f);
        }
        else
        {
            balloon.SetActive(false);
            SpawnDioramaFX(fxBalloonPopPrefab, hitPos, 0.82f, 1.0f);
            CartoonFXHelper.SpawnFlashStar(hitPos, new Color(1f, 0.9f, 0.55f, 1f), 0.22f, 1.15f, 0.18f, 22f, 150f, 24);
            CartoonFXHelper.SpawnRing(hitPos, new Color(1f, 0.45f, 0.35f, 0.95f), new Vector3(0.25f, 0.25f, 1f), new Vector3(1.25f, 1.25f, 1f), 0.28f, 23);
            if (wolf != null)
            {
                StartCoroutine(DropWolfToFloorRoutine(wolf, anim, wolf.transform.localPosition.y));
            }
        }
    }

    private IEnumerator FlyMiniArrow(Vector3 startWorld, Vector3 endWorld, float duration)
    {
        GameObject arrow = null;
        if (projectilePrefab != null)
        {
            arrow = Instantiate(projectilePrefab, startWorld, Quaternion.identity, transform);
            StripGameplayPhysics(arrow);
            arrow.transform.localScale *= 0.52f;
            spawnedTemps.Add(arrow);
        }

        float t = 0f;
        while (t < duration && arrow != null)
        {
            t += Time.deltaTime;
            arrow.transform.position = Vector3.Lerp(startWorld, endWorld, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (arrow != null)
        {
            spawnedTemps.Remove(arrow);
            Destroy(arrow);
        }
    }

    // ช่อง 8: บอสโบนัส/ยอดไม้วิ่งไป-กลับบนกิ่งไม้แล้วปาผลไม้ลงมา ขนาดตามจริง + ใช้ Sprite ผลไม้จริงทั้ง 3 ชนิด (Strawberry/Cherry/Peach)
    private float bossPatrolDir = 1f;
    private float bossThrowTimer = 0.6f;
    private bool bossIsThrowing = false;
    private int fruitCycleIndex = 0;

    private void StartBonusBossFruitDemo()
    {
        bossPatrolDir = 1f;
        bossThrowTimer = 0.55f;
        bossIsThrowing = false;
        PlayAnimatorSafe(targetAnimator, "Run");
    }

    private void UpdateBonusBossPatrolAndThrow()
    {
        if (actorTransform == null) return;

        if (!bossIsThrowing)
        {
            Vector3 p = actorTransform.localPosition;
            p.x += bossPatrolDir * 1.95f * Time.deltaTime;
            if (p.x >= 2.25f)
            {
                p.x = 2.25f;
                bossPatrolDir = -1f;
            }
            else if (p.x <= -2.25f)
            {
                p.x = -2.25f;
                bossPatrolDir = 1f;
            }
            actorTransform.localPosition = p;

            var sr = actorTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.flipX = bossPatrolDir < 0f;

            bossThrowTimer -= Time.deltaTime;
            if (bossThrowTimer <= 0f)
            {
                bossThrowTimer = Mathf.Max(0.95f, actionInterval);
                StartCoroutine(BossThrowRealFruitRoutine());
            }
        }
    }

    private IEnumerator BossThrowRealFruitRoutine()
    {
        bossIsThrowing = true;
        if (targetAnimator != null && targetAnimator.runtimeAnimatorController != null)
        {
            int throwFruitHash = Animator.StringToHash("ThrowFruit");
            int throwRockHash = Animator.StringToHash("ThrowRock");
            if (targetAnimator.HasState(0, throwFruitHash)) targetAnimator.Play(throwFruitHash, 0, 0f);
            else if (targetAnimator.HasState(0, throwRockHash)) targetAnimator.Play(throwRockHash, 0, 0f);
        }

        yield return new WaitForSeconds(0.18f);
        SpawnRealScaleFruitDrop();
        yield return new WaitForSeconds(0.18f);

        PlayAnimatorSafe(targetAnimator, "Run");
        bossIsThrowing = false;
    }

    private void SpawnRealScaleFruitDrop()
    {
        if (actorTransform == null) return;

        int typeIdx = fruitCycleIndex % 3;
        fruitCycleIndex++;

        Color[] glowColors =
        {
            new Color(1.0f, 0.25f, 0.45f, 1f), // Strawberry Ruby Glow
            new Color(1.0f, 0.12f, 0.28f, 1f), // Cherry Crimson Glow
            new Color(1.0f, 0.72f, 0.22f, 1f)  // Peach Golden Amber Glow
        };
        // สเกลตามจริงจาก BonusFruit.cs: Strawberry = 1.5, Cherry = 1.3, Peach = 1.5 (คูณด้วยสัดส่วน dioramaScaleRatio ให้ตรงกับขนาดบอสจริง!)
        float[] realFruitScales = { 1.5f, 1.3f, 1.5f };

        Vector3 dropWorld = actorTransform.position + new Vector3(bossPatrolDir * 0.25f, -0.35f, 0f);
        GameObject fruitObj;
        if (fruitPrefab != null)
        {
            fruitObj = Instantiate(fruitPrefab, dropWorld, Quaternion.identity, transform);
            StripGameplayPhysics(fruitObj);
        }
        else
        {
            fruitObj = new GameObject("RealDemoFruit");
            fruitObj.transform.SetParent(transform, false);
            fruitObj.transform.position = dropWorld;
            fruitObj.AddComponent<SpriteRenderer>();
        }

        float finalScale = realFruitScales[typeIdx] * dioramaScaleRatio;
        fruitObj.transform.localScale = new Vector3(finalScale, finalScale, 1f);

        var sr = fruitObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (realFruitSprites != null && typeIdx < realFruitSprites.Length && realFruitSprites[typeIdx] != null)
            {
                sr.sprite = realFruitSprites[typeIdx];
                sr.color = Color.white;
            }
            sr.sortingOrder = 20;
        }

        // ตั้งสี GlowAura & SparkleTrail ตามสีผลไม้จริงเหมือน BonusFruit.SetupGlowParticles()
        Color gCol = glowColors[typeIdx];
        foreach (var ps in fruitObj.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            Color c = gCol;
            c.a = 0.85f;
            main.startColor = c;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null) psr.sortingOrder = 19;
            ps.Play(true);
        }

        spawnedTemps.Add(fruitObj);
        StartCoroutine(FallRealFruitRoutine(fruitObj, gCol));
    }

    private IEnumerator FallRealFruitRoutine(GameObject fruit, Color glowColor)
    {
        float fallSpeed = 2.4f;
        float driftX = Random.Range(-0.22f, 0.22f);
        while (fruit != null && fruit.transform.localPosition.y > dioramaFloorLocalY + 0.15f)
        {
            float dt = Time.deltaTime;
            fruit.transform.localPosition += new Vector3(driftX * dt, -fallSpeed * dt, 0f);
            yield return null;
        }
        if (fruit != null)
        {
            CartoonFXHelper.SpawnFlashStar(fruit.transform.position, glowColor, 0.20f, 0.95f, 0.18f, 22f, 140f, 23);
            spawnedTemps.Remove(fruit);
            Destroy(fruit);
        }
    }

    // ช่อง 9: สลับให้เห็นชัดเจนระหว่าง ด่านคี่ (โรยตัวลง ↓ Descending) <-> ด่านคู่ (บินลอยขึ้น ↑ Ascending)
    private IEnumerator OddEvenCycleStageRoutine()
    {
        oddEvenToggle = !oddEvenToggle;
        int[] lanes = { 0, 2, 4 };

        if (oddEvenToggle)
        {
            // โหมดด่านคี่ (Stage 1, 3, 5): เดินบนกิ่งไม้บนสุด -> เป่าลูกโป่ง -> โรยตัวลงมา ↓
            if (statusLabel != null)
            {
                statusLabel.text = "▼ ด่านคี่ (Stage 1, 3, 5) : โรยตัวลงมา ↓ (Descending)";
                statusLabel.color = new Color(1f, 0.62f, 0.45f, 1f);
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                float targetX = GetMiniLaneX(lanes[i]);
                Color c = (i == 1) ? new Color(1f, 0.30f, 0.48f, 1f) : new Color(1f, 0.42f, 0.32f, 1f);
                var w = SpawnMiniWolf(new Vector3(-2.35f, 1.50f, 0f), c, false, out var anim, out var b);
                StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, 1.50f, -0.42f, walkSpeed: 2.9f, floatSpeed: 1.05f, preDelay: i * 0.28f));
            }
        }
        else
        {
            // โหมดด่านคู่ (Stage 2, 4): เดินจากพื้นด้านล่าง -> เป่าลูกโป่ง -> บินลอยขึ้น ↑
            if (statusLabel != null)
            {
                statusLabel.text = "▲ ด่านคู่ (Stage 2, 4) : บินลอยขึ้น ↑ (Ascending)";
                statusLabel.color = new Color(0.45f, 0.90f, 1f, 1f);
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                float targetX = GetMiniLaneX(lanes[i]);
                Color c = (i == 1) ? new Color(1f, 0.82f, 0.20f, 1f) : new Color(0.28f, 0.85f, 1f, 1f);
                var w = SpawnMiniWolf(new Vector3(-2.35f, -0.42f, 0f), c, false, out var anim, out var b);
                StartCoroutine(AnimateMiniWolfDrop(w, anim, b, targetX, -0.42f, 1.50f, walkSpeed: 2.9f, floatSpeed: 1.05f, preDelay: i * 0.28f));
            }
        }

        yield return new WaitForSeconds(3.5f);
    }

    // =========================================================================================
    // MAMA PIG & WEAPON DEMOS
    // =========================================================================================
    private IEnumerator MamaShootRoutine()
    {
        PlayAnimatorSafe(targetAnimator, string.IsNullOrEmpty(secondaryStateName) ? "Shoot" : secondaryStateName);

        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(-0.45f, 0f, 0f);
        CartoonFXHelper.SpawnMuzzleGust(origin, -1f, new Color(1f, 0.85f, 0.55f, 0.85f));

        if (projectilePrefab != null)
        {
            GameObject demoArrow = Instantiate(projectilePrefab, origin, Quaternion.identity, transform);
            StripGameplayPhysics(demoArrow);
            spawnedTemps.Add(demoArrow);
            StartCoroutine(MoveDemoArrowRoutine(demoArrow, origin, Vector3.left, speed: 6.5f, maxDist: 2.35f));
        }

        yield return new WaitForSeconds(Mathf.Max(0.14f, actionHoldDuration));
        PlayAnimatorSafe(targetAnimator, primaryStateName);
    }

    private IEnumerator MoveDemoArrowRoutine(GameObject obj, Vector3 startPos, Vector3 dir, float speed, float maxDist)
    {
        float traveled = 0f;
        while (obj != null && traveled < maxDist)
        {
            float step = speed * Time.deltaTime;
            obj.transform.position += dir * step;
            traveled += step;
            yield return null;
        }

        if (obj != null)
        {
            spawnedTemps.Remove(obj);
            Destroy(obj);
        }
    }

    private IEnumerator MamaMeatRoutine()
    {
        if (heldVisualObject != null) heldVisualObject.SetActive(true);
        if (indicatorObject != null) indicatorObject.SetActive(true);
        yield return new WaitForSeconds(0.55f);

        if (heldVisualObject != null) heldVisualObject.SetActive(false);
        if (indicatorObject != null) indicatorObject.SetActive(false);
        PlayAnimatorSafe(targetAnimator, string.IsNullOrEmpty(secondaryStateName) ? "Shoot" : secondaryStateName);

        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(-0.35f, 0.1f, 0f);
        if (projectilePrefab != null)
        {
            GameObject demoMeat = Instantiate(projectilePrefab, origin, Quaternion.identity, transform);
            StripGameplayPhysics(demoMeat);
            spawnedTemps.Add(demoMeat);
            StartCoroutine(MoveDemoMeatArcRoutine(demoMeat, origin));
        }

        yield return new WaitForSeconds(0.22f);
        PlayAnimatorSafe(targetAnimator, primaryStateName);
    }

    private IEnumerator MoveDemoMeatArcRoutine(GameObject obj, Vector3 startPos)
    {
        float t = 0f;
        float vx = -3.4f;
        float vy = 0.25f;
        const float glideDuration = 0.18f;
        const float gravity = 8.5f;

        while (obj != null && t < 0.72f)
        {
            float dt = Time.deltaTime;
            t += dt;
            if (t > glideDuration)
            {
                vy -= gravity * dt;
                vx = Mathf.MoveTowards(vx, -1.2f, 3.0f * dt);
            }
            obj.transform.position += new Vector3(vx * dt, vy * dt, 0f);
            yield return null;
        }

        if (obj != null)
        {
            spawnedTemps.Remove(obj);
            Destroy(obj);
        }
    }

    private IEnumerator MamaFallRoutine()
    {
        if (actorTransform == null) yield break;

        actorTransform.localPosition = initialActorLocalPos;
        PlayAnimatorSafe(targetAnimator, string.IsNullOrEmpty(primaryStateName) ? "Fall" : primaryStateName);

        float t = 0f;
        const float vx = -1.15f;
        const float vy = 1.95f;
        const float gravity = 11.5f;
        const float floorOffsetY = -1.05f;

        while (t < 1.15f)
        {
            t += Time.deltaTime;
            float dx = vx * t;
            float dy = vy * t - 0.5f * gravity * t * t;
            if (dy <= floorOffsetY)
            {
                actorTransform.localPosition = initialActorLocalPos + new Vector3(dx, floorOffsetY, 0f);
                break;
            }
            actorTransform.localPosition = initialActorLocalPos + new Vector3(dx, dy, 0f);
            yield return null;
        }

        yield return new WaitForSeconds(0.55f);
        actorTransform.localPosition = initialActorLocalPos;
    }

    private void UpdateRopeDemo()
    {
        float cycle = elapsed % 3.2f;
        bool isPulling = cycle < 2.2f;

        float deltaY = 0f;
        if (ropeGondolaTransform != null)
        {
            float prevY = ropeGondolaTransform.localPosition.y;
            float targetY = isPulling
                ? initialActorLocalPos.y + Mathf.Sin(cycle * Mathf.PI * 1.1f) * 0.45f
                : initialActorLocalPos.y;
            Vector3 p = ropeGondolaTransform.localPosition;
            p.y = Mathf.Lerp(p.y, targetY, 1f - Mathf.Exp(-10f * Time.deltaTime));
            ropeGondolaTransform.localPosition = p;
            deltaY = p.y - prevY;
        }

        string desiredState = isPulling
            ? (string.IsNullOrEmpty(secondaryStateName) ? "Piglet_PullRobe" : secondaryStateName)
            : (string.IsNullOrEmpty(primaryStateName) ? "Piglet_Idle" : primaryStateName);

        EnsureStatePlaying(targetAnimator, desiredState);
        EnsureStatePlaying(secondaryAnimator, desiredState);

        if (pulleyDustFX != null)
        {
            var em = pulleyDustFX.emission;
            em.enabled = isPulling;
            if (isPulling && !pulleyDustFX.isPlaying) pulleyDustFX.Play();
            else if (!isPulling && pulleyDustFX.isPlaying) pulleyDustFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        UpdateRopeVisual(deltaY);
    }

    private void UpdateRopeVisual(float deltaY)
    {
        if (ropeLineRenderer == null || ropeGondolaTransform == null || ropeHandTransform == null) return;

        if (!ropeLineRenderer.enabled) ropeLineRenderer.enabled = true;
        if (!ropeLineRenderer.useWorldSpace) ropeLineRenderer.useWorldSpace = true;

        float anchorOffY = demoType == ShowcaseDemoType.PigletElevatorStage ? ropeGondolaAnchorOffsetY : 0.35f;
        Vector3 point0 = ropeGondolaTransform.position + new Vector3(0f, anchorOffY, 0f);
        Vector3 point2 = ropeHandTransform.position;
        point0.z = 0f;
        point2.z = 0f;
        float pulleyY = transform.position.y + ropePulleyLocalY;

        float dx = point2.x - point0.x;
        float dirX = dx >= 0f ? 1f : -1f;
        float maxR = Mathf.Min(Mathf.Max(0.01f, pulleyY - point0.y) * 0.45f, Mathf.Max(0.01f, Mathf.Abs(dx)) * 0.45f);
        float r = Mathf.Clamp(ropeCornerRadius, 0f, maxR);

        int segments = Mathf.Max(2, ropeCornerSegments);
        int totalCount = 2 + segments;
        if (ropeLineRenderer.positionCount != totalCount)
            ropeLineRenderer.positionCount = totalCount;

        ropeLineRenderer.SetPosition(0, point0);
        Vector2 center = new Vector2(point0.x + dirX * r, pulleyY - r);
        float startAngle = dirX >= 0f ? Mathf.PI : 0f;
        float endAngle = Mathf.PI * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            ropeLineRenderer.SetPosition(1 + i, new Vector3(center.x + r * Mathf.Cos(angle), center.y + r * Mathf.Sin(angle), 0f));
        }

        ropeLineRenderer.SetPosition(totalCount - 1, point2);

        if (runtimeRopeMat != null && Mathf.Abs(deltaY) > 0.0001f)
        {
            ropeUvOffset -= deltaY * 0.65f;
            runtimeRopeMat.mainTextureOffset = new Vector2(ropeUvOffset, 0f);
        }
    }

    private IEnumerator WolfThrowRockRoutine()
    {
        PlayAnimatorSafe(targetAnimator, string.IsNullOrEmpty(secondaryStateName) ? "ThrowRock" : secondaryStateName);
        yield return new WaitForSeconds(0.18f);

        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + new Vector3(0.35f, 0.1f, 0f);
        if (projectilePrefab != null)
        {
            GameObject demoRock = Instantiate(projectilePrefab, origin, Quaternion.identity, transform);
            StripGameplayPhysics(demoRock);
            spawnedTemps.Add(demoRock);
            StartCoroutine(MoveDemoRockRoutine(demoRock, origin));
        }

        yield return new WaitForSeconds(0.45f);
        PlayAnimatorSafe(targetAnimator, primaryStateName);
    }

    private IEnumerator MoveDemoRockRoutine(GameObject obj, Vector3 startPos)
    {
        float t = 0f;
        float vx = 3.0f;
        float vy = 1.4f;
        const float gravity = 5.5f;

        while (obj != null && t < 0.7f)
        {
            float dt = Time.deltaTime;
            t += dt;
            vy -= gravity * dt;
            obj.transform.position += new Vector3(vx * dt, vy * dt, 0f);
            obj.transform.Rotate(0f, 0f, -220f * dt);
            yield return null;
        }

        if (obj != null)
        {
            spawnedTemps.Remove(obj);
            Destroy(obj);
        }
    }

    // =========================================================================================
    // PARTICLE REPLAY & MINI RAIN BOOTH (BOUNDED INSIDE CELL, ALWAYS LOOPING, ZERO MAGENTA)
    // =========================================================================================
    private void EnsureLoopingParticlesPlaying()
    {
        Transform mount = particleMount != null ? particleMount : transform;
        var systems = mount.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
            if (!main.loop) main.loop = true;
            var em = ps.emission;
            em.enabled = true;
            if (!ps.isPlaying) ps.Play(true);
        }
    }

    private void TriggerParticleReplay()
    {
        Transform mount = particleMount != null ? particleMount : transform;

        if (spawnRingAndStar)
        {
            CartoonFXHelper.SpawnFlashStar(
                mount.position,
                Color.Lerp(particleTintColor, Color.white, 0.65f),
                0.24f,
                1.15f,
                0.18f,
                22f,
                150f,
                22);
            CartoonFXHelper.SpawnRing(
                mount.position,
                Color.Lerp(particleTintColor, Color.white, 0.25f),
                new Vector3(0.30f, 0.30f, 1f),
                new Vector3(1.35f, 1.35f, 1f),
                0.32f,
                20);
        }

        var systems = mount.GetComponentsInChildren<ParticleSystem>(true);
        if (systems != null && systems.Length > 0)
        {
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.None;
                var em = ps.emission;
                em.enabled = true;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
            return;
        }

        if (particlePrefab != null)
        {
            var inst = Instantiate(particlePrefab, mount.position, Quaternion.identity, mount);
            StripGameplayPhysics(inst);
            foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.None;
                ps.Play(true);
            }
            spawnedTemps.Add(inst);
            Destroy(inst, Mathf.Max(0.8f, actionInterval - 0.1f));
        }
    }

    private static Material cachedMiniRainStreakMat;
    private static Material cachedMiniRainSplashMat;

    private static Material GetOrCreateMiniRainMat(bool isStreak)
    {
        if (isStreak && cachedMiniRainStreakMat != null) return cachedMiniRainStreakMat;
        if (!isStreak && cachedMiniRainSplashMat != null) return cachedMiniRainSplashMat;

        // ใช้สูตรเดียวกับ RainWeatherFX.cs 100% + รองรับ URP Transparent เพื่อไม่ให้ Texture/Shader บัคหรือม่วง!
        Texture2D tex = isStreak ? CreateMiniRainStreakTex(16, 64) : CreateMiniRainSplashTex(32);
        Shader shader = Shader.Find("Sprites/Default");
        bool isUrpParticles = false;
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            isUrpParticles = shader != null;
        }
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        Material mat = new Material(shader)
        {
            name = isStreak ? "ShowcaseRainStreakMat" : "ShowcaseRainSplashMat",
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = tex
        };
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (isUrpParticles)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        if (isStreak) cachedMiniRainStreakMat = mat;
        else cachedMiniRainSplashMat = mat;
        return mat;
    }

    private static Texture2D CreateMiniRainStreakTex(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            float vp = Mathf.Pow(Mathf.Sin(v * Mathf.PI), 0.75f);
            for (int x = 0; x < width; x++)
            {
                float u = ((float)x / (width - 1)) * 2f - 1f;
                float hp = Mathf.Clamp01(1f - Mathf.Abs(u));
                pixels[y * width + x] = new Color(1f, 1f, 1f, hp * hp * vp);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static Texture2D CreateMiniRainSplashTex(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color[] pixels = new Color[size * size];
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private void BuildMiniRainSystem()
    {
        // จำลองระบบ RainWeatherFX.cs ฉบับย่อส่วน 0.296x สมมาตรกับฉาก Rainy จริง + ตัดทุกเม็ดที่แตะขอบ 100%
        Transform mount = particleMount != null ? particleMount : transform;
        float halfW = clampParticlesToBounds ? Mathf.Max(0.8f, particleClipHalfSize.x) : 2.1f;
        float halfH = clampParticlesToBounds ? Mathf.Max(0.6f, particleClipHalfSize.y) : 1.15f;
        float boxW = Mathf.Max(0.9f, halfW * 2f - 0.35f);
        float fallSpeed = 6.1f; // 20.5 * 0.296 = 6.07 หน่วย/วิ สมมาตรกับความเร็วฝนในด่าน Rainy จริง!
        float travelH = Mathf.Max(0.6f, halfH * 2f - 0.28f);
        float rainLife = Mathf.Clamp(travelH / fallSpeed, 0.18f, 0.55f);

        GameObject rainObj = new GameObject("MiniRainDrops_Bounded");
        rainObj.transform.SetParent(mount, false);
        rainObj.transform.localPosition = new Vector3(0.20f, halfH - 0.10f, 0f);
        spawnedTemps.Add(rainObj);

        var ps = rainObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.stopAction = ParticleSystemStopAction.None;
        main.startLifetime = rainLife;
        main.startSpeed = 0f;
        main.startSize = 0.020f; // เส้นฝนเรียวเล็กสมส่วนกับฉากย่อส่วน 0.296x
        main.startColor = new Color(0.82f, 0.93f, 1f, 0.56f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var em = ps.emission;
        em.rateOverTime = 95f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(boxW, 0.06f, 0.04f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = -1.36f; // -4.6 * 0.296 = -1.36 องศาเฉียงตรงกับฉากจริงเป๊ะ
        vel.y = -fallSpeed;

        var colLife = ps.colorOverLifetime;
        colLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.88f), new GradientAlphaKey(0f, 1f) }
        );
        colLife.color = new ParticleSystem.MinMaxGradient(g);

        var psr = rainObj.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.velocityScale = 0.018f;
        psr.lengthScale = 1.65f;
        psr.sortingOrder = 19;
        psr.sharedMaterial = GetOrCreateMiniRainMat(isStreak: true);

        // ละอองน้ำกระเซ็นตรงระดับพื้นด้านในกรอบ (ย่อขนาดสมส่วนกับฉากจริง)
        GameObject splashObj = new GameObject("MiniRainSplash_Bounded");
        splashObj.transform.SetParent(mount, false);
        splashObj.transform.localPosition = new Vector3(0f, -halfH + 0.14f, 0f);
        spawnedTemps.Add(splashObj);

        var sps = splashObj.AddComponent<ParticleSystem>();
        var smain = sps.main;
        smain.loop = true;
        smain.prewarm = true;
        smain.stopAction = ParticleSystemStopAction.None;
        smain.startLifetime = 0.17f;
        smain.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 0.88f);
        smain.startSize = 0.032f;
        smain.gravityModifier = 0.35f;
        smain.startColor = new Color(0.86f, 0.96f, 1f, 0.72f);
        smain.simulationSpace = ParticleSystemSimulationSpace.Local;

        var sem = sps.emission;
        sem.rateOverTime = 42f;

        var sshape = sps.shape;
        sshape.shapeType = ParticleSystemShapeType.Box;
        sshape.scale = new Vector3(boxW, 0.04f, 0.04f);

        var spsr = splashObj.GetComponent<ParticleSystemRenderer>();
        spsr.sortingOrder = 19;
        spsr.sharedMaterial = GetOrCreateMiniRainMat(isStreak: false);

        ps.Play();
        sps.Play();
    }

    // =========================================================================================
    // SCENE 03 AUTHENTIC REAL-GAME VFX SCENARIOS (อ้างอิง Animation & สถานการณ์จริงทุกช่อง!)
    // =========================================================================================
    private void TriggerVfxScenario()
    {
        CleanupSpawnedTemps();
        PlayAnimatorSafe(targetAnimator, primaryStateName);

        switch (demoType)
        {
            case ShowcaseDemoType.VfxWolfBalloonPop:
                RestartRoutine(VfxWolfBalloonPopRoutine());
                break;
            case ShowcaseDemoType.VfxWolfRockBreak:
                RestartRoutine(VfxWolfRockBreakRoutine());
                break;
            case ShowcaseDemoType.VfxWolfRockHitBasket:
                RestartRoutine(VfxWolfRockHitBasketRoutine());
                break;
            case ShowcaseDemoType.VfxWolfFallGroundDust:
                RestartRoutine(VfxWolfFallGroundDustRoutine());
                break;
            case ShowcaseDemoType.VfxBossFruitThrow:
                RestartRoutine(VfxBossFruitThrowRoutine());
                break;
            case ShowcaseDemoType.VfxMamaWeaponStream:
                RestartRoutine(VfxMamaWeaponStreamRoutine());
                break;
        }
    }

    private void SpawnBoundedVfxBurst(GameObject fxPrefab, Vector3 worldPos, float scale = 0.65f, float life = 1.2f)
    {
        if (fxPrefab == null) return;
        Transform parentTr = particleMount != null ? particleMount : transform;
        GameObject fx = Instantiate(fxPrefab, worldPos, Quaternion.identity, parentTr);
        StripGameplayPhysics(fx);
        fx.transform.localScale *= scale;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sortingOrder < 22) psr.sortingOrder = 22;
            ps.Play(true);
        }
        spawnedTemps.Add(fx);
        RefreshBoundParticleCache();
        Destroy(fx, life);
    }

    private GameObject SpawnMiniScenarioWolf(Transform parentTr, Vector3 localPos, Color balloonColor, bool withBalloon, bool isBoss, out Animator wAnim, out GameObject bObj, out SpriteRenderer bSr)
    {
        GameObject root = new GameObject("MiniWolf");
        root.transform.SetParent(parentTr, false);
        root.transform.localPosition = localPos;
        spawnedTemps.Add(root);

        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var sr = body.AddComponent<SpriteRenderer>();
        sr.sprite = wolfDemoSprite;
        sr.color = isBoss ? new Color(1f, 0.56f, 0.80f, 1f) : Color.white;
        sr.sortingOrder = 16;
        FitSpriteScale(body.transform, wolfDemoSprite, isBoss ? 0.92f : 0.72f, isBoss ? 0.95f : 0.76f);

        wAnim = body.AddComponent<Animator>();
        if (wolfDemoController != null) wAnim.runtimeAnimatorController = wolfDemoController;
        PlayFloatStateSafe(wAnim);

        bObj = null;
        bSr = null;
        if (withBalloon)
        {
            bObj = new GameObject("Balloon");
            bObj.transform.SetParent(root.transform, false);
            bObj.transform.localPosition = new Vector3(-0.02f, 0.58f, 0f);
            bSr = bObj.AddComponent<SpriteRenderer>();
            bSr.sprite = balloonDemoSprite;
            bSr.color = balloonColor;
            bSr.sortingOrder = 17;
            if (balloonShaderMat != null) bSr.sharedMaterial = balloonShaderMat;
            FitSpriteScale(bObj.transform, balloonDemoSprite, 0.46f, 0.54f);
        }
        return root;
    }

    private IEnumerator FlyMiniArrowRoutine(Transform parentTr, Vector3 startLocal, Vector3 endLocal, float duration)
    {
        if (projectilePrefab == null) yield break;
        GameObject arrow = Instantiate(projectilePrefab, parentTr);
        arrow.transform.localPosition = startLocal;
        StripGameplayPhysics(arrow);
        arrow.transform.localScale *= 0.62f;
        spawnedTemps.Add(arrow);

        float t = 0f;
        while (t < duration && arrow != null)
        {
            t += Time.deltaTime;
            arrow.transform.localPosition = Vector3.Lerp(startLocal, endLocal, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (arrow != null)
        {
            spawnedTemps.Remove(arrow);
            Destroy(arrow);
        }
    }

    // 6.1 (1-Hit Pop) & 6.2 (3-HP Multi-Hit -> Pop): หมาป่าห้อยลูกโป่ง -> ธนูยิงโดน -> ลูกโป่งแตกหายไปจริง -> หมาป่าร่วง!
    private IEnumerator VfxWolfBalloonPopRoutine()
    {
        Transform mount = particleMount != null ? particleMount : transform;
        Color startCol = multiHitBalloon ? new Color(0.28f, 0.85f, 1f) : new Color(1f, 0.30f, 0.55f);
        GameObject wolf = SpawnMiniScenarioWolf(mount, new Vector3(-0.75f, 0.16f, 0f), startCol, withBalloon: true, isBoss: false, out Animator wAnim, out GameObject balloon, out SpriteRenderer bSr);

        if (multiHitBalloon)
        {
            Color[] stepCols = { new Color(1f, 0.82f, 0.18f), new Color(1f, 0.28f, 0.28f) };
            float[] stepScales = { 0.86f, 0.72f };
            Vector3 baseBScale = balloon != null ? balloon.transform.localScale : Vector3.one;

            for (int hit = 0; hit < 2; hit++)
            {
                if (wolf == null || balloon == null) yield break;
                Vector3 targetLocal = wolf.transform.localPosition + balloon.transform.localPosition;
                yield return StartCoroutine(FlyMiniArrowRoutine(mount, new Vector3(1.95f, targetLocal.y, 0f), targetLocal, 0.32f));
                if (balloon == null) yield break;

                SpawnBoundedVfxBurst(fxBalloonHitPrefab, balloon.transform.position, 0.60f, 0.9f);
                CartoonFXHelper.SpawnRing(balloon.transform.position, bSr != null ? bSr.color : Color.cyan, new Vector3(0.16f, 0.16f, 1f), new Vector3(0.72f, 0.72f, 1f), 0.24f, 22);
                if (bSr != null) bSr.color = stepCols[hit];
                balloon.transform.localScale = baseBScale * stepScales[hit];
                yield return new WaitForSeconds(0.28f);
            }
        }
        else
        {
            // ลอยลงมาเล็กน้อยก่อนโดนยิง
            float t0 = 0f;
            while (t0 < 0.35f && wolf != null)
            {
                t0 += Time.deltaTime;
                wolf.transform.localPosition = new Vector3(-0.75f, Mathf.Lerp(0.32f, 0.08f, t0 / 0.35f), 0f);
                yield return null;
            }
        }

        if (wolf == null || balloon == null) yield break;
        Vector3 popTargetLocal = wolf.transform.localPosition + balloon.transform.localPosition;
        yield return StartCoroutine(FlyMiniArrowRoutine(mount, new Vector3(1.95f, popTargetLocal.y, 0f), popTargetLocal, 0.32f));
        if (wolf == null || balloon == null) yield break;

        // ลูกโป่งแตก -> หายไปทันที + BalloonPopFX + หมาป่าเปลี่ยนเป็นท่า Fall ร่วงตกพื้น!
        Vector3 popWorld = balloon.transform.position;
        Color popCol = bSr != null ? bSr.color : new Color(1f, 0.30f, 0.55f);
        balloon.SetActive(false);

        SpawnBoundedVfxBurst(fxBalloonPopPrefab, popWorld, 0.62f, 1.0f);
        CartoonFXHelper.SpawnRing(popWorld, popCol, new Vector3(0.18f, 0.18f, 1f), new Vector3(0.85f, 0.85f, 1f), 0.25f, 22);
        CartoonFXHelper.SpawnFlashStar(popWorld, popCol, 0.14f, 0.65f, 0.16f, 22f, 140f, 23);

        PlayAnimatorSafe(wAnim, "Fall");
        float vy = 0.5f;
        while (wolf != null && wolf.transform.localPosition.y > -0.72f)
        {
            float dt = Time.deltaTime;
            vy -= 10.5f * dt;
            Vector3 p = wolf.transform.localPosition;
            p.y = Mathf.Max(-0.72f, p.y + vy * dt);
            wolf.transform.localPosition = p;
            yield return null;
        }
        if (wolf != null)
        {
            SpawnBoundedVfxBurst(fxGroundDustPrefab, wolf.transform.position + new Vector3(0f, -0.14f, 0f), 0.48f, 0.9f);
            wolf.SetActive(false);
        }
        yield return new WaitForSeconds(0.40f);
    }

    // 6.3 RockBreak: หมาป่าปาหิน -> ธนูพุ่งมาสอยกลางอากาศ -> หินแตกหายไปพร้อม RockBreak!
    private IEnumerator VfxWolfRockBreakRoutine()
    {
        Transform mount = particleMount != null ? particleMount : transform;
        GameObject wolf = SpawnMiniScenarioWolf(mount, new Vector3(-1.45f, 0.02f, 0f), new Color(1f, 0.78f, 0.22f), withBalloon: true, isBoss: false, out Animator wAnim, out _, out _);
        yield return new WaitForSeconds(0.22f);
        if (wolf == null || rockPrefab == null) yield break;

        PlayAnimatorSafe(wAnim, "ThrowRock");
        yield return new WaitForSeconds(0.12f);

        GameObject rock = Instantiate(rockPrefab, mount);
        rock.transform.localPosition = new Vector3(-1.05f, 0.10f, 0f);
        StripGameplayPhysics(rock);
        rock.transform.localScale *= 0.62f;
        spawnedTemps.Add(rock);

        GameObject arrow = projectilePrefab != null ? Instantiate(projectilePrefab, mount) : null;
        if (arrow != null)
        {
            arrow.transform.localPosition = new Vector3(1.85f, 0.10f, 0f);
            StripGameplayPhysics(arrow);
            arrow.transform.localScale *= 0.60f;
            spawnedTemps.Add(arrow);
        }

        float t = 0f;
        const float meetDur = 0.44f;
        while (t < meetDur && rock != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / meetDur);
            rock.transform.localPosition = new Vector3(Mathf.Lerp(-1.05f, 0.25f, u), 0.10f + Mathf.Sin(u * Mathf.PI) * 0.14f, 0f);
            rock.transform.Rotate(0f, 0f, -340f * Time.deltaTime);
            if (arrow != null) arrow.transform.localPosition = new Vector3(Mathf.Lerp(1.85f, 0.35f, u), 0.10f, 0f);
            yield return null;
        }

        PlayFloatStateSafe(wAnim);
        Vector3 hitWorld = rock != null ? rock.transform.position : mount.position;
        if (rock != null) rock.SetActive(false);
        if (arrow != null) arrow.SetActive(false);

        SpawnBoundedVfxBurst(fxRockBreakPrefab, hitWorld, 0.65f, 1.0f);
        CartoonFXHelper.SpawnFlashStar(hitWorld, new Color(1f, 0.85f, 0.45f), 0.14f, 0.65f, 0.16f, 22f, 150f, 23);
        yield return new WaitForSeconds(0.55f);
    }

    // 6.4 RockHitBasket: หมาป่าปาหินใส่เกราะกระเช้า -> กระดอนออก + RockHitBasket + ShieldParryRing!
    private IEnumerator VfxWolfRockHitBasketRoutine()
    {
        Transform mount = particleMount != null ? particleMount : transform;
        GameObject wolf = SpawnMiniScenarioWolf(mount, new Vector3(-1.45f, 0.12f, 0f), new Color(1f, 0.78f, 0.22f), withBalloon: true, isBoss: false, out Animator wAnim, out _, out _);
        yield return new WaitForSeconds(0.22f);
        if (wolf == null || rockPrefab == null) yield break;

        PlayAnimatorSafe(wAnim, "ThrowRock");
        yield return new WaitForSeconds(0.12f);

        Vector3 startWorld = wolf.transform.position + new Vector3(0.35f, 0.08f, 0f);
        Vector3 targetWorld = topArmorTransform != null ? topArmorTransform.position : mount.position + new Vector3(1.10f, 0.05f, 0f);

        GameObject rock = Instantiate(rockPrefab, startWorld, Quaternion.identity, mount);
        StripGameplayPhysics(rock);
        rock.transform.localScale *= 0.60f;
        spawnedTemps.Add(rock);

        float t = 0f;
        const float flyDur = 0.48f;
        while (t < flyDur && rock != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / flyDur);
            Vector3 p = Vector3.Lerp(startWorld, targetWorld, u);
            p.y += Mathf.Sin(u * Mathf.PI) * 0.26f;
            rock.transform.position = p;
            rock.transform.Rotate(0f, 0f, -340f * Time.deltaTime);
            yield return null;
        }

        PlayFloatStateSafe(wAnim);
        SpawnBoundedVfxBurst(fxRockHitBasketPrefab, targetWorld, 0.65f, 1.0f);
        CartoonFXHelper.SpawnRing(targetWorld, new Color(0.45f, 0.92f, 1f, 0.95f), new Vector3(0.18f, 0.18f, 1f), new Vector3(0.88f, 0.88f, 1f), 0.26f, 23);
        StartCoroutine(FlashArmorRoutine(topArmorTransform));

        if (rock != null)
        {
            StartCoroutine(BounceDeflectedRockRoutine(rock, new Vector2(-2.2f, 1.8f)));
        }
        yield return new WaitForSeconds(0.65f);
    }

    // 6.5 GroundDustImpactFX: หมาป่าเล่นท่า Fall ตกจากฟ้ามากระแทกพื้น -> GroundDustImpactFX ฝุ่นตลบ!
    private IEnumerator VfxWolfFallGroundDustRoutine()
    {
        Transform mount = particleMount != null ? particleMount : transform;
        GameObject wolf = SpawnMiniScenarioWolf(mount, new Vector3(0f, 0.72f, 0f), Color.white, withBalloon: false, isBoss: false, out Animator wAnim, out _, out _);
        PlayAnimatorSafe(wAnim, "Fall");

        float vy = -0.35f;
        const float floorY = -0.48f;
        while (wolf != null && wolf.transform.localPosition.y > floorY)
        {
            float dt = Time.deltaTime;
            vy -= 9.5f * dt;
            Vector3 p = wolf.transform.localPosition;
            p.y = Mathf.Max(floorY, p.y + vy * dt);
            wolf.transform.localPosition = p;
            yield return null;
        }

        if (wolf != null)
        {
            SpawnBoundedVfxBurst(fxGroundDustPrefab, wolf.transform.position + new Vector3(0f, -0.18f, 0f), 0.72f, 1.1f);
            // เด้งเบาๆ แล้วหายไปเหมือนตอนหมาป่าตายจริง
            float bt = 0f;
            while (bt < 0.32f && wolf != null)
            {
                bt += Time.deltaTime;
                float u = bt / 0.32f;
                wolf.transform.localPosition = new Vector3(0f, floorY + Mathf.Sin(u * Mathf.PI) * 0.16f, 0f);
                var sr = wolf.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Clamp01(1f - u);
                    sr.color = c;
                }
                yield return null;
            }
            if (wolf != null) wolf.SetActive(false);
        }
        yield return new WaitForSeconds(0.45f);
    }

    // 6.6 Bonus Fruit + CartoonSparkleHDR: แสดงเฉพาะผลไม้จริง (Strawberry / Cherry / Peach) โค้งตกลงมาพร้อม GlowAura + SparkleTrail สีตามผลไม้จริง (ไม่มีหมาป่า)
    private IEnumerator VfxBossFruitThrowRoutine()
    {
        Transform mount = particleMount != null ? particleMount : transform;

        int typeIdx = seasonalIndex % 3;
        seasonalIndex++;

        Color[] glowColors =
        {
            new Color(1.0f, 0.25f, 0.45f, 0.90f), // Strawberry Ruby Glow
            new Color(1.0f, 0.12f, 0.28f, 0.90f), // Cherry Crimson Glow
            new Color(1.0f, 0.72f, 0.22f, 0.90f)  // Peach Golden Amber Glow
        };
        float[] fruitScales = { 1.10f, 0.95f, 1.10f };

        Vector3 p0 = new Vector3(-1.45f, 0.35f, 0f);
        Vector3 p1 = new Vector3(1.45f, -0.52f, 0f);

        GameObject fruit;
        if (fruitPrefab != null)
        {
            fruit = Instantiate(fruitPrefab, mount);
            StripGameplayPhysics(fruit);
        }
        else
        {
            fruit = new GameObject("ShowcaseFruitFallback");
            fruit.transform.SetParent(mount, false);
            fruit.AddComponent<SpriteRenderer>();
        }
        fruit.transform.localPosition = p0;

        if (realFruitSprites != null && realFruitSprites.Length > 0)
        {
            Sprite spr = realFruitSprites[typeIdx % realFruitSprites.Length];
            if (spr != null && fruit.TryGetComponent<SpriteRenderer>(out var fsr))
            {
                fsr.sprite = spr;
                fsr.color = Color.white;
                fsr.sortingOrder = 20;
            }
        }
        if (statusLabel != null && seasonalLabels != null && seasonalLabels.Length > 0)
        {
            statusLabel.text = seasonalLabels[typeIdx % seasonalLabels.Length];
        }

        fruit.transform.localScale = Vector3.one * fruitScales[typeIdx];
        Color gCol = glowColors[typeIdx];
        foreach (var ps in fruit.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.None;
            main.startColor = gCol;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sortingOrder < 19) psr.sortingOrder = 19;
            ps.Play(true);
        }
        spawnedTemps.Add(fruit);
        RefreshBoundParticleCache();

        float t = 0f;
        const float arcDur = 1.05f;
        while (t < arcDur && fruit != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / arcDur);
            Vector3 p = Vector3.Lerp(p0, p1, u);
            p.y += Mathf.Sin(u * Mathf.PI) * 0.52f;
            fruit.transform.localPosition = p;
            fruit.transform.Rotate(0f, 0f, -180f * Time.deltaTime);
            yield return null;
        }

        // ซ่อนตัวผลไม้แล้วปล่อยให้ GlowAura / SparkleTrail ค่อยๆ จางหายอย่างเป็นธรรมชาติ
        if (fruit != null)
        {
            foreach (var sr in fruit.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
            foreach (var tr in fruit.GetComponentsInChildren<TrailRenderer>(true)) tr.emitting = false;
            foreach (var ps in fruit.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        yield return new WaitForSeconds(0.45f);
    }

    // 7.2 - 7.6 Mama Pig Weapon Stream: แม่หมูเล่นท่า Shoot / HoldingMeat จริง ปล่อยธนูความเร็วสมจริง (หาง Trail/Particle ไหลพริ้วแล้วค่อยๆ จาง) หรือปาเนื้อวิถีโค้ง!
    private IEnumerator VfxMamaWeaponStreamRoutine()
    {
        Transform mount = particleMount != null ? particleMount : transform;
        if (seasonalProjectiles == null || seasonalProjectiles.Length == 0) yield break;

        int idx = seasonalIndex % seasonalProjectiles.Length;
        seasonalIndex++;
        GameObject projPrefab = seasonalProjectiles[idx];
        if (statusLabel != null && seasonalLabels != null && idx < seasonalLabels.Length)
        {
            statusLabel.text = seasonalLabels[idx];
        }

        if (isParabolicMeatThrow)
        {
            PlayAnimatorSafe(targetAnimator, "HoldingMeat");
            yield return new WaitForSeconds(0.28f);
        }

        PlayAnimatorSafe(targetAnimator, "Shoot");
        yield return new WaitForSeconds(0.08f);

        Vector3 startLocal = spawnPoint != null
            ? mount.InverseTransformPoint(spawnPoint.position)
            : new Vector3(0.92f, 0.06f, 0f);

        if (fxArrowWindPrefab != null)
        {
            GameObject windFx = Instantiate(fxArrowWindPrefab, mount.TransformPoint(startLocal), Quaternion.identity, mount);
            StripGameplayPhysics(windFx);
            windFx.transform.localScale *= 0.85f;
            foreach (var ps in windFx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.playOnAwake = true;
                main.stopAction = ParticleSystemStopAction.None;
                var psr = ps.GetComponent<ParticleSystemRenderer>();
                if (psr != null && psr.sortingOrder < 21) psr.sortingOrder = 21;
                ps.Play(true);
            }
            spawnedTemps.Add(windFx);
            RefreshBoundParticleCache();
            Destroy(windFx, 1.15f);
        }

        if (projPrefab != null)
        {
            GameObject proj = Instantiate(projPrefab, mount);
            proj.transform.localPosition = startLocal;
            StripGameplayPhysics(proj);
            proj.transform.localScale *= (isParabolicMeatThrow ? 0.56f : 0.68f);

            // คงค่า TrailRenderer & ParticleSystem ดั้งเดิมของ Prefab ไว้ให้พริ้วเหมือนเกมจริง (ไม่หั่น Lifetime ให้แข็งทื่อ!)
            foreach (var tr in proj.GetComponentsInChildren<TrailRenderer>(true))
            {
                tr.Clear();
                tr.emitting = true;
                if (tr.sortingOrder < 18) tr.sortingOrder = 18;
            }
            foreach (var ps in proj.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.None;
                ps.Clear(true);
                ps.Play(true);
            }
            spawnedTemps.Add(proj);
            RefreshBoundParticleCache();

            float t = 0f;
            // ความเร็วธนูใกล้เคียงเกมจริง (~10.5 หน่วย/วิ) เพื่อให้ TrailRenderer & Particle พริ้วเป็นสายสวยงาม
            float dur = isParabolicMeatThrow ? 0.82f : 0.28f;
            Vector3 endLocal = isParabolicMeatThrow ? new Vector3(-1.82f, -0.56f, 0f) : new Vector3(-1.98f, startLocal.y, 0f);
            while (t < dur && proj != null)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                Vector3 p = Vector3.Lerp(startLocal, endLocal, u);
                if (isParabolicMeatThrow)
                {
                    p.y += Mathf.Sin(u * Mathf.PI) * 0.55f;
                    proj.transform.Rotate(0f, 0f, 340f * Time.deltaTime);
                }
                proj.transform.localPosition = p;
                yield return null;
            }

            // ซ่อนเฉพาะตัวสไปรท์แล้วปล่อยให้ Trail & Particle ค่อยๆ จางสลายตัวเหมือน ArrowTrailEffect.StopTrailForPool() ในเกมจริง!
            if (proj != null)
            {
                foreach (var sr in proj.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                foreach (var tr in proj.GetComponentsInChildren<TrailRenderer>(true)) tr.emitting = false;
                foreach (var ps in proj.GetComponentsInChildren<ParticleSystem>(true)) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        PlayAnimatorSafe(targetAnimator, "Idle");
        yield return new WaitForSeconds(0.55f);
    }

    private static void FitSpriteScale(Transform t, Sprite sprite, float maxW, float maxH)
    {
        if (t == null || sprite == null) return;
        Vector2 sz = sprite.bounds.size;
        float sx = sz.x > 0.001f ? maxW / sz.x : 1f;
        float sy = sz.y > 0.001f ? maxH / sz.y : 1f;
        float u = Mathf.Min(sx, sy);
        t.localScale = new Vector3(u, u, 1f);
    }

    private static void StripGameplayPhysics(GameObject go)
    {
        if (go == null) return;
        foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>(true))
            rb.simulated = false;

        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            if (mb is ArrowTrailEffect || mb is MeatAuraEffect) continue;
            mb.enabled = false;
        }

        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sortingOrder < 18) sr.sortingOrder = 18;
        foreach (var tr in go.GetComponentsInChildren<TrailRenderer>(true))
            if (tr.sortingOrder < 17) tr.sortingOrder = 17;
        foreach (var psr in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (psr.sortingOrder < 19) psr.sortingOrder = 19;
    }

    private static void PlayAnimatorSafe(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName) || anim.runtimeAnimatorController == null) return;
        int hash = Animator.StringToHash(stateName);
        if (anim.HasState(0, hash))
        {
            anim.Play(hash, 0, 0f);
        }
    }

    private static void EnsureStatePlaying(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName) || anim.runtimeAnimatorController == null) return;
        int hash = Animator.StringToHash(stateName);
        if (!anim.HasState(0, hash)) return;

        var info = anim.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(stateName))
        {
            anim.Play(hash, 0, 0f);
        }
    }

    private void CleanupSpawnedTemps()
    {
        for (int i = spawnedTemps.Count - 1; i >= 0; i--)
        {
            if (spawnedTemps[i] != null)
            {
                if (Application.isPlaying) Destroy(spawnedTemps[i]);
                else DestroyImmediate(spawnedTemps[i]);
            }
        }
        spawnedTemps.Clear();
    }
}
