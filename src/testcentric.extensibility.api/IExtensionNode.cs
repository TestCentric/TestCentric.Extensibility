// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TestCentric.Extensibility
{
    public interface IExtensionNode
    {
        /// <summary>
        /// Gets the full name of the Type of the extension object.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TestCentric.Engine.Extensibility.ExtensionNode"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        bool Enabled { get; }

        /// <summary>
        /// Gets the unique string identifying the ExtensionPoint for which
        /// this Extension is intended. This identifier may be supplied by the attribute
        /// marking the extension or deduced by NUnit from the Type of the extension class.
        /// </summary>
        string? Path { get; }

        /// <summary>
        /// An optional description of what the extension does.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets a collection of the names of all this extension's properties
        /// </summary>
        IEnumerable<string> PropertyNames { get; }

        /// <summary>
        /// Gets the path to the assembly where the extension is defined.
        /// </summary>
        string AssemblyPath { get; }

        /// <summary>
        /// Gets the version of the extension assembly.
        /// </summary>
        Version AssemblyVersion { get; }

        IEnumerable<string> GetValues(string name);

        /// <summary>
        /// Gets an object of the specified extension type, loading the Assembly
        /// and creating the object as needed. Note that this property always
        /// returns the same object. Use CreateExtensionObject if a new one is
        /// needed each time or to specify arguments.
        /// </summary>
        object ExtensionObject { get; }

        /// <summary>
        /// Gets a newly created extension object, created in the current application domain
        /// </summary>
        object CreateExtensionObject(params object[] args);
    }
}
