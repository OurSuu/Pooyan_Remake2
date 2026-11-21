# 🛠️ Pooyan Remake — Implementation Plan
# Unity 2D | C# Scripts

> **อ้างอิงระบบเกมจาก:** `GAME_DESIGN.md` (ไฟล์เดียวกันใน root โปรเจค)
> **Script Location:** `Assets/Script/` (แยก folder: Core/, Player/, Enemy/, Stage/, UI/)
> **สถานะ:** ⬜ = ยังไม่เริ่ม | 🟡 = กำลังทำ | ✅ = เสร็จ
> **หมายเหตุ:** ไฟล์ Phase 1 (GameManager, ScoreManager, LevelManager) อยู่ใน `Assets/Script/` ให้ย้ายไป `Assets/Script/Core/` ใน Unity Editor

---

## Phase 1 — Core Foundation (ทำก่อน)

### ✅ 1.1 `GameManager.cs`
- Singleton ควบคุม game state ทั้งหมด
- States: `MainMenu`, `Playing`, `BonusStage`, `GameOver`, `StageClear`
- เก็บ: currentStage, lives (เริ่ม 3), isOddStage (คี่=ลง, คู่=ขึ้น)
- จัดการ stage progression: คี่→คู่→Bonus→คี่ (loop)
- เงื่อนไข Game Over (lives=0), Stage Clear

### ✅ 1.2 `ScoreManager.cs`
- คะแนนปัจจุบัน, High Score
- ตารางคะแนน: ลูกโป่งแตก=100, ยิงหิน=200, ยิงผลไม้=200
- ระบบคอมโบเนื้อ: 400→800→1600(cap) — ใช้ comboCounter reset เมื่อเนื้อหายไป
- Bonus fruit: Strawberry=100, Cherry=200, Peach=400

### ✅ 1.3 `LevelManager.cs`
- กำหนดค่าความยากแต่ละด่าน (ScriptableObject หรือ config)
- Parameters: wolfCount, wolfSpeedRange, shieldRatio, rockThrowRatio, balloonHP, hasBossWolf, hasTreeTopBoss
- ควบคุม wave spawning timing

---

## Phase 2 — Player System

### ✅ 2.1 `Player/PlayerController.cs`
- เลื่อนกระเช้าขึ้น/ลง (Vertical only) บน rail
- ขอบเขตบน/ล่าง (clamp position)
- State: `Normal`, `HoldingMeat`
- ตรวจจับการอยู่ที่จุดสูงสุด → หยิบเนื้อ
- รับ damage → เสีย life, เล่น death animation

### ✅ 2.2 `Player/ArrowShooter.cs`
- ยิงลูกธนู (Fire button) → spawn Arrow prefab
- จำกัด max 2 ลูกบนจอ (นับ active arrows)
- ไม่ยิงถ้า state=HoldingMeat
- Cooldown ระหว่างนัด (เล็กน้อย)

### ✅ 2.3 `Player/Arrow.cs` (ติด Arrow Prefab)
- เคลื่อนที่ **เส้นตรงแนวนอน** (ไม่มี gravity!)
- Collision: Balloon→ลดHP/แตก, WolfBody→หายไป, Shield→สะท้อน/หาย, Rock→ทำลาย(200), Fruit→ทำลาย(200)
- ทำลายตัวเองเมื่อออกจอ
- นับใน ArrowShooter เมื่อ spawn/destroy

### ✅ 2.4 `Player/MeatWeapon.cs` (ติด Meat Prefab)
- วิถี **Parabolic** (ใช้ Rigidbody2D + initial velocity + gravity)
- **Piercing** — ไม่หายเมื่อโดนศัตรู, กวาดทุกตัวในแนวตก
- comboCounter: ตัวที่1=400, ตัวที่2=800, ตัวที่3+=1600
- ทะลุโล่ — Boss Wolf โดนตายทันที
- ทำลายตัวเองเมื่อออกจอล่าง

---

## Phase 3 — Enemy System

### ✅ 3.1 `Enemy/WolfSpawner.cs`
- Spawn หมาป่าตาม LevelManager config
- กำหนด: balloon type, speed, มีโล่ไหม, ปาหินไหม
- ด่านคี่: spawn จากบน, ด่านคู่: spawn จากล่าง
- จัดการ Boss Wolf spawn timing + หยุด spawn เนื้อก่อน Boss มา

