using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Serenity.PropertyGenerator;

public sealed class WorkItem
{
    public readonly FieldDeclarationSyntax FieldDeclarationSyntax;
    public bool ExistGetterProperty { get; private set; }
    public bool ExistSetterProperty { get; private set; }
    public string? ClassName { get; private set; }

    public WorkItem(FieldDeclarationSyntax fieldDeclarationSyntax)
    {
        FieldDeclarationSyntax = fieldDeclarationSyntax;
    }

    public void SetClassName(string className)
    {
        ClassName = className;
    }

    public void SetExistGetterProperty(bool existGetterProperty)
    {
        ExistGetterProperty = existGetterProperty;
    }

    public void SetExistSetterProperty(bool existSetterProperty)
    {
        ExistSetterProperty = existSetterProperty;
    }

    public bool ExistProperty() => ExistGetterProperty || ExistSetterProperty;
}