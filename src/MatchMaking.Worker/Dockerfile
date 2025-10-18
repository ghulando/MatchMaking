FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MatchMaking.Worker/MatchMaking.Worker.csproj", "MatchMaking.Worker/"]
COPY ["MatchMaking.Application/MatchMaking.Application.csproj", "MatchMaking.Application/"]
COPY ["MatchMaking.Domain/MatchMaking.Domain.csproj", "MatchMaking.Domain/"]
COPY ["MatchMaking.Infrastructure/MatchMaking.Infrastructure.csproj", "MatchMaking.Infrastructure/"]
RUN dotnet restore "MatchMaking.Worker/MatchMaking.Worker.csproj"
COPY . .
WORKDIR "/src/MatchMaking.Worker"
RUN dotnet build "MatchMaking.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MatchMaking.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaking.Worker.dll"]