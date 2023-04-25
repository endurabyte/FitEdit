param (
  [string]$configuration,
  [string]$targetFramework
)

pushd $PSScriptRoot
echo "protect.ps1: Protecting..."
. "C:\Program Files (x86)\Eziriz\.NET Reactor\dotNET_Reactor.Console.exe" -project .\Dauer.Fuse.nrproj -nodialog -file ..\Dauer.Fuse\bin\$configuration\$targetFramework\Dauer.Fuse.dll
popd
