$oldGuid = "8f586378b4e144a9851e7b34d9b748ee"
$newGuid = "e00828f873d75d246aa91b57c3aa1fca"

$oldMatFileId = "2180264"
$newMatFileId = "-6445253934987609687"

$files = Get-ChildItem -Path Assets -Recurse -Include *.unity, *.prefab

$updatedCount = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    if ($content.Contains($oldGuid) -or $content.Contains($oldMatFileId)) {
        # Replace fontAsset guid
        $newContent = $content.Replace($oldGuid, $newGuid)
        # Replace material fileID
        $newContent = $newContent.Replace("m_sharedMaterial: {fileID: $oldMatFileId, guid: $newGuid", "m_sharedMaterial: {fileID: $newMatFileId, guid: $newGuid")
        $newContent = $newContent.Replace("m_sharedMaterial: {fileID: $oldMatFileId, guid: $oldGuid", "m_sharedMaterial: {fileID: $newMatFileId, guid: $newGuid")
        
        [System.IO.File]::WriteAllText($file.FullName, $newContent, [System.Text.Encoding]::UTF8)
        Write-Host "Updated $($file.Name)"
        $updatedCount++
    }
}

Write-Host "Total files updated: $updatedCount"
