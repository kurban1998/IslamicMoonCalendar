namespace IslamicCalendar.Api.Models;

/// <summary>Полные данные для карточки одного дня, которые запрашивает фронтенд.</summary>
public class DayCardResponse
{
    public DateOnly GregorianDate { get; set; }
    public HijriDate Hijri { get; set; } = new();
    public MoonPhaseInfo Moon { get; set; } = new();
    public PrayerTimes Prayers { get; set; } = new();
    public QuoteOfDay Quote { get; set; } = new();
    public FridayInfo Friday { get; set; } = new();
}
