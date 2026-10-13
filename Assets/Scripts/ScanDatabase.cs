using System.Collections.Generic;
using UnityEngine;

// Not attached to anything. Any script can call ScanDatabase.HasScanned("Alien1")
// (for example a map door that unlocks once Alien1 has been scanned).
public static class ScanDatabase
{
    static List<string> scannedIDs = new List<string>();

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

    // since the list is static, this is to reset the list everytime u play the game
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetData()
    {
        scannedIDs.Clear();
    }
}

