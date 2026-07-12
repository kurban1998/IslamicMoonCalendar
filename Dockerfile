# ============================================================
# Build context — корень репозитория (telegram-hijri-calendar/)
# Сборка:  docker build -t islamic-calendar .
# Запуск:  docker run -p 8080:8080 islamic-calendar
# ============================================================

# ---- 1. Сборка backend ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Сначала копируем только csproj — слой restore кэшируется, пока зависимости не меняются
COPY backend/IslamicCalendar.Api/IslamicCalendar.Api.csproj backend/IslamicCalendar.Api/
RUN dotnet restore backend/IslamicCalendar.Api/IslamicCalendar.Api.csproj

COPY backend/IslamicCalendar.Api/ backend/IslamicCalendar.Api/
RUN dotnet publish backend/IslamicCalendar.Api/IslamicCalendar.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---- 2. Финальный образ ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

# Фронтенд (HTML/CSS/JS) кладём в wwwroot — backend раздаёт его как статику
# через app.UseStaticFiles(), поэтому и API, и мини-приложение живут на одном домене
COPY frontend/ ./wwwroot/

ENTRYPOINT ["dotnet", "IslamicCalendar.Api.dll"]
