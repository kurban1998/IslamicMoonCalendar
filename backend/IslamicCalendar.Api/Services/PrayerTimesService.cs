using System.Globalization;
using System.Net.Http.Json;
using IslamicCalendar.Api.Models;
using Microsoft.Extensions.Options;

namespace IslamicCalendar.Api.Services;

public interface IPrayerTimesService
{
    Task<PrayerTimes> GetPrayerTimesAsync(DateOnly date, double latitude, double longitude, CancellationToken ct = default);
}

/// <summary>
/// Получает время намазов через Aladhan API (https://aladhan.com/prayer-times-api).
/// </summary>
public class PrayerTimesService(HttpClient http, IOptions<IslamicCalendarOptions> options) : IPrayerTimesService
{
    private readonly IslamicCalendarOptions _options = options.Value;

    public async Task<PrayerTimes> GetPrayerTimesAsync(DateOnly date, double latitude, double longitude, CancellationToken ct = default)
    {
        var dateStr = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        var url = $"timings/{dateStr}" +
                   $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&method={_options.PrayerCalculationMethod}";

        var response = await http.GetFromJsonAsync<ApiEnvelope<AladhanTimingsData>>(url, ct);

        if (response?.Data?.Timings is null)
            throw new InvalidOperationException("Не удалось получить время намазов из Aladhan API");

        var t = response.Data.Timings;
        return new PrayerTimes
        {
            Fajr = CleanTime(t.Fajr),
            Sunrise = CleanTime(t.Sunrise),
            Dhuhr = CleanTime(t.Dhuhr),
            Asr = CleanTime(t.Asr),
            Maghrib = CleanTime(t.Maghrib),
            Isha = CleanTime(t.Isha)
        };
    }

    // Aladhan возвращает время вида "04:32 (MSK)" — отбрасываем часовой пояс в скобках
    private static string CleanTime(string raw) =>
        raw.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries) is [var time, ..] ? time : raw;
}
