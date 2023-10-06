& .\submodule-init.ps1
dotnet build FitEdit.Deployment.sln --configuration Release --version-suffix ""
exit $LASTEXITCODE
