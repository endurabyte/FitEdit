# How to sign code

# 1. Generate CSRs (Certificate Signing Requests)
#    You need 2: one to sign the .app, one to sign the pkg.
# openssl req -new -newkey rsa:2048 -nodes -keyout app.csr.key -out app.csr -subj "/C=US/ST=TN/L=Knoxville/O=FitEdit/OU=FitEdit Support/CN=fitedit.io"
# openssl req -new -newkey rsa:2048 -nodes -keyout installer.csr.key -out installer.csr -subj "/C=US/ST=TN/L=Knoxville/O=FitEdit/OU=FitEdit Support/CN=fitedit.io"

# Upload CSRs to Apple, get certs app.cer and installer.cer 
# Manual process: https://developer.apple.com/account => Certificates, Identifiers & Profiles"
# On macOS you can also use Keychain Access => Certificate Assistant => Request a Certificate from a Certificate Authority

# Download certs, convert CER to PEM then PEM to p12
# openssl x509 -in app.cer -inform DER -out app.pem -outform PEM
# openssl x509 -in installer.cer -inform DER -out installer.pem -outform PEM
# openssl pkcs12 -export -inkey app.csr.key -in app.pem -out app.p12
# openssl pkcs12 -export -inkey installer.csr.key -in installer.pem -out installer.p12

# Import and trust certs
# security import app.p12
# security import installer.p12

# Verify certs were imported
# security find-certificate -c "Developer ID Application: Carl Slater (D89E59Y3DZ)"
# security find-certificate -c "Developer ID Installer: Carl Slater (D89E59Y3DZ)"

# If necessary add trust chain
# security import AppleWWDRCA.cer
# security import AppleWWDRCAG3.cer
# security import AppleWWDRCAG4.cer
# security import DeveloperIDG2CA.cer

# Verify you can sign code
# security find-identity -p codesigning -v
#  1) B6BE41B3925840FD4714211B60890DD16AC9CF1A "Developer ID Application: Carl Slater (D89E59Y3DZ)"
#     1 valid identities found

# Generate app-specific password
# Manual process: https://appleid.apple.com/account/manage => App-Specific Passwords

# Verify app-specific password was created
# xcrun altool --list-providers -u doug@slater.dev -p APP_SPECIFIC_PASSWORD
# ProviderName ProviderShortname PublicID                             WWDRTeamID
# ------------ ----------------- ------------------------------------ ----------
# Carl Slater  D89E59Y3DZ        69a6de88-466d-47e3-e053-5b8c7c11a4d1 D89E59Y3DZ

# Import app-specific password into keychain
# xcrun notarytool store-credentials "FitEdit macOS" --apple-id doug@slater.dev --password APP_SPECIFIC_PASSWORD --team-id D89E59Y3DZ

# Use app-specific password and certs to sign app, create pkg, sign pkg
#csq pack --xplat=osx ... --signAppIdentity="Developer ID Application: Carl Slater (D89E59Y3DZ)" --signInstallIdentity="Developer ID Installer: Carl Slater (D89E59Y3DZ)" --notaryProfile="FitEdit macOS"
