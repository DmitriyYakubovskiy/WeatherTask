using System.Globalization;
using System.Net;
using System.Text.Json;
using WeatherTask.Models;

namespace WeatherTask.Services;

public sealed class WeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WeatherClient(HttpClient httpClient, int maxConcurrency = 4)
    {
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        _httpClient = httpClient;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task<WeatherFetchSummary> GetManyAsync(
        IEnumerable<string> cities,
        CancellationToken cancellationToken = default)
    {
        var tasks = cities.Select(city => GetSafeAsync(city, cancellationToken));
        var outcomes = await Task.WhenAll(tasks);

        var items = outcomes
            .Where(x => x.Data is not null)
            .Select(x => x.Data!)
            .OrderBy(x => x.Country, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.City, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var errors = outcomes
            .Where(x => x.Error is not null)
            .Select(x => x.Error!)
            .ToArray();

        return new WeatherFetchSummary(items, errors);
    }

    private async Task<WeatherOutcome> GetSafeAsync(
        string city,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            try
            {
                WeatherData data = await GetAsync(city, cancellationToken);
                return new WeatherOutcome(data, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new WeatherOutcome(null, new WeatherError(city, ex.Message));
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<WeatherData> GetAsync(
        string city,
        CancellationToken cancellationToken)
    {
        string encodedCity = Uri.EscapeDataString(city);
        string url = $"https://wttr.in/{encodedCity}?format=j1";

        using HttpResponseMessage response = await SendWithRetryAsync(url, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        var payload = JsonSerializer.Deserialize<WttrResponse>(json, JsonOptions)
                      ?? throw new InvalidOperationException("API вернул пустой JSON.");

        string? tempText = payload.CurrentCondition?.FirstOrDefault()?.TempC;
        if (!int.TryParse(tempText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int temperature))
            throw new InvalidOperationException("В ответе API отсутствует текущая температура.");

        var area = payload.NearestArea?.FirstOrDefault();
        string canonicalCity = area?.AreaName?.FirstOrDefault()?.Value?.Trim() ?? city;
        string country = area?.Country?.FirstOrDefault()?.Value?.Trim() ?? "Unknown";

        return new WeatherData(canonicalCity, temperature, country);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string url,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
                return response;

            bool canRetry = response.StatusCode == HttpStatusCode.TooManyRequests
                            || (int)response.StatusCode >= 500;

            if (!canRetry || attempt == maxAttempts)
            {
                string status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                response.Dispose();
                throw new HttpRequestException($"wttr.in вернул {status}.");
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
        }

        throw new InvalidOperationException("Не удалось выполнить запрос к wttr.in.");
    }

    private sealed record WeatherOutcome(WeatherData? Data, WeatherError? Error);
}
