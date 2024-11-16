param (
  [Parameter(mandatory)][string]$version,
  [string]$sync = $false
)

$framework = "net8.0"
$configuration = "Release"
$rid = "win-x64"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$csp = "SafeNet Smart Card Key Storage Provider"
$certTmpPath = "cert.tmp.crt"
$certKey = $env:ENDURABYTE_WINDOWS_CODE_SIGN_KEY

pushd $PSScriptRoot

& ./Install-Velopack.ps1
& ./Decode-FromBase64.ps1 $env:ENDURABYTE_WINDOWS_CODE_SIGN_CERTIFICATE $certTmpPath

$certTmpPath = "$PSScriptRoot/$certTmpPath"
echo "Created $certTmpPath..."

echo "Publishing..."
dotnet publish FitEdit.Ui.Desktop.csproj --configuration $configuration --runtime $rid --framework $framework --output "./bin/Release/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo "Packing..."

vpk pack -y `
  --packId $packId `
  --packTitle "FitEdit" `
  --packAuthors $authors `
  --packVersion $version `
  --packDir "./bin/$configuration/$framework/publish/$rid/" `
  --icon "../FitEdit.Ui/Assets/logo.ico" `
  --mainExe "FitEdit.exe" `
  --releaseDir "./Releases/$rid" `
  --delta None `
  --signParams="/f `"$certTmpPath`" /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /csp `"$csp`" /k $certKey"

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
