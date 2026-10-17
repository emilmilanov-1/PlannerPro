using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace PlannerPro.Web.Services.Holidays;

public sealed class HolidayApiClient(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<HolidayApiClient> logger) : IHolidayApiClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    public async Task<HolidayFetchResult> GetHolidaysAsync(
        string countryCode,
        int year,
        CancellationToken cancellationToken = default)
    {
        var normalizedCountryCode = NormalizeCountryCode(countryCode);
        ValidateYear(year);
        var cacheKey = $"holidays:{normalizedCountryCode}:{year}";

        if (cache.TryGetValue<IReadOnlyList<HolidayDto>>(cacheKey, out var cachedHolidays) &&
            cachedHolidays is not null)
        {
            return new HolidayFetchResult(cachedHolidays, IsAvailable: true, FromCache: true);
        }

        try
        {
            using var response = await httpClient.GetAsync(
                $"Holidays/{normalizedCountryCode}/{year}",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var holidays = await response.Content.ReadFromJsonAsync<List<HolidayDto>>(
                cancellationToken: cancellationToken)
                ?? throw new JsonException("The holiday API returned an empty JSON document.");

            var nationalHolidays = holidays
                .Where(item => item.NationalHoliday)
                .OrderBy(item => item.Date)
                .ToArray();

            cache.Set(cacheKey, nationalHolidays, CacheDuration);
            return new HolidayFetchResult(nationalHolidays, IsAvailable: true, FromCache: false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "The holiday API timed out for {CountryCode} and {Year}.",
                normalizedCountryCode,
                year);
            return CreateFallbackResult("Услугата за официални празници не отговори навреме. Планерът ще продължи без празници.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "The holiday API request failed for {CountryCode} and {Year}.",
                normalizedCountryCode,
                year);
            return CreateFallbackResult("Официалните празници временно не могат да бъдат заредени. Планерът ще продължи без тях.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "The holiday API returned invalid JSON for {CountryCode} and {Year}.",
                normalizedCountryCode,
                year);
            return CreateFallbackResult("Получени са невалидни данни за празниците. Планерът ще продължи без тях.");
        }
    }

    private static HolidayFetchResult CreateFallbackResult(string warning) =>
        new([], IsAvailable: false, FromCache: false, warning);

    private static string NormalizeCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Кодът на държавата е задължителен.", nameof(countryCode));
        }

        var normalized = countryCode.Trim().ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new ArgumentException("Кодът на държавата трябва да съдържа две латински букви.", nameof(countryCode));
        }

        return normalized;
    }

    private static void ValidateYear(int year)
    {
        if (year is < 1970 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year), "Годината трябва да е между 1970 и 2100.");
        }
    }
}
