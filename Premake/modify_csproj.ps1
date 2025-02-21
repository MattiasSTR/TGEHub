param (
    [string]$csprojFile
)

# Load the XML
[xml]$xml = Get-Content -Path $csprojFile

# Ensure ApplicationIcon is set
$propertyGroup = $xml.Project.PropertyGroup | Where-Object { $_.EnableDefaultCompileItems -eq $false }
if ($propertyGroup -and -not $propertyGroup.ApplicationIcon) {
    $appIcon = $xml.CreateElement("ApplicationIcon")
    $appIcon.InnerText = "TGAIcon.ico"
    $propertyGroup.AppendChild($appIcon) | Out-Null
}

# Remove <None Include="TGAIcon.ico" />
$noneNodes = $xml.Project.ItemGroup.None | Where-Object { $_.Include -eq "TGAIcon.ico" }
foreach ($node in $noneNodes) {
    $node.ParentNode.RemoveChild($node) | Out-Null
}

# Ensure <Content Include="TGAIcon.ico" /> exists
$contentGroup = $xml.Project.ItemGroup | Where-Object { $_.Content -and $_.Content.Include -eq "TGAIcon.ico" }
if (-not $contentGroup) {
    $itemGroup = $xml.CreateElement("ItemGroup")
    $contentNode = $xml.CreateElement("Content")
    $contentNode.SetAttribute("Include", "TGAIcon.ico")
    $itemGroup.AppendChild($contentNode) | Out-Null
    $xml.Project.AppendChild($itemGroup) | Out-Null
}

# Ensure PreBuild Target exists before </Project>
$existingPreBuild = $xml.Project.Target | Where-Object { $_.Name -eq "PreBuild" }
if (-not $existingPreBuild) {
    $preBuildTarget = $xml.CreateElement("Target")
    $preBuildTarget.SetAttribute("Name", "PreBuild")
    $preBuildTarget.SetAttribute("BeforeTargets", "PreBuildEvent")

    $exec = $xml.CreateElement("Exec")
    $exec.SetAttribute("Command", 'xcopy "$(SolutionDir)Files" "$(TargetDir)\Files" /D /Y /I /S')
    
    $preBuildTarget.AppendChild($exec) | Out-Null
    $xml.Project.AppendChild($preBuildTarget) | Out-Null
}

# Save the modified XML
$xml.Save($csprojFile)