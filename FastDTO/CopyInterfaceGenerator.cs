using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace FastDto
{
[Generator]
    public class CopyInterfaceGenerator : IIncrementalGenerator
    {
        private const string AttributeName = "CopyByInterfaz";
        private const string AttributeFullName = "FastDto.CopyByInterfazAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Registrar la versión como una salida estática y el atributo
            context.RegisterPostInitializationOutput(ctx =>
            {
                // Añadir información de versión
                ctx.AddSource("VersionInfo.g.cs",
                    SourceText.From($"// Generador activo: {DateTime.Now}", Encoding.UTF8));
                
                // Añadir la definición del atributo
                var attributeSource = @"
using System;

namespace FastDto
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class CopyByInterfazAttribute : Attribute
    {
        public CopyByInterfazAttribute()
        {
        }
    }
}";
                ctx.AddSource("CopyByInterfazAttribute.g.cs", SourceText.From(attributeSource, Encoding.UTF8));
            });

            // Configurar el proveedor para interfaces
            IncrementalValuesProvider<InterfaceDeclarationSyntax> interfaceDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is InterfaceDeclarationSyntax,
                    transform: static (ctx, _) => (InterfaceDeclarationSyntax)ctx.Node)
                .Where(static m => m != null);

            // Configurar el proveedor para clases
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax cds && cds.BaseList != null,
                    transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
                .Where(static m => m != null);

            // Combinar interfaces con la compilación
            IncrementalValueProvider<(Compilation, ImmutableArray<InterfaceDeclarationSyntax>)> compilationAndInterfaces
                = context.CompilationProvider.Combine(interfaceDeclarations.Collect());

            // Combinar clases con la compilación
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
                = context.CompilationProvider.Combine(classDeclarations.Collect());

            // Registrar la generación de código
            context.RegisterSourceOutput(compilationAndInterfaces,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(
            Compilation compilation,
            ImmutableArray<InterfaceDeclarationSyntax> interfaces,
            SourceProductionContext context)
        {
            if (interfaces.IsDefaultOrEmpty)
                return;

            // Obtener el símbolo del atributo CopyByInterfazAttribute
            INamedTypeSymbol? attributeSymbol = compilation.GetTypeByMetadataName(AttributeFullName);
            if (attributeSymbol == null)
                return;

            // Encontrar todas las interfaces marcadas (tanto en el proyecto actual como en referencias)
            List<INamedTypeSymbol> markedInterfaces = new List<INamedTypeSymbol>();

            // Procesar interfaces del proyecto actual
            foreach (var interfaceDecl in interfaces)
            {
                var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
                var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

                if (interfaceSymbol != null && HasAttribute(interfaceSymbol, attributeSymbol))
                {
                    markedInterfaces.Add(interfaceSymbol);
                }
            }

            // Procesar interfaces en proyectos referenciados
            foreach (var reference in compilation.References)
            {
                var refAssembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (refAssembly != null)
                {
                    var interfacesInRef = FindMarkedInterfacesInAssembly(refAssembly.GlobalNamespace, attributeSymbol);
                    markedInterfaces.AddRange(interfacesInRef);
                }
            }

            // Generar informe de diagnóstico
            GenerateDiagnosticInfo(context, compilation, markedInterfaces);

            // Generar código para cada interfaz marcada
            foreach (var interfaceSymbol in markedInterfaces)
            {
                var sourceCode = GenerateCopyMethod(interfaceSymbol);
                var fileName = GetFileName(interfaceSymbol, compilation);
                context.AddSource(fileName, SourceText.From(sourceCode, Encoding.UTF8));
            }
        }

        private static List<INamedTypeSymbol> FindMarkedInterfacesInAssembly(
            INamespaceSymbol namespaceSymbol,
            INamedTypeSymbol attributeSymbol)
        {
            var result = new List<INamedTypeSymbol>();

            foreach (var member in namespaceSymbol.GetMembers())
            {
                if (member is INamedTypeSymbol typeSymbol &&
                    typeSymbol.TypeKind == TypeKind.Interface &&
                    HasAttribute(typeSymbol, attributeSymbol))
                {
                    result.Add(typeSymbol);
                }
                else if (member is INamespaceSymbol nestedNamespace)
                {
                    result.AddRange(FindMarkedInterfacesInAssembly(nestedNamespace, attributeSymbol));
                }
            }

            return result;
        }

        private static bool HasAttribute(INamedTypeSymbol symbol, INamedTypeSymbol attributeSymbol)
        {
            return attributeSymbol != null &&
                   symbol.GetAttributes()
                       .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
        }

        private static void GenerateDiagnosticInfo(
            SourceProductionContext context,
            Compilation compilation,
            List<INamedTypeSymbol> markedInterfaces)
        {
            var diagnosticInfo = new StringBuilder();
            diagnosticInfo.AppendLine($"// Proyecto: {compilation.AssemblyName}");
            diagnosticInfo.AppendLine($"// Interfaces marcadas encontradas: {markedInterfaces.Count}");

            foreach (var iface in markedInterfaces)
            {
                diagnosticInfo.AppendLine($"// - {iface.ToDisplayString()} (Ensamblado: {iface.ContainingAssembly.Name})");
            }

            context.AddSource("DiagnosticInfo.g.cs", SourceText.From(diagnosticInfo.ToString(), Encoding.UTF8));
        }

        private static string GetFileName(INamedTypeSymbol interfaceSymbol, Compilation compilation)
        {
            var fileName = $"{interfaceSymbol.Name}_CopyInterface.g.cs";

            // Evitar colisiones de nombres y mostrar origen
            if (!SymbolEqualityComparer.Default.Equals(interfaceSymbol.ContainingAssembly, compilation.Assembly))
            {
                fileName = $"{interfaceSymbol.ContainingAssembly.Name}_{fileName}";
            }

            return fileName;
        }

        private static string GenerateCopyMethod(INamedTypeSymbol interfaceSymbol)
        {
            var interfaceName = interfaceSymbol.ToDisplayString();
            var allProperties = GetAllWritableProperties(interfaceSymbol).ToList();
            var sourceAssembly = interfaceSymbol.ContainingAssembly.Name;
            var namespaceName = interfaceSymbol.ContainingNamespace.ToDisplayString();

            var code = new StringBuilder();
            code.AppendLine($"// Generated for interface: {interfaceName}");
            code.AppendLine($"// Source assembly: {sourceAssembly}");
            code.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using System.Linq;");
            code.AppendLine("");
            code.AppendLine($"namespace {namespaceName}");
            code.AppendLine("{");
            code.AppendLine($"    public static class {interfaceSymbol.Name}Extensions");
            code.AppendLine("    {");
            
            // Método CopyFrom
            code.AppendLine($"        /// <summary>");
            code.AppendLine($"        /// Copia todas las propiedades de la interfaz {interfaceSymbol.Name} desde el origen al destino");
            code.AppendLine($"        /// </summary>");
            code.AppendLine($"        public static void CopyFrom<TSource, TTarget>(this TTarget target, TSource source)");
            code.AppendLine($"            where TSource : {interfaceName}");
            code.AppendLine($"            where TTarget : {interfaceName}");
            code.AppendLine("        {");
            code.AppendLine("            if (source == null)");
            code.AppendLine("                throw new ArgumentNullException(nameof(source));");
            code.AppendLine("");

            foreach (var prop in allProperties)
            {
                code.AppendLine($"            target.{prop.Name} = source.{prop.Name};");
            }
            code.AppendLine("        }");
            code.AppendLine("");

            // Método NewFrom
            code.AppendLine($"        /// <summary>");
            code.AppendLine($"        /// Crea una nueva instancia de TTarget con las propiedades de {interfaceSymbol.Name} copiadas desde el origen");
            code.AppendLine($"        /// </summary>");
            code.AppendLine($"        public static TTarget NewFrom<TTarget>(this {interfaceName} source)");
            code.AppendLine($"            where TTarget : {interfaceName}, new()");
            code.AppendLine("        {");
            code.AppendLine("            if (source == null)");
            code.AppendLine("                throw new ArgumentNullException(nameof(source));");
            code.AppendLine("");
            code.AppendLine("            var target = new TTarget();");
            code.AppendLine("            target.CopyFrom(source);");
            code.AppendLine("            return target;");
            code.AppendLine("        }");
            code.AppendLine("");

            // Método NewListFrom
            code.AppendLine($"        /// <summary>");
            code.AppendLine($"        /// Crea una lista de objetos TTarget a partir de una colección de objetos que implementan {interfaceSymbol.Name}");
            code.AppendLine($"        /// </summary>");
            code.AppendLine($"        public static List<TTarget> NewListFrom<TTarget>(this IEnumerable<{interfaceName}> source)");
            code.AppendLine($"            where TTarget : {interfaceName}, new()");
            code.AppendLine("        {");
            code.AppendLine("            if (source == null)");
            code.AppendLine("                throw new ArgumentNullException(nameof(source));");
            code.AppendLine("");
            code.AppendLine("            return source.Select(item => item.NewFrom<TTarget>()).ToList();");
            code.AppendLine("        }");

            code.AppendLine("    }");
            code.AppendLine("}");

            return code.ToString();
        }

        private static IEnumerable<IPropertySymbol> GetAllWritableProperties(INamedTypeSymbol symbol)
        {
            // Combinar propiedades propias y heredadas
            return symbol.AllInterfaces
                .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
                .Concat(symbol.GetMembers().OfType<IPropertySymbol>())
                .Where(prop => prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility != Accessibility.Private);
        }
    }
}
