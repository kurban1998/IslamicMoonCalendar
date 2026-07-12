namespace IslamicCalendar.Api.Models;

/// <summary>Запись хадиса в локальном файле Data/hadiths.json.</summary>
public class Hadith
{
    public int Id { get; set; }

    /// <summary>Например "Аль-Бухари" или "Муслим".</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Например "Китаб аль-Иман, № 1" — необязательное поле.</summary>
    public string? Reference { get; set; }

    public string TextRu { get; set; } = string.Empty;
}

/// <summary>Цитата дня — либо хадис, либо аят, показывается в карточке дня.</summary>
public class QuoteOfDay
{
    /// <summary>"hadith" или "ayah".</summary>
    public string Type { get; set; } = string.Empty;

    public string TextRu { get; set; } = string.Empty;

    /// <summary>Например "Аль-Бухари" или "Коран, сура «Аль-Бакара» (2:255)".</summary>
    public string Source { get; set; } = string.Empty;
}
