param (
  [Parameter(mandatory)][string]$version,
  [string]$sync = $false
)

$framework = "net7.0"
$configuration = "Release"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$signAppId = "Developer ID Application: Carl Slater ($env:FITEDIT_APPLE_TEAM_ID)"
$signInstallId = "Developer ID Installer: Carl Slater ($env:FITEDIT_APPLE_TEAM_ID)"
$notaryProfile = "'FitEdit macOS'"
$appCertPath = "app.p12"
$installCertPath = "installer.p12"

$rid_x64 = "osx-x64"
$rid_arm64 = "osx-arm64"

$tmpKeychainPassword = "fitedit2023"
$tmpKeychainName = "fiteditKeychain"

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

echo "Creating temporary keychain..."
security create-keychain -p $tmpKeychainPassword $tmpKeychainName
echo "Appending temporary keychain to login keychain..."
security list-keychains -d user -s $tmpKeychainName ~/Library/Keychains/login.keychain-db
echo "Removing relock timeout..."
security set-keychain-settings $tempKeychainName
echo "Unlocking temporary keychain..."
security unlock-keychain -p $tmpKeychainPassword $tmpKeychainName

echo "Importing $appCertPath into keychain..."
security import $appCertPath -k $tmpKeychainName -P $appCertPassword -A -T /usr/bin/codesign -T /usr/bin/productsign
Remove-Item -Path $appCertPath

echo "Importing $installCertPath into keychain..."
security import $installCertPath -k $tmpKeychainName -P $installCertPassword -A -T /usr/bin/codesign -T /usr/bin/productsign
Remove-Item -Path $installCertPath

echo "Enabling code-signing from a non-interactive shell..."
security set-key-partition-list -S apple-tool:,apple:, -s -k $tmpKeychainPassword  -t private $tmpKeychainName

echo "Storing notary profile..."
iex -Command "xcrun notarytool store-credentials $notaryProfile --apple-id $env:FITEDIT_APPLE_DEVELOPER_ID --password $env:FITEDIT_APPLE_APP_SPECIFIC_PASSWORD --team-id $env:FITEDIT_APPLE_TEAM_ID"

echo "Installing Clowd.Squirrel..."
dotnet tool install -g csq --prerelease

# Build for Intel

echo "Building $rid_x64..."
dotnet publish Dauer.Ui.Desktop.csproj --configuration $configuration --runtime $rid_x64 --framework $framework --output "./bin/$configuration/$framework/publish/$rid_x64/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo "Packing $rid_x64..."
csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid_x64" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid_x64" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile --noDelta

# Build for ARM / Apple Silicon

echo "Building $rid_arm64..."
dotnet publish Dauer.Ui.Desktop.csproj --configuration $configuration --runtime $rid_arm64 --framework $framework --output "./bin/$configuration/$framework/publish/$rid_arm64/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo "Packing $rid_arm64..."
csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid_arm64" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid_arm64" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile --noDelta

# Clean up
echo "Removing temporary keychain..."
security delete-keychain $tempKeychainName

echo "Restoring default keychain..."
security list-keychains -d user -s login.keychain

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
