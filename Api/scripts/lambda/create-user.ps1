<#
    .SYNOPSIS
    This script creates a new random user and signs them up using the Cognito
    SignUp API call.

    Note that the email address will automatically be set to a random alias of
    the base email address configuration to make testing easier. The phone number
    is generated automatically, but will not be used for verification.
#>

param (
    [Parameter(Mandatory=$true)][string]$cognitoUserPoolClientId,
    [Parameter(Mandatory=$true)][string]$baseEmailAddress,
    [Parameter(Mandatory=$false)][bool]$isReal
)

function Get-RandomNumber {
    param (
        [Parameter(Mandatory=$true)][int]$digits
    )
    return (Get-Random -Minimum ([math]::Pow(10, $digits - 1)) -Maximum ([math]::Pow(10, $digits) - 1))
}

# create an email alias from the base email address
$baseUser = $baseEmailAddress.Split("@")[0]
$baseDomain = $baseEmailAddress.Split("@")[1]
$alias = [System.Guid]::NewGuid().ToString().Substring(0,6).ToString()
$email = "$baseUser+" + $alias + "@$baseDomain"
if ($isReal -eq $true) {
    $email = $baseEmailAddress
}
$phoneNumber = "+1" + [Math]::Floor((Get-RandomNumber -digits 10))
$name = "FakeUser" + (Get-Random)
$password = [System.Guid]::NewGuid().ToString().Substring(0,20) + "ABC!"

Write-Host "Creating a user with the following details:"
Write-Host "Email=$email"
Write-Host "Phone=$phoneNumber"
Write-Host "Name=$name"
Write-Host "Password=$password"

$user_attributes = @( 
    @{Name="name";Value=$name}, 
    @{Name="phone_number";Value=$phoneNumber} 
) | ConvertTo-Json

Write-Host "User Attributes=$user_attributes"
Write-Host ""

# sign up a user
aws cognito-idp sign-up `
    --client-id $cognitoUserPoolClientId `
    --username $email `
    --password $password `
    --user-attributes $user_attributes 

Write-Host "Finished."
