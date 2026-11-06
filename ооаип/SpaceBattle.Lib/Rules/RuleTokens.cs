namespace SpaceBattle.Lib;

/// <summary>
/// Управляющие ключевые слова (контрольные токены) файла сценария.
/// Регистр в файле не важен.
/// </summary>
public static class RuleTokens
{
    /// <summary>SHIP x y vx vy — создать корабль с позицией и скоростью.</summary>
    public const string Ship = "SHIP";

    /// <summary>TELEPORT x y — поставить в очередь команду установки позиции (через адаптер).</summary>
    public const string Teleport = "TELEPORT";

    /// <summary>MOVE — поставить в очередь команду движения.</summary>
    public const string Move = "MOVE";

    /// <summary>TURN — выполнить один ход (квант) над накопленной очередью команд.</summary>
    public const string Turn = "TURN";

    /// <summary>END — завершить сценарий, остальные строки игнорируются.</summary>
    public const string End = "END";
}
