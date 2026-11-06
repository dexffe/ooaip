using Hwdtech;
using Hwdtech.Ioc;
using SpaceBattle.ConsoleApp;
namespace SpaceBattle.Lib.Test;

public class ReturnCommandTimeCommandTest
{
    private static void RunMatch(TextReader input, TextWriter output, string logPath)
    {
        MatchRunner.Run(input, output, logPath);
    }

    private static void StartMatchLog(string logPath) => MatchLogger.StartMatch(logPath);

    private static void WriteMatchLog(string logPath, string message) => MatchLogger.Write(logPath, message);

    private static void WriteCurrentMatchLog(string message) => MatchLogger.WriteCurrent(message);

    private static IReadOnlyList<string> ReadMatchLog(string logPath) => MatchLogger.Read(logPath);

    [Fact]
    public void MatchRunnerCreatesGameScopeExecutesMoveAndKeepsOnlyLatestMatchLog()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"spacebattle-{Guid.NewGuid():N}.log");

        try
        {
            WriteMatchLog(logPath, "запись предыдущего матча");
            using var input = new StringReader("12\n5\n-7\n3\n");
            using var output = new StringWriter();

            RunMatch(input, output, logPath);

            var entries = ReadMatchLog(logPath);
            Assert.DoesNotContain(entries, entry => entry.Contains("запись предыдущего матча"));
            Assert.Contains(entries, entry => entry.Contains("Матч начался."));
            Assert.Contains(entries, entry => entry.Contains("Ход 1 начат."));
            Assert.Contains(entries, entry => entry.Contains("Для команды создан адаптер: IMovableAdapter."));
            Assert.Contains(entries, entry => entry.Contains("Ход 1 завершён. Новая позиция: (5, 8)."));
            Assert.Contains(entries, entry => entry.Contains("Матч завершён."));
            Assert.Contains("Позиция после хода: (5, 8)", output.ToString());
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public async Task MatchLoggerWritesConcurrentEntriesAsWholeLines()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"spacebattle-{Guid.NewGuid():N}.log");

        try
        {
            StartMatchLog(logPath);
            const int writerCount = 8;
            const int entriesPerWriter = 25;

            await Task.WhenAll(Enumerable.Range(0, writerCount).Select(writer => Task.Run(() =>
            {
                for (var entry = 0; entry < entriesPerWriter; entry++)
                {
                    WriteCurrentMatchLog($"writer-{writer}-entry-{entry}");
                }
            })));

            var entries = ReadMatchLog(logPath);
            Assert.Equal(writerCount * entriesPerWriter, entries.Count);
            Assert.Equal(writerCount * entriesPerWriter, entries.Select(entry => entry.Split(']')[1]).Distinct().Count());

            StartMatchLog(logPath);
            Assert.Empty(ReadMatchLog(logPath));
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
    }

    [Fact]
    public void SuccessfulReturnCommandTimeCommand()
    {
        new InitScopeBasedIoCImplementationCommand().Execute();
        var scope = IoC.Resolve<object>("Scopes.New", IoC.Resolve<object>("Scopes.Root"));
        IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", scope).Execute();
        
        var queue = new Queue<ICommand>();

        queue.Enqueue(new ActionCommand(() => {
            IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", IoC.Resolve<object>("Scopes.New", scope)).Execute();
            
            var strategy = new Mock<IStrategy>();
            strategy.Setup(s => s.DoAlgorithm()).Returns(0);

            IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetQuantum", (object[] args) => strategy.Object.DoAlgorithm(args)).Execute();  
        }));

        queue.Enqueue(new ActionCommand(() => {}));
        queue.Enqueue(new ActionCommand(() => {}));

        var getQuantumStrategy = new Mock<IStrategy>();
        getQuantumStrategy.Setup(s => s.DoAlgorithm()).Returns(300);

        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetCommandFromQueueStrategy", (object[] args) => queue.Dequeue()).Execute();
        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetQuantum", (object[] args) => getQuantumStrategy.Object.DoAlgorithm(args)).Execute();

        var returnCommandTimeCommand = new ReturnCommandTimeCommand(queue);

        Assert.Equal(300, IoC.Resolve<int>("GetQuantum"));

        returnCommandTimeCommand.Execute();

        Assert.Equal(0, IoC.Resolve<int>("GetQuantum"));
        Assert.True(queue.Count == 2);
    }

    [Fact]
    public void UnsuccessfulReturnCommandTimeCommand()
    {
        var handler = new Mock<ICommand>();
        handler.Setup(h => h.Execute()).Callback(() => {}).Verifiable();

        new InitScopeBasedIoCImplementationCommand().Execute();
        var scope = IoC.Resolve<object>("Scopes.New", IoC.Resolve<object>("Scopes.Root"));
        IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", scope).Execute();
        
        var queue = new Queue<ICommand>();

        var cmd = new Mock<ICommand>();
        cmd.Setup(c => c.Execute()).Throws<Exception>();

        queue.Enqueue(cmd.Object);
        queue.Enqueue(new ActionCommand(() => {
            IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", IoC.Resolve<object>("Scopes.New", scope)).Execute();
            
            var strategy = new Mock<IStrategy>();
            strategy.Setup(s => s.DoAlgorithm()).Returns(0);

            IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetQuantum", (object[] args) => strategy.Object.DoAlgorithm(args)).Execute();  
        }));

        var getQuantumStrategy = new Mock<IStrategy>();
        getQuantumStrategy.Setup(s => s.DoAlgorithm()).Returns(300);

        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetCommandFromQueueStrategy", (object[] args) => queue.Dequeue()).Execute();
        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "FindHandlerStrategy", (object[] args) => handler.Object).Execute();
        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetQuantum", (object[] args) => getQuantumStrategy.Object.DoAlgorithm(args)).Execute();

        var returnCommandTimeCommand = new ReturnCommandTimeCommand(queue);

        returnCommandTimeCommand.Execute();

        handler.VerifyAll();
    }
}
