# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

./Publish.ps1 -ProjectPath "p3rpc.eventframework/p3rpc.eventframework.csproj" -PackageName "p3rpc.eventframework" -PublishOutputDir "Publish/P3R/ToUpload"
# ./Publish.ps1 -ProjectPath "p4rpc.eventframework/p4rpc.eventframework.csproj" -PackageName "p4rpc.eventframework" -PublishOutputDir "Publish/P4R/ToUpload"