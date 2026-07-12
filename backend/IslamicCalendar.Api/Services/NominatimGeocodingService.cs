using System.Globalization;
using System.Text.Json;

namespace IslamicCalendar.Api.Services;

public interface IGeocodingService
{
    Task<string> GetLocationNameAsync(double latitude, double longitude, CancellationToken ct = default);
}

/// <summary>
/// Определяет название города по координатам через Nominatim (OpenStreetMap) —
/// бесплатный сервис без ключа. https://nominatim.org/release-docs/latest/api/Reverse/
///
/// Важно: политика использования Nominatim требует указывать осмысленный
/// User-Agent/контакт и не превышать 1 запрос в секунду — см. регистрацию
/// HttpClient в Program.cs. Для продакшена с заметной нагрузкой лучше поднять
/// свой инстанс Nominatim или использовать платный геокодер.
///
/// Если название города всё равно не определяется (возвращаются координаты),
/// смотрите Warning в логах — там будет причина: либо Nominatim вернул ошибку
/// (часто 403/429 — лимит запросов или блокировка дата-центровых IP облачных
/// хостингов), либо в ответе просто нет ни одного из ожидаемых полей адреса
/// для этой точки.
/// </summary>
public class NominatimGeocodingService(HttpClient http, ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    public async Task<string> GetLocationNameAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var url = $"reverse?format=jsonv2" +
                   $"&lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&accept-language=ru&zoom=12&addressdetails=1";

        string rawJson;
        try
        {
            // Сначала читаем как текст — если Nominatim вернёт ошибку не в JSON
            // (например, HTML-страницу при 403), это будет видно в логах
            using var httpResponse = await http.GetAsync(url, ct);
            rawJson = await httpResponse.Content.ReadAsStringAsync(ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Nominatim вернул {StatusCode} для координат {Lat},{Lon}. Тело ответа: {Body}",
                    httpResponse.StatusCode, latitude, longitude, Truncate(rawJson));
                return FormatCoordinates(latitude, longitude);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось выполнить запрос к Nominatim для координат {Lat},{Lon}", latitude, longitude);
            return FormatCoordinates(latitude, longitude);
        }

        try
        {
            var response = JsonSerializer.Deserialize<NominatimReverseResponse>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var address = response?.Address;

            // Расширенный список полей: для маленьких населённых пунктов и окраин
            // мегаполисов "city" в ответе Nominatim часто отсутствует
            var name = address?.City
                ?? address?.Town
                ?? address?.Village
                ?? address?.Municipality
                ?? address?.CityDistrict
                ?? address?.Suburb
                ?? address?.Hamlet
                ?? address?.County
                ?? address?.State;

            // Если ни одного именованного поля нет — берём первый фрагмент
            // человекочитаемого display_name вместо голых координат
            if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(response?.DisplayName))
            {
                name = response.DisplayName.Split(',')[0].Trim();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogWarning(
                    "Nominatim ответил без пригодного названия места для {Lat},{Lon}. Сырой ответ: {Body}",
                    latitude, longitude, Truncate(rawJson));
                return FormatCoordinates(latitude, longitude);
            }

            return name;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Не удалось разобрать ответ Nominatim для координат {Lat},{Lon}. Сырой ответ: {Body}",
                latitude, longitude, Truncate(rawJson));
            return FormatCoordinates(latitude, longitude);
        }
    }

    private static string Truncate(string s) => s.Length > 500 ? s[..500] + "…" : s;

    private static string FormatCoordinates(double lat, double lon) =>
        $"{lat.ToString("0.00", CultureInfo.InvariantCulture)}°, {lon.ToString("0.00", CultureInfo.InvariantCulture)}°";
}
