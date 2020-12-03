# ServiceTest01
Windows Service
FileSystemWatcher
C#
PoC
New-Service -Name "ServiceTest01" -BinaryPathName 'd:\...\ServiceTest01.exe'
Get-Service -DisplayName "ServiceTest01" | Remove-Service
