using System.Globalization;
using System.Text.Json;

namespace IslamicCalendar.Api.Services;

/// <summary>
/// Обратное геокодирование через BigDataCloud (https://www.bigdatacloud.com/geocoding-apis) —
/// бесплатный "client" endpoint без ключа, без строгих ограничений на IP облачных
/// хостингов (в отличие от Nominatim). Выбран основным провайдером именно поэтому —
/// на многих PaaS/облачных хостингах Nominatim молча блокирует или режет запросы.
/// </summary>
public class BigDataCloudGeocodingProvider(HttpClient http, ILogger<BigDataCloudGeocodingProvider> logger)
{
    public async Task<string?> TryGetLocationNameAsync(double latitude, double longitude, CancellationToken ct)
    {
        var url = $"reverse-geocode-client?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&localityLanguage=ru";

        try
        {
            using var httpResponse = await http.GetAsync(url, ct);
            var rawJson = await httpResponse.Content.ReadAsStringAsync(ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("BigDataCloud вернул {StatusCode} для {Lat},{Lon}: {Body}",
                    httpResponse.StatusCode, latitude, longitude, Truncate(rawJson));
                return null;
            }

            var data = JsonSerializer.Deserialize<BigDataCloudResponse>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var name = FirstNonEmpty(data?.City, data?.Locality, data?.PrincipalSubdivision);

            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogWarning("BigDataCloud не вернул название места для {Lat},{Lon}: {Body}",
                    latitude, longitude, Truncate(rawJson));
            }

            return name;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка запроса к BigDataCloud для {Lat},{Lon}", latitude, longitude);
            return null;
        }
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string Truncate(string s) => s.Length > 400 ? s[..400] + "…" : s;

    private sealed class BigDataCloudResponse
    {
        public string? City { get; set; }
        public string? Locality { get; set; }
        public string? PrincipalSubdivision { get; set; }
    }
}
