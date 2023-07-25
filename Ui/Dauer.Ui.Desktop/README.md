# How to sign code on Windows

## Use a public certificate

1. Create an LLC
2. Pay the certificate mafia
3. Get a certificate on a USB key or cloud-based HSM
4. Make it work with your CI

## Create a self-signed certificate
Source: https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-self-signed-certificate
```powershell
$certname = "fitedit" # Specify your preferred name
$cert = New-SelfSignedCertificate -Subject "CN=$certname" -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeySpec Signature -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256
```

# Export the public certificate
```powershell
Export-Certificate -Cert $cert -FilePath "C:\Users\$user\Desktop\$certname.cer" # Specify your preferred location
```

# (Optional) Export the public certificate with its private key
.pfx is a private key file format. It's password protected.
```powershell
$mypwd = ConvertTo-SecureString -String "{myPassword}" -Force -AsPlainText  ## Replace {myPassword}
Export-PfxCertificate -Cert $cert -FilePath "C:\Users\$user\Desktop\$certname.pfx" -Password $mypwd # Specify your preferred location
```

# Sign the exe
You can use SignTool.exe
Source: https://learn.microsoft.com/en-us/windows/win32/appxpkg/how-to-sign-a-package-using-signtool
# Optional: You can omit the /p password parameter if you use a .pfx file that isn't password protected.
```powershell
``` powershell
SignTool sign /fd SHA256 /f $certname.pfx /p password /tr http://timestamp.digicert.com FitEdit.exe
```
# Optional: The /n parameter uses a cert located in the `My` store with the subject name specified. 
#           Then you don't have to store the certificate somewhere in the filesystem.
```powershell
SignTool sign /n "$certname" /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com FitEdit.exe
```

## Or pass it as an argument to csq (Clowd.Squirrel)
```powershell
csq pack --xplat=win ... --signParams="/n fitedit-selfSigned /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com"
```

# How to sign code on macOS

## Generate CSRs (Certificate Signing Requests)
```powershell
You need two: one to sign the `.app`, one to sign the `.pkg`.

```bash
openssl req -new -newkey rsa:2048 -nodes -keyout app.csr.key -out app.csr -subj "/C=US/ST=TN/L=Knoxville/O=FitEdit/OU=FitEdit Support/CN=fitedit.io
openssl req -new -newkey rsa:2048 -nodes -keyout installer.csr.key -out installer.csr -subj "/C=US/ST=TN/L=Knoxville/O=FitEdit/OU=FitEdit Support/CN=fitedit.io
```

On macOS you can also use Keychain Access => Certificate Assistant => Request a Certificate from a Certificate Authority.
Do this twice, once for the app CSR and once for the installer CSR.

## Get certificates
It's a manual process. You upload the CSRs to Apple's website and download the resulting certificates.
- Go to https://developer.apple.com/account 
- Navigate to Certificates, Identifiers & Profiles => Certificates
- Click the + icon
- choose "Developer ID Application"
  - Follow the steps. Download the certificate, and rename it to app.cer.
- repeat for "Developer ID Installer"
  - Follow the steps. Download the certificate, and rename it to installer.cer.

## Import certs into Keychain Access
You can do this in Keychain Access manually or via the command line.
If working via the command line, first we have to convert the certs to PKCS format (.p12 file):

### Convert to PKCS format
```bash
openssl x509 -in app.cer -inform DER -out app.pem -outform PEM
openssl x509 -in installer.cer -inform DER -out installer.pem -outform PEM
openssl pkcs12 -export -inkey app.csr.key -in app.pem -out app.p12
openssl pkcs12 -export -inkey installer.csr.key -in installer.pem -out installer.p12
```

### Import and trust certs
```
security import app.p12
security import installer.p12
```

### Verify certs were imported
```
security find-certificate -c "Developer ID Application: Carl Slater (D89E59Y3DZ)"
security find-certificate -c "Developer ID Installer: Carl Slater (D89E59Y3DZ)"
```

### If necessary add the trust chain
```bash
# security import AppleWWDRCA.cer
# security import AppleWWDRCAG3.cer
# security import AppleWWDRCAG4.cer
# security import DeveloperIDG2CA.cer
```

### (Optional) Verify you can sign code
```
security find-identity -p codesigning -v
  1) B6BE41B3925840FD4714211B60890DD16AC9CF1A "Developer ID Application: Carl Slater (D89E59Y3DZ)"
     1 valid identities found
```

## Generate an app-specific password
It's a manual process. You do it on Apple's website.
- Go to https://appleid.apple.com/account/manage
- Click the link `App-Specific Passwords`
- Follow the instructions
- Make note of the app-specific password, referred to here as APP_SPECIFIC_PASSWORD.
 
## (Optional) Verify the app-specific password was created
```bash
# xcrun altool --list-providers -u doug@slater.dev -p APP_SPECIFIC_PASSWORD
# ProviderName ProviderShortname PublicID                             WWDRTeamID
# ------------ ----------------- ------------------------------------ ----------
# Carl Slater  D89E59Y3DZ        69a6de88-466d-47e3-e053-5b8c7c11a4d1 D89E59Y3DZ
```

## Import the app-specific password into keychain
```bash
xcrun notarytool store-credentials "FitEdit macOS" --apple-id doug@slater.dev --password APP_SPECIFIC_PASSWORD --team-id D89E59Y3DZ
```

## Sign the .app and .pkg
Pass the cert names and the name of the app-specific password stored to Clowd.Squirrel.

```bash
csq pack --xplat=osx ... --signAppIdentity="Developer ID Application: Carl Slater (D89E59Y3DZ)" --signInstallIdentity="Developer ID Installer: Carl Slater (D89E59Y3DZ)" --notaryProfile="FitEdit macOS"
```
