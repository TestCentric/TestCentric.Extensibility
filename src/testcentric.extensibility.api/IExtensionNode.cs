// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TestCentric.Extensibility
{
    /// <summary>
    /// Enumeration representing the status of an extension
    /// </summary>
    public enum ExtensionStatus
    {
        /// <summary>Extension is not yet loaded</summary>
        Unloaded,
        /// <summary>Extension has been loaded</summary>
        Loaded,
        /// <summary>An error occurred trying to load the extension</summary>
        Error
    }

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
        bool Enabled { get; set; }

        /// <summary>
        /// Status of this extension.
        /// </summary>
        ExtensionStatus Status { get; }

        /// <summary>
        /// Exception thrown in creating the ExtensionObject, if Status is error, otherwise null.
        /// </summary>
        Exception? Exception { get; }

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

        /// <summary>
        /// Gets a collection of the values of a particular named property.
        /// If none are present, returns an empty enumerator.
        /// </summary>
        /// <param name="name">The property name</param>
        /// <returns>A collection of values</returns>
        IEnumerable<string> GetValues(string name);

        /// <summary>
        /// Gets an object of the specified extension type, loading the Assembly
        /// and creating the object as needed. A null return indicates an error,
        /// which is recorded in the ExtensionNode.
        /// </summary>
        object? ExtensionObject { get; }
    }
}
