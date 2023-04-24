pushd $PSScriptRoot
echo "protect.ps1: Protecting..."
. "C:\Program Files (x86)\Eziriz\.NET Reactor\dotNET_Reactor.Console.exe" -project .\Dauer.Fuse.nrproj -nodialog
popd
