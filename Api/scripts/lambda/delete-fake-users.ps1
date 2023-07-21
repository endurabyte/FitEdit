function Usage {
    Write-Host ""
    Write-Host "DESCRIPTION:"
    Write-Host "  Deletes all take users in Cognito user pool."
    Write-Host ""
    Write-Host "USAGE:"
    Write-Host "  .\delete-fake-users.ps1 <COGNITO_USER_POOL_ID>"
}

$userPoolId = $env:FITEDIT_USER_POOL_ID
if (-not $userPoolId) {
    Write-Host "Please set the FITEDIT_USER_POOL_ID environment variable, e.g. us-east-1_abCD1FGhi"
    Exit 1
}

# Define name pattern for deletion
$namePattern = "FakeUser*"

# safety check
Write-Host "WARNING: This will delete ALL users whose name starts matches $namePattern"
$response = Read-Host "Type any key to continue, CTRL+C to cancel"

# fetch list of all users
$cognitoUsers = aws cognito-idp list-users --user-pool-id $userPoolId | ConvertFrom-Json | ForEach-Object { $_.Users }

# filter users by name pattern
$matchingUsers = $cognitoUsers | Where-Object { 
    $name = $_.Attributes | Where-Object { $_.Name -eq 'name' } | ForEach-Object { $_.Value }
    $name -like $namePattern
}

# print each user and email, then delete
foreach ($user in $matchingUsers) {
    # Get the email attribute
    $email = $user.Attributes | Where-Object { $_.Name -eq 'email' } | ForEach-Object { $_.Value }

    # Print user details
    Write-Host "Username: $($user.Username)"
    Write-Host "Email: $email"

    # Delete the user
    aws cognito-idp admin-delete-user --user-pool-id $userPoolId --username $user.Username

    Write-Host "User $($user.Username) has been deleted."
}

Write-Host "Finished."
