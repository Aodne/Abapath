public static class ScoreRules
{
    // fraction of your points you keep 
    public static float KeepPct(int tot, int tgt)
    {
        if (tot * 100 >= tgt * 95) return 1f;
        if (tot * 100 >= tgt * 75) return 0.9f;
        if (tot * 100 >= tgt * 50) return 0.75f;
        if (tot * 100 >= tgt * 25) return 0.4f;
        return 0.2f;
    }

    // exact-hit multiplier
    public static int ExactFactor(int pathCount)
    {
        if (!GameConfig.difficultyTiers) return 2;
        if (pathCount >= 3) return 2;
        if (pathCount == 2) return 3;
        return 5;
    }

    public static string TierName(int pathCount)
    {
        if (pathCount >= 3) return "EASY";
        if (pathCount == 2) return "MEDIUM";
        return "HARD";
    }
}
