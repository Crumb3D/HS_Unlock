using Audio;
using UnityEngine;

// BlockSwitch (3.2 and 3.3): wire tool accepts BlockPowered, CreateTileEntity
// returns TileEntityPoweredTrigger, and "is BlockSwitch" selects TriggerTypes.Switch.
// POI buttons were Activate / ActivateSwitch / ActivateSingle, so they had no power tile.
public class BlockHSUnlockButton : BlockSwitch
{
    enum PoiMode
    {
        Repeat,
        Single,
        Gated
    }

    const int MetaReady = 1;
    const int MetaUsed = 2;

    PoiMode poiMode = PoiMode.Repeat;
    string activateSound;
    new BlockActivationCommand[] cmds = new BlockActivationCommand[2]
    {
        new BlockActivationCommand("light", "electric_switch", false),
        new BlockActivationCommand("take", "hand", false)
    };

    public override bool AllowBlockTriggers
    {
        get { return true; }
    }

    public override void Init()
    {
        base.Init();
        if (Properties.Values.ContainsKey("ActivateSound"))
            activateSound = Properties.Values["ActivateSound"];
        string mode = "repeat";
        if (Properties.Values.ContainsKey("HSUnlockPoi"))
            mode = Properties.Values["HSUnlockPoi"];
        if (mode == "gated")
            poiMode = PoiMode.Gated;
        else if (mode == "single")
            poiMode = PoiMode.Single;
        else
            poiMode = PoiMode.Repeat;
    }

