param (
    [string]$version = "1.0.0",
    [bool]$sync = $false
)

$framework = "net7.0"
$configuration = "Release"
$rid = "win-x64"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$cert = "fitedit-selfSigned"

pushd $PSScriptRoot

dotnet tool install -g csq --prerelease

dotnet publish Dauer.Ui.Desktop.csproj --configuration Release --runtime $rid --framework $framework --output "./bin/Release/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=win --packId "FitEdit" --packAuthors $authors --packVersion $version --packDirectory "./bin/Release/$framework/publish/$rid" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit.exe" --releaseDir "./releases/$rid" --signParams="/n $cert /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com"

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
