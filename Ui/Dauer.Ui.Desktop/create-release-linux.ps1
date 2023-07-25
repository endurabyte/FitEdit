param (
    [string]$version = "1.0.0",
    [bool]$sync = $false
)

$projectFile = "Dauer.Ui.Desktop.csproj"
$targetFramework = "net7.0"
$runtimeIdentifier = "linux-x64"
$configuration = "Release"
$buildDir = "./bin/$configuration/$targetFramework/$runtimeIdentifier/" 
$publishDir = "./bin/$configuration/$targetFramework/publish/$runtimeIdentifier/"
$releaseDir = "./releases/$runtimeIdentifier/"
$fileExtensions = @("*.deb", "*.rpm", "*.tar.gz")

pushd $PSScriptRoot

dotnet msbuild $projectFile /t:CreateDeb /t:CreateRpm /t:CreateTarball /p:Version=$version /p:TargetFramework=$targetFramework /p:RuntimeIdentifier=$runtimeIdentifier /p:Configuration=$configuration /p:SelfContained=true /p:PublishSingleFile=true /p:PublishTrimmed=false /p:PublishDir=$publishDir /p:ApplicationVersion=$version /p:DebugSymbols=false /p:DebugType=None

New-Item -ItemType Directory -Path $releaseDir -Force
$fileExtensions | ForEach-Object {
  Get-ChildItem -Path $buildDir -Filter $_ | Where-Object { $_.Name -match $version } | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $releaseDir
  }
}

if ($sync -ne $true) {
    popd
    return
}

# Sync with s3
pushd
cd releases
& .\sync.ps1
popd
popd
