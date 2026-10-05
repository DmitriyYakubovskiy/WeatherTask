using System.Text.Json.Serialization;

namespace WeatherTask.Models;

internal sealed class WttrResponse
{
    [JsonPropertyName("current_condition")]
    public List<WttrCurrentCondition>? CurrentCondition { get; init; }

    [JsonPropertyName("nearest_area")]
    public List<WttrNearestArea>? NearestArea { get; init; }
}

internal sealed class WttrCurrentCondition
{
    [JsonPropertyName("temp_C")]
    public string? TempC { get; init; }
}

internal sealed class WttrNearestArea
{
    [JsonPropertyName("areaName")]
    public List<WttrValue>? AreaName { get; init; }

    [JsonPropertyName("country")]
    public List<WttrValue>? Country { get; init; }
}

internal sealed class WttrValue
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
