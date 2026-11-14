public enum EnemyRank
{
    Low,
    Mid,
    High,
    Captain
}

// Display names for UI text: "Low-Rank", "Mid-Rank", "High-Rank", "Captain"
public static class RankNames
{
    public static string GetName(EnemyRank rank)
    {
        if (rank == EnemyRank.Captain)
        {
            return "Captain";
        }

        return rank + "-Rank";
    }
}