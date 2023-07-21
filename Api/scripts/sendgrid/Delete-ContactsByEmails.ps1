$contactsData = & ".\Get-ContactsByEmails.ps1" @args

# Parse the response JSON to a PowerShell object
$contactsDataObj = $contactsData | ConvertFrom-Json

# Extract the IDs of the contacts
$contactIds = $contactsDataObj.result.PSObject.Properties | ForEach-Object {
    $_.Value.contact.id
}

# Make the DELETE request only if there are contact IDs
if ($contactIds -and $contactIds -and $contactIds.Count -gt 0) {
    $apiKey = $env:SENDGRID_API_KEY

    # Set the url and headers
    $url = "https://api.sendgrid.com/v3/marketing/contacts"
    $headers = @{
        "Authorization" = "Bearer $apiKey"
        "Content-Type" = "application/x-www-form-urlencoded"
    }

    # Join the contact IDs with a comma for the request body
    $idsString = $contactIds -join ", "

    $url = "https://api.sendgrid.com/v3/marketing/contacts?ids=$idsString"
    
    # Set the body
    $body = @{
        "ids" = $idsString
    }

    # Make the DELETE request
    Invoke-RestMethod -Method Delete -Uri $url -Headers $headers -Body $body
}
else {
    Write-Output "No contact IDs to delete."
}
