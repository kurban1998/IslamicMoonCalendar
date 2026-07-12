namespace IslamicCalendar.Api.Models;

public class HijriDate
{
    public int Day { get; set; }
    public int Month { get; set; }
    public string MonthNameRu { get; set; } = string.Empty;
    public string MonthNameTransliterated { get; set; } = string.Empty;
    public int Year { get; set; }
    public string WeekdayRu { get; set; } = string.Empty;

    /// <summary>
    /// true для 4 запретных (священных) месяцев по Хиджре: Мухаррам, Раджаб,
    /// Зуль-Каада, Зуль-Хиджа.
    /// </summary>
    public bool IsSacredMonth { get; set; }

    /// <summary>Номера 4 запретных месяцев: Мухаррам(1), Раджаб(7), Зуль-Каада(11), Зуль-Хиджа(12).</summary>
    public static readonly int[] SacredMonthNumbers = [1, 7, 11, 12];

    /// <summary>Названия 12 месяцев Хиджры на русском языке.</summary>
    public static readonly string[] MonthNamesRu =
    [
        "Мухаррам", "Сафар", "Раби‘ аль-авваль", "Раби‘ ас-сани",
        "Джумада аль-уля", "Джумада ас-сания", "Раджаб", "Ша‘бан",
        "Рамадан", "Шавваль", "Зуль-ка‘да", "Зуль-хиджа"
    ];
}
