cd %~dp0
dotnet build magic.signals.contracts/magic.signals.contracts.csproj --configuration Release
dotnet build magic.signals.services/magic.signals.services.csproj --configuration Release
