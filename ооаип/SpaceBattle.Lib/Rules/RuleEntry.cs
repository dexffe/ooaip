namespace SpaceBattle.Lib;

/// <summary>Разобранная строка сценария: токен и его целочисленные аргументы.</summary>
public sealed record RuleEntry(int LineNumber, string Token, IReadOnlyList<int> Arguments);

/// <summary>Ошибки разбора сценария (все найденные ошибки с номерами строк).</summary>
public sealed class RuleParseException : FormatException
{
    public IReadOnlyList<string> Errors { get; }

    public RuleParseException(IReadOnlyList<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors;
    }
}
