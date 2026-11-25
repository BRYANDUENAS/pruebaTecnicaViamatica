# ----- Compilación -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primero los proyectos, para aprovechar la caché de capas al restaurar paquetes.
COPY Viamatica.sln ./
COPY src/Viamatica.Domain/Viamatica.Domain.csproj src/Viamatica.Domain/
COPY src/Viamatica.Application/Viamatica.Application.csproj src/Viamatica.Application/
COPY src/Viamatica.Infrastructure/Viamatica.Infrastructure.csproj src/Viamatica.Infrastructure/
COPY src/Viamatica.Api/Viamatica.Api.csproj src/Viamatica.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Viamatica.Api -c Release -o /app --no-restore

# ----- Ejecución -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Viamatica.Api.dll"]
