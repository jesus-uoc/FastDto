using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FastDTO
{
    [Generator]
    public class CopyInterfaceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("VersionInfo.g.cs",
                SourceText.From($"// Generador activo: {DateTime.Now}", Encoding.UTF8));

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            var compilation = context.Compilation;

            foreach (var interfaceDecl in receiver.CandidateInterfaces)
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

                if (interfaceSymbol != null)
                {
                    var sourceCode = GenerateCopyMethod(interfaceSymbol);
                    context.AddSource($"{interfaceSymbol.Name}_CopyInterface.g.cs",
                        SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
        }

        private static string GenerateCopyMethod(INamedTypeSymbol interfaceSymbol)
        {
            var interfaceName = interfaceSymbol.ToDisplayString();

            var allProperties = GetAllProperties(interfaceSymbol);

            var code = new StringBuilder();
            code.AppendLine($"namespace {interfaceSymbol.ContainingNamespace}");
            code.AppendLine("{");
            code.AppendLine($"    public static class {interfaceSymbol.Name}Extensions");
            code.AppendLine("    {");
            code.AppendLine($"        public static void CopyFrom<TSource, TTarget>(this TTarget target, TSource source)");
            code.AppendLine($"            where TSource : {interfaceName}");
            code.AppendLine($"            where TTarget : {interfaceName}");
            code.AppendLine("        {");

            foreach (var prop in allProperties)
            {
                code.AppendLine($"            target.{prop.Name} = source.{prop.Name};");
            }
            code.AppendLine("        }");

            code.AppendLine($"        public static TTarget NewFrom<TTarget>(this {interfaceName} source)");
            code.AppendLine($"            where TTarget : {interfaceName}, new()");
            code.AppendLine("        {");
            code.AppendLine("            var target = new TTarget();");

            foreach (var prop in allProperties)
            {
                code.AppendLine($"            target.{prop.Name} = source.{prop.Name};");
            }
            code.AppendLine("            return target;");

            code.AppendLine("        }");


            code.AppendLine($"        public static List<TTarget> NewListFrom<TTarget>(this IEnumerable<{interfaceName}> source)");
            code.AppendLine($"            where TTarget : {interfaceName}, new()");
            code.AppendLine("        {");
            code.AppendLine("            var target = new List<TTarget>();");
            code.AppendLine("            foreach (var item in source)");
            code.AppendLine("            {");
            code.AppendLine("               var newItem = item.NewFrom<TTarget>();");
            code.AppendLine("               target.Add(newItem);");
            code.AppendLine("            }");
            code.AppendLine("            return target;");

            code.AppendLine("        }");


            code.AppendLine("    }");
            code.AppendLine("}");

            return code.ToString();
            
        }

        private static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol symbol)
        {
            // Combinar propiedades propias y heredadas
            return symbol.AllInterfaces
                .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
                .Concat(symbol.GetMembers().OfType<IPropertySymbol>());
        }
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        public List<InterfaceDeclarationSyntax> CandidateInterfaces { get; } = new List<InterfaceDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InterfaceDeclarationSyntax interfaceDeclaration &&
                interfaceDeclaration.AttributeLists.Count > 0 &&
                interfaceDeclaration.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "CopyByInterfaz")))
            {
                CandidateInterfaces.Add(interfaceDeclaration);
            }
        }
    }
}
