// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if false // TODO: Re-design this test fixture
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TestCentric.Extensibility
{
    [TestFixture(null)]
    [TestFixture("/TestCentric/Engine/TypeExtensions/")]
    public class ExtensionManagerTests
    {
        protected static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionManagerTests).Assembly;
        protected static readonly string THIS_ASSEMBLY_DIRECTORY = Path.GetDirectoryName(THIS_ASSEMBLY.Location);

        protected static readonly Assembly TESTCENTRIC_ENGINE_API = typeof(TestCentric.Engine.Extensibility.IDriverFactory).Assembly;
        protected static readonly Assembly NUNIT_ENGINE_API = typeof(NUnit.Engine.Extensibility.IDriverFactory).Assembly;

        protected string DefaultTypeExtensionsPath;
        protected ExtensionManager ExtensionManager;

        public ExtensionManagerTests(string defaultTypeExtensionsPath)
        {
            DefaultTypeExtensionsPath = defaultTypeExtensionsPath;

            var prefix = defaultTypeExtensionsPath ?? "/TestCentric/TypeExtensions/";

            ExpectedExtensionPointPaths = new[]
            {
                prefix + "ITestEventListener",
                prefix + "IService",
                prefix + "IAgentLauncher",
                prefix + "IDriverFactory",
                prefix + "IProjectLoader",
                prefix + "IResultWriter",
                //"/NUnit/Engine/TypeExtensions/ITestEventListener",
                //"/NUnit/Engine/TypeExtensions/IService",
                //"/NUnit/Engine/TypeExtensions/IDriverFactory",
                //"/NUnit/Engine/TypeExtensions/IResultWriter",
                //"/NUnit/Engine/TypeExtensions/IProjectLoader",
#if !NET35
                "/NUnit/Engine/TypeExtensions/IAgentLauncher"
#endif
            };

            // This could be initialized inline, but it's here for clarity
            ExpectedExtensionPointTypes = new[]
            {
                typeof(TestCentric.Engine.ITestEventListener),
                typeof(TestCentric.Engine.Services.IService),
                typeof(TestCentric.Engine.Extensibility.IAgentLauncher),
                typeof(TestCentric.Engine.Extensibility.IDriverFactory),
                typeof(TestCentric.Engine.Extensibility.IProjectLoader),
                typeof(TestCentric.Engine.Extensibility.IResultWriter),
                typeof(NUnit.Engine.ITestEventListener),
                typeof(NUnit.Engine.IService),
                typeof(NUnit.Engine.Extensibility.IDriverFactory),
                typeof(NUnit.Engine.Extensibility.IResultWriter),
                typeof(NUnit.Engine.Extensibility.IProjectLoader),
#if !NET35
                typeof(NUnit.Engine.Extensibility.IAgentLauncher)
#endif
            };
        }

        protected string[] ExpectedExtensionPointPaths;

        protected Type[] ExpectedExtensionPointTypes;

        [OneTimeSetUp]
        public void CreateManager()
        {
            ExtensionManager = new ExtensionManager();
            if (DefaultTypeExtensionsPath != null)
                ExtensionManager.TypeExtensionPath = DefaultTypeExtensionsPath;

            // Initialize ExtensionManager using extension points in TestCentric API assembly
            // with fake extensions defined in this assembly.

            ExtensionManager.FindExtensionPoints(TESTCENTRIC_ENGINE_API, NUNIT_ENGINE_API);
            Assert.That(ExtensionManager.ExtensionPoints.Count, Is.GreaterThan(0), "No ExtensionPoints were found");

            ExtensionManager.FindExtensionAssemblies(FAKE_EXTENSIONS_PARENT_DIRECTORY);
            ExtensionManager.CompleteExtensionDiscovery();
            Assert.That(ExtensionManager.Extensions.Count, Is.GreaterThan(0), "No Extensions were found");
        }

        #region Extension Point Tests

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(ExtensionManager.ExtensionPoints.Select(ep => ep.Path),
                Is.EquivalentTo(ExpectedExtensionPointPaths));
        }

        [Test]
        public void GetExtensionPointsByPath()
        {
            for (int i = 0; i < ExpectedExtensionPointPaths.Length; i++)
            {
                var path = ExpectedExtensionPointPaths[i];
                var ep = ExtensionManager.GetExtensionPoint(path);
                Assert.That(ep, Is.Not.Null, $"Unable to get ExtensionPoint for {path}");
                Assert.That(ep.Path, Is.EqualTo(path));
                Assert.That(ep.TypeName, Is.EqualTo(ExpectedExtensionPointTypes[i].FullName));
            }
        }

        [Test]
        public void CanGetExtensionPointByType()
        {
            for (int i = 0; i < ExpectedExtensionPointTypes.Length; i++)
            {
                var type = ExpectedExtensionPointTypes[i];
                var ep = ExtensionManager.GetExtensionPoint(type);
                Assert.That(ep, Is.Not.Null, $"Unable to get ExtensionPoint for {type.FullName}");
                Assert.That(ep.Path, Is.EqualTo(ExpectedExtensionPointPaths[i]));
                Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
            }
        }

        [Test]
        public void UnknownExtensionPointPathReturnsNull()
        {
            Assert.That(ExtensionManager.GetExtensionPoint("/Path/Does/Not/Exist"), Is.Null);
        }

        [Test]
        public void UnknownExtensionPointTypeReturnsNull()
        {
            Assert.That(ExtensionManager.GetExtensionPoint(typeof(ThisIsNotAnExtensionPoint)), Is.Null);
        }

        internal class ThisIsNotAnExtensionPoint
        {
        }

        #endregion

        #region Extensions

        private static string[] KnownExtensionTypeNames =
        [
            "TestCentric.Engine.Extensibility.FakeAgentLauncher",
            "TestCentric.Engine.Extensibility.FakeTestEventListener",
            "TestCentric.Engine.Extensibility.FakeExtension_ThrowsInConstructor",
#if !NET35
            "NUnit.Engine.Extensibility.FakeProjectLoader"
#endif
        ];

        [Test]
        public void AllExtensionsAreKnown()
        {
            Assert.That(ExtensionManager.Extensions.Select(ep => ep.TypeName), Is.EquivalentTo(KnownExtensionTypeNames));
        }

        //// Run this first as subsequent test will enable the extension
        //[Test, Order(1)]
        //public void ExtensionMayBeDisabledByDefault()
        //{
        //    Assert.That(ExtensionManager.Extensions,
        //        Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("TestCentric.Engine.Extensibility.FakeTestEventListener")
        //           .And.Property(nameof(ExtensionNode.Enabled)).False);
        //}

        //[Test]
        //public void DisabledExtensionMayBeEnabled()
        //{
        //    ExtensionManager.EnableExtension("TestCentric.Engine.Extensibility.FakeTestEventListener", true);

        //    Assert.That(ExtensionManager.Extensions,
        //        Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("TestCentric.Engine.Extensibility.FakeTestEventListener")
        //           .And.Property(nameof(ExtensionNode.Enabled)).True);
        //}

        //[Test]
        //public void ExtensionThrowsInConstructor()
        //{
        //    string typeName = "TestCentric.Engine.Extensibility.FakeExtension_ThrowsInConstructor";
        //    var exNode = ExtensionManager.Extensions.Where(n => n.TypeName == typeName).Single();

        //    // Although the constructor throws, we don't get an exception.
        //    // However, the node contains the error information.
        //    Assert.DoesNotThrow(() => { var o = exNode.ExtensionObject; });
        //    Assert.That(exNode.Status, Is.EqualTo(ExtensionStatus.Error));
        //    Assert.That(exNode.Exception, Is.InstanceOf<ExtensibilityException>());
        //    Assert.That(exNode.Exception.InnerException, Is.InstanceOf<NotImplementedException>());
        //}

