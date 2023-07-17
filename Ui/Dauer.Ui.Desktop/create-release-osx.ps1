dotnet publish Dauer.Ui.Desktop.csproj --configuration Release --runtime osx-x64 --framework net7.0 --output "./bin/Release/net7.0/publish/osx-x64/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=osx --packId "FitEdit" --packAuthors "Doug Slater" --packVersion "1.0.3" --packDir "./bin/Release/net7.0/publish/osx-x64" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit" --releaseDir="./releases/osx-x64"

# Sync with s3
pushd
cd releases
& .\sync.ps1
popd
