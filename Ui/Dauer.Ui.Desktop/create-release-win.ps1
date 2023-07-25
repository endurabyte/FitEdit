param (
    [string]$version = "1.0.0",
    [bool]$sync = $false
)

$framework = "net7.0"
$configuration = "Release"
$rid = "win-x64"
$authors = "EnduraByte LLC"
$packId = "FitEdit"
$certTmpPath = "cert.tmp.pfx"
$certSubject = "fitedit-SelfSigned"

pushd $PSScriptRoot

echo "Creating $certTmpPath from CODE_SIGN_CERTIFICATE environment variable..."
& ./Decode-FromBase64.ps1 $env:CODE_SIGN_CERTIFICATE $certTmpPath

echo "Importing $certTmpPath into certificate store..."
$pass = ConvertTo-SecureString -String $env:CODE_SIGN_CERTIFICATE_PASSWORD -Force -AsPlainText
Import-PfxCertificate -FilePath $certTmpPath -CertStoreLocation Cert:\CurrentUser\My -Password $pass

echo "Publishing..."
dotnet publish Dauer.Ui.Desktop.csproj --configuration Release --runtime $rid --framework $framework --output "./bin/Release/$framework/publish/$rid/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

echo "Packing..."
dotnet tool install -g csq --prerelease
csq pack --xplat=win --packId "FitEdit" --packAuthors $authors --packVersion $version --packDirectory "./bin/Release/$framework/publish/$rid" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit.exe" --releaseDir "./releases/$rid" --signParams="/n $certSubject /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com"

if ($sync -ne $true) {
    popd
    return
}

# Sync with s3
echo "Deploying..."
pushd
cd releases
& .\sync.ps1
popd
popd
