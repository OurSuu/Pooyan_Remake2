#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// เครื่องมือสร้าง 4 Portfolio Showcase Scenes สำหรับโชว์ผลงานฝั่ง Programmer (ไม่เชื่อมเข้า Build Settings / Profile Scene List):
/// - 01_Showcase_MamaPig.unity          : ระบบแม่หมู, ท่า Animation + โค้ดที่เชื่อม, ยิงธนู/ปาเนื้อ/เชือกรอก + แยกชิ้น Prefab ที่ใช้จริง
/// - 02_Showcase_Wolves_Spawning.unity  : ระบบเกิดหมาป่า 4 แพตเทิร์นโดด (Single/Stack/Diagonal/Horizontal Sync), Odd/Even, Ladder/Boulder + บอส/ศัตรู
/// - 03_Showcase_VFX_Shaders.unity      : สแกน Dependency จากด่านจริงแบบ 100% — รวม Particle & Shader ที่ใช้จริงทุกชิ้น (7-Block Architecture)
/// - 04_Showcase_Audio_Soundboard.unity : หน้า Soundboard Studio กดฟัง BGM/SFX จริง, Footstep Mixer แยกพื้นผิว, Stereo Panning & Procedural DSP
/// </summary>
[InitializeOnLoad]
public static class PortfolioShowcaseBuilder
{
    private const string ShowcaseVersion = "20";
    private const string ShowcaseFolder = "Assets/Scenes/Portfolio_Showcase";
    private const string VersionFilePath = ShowcaseFolder + "/ShowcaseVersion.txt";

    private const string Scene01Path = ShowcaseFolder + "/01_Showcase_MamaPig.unity";
    private const string Scene02Path = ShowcaseFolder + "/02_Showcase_Wolves_Spawning.unity";
    private const string Scene03Path = ShowcaseFolder + "/03_Showcase_VFX_Shaders.unity";
    private const string Scene04Path = ShowcaseFolder + "/04_Showcase_Audio_Soundboard.unity";
    private const string LegacyScene02Path = ShowcaseFolder + "/02_Showcase_Wolves_Enemies.unity";
    private const string LegacyScene05Path = ShowcaseFolder + "/05_Showcase_GameManager_Cutscene.unity";

    private const string PanelSpritePath = ShowcaseFolder + "/ShowcasePanel.png";
    private const string HeatTestSpritePath = ShowcaseFolder + "/ShowcaseHeatTestBg.png";

    static PortfolioShowcaseBuilder()
    {
        EditorApplication.delayCall += AutoBuildLightweightIfOutdatedOnce;
    }

    private static void AutoBuildLightweightIfOutdatedOnce()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        string currentVer = File.Exists(VersionFilePath) ? File.ReadAllText(VersionFilePath).Trim() : "";
        bool coreExist = File.Exists(Scene01Path) && File.Exists(Scene02Path) && File.Exists(Scene04Path);
        if (coreExist && currentVer == ShowcaseVersion) return;

