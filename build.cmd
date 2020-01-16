cd %~dp0
dotnet build magic.signals.contracts/magic.signals.contracts.csproj --configuration Release --source https://api.nuget.org/v3/index.json
dotnet build magic.signals.services/magic.signals.services.csproj --configuration Release --source https://api.nuget.org/v3/index.json
