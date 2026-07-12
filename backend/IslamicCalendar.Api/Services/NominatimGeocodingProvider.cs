using System.Globalization;
using System.Text.Json;

namespace IslamicCalendar.Api.Services;

/// <summary>
/// Обратное геокодирование через Nominatim (OpenStreetMap) — резервный провайдер,
/// используется только если BigDataCloud не смог определить название места.
/// https://nominatim.org/release-docs/latest/api/Reverse/
/// </summary>
public class NominatimGeocodingProvider(HttpClient http, ILogger<NominatimGeocodingProvider> logger)
{
    public async Task<string?> TryGetLocationNameAsync(double latitude, double longitude, CancellationToken ct)
    {
        var url = $"reverse?format=jsonv2" +
                   $"&lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}" +
                   $"&accept-language=ru&zoom=12&addressdetails=1";

        try
        {
            using var httpResponse = await http.GetAsync(url, ct);
            var rawJson = await httpResponse.Content.ReadAsStringAsync(ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Nominatim вернул {StatusCode} для {Lat},{Lon}: {Body}",
                    httpResponse.StatusCode, latitude, longitude, Truncate(rawJson));
                return null;
            }

            var response = JsonSerializer.Deserialize<NominatimReverseResponse>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var address = response?.Address;
            var name = address?.City ?? address?.Town ?? address?.Village
                ?? address?.Municipality ?? address?.CityDistrict ?? address?.Suburb
                ?? address?.Hamlet ?? address?.County ?? address?.State;

            if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(response?.DisplayName))
                name = response.DisplayName.Split(',')[0].Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                logger.LogWarning("Nominatim не вернул название места для {Lat},{Lon}: {Body}",
                    latitude, longitude, Truncate(rawJson));
                return null;
            }

            return name;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка запроса к Nominatim для {Lat},{Lon}", latitude, longitude);
            return null;
        }
    }

    private static string Truncate(string s) => s.Length > 400 ? s[..400] + "…" : s;
}
