using System.Text.Json;
using IslamicCalendar.Api.Models;

namespace IslamicCalendar.Api.Services;

public interface IHadithService
{
    QuoteOfDay? GetRandomHadith(int seed);
}

/// <summary>
/// Читает хадисы из локального файла Data/hadiths.json (сборники Аль-Бухари и Муслима).
/// Файл — простой JSON-массив, пополняйте его по своему усмотрению.
/// </summary>
public class HadithService : IHadithService
{
    private readonly List<Hadith> _hadiths;
    private readonly ILogger<HadithService> _logger;

    public HadithService(IWebHostEnvironment env, ILogger<HadithService> logger)
    {
        _logger = logger;
        var path = Path.Combine(env.ContentRootPath, "Data", "hadiths.json");

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            _hadiths = JsonSerializer.Deserialize<List<Hadith>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        else
        {
            _logger.LogWarning("Файл с хадисами не найден: {Path}", path);
            _hadiths = [];
        }
    }

    public QuoteOfDay? GetRandomHadith(int seed)
    {
        if (_hadiths.Count == 0) return null;

        var rnd = new Random(seed);
        var hadith = _hadiths[rnd.Next(_hadiths.Count)];

        return new QuoteOfDay
        {
            Type = "hadith",
            TextRu = hadith.TextRu,
            Source = string.IsNullOrWhiteSpace(hadith.Reference)
                ? hadith.Source
                : $"{hadith.Source}, {hadith.Reference}"
        };
    }
}
