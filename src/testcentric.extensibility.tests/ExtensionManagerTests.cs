// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TestCentric.Extensibility
{
    public class ExtensionManagerTests
    {
        protected static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionManagerTests).Assembly;
        protected static readonly string THIS_ASSEMBLY_DIRECTORY = Path.GetDirectoryName(THIS_ASSEMBLY.Location);

        protected ExtensionManager ExtensionManager;

        public ExtensionManagerTests()
        {
            ExpectedExtensionPointPaths = new[]
            {
                "/TestCentric/Engine/AgentLaunchers",
                "/TestCentric/Engine/TestEventListeners",
                "/TestCentric/Engine/DriverFactories",
                "/TestCentric/Engine/ResultWriters",
                "/TestCentric/Engine/ProjectLoaders"
            };

            // This could be initialized inline, but it's here for clarity
            ExpectedExtensionPointTypes = new[]
            {
                typeof(TestCentric.Engine.Extensibility.IAgentLauncher),
                typeof(NUnit.Engine.ITestEventListener),
                typeof(NUnit.Engine.Extensibility.IDriverFactory),
                typeof(NUnit.Engine.Extensibility.IResultWriter),
                typeof(NUnit.Engine.Extensibility.IProjectLoader),
            };
        }

        protected string[] ExpectedExtensionPointPaths;

        protected Type[] ExpectedExtensionPointTypes;

        [OneTimeSetUp]
        public void CreateManager()
        {
            ExtensionManager = new ExtensionManager();

            // Initialize ExtensionManager using extension points defined in this
            // assembly and extensions defined in the fake extensions assembly.

            ExtensionManager.FindExtensionPoints(THIS_ASSEMBLY);
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
            "TestCentric.Engine.Extensibility.FakeAgentLauncher_ThrowsInConstructor",
            "TestCentric.Engine.Extensibility.FakeTestEventListener",
            "TestCentric.Engine.Extensibility.FakeProjectLoader",
            "TestCentric.Engine.Extensibility.FakeResultWriter",
            "TestCentric.Engine.Extensibility.FakeNUnitExtension_ThrowsInConstructor"
        ];

        [Test]
        public void AllExtensionsAreKnown()
        {
            Assert.That(ExtensionManager.Extensions.Select(ep => ep.TypeName), Is.EquivalentTo(KnownExtensionTypeNames));
        }

        // Run this first as subsequent test will enable the extension
        [Test, Order(1)]
        public void ExtensionMayBeDisabledByDefault()
        {
            Assert.That(ExtensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("TestCentric.Engine.Extensibility.FakeTestEventListener")
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            ExtensionManager.EnableExtension("TestCentric.Engine.Extensibility.FakeTestEventListener", true);

            Assert.That(ExtensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("TestCentric.Engine.Extensibility.FakeTestEventListener")
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test]
        public void NUnitExtensionThrowsInConstructor()
        {
            string typeName = "TestCentric.Engine.Extensibility.FakeNUnitExtension_ThrowsInConstructor";
            var exNode = ExtensionManager.Extensions.Where(n => n.TypeName == typeName).Single();

            // Although the constructor throws, we don't get an exception.
            // However, the node contains the error information.
            Assert.DoesNotThrow(() => { var o = exNode.ExtensionObject; });
            Assert.That(exNode.Status, Is.EqualTo(ExtensionStatus.Error));
            Assert.That(exNode.Exception, Is.InstanceOf<ExtensibilityException>());
            Assert.That(exNode.Exception.InnerException, Is.InstanceOf<NotImplementedException>());
        }

        [Test]
        public void TestCentricExtensionThrowsInConstructor()
        {
            string typeName = "TestCentric.Engine.Extensibility.FakeAgentLauncher_ThrowsInConstructor";
            var exNode = ExtensionManager.Extensions.Where(n => n.TypeName == typeName).Single();

            // Although the constructor throws, we don't get an exception.
            // However, the node contains the error information.
            Assert.DoesNotThrow(() => { var o = exNode.ExtensionObject; });
            Assert.That(exNode.Status, Is.EqualTo(ExtensionStatus.Error));
            Assert.That(exNode.Exception, Is.InstanceOf<ExtensibilityException>());
            Assert.That(exNode.Exception.InnerException, Is.InstanceOf<NotImplementedException>());
        }

#if NETCOREAPP
        [TestCase("netstandard2.0", ExpectedResult = true)]
        [TestCase("net462", ExpectedResult = false)]
        [TestCase("net20", ExpectedResult = false)]
#elif NET40_OR_GREATER
        [TestCase("netstandard2.0", ExpectedResult = false)]
        [TestCase("net462", ExpectedResult = true)]
        [TestCase("net20", ExpectedResult = true)]
#else
        [TestCase("netstandard2.0", ExpectedResult = false)]
        [TestCase("net462", ExpectedResult = false)]
        [TestCase("net20", ExpectedResult = true)]
#endif
        public bool LoadTargetFramework(string tfm)
        {
            return ExtensionManager.CanLoadTargetFramework(THIS_ASSEMBLY, FakeExtensions(tfm));
        }

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
