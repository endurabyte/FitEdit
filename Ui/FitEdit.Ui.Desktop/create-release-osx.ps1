param (
  [Parameter(mandatory)][string]$version,
  [string]$sync = $false
)

$framework = "net8.0"
$configuration = "Release"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$signAppId = "Developer ID Application: Carl Slater ($env:FITEDIT_APPLE_TEAM_ID)"
$signInstallId = "Developer ID Installer: Carl Slater ($env:FITEDIT_APPLE_TEAM_ID)"
$notaryProfile = "FitEdit"
$appCertPath = "app.p12"
$installCertPath = "installer.p12"

$rid_x64 = "osx-x64"
$rid_arm64 = "osx-arm64"

$tmpKeychainPassword = "fitedit2023"
$tmpKeychainName = "fitedit.keychain"

pushd $PSScriptRoot

# Setup keychain
& ./Setup-Keychain.ps1 `
  -tmpKeychainPassword $tmpKeychainPassword `
  -tmpKeychainName $tmpKeychainName `
  -notaryProfile $notaryProfile

echo "Installing Clowd.Squirrel..."
dotnet tool install -g csq --prerelease

# Build for Intel

echo "Building $rid_x64..."
dotnet publish FitEdit.Ui.Desktop.csproj --configuration $configuration --runtime $rid_x64 --framework $framework --output "./bin/$configuration/$framework/publish/$rid_x64/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

# Build for ARM / Apple Silicon

echo "Building $rid_arm64..."
dotnet publish FitEdit.Ui.Desktop.csproj --configuration $configuration --runtime $rid_arm64 --framework $framework --output "./bin/$configuration/$framework/publish/$rid_arm64/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

# Code sign both builds in parallel
echo "Packing $rid_x64..."
$job1 = Start-Job -ScriptBlock {
    param($packId, $authors, $version, $configuration, $framework, $rid_x64, $signAppId, $signInstallId, $notaryProfile)
    csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid_x64" --icon "../FitEdit.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid_x64" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile --noDelta
} -ArgumentList $packId, $authors, $version, $configuration, $framework, $rid_x64, $signAppId, $signInstallId, $notaryProfile

echo "Packing $rid_arm64..."
$job2 = Start-Job -ScriptBlock {
    param($packId, $authors, $version, $configuration, $framework, $rid_arm64, $signAppId, $signInstallId, $notaryProfile)
    csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid_arm64" --icon "../FitEdit.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid_arm64" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile --noDelta
} -ArgumentList $packId, $authors, $version, $configuration, $framework, $rid_arm64, $signAppId, $signInstallId, $notaryProfile

# Wait for both jobs to finish
$job1 | Wait-Job
$output1 = $job1 | Receive-Job
$output1 | Write-Output

$job2 | Wait-Job
$output2 = $job2 | Receive-Job
$output2 | Write-Output

# Clean up
echo "Removing temporary keychain..."
iex -Command "security delete-keychain $tmpKeychainName"

echo "Restoring default keychain..."
iex -Command "security list-keychains -d user -s login.keychain"

# Upload artifacts

$doSync = [System.Boolean]::Parse($sync)
if ($doSync -ne $true) {
    popd
    return
}

# Sync with s3
pushd
cd releases
& .\sync.ps1
popd
popd
