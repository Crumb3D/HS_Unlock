# HS_Unlock — game calls

3.2 vs 3.3: `7DaysRef/HS_Unlock_3.2_vs_3.3.md`. Signatures match. No version fork.

## Inherits
- `BlockSwitch` (`BlockPowered`)
- `IModApi`

## Harmony
- `TEFeatureDoor.OnBlockTriggered`

## Calls
- `Block.HandleTrigger`
- `BlockTrigger.HasAnyTriggers` / `HasAnyTriggeredBy` / `ToWorldPos` / `Unlock`
- `Chunk.GetBlockTrigger` / `AddTileEntity` / `GetBlockEntity`
- `World.SetBlock` / `SetBlockRPC` / `GetTileEntity` / `GetBlock`
- `TileEntityPowered.InitializePowerData` (`CreateTileEntity` from `BlockSwitch`)
- `TriggerManager.CheckPowerState`
- `Manager.BroadcastPlay`
- `Constants.cVersionInformation`
