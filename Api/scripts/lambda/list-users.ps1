$userPoolId = $env:FITEDIT_USER_POOL_ID
if (-not $userPoolId) {
    Write-Host "Please set the FITEDIT_USER_POOL_ID environment variable, e.g. us-east-1_abCD1FGhi"
    Exit 1
}

aws cognito-idp list-users --user-pool-id $userPoolId
