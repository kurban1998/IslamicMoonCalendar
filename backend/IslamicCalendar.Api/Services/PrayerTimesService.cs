using System.Globalization;
using System.Net.Http.Json;
using IslamicCalendar.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace IslamicCalendar.Api.Services;

public interface IPrayerTimesService
{
    Task<PrayerTimes> GetPrayerTimesAsync(DateOnly date, double latitude, double longitude, CancellationToken ct = default);
}

/// <summary>
/// Получает время намазов через Aladhan API (https://aladhan.com/prayer-times-api).
/// Результат кэшируется на 12 часов по дате+координатам (округлённым до ~1 км) —
/// повторное открытие того же дня в той же геолокации не ходит в сеть заново.
/// </summary>
public class PrayerTimesService(HttpClient http, IMemoryCache cache, IOptions<IslamicCalendarOptions> options) : IPrayerTimesService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);
    private readonly IslamicCalendarOptions _options = options.Value;

    public async Task<PrayerTimes> GetPrayerTimesAsync(DateOnly date, double latitude, double longitude, CancellationToken ct = default)
    {
        var cacheKey = $"prayer:{date:yyyy-MM-dd}:{latitude.ToString("0.00", CultureInfo.InvariantCulture)}:{longitude.ToString("0.00", CultureInfo.InvariantCulture)}:{_options.PrayerCalculationMethod}";

        if (cache.TryGetValue(cacheKey, out PrayerTimes? cached) && cached is not null)
            return cached;

        var dateStr = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        var url = $"timings/{dateStr}" +
                   $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&method={_options.PrayerCalculationMethod}";

        var response = await http.GetFromJsonAsync<ApiEnvelope<AladhanTimingsData>>(url, ct);

        if (response?.Data?.Timings is null)
            throw new InvalidOperationException("Не удалось получить время намазов из Aladhan API");

        var t = response.Data.Timings;
        var result = new PrayerTimes
        {
            Fajr = CleanTime(t.Fajr),
            Sunrise = CleanTime(t.Sunrise),
            Dhuhr = CleanTime(t.Dhuhr),
            Asr = CleanTime(t.Asr),
            Maghrib = CleanTime(t.Maghrib),
            Isha = CleanTime(t.Isha)
        };

        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    // Aladhan возвращает время вида "04:32 (MSK)" — отбрасываем часовой пояс в скобках
    private static string CleanTime(string raw) =>
        raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) is [var time, ..] ? time : raw;
}
