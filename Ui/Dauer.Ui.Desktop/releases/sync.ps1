write-host "Syncing with S3..."
pushd $PSScriptRoot
aws s3 sync . "s3://fitedit-releases" --exclude '*.ps1' --exclude '.git/*' --exclude '*.app/**' --exclude ".gitignore"

write-host "Deployment completed"
popd
