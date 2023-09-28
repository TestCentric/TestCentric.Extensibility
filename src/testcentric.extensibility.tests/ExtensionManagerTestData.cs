// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

// Use aliases so we can later switch to TestCentric attibutes
using ExtensionPointAttribute = NUnit.Engine.Extensibility.ExtensionPointAttribute;
using TypeExtensionPointAttribute = NUnit.Engine.Extensibility.TypeExtensionPointAttribute;
using ExtensionAttribute = NUnit.Engine.Extensibility.ExtensionAttribute;
using NUnit.Engine.Extensibility;
using System;
using NUnit.Framework;

// ExtensionPoint specified at assembly level - we use this technique for the NUnit V2 Fraemwork Driver.
[assembly: ExtensionPoint("/TestCentric/DoesSomething", typeof(TestCentric.Extensibility.IDoSomething),
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

    // Extensions

    [Extension]
    public class DoesSomething : IDoSomething { }

    [Extension(Path = "/TestCentric/DoesSomething")]
    public class DoesSomething2 : IDoSomething { }

    [Extension(EngineVersion = "2.0.0-dev00010")] // Should not throw - bug #1
    public class DoesSomethingElse : IDoSomethingElse { }

    [Extension(Enabled = false)]
    public class DoesYetAnotherThing : IDoYetAnotherThing { }

    // TestData

    public class ExtensionManagerTestData
    {
        public string Prefix;
        public string Path;
        public Type ExtensionPointType;
        public Type ExtensionType;

        public ExtensionManagerTestData(string prefix, string path, Type type, Type extension)
        {
            Prefix = prefix;
            Path = path;
            ExtensionPointType = type;
            ExtensionType = extension;
        }

        public override string ToString()
        {
            return $"Prefix={Prefix ?? "<null>"} Path=\"{Path}\", ExtensionPointType={ExtensionPointType.Name}, ExtensionType = {ExtensionType.Name}";
        }

        public static readonly ExtensionManagerTestData[] Examples = new ExtensionManagerTestData[]
        {
            new ExtensionManagerTestData(null, "/TestCentric/TypeExtensions/IDoSomething", typeof(IDoSomething), typeof(DoesSomething)),
            new ExtensionManagerTestData(null, "/TestCentric/TypeExtensions/IDoSomethingElse", typeof(IDoSomethingElse), typeof(DoesSomethingElse)),
            new ExtensionManagerTestData(null, "/TestCentric/TypeExtensions/IDoYetAnotherThing", typeof(IDoYetAnotherThing), typeof(DoesYetAnotherThing)),
            new ExtensionManagerTestData(null, "/TestCentric/DoesSomething", typeof(IDoSomething), typeof(DoesSomething)),
            new ExtensionManagerTestData("/TestCentric/Engine/TypeExtensions/", "/TestCentric/Engine/TypeExtensions/IDoSomething", typeof(IDoSomething), typeof(DoesSomething)),
            new ExtensionManagerTestData("/TestCentric/Engine/TypeExtensions/", "/TestCentric/Engine/TypeExtensions/IDoSomethingElse", typeof(IDoSomethingElse), typeof(DoesSomethingElse)),
            new ExtensionManagerTestData("/TestCentric/Engine/TypeExtensions/", "/TestCentric/Engine/TypeExtensions/IDoYetAnotherThing", typeof(IDoYetAnotherThing), typeof(DoesYetAnotherThing)),
            new ExtensionManagerTestData("/TestCentric/Engine/TypeExtensions/", "/TestCentric/DoesSomething", typeof(IDoSomething), typeof(DoesSomething))
        };
    }
}
