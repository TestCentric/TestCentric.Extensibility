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
        string TypeName { get; }

        bool Enabled { get; }

        string Path { get; }

        string Description { get; }

        // TODO: Determine whether we need this or some other kind
        // of info about the target framework.
        //IRuntimeFramework TargetFramework { get; }

        IEnumerable<string> PropertyNames { get; }

        string AssemblyPath { get; }

        Version AssemblyVersion { get; }

        IEnumerable<string> GetValues(string name);
    }
}
