using System.Collections.Generic;

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
}

