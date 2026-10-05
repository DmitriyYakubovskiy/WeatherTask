namespace WeatherTask.Models;

public sealed record WeatherData(
    string City,
    int CurrentTemperatureC,
    string Country);

public sealed record WeatherError(string City, string Message);

public sealed record WeatherFetchSummary(
    IReadOnlyList<WeatherData> Items,
    IReadOnlyList<WeatherError> Errors);
