# Set the name variabl
# Script for deleting Stripe customers based on name prefix

# The name prefix to check
$NamePrefix = "FakeUser"

# Get list of all customer IDs
$apiKey = $args[0]

if (-not $apiKey) {
    Write-Error "Plese provide a live secret key"
    Exit 1;
}

$customerIDs = (stripe customers list --limit 1000 --live --api-key $apiKey | ConvertFrom-Json).data.id

# Loop over each customer ID
foreach ($id in $customerIDs) {
    # Retrieve individual customer data
    $customer = stripe customers retrieve --live $id --api-key $apiKey | ConvertFrom-Json

    # Check if the name exists and starts with the given prefix
    if (($null -ne $customer.name) -and $customer.name.StartsWith($NamePrefix)) {
        # If the name starts with the prefix, delete the customer
        Write-Output "Deleting customer $id"
        stripe customers delete $id --confirm --live --api-key $apiKey
    }
}
