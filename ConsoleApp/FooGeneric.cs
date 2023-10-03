﻿using DifferentNamespace;
using Serenity.Property;

namespace ConsoleApp;

public partial class FooGeneric<T> where T : Enum
{
    [Getter] private T _enumGetter;

    [Getter] private int _getterIntField;

    [Setter] private int[] _setterIntField;

    [Getter, Setter] private int _getterSetterIntField;

    [Getter] private FooFieldType[] _getterTestField;

    [Setter] private FooFieldType _setterFooField;

    [Getter, Setter] private FooFieldType _getterSetterFooField;
}