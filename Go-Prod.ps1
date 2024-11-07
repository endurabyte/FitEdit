# Ensure the environment variables for source and target buckets are set
if (-not $env:FITEDIT_STAGE_S3_BUCKET -or -not $env:FITEDIT_RELEASES_S3_BUCKET) {
    Write-Host "Error: bucket environment variables are not set." -ForegroundColor Red
    exit 1
}

# Define source and target S3 bucket URIs
$sourceBucketUri = "s3://$env:FITEDIT_STAGE_S3_BUCKET"
$targetBucketUri = "s3://$env:FITEDIT_RELEASES_S3_BUCKET"

# Sync source bucket to target bucket
try {
    Write-Host "Starting sync from $sourceBucketUri to $targetBucketUri..."
    aws s3 sync $sourceBucketUri $targetBucketUri --delete
    Write-Host "Sync completed successfully." -ForegroundColor Green
} catch {
    Write-Host "Error during sync: $_" -ForegroundColor Red
    exit 1
}
