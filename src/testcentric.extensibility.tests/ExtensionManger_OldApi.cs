// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

// TODO: Get this test working under .NET 8.0
#if NETFRAMEWORK
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using System.Diagnostics;

namespace TestCentric.Extensibility
{
    [TestFixture(null)]
    [TestFixture("/TestCentric/Engine/TypeExtensions/")]
    public class ExtensionManager_OldApi : ExtensionManagerTestBase
    {
        public ExtensionManager_OldApi(string defaultTypeExtensionsPath)
            : base(defaultTypeExtensionsPath)
        {
            PrefixWasProvided = defaultTypeExtensionsPath != null;
        }

        bool PrefixWasProvided { get; set; }

        [OneTimeSetUp]
        public void CreateManager()
        {
#pragma warning disable 612, 618
            ExtensionManager = new ExtensionManager(TESTCENTRIC_ENGINE_API, NUNIT_ENGINE_API) { InitialAddinsDirectory = THIS_ASSEMBLY_DIRECTORY };

            if (PrefixWasProvided)
                ExtensionManager.DefaultTypeExtensionPrefix = DefaultTypeExtensionsPath;

            ExtensionManager.Initialize();
#pragma warning restore
            Assert.That(ExtensionManager.ExtensionPoints.Count, Is.GreaterThan(0), "No ExtensionPoints were found");
            Assert.That(ExtensionManager.Extensions.Count, Is.GreaterThan(0), "No Extensions were found");
        }
    }  
}
#endif
