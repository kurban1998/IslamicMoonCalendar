using System.Net.Http.Json;
using IslamicCalendar.Api.Models;
using Microsoft.Extensions.Options;

namespace IslamicCalendar.Api.Services;

public interface IQuranService
{
    Task<QuoteOfDay?> GetRandomAyahAsync(int seed, CancellationToken ct = default);
}

/// <summary>
/// Получает случайный аят с переводом через api.alquran.cloud
/// (https://alquran.cloud/api — бесплатный, без ключа).
/// </summary>
public class QuranService(HttpClient http, IOptions<IslamicCalendarOptions> options) : IQuranService
{
    private readonly IslamicCalendarOptions _options = options.Value;

    // Русские названия 114 сур. Стандартные транслитерации (перевод Кулиева/Османова).
    // Можно поправить по своему вкусу.
    private static readonly string[] SurahNamesRu =
    [
        "Аль-Фатиха", "Аль-Бакара", "Али Имран", "Ан-Ниса", "Аль-Маида", "Аль-Анам",
        "Аль-Араф", "Аль-Анфаль", "Ат-Тауба", "Юнус", "Худ", "Юсуф", "Ар-Раад",
        "Ибрахим", "Аль-Хиджр", "Ан-Нахль", "Аль-Исра", "Аль-Кахф", "Марьям", "Та-Ха",
        "Аль-Анбия", "Аль-Хадж", "Аль-Муминун", "Ан-Нур", "Аль-Фуркан", "Аш-Шуара",
        "Ан-Намль", "Аль-Касас", "Аль-Анкабут", "Ар-Рум", "Лукман", "Ас-Саджда",
        "Аль-Ахзаб", "Саба", "Фатыр", "Йа-Син", "Ас-Саффат", "Сад", "Аз-Зумар",
        "Гафир", "Фуссылат", "Аш-Шура", "Аз-Зухруф", "Ад-Духан", "Аль-Джасия",
        "Аль-Ахкаф", "Мухаммад", "Аль-Фатх", "Аль-Худжурат", "Каф", "Аз-Зарият",
        "Ат-Тур", "Ан-Наджм", "Аль-Камар", "Ар-Рахман", "Аль-Вакиа", "Аль-Хадид",
        "Аль-Муджадала", "Аль-Хашр", "Аль-Мумтахана", "Ас-Сафф", "Аль-Джума",
        "Аль-Мунафикун", "Ат-Тагабун", "Ат-Талак", "Ат-Тахрим", "Аль-Мульк",
        "Аль-Калям", "Аль-Хакка", "Аль-Мааридж", "Нух", "Аль-Джинн", "Аль-Муззаммиль",
        "Аль-Муддассир", "Аль-Кияма", "Аль-Инсан", "Аль-Мурсалят", "Ан-Наба",
        "Ан-Назиат", "Абаса", "Ат-Таквир", "Аль-Инфитар", "Аль-Мутаффифин",
        "Аль-Иншикак", "Аль-Бурудж", "Ат-Тарик", "Аль-Аля", "Аль-Гашия", "Аль-Фаджр",
        "Аль-Балад", "Аш-Шамс", "Аль-Лейль", "Ад-Духа", "Аш-Шарх", "Ат-Тин",
        "Аль-Аляк", "Аль-Кадр", "Аль-Баййина", "Аз-Зальзаля", "Аль-Адият",
        "Аль-Кариа", "Ат-Такасур", "Аль-Аср", "Аль-Хумаза", "Аль-Филь", "Курайш",
        "Аль-Мауна", "Аль-Каусар", "Аль-Кафирун", "Ан-Наср", "Аль-Масад",
        "Аль-Ихлас", "Аль-Фаляк", "Ан-Нас"
    ];

    public async Task<QuoteOfDay?> GetRandomAyahAsync(int seed, CancellationToken ct = default)
    {
        try
        {
            var rnd = new Random(seed);
            var ayahNumber = rnd.Next(1, 6237); // в Коране 6236 аятов (сквозная нумерация)

            var url = $"ayah/{ayahNumber}/{_options.QuranTranslationEdition}";
            var response = await http.GetFromJsonAsync<ApiEnvelope<AlquranAyahData>>(url, ct);

            if (response?.Data is null) return null;

            var surahIndex = Math.Clamp(response.Data.Surah.Number - 1, 0, SurahNamesRu.Length - 1);
            var surahName = SurahNamesRu[surahIndex];

            return new QuoteOfDay
            {
                Type = "ayah",
                TextRu = response.Data.Text.Trim(),
                Source = $"Коран, сура «{surahName}» ({response.Data.Surah.Number}:{response.Data.NumberInSurah})"
            };
        }
        catch
        {
            // При сбое внешнего API вызывающий код (QuoteOfDayService) подстрахуется хадисом
            return null;
        }
    }
}
