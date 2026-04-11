public static class GameScore
{
    public static int CurrentScore { get; private set; }
    public static int CurrentInsectsEaten { get; private set; }
    public static int TricksLandedCount { get; private set; }
    public static float DistanceX { get; private set; }

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;
        CurrentScore += amount;
    }

    public static void AddInsect()
    {
        CurrentInsectsEaten++;
    }

    public static void RegisterTrickLanded()
    {
        TricksLandedCount++;
    }

    public static void AddDistanceX(float deltaRight)
    {
        if (deltaRight <= 0f) return;
        DistanceX += deltaRight;
    }

    public static void Reset()
    {
        CurrentScore = 0;
        CurrentInsectsEaten = 0;
        TricksLandedCount = 0;
        DistanceX = 0f;
    }
}
