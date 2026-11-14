using System.Collections.Generic;
using Audio;
using HarmonyLib;
using UnityEngine;

// Quest end buttons open the door and leave TEFeatureLockable locked.
// 3.2 and 3.3 TEFeatureDoor.OnBlockTriggered only calls SetLocked when
// BlockTrigger.Unlock is set, then always SetOpen. The same switch locks
// that door again on the next click.
[HarmonyPatch(typeof(TEFeatureDoor), "OnBlockTriggered")]
public static class HSUnlockDoorPatch
{
    static ulong markedTick;
    static readonly HashSet<Vector3i> marked = new HashSet<Vector3i>();

    static bool Prefix(TEFeatureDoor __instance, BlockTrigger _triggeredBy)
    {
        TileEntityComposite parent = __instance.Parent;
        if (parent == null)
            return true;
        TEFeatureLockable lockable = parent.GetFeature<TEFeatureLockable>();
        if (lockable == null)
            return true;

        bool flagged = _triggeredBy != null && _triggeredBy.Unlock;
        bool locked = lockable.IsLocked();
        bool poiDoor = parent.Owner == null;
        Vector3i doorPos = __instance.ToWorldPos();
        bool sameSwitch = false;
        Vector3i triggerPos = Vector3i.zero;
        if (_triggeredBy != null)
        {
            triggerPos = _triggeredBy.ToWorldPos();
            sameSwitch = HSUnlockDoorLinks.Controls(doorPos, triggerPos);
        }

        if (!locked && sameSwitch)
        {
            if (JustUnlocked(doorPos))
                return false;
            lockable.SetLocked(true);
            HSUnlockLockPick.ClearProgress(doorPos);
            if (__instance.IsOpen())
                __instance.SetOpen(false, true);
            Manager.BroadcastPlay(doorPos.ToVector3() + Vector3.one * 0.5f, "Misc/locking");
            return false;
        }

        if (locked && (flagged || poiDoor))
        {
            lockable.SetLocked(false);
            if (_triggeredBy != null)
                HSUnlockDoorLinks.Remember(doorPos, triggerPos);
            MarkUnlocked(doorPos);
            Manager.BroadcastPlay(doorPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
            if (HSUnlockSettings.OpenOnUnlock && !__instance.IsOpen())
                __instance.SetOpen(true, true);
            return false;
        }

        if (flagged && !HSUnlockSettings.OpenOnUnlock)
            return false;
        return true;
    }

    static void MarkUnlocked(Vector3i pos)
    {
        if (GameTimer.Instance == null)
            return;
        ulong tick = GameTimer.Instance.ticks;
        if (tick != markedTick)
        {
            markedTick = tick;
            marked.Clear();
        }
        marked.Add(pos);
    }

    static bool JustUnlocked(Vector3i pos)
    {
        return GameTimer.Instance != null && GameTimer.Instance.ticks == markedTick && marked.Contains(pos);
    }
}

[HarmonyPatch(typeof(TEFeatureDoor))]
public static class HSUnlockDoorPrompt
{
    [HarmonyPatch("AllowBlockActivationCommand")]
    [HarmonyPostfix]
    static void HideOpenWhenLocked(TEFeatureDoor __instance, ITileEntityFeature _module, System.ReadOnlySpan<char> _commandName, ref bool __result)
    {
        if (!__result || !__instance.Equals(_module))
            return;
        if (!__instance.CommandIs(_commandName, "open") && !__instance.CommandIs(_commandName, "close"))
            return;
        TEFeatureLockable lockable = __instance.Parent != null ? __instance.Parent.GetFeature<TEFeatureLockable>() : null;
        if (lockable != null && lockable.IsLocked() && !lockable.IsUserAllowed(Platform.PlatformManager.InternalLocalUserIdentifier))
            __result = false;
    }

    [HarmonyPatch("GetActivationText")]
    [HarmonyPostfix]
    static void LockedPrompt(TEFeatureDoor __instance, string _activateHotkeyMarkup, ref string __result)
    {
        TEFeatureLockable lockable = __instance.Parent != null ? __instance.Parent.GetFeature<TEFeatureLockable>() : null;
        if (lockable == null || !lockable.IsLocked() || lockable.IsUserAllowed(Platform.PlatformManager.InternalLocalUserIdentifier))
            return;
        string label = HSUnlockLockPick.CanPick(lockable) ? Localization.Get("HSUnlockLockedPickable") : Localization.Get("HSUnlockLocked");
        __result = _activateHotkeyMarkup + " " + label;
    }
}
