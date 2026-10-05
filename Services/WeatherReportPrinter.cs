using System.Globalization;
using WeatherTask.Models;

namespace WeatherTask.Services;

public static class WeatherReportPrinter
{
    public static void PrintCities(IEnumerable<WeatherData> data)
    {
        Console.WriteLine("=== Погода по городам ===");

        foreach (var item in data)
        {
            Console.WriteLine(
                $"{item.City}, {item.Country}: {FormatTemperature(item.CurrentTemperatureC)}");
        }
    }

    public static void PrintCountryStatistics(IEnumerable<WeatherData> data)
    {
        Console.WriteLine();
        Console.WriteLine("=== Статистика по странам ===");

        var groups = data
            .GroupBy(x => x.Country, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            int count = group.Count();
            double average = group.Average(x => x.CurrentTemperatureC);
            int min = group.Min(x => x.CurrentTemperatureC);
            int max = group.Max(x => x.CurrentTemperatureC);

            Console.WriteLine(
                $"{group.Key} — {count} {CityWord(count)}, " +
                $"avg: {FormatAverageTemperature(average)}, " +
                $"min: {FormatTemperature(min)}, " +
                $"max: {FormatTemperature(max)}");
        }
    }

    private static string CityWord(int count)
    {
        int lastTwo = count % 100;
        if (lastTwo is >= 11 and <= 14)
            return "cities";

        return count % 10 == 1 ? "city" : "cities";
    }

    private static string FormatTemperature(int value) =>
        $"{value.ToString("+0;-0;0", CultureInfo.InvariantCulture)} °C";

    private static string FormatAverageTemperature(double value)
    {
        string format = Math.Abs(value - Math.Round(value)) < 0.0001
            ? "+0;-0;0"
            : "+0.0;-0.0;0.0";

        return $"{value.ToString(format, CultureInfo.InvariantCulture)} °C";
    }
}