//#if NETCOREAPP
//        [TestCase("netstandard2.0", ExpectedResult = true)]
//        [TestCase("net462", ExpectedResult = false)]
//        //[TestCase("net20", ExpectedResult = false)]
//#elif NET40_OR_GREATER
//        [TestCase("netstandard2.0", ExpectedResult = false)]
//        [TestCase("net462", ExpectedResult = true)]
//        //[TestCase("net20", ExpectedResult = true)]
//#else
//        [TestCase("netstandard2.0", ExpectedResult = false)]
//        [TestCase("net462", ExpectedResult = false)]
//        //[TestCase("net20", ExpectedResult = true)]
//#endif
//        public bool LoadTargetFramework(string tfm)
//        {
//            return ExtensionManager.CanLoadTargetFramework(THIS_ASSEMBLY, FakeExtensions(tfm));
//        }
        #endregion

        private const string FAKE_EXTENSIONS_FILENAME = "TestCentric.Extensibility.FakeExtensions.dll";
        private static readonly string FAKE_EXTENSIONS_PARENT_DIRECTORY =
            Path.Combine(new DirectoryInfo(THIS_ASSEMBLY_DIRECTORY).Parent.Parent.FullName, "fakes");

        /// <summary>
        /// Returns an ExtensionAssembly referring to a particular build of the fake test extensions
        /// assembly based on the argument provided.
        /// </summary>
        /// <param name="tfm">A test framework moniker. Must be one for which the fake extensions are built.</param>
        /// <returns></returns>
        private static ExtensionAssembly FakeExtensions(string tfm)
        {
            return new ExtensionAssembly(
                Path.Combine(FAKE_EXTENSIONS_PARENT_DIRECTORY, Path.Combine(tfm, FAKE_EXTENSIONS_FILENAME)), false);
        }
    }
}
#endif
