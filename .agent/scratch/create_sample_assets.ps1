# PowerShell script to create 9 WeaponItemData assets and 9 GemItemData assets in Assets/Resources/Data/

$weaponDir = "c:\Users\anyes\Gotblin2\Assets\Resources\Data\Weapons"
$gemDir = "c:\Users\anyes\Gotblin2\Assets\Resources\Data\Gems"

if (-not (Test-Path $weaponDir)) { New-Item -ItemType Directory -Path $weaponDir -Force }
if (-not (Test-Path $gemDir)) { New-Item -ItemType Directory -Path $gemDir -Force }

$weaponScriptGuid = "37ac19c60858c134baed394e5817140e"
$gemScriptGuid = "51efac4d3f51d9340b39ac80dc3e6442"

# Helper function to generate Weapon YAML
function Create-WeaponYaml ($id, $name, $type, $grade, $baseAtk, $succ, $keep, $iron, $nextGuid, $optionsYaml) {
    return @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $weaponScriptGuid, type: 3}
  m_Name: $id
  m_EditorClassIdentifier: 
  weaponID: $id
  weaponName: "$name"
  weaponType: "$type"
  grade: $grade
  visualSprite: {fileID: 0}
  iconSprite: {fileID: 0}
  baseATK: $baseAtk
  additionalOptions:
$optionsYaml
  upgradeSuccessRate: $succ
  upgradeKeepRate: $keep
  requiredIronIngot: $iron
  nextGradeWeapon: $nextGuid
"@
}

# Helper function to generate Gem YAML
function Create-GemYaml ($id, $name, $level, $sellPrice, $succ, $keep, $nextGuid) {
    return @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $gemScriptGuid, type: 3}
  m_Name: $id
  m_EditorClassIdentifier: 
  gemID: $id
  gemName: "$name"
  level: $level
  iconSprite: {fileID: 0}
  sellPrice: $sellPrice
  upgradeSuccessRate: $succ
  upgradeKeepRate: $keep
  nextLevelGem: $nextGuid
"@
}

function Create-MetaYaml ($guid) {
    return @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
}

# GUID Mappings
$guids = @{
    "dagger_1" = "a1111111111111111111111111111111";
    "dagger_2" = "a2222222222222222222222222222222";
    "dagger_3" = "a3333333333333333333333333333333";
    "sword_1"  = "b1111111111111111111111111111111";
    "sword_2"  = "b2222222222222222222222222222222";
    "sword_3"  = "b3333333333333333333333333333333";
    "axe_1"    = "c1111111111111111111111111111111";
    "axe_2"    = "c2222222222222222222222222222222";
    "axe_3"    = "c3333333333333333333333333333333";
    "ruby_1"   = "d1111111111111111111111111111111";
    "ruby_2"   = "d2222222222222222222222222222222";
    "ruby_3"   = "d3333333333333333333333333333333";
    "emerald_1"= "e1111111111111111111111111111111";
    "emerald_2"= "e2222222222222222222222222222222";
    "emerald_3"= "e3333333333333333333333333333333";
    "diamond_1"= "f1111111111111111111111111111111";
    "diamond_2"= "f2222222222222222222222222222222";
    "diamond_3"= "f3333333333333333333333333333333";
}

# 1. Daggers
$optD1 = "  - optionType: 2`n    value: 0.2`n  - optionType: 4`n    value: 10"
$optD2 = "  - optionType: 2`n    value: 0.5`n  - optionType: 4`n    value: 25"
$optD3 = "  - optionType: 2`n    value: 1.2`n  - optionType: 4`n    value: 60"

Set-Content "$weaponDir\Dagger_Lv1.asset" (Create-WeaponYaml "dagger_1" "수습 단검" "단검" 1 15 0.70 0.20 5 "{fileID: 11400000, guid: $($guids['dagger_2']), type: 2}" $optD1)
Set-Content "$weaponDir\Dagger_Lv1.asset.meta" (Create-MetaYaml $guids["dagger_1"])

Set-Content "$weaponDir\Dagger_Lv2.asset" (Create-WeaponYaml "dagger_2" "은빛 단검" "단검" 2 35 0.50 0.30 15 "{fileID: 11400000, guid: $($guids['dagger_3']), type: 2}" $optD2)
Set-Content "$weaponDir\Dagger_Lv2.asset.meta" (Create-MetaYaml $guids["dagger_2"])

Set-Content "$weaponDir\Dagger_Lv3.asset" (Create-WeaponYaml "dagger_3" "그림자 단검" "단검" 3 90 0.30 0.40 45 "{fileID: 0}" $optD3)
Set-Content "$weaponDir\Dagger_Lv3.asset.meta" (Create-MetaYaml $guids["dagger_3"])

# 2. Swords
$optS1 = "  - optionType: 0`n    value: 0.1"
$optS2 = "  - optionType: 0`n    value: 0.25`n  - optionType: 1`n    value: 1"
$optS3 = "  - optionType: 0`n    value: 0.6`n  - optionType: 1`n    value: 2"

Set-Content "$weaponDir\Sword_Lv1.asset" (Create-WeaponYaml "sword_1" "강철검 1단계" "양손검" 1 30 0.65 0.25 8 "{fileID: 11400000, guid: $($guids['sword_2']), type: 2}" $optS1)
Set-Content "$weaponDir\Sword_Lv1.asset.meta" (Create-MetaYaml $guids["sword_1"])

