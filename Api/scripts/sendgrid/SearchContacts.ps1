if ($args.Count -eq 0) {
    Write-Output "Please provide an email string as an argument."
    return
}

$apiKey = $env:SENDGRID_API_KEY

$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type" = "application/json"
}

$url = "https://api.sendgrid.com/v3/marketing/contacts/search"

$emailString = $args[0].ToLower()
$query = "email LIKE '%$emailString%'"

$body = @{
    "query" = $query
} | ConvertTo-Json

$response = Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body $body

Write-Output $response | ConvertTo-Json -Depth 100
