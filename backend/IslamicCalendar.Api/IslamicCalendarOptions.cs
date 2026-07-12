namespace IslamicCalendar.Api;

/// <summary>
/// Настройки приложения, привязанные к секции "IslamicCalendar" в appsettings.json.
/// </summary>
public class IslamicCalendarOptions
{
    public const string SectionName = "IslamicCalendar";

    public string AladhanBaseUrl { get; set; } = "https://api.aladhan.com/v1/";
    public string AlQuranBaseUrl { get; set; } = "https://api.alquran.cloud/v1/";

    /// <summary>
    /// Метод расчёта времени молитв Aladhan API.
    /// 2 = ISNA, 3 = Muslim World League, 14 = Spiritual Administration of Muslims of Russia.
    /// Полный список: https://aladhan.com/calculation-methods
    /// </summary>
    public int PrayerCalculationMethod { get; set; } = 2;

    /// <summary>
    /// Издание перевода Корана на api.alquran.cloud (ru.kuliev — перевод Кулиева).
    /// </summary>
    public string QuranTranslationEdition { get; set; } = "ru.kuliev";

    public double DefaultLatitude { get; set; } = 55.7558;
    public double DefaultLongitude { get; set; } = 37.6173;
    public string DefaultCityName { get; set; } = "Москва";
}
