using System.Collections.Generic;
using System.IO;

// Which switch last unlocked which door. Saved with the world so a later click
// on that same switch locks the door again after a restart.
public static class HSUnlockDoorLinks
{
    static string loadedFor;
    static Dictionary<string, List<string>> byDoor = new Dictionary<string, List<string>>();

    public static bool Controls(Vector3i door, Vector3i trigger)
    {
        Ensure();
        List<string> list;
        if (!byDoor.TryGetValue(Key(door), out list))
            return false;
        return list.Contains(Key(trigger));
    }

    public static void Remember(Vector3i door, Vector3i trigger)
    {
        Ensure();
        string doorKey = Key(door);
        string triggerKey = Key(trigger);
        List<string> list;
        if (!byDoor.TryGetValue(doorKey, out list))
        {
            list = new List<string>();
            byDoor[doorKey] = list;
        }
        if (list.Contains(triggerKey))
            return;
        list.Add(triggerKey);
        Save();
    }

    static void Ensure()
    {
        string path = SavePath();
        if (path == loadedFor)
            return;
        byDoor = new Dictionary<string, List<string>>();
        loadedFor = path;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        string[] lines = File.ReadAllLines(path);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int split = line.IndexOf(' ');
            if (split <= 0)
                continue;
            string doorKey = line.Substring(0, split);
            string triggerKey = line.Substring(split + 1).Trim();
            if (triggerKey.Length == 0)
                continue;
            List<string> list;
            if (!byDoor.TryGetValue(doorKey, out list))
            {
                list = new List<string>();
                byDoor[doorKey] = list;
            }
            if (!list.Contains(triggerKey))
                list.Add(triggerKey);
        }
    }

    static void Save()
    {
        string path = SavePath();
        if (string.IsNullOrEmpty(path))
            return;
        List<string> lines = new List<string>();
        foreach (KeyValuePair<string, List<string>> pair in byDoor)
        {
            for (int i = 0; i < pair.Value.Count; i++)
                lines.Add(pair.Key + " " + pair.Value[i]);
        }
        File.WriteAllLines(path, lines.ToArray());
    }

    static string SavePath()
    {
        try
        {
            string dir = GameIO.GetSaveGameDir();
            if (string.IsNullOrEmpty(dir))
                return null;
            return Path.Combine(dir, "HSUnlockDoors.txt");
        }
        catch
        {
            return null;
        }
    }

    static string Key(Vector3i pos)
    {
        return pos.x + "," + pos.y + "," + pos.z;
    }
}
