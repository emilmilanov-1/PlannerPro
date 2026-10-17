using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using PlannerPro.Web.Services.Holidays;

namespace PlannerPro.Tests;

public class HolidayApiClientTests
{
    [Fact]
    public async Task GetHolidaysAsync_DeserializesAndSortsNationalHolidays()
    {
        const string json = """
            [
              { "date": "2026-03-03", "name": "Liberation Day", "countryCode": "BG", "nationalHoliday": true, "holidayTypes": ["Public"] },
              { "date": "2026-01-01", "name": "New Year's Day", "countryCode": "BG", "nationalHoliday": true, "holidayTypes": ["Public"] },
              { "date": "2026-02-14", "name": "Regional Day", "countryCode": "BG", "nationalHoliday": false, "holidayTypes": ["Observance"] }
            ]
            """;
        var handler = new StubHttpMessageHandler((_, _) => JsonResponse(json));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var result = await client.GetHolidaysAsync("bg", 2026);

        Assert.True(result.IsAvailable);
        Assert.False(result.FromCache);
        Assert.Equal(2, result.Holidays.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Holidays[0].Date);
        Assert.Equal("Liberation Day", result.Holidays[1].Name);
    }

    [Fact]
    public async Task GetHolidaysAsync_UsesCacheForSameCountryAndYear()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonResponse("[]"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var first = await client.GetHolidaysAsync("BG", 2026);
        var second = await client.GetHolidaysAsync("BG", 2026);

        Assert.False(first.FromCache);
        Assert.True(second.FromCache);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetHolidaysAsync_UsesExpectedEndpointAndHeaders()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return JsonResponse("[]");
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        await client.GetHolidaysAsync("bg", 2026);

        Assert.Equal("https://nagerholidays.com/api/v4/Holidays/BG/2026", capturedRequest?.RequestUri?.ToString());
        Assert.Contains("application/json", capturedRequest?.Headers.Accept.ToString());
        Assert.Contains("PlannerPro/1.0", capturedRequest?.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task GetHolidaysAsync_ReturnsFallbackOnHttpError()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var result = await client.GetHolidaysAsync("BG", 2026);

        Assert.False(result.IsAvailable);
        Assert.Empty(result.Holidays);
        Assert.Contains("временно", result.Warning);
    }

    [Fact]
    public async Task GetHolidaysAsync_ReturnsFallbackOnInvalidJson()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonResponse("not-json"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var result = await client.GetHolidaysAsync("BG", 2026);

        Assert.False(result.IsAvailable);
        Assert.Empty(result.Holidays);
        Assert.Contains("невалидни данни", result.Warning);
    }

    [Fact]
    public async Task GetHolidaysAsync_ReturnsFallbackOnTimeout()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new TaskCanceledException("timeout"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        var result = await client.GetHolidaysAsync("BG", 2026);

        Assert.False(result.IsAvailable);
        Assert.Contains("не отговори навреме", result.Warning);
    }

    [Fact]
    public async Task GetHolidaysAsync_PropagatesCallerCancellation()
    {
        var handler = new StubHttpMessageHandler((_, cancellationToken) =>
            throw new OperationCanceledException(cancellationToken));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetHolidaysAsync("BG", 2026, cancellationSource.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("B")]
    [InlineData("123")]
    public async Task GetHolidaysAsync_RejectsInvalidCountryCode(string countryCode)
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonResponse("[]"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(handler, cache);

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetHolidaysAsync(countryCode, 2026));
        Assert.Equal(0, handler.RequestCount);
    }

    private static HolidayApiClient CreateClient(HttpMessageHandler handler, IMemoryCache cache)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://nagerholidays.com/api/v4/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlannerPro/1.0");

        return new HolidayApiClient(httpClient, cache, NullLogger<HolidayApiClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request, cancellationToken));
        }
    }
}
