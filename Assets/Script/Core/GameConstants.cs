/// <summary>
/// แหล่งรวมค่าคงที่ (Constants) ของเกม Pooyan Remake 
/// พวก Tags, Layers, คะแนน แล้วก็ค่า Setting ต่างๆ มัดรวมไว้ที่นี่หมด
/// </summary>
public static class GameConstants
{
    // พวก Tags เอาไว้เช็คตอนชนหรือค้นหา Object
    public const string TagPlayer = "Player";
    public const string TagArrow = "Arrow";
    public const string TagMeat = "Meat";
    public const string TagEnemy = "Enemy";
    public const string TagBalloon = "Balloon";
    public const string TagShield = "Shield";
    public const string TagEnemyProjectile = "EnemyProjectile";
    public const string TagPickup = "Pickup";

    // เรื่องคะแนน (อิงตามเกมตู้ Arcade เป๊ะๆ เลย)
    public const int ScoreBalloonPop = 200;       // เมื่อก่อนให้ 100 แต่ตู้ Arcade มัน 200 เลยแก้ตาม
    public const int ScoreRockDestroy = 100;      // ทุบหินแตกได้ 100 (เมื่อก่อน 200)
    public const int ScoreFruitDestroy = 200;
    public const int ScoreMeatPickup = 200;
    // คะแนนคอมโบเนื้อ: สูตรคือ 400 * 2^(n-1) — ไม่มีลิมิต! ไปคำนวณใน ScoreManager เอานะ
    public const int ScoreMeatComboBase = 400;
    public const int ScoreBonusStrawberry = 100;
    public const int ScoreBonusCherry = 200;
    public const int ScoreBonusPeach = 400;
    public const int ScoreSecretLeaf = 4000;
    public const int ScoreSecretMushroom = 8000;

    // การตั้งค่าเกมเพลย์
    public const int StartingLives = 3;           // เริ่มต้นมามี 3 ชีวิต
    public const int MaxArrowsOnScreen = 2;       // ยิงธนูได้มากสุด 2 ดอกพร้อมกันบนจอ
    public const int BoulderWolfThreshold = 5;    // จำนวนหมาป่าที่หินกลิ้งลงมา
    public const int BoulderWolvesAfterDeath = 4; // ถ้าตายแล้ว หมาป่าจะเหลือ 4 ตัว
    public const int BossEscapeExtraWolves = 4;
    public const int BossShieldHits = 5;          // บอสมีโล่ที่ต้องยิง 5 ทีถึงจะพัง
    public const float ArrowSpeed = 24f;          // สปีดธนู ไวๆ สไตล์ Arcade เลย
    public const float RapidShootCooldown = 0.12f; // ดีเลย์ระหว่างนัดที่ 1 กับนัดที่ 2 (0.12s) ง้างปล่อยไว ยิงเบิ้ล 2 ดอกติดกันได้ตามต้นฉบับ
    public const float ReloadShootCooldown = 0.40f; // คูลดาวน์หลังยิงครบ 2 นัด (0.40s) จังหวะดึงลูกธนูชุดใหม่ขึ้นมาง้าง ป้องกันสแปมปืนกล
    public const float DefaultShootCooldown = 0.35f; // ค่าคูลดาวน์เฉลี่ยทั่วไป
    public const float MeatThrowForce = 8f;       // แรงตอนขว้างเนื้อ

    // เกณฑ์คะแนนสำหรับได้ 1-Up (อิงตามค่าเริ่มต้นจาก DIP switch ของตู้)
    public static readonly int[] ExtraLifeThresholds = { 30000, 70000 };
}
