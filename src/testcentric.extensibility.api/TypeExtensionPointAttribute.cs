// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

namespace TestCentric.Extensibility
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class TypeExtensionPointAttribute : Attribute
    {
        public string Path { get; private set; }

        public string Description { get; set; }

        public TypeExtensionPointAttribute(string path)
        {
            Path = path;
        }

        public TypeExtensionPointAttribute()
        {
        }
    }
}
