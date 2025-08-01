// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

namespace TestCentric.Extensibility
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExtensionAttribute : Attribute
    {
        public string? Path { get; set; }

        public string? Description { get; set; }

        public bool Enabled { get; set; }

        public string? EngineVersion { get; set; }

        public ExtensionAttribute()
        {
            Enabled = true;
        }
    }
}
