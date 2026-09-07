// เก็บ Enum ต่างๆ สำหรับสถานะในเกม
public enum GameState
{
    MainMenu,
    Playing,
    BonusStage,
    StageClear,
    GameOver
}

// ทิศทางของด่าน ลงหรือขึ้น
public enum StageDirection
{
    Descending, // หมาป่าโรยตัวลงมา (ด่าน 1, 3, 5...)
    Ascending   // หมาป่าลอยลูกโป่งขึ้น (ด่าน 2, 4, 6...)
}

// ชนิดของของรางวัลในฉากโบนัส
public enum BonusType
{
    Fruit, // ผลไม้
    Meat   // เนื้อ
}
