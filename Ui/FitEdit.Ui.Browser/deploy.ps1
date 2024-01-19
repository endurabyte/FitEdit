param (
  [string]$configuration,
  [string]$targetFramework,

  [Parameter(Mandatory=$false)]
  [bool]$upload = $false
)

$distributionId = "E3FTX8LWBZEGE7"

function Log {
    param (
        [string]$Message
    )

    $scriptName = (Split-Path -Leaf $MyInvocation.ScriptName)
    Write-Host "[$scriptName] $Message"
}

function Remove-FitEditAssemblies {
    param (
        [string]$Path
    )
    
    # Load the JSON file
    $json = Get-Content $Path | ConvertFrom-Json
   
    # Remove all entries starting with "FitEdit"
    $json.assets = $json.assets | Where-Object {
        $_.name -notmatch '^FitEdit'
    }

    # Save the JSON file
    $json | ConvertTo-Json -Depth 100 | Set-Content $Path
}

pushd $PSScriptRoot

Log "Securing assemblies..."
. "C:\Program Files (x86)\Eziriz\.NET Reactor\dotNET_Reactor.Console.exe" -project .\FitEdit.Ui.Browser.nrproj -nodialog

Log "Removing unsecured assemblies from AppBundle..."
pushd $PSScriptRoot\bin\$configuration\$targetFramework\browser-wasm\AppBundle
Remove-FitEditAssemblies -Path 'mono-config.json'
popd

pushd $PSScriptRoot\bin\$configuration\$targetFramework\browser-wasm\AppBundle\managed
rm FitEdit.*.dll
rm *.pdb
popd

if ($upload -eq $false) {
  return;
}

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