### ✅ 3.2 `Enemy/Wolf.cs`
- State Machine: `Floating`→`Landed`→`Climbing`→`Waiting`→`Biting` (ด่านคี่) หรือ `Floating`→`ReachedCliff` (ด่านคู่)
- ความเร็วลอยสุ่มในช่วงที่กำหนด
- ระบบโล่: สลับ shieldUp/shieldDown เป็นจังหวะ
- ระบบปาหิน: ปาเป็นระยะขณะลอย
- Passive Deflection: ลูกธนูโดนตัว=ไม่มีผล
- Death: ลูกโป่งแตก→ร่วง→remove

### ✅ 3.3 `Enemy/BossWolf.cs` (extends Wolf)
- ลูกโป่งกระพริบแดง (Flashing Red)
- โล่ทน 5 นัด (shieldHP=5) → นัดที่6 ทะลุ
- โดนเนื้อ = ตายทันที
- ยิงสำเร็จ = trigger StageClear ทันที
- ปรากฏท้าย wave

### ✅ 3.4 `Enemy/TreeTopBossWolf.cs`
- เดินไปมาบนยอดไม้ (ด้านบนจอ)
- ปาผลไม้ลงมาเป็นระยะ
- ปรากฏตั้งแต่ Stage 3+
- ไม่สามารถยิงตัว Boss ตัวนี้ได้ (แค่ยิงผลไม้ที่มันปา)

### ✅ 3.5 `Enemy/Balloon.cs` (ติด Balloon child object ของ Wolf)
- HP system: 1 (Normal) → 2-5 (Multi-Hit ตามด่าน)
- TakeDamage(): ลด HP, เปลี่ยน sprite/ขนาด (visual feedback)
- HP=0 → Pop() → แจ้ง Wolf ให้ร่วง
- Boss variant: กระพริบแดง (coroutine สลับสี)

### ✅ 3.6 `Enemy/WolfProjectile.cs` (หิน/ผลไม้ที่หมาป่าปา)
- เคลื่อนที่หากระเช้า (หรือวิถีตรง/โค้งเล็กน้อย)
- โดนลูกธนู = ทำลาย → 200 แต้ม
- โดนกระเช้า = ผู้เล่นเสีย life
- ทำลายเมื่อออกจอ

---

## Phase 4 — Stage-Specific Systems

### ✅ 4.1 `Stage/LadderSystem.cs` (ด่านคี่)
- บันไดอยู่ด้านหลังกระเช้า
- หมาป่าที่ลงพื้น → เดินไปบันได → ปีนขึ้น → หยุดรอ
- ตรวจจับว่ากระเช้าเลื่อนลงมาถึงตำแหน่งหมาป่า → trigger bite → เสีย life
- รองรับหมาป่าหลายตัวบนบันไดพร้อมกัน

### ✅ 4.2 `Stage/BoulderSystem.cs` (ด่านคู่)
- นับหมาป่าที่ขึ้นถึงหน้าผา (wolvesOnCliff counter)
- แสดง visual ของหมาป่าบนหน้าผา
- ครบ 7 ตัว → เล่น animation ผลักหิน → หินตกลงมา → ผู้เล่นตาย
- Reset counter เมื่อเริ่มด่านใหม่

### ✅ 4.3 `Stage/BonusStageManager.cs`
- สลับ Fruit Bonus / Meat Bonus
- Fruit: spawn ผลไม้ตกลงมา (Strawberry100/Cherry200/Peach400)
- Meat: spawn หมาป่าเป็น pattern, ให้ใช้เนื้อกวาด
- ไม่มี Game Over, มี timer → จบ Bonus → ไปด่านถัดไป

---

## Phase 5 — UI & Polish

### ✅ 5.1 `UI/UIManager.cs`
- HUD: Score, High Score, Lives, Stage number
- ด่านคู่: แสดงจำนวนหมาป่าบนหน้าผา (x/7)
- Stage Clear screen, Game Over screen
- Bonus Stage results

### ✅ 5.2 `UI/ComboDisplay.cs`
- Popup คะแนนคอมโบ (400→800→1600) ตำแหน่งที่เนื้อโดนหมาป่า
- Float up + fade out animation

### ✅ 5.3 `UI/MeatPickupIndicator.cs`
- แสดง indicator ว่าเนื้อพร้อมหยิบที่จุดสูงสุด
- กระพริบ/เรืองแสงเมื่อพร้อม

