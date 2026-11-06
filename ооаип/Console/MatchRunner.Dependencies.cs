using Hwdtech;
using Microsoft.CodeAnalysis;
using SpaceBattle.Lib;

namespace SpaceBattle.ConsoleApp;

public static partial class MatchRunner
{
    private static void RegisterDependencies(Queue<Lib.ICommand> queue, Action<string> log)
    {
        var codeStringMap = new Dictionary<KeyValuePair<Type, Type>, string>();
        var memoryStreamMap = new Dictionary<string, MemoryStream>();

        Register("GetQuantum", _ => 1000);
        Register("GetGameQueue", _ => queue);
        Register("CodeStringmap", _ => codeStringMap);
        Register("GetMemoryStreammap", _ => memoryStreamMap);
        Register("GetAdapterBuilder", args => new GetAdapterBuilderStrategy().DoAlgorithm(args));
        Register("CodeStringGenerateStrategy", args => WrapAdapterCode((string)new CodeStringGenerateStrategy().DoAlgorithm(args)));
        Register("CompileStringstrategy", args => new CompileStringstrategy().DoAlgorithm(args));
        Register("CodeStringGenerateAdapter", args => new CodeStringGenerateAdapterCommand((string)args[0], (Type)args[1], (IUObject)args[2]));
        Register("FindAdapterStrategy", args => new FindAdapterStrategy().DoAlgorithm(args));
        Register("Adapter", args => new CreateadapterStrategy().DoAlgorithm(args));
        Register("CompileReferences", _ => BuildAdapterReferences());
        Register("GetPosition", args => ((IUObject)args[0]).getProperty("position"));
        Register("GetVelocity", args => ((IUObject)args[0]).getProperty("velocity"));
        Register("SetPosition", args => new ActionCommand(() => ((IUObject)args[0]).setProperty("position", args[1])));
        Register("FindHandlerStrategy", args => new ActionCommand(() => log($"Ошибка команды: {((Exception)args[0]).Message}")));
        Register("GetCommandFromQueueStrategy", args => new GetCommandFromQueueStrategy().DoAlgorithm(args));
        Register("ReturnCommandTimeStrategy", args => new ReturnCommandTimeStrategy().DoAlgorithm(args));
    }

    private static void Register(string key, Func<object[], object> dependency)
    {
        IoC.Resolve<Hwdtech.ICommand>("IoC.Register", key, dependency).Execute();
    }

    private static string WrapAdapterCode(string code) =>
        "using Hwdtech;" + Environment.NewLine +
        "namespace SpaceBattle.Lib" + Environment.NewLine +
        "{" + Environment.NewLine + code + Environment.NewLine + "}";

    private static MetadataReference[] BuildAdapterReferences()
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return trusted
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(File.Exists)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
