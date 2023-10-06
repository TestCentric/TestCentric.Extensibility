// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Collections.Generic;
using System.Reflection;

namespace TestCentric.Extensibility
{
    public interface IExtensionManager
    {
        #region Properties

        /// <summary>
        /// Array of assemblies whose ExtensionPoints are manaaged by this instance.
        /// </summary>
        IList<Assembly> RootAssemblies { get; }

        /// <summary>
        /// Prefix used if the Path is not specified for a TypeExtensionPoint
        /// </summary>
        string DefaultTypeExtensionPrefix { get; set; }

        /// <summary>
        /// Directory containing the initial .addins files used to locate extensions
        /// </summary>
        string InitialAddinsDirectory { get; set; }

        /// <summary>
        /// Gets an enumeration of all ExtensionPoints in the engine.
        /// </summary>
        IEnumerable<IExtensionPoint> ExtensionPoints { get; }

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        IEnumerable<IExtensionNode> Extensions { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize this instance of ExtensionManager by finding
        /// all extension points and extensions.
        /// </summary>
        void Initialize();

        IExtensionPoint GetExtensionPoint(string path);

        IEnumerable<T> GetExtensions<T>();

        IEnumerable<IExtensionNode> GetExtensionNodes(string path);

        IEnumerable<IExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false);

        void EnableExtension(string typeName, bool enabled);

        #endregion
    }
}