---

## Phase 6 — Audio & Effects

### ✅ 6.1 `Core/AudioManager.cs`
- เล่น SE/BGM ตาม event (ดูรายการใน GAME_DESIGN.md Section 11)

### ⬜ 6.2 Visual Effects
- Balloon pop particle
- Wolf falling animation
- Meat trail effect
- Boulder falling shake screen
- Score popup floating text

---

## Phase 7 — Scene Setup & Prefabs

### ⬜ 7.1 Prefabs ที่ต้องสร้าง
- `Arrow` — Sprite + Arrow.cs + Collider2D
- `Meat` — Sprite + MeatWeapon.cs + Rigidbody2D + Collider2D
- `Wolf` — Sprite + Wolf.cs + Balloon child + Collider2D
- `BossWolf` — Sprite + BossWolf.cs + Flashing Balloon + Shield
- `WolfProjectile` — Sprite + WolfProjectile.cs + Collider2D
- `Fruit` (Bonus) — Sprite + Collider2D
- `ScorePopup` — TextMeshPro + ComboDisplay.cs

### ⬜ 7.2 Scene Hierarchy
```
GameScene
├── Managers (GameManager, ScoreManager, LevelManager, AudioManager)
├── Player
│   ├── Elevator Rail
│   ├── MamaPig (PlayerController, ArrowShooter)
│   └── MeatSpawnPoint
├── EnemySystem
│   ├── WolfSpawner
│   ├── TreeTopBossWolf (Stage 3+)
│   └── SpawnPoints (Top/Bottom)
├── StageElements
│   ├── Ladders (ด่านคี่)
│   ├── Cliff (ด่านคู่)
│   └── Boulder
├── UI Canvas
│   ├── HUD
│   ├── ComboDisplay
│   └── GameOverScreen
└── Background
```

---

## ลำดับการ Implement (Recommended Order)

```
1. GameManager + ScoreManager          ← Foundation
2. PlayerController + ArrowShooter     ← ผู้เล่นเคลื่อนที่ได้
3. Arrow                              ← ยิงได้
4. Wolf + Balloon + WolfSpawner        ← มีศัตรูให้ยิง (playable!)
5. MeatWeapon                          ← ระบบเนื้อ+คอมโบ
6. Wolf behaviors (Shield, Rock)       ← ศัตรูฉลาดขึ้น
7. BossWolf + TreeTopBossWolf          ← บอส
8. LadderSystem + BoulderSystem        ← ระบบ death ด่านคี่/คู่
9. LevelManager + Difficulty           ← ความยากเพิ่มขึ้น
10. BonusStageManager                  ← Bonus Stage
11. UIManager + ComboDisplay           ← UI
12. AudioManager + VFX                 ← Polish
```

---

## Tags & Layer Setup (Unity)

| Layer | ใช้สำหรับ |
|---|---|
| Player | กระเช้า/แม่หมู |
| Arrow | ลูกธนู |
| Meat | เนื้อ |
| Enemy | หมาป่า (ลำตัว) |
| Balloon | ลูกโป่ง |
| Shield | โล่หมาป่า |
| EnemyProjectile | หิน/ผลไม้ที่หมาป่าปา |
| Pickup | เนื้อที่รอหยิบ |

## Collision Matrix (ใคร collide กับใคร)

| | Player | Arrow | Meat | Enemy | Balloon | Shield | EnemyProj |
|---|---|---|---|---|---|---|---|
| **Player** | - | - | - | ✅ bite | - | - | ✅ damage |
| **Arrow** | - | - | - | ❌ no effect | ✅ pop | ✅ block | ✅ destroy |
| **Meat** | - | - | - | ✅ kill | ✅ pop | ✅ bypass | - |

---

> **หมายเหตุสำหรับ AI/Developer ที่มาทำต่อ:**
> 1. อ่าน `GAME_DESIGN.md` เพื่อเข้าใจระบบเกม 100%
> 2. อ่านไฟล์นี้เพื่อดูว่า implement ถึงไหนแล้ว (ดูสถานะ ⬜/🟡/✅)
> 3. ทำตามลำดับ "Recommended Order" ด้านบน
> 4. ทุก Script อยู่ใน `Assets/Script/`
> 5. อัพเดทสถานะในไฟล์นี้เมื่อทำเสร็จแต่ละ Script
