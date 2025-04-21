FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IdP-Server-v2/IdP-Server-v2.Client/IdP-Server-v2.Client.csproj", "IdP-Server-v2/IdP-Server-v2.Client/"]
RUN dotnet restore "IdP-Server-v2/IdP-Server-v2.Client/IdP-Server-v2.Client.csproj"
COPY . .
WORKDIR "/src/IdP-Server-v2/IdP-Server-v2.Client"
RUN dotnet build "IdP-Server-v2.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "IdP-Server-v2.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdP-Server-v2.Client.dll"]
