// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCentric.Extensibility
{
    [TestFixture(null)]
    [TestFixture("/TestCentric/Engine/TypeExtensions/")]
    public class ExtensionManager_NewApi : ExtensionManagerTestBase
    {
        public ExtensionManager_NewApi(string defaultTypeExtensionsPath) : base(defaultTypeExtensionsPath) { }

        [OneTimeSetUp]
        public void CreateManager()
        {
            if (DefaultTypeExtensionsPath != null)
                ExtensionManager = new ExtensionManager(DefaultTypeExtensionsPath);
            else
            {
                ExtensionManager = new ExtensionManager();
                DefaultTypeExtensionsPath = "/TestCentric/TypeExtensions/";
            }

            // Initialize ExtensionManager using extension points in TestCentric API assembly
            // with fake extensions defined in this assembly.

            ExtensionManager.FindExtensionPoints(TESTCENTRIC_ENGINE_API, NUNIT_ENGINE_API);
            Assert.That(ExtensionManager.ExtensionPoints.Count, Is.GreaterThan(0), "No ExtensionPoints were found");

            ExtensionManager.FindExtensions(THIS_ASSEMBLY_DIRECTORY);
            Assert.That(ExtensionManager.Extensions.Count, Is.GreaterThan(0), "No Extensions were found");
        }
    }
}
