using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Serenity.PropertyGenerator;

[Generator(LanguageNames.CSharp)]
public class PropertySourceGenerator : ISourceGenerator
{
    public const string NameSpace = "Serenity.Property";
    public const string GetterAttributeName = "Getter";
    public const string SetterAttributeName = "Setter";

    private const string GetterAttribute = $@"
using System;

namespace {NameSpace}
{{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class {GetterAttributeName}Attribute : Attribute
    {{
    }}
}}";

    private const string SetterAttribute = $@"
using System;

namespace {NameSpace}
{{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class {SetterAttributeName}Attribute : Attribute
    {{
    }}
}}";

    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            Debugger.Launch();
        }
#endif
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var moduleName = context.Compilation.SourceModule.Name.AsSpan();
        if (moduleName.StartsWith("UnityEngine.")) return;
        if (moduleName.StartsWith("UnityEditor.")) return;
        if (moduleName.StartsWith("Unity.")) return;

        context.AddSource($"GetterAttribute.g.cs", SourceText.From(GetterAttribute, Encoding.UTF8));
        context.AddSource($"SetterAttribute.g.cs", SourceText.From(SetterAttribute, Encoding.UTF8));

        var syntaxReceiver = (SyntaxReceiver)context.SyntaxReceiver!;

        if (syntaxReceiver.WorkItems.Count == 0) return;

        var codeWriter = new CodeWriter();
        foreach (var workItems in syntaxReceiver.WorkItems.Values)
        {
            var workItem = workItems[0];
            var semanticModel = context.Compilation.GetSemanticModel(workItem.FieldDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(workItem.FieldDeclarationSyntax.Parent) is not INamedTypeSymbol typeSymbol)
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(workItem.FieldDeclarationSyntax.Parent);

            if (symbol == null) continue;

            var sourceText = WriteProperty(codeWriter, semanticModel,
                symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToString(),
                WriteTypeName(semanticModel, (TypeDeclarationSyntax)workItem.FieldDeclarationSyntax.Parent), workItems);
            context.AddSource($"{symbol.Name}.g.cs", SourceText.From(sourceText, Encoding.UTF8));
            codeWriter.Clear();
        }
    }

    private static string WriteTypeName(in SemanticModel? semanticModel, TypeDeclarationSyntax typeDeclarationSyntax)
    {
        var typeName = new StringBuilder("partial ")
            .Append(typeDeclarationSyntax.Keyword.ValueText)
            .Append(" ")
            .Append(typeDeclarationSyntax.Identifier.ToString())
            .Append(typeDeclarationSyntax.TypeParameterList);
        foreach (var constraint in typeDeclarationSyntax.ConstraintClauses)
        {
            typeName.Append(" where ");
            foreach (var childNode in constraint.ChildNodes())
            {
                switch (childNode)
                {
                    case IdentifierNameSyntax:
                        typeName.Append(childNode).Append(" : ");
                        break;
                    case TypeConstraintSyntax typeConstraintSyntax:
                        typeName.Append(semanticModel.GetTypeInfo(typeConstraintSyntax.Type).Type.ToDisplayString());
                        break;
                }
            }
        }

        return typeName.ToString();
    }

    private static string WriteProperty(in CodeWriter codeWriter, in SemanticModel? semanticModel, string namespaceName,
        string typeName, List<WorkItem> workItems)
    {
        if (!string.IsNullOrEmpty(namespaceName))
        {
            codeWriter.AppendLine($"namespace {namespaceName}");
            codeWriter.BeginBlock();
        }

        codeWriter.AppendLine(typeName);
        codeWriter.BeginBlock();

        foreach (var workItem in workItems)
        {
            AppendProperty(codeWriter, semanticModel, workItem);
            codeWriter.AppendLine();
        }

        codeWriter.EndBlock();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            codeWriter.EndBlock();
        }

        return codeWriter.ToString();
    }

    private static void AppendProperty(in CodeWriter codeWriter, in SemanticModel? semanticModel, WorkItem workItem)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("public ");

        var fieldType = semanticModel.GetTypeInfo(workItem.FieldDeclarationSyntax.Declaration.Type).Type.ToDisplayString();
        stringBuilder.Append(fieldType);

        stringBuilder.Append(" ");

        // Switch from _camelCase to PascalCase
        var fieldName = workItem.FieldDeclarationSyntax.Declaration.Variables.First().Identifier.ValueText.AsSpan();
        Span<char> pascalCaseName = stackalloc char[fieldName.Length - 1];
        fieldName.Slice(1, fieldName.Length - 1).CopyTo(pascalCaseName);
        fieldName[1..2].ToUpperInvariant(pascalCaseName);

        stringBuilder.Append(pascalCaseName);

        codeWriter.AppendLine(stringBuilder.ToString());
        codeWriter.BeginBlock();

        if (workItem.ExistGetterProperty)
        {
            stringBuilder.Clear();
            stringBuilder.Append("get => ");
            stringBuilder.Append(fieldName);
            stringBuilder.Append(";");
            codeWriter.AppendLine(stringBuilder.ToString());
        }

        if (workItem.ExistSetterProperty)
        {
            stringBuilder.Clear();
            stringBuilder.Append("set => ");
            stringBuilder.Append(fieldName);
            stringBuilder.Append(" = value;");
            codeWriter.AppendLine(stringBuilder.ToString());
        }

        codeWriter.EndBlock();
    }
}