using System.Text;

namespace SpaceBattle.ConsoleApp;

/// <summary>
/// Потокобезопасный журнал матча. Файл хранит историю только последнего матча:
/// <see cref="StartMatch"/> обнуляет файл. Каждая запись — одна целая строка,
/// запись выполняется под общей блокировкой, поэтому строки из разных потоков не перемешиваются.
/// </summary>
public static class MatchLogger
{
    private static readonly object Sync = new();
    private static string? _currentPath;

    public static void StartMatch(string path)
    {
        lock (Sync)
        {
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, string.Empty, Encoding.UTF8);
            _currentPath = fullPath;
        }
    }

    public static void WriteCurrent(string message)
    {
        lock (Sync)
        {
            if (_currentPath is null)
            {
                throw new InvalidOperationException("Сначала необходимо начать матч вызовом StartMatch.");
            }

            WriteToFile(_currentPath, message);
        }
    }

    public static void Write(string path, string message)
    {
        lock (Sync)
        {
            WriteToFile(Path.GetFullPath(path), message);
        }
    }

    public static IReadOnlyList<string> Read(string path)
    {
        lock (Sync)
        {
            var fullPath = Path.GetFullPath(path);
            return File.Exists(fullPath)
                ? File.ReadAllLines(fullPath, Encoding.UTF8)
                : Array.Empty<string>();
        }
    }

    private static void WriteToFile(string path, string message)
    {
        // Переводы строк внутри сообщения заменяются, чтобы одна запись всегда занимала ровно одну строку.
        var singleLine = message.Replace("\r", " ").Replace("\n", " ");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {singleLine}{Environment.NewLine}";
        File.AppendAllText(path, line, Encoding.UTF8);
    }
}
