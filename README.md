# Исламский лунный календарь — Telegram Mini App

Мусульманский (хиджра) календарь с листанием месяцев, карточкой дня (фаза Луны,
время намазов, хадис/аят) и напоминаниями по пятницам.

## Структура проекта

```
telegram-hijri-calendar/
  backend/IslamicCalendar.Api/   — ASP.NET Core Web API (.NET 9)
    Program.cs                  — эндпоинты /api/calendar/month, /api/day
    Models/                     — DTO-модели ответов
    Services/                   — интеграции с Aladhan API, AlQuran Cloud,
                                   локальный расчёт фазы Луны, хадисы
    Data/hadiths.json           — локальная база хадисов (Бухари/Муслим) — ДОПОЛНИТЕ
    appsettings.json            — настройки (координаты по умолчанию, метод расчёта)
  frontend/                     — статика мини-приложения (HTML/CSS/JS)
    index.html
    css/style.css
    js/app.js
```

Backend одновременно раздаёт и API, и статику фронтенда (`app.UseStaticFiles()`),
поэтому для простого варианта достаточно скопировать содержимое `frontend/` в
`backend/IslamicCalendar.Api/wwwroot/` — тогда всё будет работать на одном домене
без проблем с CORS. Либо разместите фронтенд отдельно и укажите домен backend
в `Cors:AllowedOrigins` (appsettings.json) и `API_BASE` в `js/app.js`.

## Используемые внешние API

- **Aladhan API** (https://aladhan.com) — бесплатный, без ключа:
  - `gToH` / `gToHCalendar` — перевод григорианских дат в Хиджру;
  - `timings` — время намазов по координатам.
- **AlQuran Cloud API** (https://alquran.cloud/api) — бесплатный, без ключа:
  - случайный аят с переводом (по умолчанию `ru.kuliev`, можно сменить в
    `appsettings.json` → `QuranTranslationEdition`, например на `ru.osmanov`).
- **Фаза Луны** считается локально по астрономической формуле (без API) — так
  надёжнее: публичные бесплатные API фаз Луны либо платные, либо нестабильны,
  а формула детерминирована и точна для отображения иконки. При желании можно
  подменить `MoonPhaseService` на обёртку над внешним API — интерфейс
  `IMoonPhaseService` для этого и предназначен.

## Хадисы

Файл `backend/IslamicCalendar.Api/Data/hadiths.json` — простой JSON-массив,
я закинул 8 стартовых хадисов (Бухари/Муслим) в вольном пересказе. Дополняйте
своими текстами и точными ссылками (номер хадиса, глава и т.д.), формат:

```json
{
  "id": 9,
  "source": "Аль-Бухари",
  "reference": "№ 6018",
  "textRu": "Текст хадиса на русском"
}
```

Цитата дня (`QuoteOfDayService`) выбирает хадис или аят детерминированно по
дате — один день всегда показывает одну и ту же цитату, и если API Корана
недоступен, автоматически подстраховывается хадисом.

## Запуск локально

```bash
cd backend/IslamicCalendar.Api
dotnet restore
dotnet run
```

Откроется на `https://localhost:xxxx`. Swagger — на `/swagger` (только в Development).

## Подключение к Telegram

1. Разверните backend на сервере с HTTPS (Telegram требует HTTPS для Mini Apps).
2. У @BotFather: `/newapp` (или `/mybots` → выбрать бота → Bot Settings → Menu
   Button / Mini App) и укажите URL вашего приложения.
3. В `appsettings.json` пропишите домен мини-приложения в `Cors:AllowedOrigins`,
   если фронтенд и backend на разных доменах.
4. Геолокация для времени намазов запрашивается через `Telegram.WebApp.LocationManager`
   (Bot API 8.0+) с fallback на `navigator.geolocation` и, если пользователь не
   даст доступ, backend использует координаты по умолчанию из `appsettings.json`.

## Что стоит доделать/проверить

- Проверить/поправить формулировки хадисов и добавить точные ссылки на издания.
- Уточнить `PrayerCalculationMethod` под свой регион (список методов —
  https://aladhan.com/calculation-methods; например, для России часто
  используют метод 14 — Spiritual Administration of Muslims of Russia).
- При желании добавить кэширование ответов Aladhan/AlQuran на backend
  (`IMemoryCache`), чтобы не дёргать внешние API на каждый клик.
- Названия сур на русском в `QuranService` — стандартные транслитерации,
  но при желании сверьте с предпочитаемым изданием перевода.
