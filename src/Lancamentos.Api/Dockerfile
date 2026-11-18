FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Lancamentos.Api/Lancamentos.Api.csproj src/Lancamentos.Api/
RUN dotnet restore src/Lancamentos.Api/Lancamentos.Api.csproj
COPY src/Lancamentos.Api/ src/Lancamentos.Api/
RUN dotnet publish src/Lancamentos.Api/Lancamentos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lancamentos.Api.dll"]
