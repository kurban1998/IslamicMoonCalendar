using System.Globalization;
using System.Net.Http.Json;

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
/// </summary>
public class NominatimGeocodingService(HttpClient http, ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    public async Task<string> GetLocationNameAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var url = $"reverse?format=jsonv2" +
                       $"&lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                       $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}" +
                       $"&accept-language=ru&zoom=10";

            var response = await http.GetFromJsonAsync<NominatimReverseResponse>(url, ct);
            var address = response?.Address;

            var name = address?.City ?? address?.Town ?? address?.Village
                ?? address?.Municipality ?? address?.County ?? address?.State;

            return string.IsNullOrWhiteSpace(name) ? FormatCoordinates(latitude, longitude) : name;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось определить город по координатам {Lat},{Lon}", latitude, longitude);
            return FormatCoordinates(latitude, longitude);
        }
    }

    private static string FormatCoordinates(double lat, double lon) =>
        $"{lat.ToString("0.00", CultureInfo.InvariantCulture)}°, {lon.ToString("0.00", CultureInfo.InvariantCulture)}°";
}
