using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace IslamicCalendar.Api.Services;

public interface IGeocodingService
{
    Task<string> GetLocationNameAsync(double latitude, double longitude, CancellationToken ct = default);
}

/// <summary>
/// Определяет название города по координатам. Пробует по очереди двух бесплатных
/// провайдеров без ключей: сперва BigDataCloud (быстрее и не блокирует облачные
/// IP), при неудаче — Nominatim (OpenStreetMap). Если оба не смогли — отдаёт
/// координаты как крайний случай. Результат кэшируется в памяти на 7 дней по
/// координатам, округлённым до ~1 км, чтобы не дёргать внешние API повторно
/// при каждом открытии карточки в одной и той же геолокации — это и быстрее,
/// и меньше шансов упереться в лимит запросов провайдера.
///
/// Если у вас по-прежнему выводятся координаты — смотрите Warning в логах:
/// там будет видно, что именно ответили оба провайдера.
/// </summary>
public class GeocodingService(
    BigDataCloudGeocodingProvider bigDataCloud,
    NominatimGeocodingProvider nominatim,
    IMemoryCache cache,
    ILogger<GeocodingService> logger) : IGeocodingService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

    public async Task<string> GetLocationNameAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var cacheKey = $"geo:{latitude.ToString("0.00", CultureInfo.InvariantCulture)}:{longitude.ToString("0.00", CultureInfo.InvariantCulture)}";

        if (cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        var name = await bigDataCloud.TryGetLocationNameAsync(latitude, longitude, ct)
            ?? await nominatim.TryGetLocationNameAsync(latitude, longitude, ct);

        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning(
                "Оба геокодера не смогли определить город для {Lat},{Lon} — показываю координаты",
                latitude, longitude);
            name = FormatCoordinates(latitude, longitude);
        }

        cache.Set(cacheKey, name, CacheDuration);
        return name;
    }

    private static string FormatCoordinates(double lat, double lon) =>
        $"{lat.ToString("0.00", CultureInfo.InvariantCulture)}°, {lon.ToString("0.00", CultureInfo.InvariantCulture)}°";
}
