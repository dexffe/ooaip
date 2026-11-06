using Hwdtech;
using Hwdtech.Ioc;
namespace SpaceBattle.Lib.Test;

public class CommandAdaptersTest
{
    [Fact]
    public void HwdtechCommandAdapterForwardsExecuteToHwdtechCommand()
    {
        var inner = new Mock<Hwdtech.ICommand>();
        ICommand adapter = new HwdtechCommandAdapter(inner.Object);

        adapter.Execute();

        inner.Verify(c => c.Execute(), Times.Once);
    }

    [Fact]
    public void GameCommandHwdtechAdapterForwardsExecuteToGameCommand()
    {
        var inner = new Mock<ICommand>();
        Hwdtech.ICommand adapter = new GameCommandHwdtechAdapter(inner.Object);

        adapter.Execute();

        inner.Verify(c => c.Execute(), Times.Once);
    }

    [Fact]
    public void AdaptersRejectNullCommands()
    {
        Assert.Throws<ArgumentNullException>(() => new HwdtechCommandAdapter(null!));
        Assert.Throws<ArgumentNullException>(() => new GameCommandHwdtechAdapter(null!));
    }

    [Fact]
    public void AdaptersPropagateExceptionsOfWrappedCommands()
    {
        var failing = new Mock<ICommand>();
        failing.Setup(c => c.Execute()).Throws<InvalidOperationException>();

        Assert.Throws<InvalidOperationException>(() => new GameCommandHwdtechAdapter(failing.Object).Execute());
    }

    [Fact]
    public void HwdtechCommandFromIoCRunsInsideGameQueue()
    {
        new InitScopeBasedIoCImplementationCommand().Execute();
        IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", IoC.Resolve<object>("Scopes.New", IoC.Resolve<object>("Scopes.Root"))).Execute();

        var executed = 0;
        var queue = new Queue<ICommand>();
        queue.Enqueue(new HwdtechCommandAdapter(new GameCommandHwdtechAdapter(new ActionCommand(() => executed++))));

        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetQuantum", (object[] _) => (object)1000).Execute();
        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", "GetCommandFromQueueStrategy", (object[] args) => new GetCommandFromQueueStrategy().DoAlgorithm(args)).Execute();

        new ReturnCommandTimeCommand(queue).Execute();

        Assert.Equal(1, executed);
        Assert.Empty(queue);
    }

    [Fact]
    public void IoCAdapterTranslatesInterfaceCallsToUObjectProperties()
    {
        new InitScopeBasedIoCImplementationCommand().Execute();
        IoC.Resolve<Hwdtech.ICommand>("Scopes.Current.Set", IoC.Resolve<object>("Scopes.New", IoC.Resolve<object>("Scopes.Root"))).Execute();

        var properties = new Dictionary<string, object>
        {
            ["position"] = new Vector(1, 2),
            ["velocity"] = new Vector(3, 4)
        };
        var ship = new Mock<IUObject>();
        ship.Setup(s => s.getProperty(It.IsAny<string>())).Returns((string key) => properties[key]);
        ship.Setup(s => s.setProperty(It.IsAny<string>(), It.IsAny<object>())).Callback((string key, object value) => properties[key] = value);

        var codeStringMap = new Dictionary<KeyValuePair<Type, Type>, string>();
        var memoryStreamMap = new Dictionary<string, MemoryStream>();
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (Microsoft.CodeAnalysis.MetadataReference)Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(a.Location))
            .ToArray();

        Register("CodeStringmap", _ => codeStringMap);
        Register("GetMemoryStreammap", _ => memoryStreamMap);
        Register("CompileReferences", _ => references);
        Register("GetAdapterBuilder", args => new GetAdapterBuilderStrategy().DoAlgorithm(args));
        Register("CodeStringGenerateStrategy", args => "using Hwdtech;namespace SpaceBattle.Lib{" + new CodeStringGenerateStrategy().DoAlgorithm(args) + "}");
        Register("CompileStringstrategy", args => new CompileStringstrategy().DoAlgorithm(args));
        Register("CodeStringGenerateAdapter", args => new CodeStringGenerateAdapterCommand((string)args[0], (Type)args[1], (IUObject)args[2]));
        Register("FindAdapterStrategy", args => new FindAdapterStrategy().DoAlgorithm(args));
        Register("Adapter", args => new CreateadapterStrategy().DoAlgorithm(args));
        Register("GetPosition", args => ((IUObject)args[0]).getProperty("position"));
        Register("GetVelocity", args => ((IUObject)args[0]).getProperty("velocity"));
        Register("SetPosition", args => new ActionCommand(() => ((IUObject)args[0]).setProperty("position", args[1])));

        var movable = (IMovable)IoC.Resolve<object>("Adapter", ship.Object, typeof(IMovable));

        Assert.Equal(new Vector(1, 2), movable.Position);
        Assert.Equal(new Vector(3, 4), movable.Velocity);

        new MoveCommand(movable).Execute();

        Assert.Equal(new Vector(4, 6), properties["position"]);
    }

    private static void Register(string key, Func<object[], object> dependency) =>
        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", key, dependency).Execute();
}
