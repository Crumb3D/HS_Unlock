using System.Reflection;
using HarmonyLib;

public class HSUnlockMod : IModApi
{
    public static string ModPath;

    public void InitMod(Mod _modInstance)
    {
        if (!HSGameVersion.AllowLoad("[HSUnlock]"))
            return;
        ModPath = _modInstance.Path;
        HSUnlockSettings.Load(ModPath);
        Log.Out("[HSUnlock] v1.2.3 locked doors unlock without moving. The same switch locks them again. OpenOnUnlock also opens a shut door. Lockpick starts at 10s and resumes after a break. Install on the host and on every client.");
        try
        {
            new Harmony("HSUnlock").PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (System.Exception e)
        {
            Log.Error("[HSUnlock] Harmony patch failed. Unlock buttons will still open doors. " + e);
        }
    }
}
