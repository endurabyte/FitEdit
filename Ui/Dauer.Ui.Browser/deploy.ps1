param (
  [string]$configuration,
  [string]$targetFramework
)

$distributionId = "E3FTX8LWBZEGE7"

function Log {
    param (
        [string]$Message
    )

    $scriptName = (Split-Path -Leaf $MyInvocation.ScriptName)
    Write-Host "[$scriptName] $Message"
}

pushd $PSScriptRoot

Log "Securing assemblies..."
. "C:\Program Files (x86)\Eziriz\.NET Reactor\dotNET_Reactor.Console.exe" -project .\Dauer.Ui.Browser.nrproj -nodialog

Log "Syncing with S3..."
pushd $PSScriptRoot\bin\$configuration\$targetFramework\browser-wasm\AppBundle
aws s3 sync . s3://app.fitedit.io
popd

Log "Invalidating Cloudfront caches..."
$createInvalidationResult = aws cloudfront create-invalidation --distribution-id $distributionId --paths "/*" | ConvertFrom-Json
$invalidationId = $createInvalidationResult.Invalidation.Id
Log "Invalidation created with ID: $invalidationId"

# Monitor the invalidation status
$invalidationStatus = $createInvalidationResult.Invalidation.Status
while ($invalidationStatus -ne "Completed") {
    $getInvalidationResult = aws cloudfront get-invalidation --distribution-id $distributionId --id $invalidationId | ConvertFrom-Json
    $invalidationStatus = $getInvalidationResult.Invalidation.Status
    Log "Invalidation status: $invalidationStatus"
    Start-Sleep -Seconds 2
}

Log "Invalidation completed"
Log "Deployment completed"
popd
