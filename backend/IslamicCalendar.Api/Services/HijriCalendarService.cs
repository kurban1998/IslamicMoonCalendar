using System.Globalization;
using System.Net.Http.Json;
using IslamicCalendar.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IslamicCalendar.Api.Services;

public interface IHijriCalendarService
{
    Task<HijriDate> ConvertAsync(DateOnly date, CancellationToken ct = default);
    Task<List<MonthCalendarDay>> GetMonthAsync(int year, int month, CancellationToken ct = default);
    Task<List<MonthCalendarDay>> GetHijriMonthAsync(int hijriYear, int hijriMonth, CancellationToken ct = default);
}

/// <summary>
/// Получает соответствие григорианских и хиджра-дат через Aladhan API.
/// https://aladhan.com/islamic-calendar-api
///
/// Результаты кэшируются в памяти: соответствие дат не меняется со временем,
/// поэтому повторное открытие того же дня/месяца не ходит в сеть заново —
/// это заметно ускоряет навигацию по календарю.
/// </summary>
public class HijriCalendarService(HttpClient http, IMemoryCache cache, ILogger<HijriCalendarService> logger) : IHijriCalendarService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(30);

    private static readonly string[] WeekdaysRu =
        ["Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота"];

    public async Task<HijriDate> ConvertAsync(DateOnly date, CancellationToken ct = default)
    {
        var cacheKey = $"hijri:{date:yyyy-MM-dd}";
        if (cache.TryGetValue(cacheKey, out HijriDate? cached) && cached is not null)
            return cached;

        var dateStr = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        var response = await http.GetFromJsonAsync<ApiEnvelope<AladhanDateEntry>>(
            $"gToH?date={dateStr}", ct);

        if (response?.Data is null)
        {
            logger.LogWarning("Aladhan gToH не вернул данные для даты {Date}", dateStr);
            throw new InvalidOperationException("Не удалось получить дату по Хиджре из Aladhan API");
        }

        var result = MapHijri(response.Data.Hijri, date);
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<List<MonthCalendarDay>> GetMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var cacheKey = $"hijri-month:{year}-{month}";
        if (cache.TryGetValue(cacheKey, out List<MonthCalendarDay>? cached) && cached is not null)
        {
            // isToday мог "устареть", если кэш пережил полночь — пересчитываем на лету
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return cached.Select(d => new MonthCalendarDay
            {
                GregorianDate = d.GregorianDate,
                Hijri = d.Hijri,
                IsFriday = d.IsFriday,
                IsToday = d.GregorianDate == today
            }).ToList();
        }

        var response = await http.GetFromJsonAsync<ApiEnvelope<List<AladhanDateEntry>>>(
            $"gToHCalendar/{month}/{year}", ct);

        if (response?.Data is null)
        {
            logger.LogWarning("Aladhan gToHCalendar не вернул данные для {Month}/{Year}", month, year);
            throw new InvalidOperationException("Не удалось получить календарь по Хиджре из Aladhan API");
        }

        var todayNow = DateOnly.FromDateTime(DateTime.UtcNow);

        var days = response.Data.Select(entry =>
        {
            var gDate = DateOnly.ParseExact(entry.Gregorian.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            return new MonthCalendarDay
            {
                GregorianDate = gDate,
                Hijri = MapHijri(entry.Hijri, gDate),
                IsFriday = gDate.DayOfWeek == DayOfWeek.Friday,
                IsToday = gDate == todayNow
            };
        }).ToList();

        cache.Set(cacheKey, days, CacheDuration);
        return days;
    }

    /// <summary>
    /// Возвращает все дни ОДНОГО лунного (хиджра) месяца через Aladhan hToGCalendar —
    /// в отличие от GetMonthAsync (григорианский месяц), здесь границы месяца
    /// определяются по Хиджре. Это важно для UI: календарь листается по лунным
    /// месяцам, и именно этот набор дней считается "текущим" (не приглушённым) —
    /// григорианская граница месяца тут вообще не участвует.
    /// </summary>
    public async Task<List<MonthCalendarDay>> GetHijriMonthAsync(int hijriYear, int hijriMonth, CancellationToken ct = default)
    {
        var cacheKey = $"hijri-month-h:{hijriYear}-{hijriMonth}";
        if (cache.TryGetValue(cacheKey, out List<MonthCalendarDay>? cached) && cached is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return cached.Select(d => new MonthCalendarDay
            {
                GregorianDate = d.GregorianDate,
                Hijri = d.Hijri,
                IsFriday = d.IsFriday,
                IsToday = d.GregorianDate == today
            }).ToList();
        }

        var response = await http.GetFromJsonAsync<ApiEnvelope<List<AladhanDateEntry>>>(
            $"hToGCalendar/{hijriMonth}/{hijriYear}", ct);

        if (response?.Data is null)
        {
            logger.LogWarning("Aladhan hToGCalendar не вернул данные для {Month}/{Year} по Хиджре", hijriMonth, hijriYear);
            throw new InvalidOperationException("Не удалось получить лунный месяц из Aladhan API");
        }

        var todayNow = DateOnly.FromDateTime(DateTime.UtcNow);

        var days = response.Data.Select(entry =>
        {
            var gDate = DateOnly.ParseExact(entry.Gregorian.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            return new MonthCalendarDay
            {
                GregorianDate = gDate,
                Hijri = MapHijri(entry.Hijri, gDate),
                IsFriday = gDate.DayOfWeek == DayOfWeek.Friday,
                IsToday = gDate == todayNow
            };
        }).ToList();

        cache.Set(cacheKey, days, CacheDuration);
        return days;
    }

    private static HijriDate MapHijri(AladhanHijri hijri, DateOnly gregorianDate)
    {
        var monthIndex = Math.Clamp(hijri.Month.Number - 1, 0, HijriDate.MonthNamesRu.Length - 1);

        return new HijriDate
        {
            Day = int.Parse(hijri.Day, CultureInfo.InvariantCulture),
            Month = hijri.Month.Number,
            MonthNameRu = HijriDate.MonthNamesRu[monthIndex],
            MonthNameTransliterated = hijri.Month.En,
            Year = int.Parse(hijri.Year, CultureInfo.InvariantCulture),
            WeekdayRu = WeekdaysRu[(int)gregorianDate.DayOfWeek],
            IsSacredMonth = HijriDate.SacredMonthNumbers.Contains(hijri.Month.Number)
        };
    }
}
