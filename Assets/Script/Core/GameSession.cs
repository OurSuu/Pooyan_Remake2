public static class GameSession
{
    public static bool IsActive = false;
    public static int CurrentStage = 1;
    public static int Lives = 3;
    public static int Score = 0;
    
    public static void ResetSession()
    {
        IsActive = true;
        CurrentStage = 1;
        Lives = 3; // GameConstants.StartingLives
        Score = 0;
    }
}
