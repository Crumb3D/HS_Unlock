using System.IO;

public static class HSUnlockSettings
{
    public static bool OpenOnUnlock;

    public static void Load(string modPath)
    {
        OpenOnUnlock = false;
        if (!string.IsNullOrEmpty(modPath))
            TryRead(Path.Combine(modPath, "HSUnlockSettings.json"));
        try
        {
            string user = GameIO.GetUserGameDataDir();
            if (!string.IsNullOrEmpty(user))
                TryRead(Path.Combine(user, "HSUnlock", "HSUnlockSettings.json"));
        }
        catch { }
        Log.Out("[HSUnlock] OpenOnUnlock=" + (OpenOnUnlock ? "true (unlock and open)" : "false (unlock, leave shut)"));
    }

    static void TryRead(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        string text = File.ReadAllText(path);
        int i = text.IndexOf("OpenOnUnlock");
        if (i < 0)
            return;
        int colon = text.IndexOf(':', i);
        if (colon < 0)
            return;
        string rest = text.Substring(colon + 1).TrimStart();
        if (rest.StartsWith("true"))
            OpenOnUnlock = true;
        else if (rest.StartsWith("false"))
            OpenOnUnlock = false;
        Log.Out("[HSUnlock] Read " + path);
    }
}
