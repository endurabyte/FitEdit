#!/bin/vbash
source /opt/vyatta/etc/functions/script-template
configure
delete port-forward rule 1
delete port-forward rule 3
set port-forward rule 1 description 'https to ASP.NET Core'
set port-forward rule 1 forward-to address 192.168.1.163
set port-forward rule 1 forward-to port 443
set port-forward rule 1 original-port 444
set port-forward rule 1 protocol tcp
set port-forward rule 3 description 'https to pi4 (tcc-mitm)'
set port-forward rule 3 forward-to address 192.168.1.160
set port-forward rule 3 forward-to port 5007
set port-forward rule 3 original-port 443
set port-forward rule 3 protocol tcp
commit
save
exit