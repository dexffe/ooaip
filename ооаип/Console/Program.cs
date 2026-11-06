namespace SpaceBattle.ConsoleApp;

internal static class Program
{
    private static void Main()
    {
        MatchRunner.Run(Console.In, Console.Out, GetLogPath());
    }

    private static string GetLogPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Console.csproj")))
            {
                return Path.Combine(directory.FullName, "match.log");
            }

            directory = directory.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "match.log");
    }
}