    public override void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue, PlatformUserIdentifierAbs _addedByPlayer)
    {
        base.OnBlockAdded(_world, _chunk, _blockPos, _blockValue, _addedByPlayer);
        if (_blockValue.ischild || _addedByPlayer == null)
            return;
        if (_world.GetTileEntity(_blockPos) is TileEntityPoweredTrigger)
            return;
        TileEntityPowered te = CreateTileEntity(_chunk);
        te.localChunkPos = World.toBlock(_blockPos);
        te.InitializePowerData();
        _chunk.AddTileEntity(te);
    }

    public override void OnTriggerAddedFromPrefab(BlockTrigger _trigger, Vector3i _blockPos, BlockValue _blockValue, FastTags<TagGroup.Global> _questTags)
    {
        if (poiMode != PoiMode.Gated)
            return;
        if (GameManager.Instance.World.IsEditor())
            return;
        World world = GameManager.Instance.World;
        base.OnTriggerAddedFromPrefab(_trigger, _blockPos, _blockValue, _questTags);
        bool waiting = _trigger.HasAnyTriggeredBy();
        _blockValue.meta = (byte)((_blockValue.meta & ~MetaReady) | (waiting ? 0 : MetaReady));
        world.SetBlock(_trigger.ToWorldPos(), _blockValue, true, false);
    }

    public override void OnTriggerRefresh(BlockTrigger _trigger, BlockValue _bv, FastTags<TagGroup.Global> questTag)
    {
        if (poiMode != PoiMode.Gated)
            return;
        GameManager gm = GameManager.Instance;
        gm.StartCoroutine(gm.World.triggerManager.CheckPowerState(_trigger, questTag, powered =>
        {
            _bv.meta = (byte)((_bv.meta & ~MetaReady) | (powered ? MetaReady : 0));
            gm.World.SetBlockRPC(_trigger.ToWorldPos(), _bv);
        }));
    }

    public override void OnTriggered(EntityPlayer _player, WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, System.Collections.Generic.List<BlockChangeInfo> _blockChanges, BlockTrigger _triggeredBy)
    {
        base.OnTriggered(_player, _world, _blockPos, _blockValue, _blockChanges, _triggeredBy);
        if (poiMode != PoiMode.Gated)
            return;
        _blockValue.meta = (byte)((_blockValue.meta & ~MetaReady) | MetaReady);
        _blockChanges.Add(new BlockChangeInfo(_blockPos, _blockValue));
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return GetActivationText(_world, _world.GetBlock(parentPos), parentPos, _entityFocusing);
        }
        BlockTrigger trigger;
        if (TryGetPoiTrigger(_world, _blockPos, out trigger))
        {
            if (!PoiCanFire(_blockValue))
                return "";
            PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
            string key = playerInput.Activate.GetBindingXuiMarkupString() + playerInput.PermanentActions.Activate.GetBindingXuiMarkupString();
            return string.Format(Localization.Get("questBlockActivate"), key, _blockValue.Block.GetLocalizedBlockName());
        }
        return base.GetActivationText(_world, _blockValue, _blockPos, _entityFocusing);
    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return OnBlockActivated(_commandName, _world, parentPos, _world.GetBlock(parentPos), _player);
        }
        if (_commandName == "take")
            return base.OnBlockActivated(_commandName, _world, _blockPos, _blockValue, _player);
        if (_commandName != "light")
            return false;

        BlockTrigger trigger;
        if (TryGetPoiTrigger(_world, _blockPos, out trigger))
        {
            if (!_world.IsEditor() && !PoiCanFire(_blockValue))
                return true;
            HandleTrigger(_player, (World)_world, _blockPos, _blockValue);
            if (!string.IsNullOrEmpty(activateSound))
                Manager.BroadcastPlay(_blockPos.ToVector3() + Vector3.one * 0.5f, activateSound);
            if (poiMode != PoiMode.Repeat)
            {
                bool used = (_blockValue.meta & MetaUsed) != 0;
                _blockValue.meta = (byte)((_blockValue.meta & ~MetaUsed) | (used ? 0 : MetaUsed));
                _world.SetBlockRPC(_blockPos, _blockValue);
            }
            return true;
        }
        return base.OnBlockActivated(_commandName, _world, _blockPos, _blockValue, _player);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return GetBlockActivationCommands(_world, _world.GetBlock(parentPos), parentPos, _entityFocusing);
        }
        BlockTrigger trigger;
        bool poi = TryGetPoiTrigger(_world, _blockPos, out trigger);
        cmds[0].enabled = !poi || PoiCanFire(_blockValue) || _world.IsEditor();
        bool claim = _world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer());
        cmds[1].enabled = claim && TakeDelay > 0f && !poi;
        return cmds;
    }

    public override void OnBlockValueChanged(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _chunk, _blockPos, _oldBlockValue, _newBlockValue);
        UpdateModel(_chunk, _blockPos, _newBlockValue);
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _blockValue, _ebcd);
        if (_ebcd != null && _ebcd.bHasTransform)
            ApplyModel(_ebcd, _blockValue);
    }

    bool PoiCanFire(BlockValue bv)
    {
        if (poiMode == PoiMode.Gated && (bv.meta & MetaReady) == 0)
            return false;
        return true;
    }

    bool TryGetPoiTrigger(WorldBase world, Vector3i pos, out BlockTrigger trigger)
    {
        trigger = null;
        if (world == null || world.ChunkCache == null)
            return false;
        Chunk chunk = world.ChunkCache.GetChunkSync(World.toChunkXZ(pos.x), pos.y, World.toChunkXZ(pos.z)) as Chunk;
        if (chunk == null)
            return false;
        trigger = chunk.GetBlockTrigger(World.toBlock(pos));
        return trigger != null && trigger.HasAnyTriggers();
    }

    void UpdateModel(Chunk chunk, Vector3i pos, BlockValue bv)
    {
        if (chunk == null)
            return;
        BlockEntityData ebcd = chunk.GetBlockEntity(pos);
        if (ebcd != null && ebcd.bHasTransform)
            ApplyModel(ebcd, bv);
    }

    void ApplyModel(BlockEntityData ebcd, BlockValue bv)
    {
        bool used = (bv.meta & MetaUsed) != 0;
        bool ready = (bv.meta & MetaReady) != 0;
        BlockSwitchController lights = ebcd.transform.GetComponent<BlockSwitchController>();
        if (lights != null)
            lights.SetState(ready, used);
        BlockSwitchSingleController single = ebcd.transform.GetComponent<BlockSwitchSingleController>();
        if (single != null)
            single.SetState(used);
    }
}
