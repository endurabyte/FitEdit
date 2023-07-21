function Usage {
    Write-Host ""
    Write-Host "DESCRIPTION:"
    Write-Host "  Deletes all take users in Cognito user pool."
    Write-Host ""
    Write-Host "USAGE:"
    Write-Host "  .\delete-fake-users.ps1 <COGNITO_USER_POOL_ID>"
}

# make sure user pool ID is given as parameter
if ($args.Length -eq 0) {
    Write-Host "Please provide a user pool ID"
    Usage
    Exit 1
}

# safety check that we're sure we don't delete all users
# in a cognito pool we didn't mean to specify
Write-Host "WARNING: This will delete ALL fake users in Cognito User Pool: $($args[0])"
Write-Host "ARE YOU SURE?"
$response = Read-Host "Type YES to continue"

# check user response
if ($response.ToLower() -ne "yes") {
    Write-Host "Operation cancelled..."
    Exit 1
}

# Define name pattern for deletion
$namePattern = "FakeUser*"

# fetch list of all users
$cognitoUsers = aws cognito-idp list-users --user-pool-id $args[0] | ConvertFrom-Json | ForEach-Object { $_.Users }

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
    aws cognito-idp admin-delete-user --user-pool-id $args[0] --username $user.Username

    Write-Host "User $($user.Username) has been deleted."
}

Write-Host "Finished."
