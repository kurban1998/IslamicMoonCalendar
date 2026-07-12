using IslamicCalendar.Api;
using IslamicCalendar.Api.Models;
using IslamicCalendar.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IslamicCalendarOptions>(
    builder.Configuration.GetSection(IslamicCalendarOptions.SectionName));
builder.Services.Configure<TelegramBotOptions>(
    builder.Configuration.GetSection(TelegramBotOptions.SectionName));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddPolicy("MiniApp", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (builder.Environment.IsDevelopment() || origins.Length == 0)
        {
            // В разработке (и пока не указан домен мини-приложения) разрешаем всё
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddHttpClient<IHijriCalendarService, HijriCalendarService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<IslamicCalendarOptions>>().Value;
    client.BaseAddress = new Uri(opts.AladhanBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IPrayerTimesService, PrayerTimesService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<IslamicCalendarOptions>>().Value;
    client.BaseAddress = new Uri(opts.AladhanBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IQuranService, QuranService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<IslamicCalendarOptions>>().Value;
    client.BaseAddress = new Uri(opts.AlQuranBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    // Политика использования Nominatim требует осмысленный User-Agent с контактом —
    // замените e-mail на свой перед продакшен-использованием
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "IslamicCalendarMiniApp/1.0 (contact: replace-with-your-email@example.com)");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IMoonPhaseService, MoonPhaseService>();
builder.Services.AddSingleton<IHadithService, HadithService>();
builder.Services.AddSingleton<IFridayReminderService, FridayReminderService>();
builder.Services.AddScoped<IQuoteOfDayService, QuoteOfDayService>();
builder.Services.AddHostedService<TelegramBotHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MiniApp");
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

// GET /api/calendar/month/2026/7 — сетка дней месяца для календаря
api.MapGet("/calendar/month/{year:int}/{month:int}", async (
        int year, int month, IHijriCalendarService hijriService, CancellationToken ct) =>
    {
        if (month is < 1 or > 12)
            return Results.BadRequest(new { error = "Месяц должен быть от 1 до 12" });

        var days = await hijriService.GetMonthAsync(year, month, ct);
        return Results.Ok(days);
    })
    .WithName("GetMonth")
    .Produces<List<MonthCalendarDay>>();

// GET /api/day?date=2026-07-12&lat=55.75&lon=37.61 — полная карточка дня
api.MapGet("/day", async (
        string date,
        double? lat,
        double? lon,
        IHijriCalendarService hijriService,
        IPrayerTimesService prayerTimesService,
        IMoonPhaseService moonPhaseService,
        IQuoteOfDayService quoteOfDayService,
        IFridayReminderService fridayReminderService,
        IGeocodingService geocodingService,
        IOptions<IslamicCalendarOptions> options,
        CancellationToken ct) =>
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
            return Results.BadRequest(new { error = "Некорректный формат даты, ожидается yyyy-MM-dd" });

        var opts = options.Value;
        var latitude = lat ?? opts.DefaultLatitude;
        var longitude = lon ?? opts.DefaultLongitude;
        var locationProvided = lat.HasValue && lon.HasValue;

        var hijriTask = hijriService.ConvertAsync(parsedDate, ct);
        var prayersTask = prayerTimesService.GetPrayerTimesAsync(parsedDate, latitude, longitude, ct);
        var quoteTask = quoteOfDayService.GetQuoteAsync(parsedDate, ct);

        // Город определяем только если координаты реально пришли от клиента —
        // иначе просто берём название по умолчанию из конфига, не дёргая Nominatim
        var locationTask = locationProvided
            ? geocodingService.GetLocationNameAsync(latitude, longitude, ct)
            : Task.FromResult(opts.DefaultCityName);

        await Task.WhenAll(hijriTask, prayersTask, quoteTask, locationTask);

        var response = new DayCardResponse
        {
            GregorianDate = parsedDate,
            Hijri = hijriTask.Result,
            Prayers = prayersTask.Result,
            Moon = moonPhaseService.GetMoonPhase(parsedDate),
            Quote = quoteTask.Result,
            Friday = fridayReminderService.GetFridayInfo(parsedDate),
            LocationName = locationTask.Result
        };

        return Results.Ok(response);
    })
    .WithName("GetDayCard")
    .Produces<DayCardResponse>();

app.Run();
