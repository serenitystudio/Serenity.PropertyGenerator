using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Serenity.PropertyGenerator;

public sealed class SyntaxReceiver : ISyntaxReceiver
{
    public Dictionary<string, List<WorkItem>> WorkItems { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (TryGetWorkItem(syntaxNode, out var workItem))
        {
            if (WorkItems.ContainsKey(workItem.TypeName))
            {
                WorkItems[workItem.TypeName].Add(workItem);
            }
            else
            {
                WorkItems.Add(workItem.TypeName, new List<WorkItem> { workItem });
            }
        }
    }

    private static bool TryGetWorkItem(SyntaxNode syntax, out WorkItem? workItem)
    {
        if (syntax is FieldDeclarationSyntax { AttributeLists.Count: > 0 } fieldDeclarationSyntax)
        {
            var attributes = fieldDeclarationSyntax.AttributeLists.SelectMany(x => x.Attributes);
            var item = new WorkItem(fieldDeclarationSyntax);
            foreach (var attributeSyntax in attributes)
            {
                var attributeName = attributeSyntax.Name.ToString();
                switch (attributeName)
                {
                    case PropertySourceGenerator.GetterAttributeName:
                    case $"{PropertySourceGenerator.GetterAttributeName}Attribute":
                    case $"{PropertySourceGenerator.NameSpace}.{PropertySourceGenerator.GetterAttributeName}":
                    case $"{PropertySourceGenerator.NameSpace}.{PropertySourceGenerator.GetterAttributeName}Attribute":
                        item.SetExistGetterProperty(true);
                        break;
                    case PropertySourceGenerator.SetterAttributeName:
                    case $"{PropertySourceGenerator.SetterAttributeName}Attribute":
                    case $"{PropertySourceGenerator.NameSpace}.{PropertySourceGenerator.SetterAttributeName}":
                    case $"{PropertySourceGenerator.NameSpace}.{PropertySourceGenerator.SetterAttributeName}Attribute":
                        item.SetExistSetterProperty(true);
                        break;
                }
            }

            if (item.ExistProperty())
            {
                var typeName = item.FieldDeclarationSyntax.SyntaxTree.GetRoot().DescendantNodes()
                    .OfType<ClassDeclarationSyntax>().Last().Identifier.Text;
                item.SetTypeName(typeName);
                workItem = item;
                return true;
            }
        }

        workItem = null;
        return false;
    }
}