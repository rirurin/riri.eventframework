# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p4rpc.eventframework/*" -Force -Recurse
dotnet publish "./p4rpc.eventframework.csproj" -c Release -o "$env:RELOADEDIIMODS/p4rpc.eventframework" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location