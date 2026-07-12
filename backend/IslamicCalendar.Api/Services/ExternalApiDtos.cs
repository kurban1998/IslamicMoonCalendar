using System.Text.Json.Serialization;

namespace IslamicCalendar.Api.Services;

// ---- Общая обёртка ответов Aladhan API и AlQuran Cloud API ----
// Оба сервиса возвращают { "code": 200, "status": "OK", "data": ... }

internal sealed class ApiEnvelope<T>
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("data")] public T? Data { get; set; }
}

// ---- Aladhan: конвертация дат (gToH / gToHCalendar) ----

internal sealed class AladhanDateEntry
{
    [JsonPropertyName("hijri")] public AladhanHijri Hijri { get; set; } = new();
    [JsonPropertyName("gregorian")] public AladhanGregorian Gregorian { get; set; } = new();
}

internal sealed class AladhanHijri
{
    [JsonPropertyName("day")] public string Day { get; set; } = "1";
    [JsonPropertyName("month")] public AladhanMonth Month { get; set; } = new();
    [JsonPropertyName("year")] public string Year { get; set; } = "1447";
}

internal sealed class AladhanMonth
{
    [JsonPropertyName("number")] public int Number { get; set; } = 1;
    [JsonPropertyName("en")] public string En { get; set; } = string.Empty;
}

internal sealed class AladhanGregorian
{
    /// <summary>Дата в формате DD-MM-YYYY.</summary>
    [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;
}

// ---- Aladhan: время намазов (timings) ----

internal sealed class AladhanTimingsData
{
    [JsonPropertyName("timings")] public AladhanTimings Timings { get; set; } = new();
}

internal sealed class AladhanTimings
{
    [JsonPropertyName("Fajr")] public string Fajr { get; set; } = string.Empty;
    [JsonPropertyName("Sunrise")] public string Sunrise { get; set; } = string.Empty;
    [JsonPropertyName("Dhuhr")] public string Dhuhr { get; set; } = string.Empty;
    [JsonPropertyName("Asr")] public string Asr { get; set; } = string.Empty;
    [JsonPropertyName("Maghrib")] public string Maghrib { get; set; } = string.Empty;
    [JsonPropertyName("Isha")] public string Isha { get; set; } = string.Empty;
}

// ---- AlQuran Cloud: случайный аят (ayah/{number}/{edition}) ----

internal sealed class AlquranAyahData
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("numberInSurah")] public int NumberInSurah { get; set; }
    [JsonPropertyName("surah")] public AlquranSurah Surah { get; set; } = new();
}

internal sealed class AlquranSurah
{
    [JsonPropertyName("number")] public int Number { get; set; } = 1;
    [JsonPropertyName("englishName")] public string EnglishName { get; set; } = string.Empty;
}
