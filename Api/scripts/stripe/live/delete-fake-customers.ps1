# Set the name variabl
# Script for deleting Stripe customers based on name prefix

# The name prefix to check
$NamePrefix = "FakeUser"

# Get list of all customer IDs
$customerIDs = (stripe customers list --live | ConvertFrom-Json).data.id
$apiKey = $args[0]

# Loop over each customer ID
foreach ($id in $customerIDs) {
    # Retrieve individual customer data
    $customer = stripe customers retrieve --live $id | ConvertFrom-Json

    # Check if the name exists and starts with the given prefix
    if (($null -ne $customer.name) -and $customer.name.StartsWith($NamePrefix)) {
        # If the name starts with the prefix, delete the customer
        Write-Output "Deleting customer $id"
        stripe customers delete $id --confirm --live --api-key $apiKey
    }
}