Set-Content "$weaponDir\Sword_Lv2.asset" (Create-WeaponYaml "sword_2" "명검 2단계" "양손검" 2 70 0.45 0.35 20 "{fileID: 11400000, guid: $($guids['sword_3']), type: 2}" $optS2)
Set-Content "$weaponDir\Sword_Lv2.asset.meta" (Create-MetaYaml $guids["sword_2"])

Set-Content "$weaponDir\Sword_Lv3.asset" (Create-WeaponYaml "sword_3" "용살검 3단계" "양손검" 3 160 0.25 0.45 60 "{fileID: 0}" $optS3)
Set-Content "$weaponDir\Sword_Lv3.asset.meta" (Create-MetaYaml $guids["sword_3"])

# 3. Axes
$optA1 = "  - optionType: 3`n    value: 0.1"
$optA2 = "  - optionType: 3`n    value: 0.25`n  - optionType: 1`n    value: 1"
$optA3 = "  - optionType: 3`n    value: 0.6`n  - optionType: 1`n    value: 2"

Set-Content "$weaponDir\Axe_Lv1.asset" (Create-WeaponYaml "axe_1" "벌목도끼" "전투도끼" 1 50 0.60 0.30 10 "{fileID: 11400000, guid: $($guids['axe_2']), type: 2}" $optA1)
Set-Content "$weaponDir\Axe_Lv1.asset.meta" (Create-MetaYaml $guids["axe_1"])

Set-Content "$weaponDir\Axe_Lv2.asset" (Create-WeaponYaml "axe_2" "전투도끼" "전투도끼" 2 120 0.40 0.40 25 "{fileID: 11400000, guid: $($guids['axe_3']), type: 2}" $optA2)
Set-Content "$weaponDir\Axe_Lv2.asset.meta" (Create-MetaYaml $guids["axe_2"])

Set-Content "$weaponDir\Axe_Lv3.asset" (Create-WeaponYaml "axe_3" "파괴의 광도끼" "전투도끼" 3 280 0.20 0.50 75 "{fileID: 0}" $optA3)
Set-Content "$weaponDir\Axe_Lv3.asset.meta" (Create-MetaYaml $guids["axe_3"])

# 4. Gems
Set-Content "$gemDir\Ruby_Lv1.asset" (Create-GemYaml "ruby_1" "루비 1단계" 1 500 0.60 0.30 "{fileID: 11400000, guid: $($guids['ruby_2']), type: 2}")
Set-Content "$gemDir\Ruby_Lv1.asset.meta" (Create-MetaYaml $guids["ruby_1"])

Set-Content "$gemDir\Ruby_Lv2.asset" (Create-GemYaml "ruby_2" "루비 2단계" 2 1200 0.40 0.40 "{fileID: 11400000, guid: $($guids['ruby_3']), type: 2}")
Set-Content "$gemDir\Ruby_Lv2.asset.meta" (Create-MetaYaml $guids["ruby_2"])

Set-Content "$gemDir\Ruby_Lv3.asset" (Create-GemYaml "ruby_3" "빛나는 루비 3단계" 3 3000 0.20 0.50 "{fileID: 0}")
Set-Content "$gemDir\Ruby_Lv3.asset.meta" (Create-MetaYaml $guids["ruby_3"])

Set-Content "$gemDir\Emerald_Lv1.asset" (Create-GemYaml "emerald_1" "에메랄드 1단계" 1 800 0.55 0.30 "{fileID: 11400000, guid: $($guids['emerald_2']), type: 2}")
Set-Content "$gemDir\Emerald_Lv1.asset.meta" (Create-MetaYaml $guids["emerald_1"])

Set-Content "$gemDir\Emerald_Lv2.asset" (Create-GemYaml "emerald_2" "에메랄드 2단계" 2 2000 0.35 0.40 "{fileID: 11400000, guid: $($guids['emerald_3']), type: 2}")
Set-Content "$gemDir\Emerald_Lv2.asset.meta" (Create-MetaYaml $guids["emerald_2"])

Set-Content "$gemDir\Emerald_Lv3.asset" (Create-GemYaml "emerald_3" "영롱한 에메랄드 3단계" 3 5000 0.15 0.55 "{fileID: 0}")
Set-Content "$gemDir\Emerald_Lv3.asset.meta" (Create-MetaYaml $guids["emerald_3"])

Set-Content "$gemDir\Diamond_Lv1.asset" (Create-GemYaml "diamond_1" "다이아몬드 1단계" 1 2000 0.50 0.30 "{fileID: 11400000, guid: $($guids['diamond_2']), type: 2}")
Set-Content "$gemDir\Diamond_Lv1.asset.meta" (Create-MetaYaml $guids["diamond_1"])

Set-Content "$gemDir\Diamond_Lv2.asset" (Create-GemYaml "diamond_2" "다이아몬드 2단계" 2 5000 0.30 0.40 "{fileID: 11400000, guid: $($guids['diamond_3']), type: 2}")
Set-Content "$gemDir\Diamond_Lv2.asset.meta" (Create-MetaYaml $guids["diamond_2"])

Set-Content "$gemDir\Diamond_Lv3.asset" (Create-GemYaml "diamond_3" "찬란한 다이아몬드 3단계" 3 12000 0.10 0.60 "{fileID: 0}")
Set-Content "$gemDir\Diamond_Lv3.asset.meta" (Create-MetaYaml $guids["diamond_3"])

Write-Host "All 9 WeaponItemData and 9 GemItemData assets created successfully!"
