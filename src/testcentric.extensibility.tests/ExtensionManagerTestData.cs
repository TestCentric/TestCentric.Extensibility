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

    [Extension]
    public class DoesSomethingElse : IDoSomethingElse { }

    [Extension(Enabled = false)]
    public class DoesYetAnotherThing : IDoYetAnotherThing { }

    // TestData

    public class ExtensionManagerTestData
    {
        public string Path;
        public Type ExtensionPointType;
        public Type ExtensionType;

        public ExtensionManagerTestData(string path, Type type, Type extension)
        {
            Path = path;
            ExtensionPointType = type;
            ExtensionType = extension;
        }

        public override string ToString()
        {
            return $"Path=\"{Path}\", ExtensionPointType={ExtensionPointType.Name}, ExtensionType = {ExtensionType.Name}";
        }

        public static readonly ExtensionManagerTestData[] Examples = new ExtensionManagerTestData[]
        {
            new ExtensionManagerTestData("/NUnit/Engine/TypeExtensions/IDoSomething", typeof(IDoSomething), typeof(DoesSomething)),
            new ExtensionManagerTestData("/NUnit/Engine/TypeExtensions/IDoSomethingElse", typeof(IDoSomethingElse), typeof(DoesSomethingElse)),
            new ExtensionManagerTestData("/NUnit/Engine/TypeExtensions/IDoYetAnotherThing", typeof(IDoYetAnotherThing), typeof(DoesYetAnotherThing)),
            new ExtensionManagerTestData("/TestCentric/DoesSomething", typeof(IDoSomething), typeof(DoesSomething))

        };
    }
}
