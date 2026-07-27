# PowerShell script to create Popup_Layer and PopupManager in SampleScene.unity

$scenePath = "c:\Users\anyes\Gotblin2\Assets\Scenes\SampleScene.unity"
$content = Get-Content $scenePath -Raw

# 1. Check if PopupManager already exists in Scene YAML
if (-not $content.Contains("PopupManager")) {
    Write-Host "Adding PopupManager and Popup_Layer structure to SampleScene.unity..."

    # Create YAML node for PopupManager GameObject & Component
    # ID range: 991000001 (PopupManager), 991000002 (Transform/RectTransform), 991000003 (PopupManager MonoBehaviour)
    # ID range: 991000004 (Popup_Layer GameObject), 991000005 (Popup_Layer RectTransform), 991000006 (CanvasRenderer)

    $popupManagerYaml = @"
--- !u!1 &991000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 991000002}
  - component: {fileID: 991000003}
  m_Layer: 0
  m_Name: PopupManager
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &991000002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 991000001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &991000003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 991000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 7859cfa12b489d24cb68019a7102c9a1, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  popupLayerParent: {fileID: 991000005}
--- !u!1 &991000004
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 991000005}
  - component: {fileID: 991000006}
  m_Layer: 5
  m_Name: Popup_Layer
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &991000005
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 991000004}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 915077649}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &991000006
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 991000004}
  m_CullTransparentMesh: 1
"@

    # Register Popup_Layer RectTransform (991000005) under Canvas (915077649) m_Children
    $pattern = "m_Children:`r?`n  - {fileID: 1511014179}"
    $replacement = "m_Children:`r`n  - {fileID: 1511014179}`r`n  - {fileID: 991000005}"
    $content = $content -replace $pattern, $replacement

    $content = $content + "`r`n" + $popupManagerYaml
    Set-Content $scenePath $content -NoNewline
    Write-Host "PopupManager & Popup_Layer added to SampleScene!"
}
