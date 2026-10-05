namespace WeatherTask.Services;

public sealed class CityLoader
{
    private readonly HttpClient _httpClient;

    public CityLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<string>> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        string content;

        if (File.Exists(source))
        {
            content = await File.ReadAllTextAsync(source, cancellationToken);
        }
        else if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
                 && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            content = await _httpClient.GetStringAsync(uri, cancellationToken);
        }
        else
        {
            throw new FileNotFoundException(
                $"Не найден файл со списком городов: {source}");
        }

        return ParseCities(content);
    }

    private static IReadOnlyList<string> ParseCities(string content)
    {
        return content
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(city => city.Trim().Trim('\uFEFF'))
            .Where(city => city.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
