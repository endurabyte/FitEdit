param (
    [string]$version = "1.0.0",
    [string]$sync = $false
)

$framework = "net7.0"
$configuration = "Release"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$signAppId = "Developer ID Application: Carl Slater ($env:FITEDIT_APPLE_TEAM_ID)"
$signInstallId = "Developer ID Installer: Carl Slater ($env:FITEDIT_APPLE_TEAM_ID)"
$notaryProfile = "FitEdit macOS"
$appCertPath = "app.p12"
$installCertPath = "installer.p12"

pushd $PSScriptRoot

# Convert certs from base64 env vars to p12 files and import into kechain
# The base64 env vars are set in the GitHub actions secrets but they are too long to fit
# on Windows where the limit is 4096 chars. So we split them into two env vars and concatenate.
$appCert_base64 = $env:FITEDIT_MACOS_APP_CERT_P12_1 + $env:FITEDIT_MACOS_APP_CERT_P12_2
$installCert_base64 = $env:FITEDIT_MACOS_INSTALL_CERT_P12_1 + $env:FITEDIT_MACOS_INSTALL_CERT_P12_2

$appCertPassword = $env:FITEDIT_MACOS_APP_CERT_P12_PW
$installCertPassword = $env:FITEDIT_MACOS_INSTALL_CERT_P12_PW

echo "Creating $appCertPath..."
& ./Decode-FromBase64.ps1 $appCert_base64 $appCertPath

echo "Creating $installCertPath..."
& ./Decode-FromBase64.ps1 $installCert_base64 $installCertPath

echo "Importing $appCertPath into login keychain..."
Invoke-Expression -Command "security import $appCertPath -k ~/Library/Keychains/login.keychain-db -P $appCertPassword -T /usr/bin/codesign"

echo "Importing $installCertPath into login keychain..."
Invoke-Expression -Command "security import $installCertPath -k ~/Library/Keychains/login.keychain-db -P $installCertPassword -T /usr/bin/codesign"

Remove-Item -Path $appCertPath
Remove-Item -Path $installCertPath

xcrun notarytool store-credentials $notaryProfile --apple-id $env:FITEDIT_APPLE_DEVELOPER_ID --password $env:FITEDIT_APPLE_APP_SPECIFIC_PASSWORD --team-id $env:FITEDIT_APPLE_TEAM_ID

dotnet tool install -g csq --prerelease

# Build for Intel
$rid = "osx-x64"

dotnet publish Dauer.Ui.Desktop.csproj --configuration $configuration --runtime $rid --framework $framework --output "./bin/$configuration/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile --noDelta

# Build for ARM / Apple Silicon
$rid = "osx-arm64"

dotnet publish Dauer.Ui.Desktop.csproj --configuration $configuration --runtime $rid --framework $framework --output "./bin/$configuration/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile --noDelta

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
