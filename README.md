# HS Unlock

7 Days to Die 3.0–3.3. Same DLL on the client and the dedicated server.

Offline play only needs this folder in the game Mods directory, because that process is the host. A dedicated server still needs it on the server and on every client: recipes and the wire tool run on the client, and the unlock runs on the host.

## Unlock

The button or panel at the end of a job opens the loot door and leaves it locked. That door now unlocks and stays shut. Open it yourself. Click that same switch or panel again and it locks the door (and shuts it if it was open).

`HSUnlockSettings.json` next to this mod (a copy in the user data folder `HSUnlock` overrides it):

```json
{ "OpenOnUnlock": false }
```

Set `OpenOnUnlock` to `true` if you also want that door to swing open. The host's file is the one that counts. Buttons on doors that are not locked still open and close them.

Locked doors that had no lockpick can be picked with a lock pick, same timer and break chance as a chest. Trader doors are left alone.

## Craft and wire

These blocks craft at the workbench for the same cost as the vanilla switch, show in the electrical creative tab, and take a wire:

- powerSwitch01, powerSwitch02
- pushButtonSwitch01, pushButtonSwitch02
- pipe valve switches (all colours)

Place one you crafted or spawned, wire it like a switch, then press it. A copy that is still part of a POI keeps its door trigger. Key racks and decorative control-panel meshes are left alone.
