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

$jobs = @()
$rids = @("win-x64", "win-arm64")

# dotnet does not support publishing multiple RIDs in parallel
# Attempting to results in either
#   project.assets.json doesn't have a target for +'net8.0-macos/osx-x64'
# or
#   project.assets.json doesn't have a target for +'net8.0-macos/osx-arm64'
# depending on which built first
#
#  https://github.com/dotnet/sdk/issues/9363
foreach ($rid in $rids) {

  echo "Publishing $rid..."
  dotnet publish FitEdit.Ui.Desktop.csproj `
    --configuration $configuration `
    --runtime $rid `
    --framework $framework `
    --output "./bin/$configuration/$framework/publish/$rid/" `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false

  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}

foreach ($rid in $rids) {
  echo "Packing $rid..."
  $job = Start-ThreadJob {
    # Create thread-local copies of these variables
    $packId = $using:packId
    $authors = $using:authors
    $version = $using:version
    $configuration = $using:configuration
    $framework = $using:framework
    $rid = $using:rid

    & {
      vpk pack -y `
        --packId $packId `
        --packTitle "FitEdit" `
        --packAuthors $authors `
        --packVersion $version `
        --packDir "./bin/$configuration/$framework/publish/$rid/" `
        --icon "../FitEdit.Ui/Assets/logo.ico" `
        --mainExe "FitEdit.exe" `
        --outputDir "./Releases/$rid" `
        --delta None `
        --signParams="/f `"$certTmpPath`" /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com /csp `"$csp`" /k $certKey"

      # Redirect all output (including stdout and stderr) to the main thread
      # so it appears in the console and in CI build logs
    } *>&1 | ForEach-Object { "[$rid] $_" } | Out-Host

  } -StreamingHost $Host

  # Add the job to the list
  $jobs += $job
}

# Wait for all jobs to complete
Wait-Job -Job $jobs

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
