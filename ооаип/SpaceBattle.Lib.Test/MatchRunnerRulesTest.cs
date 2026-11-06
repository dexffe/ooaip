using SpaceBattle.ConsoleApp;
namespace SpaceBattle.Lib.Test;

public class MatchRunnerRulesTest
{
    private static string TempPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"spacebattle-{Guid.NewGuid():N}{extension}");

    [Fact]
    public void RulesFileDrivesMatchThroughAdaptersAndLogsEveryStep()
    {
        var rulesPath = TempPath(".txt");
        var logPath = TempPath(".log");
        try
        {
            File.WriteAllLines(rulesPath, new[]
            {
                "SHIP 0 0 2 3",
                "MOVE",
                "TURN",
                "TELEPORT 100 100",
                "MOVE",
                "TURN",
                "END"
            });
            MatchLogger.Write(logPath, "запись предыдущего матча");
            using var output = new StringWriter();

            MatchRunner.RunRules(RuleParser.ParseFile(rulesPath), output, logPath);

            var entries = MatchLogger.Read(logPath);
            Assert.DoesNotContain(entries, e => e.Contains("запись предыдущего матча"));
            Assert.Contains("Матч начался.", entries[0]);
            Assert.Contains("Матч завершён.", entries[^1]);
            Assert.Contains(entries, e => e.Contains("Ход 1 начат. Текущая позиция: (0, 0)."));
            Assert.Contains(entries, e => e.Contains("Ход 1 завершён. Новая позиция: (2, 3)."));
            Assert.Contains(entries, e => e.Contains("Ход 2 начат. Текущая позиция: (2, 3)."));
            Assert.Contains(entries, e => e.Contains("Ход 2 завершён. Новая позиция: (102, 103)."));
        }
        finally
        {
            File.Delete(rulesPath);
            File.Delete(logPath);
        }
    }

    [Fact]
    public void SecondMatchOverwritesLogOfFirstMatch()
    {
        var logPath = TempPath(".log");
        try
        {
            var first = new[] { new RuleEntry(1, RuleTokens.Ship, new[] { 0, 0, 1, 1 }), new RuleEntry(2, RuleTokens.Move, Array.Empty<int>()), new RuleEntry(3, RuleTokens.Turn, Array.Empty<int>()) };
            var second = new[] { new RuleEntry(1, RuleTokens.Ship, new[] { 5, 5, 0, 0 }), new RuleEntry(2, RuleTokens.Move, Array.Empty<int>()), new RuleEntry(3, RuleTokens.Turn, Array.Empty<int>()) };

            MatchRunner.RunRules(first, new StringWriter(), logPath);
            MatchRunner.RunRules(second, new StringWriter(), logPath);

            var entries = MatchLogger.Read(logPath);
            Assert.Single(entries, e => e.Contains("Матч начался."));
            Assert.DoesNotContain(entries, e => e.Contains("(1, 1)"));
            Assert.Contains(entries, e => e.Contains("Ход 1 завершён. Новая позиция: (5, 5)."));
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void MatchWithoutShipFailsButRestoresRootScope()
    {
        var logPath = TempPath(".log");
        try
        {
            // MOVE без SHIP: у корабля нет свойств, чтение позиции через адаптер бросает исключение.
            var entries = new[] { new RuleEntry(1, RuleTokens.Move, Array.Empty<int>()), new RuleEntry(2, RuleTokens.Turn, Array.Empty<int>()) };

            Assert.ThrowsAny<Exception>(() => MatchRunner.RunRules(entries, new StringWriter(), logPath));

            var log = MatchLogger.Read(logPath);
            Assert.Contains("Матч начался.", log[0]);
            Assert.DoesNotContain(log, e => e.Contains("Матч завершён."));
            Assert.Same(Hwdtech.IoC.Resolve<object>("Scopes.Root"), Hwdtech.IoC.Resolve<object>("Scopes.Current"));
        }
        finally
        {
            File.Delete(logPath);
        }
    }
}
