// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
#if !NET20
using System.Linq;
#endif
using System.Reflection;

namespace TestCentric.Extensibility
{
    /// <summary>
    /// The ExtensionNode class represents a single extension being installed
    /// on a particular extension point. It stores information needed to
    /// activate the extension object on a just-in-time basis.
    /// </summary>
    public class ExtensionNode : IExtensionNode
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionNode));

        private object? _extensionObject;
        private readonly Dictionary<string, List<string>> _properties = new Dictionary<string, List<string>>();

        /// <summary>
        /// Construct an ExtensionNode supplying the assembly path and type name.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly where this extension is found.</param>
        /// <param name="assemblyVersion">The version of the extension assembly.</param>
        /// <param name="typeName">The full name of the Type of the extension object.</param>
        public ExtensionNode(string assemblyPath, Version assemblyVersion, string typeName)
        {
            AssemblyPath = assemblyPath;
            AssemblyVersion = assemblyVersion;
            TypeName = typeName;
            Enabled = true; // By default
        }

        #region IExtensionNode Implementation

        /// <summary>
        /// Gets the path to the assembly where the extension is defined.
        /// </summary>
        public string AssemblyPath { get; }

        /// <summary>
        /// Gets the version of the extension assembly.
        /// </summary>
        public Version AssemblyVersion { get; }

        /// <summary>
        /// Gets the full name of the Type of the extension object.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TestCentric.Engine.Extensibility.ExtensionNode"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets and sets the unique string identifying the ExtensionPoint for which
        /// this Extension is intended. This identifier may be supplied by the attribute
        /// marking the extension or deduced by NUnit from the Type of the extension class.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// An optional description of what the extension does.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets a collection of the names of all this extension's properties
        /// </summary>
        public IEnumerable<string> PropertyNames
        {
            get { return _properties.Keys; }
        }

        /// <summary>
        /// Gets a collection of the values of a particular named property.
        /// If none are present, returns an empty enumerator.
        /// </summary>
        /// <param name="name">The property name</param>
        /// <returns>A collection of values</returns>
        public IEnumerable<string> GetValues(string name)
        {
            if (_properties.TryGetValue(name, out List<string>? value))
                return value;
            else
#if NET20
                return new string[0];
#else
                return Enumerable.Empty<string>();
#endif
        }

        /// <summary>
        /// Gets an object of the specified extension type, loading the Assembly
        /// and creating the object as needed. Note that this property always
        /// returns the same object. Use CreateExtensionObject if a new one is
        /// needed each time or to specify arguments.
        /// </summary>
        public object ExtensionObject
        {
            get
            {
                if (_extensionObject is null)
                    _extensionObject = CreateExtensionObject();

                return _extensionObject;
            }
        }

        /// <summary>
        /// Gets a newly created extension object, created in the current application domain
        /// </summary>
        public object CreateExtensionObject(params object[] args)
        {
            try
            {
#if NETFRAMEWORK
            return AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(
                AssemblyPath, TypeName, false, 0, null, args, null, null, null);
#else
                var assembly = Assembly.LoadFrom(AssemblyPath);
                var type = assembly.GetType(TypeName, throwOnError: true)!;
                return Activator.CreateInstance(type, args)!;
#endif
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;
                throw new ExtensibilityException("Error in constructing extension object", ex);
            }
        }

#endregion

        public void AddProperty(string name, string val)
        {
            if (_properties.TryGetValue(name, out List<string>? list))
                list.Add(val);
            else
            {
                list = new List<string> { val };
                _properties.Add(name, list);
            }
        }

        public override string ToString()
        {
            return $"{TypeName} - {Path}";
        }
    }
}
