using System;
using Audio;
using HarmonyLib;
using UnityEngine;

// Doors use TEFeatureLockable and no TEFeatureLockPickable, so a lockpick does nothing.
// Adding the feature in XML changes the saved feature count and drops the tile entity.
// The pick command is added on the lock feature that is already there.
[HarmonyPatch(typeof(TEFeatureLockable))]
public static class HSUnlockLockPick
{
    const string ItemName = "resourceLockPick";
    const float PickTime = 15f;
    const float BreakChance = 0.75f;

    class Job
    {
        public TEFeatureLockable Lock;
        public EntityPlayerLocal Player;
        public Vector3i Pos;
    }

    [HarmonyPatch("InitBlockActivationCommands")]
    [HarmonyPostfix]
    static void AddPick(TEFeatureLockable __instance, Action<BlockActivationCommand, TileEntityComposite.EBlockCommandOrder, TileEntityFeatureData> _addCallback)
    {
        if (!IsDoorWithoutPick(__instance))
            return;
        _addCallback(new BlockActivationCommand("pick", "unlock", false), TileEntityComposite.EBlockCommandOrder.First, __instance.FeatureData);
    }

    [HarmonyPatch("AllowBlockActivationCommand")]
    [HarmonyPostfix]
    static void AllowPick(TEFeatureLockable __instance, ITileEntityFeature _module, ReadOnlySpan<char> _commandName, ref bool __result)
    {
        if (!__instance.CommandIs(_commandName, "pick") || !__instance.Equals(_module))
            return;
        __result = CanPick(__instance);
    }

    [HarmonyPatch("OnBlockActivated")]
    [HarmonyPostfix]
    static void DoPick(TEFeatureLockable __instance, ReadOnlySpan<char> _commandName, Vector3i _blockPos, EntityPlayerLocal _player, ref bool __result)
    {
        if (!__instance.CommandIs(_commandName, "pick") || !CanPick(__instance))
            return;
        __result = true;
        LocalPlayerUI ui = _player.PlayerUI;
        ItemValue item = ItemClass.GetItem(ItemName);
        if (ui.xui.PlayerInventory.GetItemCount(item) == 0)
        {
            ui.xui.CollectedItemList.AddItemStack(new ItemStack(item, 0), true);
            GameManager.ShowTooltip(_player, Localization.Get("ttLockpickMissing"));
            return;
        }
        _player.AimingGun = false;
        float time = EffectManager.GetValue(PassiveEffects.LockPickTime, _player.inventory.holdingItemItemValue, PickTime, _player);
        float breakChance = EffectManager.GetValue(PassiveEffects.LockPickBreakChance, _player.inventory.holdingItemItemValue, BreakChance, _player);
        if (time < 0.5f)
            time = 0.5f;
        TimerEventData data = new TimerEventData();
        data.Data = new Job { Lock = __instance, Player = _player, Pos = _blockPos };
        data.FullTimeFinishEvent += OnSuccess;
        data.CloseEvent += OnClose;
        if (_player.rand.RandomFloat < breakChance)
        {
            data.AlternateTime = time * _player.rand.RandomRange(0.25f, 0.9f);
            data.AlternateEvent += OnBreak;
        }
        Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
        XUiC_Timer.OpenTimer(ui.xui, time, data, -1f, "", true);
    }

    static bool IsDoorWithoutPick(TEFeatureLockable lockable)
    {
        TileEntityComposite parent = lockable.Parent;
        if (parent == null)
            return false;
        if (parent.GetFeature<TEFeatureDoor>() == null)
            return false;
        return parent.GetFeature<TEFeatureLockPickable>() == null;
    }

    static bool CanPick(TEFeatureLockable lockable)
    {
        if (!IsDoorWithoutPick(lockable) || !lockable.IsLocked() || lockable.LocalPlayerIsOwner())
            return false;
        World world = GameManager.Instance.World;
        if (world != null && world.GetTraderAreaAt(lockable.ToWorldPos()) != null)
            return false;
        return true;
    }

    static void TakePick(EntityPlayerLocal player)
    {
        ItemValue item = ItemClass.GetItem(ItemName);
        ItemStack stack = new ItemStack(item, 1);
        LocalPlayerUI ui = LocalPlayerUI.GetUIForPlayer(player);
        ui.xui.PlayerInventory.RemoveItem(stack);
        ui.xui.CollectedItemList.RemoveItemStack(stack);
    }

    static void OnBreak(TimerEventData data)
    {
        Job job = data.Data as Job;
        if (job == null || job.Player == null)
            return;
        TakePick(job.Player);
        Manager.BroadcastPlayByLocalPlayer(job.Pos.ToVector3() + Vector3.one * 0.5f, "Misc/locked");
        GameManager.ShowTooltip(job.Player, Localization.Get("ttLockpickBroken"));
    }

    static void OnSuccess(TimerEventData data)
    {
        Job job = data.Data as Job;
        if (job == null || job.Lock == null || job.Player == null)
            return;
        TakePick(job.Player);
        job.Lock.SetLocked(false);
        Manager.BroadcastPlayByLocalPlayer(job.Pos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
        GameManager.ShowTooltip(job.Player, Localization.Get("containerUnlocked"));
    }

    static void OnClose(TimerEventData data)
    {
        Job job = data.Data as Job;
        if (job == null)
            return;
        Manager.BroadcastPlayByLocalPlayer(job.Pos.ToVector3() + Vector3.one * 0.5f, "Misc/locked");
    }
}
