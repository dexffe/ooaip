namespace SpaceBattle.Lib.Test;

public class RuleParserTest
{
    [Fact]
    public void ParsesAllControlTokensFromTxtFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"spacebattle-rules-{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllLines(path, new[]
            {
                "# комментарий",
                "",
                "ship 10 20 3 -2   # корабль",
                "TELEPORT 0 0",
                "Move",
                "TURN",
                "END",
                "MOVE"
            });

            var entries = RuleParser.ParseFile(path);

            Assert.Equal(new[] { RuleTokens.Ship, RuleTokens.Teleport, RuleTokens.Move, RuleTokens.Turn, RuleTokens.End },
                entries.Select(e => e.Token));
            Assert.Equal(new[] { 10, 20, 3, -2 }, entries[0].Arguments);
            Assert.Equal(new[] { 0, 0 }, entries[1].Arguments);
            Assert.Equal(new[] { 3, 4, 5, 6, 7 }, entries.Select(e => e.LineNumber));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void StopsParsingAtEndToken()
    {
        var entries = RuleParser.Parse(new[] { "MOVE", "END", "THIS LINE IS NEVER VALIDATED" });

        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void ReportsAllErrorsWithLineNumbers()
    {
        var exception = Assert.Throws<RuleParseException>(() => RuleParser.Parse(new[]
        {
            "FLY 1 2",
            "SHIP 1 2",
            "TELEPORT a b",
            "MOVE"
        }));

        Assert.Equal(3, exception.Errors.Count);
        Assert.Contains("Строка 1", exception.Errors[0]);
        Assert.Contains("FLY", exception.Errors[0]);
        Assert.Contains("Строка 2", exception.Errors[1]);
        Assert.Contains("Строка 3", exception.Errors[2]);
    }

    [Fact]
    public void ThrowsOnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => RuleParser.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => RuleParser.ParseFile(null!));
    }
}
