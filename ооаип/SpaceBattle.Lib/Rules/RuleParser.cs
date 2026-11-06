using System.Globalization;

namespace SpaceBattle.Lib;

/// <summary>
/// Парсер текстовых (.txt) сценариев на основе управляющих токенов <see cref="RuleTokens"/>.
/// Формат: одна команда в строке, пустые строки и всё после '#' игнорируются,
/// токен регистронезависим, аргументы — целые числа через пробел.
/// Разбор останавливается на токене END. Все ошибки собираются и сообщаются разом.
/// </summary>
public static class RuleParser
{
    private static readonly IReadOnlyDictionary<string, int> ArgumentCounts =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [RuleTokens.Ship] = 4,
            [RuleTokens.Teleport] = 2,
            [RuleTokens.Move] = 0,
            [RuleTokens.Turn] = 0,
            [RuleTokens.End] = 0
        };

    public static IReadOnlyList<RuleEntry> ParseFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Parse(File.ReadAllLines(path, System.Text.Encoding.UTF8));
    }

    public static IReadOnlyList<RuleEntry> Parse(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var entries = new List<RuleEntry>();
        var errors = new List<string>();
        var lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var token = parts[0].ToUpperInvariant();

            if (!ArgumentCounts.TryGetValue(token, out var expectedCount))
            {
                errors.Add($"Строка {lineNumber}: неизвестный токен '{parts[0]}'.");
                continue;
            }

            if (parts.Length - 1 != expectedCount)
            {
                errors.Add($"Строка {lineNumber}: токен {token} ожидает аргументов: {expectedCount}, получено: {parts.Length - 1}.");
                continue;
            }

            if (!TryParseArguments(parts, out var arguments, out var badArgument))
            {
                errors.Add($"Строка {lineNumber}: аргумент '{badArgument}' не является целым числом.");
                continue;
            }

            entries.Add(new RuleEntry(lineNumber, token, arguments));

            if (token == RuleTokens.End)
            {
                break;
            }
        }

        if (errors.Count > 0)
        {
            throw new RuleParseException(errors);
        }

        return entries;
    }

    private static string StripComment(string line)
    {
        var index = line.IndexOf('#');
        return index < 0 ? line : line[..index];
    }

    private static bool TryParseArguments(string[] parts, out List<int> arguments, out string badArgument)
    {
        arguments = new List<int>();
        badArgument = string.Empty;

        foreach (var part in parts.Skip(1))
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                badArgument = part;
                return false;
            }

            arguments.Add(value);
        }

        return true;
    }
}
