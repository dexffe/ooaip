namespace SpaceBattle.Lib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Reflection;
using Hwdtech;
public class CompileStringstrategy : IStrategy
{
    public object DoAlgorithm(params object[] args)
    {
        string str = (string)args[0];
        var obj = (IUObject)args[1];
        var types = (Type)args[2];
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(str);
        Compilation compilation = CSharpCompilation.Create(types.ToString() + "Adapter")
        .AddReferences(IoC.Resolve<MetadataReference[]>("CompileReferences"));
        compilation = compilation.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        .AddSyntaxTrees(syntaxTree);

        using (var ms = new MemoryStream())
        {
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                var errors = result.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.ToString());
                throw new InvalidOperationException(
                    $"Не удалось скомпилировать адаптер {types}:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
