﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Collections.Generic;

namespace TestCentric.Extensibility
{
    /// <summary>
    /// An ExtensionPoint represents a single point in the TestEngine
    /// that may be extended by user addins and extensions.
    /// </summary>
    public interface IExtensionPoint
    {
        /// <summary>
        /// Gets the unique path identifying this extension point.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the description of this extension point. May be null.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the FullName of the Type required for any extension to be installed at this extension point.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets an enumeration of IExtensionNodes for extensions installed on this extension point.
        /// </summary>
        IEnumerable<IExtensionNode> Extensions { get; }
    }
}
