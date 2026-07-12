namespace IslamicCalendar.Api.Models;

public class HijriDate
{
    public int Day { get; set; }
    public int Month { get; set; }
    public string MonthNameRu { get; set; } = string.Empty;
    public string MonthNameTransliterated { get; set; } = string.Empty;
    public int Year { get; set; }
    public string WeekdayRu { get; set; } = string.Empty;

    /// <summary>Названия 12 месяцев Хиджры на русском языке.</summary>
    public static readonly string[] MonthNamesRu =
    [
        "Мухаррам", "Сафар", "Раби‘ аль-авваль", "Раби‘ ас-сани",
        "Джумада аль-уля", "Джумада ас-сания", "Раджаб", "Ша‘бан",
        "Рамадан", "Шавваль", "Зуль-ка‘да", "Зуль-хиджа"
    ];
}
