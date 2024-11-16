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
$signEntitlements = "Entitlements.entitlements"
$notaryProfile = "FitEdit"

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

dotnet restore
dotnet workload restore

& ./Install-Velopack.ps1

$jobs = @()
$rids = @("osx-x64", "osx-arm64")

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
    $signAppId = $using:signAppId
    $signInstallId = $using:signInstallId
    $signEntitlements = $using:signEntitlements
    $notaryProfile = $using:notaryProfile

    # remove all pdb files
    Get-ChildItem -Path "./bin/$configuration/$framework/publish/$rid/" -Filter *.pdb -Recurse | Remove-Item -Force
    
    & {
      # Notarize and create installer
      vpk pack -y `
        --packId $packId `
        --packTitle "FitEdit" `
        --packAuthors $authors `
        --packVersion $version `
        --packDir "./bin/$configuration/$framework/publish/$rid/" `
        --icon "../../Assets/FE.icns" `
        --mainExe "FitEdit" `
        --outputDir "./Releases/$rid" `
        --delta None `
        --signAppIdentity $signAppId `
        --signInstallIdentity $signInstallId `
        --signEntitlements $signEntitlements `
        --notaryProfile $notaryProfile `
        --verbose

      if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
      }

      # Redirect all output (including stdout and stderr) to the main thread
      # so it appears in the console and in CI build logs
    } *>&1 | ForEach-Object { "[$rid] $_" } | Out-Host

  } -StreamingHost $Host

  # Add the job to the list
  $jobs += $job
}

# Wait for all jobs to complete
Wait-Job -Job $jobs

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
