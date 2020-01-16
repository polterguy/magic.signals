
set version=%1
set key=%2

cd %~dp0

dotnet build magic.signals.contracts/magic.signals.contracts.csproj --configuration Release --source https://api.nuget.org/v3/index.json
dotnet nuget push magic.signals.contracts/bin/Release/magic.signals.contracts.%version%.nupkg -k %key% -s https://api.nuget.org/v3/index.json

dotnet build magic.signals.services/magic.signals.services.csproj --configuration Release --source https://api.nuget.org/v3/index.json
dotnet nuget push magic.signals.services/bin/Release/magic.signals.%version%.nupkg -k %key% -s https://api.nuget.org/v3/index.json
