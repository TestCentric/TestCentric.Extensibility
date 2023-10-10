// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

// ExtensionPoint specified at assembly level - we use this technique for the NUnit V2 Fraemwork Driver.
[assembly: TestCentric.Extensibility.ExtensionPoint("/TestCentric/DoesSomething", typeof(TestCentric.Extensibility.IDoSomething),
    Description = "Extension point specified at assembly level")]

namespace TestCentric.Extensibility
{
    // TypeExtensionPoints

    [TypeExtensionPoint(Description = "TypeExtensionPoint 1")]
    public interface IDoSomething { }

    [TypeExtensionPoint(Description = "TypeExtensionPoint 2")]
    public interface IDoSomethingElse { }

    [TypeExtensionPoint(Description = "TypeExtensionPoint 3")]
    public interface IDoYetAnotherThing { }
}
