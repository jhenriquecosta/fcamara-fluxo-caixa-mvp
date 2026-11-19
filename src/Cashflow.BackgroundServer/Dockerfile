FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet restore src/Cashflow.BackgroundServer/Cashflow.BackgroundServer.csproj
RUN dotnet publish src/Cashflow.BackgroundServer/Cashflow.BackgroundServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cashflow.BackgroundServer.dll"]
