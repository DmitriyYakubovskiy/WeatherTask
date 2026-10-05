# WeatherTask

Консольное приложение на **C# / .NET 8** для второго тестового задания: программа загружает список городов, получает текущую погоду через `wttr.in`, выводит данные по каждому городу и считает статистику по странам.

Проект использует только стандартную библиотеку .NET. Для JSON используется `System.Text.Json`. Сторонних NuGet-пакетов нет.

## Что делает программа

1. Загружает список городов из файла или по URL.
2. Удаляет повторяющиеся города без учёта регистра.
3. Для каждого уникального города выполняет запрос:

```text
https://wttr.in/{City}?format=j1
```

4. Разбирает JSON и создаёт объект `WeatherData`:

```csharp
public sealed record WeatherData(
    string City,
    int CurrentTemperatureC,
    string Country);
```

5. Выводит погоду для каждого города, например:

```text
Tokyo, Japan: +18 °C
```

6. Группирует результаты по странам и вычисляет кодом:
   - количество городов;
   - среднюю текущую температуру;
   - минимальную температуру;
   - максимальную температуру.

Пример:

```text
Japan — 3 cities, avg: +19 °C, min: +16 °C, max: +22 °C
```

## Требования

- Windows / Linux / macOS;
- .NET 8 SDK;
- доступ в интернет для запросов к `wttr.in`;
- файл со списком городов или доступ к источнику из задания.

Проверить .NET:

```powershell
dotnet --version
```

Если команда не найдена, установите **.NET 8 SDK** с официального сайта Microsoft.

## Быстрый запуск

### Вариант 1 — файл `cities.txt`

Положите файл со списком городов рядом с `WeatherTask.csproj` и назовите его:

```text
cities.txt
```

Пример содержимого:

```text
Tokyo
Osaka
London
Berlin
Tokyo
```

Повторяющиеся города допустимы: программа сама оставит только уникальные.

Откройте терминал в папке проекта и выполните:

```powershell
dotnet run -c Release
```

### Вариант 2 — указать путь к файлу явно

```powershell
dotnet run -c Release -- "C:\Temp\cities.txt"
```

### Вариант 3 — загрузить список напрямую по URL

Можно передать прямую ссылку на текстовый файл:

```powershell
dotnet run -c Release -- "https://gistpad.com/raw/vk-task-14"
```

Если аргументы не указаны, программа сначала ищет `cities.txt` в текущей папке. Если его нет, используется второй источник из условия:

```text
https://gistpad.com/raw/vk-task-14
```

## Пример результата

Конкретные температуры зависят от текущей погоды.

```text
Загружено уникальных городов: 5
Получение погоды...

=== Погода по городам ===
Berlin, Germany: +12 °C
London, United Kingdom: +14 °C
Osaka, Japan: +21 °C
Paris, France: +15 °C
Tokyo, Japan: +19 °C

=== Статистика по странам ===
France — 1 city, avg: +15 °C, min: +15 °C, max: +15 °C
Germany — 1 city, avg: +12 °C, min: +12 °C, max: +12 °C
Japan — 2 cities, avg: +20 °C, min: +19 °C, max: +21 °C
United Kingdom — 1 city, avg: +14 °C, min: +14 °C, max: +14 °C
```

Все числа в блоке статистики рассчитываются программой из ответов API. Никаких заранее записанных температур или итоговых значений в проекте нет.

## Структура проекта

```text
WeatherTask/
├── Models/
│   ├── WeatherData.cs
│   └── WttrResponse.cs
├── Services/
│   ├── CityLoader.cs
│   ├── WeatherClient.cs
│   └── WeatherReportPrinter.cs
├── Program.cs
├── WeatherTask.csproj
├── cities.example.txt
└── README.md
```

### `Program.cs`

Точка входа. Определяет источник списка городов, запускает загрузку погоды и вывод отчёта.

### `CityLoader.cs`

Читает города либо из локального файла, либо по HTTP. После чтения:

- удаляет пустые строки;
- убирает пробелы по краям;
- удаляет дубликаты без учёта регистра.

### `WeatherClient.cs`

Отвечает за API `wttr.in`.

Для города `Tokyo` выполняется запрос:

```text
https://wttr.in/Tokyo?format=j1
```

Город кодируется через `Uri.EscapeDataString`, поэтому пробелы и специальные символы безопасно попадают в URL.

Одновременно выполняется не более четырёх запросов. Это сделано через стандартный `SemaphoreSlim`, чтобы не создавать десятки запросов к бесплатному сервису одновременно.

При ответах `429` или ошибках сервера `5xx` запрос может быть повторён до трёх раз.

### `WttrResponse.cs`

Внутренние DTO для десериализации нужной части JSON `wttr.in` через `System.Text.Json`.

Используются поля:

```text
current_condition[0].temp_C
nearest_area[0].areaName[0].value
nearest_area[0].country[0].value
```

Остальная часть большого ответа API приложению не нужна.

### `WeatherData.cs`

Основная модель задания:

```csharp
public sealed record WeatherData(
    string City,
    int CurrentTemperatureC,
    string Country);
```

### `WeatherReportPrinter.cs`

Выводит результаты и вычисляет статистику с помощью LINQ:

```csharp
GroupBy(...)
Average(...)
Min(...)
Max(...)
```

## Почему `System.Text.Json`

По условию для C# разрешено использовать `System.Text.Json` либо `Newtonsoft.Json`.

В проекте выбран `System.Text.Json`, потому что он входит в .NET и не требует установки внешнего NuGet-пакета. Это одновременно выполняет ограничение задания на отсутствие других внешних пакетов.

## Обработка ошибок

Ошибка получения одного города не останавливает всё приложение.

Например, если API не смог обработать один город:

```text
Не удалось получить данные для некоторых городов:
- InvalidCity: wttr.in вернул 404 Not Found.
```

Остальные города всё равно попадут в отчёт и статистику.

## Важное замечание об API

`wttr.in` — внешний публичный сервис. Поэтому для запуска задания нужен интернет, а скорость выполнения зависит от доступности сервиса.

Если API временно отвечает `429 Too Many Requests` или `5xx`, приложение автоматически выполняет повторные попытки.

## Сборка без запуска

```powershell
dotnet build -c Release
```

Собранное приложение появится примерно здесь:

```text
bin\Release\net8.0\
```

## Публикация

Например, для Windows x64:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

## Соответствие условию

| Требование | Реализация |
|---|---|
| загрузить города из файла в память | `CityLoader` |
| использовать только уникальные города | `Distinct(StringComparer.OrdinalIgnoreCase)` |
| получить данные через `wttr.in/{City}?format=j1` | `WeatherClient` |
| модель `WeatherData` | `WeatherData.cs` |
| город | `WeatherData.City` |
| температура в °C | `WeatherData.CurrentTemperatureC` |
| страна | `WeatherData.Country` |
| вывести каждый город | `PrintCities` |
| сгруппировать по странам | LINQ `GroupBy` |
| количество городов | `Count()` |
| средняя температура | `Average()` |
| минимум | `Min()` |
| максимум | `Max()` |
| JSON | `System.Text.Json` |
| без сторонних пакетов | NuGet-зависимостей нет |
| вычисления выполняются кодом | статистика рассчитывается во время выполнения |
