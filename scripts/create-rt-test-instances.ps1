# Creates two test instances for LOAD MOD runtime evidence (does NOT touch "test fabric").
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Join-Path $env:APPDATA 'Vazer\Minecraft\instances'

function New-TestInstance([string]$id, [string]$name) {
  $dir = Join-Path $root $id
  if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
  New-Item -ItemType Directory -Force (Join-Path $dir 'mods') | Out-Null
  $meta = @{
    id = $id
    name = $name
    minecraftVersion = '1.21.1'
    loaderType = 'fabric'
    loaderVersion = '0.19.5'
    path = $dir
    iconPath = $null
    createdAt = [DateTimeOffset]::UtcNow.ToString('o')
    lastPlayedAt = $null
  }
  [IO.File]::WriteAllText((Join-Path $dir 'instance.json'), ($meta | ConvertTo-Json))
  return $dir
}

# Build a jar from a staging directory (entry paths preserved relative).
function Write-Jar([string]$dir, [string]$name, [hashtable]$entries) {
  $stage = Join-Path ([IO.Path]::GetTempPath()) ('vazer-jar-' + [Guid]::NewGuid().ToString('N'))
  foreach ($k in $entries.Keys) {
    $entryPath = Join-Path $stage $k
    $entryDir = Split-Path $entryPath -Parent
    if (-not (Test-Path $entryDir)) { New-Item -ItemType Directory -Force $entryDir | Out-Null }
    [IO.File]::WriteAllText($entryPath, $entries[$k])
  }
  $zipPath = Join-Path (Join-Path $dir 'mods') $name
  [IO.Compression.ZipFile]::CreateFromDirectory($stage, $zipPath)
  Remove-Item -Recurse -Force $stage
}

$fabJson = '{"schemaVersion":1,"id":"rt_valid_fab","version":"9.9.9","name":"RT Valid Fabric Mod","description":"Fabric jar with metadata for runtime test","authors":[{"name":"Vazer RT"}],"depends":{"fabricloader":">=0.14.0","minecraft":"~1.21.1"}}'
$forgeToml = "modLoader=`"javafml`"`nloaderVersion=`"[47,)`"`nlicense=`"MIT`"`n[[mods]]`nmodId=`"rt_forge_jar`"`nversion=`"1.2.3`"`ndisplayName=`"RT Forge Jar`"`nauthors=`"Vazer, Tester`"`ndescription=`"Forge jar for loader-mismatch runtime test.`""
$bulkTemplate = '{"schemaVersion":1,"id":"rt_bulk_IDX","version":"1.0.X","name":"RT Bulk Mod IDX","authors":["Vazer RT"]}'

# Instance 1: mixed valid / loader-mismatch forge / corrupt / no metadata
$d1 = New-TestInstance 'rt-mods-test' 'Runtime Mods Test'
Write-Jar $d1 'rt-valid-fabric-9.9.9.jar' @{ 'fabric.mod.json' = $fabJson }
Write-Jar $d1 'rt-forge-mismatch-1.2.3.jar' @{ 'META-INF/mods.toml' = $forgeToml }
Write-Jar $d1 'rt-empty-no-metadata.jar' @{ 'some/random/file.txt' = 'no mod metadata here' }
$corrupt = Join-Path (Join-Path $d1 'mods') 'rt-corrupt-broken.jar'
[IO.File]::WriteAllBytes($corrupt, [byte[]](1..120 | ForEach-Object { [byte]($_ % 255) }))

# Instance 2: 520 valid fabric jars for the 500+ list scenario
$d2 = New-TestInstance 'rt-mods-500' 'Runtime Mods 500'
for ($i = 1; $i -le 520; $i++) {
  $idx = '{0:D3}' -f $i
  $json = $bulkTemplate.Replace('IDX', $idx).Replace('1.0.X', ('1.0.' + $idx))
  Write-Jar $d2 ('rt-bulk-' + $idx + '-1.0.jar') @{ 'fabric.mod.json' = $json }
}

(Get-ChildItem (Join-Path $d1 'mods')).Count
(Get-ChildItem (Join-Path $d2 'mods')).Count
