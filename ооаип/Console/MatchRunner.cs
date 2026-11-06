using Hwdtech;
using Hwdtech.Ioc;
using SpaceBattle.Lib;

namespace SpaceBattle.ConsoleApp;

public static partial class MatchRunner
{
    /// <summary>Интерактивный режим: параметры корабля читаются из потока ввода.</summary>
    public static void Run(TextReader input, TextWriter output, string logPath)
    {
        RunMatch(output, logPath, (session, ship) =>
        {
            output.WriteLine("=== SpaceBattle: перемещение корабля ===");
            output.WriteLine("Введите целые координаты и скорость по двум осям.");

            var position = new Vector(ReadInteger(input, output, "X: "), ReadInteger(input, output, "Y: "));
            var velocity = new Vector(ReadInteger(input, output, "Скорость X: "), ReadInteger(input, output, "Скорость Y: "));
            ship.Set(position, velocity);

            output.WriteLine($"Начальная позиция: {position}");
            output.WriteLine($"Скорость: {velocity}");
            output.WriteLine("Нажмите Enter, чтобы выполнить один игровой ход.");
            input.ReadLine();

            session.EnqueueMove();
            session.PlayTurn();
        });
    }

    public static void RunRules(IReadOnlyList<RuleEntry> entries, TextWriter output, string logPath)
    {
        ArgumentNullException.ThrowIfNull(entries);

        RunMatch(output, logPath, (session, ship) =>
        {
            foreach (var entry in entries)
            {
                if (entry.Token == RuleTokens.End)
                {
                    return;
                }

                ApplyEntry(entry, session, ship);
            }
        });
    }

    private static void ApplyEntry(RuleEntry entry, GameSession session, ConsoleShip ship)
    {
        switch (entry.Token)
        {
            case RuleTokens.Ship:
                ship.Set(new Vector(entry.Arguments[0], entry.Arguments[1]), new Vector(entry.Arguments[2], entry.Arguments[3]));
                break;
            case RuleTokens.Teleport:
                session.EnqueueTeleport(new Vector(entry.Arguments[0], entry.Arguments[1]));
                break;
            case RuleTokens.Move:
                session.EnqueueMove();
                break;
            case RuleTokens.Turn:
                session.PlayTurn();
                break;
        }
    }

    private static void RunMatch(TextWriter output, string logPath, Action<GameSession, ConsoleShip> scenario)
    {
        MatchLogger.StartMatch(logPath);
        Action<string> log = message =>
        {
            MatchLogger.WriteCurrent(message);
            output.WriteLine($"[Журнал] {message}");
        };
        log("Матч начался.");

        new InitScopeBasedIoCImplementationCommand().Execute();
        var rootScope = IoC.Resolve<object>("Scopes.Root");
        var gameScope = IoC.Resolve<object>("Scopes.New", rootScope);
        IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", gameScope).Execute();

        try
        {
            var queue = new Queue<Lib.ICommand>();
            RegisterDependencies(queue, log);
            var ship = new ConsoleShip();
            scenario(new GameSession(ship, queue, output, log), ship);
        }
        finally
        {
            IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", rootScope).Execute();
        }

        log("Матч завершён.");
        PrintLog(output, logPath);
    }

    private static void PrintLog(TextWriter output, string logPath)
    {
        output.WriteLine();
        output.WriteLine("=== Журнал матча ===");
        foreach (var entry in MatchLogger.Read(logPath))
        {
            output.WriteLine(entry);
        }

        output.WriteLine($"Файл журнала: {logPath}");
    }

    private static int ReadInteger(TextReader input, TextWriter output, string prompt)
    {
        while (true)
        {
            output.Write(prompt);
            var line = input.ReadLine()
                ?? throw new EndOfStreamException("Поток ввода закончился до получения всех параметров.");

            if (int.TryParse(line, out var value))
            {
                return value;
            }

            output.WriteLine("Введите целое число.");
        }
    }
}
