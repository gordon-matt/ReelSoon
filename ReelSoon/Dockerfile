# ReelSoon web container.

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
WORKDIR /app
EXPOSE 8080
RUN mkdir -p /app/data \
    && apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ReelSoon/ReelSoon.csproj", "ReelSoon/"]
COPY ["ReelSoon.Core/ReelSoon.Core.csproj", "ReelSoon.Core/"]
COPY ["ReelSoon.Data/ReelSoon.Data.csproj", "ReelSoon.Data/"]
COPY ["ReelSoon.Data.MySql/ReelSoon.Data.MySql.csproj", "ReelSoon.Data.MySql/"]
COPY ["ReelSoon.Data.Npgsql/ReelSoon.Data.Npgsql.csproj", "ReelSoon.Data.Npgsql/"]
COPY ["ReelSoon.Data.Sql/ReelSoon.Data.Sql.csproj", "ReelSoon.Data.Sql/"]
COPY ["ReelSoon.Data.Sqlite/ReelSoon.Data.Sqlite.csproj", "ReelSoon.Data.Sqlite/"]
COPY ["ReelSoon.Models/ReelSoon.Models.csproj", "ReelSoon.Models/"]
COPY ["ReelSoon.Services/ReelSoon.Services.csproj", "ReelSoon.Services/"]
RUN dotnet restore "./ReelSoon/ReelSoon.csproj"
COPY . .
WORKDIR "/src/ReelSoon"
RUN dotnet build "./ReelSoon.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ReelSoon.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReelSoon.dll"]
