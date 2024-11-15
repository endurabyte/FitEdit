param (
  [Parameter(mandatory)][string]$version,
  [string]$sync = $false
)

$framework = "net8.0"
$configuration = "Release"
$rid = "win-x64"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$certTmpPath = "cert.tmp.crt"
$certKey = $env:ENDURABYTE_WINDOWS_CODE_SIGN_KEY

pushd $PSScriptRoot

& ./Decode-FromBase64.ps1 $env:ENDURABYTE_WINDOWS_CODE_SIGN_CERTIFICATE $certTmpPath
$certTmpPath = "$PSScriptRoot/$certTmpPath"
echo "Created $certTmpPath..."

echo "Publishing..."
dotnet publish FitEdit.Ui.Desktop.csproj --configuration $configuration --runtime $rid --framework $framework --output "./bin/Release/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo "Packing..."
#dotnet tool install -g csq --prerelease
csq pack --xplat=win --packId "FitEdit" --packAuthors $authors --packVersion $version --packDirectory "./bin/Release/$framework/publish/$rid" --icon "../FitEdit.Ui/Assets/logo.ico" --mainExe "FitEdit.exe" --releaseDir "./releases/$rid" --signParams="/f `"$certTmpPath`" /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /csp `"SafeNet Smart Card Key Storage Provider`" /k $certKey" --noDelta

Remove-Item -Path $certTmpPath

$doSync = [System.Boolean]::Parse($sync)
if ($doSync -ne $true) {
    popd
    return
}

# Sync with s3
echo "Deploying..."
pushd
cd releases
& .\sync.ps1
popd
popd
