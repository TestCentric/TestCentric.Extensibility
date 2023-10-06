// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

namespace TestCentric.Extensibility
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ExtensionPropertyAttribute : Attribute
    {
        public string Name { get; private set; }

        public string Value { get; private set; }

        public ExtensionPropertyAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
