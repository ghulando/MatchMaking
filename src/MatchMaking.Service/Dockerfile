FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MatchMaking.Service.csproj", "."]
RUN dotnet restore "./MatchMaking.Service.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MatchMaking.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MatchMaking.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaking.Service.dll"]