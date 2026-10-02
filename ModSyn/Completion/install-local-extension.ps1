$ErrorActionPreference = "Stop"

$extensionSource = Split-Path -Parent $MyInvocation.MyCommand.Path
$package = Get-Content (Join-Path $extensionSource "package.json") -Raw | ConvertFrom-Json
$stagingRoot = Join-Path $env:TEMP ("modsyn-extension-" + [Guid]::NewGuid().ToString("N"))
$extensionRoot = Join-Path $stagingRoot "extension"
$archivePath = Join-Path $env:TEMP ("modsyn-language-" + [Guid]::NewGuid().ToString("N") + ".zip")
$vsixPath = [System.IO.Path]::ChangeExtension($archivePath, ".vsix")

New-Item -ItemType Directory -Path $extensionRoot -Force | Out-Null
foreach ($fileName in @("package.json", "extension.js", "modsyn-completion-core.js", "modsyn-language.json", "language-configuration.json")) {
    Copy-Item (Join-Path $extensionSource $fileName) $extensionRoot
}

$contentTypes = @'
<?xml version="1.0" encoding="utf-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="json" ContentType="application/json" />
  <Default Extension="js" ContentType="application/javascript" />
  <Default Extension="xml" ContentType="text/xml" />
  <Override PartName="/extension.vsixmanifest" ContentType="text/xml" />
</Types>
'@
$vsixManifest = @'
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Language="en-US" Id="modsyn-language" Version="0.1.0" Publisher="local-modsyn" />
    <DisplayName>Modsyn Language Support</DisplayName>
    <Description>Strict completion and unknown-key diagnostics for Modsyn files.</Description>
    <Tags>modsyn;language</Tags>
    <Categories>Programming Languages</Categories>
    <Properties>
      <Property Id="Microsoft.VisualStudio.Code.Engine" Value="^1.85.0" />
      <Property Id="Microsoft.VisualStudio.Code.ExtensionKind" Value="workspace" />
    </Properties>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Code" />
  </Installation>
  <Dependencies />
  <Assets>
    <Asset Type="Microsoft.VisualStudio.Code.Manifest" Path="extension/package.json" Addressable="true" />
  </Assets>
</PackageManifest>
'@

[System.IO.File]::WriteAllText((Join-Path $stagingRoot "[Content_Types].xml"), $contentTypes)
[System.IO.File]::WriteAllText((Join-Path $stagingRoot "extension.vsixmanifest"), $vsixManifest)
Compress-Archive -Path @(
    (Join-Path $stagingRoot "[Content_Types].xml"),
    (Join-Path $stagingRoot "extension.vsixmanifest"),
    $extensionRoot
) -DestinationPath $archivePath
Move-Item $archivePath $vsixPath

& code --install-extension $vsixPath --force
if ($LASTEXITCODE -ne 0) {
    throw "VS Code failed to install the local Modsyn extension."
}

Write-Output "Installed Modsyn Language Support. Reload the VS Code window to activate it."