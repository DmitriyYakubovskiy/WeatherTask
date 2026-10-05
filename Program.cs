using System.Text;
using WeatherTask.Models;
using WeatherTask.Services;

namespace WeatherTask;

internal static class Program
{
    private const string DefaultRemoteCityList = "https://gistpad.com/raw/vk-task-14";

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherTask/1.0");

        try
        {
            string source = ResolveCitySource(args);

            var cityLoader = new CityLoader(httpClient);
            IReadOnlyList<string> cities = await cityLoader.LoadAsync(source);

            if (cities.Count == 0)
            {
                Console.Error.WriteLine("Список городов пуст.");
                return 1;
            }

            Console.WriteLine($"Загружено уникальных городов: {cities.Count}");
            Console.WriteLine("Получение погоды...\n");

            var weatherClient = new WeatherClient(httpClient, maxConcurrency: 4);
            WeatherFetchSummary summary = await weatherClient.GetManyAsync(cities);

            WeatherReportPrinter.PrintCities(summary.Items);
            WeatherReportPrinter.PrintCountryStatistics(summary.Items);

            if (summary.Errors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Не удалось получить данные для некоторых городов:");
                foreach (var error in summary.Errors)
                    Console.WriteLine($"- {error.City}: {error.Message}");
            }

            return summary.Items.Count > 0 ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 1;
        }
    }

    private static string ResolveCitySource(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            return args[0];

        string localFile = Path.Combine(Environment.CurrentDirectory, "cities.txt");
        return File.Exists(localFile) ? localFile : DefaultRemoteCityList;
    }
}
