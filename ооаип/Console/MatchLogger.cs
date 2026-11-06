using System.Text;

namespace SpaceBattle.ConsoleApp;

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
        var singleLine = message.Replace("\r", " ").Replace("\n", " ");
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {singleLine}{Environment.NewLine}";
        File.AppendAllText(path, line, Encoding.UTF8);
    }
}
