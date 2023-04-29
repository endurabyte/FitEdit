param (
  [string]$configuration,
  [string]$targetFramework
)

function Log {
    param (
        [string]$Message
    )

    $scriptName = (Split-Path -Leaf $MyInvocation.ScriptName)
    Write-Host "[$scriptName] $Message"
}

pushd $PSScriptRoot
Log "Securing exe..."
. "C:\Program Files (x86)\Eziriz\.NET Reactor\dotNET_Reactor.Console.exe" -project .\Dauer.Ui.Desktop.nrproj -nodialog

pushd $PSScriptRoot\bin\$configuration\$targetFramework\
rm Dauer.Adapters.Fit.dll
rm Dauer.Data.dll
rm Dauer.Model.dll
rm Dauer.Services.dll
rm Dauer.Ui.dll
rm *.pdb
popd
popd
