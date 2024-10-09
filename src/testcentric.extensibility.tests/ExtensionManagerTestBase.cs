// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

// TODO: Get this test working under .NET 6.0
#if NETFRAMEWORK
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
//using NUnit.Engine;

namespace TestCentric.Extensibility
{
    // NOTE For the time being we are maintaining two APIs, the older deprecated
    // API and the one that we will use going forward. This class is the common 
    // base used by the tests of both APIs. At some point, the deprecated API
    // members will be removed and this will then be combined in one class.

    public abstract class ExtensionManagerTestBase
    {
        protected static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionManager_NewApi).Assembly;
        protected static readonly string THIS_ASSEMBLY_DIRECTORY = Path.GetDirectoryName(THIS_ASSEMBLY.Location);
        protected static readonly Assembly TESTCENTRIC_ENGINE_API = typeof(TestCentric.Engine.Extensibility.IDriverFactory).Assembly;
        protected static readonly Assembly NUNIT_ENGINE_API = typeof(NUnit.Engine.Extensibility.IDriverFactory).Assembly;

        protected string DefaultTypeExtensionsPath;
        protected ExtensionManager ExtensionManager;

        public ExtensionManagerTestBase(string defaultTypeExtensionsPath)
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
                "/NUnit/Engine/TypeExtensions/ITestEventListener",
                "/NUnit/Engine/TypeExtensions/IService",
                "/NUnit/Engine/TypeExtensions/IDriverFactory",
                "/NUnit/Engine/TypeExtensions/IProjectLoader",
                "/NUnit/Engine/TypeExtensions/IResultWriter"
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
                typeof(NUnit.Engine.Extensibility.IProjectLoader),
                typeof(NUnit.Engine.Extensibility.IResultWriter)
            };
        }

        protected string[] ExpectedExtensionPointPaths;

        protected Type[] ExpectedExtensionPointTypes;

        #region Extension Point Tests

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(ExtensionManager.ExtensionPoints.Select(ep => ep.Path), 
                Is.EquivalentTo(ExpectedExtensionPointPaths) );
        }

        [Test]
        public void GetExtensionPointsByPath()
        {
            for (int i = 0; i < ExpectedExtensionPointPaths.Length; i++)
            {
                var path = ExpectedExtensionPointPaths[i];
                var ep = ExtensionManager.GetExtensionPoint(path);
                Assert.NotNull(ep, $"Unable to get ExtensionPoint for {path}");
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
                Assert.NotNull(ep, $"Unable to get ExtensionPoint for {type.FullName}");
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

        class ThisIsNotAnExtensionPoint { }

        #endregion

        #region Extensions

        static string[] KnownExtensionTypeNames = new[] {
            "TestCentric.Engine.Extensibility.FakeAgentLauncher",
            "TestCentric.Engine.Extensibility.FakeTestEventListener",
            "NUnit.Engine.Extensibility.FakeProjectLoader"
        };

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
        public void SkipsGracefullyLoadingOtherFrameworkExtensionAssembly()
        {
            //May be null on mono
            Assume.That(Assembly.GetEntryAssembly(), Is.Not.Null, "Entry assembly is null, framework loading validation will be skipped.");

#if NETCOREAPP
        var assemblyName = Path.Combine(GetSiblingDirectory("net462"), "TestCentric.Engine.Api.dll");
#else
        var assemblyName = Path.Combine(GetSiblingDirectory("net6.0"), "TestCentric.Engine.Api.dll");
#endif
            Assert.That(assemblyName, Does.Exist);

            var manager = new ExtensionManager();
            //manager.FindExtensionPoints(typeof(DriverService).Assembly);
            manager.FindExtensionPoints(typeof(Engine.ITestEngine).Assembly);
            var extensionAssembly = new ExtensionAssembly(assemblyName, false);

            Assert.That(() => manager.FindExtensionsInAssembly(extensionAssembly), Throws.Nothing);
        }

        [TestCaseSource(nameof(ValidCombos))]
        public void ValidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.True);
        }

        [TestCaseSource(nameof(InvalidTargetFrameworkCombos))]
        public void InvalidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.False);
        }

        [TestCaseSource(nameof(InvalidRunnerCombos))]
        public void InvalidRunnerTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Throws.Exception.And.Message.Contains("not .NET Standard"));
        }

        #endregion

        // ExtensionAssembly is internal, so cannot be part of the public test parameters
        public struct FrameworkCombo
        {
            internal Assembly RunnerAssembly { get; }
            internal ExtensionAssembly ExtensionAssembly { get; }

            internal FrameworkCombo(Assembly runnerAsm, ExtensionAssembly extensionAsm)
            {
                RunnerAssembly = runnerAsm;
                ExtensionAssembly = extensionAsm;
            }

            public override string ToString() =>
                $"{RunnerAssembly.GetName()}:{ExtensionAssembly.AssemblyName}";
        }

        public static IEnumerable<TestCaseData> ValidCombos()
        {
#if NETCOREAPP
            Assembly netstandard = typeof(ExtensionManager).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetStandard)).SetName("ValidCombo(.NET Core, .NET Standard)");
            yield return new TestCaseData(new FrameworkCombo(netcore, extNetCore)).SetName("ValidCombo(.NET Core, .Net Core)");
