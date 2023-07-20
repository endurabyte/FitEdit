# Forward public TCP traffic on 443 to us
Install-Module -Name Posh-SSH -Scope CurrentUser

function Set-PortForward {
    param(
        [Parameter(Mandatory=$true)]
        [bool] $active
    )

    # Ports
    $aspnetcorePort = 443
    $pi4Port = 5007
    $aspnetcoreOriginalPort = if ($active) { 443 } else { 444 }
    $pi4OriginalPort = if ($active) { 444 } else { 443 }

    # SSH Connection Information
    $Password = ConvertTo-SecureString -String $env:UBNT_SSH_PASSWORD -AsPlainText -Force
    $Credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList "ubnt", $Password

    # Create New SSH Session
    $Session = New-SSHSession -ComputerName 192.168.1.1 -Credential $Credential -AcceptKey

    # Prepare a script to allow us to run the "show port-forward" command
    $prepareCommandScript = @"
#!/bin/vbash
source /opt/vyatta/etc/functions/script-template
show port-forward
"@

    # Write the script to a local file
    $localFilePath = "$PSScriptRoot/prepareCommand.sh"
    [System.IO.File]::WriteAllText($localFilePath, $prepareCommandScript.Replace("`r`n", "`n"))

    # Upload the local file to the remote system
    Set-SCPItem -ComputerName '192.168.1.1' -Credential $Credential -Destination '/tmp' -Path $localFilePath -Verbose

    # Create, Execute and Delete script on the remote machine
    Invoke-SSHCommand -SessionId $Session.SessionId -Command "chmod +x /tmp/prepareCommand.sh"
    $output = Invoke-SSHCommand -SessionId $Session.SessionId -Command "vbash /tmp/prepareCommand.sh"
    Invoke-SSHCommand -SessionId $Session.SessionId -Command "rm /tmp/prepareCommand.sh"

    # Parse output to get rule numbers
    $aspnetcoreRuleNumber = ($output.Output -split '\n' | Select-String -Pattern "rule (\d+) {" -Context 0,1 | Where-Object { $_.Context.PostContext -like '*https to ASP.NET Core*' }).Matches.Groups[1].Value
    $pi4RuleNumber = ($output.Output -split '\n' | Select-String -Pattern "rule (\d+) {" -Context 0,1 | Where-Object { $_.Context.PostContext -like '*https to pi4 (tcc-mitm)*' }).Matches.Groups[1].Value

    Write-Host "ASP.NET Rule Number: $aspnetcoreRuleNumber"
    Write-Host "tcc-mitm Rule Number: $pi4RuleNumber"

    # Generate the configuration script
    $scriptContent = @"
#!/bin/vbash
source /opt/vyatta/etc/functions/script-template
configure
delete port-forward rule 1
delete port-forward rule 3
set port-forward rule 1 description 'https to ASP.NET Core'
set port-forward rule 1 forward-to address 192.168.1.163
set port-forward rule 1 forward-to port $aspnetcorePort
set port-forward rule 1 original-port $aspnetcoreOriginalPort
set port-forward rule 1 protocol tcp
set port-forward rule 3 description 'https to pi4 (tcc-mitm)'
set port-forward rule 3 forward-to address 192.168.1.160
set port-forward rule 3 forward-to port $pi4Port
set port-forward rule 3 original-port $pi4OriginalPort
set port-forward rule 3 protocol tcp
commit
save
exit
"@
    
    # Write the script to a local file
    $localFilePath = "$PSScriptRoot/config.sh"
    [System.IO.File]::WriteAllText($localFilePath, $scriptContent.Replace("`r`n", "`n"))

    # Upload the local file to the remote system
    Set-SCPItem -ComputerName '192.168.1.1' -Credential $Credential -Destination '/tmp' -Path $localFilePath -Verbose

    # Create, Execute and Delete script on the remote machine
    Invoke-SSHCommand -SessionId $Session.SessionId -Command "chmod +x /tmp/config.sh"
    Invoke-SSHCommand -SessionId $Session.SessionId -Command "vbash /tmp/config.sh"
    # Invoke-SSHCommand -SessionId $Session.SessionId -Command "rm /tmp/config.sh"

    # Disconnect the SSH Session
    Remove-SSHSession -SessionId $Session.SessionId
}
