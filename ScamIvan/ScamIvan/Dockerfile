# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/runtime:8.0.11-noble-amd64 AS base
#SHELL ["/bin/dash", "-c"]
RUN apt-get update && \apt-get install -y \libappindicator3-1 \libatk1.0-0 \libasound2 \libgbm1 \libnss3 \libxss1 && \rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ScamIvan/ScamIvan.csproj", "ScamIvan/"]
RUN dotnet restore "./ScamIvan/ScamIvan.csproj"
COPY . .
WORKDIR "/src/ScamIvan"
RUN dotnet build "./ScamIvan.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ScamIvan.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScamIvan.dll"]