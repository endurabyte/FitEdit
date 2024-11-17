param (
    [string]$tmpKeychainPassword,
    [string]$tmpKeychainName,
    [string]$notaryProfile
)

$appCertPath = "app.p12"
$installCertPath = "installer.p12"

# Convert certs from base64 env vars to p12 files and import into kechain
# The base64 env vars are set in the GitHub actions secrets but they are too long to fit
# on Windows where the limit is 4096 chars. So we split them into two env vars and concatenate.
$appCert_base64 = $env:FITEDIT_MACOS_APP_CERT_P12_1 + $env:FITEDIT_MACOS_APP_CERT_P12_2
$installCert_base64 = $env:FITEDIT_MACOS_INSTALL_CERT_P12_1 + $env:FITEDIT_MACOS_INSTALL_CERT_P12_2

$appCertPassword = $env:FITEDIT_MACOS_APP_CERT_P12_PW
$installCertPassword = $env:FITEDIT_MACOS_INSTALL_CERT_P12_PW

echo "Creating $appCertPath..."
& ./Decode-FromBase64.ps1 $appCert_base64 $appCertPath

echo "Creating $installCertPath..."
& ./Decode-FromBase64.ps1 $installCert_base64 $installCertPath

echo "Creating temporary keychain..."
iex -Command "security create-keychain -p $tmpKeychainPassword $tmpKeychainName"
echo "Setting default keychain..."
iex -Command "security default-keychain -s $tmpKeychainName"
echo "Appending temporary keychain to login keychain..."
iex -Command "security list-keychains -d user -s $tmpKeychainName ~/Library/Keychains/login.keychain-db"

echo "Importing $appCertPath into keychain..."
iex -Command "security import $appCertPath -k $tmpKeychainName -P $appCertPassword -A -T /usr/bin/codesign -T /usr/bin/productsign"
Remove-Item -Path $appCertPath

echo "Importing $installCertPath into keychain..."
iex -Command "security import $installCertPath -k $tmpKeychainName -P $installCertPassword -A -T /usr/bin/codesign -T /usr/bin/productsign"
Remove-Item -Path $installCertPath

echo "Enabling code-signing from a non-interactive shell..."
iex -Command "security set-key-partition-list -S apple-tool:,apple:, -s -k $tmpKeychainPassword -t private $tmpKeychainName"

echo "Storing notary profile..."
iex -Command "xcrun notarytool store-credentials $notaryProfile --apple-id $env:FITEDIT_APPLE_DEVELOPER_ID --password $env:FITEDIT_APPLE_APP_SPECIFIC_PASSWORD --team-id $env:FITEDIT_APPLE_TEAM_ID"
