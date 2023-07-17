dotnet tool install -g csq --prerelease

dotnet publish Dauer.Ui.Desktop.csproj --configuration Release --runtime win-x64 --framework net7.0 --output "./bin/Release/net7.0/publish/win-x64/" --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false

csq pack --xplat=win --packId "FitEdit" --packAuthors "Doug Slater" --packVersion "1.0.3" --packDirectory "./bin/Release/net7.0/publish/win-x64" --icon "../Dauer.Ui/Assets/logo.ico" --mainExe "FitEdit.exe" --releaseDir "./releases/win-x64"

# Sync with s3
pushd
cd releases
& .\sync.ps1
popd
