write-host "Syncing with S3..."
$bucket = $env:FITEDIT_RELEASES_S3_BUCKET

pushd $PSScriptRoot
aws s3 sync . "s3://$bucket" --exclude '*.ps1' --exclude '.git/*' --exclude '*.app/**' --exclude ".gitignore"

write-host "...Synced with S3"
popd