        try
        {
            EnsureFolder(ShowcaseFolder);
            if (File.Exists(LegacyScene02Path)) AssetDatabase.DeleteAsset(LegacyScene02Path);
            if (File.Exists(LegacyScene05Path)) AssetDatabase.DeleteAsset(LegacyScene05Path);

            Sprite panelSprite = EnsurePanelSprite();
            // รีบิลด์เฉพาะ Scene เบา (01, 02, 04) ที่ไม่ต้องเปิดฉากอื่นซ้อน — ปลอดภัย 100% ไม่ Crash!
            BuildSceneInAdditive(Scene01Path, scene => PopulateScene01_MamaPig(scene, panelSprite));
            BuildSceneInAdditive(Scene02Path, scene => PopulateScene02_WolvesAndSpawning(scene, panelSprite));
            BuildSceneInAdditive(Scene04Path, scene => PopulateScene04_AudioSoundboard(scene, panelSprite));

            if (!File.Exists(Scene03Path))
            {
                Sprite heatTestSprite = EnsureHeatTestSprite();
                BuildSceneInAdditive(Scene03Path, scene => PopulateScene03_AllUsedVfxAndShaders(scene, panelSprite, heatTestSprite));
            }

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), VersionFilePath), ShowcaseVersion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Pooyan Portfolio] อัปเดต Showcase Scenes (v" + ShowcaseVersion + ") เรียบร้อยแล้ว!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Pooyan Portfolio] AutoBuild skipped: " + ex.Message);
        }
    }

    [MenuItem("Tools/Pooyan Portfolio/Build All 4 Showcase Scenes (Rebuild)")]
    public static void BuildAllFromMenu()
    {
        BuildAllShowcaseScenes(openFirstScene: true);
    }

    [MenuItem("Tools/Pooyan Portfolio/Open 01 - Mama Pig & Weapons")]
    public static void OpenScene01() => OpenShowcaseScene(Scene01Path);

    [MenuItem("Tools/Pooyan Portfolio/Open 02 - Wolves, Formations & AI")]
    public static void OpenScene02() => OpenShowcaseScene(Scene02Path);

    [MenuItem("Tools/Pooyan Portfolio/Open 03 - All Used Particles & Shaders")]
    public static void OpenScene03() => OpenShowcaseScene(Scene03Path);

    [MenuItem("Tools/Pooyan Portfolio/Open 04 - Sound & Procedural DSP")]
    public static void OpenScene04() => OpenShowcaseScene(Scene04Path);

    private static void OpenShowcaseScene(string path)
    {
        if (!File.Exists(path))
        {
            BuildAllShowcaseScenes(openFirstScene: false);
        }
        if (File.Exists(path) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }

    public static void BuildAllShowcaseScenes(bool openFirstScene)
    {
        EnsureFolder(ShowcaseFolder);

        // ลบ Scene เก่าที่ไม่ใช้แล้ว เพื่อไม่ให้ซ้ำซ้อน
        if (File.Exists(LegacyScene02Path))
        {
            AssetDatabase.DeleteAsset(LegacyScene02Path);
        }
        if (File.Exists(LegacyScene05Path))
        {
            AssetDatabase.DeleteAsset(LegacyScene05Path);
        }

        Sprite panelSprite = EnsurePanelSprite();
        Sprite heatTestSprite = EnsureHeatTestSprite();

        BuildSceneInAdditive(Scene01Path, scene => PopulateScene01_MamaPig(scene, panelSprite));
        BuildSceneInAdditive(Scene02Path, scene => PopulateScene02_WolvesAndSpawning(scene, panelSprite));
        BuildSceneInAdditive(Scene03Path, scene => PopulateScene03_AllUsedVfxAndShaders(scene, panelSprite, heatTestSprite));
        BuildSceneInAdditive(Scene04Path, scene => PopulateScene04_AudioSoundboard(scene, panelSprite));

        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), VersionFilePath), ShowcaseVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Pooyan Portfolio] สร้างครบทั้ง 4 Programmer Showcase Scenes เรียบร้อยแล้วที่ Assets/Scenes/Portfolio_Showcase/ (ไม่ได้เพิ่มเข้า Build Settings)");

        if (openFirstScene && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(Scene01Path, OpenSceneMode.Single);
        }
    }

    private static void BuildSceneInAdditive(string scenePath, Action<Scene> populate)
    {
        Scene prevActive = SceneManager.GetActiveScene();

        // ถ้า Scene เป้าหมายเปิดค้างอยู่ใน Editor อยู่แล้ว ให้รีบิลด์ทับ In-Place ทันที (ป้องกัน Unity ไม่ยอม Save ทับ Scene ที่เปิดอยู่)
        Scene alreadyLoaded = SceneManager.GetSceneByPath(scenePath);
        if (alreadyLoaded.IsValid() && alreadyLoaded.isLoaded)
        {
            SceneManager.SetActiveScene(alreadyLoaded);
            try
            {
                foreach (GameObject rootGo in alreadyLoaded.GetRootGameObjects())
                {
                    UnityEngine.Object.DestroyImmediate(rootGo);
                }
                populate(alreadyLoaded);
                EditorSceneManager.MarkSceneDirty(alreadyLoaded);
                EditorSceneManager.SaveScene(alreadyLoaded, scenePath);
            }
            finally
            {
                if (prevActive.IsValid() && prevActive.isLoaded)
                {
                    SceneManager.SetActiveScene(prevActive);
                }
            }
            return;
        }

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(newScene);

        try
        {
            populate(newScene);
            EditorSceneManager.SaveScene(newScene, scenePath);
        }
        finally
        {
            if (prevActive.IsValid() && prevActive.isLoaded)
            {
                SceneManager.SetActiveScene(prevActive);
            }
            EditorSceneManager.CloseScene(newScene, true);
        }
    }

    // =================================================================================================
    // SCENE 01 : MAMA PIG & PIGLET SYSTEMS — 5 LARGE INTERACTIVE DIORAMA BOOTHS (NO STATIC PREFABS)
    // =================================================================================================
    private static void PopulateScene01_MamaPig(Scene scene, Sprite panelSprite)
    {
        SetupStageRootAndCamera(
            scene,
            "POOYAN REMAKE — SCENE 01: MAMA PIG & PIGLET LIVE SYSTEMS",
            "6 ฉากจำลองการทำงานจริงฝั่งแม่หมู & ลูกหมู (มี Loop แยกแต่ละช่อง): ยิงธนูแตก, ปาเนื้อ, โดนหินตาย, เกราะกระเช้า, ลูกหมูดึงเชือก และธนูร่วงปักพื้น",
            overviewPos: new Vector3(0f, 0.75f, -10f), overviewSize: 10.2f,
            topPos: new Vector3(0f, 3.85f, -10f), topSize: 5.3f,
            bottomPos: new Vector3(0f, -3.85f, -10f), bottomSize: 5.0f,
            panelSprite: panelSprite,
            boardWidth: 30.2f,
            headerY: 8.45f);

        RuntimeAnimatorController springMamaCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Mama/SpringMamaPig.controller");
        RuntimeAnimatorController springPigletCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Piglet/SpringPiglet1_0.controller");
        RuntimeAnimatorController springWolfCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Wolf/SpringGPX.controller");

        Sprite mamaSprite = LoadSpriteByGuid("1f2fd1b67c285ad469ae135c03e71a35");
        Sprite pigletSprite = LoadSpriteByGuid("c1e27b5c57c2e4e40a195e37088e930f");
        Sprite topArmorSprite = LoadSpriteByGuid("9ea8ca02761667642838ffb5879f9a73");
        Sprite bottomArmorSprite = LoadSpriteByGuid("eb9e5750e746391408d72e0b182edc53");
        Sprite wolfSprite = LoadSpriteByGuid("049a76ae25d06a54f909a66c10911824");
        Sprite balloonSprite = LoadSpriteByGuid("e360a17f218bf274db937298618caba8");

        GameObject arrowSpringPrefab = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/ArrowSpring.prefab");
        GameObject meatSpringPrefab = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/MeatSpring.prefab");
        Sprite realMeatSprite = ExtractFirstSprite(meatSpringPrefab) ?? LoadSpriteByGuid("56b467cfe8c60304ea3126a27d6b91bf");
        GameObject rockPrefab = LoadAsset<GameObject>("Assets/Scenes/1_Autumn/Prefab/Wolf/Rock.prefab");
        GameObject ropePulleyPrefab = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/RopePulleyFX.prefab");
        Material ropeMat = LoadAsset<Material>("Assets/Scenes/All/Resources/Material/RopeMaterial.mat");

        GameObject fxBalloonPop = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/BalloonPopFX.prefab");
        GameObject fxRockHitBasket = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/RockHitBasket.prefab");
        GameObject fxRockBreak = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/RockBreak.prefab");
        GameObject fxGroundDust = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/GroundDustImpactFX.prefab");

        var boothsGroup = CreateGroup(scene, "MamaPig_6_Live_Diorama_Booths");

        const float cardW = 9.4f;
        const float cardH = 7.0f;
        const float topRowY = 3.55f;
        const float botRowY = -4.00f;
        const float rigScale = 0.93f;
        const float realScaleRatio = rigScale / 1.327f; // อัตราส่วนตรงกับ SpringPlayer ใน Spring.unity เป๊ะ 100%

        // =============================================================================================
        // ช่อง 1 : ยิงธนู + หมาป่าห้อยลงมา -> แม่หมูยิงลูกโป่งแตก!
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(boothsGroup.transform, "01_ShootArrow_PopBalloon", new Vector3(-10.1f, topRowY, 0f), cardW, cardH,
                "1. ท่ายิงธนู (Shoot) → ปล่อย Arrow ยิงลูกโป่งหมาป่าแตก",
                "Code: PlayerController.cs + ArrowShooter.cs + Arrow.cs + Balloon.cs\n• เล่นอนิเมชัน Shoot (ShootAnimRoutine) → ArrowShooter.TryShoot() (Object Pool สูงสุด 2 ดอก)\n• ธนูพุ่งชน Balloon.TakeDamage() → BalloonPopFX + หมาป่าร่วงพื้น (Falling → GroundDustImpactFX)",
                panelSprite, new Color(0.48f, 0.28f, 0.16f, 0.94f));

            AddDioramaStageGuides(booth, panelSprite, shaftX: 2.45f, showVerticalRope: true);

            GameObject rig = CreateAuthenticMamaBasketRig(booth, "MamaShootRig", new Vector3(2.45f, 0.45f, 0f), rigScale,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mamaAnim, out Transform mamaBody, out Transform topArmor, out Transform botArmor, out Transform firePt);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.MamaShootPopWolf;
            demo.targetAnimator = mamaAnim;
            demo.actorTransform = rig.transform;
            demo.mamaBodyTransform = mamaBody;
            demo.topArmorTransform = topArmor;
            demo.bottomArmorTransform = botArmor;
            demo.spawnPoint = firePt;
            demo.projectilePrefab = arrowSpringPrefab;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.fxBalloonPopPrefab = fxBalloonPop;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.dioramaScaleRatio = realScaleRatio;
            demo.dioramaFloorLocalY = -1.06f;
            demo.primaryStateName = "Idle";
            demo.secondaryStateName = "Shoot";
            demo.actionInterval = 3.6f;
        }

        // =============================================================================================
        // ช่อง 2 : หยิบเนื้อขนาดจริง + ปาเนื้อใส่หมาป่าที่ห้อยลงมา!
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(boothsGroup.transform, "02_HoldingMeat_ThrowAtWolves", new Vector3(0.0f, topRowY, 0f), cardW, cardH,
                "2. หยิบเนื้อขนาดเท่าเกมจริง (HoldingMeat) → ปาเนื้อกวาดหมาป่า",
                "Code: PlayerController.cs + MeatWeapon.cs + MeatPickupIndicator.cs\n• ขึ้นถึง maxY (IsAtTop) หยิบเนื้อจากรอก → แสดง HeldMeatVisual สัดส่วนเดียวกับเกมจริง\n• ปา MeatSpring วิถี 2-Phase Parabolic + Soft Homing ทะลุโล่ กวาดหมาป่าร่วงคอมโบ (400→800→1600+)",
                panelSprite, new Color(0.46f, 0.22f, 0.28f, 0.94f));

            AddDioramaStageGuides(booth, panelSprite, shaftX: 2.45f, showVerticalRope: true);

            GameObject rig = CreateAuthenticMamaBasketRig(booth, "MamaMeatRig", new Vector3(2.45f, 0.45f, 0f), rigScale,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mamaAnim, out Transform mamaBody, out Transform topArmor, out Transform botArmor, out Transform firePt);

            // HeldMeatVisual ตรงตาม PlayerController.Awake() & Spring.unity เป๊ะ!
            GameObject heldMeat = new GameObject("HeldMeatVisual");
            heldMeat.transform.SetParent(rig.transform, false);
            heldMeat.transform.localPosition = new Vector3(-0.55f, 0.05f, 0f);
            heldMeat.transform.localScale = new Vector3(1.8f, 0.9f, 1f);
            var hmvSr = heldMeat.AddComponent<SpriteRenderer>();
            hmvSr.sprite = realMeatSprite;
            hmvSr.sortingOrder = 17;
            heldMeat.SetActive(false);

            // MeatPickupIndicator ตรงตาม Spring.unity (2.1327, 1.5151) * realScaleRatio เป๊ะ!
            GameObject indicator = new GameObject("MeatPickupIndicator");
            indicator.transform.SetParent(booth, false);
            indicator.transform.localPosition = new Vector3(2.45f, 2.22f, 0f);
            indicator.transform.localScale = new Vector3(2.1327f * realScaleRatio, 1.5151f * realScaleRatio, 1f);
            var indSr = indicator.AddComponent<SpriteRenderer>();
            indSr.sprite = realMeatSprite;
            indSr.sortingOrder = 17;

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.MamaThrowMeatWolves;
            demo.targetAnimator = mamaAnim;
            demo.actorTransform = rig.transform;
            demo.mamaBodyTransform = mamaBody;
            demo.topArmorTransform = topArmor;
            demo.bottomArmorTransform = botArmor;
            demo.spawnPoint = firePt;
            demo.projectilePrefab = meatSpringPrefab;
            demo.heldVisualObject = heldMeat;
            demo.indicatorObject = indicator;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.fxBalloonPopPrefab = fxBalloonPop;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.dioramaScaleRatio = realScaleRatio;
            demo.dioramaFloorLocalY = -1.06f;
            demo.primaryStateName = "Idle";
            demo.secondaryStateName = "Shoot";
            demo.actionInterval = 4.2f;
        }

        // =============================================================================================
        // ช่อง 3 : หมาป่าห้อยมาปาหินใส่ตัวแม่หมู -> แม่หมูโดนดาเมจกระเด็นตาย!
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(boothsGroup.transform, "03_WolfRock_MamaFallDeath", new Vector3(10.1f, topRowY, 0f), cardW, cardH,
                "3. ท่าตายแม่หมู (Fall) — หมาป่าปาหินเข้าช่องกลาง → กระเด็นตกกระเช้า",
                "Code: PlayerController.cs + WolfProjectile.cs + GameManager.cs\n• หินหมาป่าพุ่งเข้าช่องกลาง (ระหว่างเกราะบน-ล่าง) → ชน PlayerController.TakeDamage()\n• เล่นท่า Fall + DeathBounceRoutine() กระเด็นโค้งตกพื้น (กระเช้าค้างบนเชือก) → LoseLife()",
                panelSprite, new Color(0.50f, 0.18f, 0.18f, 0.94f));

            AddDioramaStageGuides(booth, panelSprite, shaftX: 2.45f, showVerticalRope: true);

            GameObject rig = CreateAuthenticMamaBasketRig(booth, "MamaDeathRig", new Vector3(2.45f, 0.72f, 0f), rigScale,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mamaAnim, out Transform mamaBody, out Transform topArmor, out Transform botArmor, out _);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.MamaHitByRockDeath;
            demo.targetAnimator = mamaAnim;
            demo.actorTransform = rig.transform;
            demo.mamaBodyTransform = mamaBody;
            demo.topArmorTransform = topArmor;
            demo.bottomArmorTransform = botArmor;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.rockPrefab = rockPrefab;
            demo.fxRockBreakPrefab = fxRockBreak;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.dioramaScaleRatio = realScaleRatio;
            demo.dioramaFloorLocalY = -1.06f;
            demo.primaryStateName = "Idle";
            demo.secondaryStateName = "Fall";
            demo.actionInterval = 4.3f;
        }

        // =============================================================================================
        // ช่อง 4 : ระบบป้องกันของกระเช้า — หมาป่าบินลงมาปาหินใส่เกราะบนก่อน แล้วค่อยปาเกราะล่าง!
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(boothsGroup.transform, "04_BasketArmor_TopAndBottomDefense", new Vector3(-10.1f, botRowY, 0f), cardW, cardH,
                "4. ระบบเกราะกระเช้า (TopArmor & BottomArmor) — กันหินจากบนและล่าง",
                "Code: TopArmor & BottomArmor (Tag: \"Shield\" + PolygonCollider2D) + WolfProjectile.cs\n• หมาป่าลอยลงมาปาหินใส่เกราะบนก่อน (กันหินด้านบน) → แล้วลอยลงมาปาใส่เกราะล่าง (กันหินด้านล่าง)\n• ชน Tag \"Shield\" → เรียก RockHitBasket FX + สะท้อนทำลายหิน แม่หมูปลอดภัย 100%",
                panelSprite, new Color(0.20f, 0.40f, 0.54f, 0.94f));

            AddDioramaStageGuides(booth, panelSprite, shaftX: 2.25f, showVerticalRope: true);

            GameObject rig = CreateAuthenticMamaBasketRig(booth, "MamaArmorRig", new Vector3(2.25f, 0.55f, 0f), rigScale * 1.03f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mamaAnim, out Transform mamaBody, out Transform topArmor, out Transform botArmor, out _);

            // ป้ายชี้เกราะบน / เกราะล่าง
            CreateLabel(booth, "◀ TopArmor [Shield]", new Vector3(3.62f, 1.18f, 0f), 10, new Color(0.55f, 0.92f, 1f), TextAnchor.MiddleCenter);
            CreateLabel(booth, "◀ BottomArmor [Shield]", new Vector3(3.68f, -0.02f, 0f), 10, new Color(0.55f, 0.92f, 1f), TextAnchor.MiddleCenter);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.BasketArmorBlockDemo;
            demo.targetAnimator = mamaAnim;
            demo.actorTransform = rig.transform;
            demo.mamaBodyTransform = mamaBody;
            demo.topArmorTransform = topArmor;
            demo.bottomArmorTransform = botArmor;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.rockPrefab = rockPrefab;
            demo.fxRockHitBasketPrefab = fxRockHitBasket;
            demo.dioramaScaleRatio = realScaleRatio;
            demo.dioramaFloorLocalY = -1.06f;
            demo.primaryStateName = "Idle";
            demo.actionInterval = 5.2f;
        }

        // =============================================================================================
        // ช่อง 5 : ระบบลูกหมูดึงเชือก (จำลองฉากลูกหมูดึงเชือกรอกแบบเต็มรูปแบบ)
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(boothsGroup.transform, "05_PigletElevatorRope_System", new Vector3(0.0f, botRowY, 0f), cardW, cardH,
                "5. ระบบลูกหมูดึงเชือกรอก (ElevatorRope) — เชือกโค้ง UV Scroll + ฝุ่นรอก",
                "Code: ElevatorRope.cs + RopeMaterial.mat + RopePulleyFX.prefab\n• LineRenderer วาดเชือกโค้ง 90° รอบรอก (Quarter-Circle Arc 6 Segments) + เลื่อนลาย UV ตาม deltaY\n• สลับท่า Piglet_Idle ↔ Piglet_PullRobe อัตโนมัติ และปล่อยฝุ่น RopePulleyFX เฉพาะตอนดึงขึ้น-ลง",
                panelSprite, new Color(0.22f, 0.46f, 0.34f, 0.94f));

            AddDioramaStageGuides(booth, panelSprite, shaftX: -1.75f, showVerticalRope: false);

            // ขอบหน้าผาด้านขวาบนให้ลูกหมู 2 ตัวมายืนดึงเชือกเหมือนฉากจริง (ไม่มีกล่องสี่เหลี่ยมตรงมุมหักเชือก ให้เชือกโค้งธรรมชาติเหมือนเกมจริง!)
            CreatePanel(booth, "CliffLedge", panelSprite, new Vector3(1.55f, 1.48f, 0f), new Vector2(5.4f, 0.16f), new Color(0.40f, 0.62f, 0.36f, 0.85f), 4);
            CreatePanel(booth, "CliffWall", panelSprite, new Vector3(1.55f, 0.08f, 0f), new Vector2(5.4f, 2.65f), new Color(0.18f, 0.24f, 0.22f, 0.45f), 1);

            // แม่หมู + กระเช้าเต็มชุด (สัดส่วนตรงกับเกมจริง) ห้อยอยู่กับเชือกฝั่งซ้าย
            GameObject gondolaRig = CreateAuthenticMamaBasketRig(booth, "ElevatorGondolaRig", new Vector3(-1.75f, -0.45f, 0f), rigScale * 0.92f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out _, out _, out _, out _, out _);

            // ลูกหมู 2 ตัวบนหน้าผา (สัดส่วนตรงกับ Spring.unity: 0.25754 * realScaleRatio)
            float pigScale = 0.25754f * realScaleRatio * 1.15f;
            GameObject pig1 = CreateExactScaleAnimatedNode(booth, "Piglet_1", pigletSprite, springPigletCtrl, new Vector3(1.15f, 1.98f, 0f), pigScale, 15, out Animator pigAnim1);
            GameObject pig2 = CreateExactScaleAnimatedNode(booth, "Piglet_2", pigletSprite, springPigletCtrl, new Vector3(2.15f, 1.98f, 0f), pigScale, 15, out Animator pigAnim2);

            GameObject handAnchor = new GameObject("PigletHandAnchor");
            handAnchor.transform.SetParent(booth, false);
            handAnchor.transform.localPosition = new Vector3(1.95f, 2.02f, 0f);

            GameObject ropeObj = new GameObject("ElevatorRopeLine");
            ropeObj.transform.SetParent(booth, false);
            var lr = ropeObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sortingLayerName = "Default";
            lr.sortingOrder = 15;
            lr.textureMode = LineTextureMode.Tile;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.14f;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            if (ropeMat != null) lr.sharedMaterial = ropeMat;

            ParticleSystem dustPs = null;
            if (ropePulleyPrefab != null)
            {
                GameObject dustObj = InstantiateDisplayPrefab(ropePulleyPrefab, scene, booth, new Vector3(-1.55f, 2.00f, 0f), 0f, disableScripts: true);
                dustPs = dustObj != null ? dustObj.GetComponentInChildren<ParticleSystem>(true) : null;
                if (dustPs != null)
                {
                    var main = dustPs.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.startSizeMultiplier = Mathf.Clamp(main.startSizeMultiplier * 1.6f, 0.16f, 0.45f);
                    var psr = dustPs.GetComponent<ParticleSystemRenderer>();
                    if (psr != null) psr.sortingOrder = 19;
                }
            }

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.PigletElevatorStage;
            demo.targetAnimator = pigAnim1;
            demo.secondaryAnimator = pigAnim2;
            demo.actorTransform = gondolaRig.transform;
            demo.ropeGondolaTransform = gondolaRig.transform;
            demo.ropeHandTransform = handAnchor.transform;
            demo.ropeLineRenderer = lr;
            demo.pulleyDustFX = dustPs;
            demo.ropePulleyLocalY = 2.02f;
            demo.ropeCornerRadius = 0.28f;
            demo.ropeCornerSegments = 6;
            demo.ropeGondolaAnchorOffsetY = 0.62f;
            demo.elevatorMinY = -0.45f;
            demo.elevatorMaxY = 0.85f;
            demo.primaryStateName = "Piglet_Idle";
            demo.secondaryStateName = "Piglet_PullRobe";
            demo.actionInterval = 4.4f;
            demo.RefreshEditModeRopePreview();
        }

        // =============================================================================================
        // ช่อง 6 : ยิงธนูโดนตัวหมาป่า/โล่ -> ธนูดิ่งลงมาปักพื้น สั่นสะเทือน + ฝุ่น + ค่อยๆ จางหาย!
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(boothsGroup.transform, "06_ArrowDeflect_StickIntoGround", new Vector3(10.1f, botRowY, 0f), cardW, cardH,
                "6. ธนูร่วงปักพื้น (Arrow Deflect & StickIntoGround)",
                "Code: Arrow.cs (DeflectDown -> StickIntoGround -> StickAndFadeRoutine)\n• ยิงโดนตัวหมาป่า (BiteArrow) / โล่ → ธนูหมุน 90° ดิ่งลงพื้นในคอลัมน์เดิม (เจาะลูกโป่งล่างต่อได้)\n• แตะพื้นแล้วเอียงมุมสุ่ม ±8° + ฝุ่น GroundDustImpactFX + สั่น Quiver 0.15s + ปักค้าง 1s แล้วจางหาย",
                panelSprite, new Color(0.28f, 0.36f, 0.56f, 0.94f));

            AddDioramaStageGuides(booth, panelSprite, shaftX: 2.45f, showVerticalRope: true);

            GameObject rig = CreateAuthenticMamaBasketRig(booth, "MamaArrowStickRig", new Vector3(2.45f, 0.45f, 0f), rigScale,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mamaAnim, out Transform mamaBody, out Transform topArmor, out Transform botArmor, out Transform firePt);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.MamaArrowStickGroundDemo;
            demo.targetAnimator = mamaAnim;
            demo.actorTransform = rig.transform;
            demo.mamaBodyTransform = mamaBody;
            demo.topArmorTransform = topArmor;
            demo.bottomArmorTransform = botArmor;
            demo.spawnPoint = firePt;
            demo.projectilePrefab = arrowSpringPrefab;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.dioramaScaleRatio = realScaleRatio;
            demo.dioramaFloorLocalY = -1.06f;
            demo.primaryStateName = "Idle";
            demo.secondaryStateName = "Shoot";
            demo.actionInterval = 4.2f;
        }
    }

    private static void AddDioramaStageGuides(Transform booth, Sprite panelSprite, float shaftX, bool showVerticalRope)
    {
        // เส้นพื้นดินและกรอบเวทีด้านในของแต่ละบูธ
        CreatePanel(booth, "StageFloorLine", panelSprite, new Vector3(0f, -1.32f, 0f), new Vector2(8.85f, 0.08f), new Color(0.55f, 0.72f, 0.48f, 0.42f), 2);
        CreatePanel(booth, "StageBackdropTint", panelSprite, new Vector3(0f, 0.62f, 0f), new Vector2(8.85f, 3.88f), new Color(0.08f, 0.11f, 0.16f, 0.55f), 0);
        if (showVerticalRope)
        {
            // เส้นเชือกแนวตั้งห้อยกระเช้าแม่หมูจากรอกด้านบน
            CreatePanel(booth, "VerticalElevatorRope", panelSprite, new Vector3(shaftX + 0.03f, 1.55f, 0f), new Vector2(0.065f, 2.05f), new Color(0.76f, 0.64f, 0.44f, 0.85f), 11);
        }
    }

    // =================================================================================================
    // SCENE 02 : WOLF SPAWNING FORMATIONS, LADDER/BOULDER, BALLOON SYSTEM & BONUS BOSS FRUIT (8 BOOTHS)
    // =================================================================================================
    private static void PopulateScene02_WolvesAndSpawning(Scene scene, Sprite panelSprite)
    {
        SetupStageRootAndCamera(
            scene,
            "POOYAN REMAKE — SCENE 02: WOLF SPAWNER, AI, BALLOON & BOSS FRUIT SYSTEMS",
            "9 ฉากจำลองโค้ดฝั่งหมาป่า: 4 แพตเทิร์นโดด, Boss Lock, บันได/ดันหิน, ลูกโป่งด่านคี่ vs คู่, บอสปาผลไม้ขนาดจริง และสลับทิศโรยตัวลง ↔ บินขึ้น",
            overviewPos: new Vector3(0f, 0.75f, -10f), overviewSize: 10.4f,
            topPos: new Vector3(0f, 3.85f, -10f), topSize: 5.4f,
            bottomPos: new Vector3(0f, -3.75f, -10f), bottomSize: 5.3f,
            panelSprite: panelSprite,
            boardWidth: 33.0f,
            headerY: 8.55f);

        RuntimeAnimatorController summerWolfCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/5_Summer/Animation/Wolf/SummerGPX.controller");
        RuntimeAnimatorController springWolfCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Wolf/SpringGPX.controller");
        RuntimeAnimatorController bonusBossCtrl = LoadAssetByGuid<RuntimeAnimatorController>("6abd8c7058a11334fa5ff2a53a4ae2eb")
            ?? LoadAssetByGuid<RuntimeAnimatorController>("ec12458f4a9326f458a92c1f0b285d70")
            ?? springWolfCtrl;

        Sprite wolfSprite = LoadSpriteByGuid("049a76ae25d06a54f909a66c10911824");
        Sprite wolfWaitLadderSprite = LoadSpriteByGuid("3585573ee1fb4f645ae943116f2e8e86");
        Sprite wolfPushIdleSprite = LoadSpriteByGuid("653025199539f5641bceca521027f7e1");
        Sprite balloonSprite = LoadSpriteByGuid("e360a17f218bf274db937298618caba8");
        Sprite realBoulderSprite = LoadSpriteByGuid("76899f0ad9ab6f047b91a28dc4c893c5") ?? LoadSpriteByGuid("afcb18ef17c4fff46b9f3dd000caf829");

        // Sprite ผลไม้จริงทั้ง 3 ชนิดจาก Fruit.prefab (Strawberry, Cherry, Peach)
        Sprite strawberrySprite = LoadSpriteByGuid("44604557a9dc5c546993994f552a4440");
        Sprite cherrySprite = LoadSpriteByGuid("3ea5ab6d7f89c224c835e3a36cef0e58");
        Sprite peachSprite = LoadSpriteByGuid("689cd0e6303411240af3932e83deaaaf");

        GameObject arrowSpringPrefab = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/ArrowSpring.prefab");
        GameObject fruitPrefab = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/Fruit.prefab");
        GameObject fxBalloonPop = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/BalloonPopFX.prefab");
        GameObject fxBalloonHit = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/BalloonHitFX.prefab");
        GameObject fxGroundDust = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/GroundDustImpactFX.prefab");
        GameObject fxWolfBiteLadder = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/WolfBiteLadderFX.prefab");

        var wolfGroup = CreateGroup(scene, "Wolf_9_Live_System_Booths");

        const float topCardW = 6.15f;
        const float botCardW = 7.45f;
        const float cardH = 6.90f;
        const float topRowY = 3.55f;
        const float botRowY = -3.90f;
        float[] topXs = { -13.00f, -6.50f, 0.00f, 6.50f, 13.00f };
        float[] botXs = { -11.75f, -3.92f, 3.92f, 11.75f };

        // =============================================================================================
        // แถวบน (ช่อง 1 - 5) : 4 แพตเทิร์นการเกิดและการโดด + Odd/Even Boss Lock
        // =============================================================================================
        CreateWolfFormationBooth(wolfGroup.transform, "01_Pattern0_Single_35pct", new Vector3(topXs[0], topRowY, 0f), topCardW, cardH,
            "1. Pattern 0: Single (35%)",
            "Code: WolfSpawner.cs -> SpawnWaveRoutine()\n• สุ่ม 1 เลนจาก 5 เลน (laneX[])\n• WalkingBeforeDrop → BlowBalloon → Float\n• ลอยส่าย Sine-Wave (sin(t * 4.5))",
            ShowcaseDemoType.WolfFormationSingle, wolfSprite, balloonSprite, summerWolfCtrl, realBoulderSprite, panelSprite,
            new Color(0.42f, 0.24f, 0.22f, 0.94f), 3.4f, showLaneGuides: true);

        CreateWolfFormationBooth(wolfGroup.transform, "02_Pattern1_VerticalStack_28pct", new Vector3(topXs[1], topRowY, 0f), topCardW, cardH,
            "2. Pattern 1: Vertical Stack (28%)",
            "Code: WolfSpawner.cs (ซ้อนเลนเดียวกัน 2-4 ตัว)\n• ใช้ sharedSpeed เท่ากันทั้งแถว\n• สูตร stackDelay = 1.68f / sharedSpeed\n• เรียงระยะแนวตั้งเท่ากันเป๊ะเหมือน Pooyan ต้นฉบับ",
            ShowcaseDemoType.WolfFormationStack, wolfSprite, balloonSprite, summerWolfCtrl, realBoulderSprite, panelSprite,
            new Color(0.46f, 0.30f, 0.16f, 0.94f), 3.8f, showLaneGuides: true);

        CreateWolfFormationBooth(wolfGroup.transform, "03_Pattern2_DiagonalStep_22pct", new Vector3(topXs[2], topRowY, 0f), topCardW, cardH,
            "3. Pattern 2: Diagonal Step (22%)",
            "Code: WolfSpawner.cs (ขยับเลนทแยงมุม)\n• สุ่มทิศ step = +1 หรือ -1 (ขยับทีละเลน)\n• ปล่อยห่างกัน 0.5s ด้วย sharedSpeed เดียวกัน\n• บีบให้ผู้เล่นขยับกระเช้าขึ้น-ลงรับมือ",
            ShowcaseDemoType.WolfFormationDiagonal, wolfSprite, balloonSprite, summerWolfCtrl, realBoulderSprite, panelSprite,
            new Color(0.36f, 0.38f, 0.16f, 0.94f), 3.8f, showLaneGuides: true);

        CreateWolfFormationBooth(wolfGroup.transform, "04_Pattern3_HorizontalSync_15pct", new Vector3(topXs[3], topRowY, 0f), topCardW, cardH,
            "4. Pattern 3: Horizontal Sync (15%)",
            "Code: WolfSpawner.cs (หน้ากระดานโดดพร้อมกัน!)\n• สูตร spawnX = targetX - uniformWalkDistance\n• ทุกตัวเดินระยะเท่ากัน → เป่าลูกโป่งพร้อมกัน\n• ลอยลง/ขึ้นเป็นแผงหน้ากระดานเดียวกันเป๊ะ",
            ShowcaseDemoType.WolfFormationHorizontal, wolfSprite, balloonSprite, summerWolfCtrl, realBoulderSprite, panelSprite,
            new Color(0.18f, 0.38f, 0.48f, 0.94f), 3.6f, showLaneGuides: true);

        CreateWolfFormationBooth(wolfGroup.transform, "05_OddEven_Direction_And_BossLock", new Vector3(topXs[4], topRowY, 0f), topCardW, cardH,
            "5. Odd/Even Wave & Boss Quota Lock",
            "Code: WolfSpawner.cs + BossWolf.cs\n• ด่านคี่ลอยลง | ด่านคู่ลอยขึ้น + บอสชมพูกระพริบ\n• สลับ BlowBalloon → Drop/Float ตามจริง\n• Boss Quota Lock: เหลือโควต้า 1 ถึงปล่อยบอส",
            ShowcaseDemoType.WolfDirectionOddEven, wolfSprite, balloonSprite, springWolfCtrl, realBoulderSprite, panelSprite,
            new Color(0.34f, 0.24f, 0.48f, 0.94f), 3.6f, showLaneGuides: true);

        // =============================================================================================
        // แถวล่าง (ช่อง 6 - 9) : บันได/ดันหิน, ลูกโป่งด่านคี่ vs คู่, บอสปาผลไม้ และสลับทิศโรยตัวลง ↔ บินขึ้น
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(wolfGroup.transform, "06_Ladder_And_Boulder_HoldPoses", new Vector3(botXs[0], botRowY, 0f), botCardW, cardH,
                "6. บันได (Ladder) & ดันหิน (Boulder) + ท่าค้างจริง",
                "Code: LadderSystem.cs + BoulderSystem.cs + Wolf.cs\n• ซ้าย: ปีนบันได (Climb) → ค้างท่า WaitLadder → กะพริบงับ BiteRobe\n• ขวา: ดันหินยักษ์ (Push) → หยุดพักค้างท่า PushIdle เหมือนเกมจริง",
                panelSprite, new Color(0.24f, 0.44f, 0.32f, 0.94f));

            GameObject formRoot = new GameObject("LadderBoulderStage");
            formRoot.transform.SetParent(booth, false);
            formRoot.transform.localPosition = new Vector3(0f, 0.30f, 0f);

            CreatePanel(formRoot.transform, "GroundLine", panelSprite, new Vector3(0f, -1.25f, 0f), new Vector2(6.8f, 0.06f), new Color(0.75f, 0.65f, 0.45f, 0.55f), 2);
            CreatePanel(formRoot.transform, "SplitDivider", panelSprite, new Vector3(-0.35f, 0.45f, 0f), new Vector2(0.03f, 3.2f), new Color(1f, 1f, 1f, 0.14f), 2);
            CreatePanel(formRoot.transform, "CliffLedgeRight", panelSprite, new Vector3(1.55f, 0.36f, 0f), new Vector2(3.4f, 0.14f), new Color(0.48f, 0.62f, 0.40f, 0.72f), 3);

            Color woodRail = new Color(0.66f, 0.44f, 0.22f, 0.95f);
            Color woodRung = new Color(0.80f, 0.58f, 0.32f, 0.95f);
            CreatePanel(formRoot.transform, "LadderRail_L", panelSprite, new Vector3(-2.12f, 0.35f, 0f), new Vector2(0.10f, 3.10f), woodRail, 6);
            CreatePanel(formRoot.transform, "LadderRail_R", panelSprite, new Vector3(-1.32f, 0.35f, 0f), new Vector2(0.10f, 3.10f), woodRail, 6);
            for (int r = 0; r < 6; r++)
            {
                float ry = -0.95f + r * 0.52f;
                CreatePanel(formRoot.transform, $"LadderRung_{r}", panelSprite, new Vector3(-1.72f, ry, 0f), new Vector2(0.82f, 0.08f), woodRung, 7);
            }

            CreateLabel(formRoot.transform, "LadderSystem", new Vector3(-1.72f, 2.08f, 0f), 11, new Color(1f, 0.88f, 0.55f), TextAnchor.MiddleCenter);
            CreateLabel(formRoot.transform, "BoulderSystem", new Vector3(1.45f, 2.08f, 0f), 11, new Color(0.68f, 0.92f, 1f), TextAnchor.MiddleCenter);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.WolfLadderAndBoulder;
            demo.formationRoot = formRoot.transform;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = summerWolfCtrl;
            demo.wolfPushController = springWolfCtrl;
            demo.wolfWaitLadderSprite = wolfWaitLadderSprite;
            demo.wolfPushIdleSprite = wolfPushIdleSprite;
            demo.boulderDemoSprite = realBoulderSprite;
            demo.fxWolfBiteLadderPrefab = fxWolfBiteLadder;
            demo.actionInterval = 3.8f;
        }

        {
            Transform booth = CreateBoothCard(wolfGroup.transform, "07_BalloonSystem_OddMany_Vs_EvenTough", new Vector3(botXs[1], botRowY, 0f), botCardW, cardH,
                "7. ระบบลูกโป่ง: ด่านคี่ (เยอะ 1 HP) vs ด่านคู่ (อึด 3-5 HP)",
                "Code: Balloon.cs + Wolf.OnBalloonDamaged()\n• ซ้าย (ด่านคี่): มาเยอะแต่ 1 นัดแตก (1 HP)\n• ขวา (ด่านคู่): อึดหลาย HP โดนตอดแล้วหดตัว+ช้าลง+เปลี่ยนสี (ฟ้า→เหลือง→แดง→แตก)",
                panelSprite, new Color(0.20f, 0.42f, 0.58f, 0.94f));

            GameObject formRoot = new GameObject("BalloonCompareStage");
            formRoot.transform.SetParent(booth, false);
            formRoot.transform.localPosition = new Vector3(0f, 0.30f, 0f);

            CreatePanel(formRoot.transform, "GroundLine", panelSprite, new Vector3(0f, -1.25f, 0f), new Vector2(6.8f, 0.06f), new Color(0.75f, 0.65f, 0.45f, 0.45f), 2);
            CreatePanel(formRoot.transform, "CenterDivider", panelSprite, new Vector3(0.05f, 0.45f, 0f), new Vector2(0.04f, 3.25f), new Color(1f, 1f, 1f, 0.22f), 3);

            CreateLabel(formRoot.transform, "ด่านคี่: เยอะ (1 นัดแตก)", new Vector3(-1.68f, 2.08f, 0f), 11, new Color(1f, 0.65f, 0.72f), TextAnchor.MiddleCenter);
            CreateLabel(formRoot.transform, "ด่านคู่: อึด (3→2→1 HP)", new Vector3(1.68f, 2.08f, 0f), 11, new Color(0.55f, 0.90f, 1f), TextAnchor.MiddleCenter);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.WolfBalloonDurabilityCompare;
            demo.formationRoot = formRoot.transform;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = summerWolfCtrl;
            demo.projectilePrefab = arrowSpringPrefab;
            demo.fxBalloonPopPrefab = fxBalloonPop;
            demo.fxBalloonHitPrefab = fxBalloonHit;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.dioramaFloorLocalY = -1.05f;
            demo.actionInterval = 4.3f;
        }

        {
            Transform booth = CreateBoothCard(wolfGroup.transform, "08_BonusBoss_ThrowRealFruits", new Vector3(botXs[2], botRowY, 0f), botCardW, cardH,
                "8. บอสปาผลไม้ (ขนาดจริง + Sprite ผลไม้จริง 3 ชนิด)",
                "Code: BonusBossWolf.cs + TreeTopBossWolf.cs + BonusFruit.cs\n• บอสวิ่งไป-กลับบนยอดไม้ เล่นท่า ThrowFruit แล้วปล่อยผลไม้ขนาดตามจริง\n• ใช้ Sprite จริงครบ 3 ชนิด (Strawberry 1.5x / Cherry 1.3x / Peach 1.5x) + GlowAura & SparkleTrail",
                panelSprite, new Color(0.54f, 0.26f, 0.42f, 0.94f));

            CreatePanel(booth, "TopBranchBar", panelSprite, new Vector3(0f, 1.38f, 0f), new Vector2(6.8f, 0.14f), new Color(0.52f, 0.36f, 0.22f, 0.85f), 4);
            CreatePanel(booth, "StageFloorLine", panelSprite, new Vector3(0f, -1.32f, 0f), new Vector2(6.8f, 0.06f), new Color(0.55f, 0.72f, 0.48f, 0.45f), 2);

            GameObject bossObj = CreateAnimatedFittedSpriteNode(booth, "BonusBossActor", wolfSprite, bonusBossCtrl, new Vector3(0f, 1.88f, 0f), 1.05f, 1.12f, 16, out Animator bossAnim);
            var bossSr = bossObj.GetComponent<SpriteRenderer>();
            if (bossSr != null) bossSr.color = new Color(1f, 0.72f, 0.88f, 1f);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.BonusBossFruitThrow;
            demo.actorTransform = bossObj.transform;
            demo.targetAnimator = bossAnim;
            demo.fruitPrefab = fruitPrefab;
            demo.realFruitSprites = new[] { strawberrySprite, cherrySprite, peachSprite };
            demo.dioramaScaleRatio = 0.78f;
            demo.dioramaFloorLocalY = -1.12f;
            demo.primaryStateName = "Run";
            demo.actionInterval = 1.15f;
        }

        // =============================================================================================
        // ช่อง 9 : สลับทิศทาง ด่านคี่ (โรยตัวลง ↓) ↔ ด่านคู่ (บินลอยขึ้น ↑)
        // =============================================================================================
        {
            Transform booth = CreateBoothCard(wolfGroup.transform, "09_StageParity_Descend_Vs_Ascend", new Vector3(botXs[3], botRowY, 0f), botCardW, cardH,
                "9. สลับทิศด่านคี่ (โรยตัวลง ↓) ↔ ด่านคู่ (บินขึ้น ↑)",
                "Code: GameManager.IsOddStage → WolfSpawner.isDescending\n• ด่านคี่ (Stage 1, 3, 5): เกิดด้านบน → เป่าลูกโป่ง → โรยตัวลงมา ↓ (Descending)\n• ด่านคู่ (Stage 2, 4): เกิดด้านล่าง → เป่าลูกโป่ง → บินลอยขึ้น ↑ (Ascending)",
                panelSprite, new Color(0.16f, 0.44f, 0.46f, 0.94f));

            GameObject formRoot = new GameObject("ParityCycleStage");
            formRoot.transform.SetParent(booth, false);
            formRoot.transform.localPosition = new Vector3(0f, 0.22f, 0f);

            float[] lanes = { -1.52f, -0.76f, 0f, 0.76f, 1.52f };
            for (int i = 0; i < lanes.Length; i++)
            {
                CreatePanel(formRoot.transform, $"LaneGuide_{i}", panelSprite, new Vector3(lanes[i], 0.55f, 0f), new Vector2(0.03f, 2.15f), new Color(1f, 1f, 1f, 0.12f), 2);
            }
            CreatePanel(formRoot.transform, "TopBranchLine", panelSprite, new Vector3(0f, 1.48f, 0f), new Vector2(6.5f, 0.06f), new Color(0.65f, 0.85f, 0.55f, 0.45f), 3);
            CreatePanel(formRoot.transform, "GroundLine", panelSprite, new Vector3(0f, -0.52f, 0f), new Vector2(6.5f, 0.06f), new Color(0.85f, 0.68f, 0.45f, 0.45f), 3);

            TextMesh statusTm = CreateLabel(formRoot.transform, "▼ ด่านคี่ (Stage 1, 3, 5) : โรยตัวลงมา ↓ (Descending)", new Vector3(0f, 2.12f, 0f), 11, new Color(1f, 0.62f, 0.45f), TextAnchor.MiddleCenter);

            var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.WolfOddEvenCycleStage;
            demo.formationRoot = formRoot.transform;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = summerWolfCtrl;
            demo.statusLabel = statusTm;
            demo.actionInterval = 3.6f;
        }
    }

    private static void CreateWolfFormationBooth(
        Transform parent,
        string objName,
        Vector3 pos,
        float cardW,
        float cardH,
        string title,
        string desc,
        ShowcaseDemoType demoType,
        Sprite wolfSprite,
        Sprite balloonSprite,
        RuntimeAnimatorController ctrl,
        Sprite boulderSprite,
        Sprite panelSprite,
        Color accentColor,
        float interval,
        bool showLaneGuides)
    {
        Transform booth = CreateBoothCard(parent, objName, pos, cardW, cardH, title, desc, panelSprite, accentColor);

        GameObject formRoot = new GameObject("FormationMiniStage");
        formRoot.transform.SetParent(booth, false);
        formRoot.transform.localPosition = new Vector3(0f, 0.30f, 0f);

        if (showLaneGuides)
        {
            float[] lanes = { -1.52f, -0.76f, 0f, 0.76f, 1.52f };
            for (int i = 0; i < lanes.Length; i++)
            {
                CreatePanel(formRoot.transform, $"LaneGuide_{i}", panelSprite, new Vector3(lanes[i], 0.62f, 0f), new Vector2(0.03f, 2.25f), new Color(1f, 1f, 1f, 0.12f), 2);
            }
            float barW = cardW - 0.55f;
            CreatePanel(formRoot.transform, "TopBranchLine", panelSprite, new Vector3(0f, 1.48f, 0f), new Vector2(barW, 0.06f), new Color(0.65f, 0.85f, 0.55f, 0.40f), 3);
            CreatePanel(formRoot.transform, "GroundLine", panelSprite, new Vector3(0f, -0.45f, 0f), new Vector2(barW, 0.06f), new Color(0.85f, 0.68f, 0.45f, 0.40f), 3);
        }

        var demo = booth.gameObject.AddComponent<ShowcaseStationDemo>();
        demo.demoType = demoType;
        demo.formationRoot = formRoot.transform;
        demo.wolfDemoSprite = wolfSprite;
        demo.balloonDemoSprite = balloonSprite;
        demo.wolfDemoController = ctrl;
        demo.boulderDemoSprite = boulderSprite;
        demo.actionInterval = interval;
    }

    // =================================================================================================
    // SCENE 03 : 7-BLOCK VFX & SHADER SHOWCASE (5 SEASONS + WOLF/BALLOON + MAMA/PIGLET/WEAPONS)
    // =================================================================================================
    private class SeasonMaterialSample
    {
        public string objectName;
        public string materialName;
        public string shaderName;
        public Sprite sprite;
        public Material material;
        public bool isOverlayEffect;
    }

    private class SeasonVfxAudit
    {
        public string seasonKey;
        public string title;
        public Color accentColor;
        public string scenePath;
        public Sprite bgSprite;
        public Sprite groundFallbackSprite;
        public GameObject clonedEnvironmentRoot;
        public readonly List<(GameObject clone, string rawName, Vector3 origPos, Quaternion origRot, Vector3 origScale)> inlineParticles = new();
        public readonly List<string> particleNames = new();
        public readonly List<SeasonMaterialSample> customMaterials = new();
        public bool useProceduralRain;
    }

    private static void PopulateScene03_AllUsedVfxAndShaders(Scene scene, Sprite panelSprite, Sprite heatTestSprite)
    {
        const float boardW = 34.0f;
        const float headerY = 9.15f;
        const float topBannerY = 7.78f;
        const float seasonRowY = 3.48f;
        const float seasonW = 6.32f;
        const float seasonH = 7.55f;
        const float seasonSpacing = 6.60f;

        const float botBannerY = -0.88f;
        const float charRowY = -5.38f;
        const float charBlockW = 16.18f;
        const float charBlockH = 7.95f;
        const float charBlockOffsetX = 8.26f;

        SetupStageRootAndCamera(
            scene,
            "POOYAN REMAKE — SCENE 03: VFX, PARTICLES & SHADERS (7-BLOCK ARCHITECTURE)",
            "แบ่งเป็น 7 ก้อนใหญ่ชัดเจน: 5 ฤดูกาล (ภาพฉาก + Particle + Material บน Sprite จริง) | ก้อนที่ 6 หมาป่า & ลูกโป่ง | ก้อนที่ 7 แม่หมู, ลูกหมู & อาวุธ (คุมขอบเขต 100%)",
            overviewPos: new Vector3(0f, -0.45f, -10f), overviewSize: 10.75f,
            topPos: new Vector3(0f, 3.65f, -10f), topSize: 5.15f,
            bottomPos: new Vector3(0f, -5.20f, -10f), bottomSize: 4.95f,
            panelSprite: panelSprite,
            boardWidth: boardW,
            headerY: headerY);

        // =============================================================================================
        // ROW 1 : 5 BIG SEASONAL BLOCKS (AUTUMN, SPRING, WINTER, RAINY, SUMMER)
        // =============================================================================================
        var seasonsGroup = CreateGroup(scene, "Blocks_1_To_5_Five_Seasons_VFX_And_Materials");
        CreateSectionBanner(seasonsGroup.transform, new Vector3(0f, topBannerY, 0f),
            "BLOCKS 1 – 5 : 5 ฤดูกาล — ภาพบรรยากาศจริง + Particles ประจำฉาก + Material/Shader ที่แปะบน Sprite/พื้นจริง (ตัดอนุภาคไม่ให้เลยกรอบ 100%)",
            new Color(0.24f, 0.42f, 0.62f, 0.94f), panelSprite, boardW - 0.8f);

        var seasonDefs = new (string key, string title, string scenePath, string folder, string fallbackBgGuid, Color accent, bool isRainy)[]
        {
            ("Autumn", "1. AUTUMN (ด่าน 1)", "Assets/Scenes/1_Autumn/Scene/Autumn.unity", "Assets/Scenes/1_Autumn", "ba376a38c4b51e640a38ea54e95ff34e", new Color(0.60f, 0.32f, 0.14f, 0.94f), false),
            ("Spring", "2. SPRING (ด่าน 2)", "Assets/Scenes/2_Spring/Scene/Spring.unity", "Assets/Scenes/2_Spring", "45cdf6f4dd9e8394383071b5419f523a", new Color(0.24f, 0.50f, 0.34f, 0.94f), false),
            ("Winter", "3. WINTER (ด่าน 3)", "Assets/Scenes/3_Winter/Scene/Winter.unity", "Assets/Scenes/3_Winter", "", new Color(0.24f, 0.42f, 0.64f, 0.94f), false),
            ("Rainy",  "4. RAINY (ด่าน 4)",  "Assets/Scenes/4_Rainy/Scene/Rainy.unity",   "Assets/Scenes/4_Rainy",  "", new Color(0.20f, 0.38f, 0.52f, 0.94f), true),
            ("Summer", "5. SUMMER (ด่าน 5)", "Assets/Scenes/5_Summer/Scene/Summer.unity", "Assets/Scenes/5_Summer", "", new Color(0.62f, 0.44f, 0.12f, 0.94f), false),
        };

        for (int i = 0; i < seasonDefs.Length; i++)
        {
            var def = seasonDefs[i];
            float x = (i - 2) * seasonSpacing;

            SeasonVfxAudit audit = InspectSeasonSceneForShowcase(
                scene, def.key, def.title, def.scenePath, def.folder, def.fallbackBgGuid, def.accent, def.isRainy, heatTestSprite);

            BuildSeasonShowcaseBlock(seasonsGroup.transform, audit, new Vector3(x, seasonRowY, 0f), seasonW, seasonH, panelSprite);
        }

        // =============================================================================================
        // ROW 2 : BLOCK 6 (WOLF & BALLOON) + BLOCK 7 (MAMA PIG, PIGLETS & WEAPONS)
        // =============================================================================================
        var charGroup = CreateGroup(scene, "Blocks_6_And_7_Character_And_Combat_VFX");
        CreateSectionBanner(charGroup.transform, new Vector3(0f, botBannerY, 0f),
            "BLOCKS 6 – 7 : ก้อนที่ 6 หมาป่า & ลูกโป่ง (ซ้าย)  |  ก้อนที่ 7 แม่หมู, ลูกหมู, เชือกรอก & อาวุธ (ขวา) — ตัด WolfBite ที่เลิกใช้ออกแล้ว",
            new Color(0.46f, 0.26f, 0.52f, 0.94f), panelSprite, boardW - 0.8f);

        // โหลด Real Assets ที่ใช้จริงทั้งหมด
        GameObject fxBalloonPop = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/BalloonPopFX.prefab");
        GameObject fxBalloonHit = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/BalloonHitFX.prefab");
        GameObject fxRockBreak = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/RockBreak.prefab");
        GameObject fxRockHitBasket = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/RockHitBasket.prefab");
        GameObject fxGroundDust = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/GroundDustImpactFX.prefab");
        GameObject fxRopePulley = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/RopePulleyFX.prefab");
        GameObject fxArrowWind = LoadAsset<GameObject>("Assets/Scenes/All/Resources/FX/ArrowWind_Autumn.prefab");

        GameObject balloonPrefab = LoadAsset<GameObject>("Assets/Scenes/1_Autumn/Prefab/Stage/Balloon.prefab");
        GameObject rockPrefab = LoadAsset<GameObject>("Assets/Scenes/1_Autumn/Prefab/Wolf/Rock.prefab");
        GameObject fruitPrefab = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/Fruit.prefab")
            ?? LoadAssetByGuid<GameObject>("6a5e74d9f5021bc46b63f2dbb7bf7d11");

        GameObject arrowAutumn = LoadAsset<GameObject>("Assets/Scenes/1_Autumn/Prefab/Stage/ArrowAutumn.prefab");
        GameObject arrowSpring = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/ArrowSpring.prefab");
        GameObject arrowWinter = LoadAsset<GameObject>("Assets/Scenes/3_Winter/Prefab/Stage/ArrowWinter.prefab");
        GameObject arrowRainy = LoadAsset<GameObject>("Assets/Scenes/4_Rainy/Prefab/Stage/ArrowRainy.prefab");
        GameObject arrowSummer = LoadAsset<GameObject>("Assets/Scenes/5_Summer/Prefab/Stage/ArrowSummer.prefab");

        GameObject meatAutumn = LoadAsset<GameObject>("Assets/Scenes/1_Autumn/Prefab/Stage/MeatAutumn.prefab");
        GameObject meatSpring = LoadAsset<GameObject>("Assets/Scenes/2_Spring/Prefab/Stage/MeatSpring.prefab");
        GameObject meatWinter = LoadAsset<GameObject>("Assets/Scenes/3_Winter/Prefab/Stage/MeatWinter.prefab");
        GameObject meatRainy = LoadAsset<GameObject>("Assets/Scenes/4_Rainy/Prefab/Stage/RainyMeat.prefab");
        GameObject meatSummer = LoadAsset<GameObject>("Assets/Scenes/5_Summer/Prefab/Stage/SummerMeat.prefab");

        Material matCartoonBalloon = LoadAsset<Material>("Assets/Scenes/All/Resources/Material/CartoonBalloon.mat");
        Material matRope = LoadAsset<Material>("Assets/Scenes/All/Resources/Material/RopeMaterial.mat");
        Material matWindWisp = LoadAsset<Material>("Assets/Scenes/All/Resources/Material/CartoonWindWisp.mat");
        Material matSparkleHdr = LoadAsset<Material>("Assets/Scenes/All/Resources/Material/CartoonSparkleHDR.mat");
        Material matShieldParry = LoadAsset<Material>("Assets/Scenes/All/Resources/Material/ShieldParryRing.mat");

        Sprite wolfSprite = LoadSpriteByGuid("049a76ae25d06a54f909a66c10911824");
        Sprite balloonSprite = LoadSpriteByGuid("e360a17f218bf274db937298618caba8");
        Sprite topArmorSprite = LoadSpriteByGuid("9ea8ca02761667642838ffb5879f9a73");
        Sprite bottomArmorSprite = LoadSpriteByGuid("eb9e5750e746391408d72e0b182edc53");
        Sprite mamaSprite = LoadSpriteByGuid("1f2fd1b67c285ad469ae135c03e71a35");
        Sprite strawberrySprite = LoadSpriteByGuid("44604557a9dc5c546993994f552a4440");
        Sprite cherrySprite = LoadSpriteByGuid("3ea5ab6d7f89c224c835e3a36cef0e58");
        Sprite peachSprite = LoadSpriteByGuid("689cd0e6303411240af3932e83deaaaf");
        Sprite pigletSprite = LoadSpriteByGuid("c1e27b5c57c2e4e40a195e37088e930f");
        RuntimeAnimatorController springMamaCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Mama/SpringMamaPig.controller");
        RuntimeAnimatorController springPigletCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Piglet/SpringPiglet1_0.controller");
        RuntimeAnimatorController springWolfCtrl = LoadAsset<RuntimeAnimatorController>("Assets/Scenes/2_Spring/Animation/Wolf/SpringGPX.controller");

        // =============================================================================================
        // BLOCK 6 (LEFT) : WOLF & BALLOON VFX — 6 AUTHENTIC REAL-GAME SCENARIOS (3x2 GRID)
        // =============================================================================================
        Transform block6 = CreateWideBayCard(charGroup.transform, "Block_06_Wolf_And_Balloon_VFX",
            new Vector3(-charBlockOffsetX, charRowY, 0f), charBlockW, charBlockH,
            "6. WOLF & BALLOON VFX — สถานการณ์และอนิเมชันจริงจากเกม (ลูกโป่งแตกหายไปจริง, ยิงหินแตก, ชนเกราะ, หมาป่าร่วง, บอสปาผลไม้)",
            panelSprite, new Color(0.56f, 0.24f, 0.30f, 0.95f));

        const float subW = 5.08f;
        const float subH = 3.20f;
        const float subColStep = 5.26f;
        const float subRowStep = 3.42f;
        const float subTopY = 1.38f;
        const float subBotY = subTopY - subRowStep;
        Vector2 subClipHalf = new Vector2(2.24f, 0.84f);

        // 6.1 BalloonPopFX + CartoonBalloon (ลูกโป่งโดนธนูยิง -> แตกหายไปทันที + หมาป่าร่วง!)
        {
            Transform sub = CreateVfxSubBooth(block6, "6_1_BalloonPop", new Vector3(-subColStep, subTopY, 0f), subW, subH,
                "6.1 BalloonPopFX + CartoonBalloon",
                "Balloon.cs | ยิงธนูโดนลูกโป่ง -> แตกหายไปจริง + BalloonPopFX + หมาป่าร่วง",
                panelSprite, new Color(0.46f, 0.20f, 0.32f, 0.92f), out Transform mount);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxWolfBalloonPop;
            demo.multiHitBalloon = false;
            demo.particleMount = mount;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.balloonShaderMat = matCartoonBalloon;
            demo.projectilePrefab = arrowSpring;
            demo.fxBalloonPopPrefab = fxBalloonPop;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.actionInterval = 2.45f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 6.2 BalloonHitFX + Multi-HP (ลูกโป่งอึดด่านคู่ 3 HP -> ยิงตอดเปลี่ยนสี/หดตัว -> นัดที่ 3 แตกหายไป!)
        {
            Transform sub = CreateVfxSubBooth(block6, "6_2_BalloonHit", new Vector3(0f, subTopY, 0f), subW, subH,
                "6.2 BalloonHitFX (ด่านคู่ 3-HP)",
                "Balloon.cs | ยิงตอด 2 ที (ฟ้า->เหลือง->แดง + HitFX) -> ทีที่ 3 แตกหาย!",
                panelSprite, new Color(0.20f, 0.36f, 0.52f, 0.92f), out Transform mount);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxWolfBalloonPop;
            demo.multiHitBalloon = true;
            demo.particleMount = mount;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.balloonShaderMat = matCartoonBalloon;
            demo.projectilePrefab = arrowSpring;
            demo.fxBalloonHitPrefab = fxBalloonHit;
            demo.fxBalloonPopPrefab = fxBalloonPop;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.actionInterval = 3.20f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 6.3 RockBreak (หมาป่าปาหิน -> ธนูยิงสอยแตกหายกลางอากาศ!)
        {
            Transform sub = CreateVfxSubBooth(block6, "6_3_RockBreak", new Vector3(subColStep, subTopY, 0f), subW, subH,
                "6.3 RockBreak (ยิงก้อนหินแตก)",
                "WolfProjectile.cs | หมาป่าปาหิน -> ธนูพุ่งสอยแตกหายกลางอากาศ",
                panelSprite, new Color(0.42f, 0.34f, 0.24f, 0.92f), out Transform mount);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxWolfRockBreak;
            demo.particleMount = mount;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.rockPrefab = rockPrefab;
            demo.projectilePrefab = arrowSpring;
            demo.fxRockBreakPrefab = fxRockBreak;
            demo.actionInterval = 2.15f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 6.4 RockHitBasket (หมาป่าปาหินใส่เกราะกระเช้า -> กระดอนออก + RockHitBasket + ShieldParryRing!)
        {
            Transform sub = CreateVfxSubBooth(block6, "6_4_RockHitBasket", new Vector3(-subColStep, subBotY, 0f), subW, subH,
                "6.4 RockHitBasket + ShieldParry",
                "WolfProjectile.cs | ปาหินใส่เกราะกระเช้า -> ประกายไฟ + กระดอนออก",
                panelSprite, new Color(0.48f, 0.30f, 0.16f, 0.92f), out Transform mount);

            CreateAuthenticMamaBasketRig(sub, "MamaShieldRig", new Vector3(1.25f, 0.14f, 0f), rigScale: 0.44f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out _, out _, out Transform topArmorTr, out _, out _);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxWolfRockHitBasket;
            demo.particleMount = mount;
            demo.wolfDemoSprite = wolfSprite;
            demo.balloonDemoSprite = balloonSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.topArmorTransform = topArmorTr;
            demo.rockPrefab = rockPrefab;
            demo.fxRockHitBasketPrefab = fxRockHitBasket;
            demo.actionInterval = 2.25f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 6.5 GroundDustImpactFX (หมาป่าเล่นท่า Fall ร่วงตกพื้น -> ฝุ่นตลบ + หายไป!)
        {
            Transform sub = CreateVfxSubBooth(block6, "6_5_GroundDustWolfFall", new Vector3(0f, subBotY, 0f), subW, subH,
                "6.5 GroundDustImpactFX (หมาป่าร่วงพื้น)",
                "Wolf.cs (Fall -> Ground) | หมาป่าร่วงกระแทกพื้น -> ฝุ่นตลบแล้วหายไป",
                panelSprite, new Color(0.36f, 0.32f, 0.24f, 0.92f), out Transform mount);

            CreatePanel(sub, "MiniFloor", panelSprite, new Vector3(0f, -0.58f, 0f), new Vector2(4.45f, 0.10f), new Color(0.38f, 0.34f, 0.26f, 0.9f), 8);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxWolfFallGroundDust;
            demo.particleMount = mount;
            demo.wolfDemoSprite = wolfSprite;
            demo.wolfDemoController = springWolfCtrl;
            demo.fxGroundDustPrefab = fxGroundDust;
            demo.actionInterval = 2.15f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 6.6 Bonus Fruit + CartoonSparkleHDR (แสดงเฉพาะผลไม้จริง Strawberry / Cherry / Peach พร้อม GlowAura & SparkleTrail)
        {
            Transform sub = CreateVfxSubBooth(block6, "6_6_BossFruitFX", new Vector3(subColStep, subBotY, 0f), subW, subH,
                "6.6 Bonus Fruit + CartoonSparkleHDR",
                "Fruit.prefab (ด่านโบนัส) | Strawberry / Cherry / Peach + GlowAura & Sparkle",
                panelSprite, new Color(0.50f, 0.24f, 0.44f, 0.92f), out Transform mount);

            TextMesh fruitBadge = CreateLabel(sub, "Strawberry", new Vector3(-2.10f, 0.88f, 0f), 9, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleLeft);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxBossFruitThrow;
            demo.particleMount = mount;
            demo.fruitPrefab = fruitPrefab;
            demo.realFruitSprites = new[] { strawberrySprite, cherrySprite, peachSprite };
            demo.seasonalLabels = new[] { "Strawberry", "Cherry", "Peach" };
            demo.statusLabel = fruitBadge;
            demo.actionInterval = 1.75f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // =============================================================================================
        // BLOCK 7 (RIGHT) : MAMA PIG, PIGLETS & WEAPONS VFX — 6 AUTHENTIC REAL-GAME SCENARIOS
        // =============================================================================================
        Transform block7 = CreateWideBayCard(charGroup.transform, "Block_07_Mama_Piglets_And_Weapons_VFX",
            new Vector3(charBlockOffsetX, charRowY, 0f), charBlockW, charBlockH,
            "7. MAMA PIG, PIGLETS & WEAPONS VFX — สถานการณ์จริง: ลูกหมูดึงเชือกรอก + แม่หมูยิงธนู/ปาเนื้อจริงครบ 5 ฤดู",
            panelSprite, new Color(0.22f, 0.48f, 0.36f, 0.95f));

        // 7.1 RopePulleyFX + RopeMaterial + Piglet Elevator (ลูกหมู 2 ตัวดึงเชือกขึ้น-ลงจริง + ฝุ่นรอก!)
        {
            Transform sub = CreateVfxSubBooth(block7, "7_1_RopePulley_And_RopeMat", new Vector3(-subColStep, subTopY, 0f), subW, subH,
                "7.1 RopePulleyFX + RopeMaterial",
                "ElevatorRope.cs | ลูกหมู 2 ตัวดึงเชือกรอกขึ้น-ลง + UV Scroll + ฝุ่นรอก",
                panelSprite, new Color(0.24f, 0.46f, 0.34f, 0.92f), out Transform mount);

            // พื้นผาด้านบนขวา (ไม่มีกล่องสี่เหลี่ยมตรงมุมหักเชือก ให้เชือกโค้งธรรมชาติเหมือนเกมจริง)
            CreatePanel(sub, "MiniCliff", panelSprite, new Vector3(0.95f, 0.34f, 0f), new Vector2(2.60f, 0.12f), new Color(0.30f, 0.46f, 0.28f, 0.92f), 9);

            CreateAnimatedFittedSpriteNode(sub, "Piglet1", pigletSprite, springPigletCtrl, new Vector3(0.45f, 0.68f, 0f), 0.72f, 0.72f, 16, out Animator p1Anim);
            CreateAnimatedFittedSpriteNode(sub, "Piglet2", pigletSprite, springPigletCtrl, new Vector3(1.22f, 0.68f, 0f), 0.72f, 0.72f, 16, out Animator p2Anim);

            // กระเช้าจริงของแม่หมูห้อยลงตรงใต้รอกพอดี (X = -0.78)
            GameObject miniGondola = CreateAuthenticMamaBasketRig(sub, "MiniGondola", new Vector3(-0.78f, -0.22f, 0f), rigScale: 0.36f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out _, out _, out _, out _, out _);

            GameObject handAnchor = new GameObject("RopeHandAnchor");
            handAnchor.transform.SetParent(sub, false);
            handAnchor.transform.localPosition = new Vector3(1.18f, 0.62f, 0f);

            GameObject ropeGo = new GameObject("LiveRopeLine");
            ropeGo.transform.SetParent(sub, false);
            var lr = ropeGo.AddComponent<LineRenderer>();
            // สำคัญมาก: UpdateRopeVisual() ส่งพิกัดเป็น World Space จึงต้องใช้ useWorldSpace = true เพื่อไม่ให้เชือกวาร์ปไปช่องอื่น!
            lr.useWorldSpace = true;
            lr.sortingOrder = 15;
            lr.textureMode = LineTextureMode.Tile;
            lr.startWidth = 0.14f;
            lr.endWidth = 0.14f;
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            if (matRope != null) lr.sharedMaterial = matRope;

            mount.localPosition = new Vector3(-0.78f, 0.62f, 0f);
            GameObject fxInst = InstantiateDisplayPrefab(fxRopePulley, scene, mount, Vector3.zero, maxFitSize: 0f, disableScripts: true);
            ParticleSystem dustPs = fxInst != null ? fxInst.GetComponentInChildren<ParticleSystem>(true) : null;
            if (fxInst != null) ConfigureParticleForShowcaseBooth(fxInst, false, out _, maxReachOverride: 0.50f);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.PigletElevatorStage;
            demo.particleMount = mount;
            demo.ropeLineRenderer = lr;
            demo.ropeGondolaTransform = miniGondola.transform;
            demo.ropeHandTransform = handAnchor.transform;
            demo.ropePulleyLocalY = 0.62f;
            demo.ropeCornerRadius = 0.16f;
            demo.ropeGondolaAnchorOffsetY = 0.26f;
            demo.elevatorMinY = -0.34f;
            demo.elevatorMaxY = 0.14f;
            demo.pulleyDustFX = dustPs;
            demo.targetAnimator = p1Anim;
            demo.secondaryAnimator = p2Anim;
            demo.primaryStateName = "Piglet_Idle";
            demo.secondaryStateName = "Piglet_PullRobe";
            demo.actionInterval = 3.8f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = new Vector2(1.40f, 0.72f);
        }

        // 7.2 ArrowWind_Autumn + CartoonWindWisp (แม่หมูในกระเช้าจริงยิงธนู -> หางลมพุ่งออกจากปลายธนู!)
        {
            Transform sub = CreateVfxSubBooth(block7, "7_2_ArrowWind_WindWisp", new Vector3(0f, subTopY, 0f), subW, subH,
                "7.2 ArrowWind + CartoonWindWisp",
                "ArrowShooter.cs | แม่หมูยิงธนู -> หางลม ArrowWind + CartoonWindWisp",
                panelSprite, new Color(0.50f, 0.34f, 0.18f, 0.92f), out Transform mount);

            CreateAuthenticMamaBasketRig(sub, "MamaBasketRig", new Vector3(1.38f, 0.14f, 0f), rigScale: 0.44f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mAnim, out _, out _, out _, out Transform firePt);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxMamaWeaponStream;
            demo.particleMount = mount;
            demo.targetAnimator = mAnim;
            demo.spawnPoint = firePt;
            demo.primaryStateName = "Idle";
            demo.fxArrowWindPrefab = fxArrowWind;
            demo.seasonalProjectiles = new[] { arrowAutumn };
            demo.isParabolicMeatThrow = false;
            demo.actionInterval = 1.65f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 7.3 Arrow Trails (Autumn / Spring / Winter) — แม่หมูในกระเช้าจริงยิงธนูตรงไปซ้าย สลับ 3 ฤดู!
        {
            Transform sub = CreateVfxSubBooth(block7, "7_3_Arrows_ASW", new Vector3(subColStep, subTopY, 0f), subW, subH,
                "7.3 Arrow Trails (Autumn/Spring/Winter)",
                "ArrowTrailEffect.cs | แม่หมูยิงธนูจริงตรงไปซ้ายโชว์หาง FX 3 ฤดูแรก",
                panelSprite, new Color(0.28f, 0.42f, 0.56f, 0.92f), out Transform mount);

            CreateAuthenticMamaBasketRig(sub, "MamaBasketRig", new Vector3(1.38f, 0.14f, 0f), rigScale: 0.44f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mAnim, out _, out _, out _, out Transform firePt);
            TextMesh lbl = CreateLabel(sub, "Autumn Arrow", new Vector3(-2.10f, 0.88f, 0f), 9, new Color(1f, 0.88f, 0.48f), TextAnchor.MiddleLeft);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxMamaWeaponStream;
            demo.particleMount = mount;
            demo.targetAnimator = mAnim;
            demo.spawnPoint = firePt;
            demo.primaryStateName = "Idle";
            demo.seasonalProjectiles = new[] { arrowAutumn, arrowSpring, arrowWinter };
            demo.seasonalLabels = new[] { "Autumn Arrow", "Spring Arrow", "Winter Arrow" };
            demo.statusLabel = lbl;
            demo.isParabolicMeatThrow = false;
            demo.actionInterval = 1.45f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 7.4 Arrow Trails (Rainy / Summer) — แม่หมูในกระเช้าจริงยิงธนูตรงไปซ้าย สลับ Rainy / Summer!
        {
            Transform sub = CreateVfxSubBooth(block7, "7_4_Arrows_RS", new Vector3(-subColStep, subBotY, 0f), subW, subH,
                "7.4 Arrow Trails (Rainy / Summer)",
                "ArrowTrailEffect.cs | แม่หมูยิงธนูจริงโชว์ละอองน้ำฝน / ประกายแดด",
                panelSprite, new Color(0.24f, 0.40f, 0.58f, 0.92f), out Transform mount);

            CreateAuthenticMamaBasketRig(sub, "MamaBasketRig", new Vector3(1.38f, 0.14f, 0f), rigScale: 0.44f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mAnim, out _, out _, out _, out Transform firePt);
            TextMesh lbl = CreateLabel(sub, "Rainy Arrow", new Vector3(-2.10f, 0.88f, 0f), 9, new Color(0.65f, 0.92f, 1f), TextAnchor.MiddleLeft);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxMamaWeaponStream;
            demo.particleMount = mount;
            demo.targetAnimator = mAnim;
            demo.spawnPoint = firePt;
            demo.primaryStateName = "Idle";
            demo.seasonalProjectiles = new[] { arrowRainy, arrowSummer };
            demo.seasonalLabels = new[] { "Rainy Arrow", "Summer Arrow" };
            demo.statusLabel = lbl;
            demo.isParabolicMeatThrow = false;
            demo.actionInterval = 1.45f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 7.5 Meat Aura (Autumn / Spring / Winter) — แม่หมูในกระเช้าจริงถือ & ปาเนื้อวิถีโค้ง 3 ฤดู!
        {
            Transform sub = CreateVfxSubBooth(block7, "7_5_Meat_ASW", new Vector3(0f, subBotY, 0f), subW, subH,
                "7.5 Meat Aura (Autumn/Spring/Winter)",
                "MeatAuraEffect.cs | แม่หมู HoldingMeat -> ปาเนื้อวิถีโค้งจริง 3 ฤดู",
                panelSprite, new Color(0.52f, 0.28f, 0.22f, 0.92f), out Transform mount);

            CreateAuthenticMamaBasketRig(sub, "MamaBasketRig", new Vector3(1.38f, 0.14f, 0f), rigScale: 0.44f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mAnim, out _, out _, out _, out Transform firePt);
            TextMesh lbl = CreateLabel(sub, "Autumn Meat", new Vector3(-2.10f, 0.88f, 0f), 9, new Color(1f, 0.82f, 0.55f), TextAnchor.MiddleLeft);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxMamaWeaponStream;
            demo.particleMount = mount;
            demo.targetAnimator = mAnim;
            demo.spawnPoint = firePt;
            demo.primaryStateName = "Idle";
            demo.seasonalProjectiles = new[] { meatAutumn, meatSpring, meatWinter };
            demo.seasonalLabels = new[] { "Autumn Meat", "Spring Meat", "Winter Meat" };
            demo.statusLabel = lbl;
            demo.isParabolicMeatThrow = true;
            demo.actionInterval = 1.95f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }

        // 7.6 Meat Aura (Rainy / Summer) — แม่หมูในกระเช้าจริงถือ & ปาเนื้อวิถีโค้ง Rainy / Summer!
        {
            Transform sub = CreateVfxSubBooth(block7, "7_6_Meat_RS", new Vector3(subColStep, subBotY, 0f), subW, subH,
                "7.6 Meat Aura (Rainy / Summer)",
                "MeatAuraEffect.cs | แม่หมู HoldingMeat -> ปาเนื้อด่านฝน & หน้าร้อน",
                panelSprite, new Color(0.52f, 0.34f, 0.18f, 0.92f), out Transform mount);

            CreateAuthenticMamaBasketRig(sub, "MamaBasketRig", new Vector3(1.38f, 0.14f, 0f), rigScale: 0.44f,
                mamaSprite, springMamaCtrl, topArmorSprite, bottomArmorSprite,
                out Animator mAnim, out _, out _, out _, out Transform firePt);
            TextMesh lbl = CreateLabel(sub, "Rainy Meat", new Vector3(-2.10f, 0.88f, 0f), 9, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleLeft);

            var demo = sub.gameObject.AddComponent<ShowcaseStationDemo>();
            demo.demoType = ShowcaseDemoType.VfxMamaWeaponStream;
            demo.particleMount = mount;
            demo.targetAnimator = mAnim;
            demo.spawnPoint = firePt;
            demo.primaryStateName = "Idle";
            demo.seasonalProjectiles = new[] { meatRainy, meatSummer };
            demo.seasonalLabels = new[] { "Rainy Meat", "Summer Meat" };
            demo.statusLabel = lbl;
            demo.isParabolicMeatThrow = true;
            demo.actionInterval = 1.95f;
            demo.clampParticlesToBounds = true;
            demo.particleClipHalfSize = subClipHalf;
        }
    }

    private static void SpawnMultiOrbitVfxGroup(Scene scene, Transform mount, GameObject[] prefabs, float maxFitSize)
    {
        if (prefabs == null || mount == null) return;
        foreach (var p in prefabs)
        {
            if (p == null) continue;
            GameObject inst = InstantiateDisplayPrefab(p, scene, mount, Vector3.zero, maxFitSize: maxFitSize, disableScripts: true);
            if (inst != null)
            {
                ConfigureParticleForShowcaseBooth(inst, isOrbitTrail: true, out _, maxReachOverride: 0.58f);
            }
        }
    }

    private static Transform CreateVfxSubBooth(
        Transform parent,
        string name,
        Vector3 localPos,
        float width,
        float height,
        string title,
        string desc,
        Sprite panelSprite,
        Color headerColor,
        out Transform fxMount)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        card.transform.localPosition = localPos;

        CreatePanel(card.transform, "SubBorder", panelSprite, Vector3.zero, new Vector2(width, height), new Color(0.24f, 0.30f, 0.40f, 0.95f), 2);
        CreatePanel(card.transform, "SubBg", panelSprite, Vector3.zero, new Vector2(width - 0.10f, height - 0.10f), new Color(0.08f, 0.10f, 0.15f, 0.98f), 3);
        CreatePanel(card.transform, "SubHeader", panelSprite, new Vector3(0f, height * 0.5f - 0.25f, 0f), new Vector2(width - 0.10f, 0.42f), headerColor, 4);
        CreateLabel(card.transform, title, new Vector3(0f, height * 0.5f - 0.25f, 0f), 11, Color.white, TextAnchor.MiddleCenter);

        // เวทีเข้มด้านในสำหรับแสดง Particle (กึ่งกลางที่ localY = +0.18, กว้าง width - 0.28, สูง 1.84)
        CreatePanel(card.transform, "SubViewport", panelSprite, new Vector3(0f, 0.18f, 0f), new Vector2(width - 0.26f, 1.86f), new Color(0.04f, 0.06f, 0.10f, 0.95f), 5);

        CreatePanel(card.transform, "SubDescBg", panelSprite, new Vector3(0f, -height * 0.5f + 0.44f, 0f), new Vector2(width - 0.24f, 0.72f), new Color(0.06f, 0.08f, 0.12f, 0.96f), 4);
        CreateLabel(card.transform, desc, new Vector3(0f, -height * 0.5f + 0.44f, 0f), 9, new Color(0.84f, 0.92f, 0.98f), TextAnchor.MiddleCenter);

        GameObject mountObj = new GameObject("FXMount");
        mountObj.transform.SetParent(card.transform, false);
        mountObj.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        fxMount = mountObj.transform;

        return card.transform;
    }

    private static SeasonVfxAudit InspectSeasonSceneForShowcase(
        Scene targetScene,
        string seasonKey,
        string title,
        string scenePath,
        string seasonFolder,
        string fallbackBgGuid,
        Color accentColor,
        bool isRainy,
        Sprite heatTestSprite)
    {
        var audit = new SeasonVfxAudit
        {
            seasonKey = seasonKey,
            title = title,
            accentColor = accentColor,
            scenePath = scenePath,
            useProceduralRain = isRainy
        };

        if (isRainy)
        {
            audit.particleNames.Add("RainWeatherFX (C# Procedural Rain + Splash)");
        }

        Scene prevActive = SceneManager.GetActiveScene();
        bool openedTemp = false;
        Scene inspected = default;

        if (File.Exists(scenePath))
        {
            Scene existing = SceneManager.GetSceneByPath(scenePath);
            if (existing.isLoaded)
            {
                inspected = existing;
            }
            else
            {
                inspected = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedTemp = true;
            }
        }

        try
        {
            if (inspected.IsValid() && inspected.isLoaded)
            {
                float bestBgArea = 0f;
                var seenMatNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // สร้าง Container สำหรับ Copy ฉากสภาพแวดล้อมจริงของฤดูนี้มาทั้งชุด (ท้องฟ้า, ผา, พื้นดิน, หญ้า, บ้าน, ต้นไม้, พร็อพ)
                GameObject envContainer = new GameObject($"RealSceneCopy_{seasonKey}");
                SceneManager.MoveGameObjectToScene(envContainer, targetScene);
                var clonedEnvRenderers = new List<(SpriteRenderer sr, int layerVal, int orderVal, float area, int seqIdx)>();

                foreach (GameObject rootGo in inspected.GetRootGameObjects())
                {
                    if (!rootGo.activeInHierarchy) continue;

                    // 1. ค้นหา SpriteRenderer ทั้งหมดเพื่อหา Background, Ground, และ Sprite ที่ใส่ Material/Shader พิเศษ
                    foreach (var sr in rootGo.GetComponentsInChildren<SpriteRenderer>(false))
                    {
                        if (sr == null || !sr.enabled || sr.sprite == null) continue;
                        string goName = sr.gameObject.name;
                        string lower = goName.ToLowerInvariant();

                        // ข้ามตัวละคร/กระสุน/ลูกโป่ง (ไปอยู่ในก้อน 6 และ 7)
                        if (IsCharacterOrProjectileObject(sr.transform)) continue;

                        float area = sr.bounds.size.x * sr.bounds.size.y;
                        bool looksBg = lower.Contains("bg") || lower.Contains("back") || lower.Contains("sky") || lower.Contains("stage") || sr.sortingOrder <= -1;
                        if ((looksBg && area > bestBgArea * 0.7f && area > 20f) || area > bestBgArea)
                        {
                            if (sr.bounds.size.x >= 7f)
                            {
                                bestBgArea = area;
                                audit.bgSprite = sr.sprite;
                            }
                        }

                        if (audit.groundFallbackSprite == null && (lower.Contains("ground") || lower.Contains("floor") || lower.Contains("grass") || lower.Contains("cliff") || lower.Contains("snow") || lower.Contains("land")))
                        {
                            audit.groundFallbackSprite = sr.sprite;
                        }

                        TryCollectSeasonCustomMaterial(sr.sharedMaterial, goName, sr.sprite, seenMatNames, audit.customMaterials);

                        // คัดลอกชิ้นส่วนฉากจริง (ท้องฟ้า, พื้น, ผา, ต้นไม้, บ้าน, หญ้า ฯลฯ) ที่อยู่ในกรอบกล้องจริง [-10..10] x [-6..6]
                        Vector3 wp = sr.transform.position;
                        if (Mathf.Abs(wp.x) <= 12.5f && Mathf.Abs(wp.y) <= 7.0f)
                        {
                            GameObject copyPiece = new GameObject($"Env_{goName}");
                            copyPiece.transform.SetParent(envContainer.transform, false);
                            copyPiece.transform.localPosition = new Vector3(wp.x, wp.y, 0f);
                            copyPiece.transform.localRotation = sr.transform.rotation;
                            copyPiece.transform.localScale = sr.transform.lossyScale;

                            var copySr = copyPiece.AddComponent<SpriteRenderer>();
                            copySr.sprite = sr.sprite;
                            copySr.color = sr.color;
                            copySr.flipX = sr.flipX;
                            copySr.flipY = sr.flipY;
                            copySr.drawMode = sr.drawMode;
                            copySr.size = sr.size;
                            copySr.sharedMaterial = sr.sharedMaterial;
                            copySr.sortingLayerName = "Default";
                            copySr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                            clonedEnvRenderers.Add((copySr, SortingLayer.GetLayerValueFromID(sr.sortingLayerID), sr.sortingOrder, area, clonedEnvRenderers.Count));
                        }
                    }

                    // 2. ค้นหา MeshRenderer (เช่น Quad ของ AutumnSunbeams / SummerHeatHaze)
                    foreach (var mr in rootGo.GetComponentsInChildren<MeshRenderer>(false))
                    {
                        if (mr == null || !mr.enabled) continue;
                        if (mr.GetComponent<TextMesh>() != null) continue;
                        if (IsCharacterOrProjectileObject(mr.transform)) continue;
                        TryCollectSeasonCustomMaterial(mr.sharedMaterial, mr.gameObject.name, null, seenMatNames, audit.customMaterials);
                    }

                    // 3. ค้นหา Inline ParticleSystem ประจำฤดู (ข้าม RainWeatherFX เพราะจะใช้ MiniRainBooth ที่ไม่บัค Texture!)
                    foreach (var ps in rootGo.GetComponentsInChildren<ParticleSystem>(false))
                    {
                        if (ps == null || !ps.gameObject.activeInHierarchy) continue;
                        if (ps.transform.parent != null && ps.transform.parent.GetComponentInParent<ParticleSystem>() != null) continue;
                        if (IsCharacterOrProjectileObject(ps.transform)) continue;
                        if (ps.GetComponentInParent<RainWeatherFX>() != null) continue;

                        string lowerPs = ps.gameObject.name.ToLowerInvariant();
                        if (lowerPs.Contains("raindrops") || lowerPs.Contains("rainsplash") || lowerPs.Contains("fallingrain")) continue;
                        if (lowerPs.Contains("windstreak") || lowerPs.Contains("heathazeoverlay") || lowerPs.Contains("wolfbite")) continue;
                        if (!HasVisibleParticleOutput(ps.gameObject)) continue;

                        GameObject clone = UnityEngine.Object.Instantiate(ps.gameObject);
                        clone.name = $"{seasonKey}_{ps.gameObject.name}";
                        SceneManager.MoveGameObjectToScene(clone, targetScene);
                        StripNonVisualComponents(clone);

                        audit.inlineParticles.Add((clone, ps.gameObject.name, ps.transform.position, ps.transform.rotation, ps.transform.lossyScale));
                        audit.particleNames.Add(ps.gameObject.name);
                    }
                }

                if (clonedEnvRenderers.Count > 0)
                {
                    // รักษาลำดับชั้นภาพจากหลังไปหน้าอย่างเสถียร:
                    // 1) SortingLayer + SortingOrder น้อยไปมาก
                    // 2) ถ้า SortingOrder เท่ากัน (เช่น BG กับ ต้นไม้เอาเส้นออก_0 ใน Spring ที่ต่างก็ -6) ให้ภาพพื้นที่ใหญ่กว่า (BG) อยู่หลังสุด และต้นไม้/พร็อพอยู่หน้าเสมอ!
                    clonedEnvRenderers.Sort((a, b) =>
                    {
                        int c = a.layerVal.CompareTo(b.layerVal);
                        if (c != 0) return c;
                        c = a.orderVal.CompareTo(b.orderVal);
                        if (c != 0) return c;
                        c = b.area.CompareTo(a.area); // พื้นที่ใหญ่กว่า (BG) ไปอยู่ด้านหลังก่อน!
                        if (c != 0) return c;
                        return a.seqIdx.CompareTo(b.seqIdx);
                    });

                    int total = Mathf.Max(1, clonedEnvRenderers.Count - 1);
                    for (int k = 0; k < clonedEnvRenderers.Count; k++)
                    {
                        int mappedOrder = 6 + Mathf.RoundToInt((k / (float)total) * 8f);
                        clonedEnvRenderers[k].sr.sortingOrder = Mathf.Clamp(mappedOrder, 6, 14);
                    }
                    audit.clonedEnvironmentRoot = envContainer;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(envContainer);
                }
            }
        }
        finally
        {
            if (openedTemp && inspected.IsValid() && inspected.isLoaded)
            {
                EditorSceneManager.CloseScene(inspected, true);
            }
            if (prevActive.IsValid() && prevActive.isLoaded)
            {
                SceneManager.SetActiveScene(prevActive);
            }
        }

        // Fallback Background หากฉากไม่ได้มี SpriteRenderer ขนาดใหญ่ที่ root
        if (audit.bgSprite == null && !string.IsNullOrEmpty(fallbackBgGuid))
        {
            audit.bgSprite = LoadSpriteByGuid(fallbackBgGuid);
        }
        if (audit.bgSprite == null)
        {
            audit.bgSprite = FindStageBackgroundSprite(seasonFolder) ?? heatTestSprite;
        }

        // ตรวจสอบ Material ประจำฤดูจากโฟลเดอร์ Resources/Material เพิ่มเติมหากในฉากสร้างผ่านโค้ด
        if (seasonKey.Equals("Summer", StringComparison.OrdinalIgnoreCase) && !audit.customMaterials.Any(m => m.materialName.IndexOf("HeatHaze", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            Material hazeMat = FindMaterialByName("SummerHeatHaze");
            if (hazeMat != null)
            {
                audit.customMaterials.Add(new SeasonMaterialSample
                {
                    objectName = "HeatHaze Overlay",
                    materialName = hazeMat.name,
                    shaderName = ShortenShaderName(hazeMat.shader != null ? hazeMat.shader.name : "Custom"),
                    sprite = null,
                    material = hazeMat,
                    isOverlayEffect = true
                });
            }
        }
        if (seasonKey.Equals("Autumn", StringComparison.OrdinalIgnoreCase) && !audit.customMaterials.Any(m => m.materialName.IndexOf("Sunbeam", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            Material sunMat = FindMaterialByName("AutumnSunbeams");
            if (sunMat != null)
            {
                audit.customMaterials.Add(new SeasonMaterialSample
                {
                    objectName = "Sunbeams Overlay",
                    materialName = sunMat.name,
                    shaderName = ShortenShaderName(sunMat.shader != null ? sunMat.shader.name : "Custom"),
                    sprite = null,
                    material = sunMat,
                    isOverlayEffect = true
                });
            }
        }

        return audit;
    }

    private static bool IsCharacterOrProjectileObject(Transform tr)
    {
        Transform cur = tr;
        while (cur != null)
        {
            if (cur.GetComponent<PlayerController>() != null || cur.GetComponent<Wolf>() != null || cur.GetComponent<Arrow>() != null || cur.GetComponent<MeatAuraEffect>() != null || cur.GetComponent<Balloon>() != null)
            {
                return true;
            }
            string n = cur.name.ToLowerInvariant();
            if (n.Contains("house") || n.Contains("home") || n.Contains("chimney") || n.Contains("smoke"))
            {
                return false;
            }
            if (n.Contains("player") || n.Contains("mama") || n.Contains("piglet") || n.Contains("wolf") || n.Contains("balloon") || n.Contains("arrow") || n.Contains("meat") || n.Contains("canvas") || n.Contains("ui"))
            {
                return true;
            }
            cur = cur.parent;
        }
        return false;
    }

    private static void TryCollectSeasonCustomMaterial(Material mat, string objName, Sprite sprite, HashSet<string> seenNames, List<SeasonMaterialSample> dest)
    {
        if (mat == null || mat.shader == null) return;
        string matPath = AssetDatabase.GetAssetPath(mat);
        if (!string.IsNullOrEmpty(matPath) && IsExplicitlyUnusedVfxAsset(matPath)) return;

        string shaderName = mat.shader.name ?? "";
        string matName = mat.name ?? "";
        if (shaderName.StartsWith("TextMeshPro/", StringComparison.OrdinalIgnoreCase)) return;
        if (shaderName.Equals("UI/Default", StringComparison.OrdinalIgnoreCase)) return;
        if (shaderName.StartsWith("Hidden/", StringComparison.OrdinalIgnoreCase)) return;
        if (matName.Equals("Sprites-Default", StringComparison.OrdinalIgnoreCase)) return;
        if (matName.Equals("RopeMaterial", StringComparison.OrdinalIgnoreCase)) return;
        if (matName.IndexOf("Balloon", StringComparison.OrdinalIgnoreCase) >= 0) return;

        bool isDefaultSpriteShader = shaderName.Equals("Sprites/Default", StringComparison.OrdinalIgnoreCase)
            || shaderName.Equals("Universal Render Pipeline/2D/Sprite-Lit-Default", StringComparison.OrdinalIgnoreCase)
            || shaderName.Equals("Universal Render Pipeline/2D/Sprite-Unlit-Default", StringComparison.OrdinalIgnoreCase);

        if (isDefaultSpriteShader) return;
        if (!seenNames.Add(matName)) return;

        string lower = (matName + " " + objName).ToLowerInvariant();
        bool isOverlay = lower.Contains("sunbeam") || lower.Contains("heathaze") || lower.Contains("haze") || lower.Contains("lightbeam") || lower.Contains("godray");

        dest.Add(new SeasonMaterialSample
        {
            objectName = objName,
            materialName = matName,
            shaderName = ShortenShaderName(shaderName),
            sprite = sprite,
            material = mat,
            isOverlayEffect = isOverlay
        });
    }

    private static Material FindMaterialByName(string exactOrPartial)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:Material {exactOrPartial}", new[] { "Assets/Scenes" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (IsExplicitlyUnusedVfxAsset(p)) continue;
            Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null) return m;
        }
        return null;
    }

    private static void BuildSeasonShowcaseBlock(Transform parent, SeasonVfxAudit audit, Vector3 pos, float width, float height, Sprite panelSprite)
    {
        GameObject block = new GameObject($"Block_Season_{audit.seasonKey}");
        block.transform.SetParent(parent, false);
        block.transform.localPosition = pos;

        CreatePanel(block.transform, "CardBorder", panelSprite, Vector3.zero, new Vector2(width, height), new Color(0.28f, 0.34f, 0.44f, 0.96f), -2);
        CreatePanel(block.transform, "CardBg", panelSprite, Vector3.zero, new Vector2(width - 0.14f, height - 0.14f), new Color(0.09f, 0.11f, 0.16f, 0.98f), -1);
        CreatePanel(block.transform, "HeaderBar", panelSprite, new Vector3(0f, height * 0.5f - 0.34f, 0f), new Vector2(width - 0.14f, 0.54f), audit.accentColor, 2);
        CreateLabel(block.transform, audit.title, new Vector3(0f, height * 0.5f - 0.34f, 0f), 13, Color.white, TextAnchor.MiddleCenter);

        // 1. UPPER VIEWPORT: ภาพฉากฤดูจริง + Particles ประจำฤดู + Overlay Shader (คุมขอบเขต 100%)
        const float vpY = 1.64f;
        const float vpW = 5.92f;
        const float vpH = 2.94f;
        CreatePanel(block.transform, "SeasonViewportBorder", panelSprite, new Vector3(0f, vpY, 0f), new Vector2(vpW + 0.08f, vpH + 0.08f), new Color(0.32f, 0.38f, 0.48f, 0.95f), 2);
        CreatePanel(block.transform, "SeasonViewportBg", panelSprite, new Vector3(0f, vpY, 0f), new Vector2(vpW, vpH), new Color(0.05f, 0.07f, 0.11f, 0.98f), 3);

        if (audit.clonedEnvironmentRoot != null)
        {
            // วาง SpriteMask ครอบขอบ Viewport เป๊ะ เพื่อตัดฉากจริงที่ Copy มาไม่ให้เลยกรอบแม้แต่พิกเซลเดียว!
            GameObject maskObj = new GameObject("SeasonViewportMask");
            maskObj.transform.SetParent(block.transform, false);
            maskObj.transform.localPosition = new Vector3(0f, vpY, 0f);
            Vector2 pSz = panelSprite != null ? (Vector2)panelSprite.bounds.size : Vector2.one;
            maskObj.transform.localScale = new Vector3((vpW - 0.10f) / Mathf.Max(0.01f, pSz.x), (vpH - 0.10f) / Mathf.Max(0.01f, pSz.y), 1f);
            var sm = maskObj.AddComponent<SpriteMask>();
            sm.sprite = panelSprite;
            sm.isCustomRangeActive = true;
            sm.frontSortingOrder = 15;
            sm.backSortingOrder = 5;

            // ย่อส่วนฉากจริงทั้งฉาก (17.78 x 10.0) ให้พอดีในกรอบ (5.82 x 2.84)
            audit.clonedEnvironmentRoot.transform.SetParent(block.transform, false);
            audit.clonedEnvironmentRoot.transform.localPosition = new Vector3(0f, vpY, 0f);
            audit.clonedEnvironmentRoot.transform.localRotation = Quaternion.identity;
            audit.clonedEnvironmentRoot.transform.localScale = Vector3.one * 0.288f;
        }
        else if (audit.bgSprite != null)
        {
            CreateFittedSpriteNode(block.transform, "SeasonRealBackground", audit.bgSprite, new Vector3(0f, vpY, 0f), vpW - 0.08f, vpH - 0.08f, 6);
        }

        // แสดง Overlay Shader (เช่น AutumnSunbeams / SummerHeatHaze) ทับบนภาพฉากจริงใน Viewport
        foreach (var matSample in audit.customMaterials)
        {
            if (matSample.isOverlayEffect && matSample.material != null)
            {
                CreateShaderPreviewQuad(block.transform, new Vector3(0f, vpY, 0f), new Vector3(vpW - 0.08f, vpH - 0.08f, 1f), matSample.material, sortingOrder: 13);
            }
        }

        GameObject fxMount = new GameObject("SeasonFXMount");
        fxMount.transform.SetParent(block.transform, false);
        fxMount.transform.localPosition = new Vector3(0f, vpY, 0f);

        var demo = block.AddComponent<ShowcaseStationDemo>();
        demo.particleMount = fxMount.transform;
        demo.clampParticlesToBounds = true;
        demo.particleClipHalfSize = new Vector2(vpW * 0.5f - 0.14f, vpH * 0.5f - 0.14f);

        if (audit.useProceduralRain)
        {
            demo.demoType = ShowcaseDemoType.MiniRainBooth;
            demo.isContinuousLoopingParticle = true;
            CreatePanel(block.transform, "RainGroundLine", panelSprite, new Vector3(0f, vpY - vpH * 0.5f + 0.15f, 0f), new Vector2(vpW - 0.24f, 0.06f), new Color(0.42f, 0.66f, 0.86f, 0.65f), 14);
        }
        else if (audit.inlineParticles.Count > 0)
        {
            demo.demoType = ShowcaseDemoType.ParticleReplay;
            demo.isContinuousLoopingParticle = true;
            const float stageMiniScale = 0.288f; // ย่อส่วน Particle ด้วยสเกลเดียวกับฉากจริงเป๊ะ เพื่อความสมมาตรเหมือนย่อฉากจริงทั้งด่าน!

            foreach (var item in audit.inlineParticles)
            {
                GameObject clone = item.clone;
                clone.transform.SetParent(fxMount.transform, false);

                // คงตำแหน่งสัมพัทธ์จากฉากจริง (คูณ stageMiniScale) และหนีบให้อยู่ในกรอบ Viewport พอดี
                float lx = Mathf.Clamp(item.origPos.x * stageMiniScale, -(vpW * 0.5f - 0.25f), vpW * 0.5f - 0.25f);
                float ly = Mathf.Clamp(item.origPos.y * stageMiniScale, -(vpH * 0.5f - 0.20f), vpH * 0.5f - 0.12f);
                clone.transform.localPosition = new Vector3(lx, ly, 0f);
                clone.transform.localRotation = item.origRot;
                clone.transform.localScale = item.origScale * stageMiniScale;

                ConfigureSeasonMiniatureParticle(clone);
            }
        }
        else
        {
            demo.demoType = ShowcaseDemoType.StaticPose;
        }

        // รวมกลุ่ม Particle ที่ซ้ำกัน (เช่น TubeAirPuffFX_1..5 -> TubeAirPuffFX (x5)) เพื่อแสดงได้ครบทุกชนิดไม่ตกหล่น!
        var groupedParticles = GroupSeasonParticles(audit);

        // 2. COMPACT PARTICLES STRIP: ช่องเล็กกะทัดรัดโชว์ Particle ทุกชนิดที่มีในฤดูนี้แยกชัดๆ (เช่น ผีเสื้อ, ป่องควัน, ใบไม้ร่วง, หิมะ, ฝน)
        const float stripW = 5.92f;
        const float psStripY = -0.64f;
        const float psStripH = 1.48f;
        CreatePanel(block.transform, "ParticlesStripBg", panelSprite, new Vector3(0f, psStripY, 0f), new Vector2(stripW, psStripH), new Color(0.06f, 0.09f, 0.14f, 0.96f), 2);
        CreateLabel(block.transform, $"PARTICLES ในฉากนี้ ({groupedParticles.Count} ชนิด):", new Vector3(0f, psStripY + psStripH * 0.5f - 0.16f, 0f), 9, new Color(0.62f, 0.95f, 0.78f), TextAnchor.MiddleCenter);

        if (groupedParticles.Count > 0)
        {
            int showPs = Mathf.Min(4, groupedParticles.Count);
            float cellW = (stripW - 0.24f) / showPs;
            for (int pIdx = 0; pIdx < showPs; pIdx++)
            {
                var grp = groupedParticles[pIdx];
                float px = -((showPs - 1) * cellW) * 0.5f + pIdx * cellW;
                Vector3 cellCenter = new Vector3(px, psStripY - 0.13f, 0f);

                GameObject cellObj = new GameObject($"ParticleMiniCell_{pIdx}_{SanitizeName(grp.baseName)}");
                cellObj.transform.SetParent(block.transform, false);
                cellObj.transform.localPosition = cellCenter;

                CreatePanel(cellObj.transform, "CellBg", panelSprite, Vector3.zero, new Vector2(cellW - 0.10f, 1.08f), new Color(0.09f, 0.12f, 0.18f, 0.96f), 3);

                GameObject miniMount = new GameObject("MiniPsMount");
                miniMount.transform.SetParent(cellObj.transform, false);
                miniMount.transform.localPosition = new Vector3(0f, 0.12f, 0f);

                var cellDemo = cellObj.AddComponent<ShowcaseStationDemo>();
                cellDemo.particleMount = miniMount.transform;
                cellDemo.clampParticlesToBounds = true;
                cellDemo.particleClipHalfSize = new Vector2(Mathf.Max(0.42f, (cellW - 0.24f) * 0.5f), 0.34f);

                if (grp.isProceduralRain)
                {
                    cellDemo.demoType = ShowcaseDemoType.MiniRainBooth;
                    cellDemo.isContinuousLoopingParticle = true;
                }
                else if (grp.sampleTemplate != null)
                {
                    GameObject miniClone = UnityEngine.Object.Instantiate(grp.sampleTemplate);
                    miniClone.name = $"MiniPreview_{grp.baseName}";
                    SceneManager.MoveGameObjectToScene(miniClone, block.scene);
                    miniClone.transform.SetParent(miniMount.transform, false);
                    ConfigureCompactParticleStripPreview(miniClone, grp.baseName, cellW - 0.30f);

                    cellDemo.demoType = ShowcaseDemoType.ParticleReplay;
                    cellDemo.isContinuousLoopingParticle = true;
                }

                string countSuffix = grp.count > 1 ? $" (×{grp.count})" : "";
                string psLabel = TrimShort(grp.baseName, showPs >= 3 ? 13 : 18) + countSuffix;
                CreateLabel(cellObj.transform, psLabel, new Vector3(0f, -0.38f, 0f), 8, new Color(0.86f, 0.98f, 0.92f), TextAnchor.MiddleCenter);
            }
        }
        else
        {
            CreateLabel(block.transform,
                "ฤดูนี้ใช้ Shader บรรยากาศ + Combat FX (ไม่มี Weather Particle แยก)",
                new Vector3(0f, psStripY - 0.12f, 0f), 9, new Color(0.74f, 0.82f, 0.92f), TextAnchor.MiddleCenter);
        }

        // 3. COMPACT MATERIAL STRIP: แสดง Sprite ที่ใส่ Material/Shader ในด่านนี้ (เช่น พื้น / ฉาก / Overlay)
        const float stripY = -2.20f;
        const float stripH = 1.48f;
        CreatePanel(block.transform, "MaterialStripBg", panelSprite, new Vector3(0f, stripY, 0f), new Vector2(stripW, stripH), new Color(0.06f, 0.08f, 0.13f, 0.96f), 2);
        CreateLabel(block.transform, $"MATERIAL / SHADER บน SPRITE ({audit.customMaterials.Count}):", new Vector3(0f, stripY + stripH * 0.5f - 0.16f, 0f), 9, new Color(1f, 0.88f, 0.45f), TextAnchor.MiddleCenter);

        if (audit.customMaterials.Count > 0)
        {
            int showCount = Mathf.Min(3, audit.customMaterials.Count);
            float cellW = (stripW - 0.24f) / showCount;
            for (int m = 0; m < showCount; m++)
            {
                var sample = audit.customMaterials[m];
                float mx = -((showCount - 1) * cellW) * 0.5f + m * cellW;
                Vector3 samplePos = new Vector3(mx, stripY + 0.04f, 0f);

                CreatePanel(block.transform, $"MatCell_{m}", panelSprite, new Vector3(mx, stripY - 0.13f, 0f), new Vector2(cellW - 0.10f, 1.08f), new Color(0.10f, 0.13f, 0.19f, 0.95f), 3);

                if (sample.sprite != null)
                {
                    GameObject spNode = CreateFittedSpriteNode(block.transform, $"RealSpriteMat_{m}", sample.sprite, samplePos, cellW - 0.34f, 0.56f, 12);
                    if (spNode.TryGetComponent<SpriteRenderer>(out var spSr) && sample.material != null)
                    {
                        spSr.sharedMaterial = sample.material;
                    }
                }
                else
                {
                    if (audit.bgSprite != null)
                    {
                        CreateFittedSpriteNode(block.transform, $"MatBgRef_{m}", audit.bgSprite, samplePos, cellW - 0.34f, 0.56f, 11);
                    }
                    CreateShaderPreviewQuad(block.transform, samplePos, new Vector3(cellW - 0.34f, 0.56f, 1f), sample.material, sortingOrder: 13);
                }

                string lbl = $"{TrimShort(sample.objectName, 14)}\n{TrimShort(sample.materialName, 16)}";
                CreateLabel(block.transform, lbl, new Vector3(mx, stripY - 0.48f, 0f), 8, new Color(0.76f, 0.94f, 1f), TextAnchor.MiddleCenter);
            }
        }
        else
        {
            Sprite previewSpr = audit.groundFallbackSprite != null ? audit.groundFallbackSprite : audit.bgSprite;
            if (previewSpr != null)
            {
                CreateFittedSpriteNode(block.transform, "DefaultTerrainPreview", previewSpr, new Vector3(-1.55f, stripY - 0.12f, 0f), 2.05f, 0.82f, 10);
            }
            CreateLabel(block.transform,
                "พื้น/ฉากใช้ URP Sprite Default\n(ไม่มี Custom Terrain Shader)",
                new Vector3(0.95f, stripY - 0.12f, 0f), 9, new Color(0.74f, 0.82f, 0.92f), TextAnchor.MiddleCenter);
        }

        // 4. BOTTOM COMPACT SPEC BAR: สรุปรายชื่อครบทุก Particle & Material ในฤดูนี้ (ไม่ตัดชื่อออก!)
        const float descY = -3.35f;
        CreatePanel(block.transform, "SpecBox", panelSprite, new Vector3(0f, descY, 0f), new Vector2(stripW, 0.72f), new Color(0.05f, 0.07f, 0.11f, 0.96f), 2);

        string psSummary = groupedParticles.Count > 0
            ? string.Join(", ", groupedParticles.Select(g => g.count > 1 ? $"{g.baseName}×{g.count}" : g.baseName))
            : "None";
        string matSummary = audit.customMaterials.Count > 0
            ? string.Join(", ", audit.customMaterials.Select(m => m.materialName))
            : "Sprite Default";

        string fullSpec = $"FX: {TrimShort(psSummary, 58)}\nMat: {TrimShort(matSummary, 58)}";
        CreateLabel(block.transform, fullSpec, new Vector3(-stripW * 0.5f + 0.14f, descY, 0f), 8, new Color(0.86f, 0.92f, 0.98f), TextAnchor.MiddleLeft);
    }

    private class SeasonParticleGroup
    {
        public string baseName;
        public int count;
        public GameObject sampleTemplate;
        public bool isProceduralRain;
    }

    private static List<SeasonParticleGroup> GroupSeasonParticles(SeasonVfxAudit audit)
    {
        var list = new List<SeasonParticleGroup>();
        var map = new Dictionary<string, SeasonParticleGroup>(StringComparer.OrdinalIgnoreCase);

        if (audit.useProceduralRain)
        {
            var rainGrp = new SeasonParticleGroup
            {
                baseName = "RainWeatherFX",
                count = 1,
                isProceduralRain = true
            };
            list.Add(rainGrp);
            map[rainGrp.baseName] = rainGrp;
        }

        foreach (var item in audit.inlineParticles)
        {
            string baseName = NormalizeParticleGroupName(item.rawName);
            if (!map.TryGetValue(baseName, out var grp))
            {
                grp = new SeasonParticleGroup
                {
                    baseName = baseName,
                    count = 0,
                    sampleTemplate = item.clone
                };
                map[baseName] = grp;
                list.Add(grp);
            }
            grp.count++;
        }
        return list;
    }

    private static string NormalizeParticleGroupName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "ParticleFX";
        string s = raw.Trim();
        // ลบเลขท้ายแบบ _1, _2, (1), (2) เพื่อให้ TubeAirPuffFX_1..5 รวมเป็นชนิดเดียว (TubeAirPuffFX ×5)
        int parenIdx = s.LastIndexOf('(');
        if (parenIdx > 0 && s.EndsWith(")"))
        {
            string inside = s.Substring(parenIdx + 1, s.Length - parenIdx - 2).Trim();
            if (int.TryParse(inside, out _)) s = s.Substring(0, parenIdx).Trim();
        }
        int underIdx = s.LastIndexOf('_');
        if (underIdx > 0 && underIdx < s.Length - 1)
        {
            string tail = s.Substring(underIdx + 1);
            if (int.TryParse(tail, out _)) s = s.Substring(0, underIdx);
        }
        return s;
    }

    private static void ConfigureCompactParticleStripPreview(GameObject clone, string baseName, float maxBoxW)
    {
        if (clone == null) return;
        string lower = (baseName ?? "").ToLowerInvariant();
        bool isDownwardWeather = lower.Contains("leaf") || lower.Contains("leaves") || lower.Contains("snow") || lower.Contains("petal") || lower.Contains("rain");
        bool isUpwardPuff = lower.Contains("puff") || lower.Contains("smoke") || lower.Contains("steam") || lower.Contains("chimney") || lower.Contains("tube");

        clone.transform.localPosition = isDownwardWeather
            ? new Vector3(0f, 0.24f, 0f)
            : (isUpwardPuff ? new Vector3(0f, -0.22f, 0f) : Vector3.zero);
        clone.transform.localRotation = Quaternion.identity;
        clone.transform.localScale = Vector3.one * 0.24f;

        foreach (var ps in clone.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null) continue;
            ps.gameObject.SetActive(true);
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.None;

            var em = ps.emission;
            em.enabled = true;
            // หากอัตราเกิดต่ำมาก (เช่น ผีเสื้อ 0.42/วิ หรือ ใบไม้ 1/วิ) เพิ่มนิดหน่อยในช่องพรีวิวเล็กเพื่อให้เห็นชัดทันทีไม่ต้องรอนาน!
            if (GetCurveMax(em.rateOverTime, em.rateOverTimeMultiplier) > 0f && GetCurveMax(em.rateOverTime, em.rateOverTimeMultiplier) < 2.2f)
            {
                em.rateOverTimeMultiplier = 2.6f;
            }

            var shape = ps.shape;
            if (shape.enabled)
            {
                Vector3 sc = shape.scale;
                float scaledMaxW = Mathf.Max(0.8f, maxBoxW / 0.24f);
                shape.scale = new Vector3(Mathf.Min(sc.x, scaledMaxW), Mathf.Min(sc.y, 1.8f), 0.1f);
                if (shape.radius > 1.2f) shape.radius = 1.2f;
            }

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sortingOrder < 18) psr.sortingOrder = 18;
        }
    }

    private static string TrimShort(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLen) return s ?? "";
        return s.Substring(0, maxLen - 1) + "…";
    }

    private static GameObject CreateShaderPreviewQuad(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, int sortingOrder)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ShaderPreviewQuad";
        DestroyCollider(quad);
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPos;
        quad.transform.localScale = localScale;

        var mr = quad.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = sortingOrder;
            if (mat != null) mr.sharedMaterial = mat;
        }
        return quad;
    }

    private static Sprite FindStageBackgroundSprite(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return null;
        Sprite best = null;
        float bestArea = 0f;
        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s == null) continue;
            float area = s.bounds.size.x * s.bounds.size.y;
            if (s.bounds.size.x >= 8f && area > bestArea)
            {
                best = s;
                bestArea = area;
            }
        }
        return best;
    }

    private static bool IsExplicitlyUnusedVfxAsset(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;
        string lower = path.Replace('\\', '/').ToLowerInvariant();
        if (!lower.StartsWith("assets/")) return true;
        if (lower.Contains("/portfolio_showcase/")) return true;
        if (lower.Contains("autumnwindstreaks")) return true;
        if (lower.Contains("summerheathazeoverlay")) return true;
        // เอฟเฟกต์หมาป่ากัดที่ผู้ใช้เอาออกจากเกมจริงแล้ว (ดึง Sprite ออกแล้ว ไม่แสดงในเกมจริง)
        if (lower.Contains("wolfbitearrow")) return true;
        if (lower.Contains("wolfbiteladder")) return true;
        if (lower.Contains("textmesh pro") || lower.Contains("/tmp/") || lower.Contains("liberationsans")) return true;
        if (lower.Contains("samplescene")) return true;
        return false;
    }

    /// <summary>
    /// ตรวจว่า Prefab/GameObject มี ParticleSystem หรือ TrailRenderer ที่ปล่อยภาพออกมาให้เห็นจริง (ไม่เอาตัวที่ถูกถอด Sprite/Material หรือปิด Emission ไว้)
    /// </summary>
    private static bool HasVisibleParticleOutput(GameObject go)
    {
        if (go == null) return false;

        foreach (var tr in go.GetComponentsInChildren<TrailRenderer>(true))
        {
            if (tr != null && tr.enabled && tr.sharedMaterial != null) return true;
        }

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null) continue;
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr == null || !psr.enabled || psr.sharedMaterial == null) continue;

            var em = ps.emission;
            bool hasRate = GetCurveMax(em.rateOverTime, em.rateOverTimeMultiplier) > 0.001f
                || GetCurveMax(em.rateOverDistance, em.rateOverDistanceMultiplier) > 0.001f;
            bool hasBurst = em.burstCount > 0;
            if (!em.enabled && !hasRate && !hasBurst) continue;
            if (!hasRate && !hasBurst) continue;

            // ตรวจว่า TextureSheetAnimation (โหมด Sprites) ไม่ได้ถูกดึง Sprite ออกจนว่างหมด
            var tsa = ps.textureSheetAnimation;
            if (tsa.enabled && tsa.mode == ParticleSystemAnimationMode.Sprites)
            {
                bool anySprite = false;
                for (int s = 0; s < tsa.spriteCount; s++)
                {
                    if (tsa.GetSprite(s) != null) { anySprite = true; break; }
                }
                if (!anySprite) continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// ตั้งค่า Particle ของฉากฤดูให้ใช้ ScalingMode.Hierarchy เพื่อย่อขนาดอนุภาค/ความเร็ว/แรงลม/ขอบเขตปล่อย
    /// ตามสเกลย่อส่วนฉากจริง (0.296x) อย่างสมมาตร 1:1 (ไม่ขยายใหญ่เกินสเกลฉาก)
    /// </summary>
    private static void ConfigureSeasonMiniatureParticle(GameObject clone)
    {
        if (clone == null) return;
        foreach (var ps in clone.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps == null) continue;
            ps.gameObject.SetActive(true);
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.stopAction = ParticleSystemStopAction.None;

            var em = ps.emission;
            em.enabled = true;

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sortingOrder < 18)
            {
                psr.sortingOrder = 18;
            }
        }
    }

    private static void ConfigureParticleForShowcaseBooth(
        GameObject inst,
        bool isOrbitTrail,
        out bool hasLoopingSystem,
        float maxReachOverride = 1.08f,
        float customWeatherBoxW = 2.25f,
        float customWeatherStartY = 1.05f)
    {
        hasLoopingSystem = false;
        var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null || systems.Length == 0) return;

        bool isAreaWeather = false;
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            var shape = ps.shape;
            if (shape.enabled && (shape.scale.x > 2.8f || shape.scale.y > 2.2f || shape.radius > 1.5f))
            {
                isAreaWeather = true;
            }
        }

        if (isAreaWeather && !isOrbitTrail)
        {
            // ปรับตำแหน่ง Weather FX (หิมะ / ใบไม้ร่วง) ให้อยู่ด้านบนของกรอบและตกลงมาหายก่อนขอบล่าง (ไม่ออกนอกกรอบ)
            inst.transform.localScale = Vector3.one;
            inst.transform.localPosition = new Vector3(0f, customWeatherStartY, 0f);
        }

        foreach (var tr in inst.GetComponentsInChildren<TrailRenderer>(true))
        {
            if (tr == null) continue;
            tr.emitting = true;
            if (tr.time > 0.14f) tr.time = 0.14f;
            if (tr.sortingOrder < 17) tr.sortingOrder = 17;
        }

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            ps.gameObject.SetActive(true);
            var main = ps.main;
            main.playOnAwake = true;
            // สำคัญมาก: ป้องกัน Prefab ที่มี StopAction=Destroy/Disable ลบตัวเองหายไปหลังเล่นรอบแรก!
            main.stopAction = ParticleSystemStopAction.None;

            var em = ps.emission;
            em.enabled = true;

            if (!isOrbitTrail)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            if (isAreaWeather)
            {
                main.loop = true;
                main.prewarm = true;
                hasLoopingSystem = true;

                var shape = ps.shape;
                if (shape.enabled)
                {
                    Vector3 sc = shape.scale;
                    shape.scale = new Vector3(Mathf.Min(sc.x, customWeatherBoxW), Mathf.Min(sc.y, 0.16f), 0.05f);
                    if (shape.radius > 1.0f) shape.radius = 1.0f;
                }

                // จำกัดแรงลมแนวนอน/Noise ไม่ให้ลอยออกด้านข้างกรอบ
                var vel = ps.velocityOverLifetime;
                if (vel.enabled)
                {
                    if (Mathf.Abs(vel.xMultiplier) > 0.55f) vel.xMultiplier = Mathf.Sign(vel.xMultiplier) * 0.55f;
                    if (vel.yMultiplier < -2.4f) vel.yMultiplier = -2.4f;
                }
                var noise = ps.noise;
                if (noise.enabled && noise.strengthMultiplier > 0.35f)
                {
                    noise.strengthMultiplier = 0.35f;
                }

                float fallHeight = Mathf.Max(1.6f, customWeatherStartY * 1.85f);
                float fallRate = Mathf.Max(0.95f, GetCurveMax(main.startSpeed, main.startSpeedMultiplier) + Mathf.Abs(main.gravityModifierMultiplier) * 2.5f);
                float maxSafeLife = Mathf.Clamp(fallHeight / fallRate, 0.55f, 1.85f);
                if (main.startLifetimeMultiplier > maxSafeLife)
                {
                    main.startLifetimeMultiplier = maxSafeLife;
                }
                if (main.startSizeMultiplier < 0.22f)
                {
                    main.startSizeMultiplier = Mathf.Clamp(main.startSizeMultiplier * 1.65f, 0.22f, 0.42f);
                }
            }
            else if (isOrbitTrail)
            {
                main.loop = true;
                hasLoopingSystem = true;
                if (main.startLifetimeMultiplier > 0.36f) main.startLifetimeMultiplier = 0.36f;
                if (main.startSpeedMultiplier > 0.95f) main.startSpeedMultiplier = 0.95f;
            }
            else
            {
                if (main.loop) hasLoopingSystem = true;

                float sizeEst = GetCurveMax(main.startSize, main.startSizeMultiplier);
                if (sizeEst > 0.0001f && sizeEst < 0.18f)
                {
                    float boost = Mathf.Clamp(0.22f / sizeEst, 1.25f, 1.85f);
                    main.startSizeMultiplier *= boost;
                }

                var shape = ps.shape;
                if (shape.enabled)
                {
                    if (shape.radius > 0.35f) shape.radius = 0.35f;
                    Vector3 sc = shape.scale;
                    if (sc.x > 1.10f || sc.y > 0.65f)
                    {
                        shape.scale = new Vector3(Mathf.Min(sc.x, 1.10f), Mathf.Min(sc.y, 0.65f), Mathf.Min(sc.z, 0.1f));
                    }
                }

                // จำกัดระยะกระเด็นรวม (ทั้งความเร็วต้น + แรงโน้มถ่วง) ไม่ให้ทะลุออกนอกกรอบบูธเด็ดขาด!
                float lifeEst = Mathf.Max(0.15f, GetCurveMax(main.startLifetime, main.startLifetimeMultiplier));
                if (lifeEst > 0.85f)
                {
                    main.startLifetimeMultiplier *= (0.85f / lifeEst);
                    lifeEst = 0.85f;
                }

                float speedEst = GetCurveMax(main.startSpeed, main.startSpeedMultiplier);
                float gravEst = Mathf.Abs(GetCurveMax(main.gravityModifier, main.gravityModifierMultiplier)) * 9.81f;
                float estReach = speedEst * lifeEst + 0.5f * gravEst * lifeEst * lifeEst;
                if (estReach > maxReachOverride)
                {
                    float factor = Mathf.Clamp((maxReachOverride * 0.92f) / estReach, 0.20f, 1f);
                    main.startSpeedMultiplier *= factor;
                    main.gravityModifierMultiplier *= factor;
                }

                var vel = ps.velocityOverLifetime;
                if (vel.enabled)
                {
                    if (Mathf.Abs(vel.xMultiplier) > 1.1f) vel.xMultiplier = Mathf.Sign(vel.xMultiplier) * 1.1f;
                    if (Mathf.Abs(vel.yMultiplier) > 1.1f) vel.yMultiplier = Mathf.Sign(vel.yMultiplier) * 1.1f;
                }
            }

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sortingOrder < 18)
            {
                psr.sortingOrder = 18;
            }
        }
    }

    private static float GetCurveMax(ParticleSystem.MinMaxCurve curve, float multiplier)
    {
        return curve.mode switch
        {
            ParticleSystemCurveMode.Constant => curve.constant,
            ParticleSystemCurveMode.TwoConstants => Mathf.Max(curve.constantMin, curve.constantMax),
            _ => multiplier
        };
    }

    private static string ShortenShaderName(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName)) return "Custom";
        int idx = shaderName.LastIndexOf('/');
        return idx >= 0 && idx < shaderName.Length - 1 ? shaderName.Substring(idx + 1) : shaderName;
    }

    // =================================================================================================
    // SCENE 04 : INTERACTIVE AUDIO SOUNDBOARD & PROCEDURAL DSP STUDIO
    // =================================================================================================
    private static void PopulateScene04_AudioSoundboard(Scene scene, Sprite panelSprite)
    {
        var stage = SetupStageRootAndCamera(
            scene,
            "POOYAN REMAKE — SCENE 04: AUDIO ARCHITECTURE & PROCEDURAL DSP SOUNDBOARD",
            "กด Play เพื่อเปิดหน้า Soundboard Studio: กดฟัง BGM/SFX จริงทั้งหมด, ทดสอบ Footstep แยกพื้นผิว, Stereo Panning & เสียงสังเคราะห์ 44.1kHz PCM",
            overviewPos: new Vector3(0f, 0f, -10f), overviewSize: 8.8f,
            topPos: new Vector3(0f, 0f, -10f), topSize: 8.8f,
            bottomPos: new Vector3(0f, 0f, -10f), bottomSize: 8.8f,
            panelSprite: panelSprite,
            boardWidth: 28.0f,
            headerY: 7.55f);

        stage.allowCameraNavigation = false;
        stage.showCameraBar = false;

        // Instantiate AudioManager.prefab ของจริง เพื่อให้ ShowcaseAudioBoard ดึงคลิปทั้งหมดไปสร้างปุ่มฟังสด
        GameObject audioMgrPrefab = LoadAsset<GameObject>("Assets/Scenes/All/Common/Prefab/AudioManager.prefab");
        AudioManager audioMgrComp = null;
        if (audioMgrPrefab != null)
        {
            GameObject audioMgrInst = PrefabUtility.InstantiatePrefab(audioMgrPrefab, scene) as GameObject;
            if (audioMgrInst != null)
            {
                audioMgrInst.transform.position = Vector3.zero;
                audioMgrComp = audioMgrInst.GetComponent<AudioManager>();
            }
        }

        GameObject boardObj = CreateGroup(scene, "InteractiveSoundboardController");
        var board = boardObj.AddComponent<ShowcaseAudioBoard>();
        board.audioManagerRef = audioMgrComp;
        board.mainMenuBgmOverride = LoadAssetByGuid<AudioClip>("ee7b66ef98d515748bd621f04c67ca45");
        board.stageHeaderRoot = stage.gameObject;

        // การ์ดพรีวิว 6 กลุ่มใน Scene View (จะถูกซ่อนอัตโนมัติใน Play Mode เพื่อให้หน้า Soundboard UI 6 การ์ดคมชัดไม่ซ้อนทับกัน!)
        var infoGroup = CreateGroup(scene, "AudioStudio_6Group_EditPreview");
        board.editModeBackdropRoot = infoGroup;

        const float cw = 8.4f;
        const float ch = 4.6f;
        const float rowTop = 2.6f;
        const float rowBot = -2.5f;
        const float colStep = 8.9f;

        CreateBoothCard(infoGroup.transform, "Group_01_BGM", new Vector3(-colStep, rowTop, 0f), cw, ch,
            "1. BGM — เพลงประกอบด่าน & โบนัส",
            "• MainMenu / Autumn / Spring / Winter / Rainy / Summer\n• Bonus Meat / Bonus Fruit / Stage Clear / Game Over\n• แยก AudioSource BGM + Preload Non-Blocking",
            panelSprite, new Color(0.20f, 0.42f, 0.72f, 0.94f));

        CreateBoothCard(infoGroup.transform, "Group_02_Cutscene", new Vector3(0f, rowTop, 0f), cw, ch,
            "2. CUTSCENE — เพลง & เสียงคัทซีน",
            "• Stage 1 Kidnap / Stage 2 & 4 Chase / Reunion\n• MainMenu Attract + Balloon Release & Dock SFX\n• แยก Cutscene AudioSource พร้อม Fade-Out นุ่มนวล",
            panelSprite, new Color(0.48f, 0.26f, 0.68f, 0.94f));

        CreateBoothCard(infoGroup.transform, "Group_03_Wolves", new Vector3(colStep, rowTop, 0f), cw, ch,
            "3. เสียงของพวกหมาป่า & ลูกโป่ง",
            "• Balloon Inflate / Pop / Damage + Wolf Fall\n• Shield Block / Ladder Climb / Rock / Boulder / Boss Fruit\n• รองรับ Stereo Panning ซ้าย-ขวาตามพิกัดแกน X จริง",
            panelSprite, new Color(0.66f, 0.24f, 0.24f, 0.94f));

        CreateBoothCard(infoGroup.transform, "Group_04_Pigs", new Vector3(-colStep, rowBot, 0f), cw, ch,
            "4. เสียงของพวกหมู (แม่หมู & ลูกหมู)",
            "• Arrow Fire / Meat Pickup / Throw / Hit / Bounce\n• Rope Pull (1-Shot & Continuous Loop) / Mama Fall / 1-UP\n• แยก RopeLoop AudioSource อิสระจาก SFX อื่น",
            panelSprite, new Color(0.16f, 0.52f, 0.36f, 0.94f));

        CreateBoothCard(infoGroup.transform, "Group_05_Particles_Surface", new Vector3(0f, rowBot, 0f), cw, ch,
            "5. เสียง PARTICLES, สภาพอากาศ & พื้นผิว",
            "• Rainy Ambience Loop + Particle Burst / Impact SFX\n• Multi-Voice Surface Footsteps: Grass / Rainy Mud / Snow\n• แยก Pitch ตามตัวละคร (Wolf / Mama / Piglet 1 / Piglet 2)",
            panelSprite, new Color(0.12f, 0.48f, 0.58f, 0.94f));

        CreateBoothCard(infoGroup.transform, "Group_06_UI_And_DSP", new Vector3(colStep, rowBot, 0f), cw, ch,
            "6. เสียง UI & PROCEDURAL C# DSP",
            "• UI Button Click / 1-UP Fanfare / Stage Clear\n• RetroSFXSynth.cs สังเคราะห์เสียง 44.1kHz PCM จากโค้ด C# ล้วน\n• กด Play เพื่อเปิดหน้า Soundboard Studio เต็มจอ!",
            panelSprite, new Color(0.64f, 0.42f, 0.14f, 0.94f));
    }

    // =================================================================================================
    // SHARED LAYOUT & HELPER BUILDERS
    // =================================================================================================
    private static PortfolioShowcaseStage SetupStageRootAndCamera(
        Scene scene,
        string title,
        string subtitle,
        Vector3 overviewPos,
        float overviewSize,
        Vector3 topPos,
        float topSize,
        Vector3 bottomPos,
        float bottomSize,
        Sprite panelSprite,
        float boardWidth,
        float headerY)
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = overviewSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        camObj.transform.position = overviewPos;
        camObj.AddComponent<AudioListener>();

        GameObject stageObj = new GameObject("PortfolioShowcaseStage");
        var stage = stageObj.AddComponent<PortfolioShowcaseStage>();
        stage.showcaseTitle = title;
        stage.showcaseSubtitle = subtitle;
        stage.overviewPos = overviewPos;
        stage.overviewOrthoSize = overviewSize;
        stage.topRowPos = topPos;
        stage.topRowOrthoSize = topSize;
        stage.bottomRowPos = bottomPos;
        stage.bottomRowOrthoSize = bottomSize;

        CreatePanel(stageObj.transform, "HeaderBackdrop", panelSprite, new Vector3(0f, headerY, 0f), new Vector2(boardWidth, 1.85f), new Color(0.11f, 0.15f, 0.22f, 0.98f), -5);
        CreateLabel(stageObj.transform, title, new Vector3(0f, headerY + 0.30f, 0f), 24, new Color(1f, 0.90f, 0.48f), TextAnchor.MiddleCenter);
        CreateLabel(stageObj.transform, subtitle, new Vector3(0f, headerY - 0.35f, 0f), 14, new Color(0.84f, 0.90f, 0.98f), TextAnchor.MiddleCenter);

        return stage;
    }

    private static GameObject CreateGroup(Scene scene, string name)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static void CreateSectionBanner(Transform parent, Vector3 pos, string text, Color bgColor, Sprite panelSprite, float width)
    {
        CreatePanel(parent, "SectionBannerBg", panelSprite, pos, new Vector2(width, 0.72f), bgColor, -4);
        CreateLabel(parent, text, pos, 15, Color.white, TextAnchor.MiddleCenter);
    }

    private static Transform CreateWideBayCard(Transform parent, string objName, Vector3 pos, float width, float height, string headerText, Sprite panelSprite, Color accentColor)
    {
        GameObject root = new GameObject(objName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;

        CreatePanel(root.transform, "BayBorder", panelSprite, Vector3.zero, new Vector2(width + 0.14f, height + 0.14f), accentColor, -3);
        CreatePanel(root.transform, "BayBg", panelSprite, Vector3.zero, new Vector2(width, height), new Color(0.12f, 0.14f, 0.19f, 0.96f), -2);

        float headerY = height * 0.5f - 0.38f;
        CreatePanel(root.transform, "HeaderStrip", panelSprite, new Vector3(0f, headerY, 0f), new Vector2(width, 0.72f), accentColor, -1);
        CreateLabel(root.transform, headerText, new Vector3(0f, headerY, 0f), 14, Color.white, TextAnchor.MiddleCenter);

        return root.transform;
    }

    private static Transform CreateBoothCard(
        Transform parent,
        string objectName,
        Vector3 position,
        float width,
        float height,
        string headerText,
        string bodyText,
        Sprite panelSprite,
        Color accentColor)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;

        CreatePanel(root.transform, "CardBorder", panelSprite, Vector3.zero, new Vector2(width + 0.12f, height + 0.12f), accentColor, -3);
        CreatePanel(root.transform, "CardBg", panelSprite, Vector3.zero, new Vector2(width, height), new Color(0.12f, 0.14f, 0.19f, 0.96f), -2);

        float headerY = height * 0.5f - 0.38f;
        CreatePanel(root.transform, "HeaderStrip", panelSprite, new Vector3(0f, headerY, 0f), new Vector2(width, 0.68f), accentColor, -1);
        CreateLabel(root.transform, headerText, new Vector3(0f, headerY, 0f), width >= 7.0f ? 16 : 13, Color.white, TextAnchor.MiddleCenter);

        float descBoxHeight = height > 7.5f ? height - 1.2f : 2.05f;
        float descY = height > 7.5f ? -0.28f : -height * 0.5f + descBoxHeight * 0.5f + 0.10f;
        CreatePanel(root.transform, "CodeDescBg", panelSprite, new Vector3(0f, descY, 0f), new Vector2(width - 0.18f, descBoxHeight), new Color(0.07f, 0.09f, 0.13f, 0.95f), -1);
        CreateLabel(root.transform, bodyText, new Vector3(0f, descY, 0f), height > 7.5f ? 12 : (width >= 7.0f ? 12 : 11), new Color(0.86f, 0.92f, 0.98f), TextAnchor.MiddleCenter);

        return root.transform;
    }

    /// <summary>
    /// สร้างชุดแม่หมู + เกราะกระเช้าบน + เกราะกระเช้าล่าง โดยอ้างอิงพิกัดและสเกลจริงจาก Spring.unity & SpringPlayer.prefab 100%
    /// ทำให้ตำแหน่งกระเช้ากับตัวแม่หมูตรงกันเป๊ะเหมือนในฉากเกมจริงทุกประการ
    /// </summary>
    private static GameObject CreateAuthenticMamaBasketRig(
        Transform parent,
        string name,
        Vector3 localPos,
        float rigScale,
        Sprite mamaSprite,
        RuntimeAnimatorController ctrl,
        Sprite topArmorSprite,
        Sprite bottomArmorSprite,
        out Animator animator,
        out Transform mamaPigBody,
        out Transform topArmorTransform,
        out Transform bottomArmorTransform,
        out Transform firePointTransform)
    {
        GameObject rig = new GameObject(name);
        rig.transform.SetParent(parent, false);
        rig.transform.localPosition = localPos;
        rig.transform.localScale = new Vector3(rigScale, rigScale, 1f);

        // TopArmor: ตำแหน่งและสเกลจริงจาก Spring.unity (parent -0.04378, 0.32500 scale 0.75358 + child 0.125, 0.296 scale 0.15280)
        GameObject topArmor = new GameObject("TopArmor");
        topArmor.transform.SetParent(rig.transform, false);
        topArmor.transform.localPosition = new Vector3(0.0504f, 0.5481f, 0f);
        topArmor.transform.localScale = new Vector3(0.115149f, 0.115149f, 1f);
        var topSr = topArmor.AddComponent<SpriteRenderer>();
        topSr.sprite = topArmorSprite;
        topSr.sortingOrder = 16;
        topArmorTransform = topArmor.transform;

        // BottomArmor: ตำแหน่งและสเกลจริงจาก Spring.unity (parent 0.03911, -0.64712 scale 0.75358 + child -0.0007, 0.27885 scale 0.11365)
        GameObject botArmor = new GameObject("BottomArmor");
        botArmor.transform.SetParent(rig.transform, false);
        botArmor.transform.localPosition = new Vector3(0.0386f, -0.4370f, 0f);
        botArmor.transform.localScale = new Vector3(0.085647f, 0.085647f, 1f);
        var botSr = botArmor.AddComponent<SpriteRenderer>();
        botSr.sprite = bottomArmorSprite;
        botSr.sortingOrder = 16;
        bottomArmorTransform = botArmor.transform;

        // SpringMamaPig: ตำแหน่งและสเกลจริงจาก SpringPlayer.prefab (-0.20, -0.03 scale 0.1945755)
        GameObject mama = new GameObject("SpringMamaPig");
        mama.transform.SetParent(rig.transform, false);
        mama.transform.localPosition = new Vector3(-0.20f, -0.03f, 0f);
        mama.transform.localScale = new Vector3(0.1945755f, 0.1945755f, 1f);
        var mamaSr = mama.AddComponent<SpriteRenderer>();
        mamaSr.sprite = mamaSprite;
        mamaSr.sortingOrder = 14;
        animator = mama.AddComponent<Animator>();
        if (ctrl != null) animator.runtimeAnimatorController = ctrl;
        mamaPigBody = mama.transform;

        // FirePoint: ตำแหน่งจริงจาก SpringPlayer.prefab (-0.95, 0)
        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(rig.transform, false);
        fp.transform.localPosition = new Vector3(-0.95f, 0f, 0f);
        firePointTransform = fp.transform;

        return rig;
    }

    private static GameObject CreateExactScaleAnimatedNode(
        Transform parent,
        string name,
        Sprite sprite,
        RuntimeAnimatorController ctrl,
        Vector3 localPos,
        float uniformScale,
        int sortingOrder,
        out Animator animator)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        animator = go.AddComponent<Animator>();
        if (ctrl != null) animator.runtimeAnimatorController = ctrl;
        return go;
    }

    private static GameObject CreateAnimatedFittedSpriteNode(
        Transform parent,
        string name,
        Sprite sprite,
        RuntimeAnimatorController ctrl,
        Vector3 localPos,
        float maxW,
        float maxH,
        int sortingOrder,
        out Animator animator)
    {
        GameObject go = CreateFittedSpriteNode(parent, name, sprite, localPos, maxW, maxH, sortingOrder);
        animator = go.AddComponent<Animator>();
        if (ctrl != null) animator.runtimeAnimatorController = ctrl;
        return go;
    }

    private static GameObject CreateFittedSpriteNode(Transform parent, string name, Sprite sprite, Vector3 localPos, float maxW, float maxH, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;

        if (sprite != null)
        {
            Vector2 sz = sprite.bounds.size;
            float sx = sz.x > 0.001f ? maxW / sz.x : 1f;
            float sy = sz.y > 0.001f ? maxH / sz.y : 1f;
            float uniform = Mathf.Min(sx, sy);
            go.transform.localScale = new Vector3(uniform, uniform, 1f);
        }
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Sprite sprite, Vector3 localPos, Vector2 size, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private static TextMesh CreateLabel(Transform parent, string text, Vector3 localPos, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(0.085f, 0.085f, 1f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = fontSize * 2;
        tm.characterSize = 0.65f;
        tm.anchor = anchor;
        tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
        tm.color = color;

        Font thaiFont = PortfolioShowcaseStage.GetThaiDynamicFont();
        if (thaiFont != null)
        {
            tm.font = thaiFont;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (thaiFont.material != null) mr.sharedMaterial = thaiFont.material;
                mr.sortingOrder = 40;
            }
        }
        return tm;
    }

    private static GameObject InstantiateDisplayPrefab(GameObject prefab, Scene scene, Transform parent, Vector3 localPos, float maxFitSize, bool disableScripts)
    {
        if (prefab == null) return null;

        GameObject inst = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (inst == null) return null;

        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = localPos;

        if (disableScripts)
        {
            StripNonVisualComponents(inst);
        }

        if (maxFitSize > 0.01f)
        {
            var renderers = inst.GetComponentsInChildren<SpriteRenderer>(false);
            if (renderers != null && renderers.Length > 0)
            {
                bool hasBounds = false;
                Bounds combined = default;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null || renderers[i].sprite == null) continue;
                    if (!hasBounds)
                    {
                        combined = renderers[i].bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(renderers[i].bounds);
                    }
                }
                if (hasBounds)
                {
                    float maxDim = Mathf.Max(combined.size.x, combined.size.y);
                    if (maxDim > 0.01f)
                    {
                        float factor = maxFitSize / maxDim;
                        inst.transform.localScale *= factor;
                    }
                }
            }
        }

        foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sortingOrder < 10) sr.sortingOrder = 14;
        foreach (var tr in inst.GetComponentsInChildren<TrailRenderer>(true))
            if (tr.sortingOrder < 10) tr.sortingOrder = 17;
        foreach (var psr in inst.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (psr.sortingOrder < 10) psr.sortingOrder = 18;

        return inst;
    }

    private static void StripNonVisualComponents(GameObject inst)
    {
        if (inst == null) return;
        foreach (var col in inst.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        foreach (var rb in inst.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
        foreach (var mb in inst.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            if (mb is ArrowTrailEffect || mb is MeatAuraEffect) continue;
            mb.enabled = false;
        }
    }

    private static Sprite ExtractFirstSprite(GameObject prefab)
    {
        if (prefab == null) return null;
        foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr != null && sr.sprite != null) return sr.sprite;
        }
        return null;
    }

    private static string SanitizeName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "Item";
        char[] invalid = { ' ', '/', '\\', ':', '(', ')', '.', ',' };
        string s = raw;
        foreach (char ch in invalid) s = s.Replace(ch, '_');
        return s;
    }

    private static void DestroyCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
    }

    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static T LoadAssetByGuid<T>(string guid) where T : UnityEngine.Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static Sprite LoadSpriteByGuid(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;

        Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (direct != null) return direct;

        UnityEngine.Object[] subs = AssetDatabase.LoadAllAssetsAtPath(path);
        if (subs != null)
        {
            foreach (var obj in subs)
            {
                if (obj is Sprite s) return s;
            }
        }
        return null;
    }

    private static void EnsureFolder(string assetPath)
    {
        string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        if (!Directory.Exists(full))
        {
            Directory.CreateDirectory(full);
            AssetDatabase.Refresh();
        }
    }

    private static Sprite EnsurePanelSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
        if (existing != null) return existing;

        Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        Color[] cols = new Color[16 * 16];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();

        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), PanelSpritePath), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(PanelSpritePath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(PanelSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
    }

    private static Sprite EnsureHeatTestSprite()
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(HeatTestSpritePath);
        if (existing != null) return existing;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool checker = ((x / 16) + (y / 16)) % 2 == 0;
                float t = y / (float)size;
                Color warmA = Color.Lerp(new Color(0.86f, 0.44f, 0.18f, 1f), new Color(0.32f, 0.56f, 0.85f, 1f), t);
                Color warmB = Color.Lerp(new Color(0.96f, 0.68f, 0.28f, 1f), new Color(0.48f, 0.72f, 0.94f, 1f), t);
                tex.SetPixel(x, y, checker ? warmA : warmB);
            }
        }
        tex.Apply();

        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), HeatTestSpritePath), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(HeatTestSpritePath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(HeatTestSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 128f;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(HeatTestSpritePath);
    }
}
#endif
