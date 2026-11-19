FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet restore src/Cashflow.Migrator/Cashflow.Migrator.csproj
RUN dotnet publish src/Cashflow.Migrator/Cashflow.Migrator.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Cashflow.Migrator.dll"]
