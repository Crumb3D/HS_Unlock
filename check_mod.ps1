# Pre-boot check. Exit 1 if anything is wrong.
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Game = "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die"
$fail = 0

function Fail($msg) {
    Write-Host "FAIL  $msg" -ForegroundColor Red
    $script:fail++
}
function Ok($msg) { Write-Host "ok    $msg" -ForegroundColor Green }

$vanillaBlocks = [regex]::Matches((Get-Content (Join-Path $Game "Data\Config\blocks.xml") -Raw), '<block name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
$vanillaItems = [regex]::Matches((Get-Content (Join-Path $Game "Data\Config\items.xml") -Raw), '<item name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
$known = @{}
foreach ($n in ($vanillaItems + $vanillaBlocks)) { $known[$n] = $true }

$blocksXml = Get-Content (Join-Path $Root "Config\blocks.xml") -Raw
$recipeXml = Get-Content (Join-Path $Root "Config\recipes.xml") -Raw
try { [xml]$blocksXml | Out-Null; Ok "blocks.xml" } catch { Fail "blocks.xml: $_" }
try { [xml]$recipeXml | Out-Null; Ok "recipes.xml" } catch { Fail "recipes.xml: $_" }

$names = @()
foreach ($n in ([regex]::Matches($recipeXml, '<recipe name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })) {
    $names += $n
    if (-not $known.ContainsKey($n)) { Fail "recipe for unknown block '$n'" } else { Ok "recipe $n" }
}
foreach ($n in ([regex]::Matches($recipeXml, '<ingredient name="([^"]+)"') | ForEach-Object { $_.Groups[1].Value })) {
    if (-not $known.ContainsKey($n)) { Fail "ingredient unknown '$n'" }
}
if ($names.Count -ne 28) { Fail "expected 28 recipes, found $($names.Count)" } else { Ok "28 recipes" }

foreach ($n in @("powerSwitch01","pushButtonSwitch01","pushButtonSwitch02","pipeSmallValveSwitch","pipeSmallWallValveSwitch","pipeSmallValve02Switch","pipeSmallWallValve02Switch")) {
    if ($blocksXml -notmatch [regex]::Escape("@name='$n'")) { Fail "blocks.xml missing $n" }
}
if ($blocksXml -notmatch "HSUnlockButton, HSUnlock") { Fail "class must be 'HSUnlockButton, HSUnlock' so Type.GetType finds the mod assembly" } else { Ok "class HSUnlockButton, HSUnlock" }

$dll = Join-Path $Root "HSUnlock.dll"
if (-not (Test-Path $dll)) { Fail "HSUnlock.dll missing" }
else {
    $ascii = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll))
    foreach ($t in @("HSUnlockMod", "BlockHSUnlockButton", "HSUnlockDoorPatch", "HSUnlockLockPick", "OpenOnUnlock", "TEFeatureDoor")) {
        if ($ascii.IndexOf($t) -lt 0) { Fail "DLL missing '$t'" } else { Ok "dll $t" }
    }
}

if ($fail -gt 0) {
    Write-Host ("{0} check(s) failed." -f $fail) -ForegroundColor Red
    exit 1
}
Write-Host "All checks passed." -ForegroundColor Green
exit 0
