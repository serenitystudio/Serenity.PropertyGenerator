using DifferentNamespace;
using Serenity.Property;

namespace ConsoleApp;

public partial class Test
{
    [Getter] private int _getterIntField;

    [Setter] private int[] _setterIntField;

    [Getter, Setter] private int _getterSetterIntField;

    [Getter] private TestFieldType[] _getterTestField;

    [Setter] private TestFieldType _setterTestField;

    [Getter, Setter] private TestFieldType _getterSetterTestField;
}