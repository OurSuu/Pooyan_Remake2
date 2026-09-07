/// <summary>
/// Shared constants for Pooyan Remake â€” tags, layers, scoring, gameplay values.
/// </summary>
public static class GameConstants
{
    // Tags
    public const string TagPlayer = "Player";
    public const string TagArrow = "Arrow";
    public const string TagMeat = "Meat";
    public const string TagEnemy = "Enemy";
    public const string TagBalloon = "Balloon";
    public const string TagShield = "Shield";
    public const string TagEnemyProjectile = "EnemyProjectile";
    public const string TagPickup = "Pickup";

    // Scoring (arcade-accurate values)
    public const int ScoreBalloonPop = 200;       // Was 100 â€” arcade is 200
    public const int ScoreRockDestroy = 100;       // Was 200 â€” arcade is 100
    public const int ScoreFruitDestroy = 200;
    public const int ScoreMeatPickup = 200;
    // Meat combo: 400 * 2^(n-1) â€” no cap! Calculated in ScoreManager.
    public const int ScoreMeatComboBase = 400;
    public const int ScoreBonusStrawberry = 100;
    public const int ScoreBonusCherry = 200;
    public const int ScoreBonusPeach = 400;
    public const int ScoreSecretLeaf = 4000;
    public const int ScoreSecretMushroom = 8000;

    // Gameplay
    public const int StartingLives = 3;
    public const int MaxArrowsOnScreen = 2;
    public const int BoulderWolfThreshold = 5;
    public const int BoulderWolvesAfterDeath = 4;
    public const int BossEscapeExtraWolves = 4;
    public const int BossShieldHits = 5;
    public const float ArrowSpeed = 24f; // Arcade style: fast arrows
    public const float MeatThrowForce = 8f;

    // Extra Life thresholds (arcade DIP switch defaults)
    public static readonly int[] ExtraLifeThresholds = { 30000, 70000 };
}

