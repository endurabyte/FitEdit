param (
    [string]$version = "1.0.0",
    [bool]$sync = $false
)

$framework = "net7.0"
$configuration = "Release"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$signAppId = "Developer ID Application: Carl Slater (D89E59Y3DZ)"
$signInstallId = "Developer ID Installer: Carl Slater (D89E59Y3DZ)"
$notaryProfile = "FitEdit macOS"

pushd $PSScriptRoot

dotnet tool install -g csq --prerelease

# Build for Intel
$rid = "osx-x64"

dotnet publish Dauer.Ui.Desktop.csproj --configuration $configuration --runtime $rid --framework $framework --output "./bin/$configuration/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile

# Build for ARM / Apple Silicon
$rid = "osx-arm64"

dotnet publish Dauer.Ui.Desktop.csproj --configuration $configuration --runtime $rid --framework $framework --output "./bin/$configuration/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=osx --packId $packId --packAuthors $authors --packVersion $version --packDir "./bin/$configuration/$framework/publish/$rid" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/$rid" --signAppIdentity=$signAppId --signInstallIdentity=$signInstallId --notaryProfile=$notaryProfile

if ($sync -ne $true) {
    popd
    return
}

# Sync with s3
pushd
cd releases
& .\sync.ps1
popd
popd