#else
            Assembly netFramework = typeof(ExtensionManager).Assembly;

            var extNetFramework = new ExtensionAssembly(netFramework.Location, false);
            var extNetStandard = new ExtensionAssembly(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestCentric.Engine.Api.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetFramework)).SetName("ValidCombo(.NET Framework, .NET Framework)");
            //yield return new TestCaseData(new FrameworkCombo(netFramework, extNetStandard)).SetName("ValidCombo(.NET Framework, .NET Standard)");
#endif
        }

        public static IEnumerable<TestCaseData> InvalidTargetFrameworkCombos()
        {
#if NETCOREAPP
            Assembly netstandard = typeof(ExtensionManager).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetNetFrameworkSiblingDirectory(), "testcentric.engine.core.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetFramework)).SetName("InvalidCombo(.NET Core, .NET Framework)");
#else
            Assembly netFramework = typeof(ExtensionManager).Assembly;


            var netCoreAppDir = GetSiblingDirectory("net6.0");
            var extNetStandard = new ExtensionAssembly(Path.Combine(netCoreAppDir, "TestCentric.Engine.Api.dll"), false);
            var extNetCoreApp = new ExtensionAssembly(Path.Combine(netCoreAppDir, "TestCentric.Extensibility.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetCoreApp)).SetName("InvalidCombo(.NET Framework, .NET Core)");
#endif

        }

        public static IEnumerable<TestCaseData> InvalidRunnerCombos()
        {
#if NETCOREAPP
            Assembly netstandard = typeof(ExtensionManager).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetNetFrameworkSiblingDirectory(), "testcentric.extensibility.tests.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netstandard, extNetStandard)).SetName("InvalidCombo(.NET Standard, .NET Standard)");
            yield return new TestCaseData(new FrameworkCombo(netstandard, extNetCore)).SetName("InvalidCombo(.NET Standard, .NET Core)");
            yield return new TestCaseData(new FrameworkCombo(netstandard, extNetFramework)).SetName("InvalidCombo(.NET Standard, .NET Framework)");
#else
            return new List<TestCaseData>();
#endif
        }

        /// <summary>
        /// Returns a directory in the parent directory that the current test assembly is in. This
        /// is used to load assemblies that target different frameworks than the current tests. So
        /// if these tests are in bin\release\net35 and dir is netstandard2.0, this will return
        /// bin\release\netstandard2.0.
        /// </summary>
        /// <param name="dir">The sibling directory</param>
        /// <returns></returns>
        private static string GetSiblingDirectory(string dir)
        {
            var file = new FileInfo(typeof(ExtensionManagerTestBase).Assembly.Location);
            return Path.Combine(file.Directory.Parent.FullName, dir);
        }
    }  
}
#endif
