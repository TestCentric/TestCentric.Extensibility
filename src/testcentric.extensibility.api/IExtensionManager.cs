// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;

namespace TestCentric.Extensibility
{

    public interface IExtensionManager
    {
        #region Extension Points

        /// <summary>
        /// Gets an enumeration of all ExtensionPoints in the engine.
        /// </summary>
        IEnumerable<IExtensionPoint> ExtensionPoints { get; }

        /// <summary>
        /// Find all ExtensionPoints in a list of assemblies and add them to the ExtensionPoints property.
        /// </summary>
        /// <param name="assemblies">The assemblies to be examined for ExtensionPoints</param>
        /// <returns>The current instance of IExtensionManager</returns>
        IExtensionManager FindExtensionPoints(params Assembly[] assemblies);

        /// <summary>
        /// Gets an IExtensionPoint given its path
        /// </summary>
        /// <param name="path">A string representing an extension point path</param>
        /// <returns>An IExtensionPoint</returns>
        IExtensionPoint GetExtensionPoint(string path);

        #endregion

        #region Extensions

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        IEnumerable<IExtensionNode> Extensions { get; }

        /// <summary>
        /// Find all extensions starting from a given directory.
        /// </summary>
        /// <param name="startDir">
        /// Path to the directory containing the initial .addins files used to locate extensions
        /// </param>
        /// <returns>The current instance of IExtensionManager</returns>
        IExtensionManager FindExtensions(string startDir);

        /// <summary>
        /// Enumerates all extension objects matching a given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IEnumerable<T> GetExtensions<T>();

        /// <summary>
        /// Enable or disable an extension. 
        /// </summary>
        /// <remarks>
        /// The TypeName is used rather than a Type so that clients without a
        /// reference to the extension Type can use it.
        /// </remarks>
        void EnableExtension(string typeName, bool enabled);

        #endregion

        #region Extension Nodes

        /// <summary>
        /// Gets an IExtensionNode given its path
        /// </summary>
        /// <param name="path">A string representing an extension point path</param>
        /// <returns>An IExtensionNode</returns>
        IExtensionNode GetExtensionNode(string path);

        /// <summary>
        /// Enumerates all extension nodes at a given path
        /// </summary>
        /// <param name="path">A string representing an extension point path</param>
        /// <returns>An enumeration of IExtensionNodes</returns>
        IEnumerable<IExtensionNode> GetExtensionNodes(string path);

        /// <summary>
        /// Enumerates all extension nodes of a given type
        /// </summary>
        /// <typeparam name="T">A class or interface Type</typeparam>
        /// <param name="includeDisabled">True to include disabled extensions, otherwise they are excluded</param>
        /// <returns></returns>
        IEnumerable<IExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false);

        #endregion
    }
}
