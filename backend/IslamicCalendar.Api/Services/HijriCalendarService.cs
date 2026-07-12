using System.Globalization;
using System.Net.Http.Json;
using IslamicCalendar.Api.Models;

namespace IslamicCalendar.Api.Services;

public interface IHijriCalendarService
{
    Task<HijriDate> ConvertAsync(DateOnly date, CancellationToken ct = default);
    Task<List<MonthCalendarDay>> GetMonthAsync(int year, int month, CancellationToken ct = default);
}

/// <summary>
/// Получает соответствие григорианских и хиджра-дат через Aladhan API.
/// https://aladhan.com/islamic-calendar-api
/// </summary>
public class HijriCalendarService(HttpClient http, ILogger<HijriCalendarService> logger) : IHijriCalendarService
{
    private static readonly string[] WeekdaysRu =
        ["Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота"];

    public async Task<HijriDate> ConvertAsync(DateOnly date, CancellationToken ct = default)
    {
        var dateStr = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

        var response = await http.GetFromJsonAsync<ApiEnvelope<AladhanDateEntry>>(
            $"gToH?date={dateStr}", ct);

        if (response?.Data is null)
        {
            logger.LogWarning("Aladhan gToH не вернул данные для даты {Date}", dateStr);
            throw new InvalidOperationException("Не удалось получить дату по Хиджре из Aladhan API");
        }

        return MapHijri(response.Data.Hijri, date);
    }

    public async Task<List<MonthCalendarDay>> GetMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<ApiEnvelope<List<AladhanDateEntry>>>(
            $"gToHCalendar/{month}/{year}", ct);

        if (response?.Data is null)
        {
            logger.LogWarning("Aladhan gToHCalendar не вернул данные для {Month}/{Year}", month, year);
            throw new InvalidOperationException("Не удалось получить календарь по Хиджре из Aladhan API");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return response.Data.Select(entry =>
        {
            var gDate = DateOnly.ParseExact(entry.Gregorian.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            return new MonthCalendarDay
            {
                GregorianDate = gDate,
                Hijri = MapHijri(entry.Hijri, gDate),
                IsFriday = gDate.DayOfWeek == DayOfWeek.Friday,
                IsToday = gDate == today
            };
        }).ToList();
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
