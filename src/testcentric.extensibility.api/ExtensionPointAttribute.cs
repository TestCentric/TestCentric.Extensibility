// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

namespace TestCentric.Extensibility
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class ExtensionPointAttribute : Attribute
    {
        public string Path { get; private set; }

        public Type Type { get; private set; }

        public string Description { get; set; }

        public ExtensionPointAttribute(string path, Type type)
        {
            Path = path;
            Type = type;
        }
    }
}
