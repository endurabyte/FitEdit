param (
  [Parameter(mandatory)][string]$version,
  [string]$sync = $false
)

$projectFile = "FitEdit.Ui.Desktop.csproj"
$configuration = "Release"
$framework = "net8.0"
$authors = "EnduraByte LLC"
$packId = "FitEdit"

pushd $PSScriptRoot

& ./Install-Velopack.ps1

$jobs = @()
$rids = @("linux-x64", "linux-arm64")

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
  dotnet publish $projectFile `
      --configuration $configuration `
      --runtime $rid `
      --framework $framework `
      --output "./bin/$configuration/$framework/publish/$rid/" `
      --self-contained true

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
      vpk [linux] pack -y `
              --packId $packId `
              --packTitle "FitEdit" `
              --packAuthors $authors `
              --packVersion $version `
              --packDir "./bin/$configuration/$framework/publish/$rid/" `
              --icon "../../Assets/FE.png" `
              --mainExe "FitEdit" `
              --outputDir "./Releases/$rid" `
              --delta None `
              --categories "Utility"

      # Redirect all output (including stdout and stderr) to the main thread
      # so it appears in the console and in CI build logs
    } *>&1 | ForEach-Object { "[$rid] $_" } | Out-Host

  } -StreamingHost $Host

  # Add the job to the list
  $jobs += $job
}

# Wait for all jobs to complete
Wait-Job -Job $jobs

$doSync = [System.Boolean]::Parse($sync)
if ($doSync -ne $true) {
    popd
    exit $LASTEXITCODE
}

# Sync with s3
pushd
cd releases
& .\sync.ps1
popd
popd

exit $LASTEXITCODE
