using System.Collections.Generic;
using UnityEngine;

public static class ScanDatabase
{
    static List<string> scannedIDs = new List<string>();
    static List<EnemyRank> unlockedRanks = new List<EnemyRank>();

    public static bool HasScanned(string id)
    {
        return scannedIDs.Contains(id);
    }

    public static void AddScan(string id)
    {
        if (!HasScanned(id))
        {
            scannedIDs.Add(id);
        }
    }

    public static bool HasRankAccess(EnemyRank rank)
    {
        return unlockedRanks.Contains(rank);
    }

    public static void UnlockRank(EnemyRank rank)
    {
        if (!HasRankAccess(rank))
        {
            unlockedRanks.Add(rank);
        }
    }

    // Since the list is static, this is to reset the list every play session
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetData()
    {
        scannedIDs.Clear();
        unlockedRanks.Clear();
    }
}