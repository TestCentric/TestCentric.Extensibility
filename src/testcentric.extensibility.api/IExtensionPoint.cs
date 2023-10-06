// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Collections.Generic;

namespace TestCentric.Extensibility
{
    public interface IExtensionPoint
    {
        string Path { get; }

        string Description { get; }

        string TypeName { get; }

        IEnumerable<IExtensionNode> Extensions { get; }
    }
}
