namespace IslamicCalendar.Api.Models;

/// <summary>Одна ячейка в сетке календаря месяца.</summary>
public class MonthCalendarDay
{
    public DateOnly GregorianDate { get; set; }
    public HijriDate Hijri { get; set; } = new();
    public bool IsFriday { get; set; }
    public bool IsToday { get; set; }
}
