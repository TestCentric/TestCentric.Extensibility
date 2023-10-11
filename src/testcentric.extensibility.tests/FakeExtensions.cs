// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

namespace TestCentric.Extensibility
{
    // Extensions

    [Extension]
    public class DoesSomething : IDoSomething { }

    [Extension(Path = "/TestCentric/DoesSomething")]
    public class DoesSomething2 : IDoSomething { }

    [NUnit.Engine.Extensibility.Extension]
    public class NUnitExtension : IDoSomething { }

    [Extension(EngineVersion = "1.0.0-dev00010")] // Should not throw - bug #1
    public class DoesSomethingElse : IDoSomethingElse { }

    [Extension(Enabled = false)]
    public class DoesYetAnotherThing : IDoYetAnotherThing { }
}
