$apiKey = $env:SENDGRID_API_KEY

$url = "https://api.sendgrid.com/v3/marketing/contacts/search/emails"
$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type" = "application/json"
}

$emails = $args

$body = @{
    "emails" = $emails
} | ConvertTo-Json

$response = Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body $body

Write-Output $response | ConvertTo-Json -Depth 100

