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
            lockable.SetLocked(true);
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
            Manager.BroadcastPlay(doorPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
            return HSUnlockSettings.OpenOnUnlock;
        }

        if (flagged && !HSUnlockSettings.OpenOnUnlock)
            return false;
        return true;
    }
}
