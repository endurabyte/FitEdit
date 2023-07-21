# Script for adding a test Stripe customer to simulate checkout

# Test customer details
$TestCustomerName = "FakeUser"
$TestCustomerEmail = $args[1]

if (-not $TestCustomerEmail) {
    $TestCustomerEmail = "support@fitedit.io"
}

# Create the test customer
Write-Output "Creating test customer named $TestCustomerName"
$testCustomer = stripe customers create --name $TestCustomerName --email $TestCustomerEmail | ConvertFrom-Json

if ($null -ne $testCustomer.id) {
    Write-Output "Successfully created customer with ID: $($testCustomer.id)"
} else {
    Write-Error "Failed to create the test customer."
}
