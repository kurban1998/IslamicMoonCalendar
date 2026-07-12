using IslamicCalendar.Api.Models;

namespace IslamicCalendar.Api.Services;

public interface IQuoteOfDayService
{
    Task<QuoteOfDay> GetQuoteAsync(DateOnly date, CancellationToken ct = default);
}

/// <summary>
/// Выбирает цитату дня — хадис или аят — детерминированно по дате
/// (одна и та же дата всегда показывает одну и ту же цитату).
/// Если внешний API Корана недоступен, подстраховывается хадисом из локального файла.
/// </summary>
public class QuoteOfDayService(IQuranService quranService, IHadithService hadithService) : IQuoteOfDayService
{
    public async Task<QuoteOfDay> GetQuoteAsync(DateOnly date, CancellationToken ct = default)
    {
        var seed = date.DayNumber;
        var preferAyah = seed % 2 == 0;

        var quote = preferAyah
            ? await quranService.GetRandomAyahAsync(seed, ct)
            : hadithService.GetRandomHadith(seed);

        quote ??= preferAyah
            ? hadithService.GetRandomHadith(seed)
            : await quranService.GetRandomAyahAsync(seed, ct);

        return quote ?? new QuoteOfDay
        {
            Type = "hadith",
            TextRu = "Дела оцениваются по намерениям, и каждому человеку достанется лишь то, что он намеревался обрести.",
            Source = "Аль-Бухари, Муслим"
        };
    }
}
