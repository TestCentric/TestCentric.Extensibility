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
    public class ExtensionManagerTests
    {
        private static readonly Assembly THIS_ASSEMBLY = typeof(IDoSomething).Assembly;
        private static readonly string THIS_ASSEMBLY_DIRECTORY = Path.GetDirectoryName(THIS_ASSEMBLY.Location);

        private ExtensionManager _extensionManager;

        public ExtensionManagerTests(string prefix)
        {
            PrefixWasProvided = prefix != null;
            Prefix = PrefixWasProvided ? prefix : "/TestCentric/TypeExtensions/";
        }

        string Prefix { get; set; }
        bool PrefixWasProvided { get; set; }

        IEnumerable<ExtensionPoint> ExpectedExtensionPoints
        {
            get
            {
                yield return new ExtensionPoint(Prefix + "IDoSomething", typeof(IDoSomething));
                yield return new ExtensionPoint(Prefix + "IDoSomethingElse", typeof(IDoSomethingElse));
                yield return new ExtensionPoint(Prefix + "IDoYetAnotherThing", typeof(IDoYetAnotherThing));
                yield return new ExtensionPoint("/TestCentric/DoesSomething", typeof(IDoSomething));
            }
        }

        [OneTimeSetUp]
        public void CreateManager()
        {
            _extensionManager = new ExtensionManager(THIS_ASSEMBLY) { InitialAddinsDirectory = THIS_ASSEMBLY_DIRECTORY };

            if (PrefixWasProvided)
                _extensionManager.DefaultTypeExtensionPrefix = Prefix;

            _extensionManager.Initialize();
        }

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(_extensionManager.ExtensionPoints.Select(ep => ep.Path), Is.EquivalentTo(ExpectedExtensionPoints.Select(ep => ep.Path))) ;
        }

        [TestCase(nameof(IDoSomething), typeof(IDoSomething))]
        [TestCase(nameof(IDoSomethingElse), typeof(IDoSomethingElse))]
        [TestCase(nameof(IDoYetAnotherThing), typeof(IDoYetAnotherThing))]
        [TestCase("/TestCentric/DoesSomething", typeof(IDoSomething))]
        public void CanGetExtensionPointByPath(string path, Type type)
        {
            if (path[0] != '/') path = Prefix + path;

            var ep = _extensionManager.GetExtensionPoint(path);
            Assert.NotNull(ep, "Unable to get ExtensionPoint");
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

        [TestCase(nameof(IDoSomething), typeof(IDoSomething))]
        [TestCase(nameof(IDoSomethingElse), typeof(IDoSomethingElse))]
        [TestCase(nameof(IDoYetAnotherThing), typeof(IDoYetAnotherThing))]
        public void CanGetExtensionPointByType(string path, Type type)
        {
            if (path[0] != '/') path = Prefix + path;

            var ep = _extensionManager.GetExtensionPoint(type);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

        [Test]
        public void UnknownExtensionPointPathReturnsNull()
        {
            Assert.That(_extensionManager.GetExtensionPoint("Path/Does/Not/Exist"), Is.Null);
        }

        [Test]
        public void UnknownExtensionPointTypeReturnsNull()
        {
            Assert.That(_extensionManager.GetExtensionPoint(typeof(ThisIsNotAnExtensionPoint)), Is.Null);
        }

        class ThisIsNotAnExtensionPoint { }

        static string[] KnownExtensions = new[] {
            "TestCentric.Extensibility.DoesSomething",
            "TestCentric.Extensibility.DoesSomething2",
            "TestCentric.Extensibility.DoesSomethingElse",
            "TestCentric.Extensibility.DoesYetAnotherThing",
            "TestCentric.Extensibility.NUnitExtension"
        };

        [Test]
        public void AllExtensionsAreKnown()
        {
            Assert.That(_extensionManager.Extensions.Select(ep => ep.TypeName), Is.EquivalentTo(KnownExtensions));
        }

        // Run this first as subsequent test will enable the extension
        [Test, Order(1)]
        public void ExtensionMayBeDisabledByDefault()
        {
            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("TestCentric.Extensibility.DoesYetAnotherThing")
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            _extensionManager.EnableExtension("TestCentric.Extensibility.DoesYetAnotherThing", true);

            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("TestCentric.Extensibility.DoesYetAnotherThing")
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        //[Test]
        //public void SkipsGracefullyLoadingOtherFrameworkExtensionAssembly()
        //{
        //    //May be null on mono
        //    Assume.That(Assembly.GetEntryAssembly(), Is.Not.Null, "Entry assembly is null, framework loading validation will be skipped.");

#if NETCOREAPP
        //var assemblyName = Path.Combine(GetNetFrameworkSiblingDirectory(), "testcentric.engine.core.tests.exe");
#else
            //var assemblyName = Path.Combine(GetNetCoreSiblingDirectory(), "testcentric.engine.core.tests.dll");
#endif
        //    Assert.That(assemblyName, Does.Exist);

        //    var manager = new ExtensionManager();
        //    //manager.FindExtensionPoints(typeof(DriverService).Assembly);
        //    manager.FindExtensionPoints(typeof(ITestEngine).Assembly);
        //    var extensionAssembly = new ExtensionAssembly(assemblyName, false);

        //    Assert.That(() => manager.FindExtensionsInAssembly(extensionAssembly), Throws.Nothing);
        //}

        //[TestCaseSource(nameof(ValidCombos))]
        //public void ValidTargetFrameworkCombinations(FrameworkCombo combo)
        //{
        //    Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
        //        Is.True);
        //}

        //[TestCaseSource(nameof(InvalidTargetFrameworkCombos))]
        //public void InvalidTargetFrameworkCombinations(FrameworkCombo combo)
        //{
        //    Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
        //        Is.False);
        //}

        //[TestCaseSource(nameof(InvalidRunnerCombos))]
        //public void InvalidRunnerTargetFrameworkCombinations(FrameworkCombo combo)
        //{
        //    Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
        //        Throws.Exception.TypeOf<NUnitEngineException>().And.Message.Contains("not .NET Standard"));
        //}

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
#if NETCOREAPP2_1
            Assembly netstandard = typeof(ExtensionManager).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetStandard)).SetName("ValidCombo(.NET Core, .NET Standard)");
            yield return new TestCaseData(new FrameworkCombo(netcore, extNetCore)).SetName("ValidCombo(.NET Core, .Net Core)");
#else
            Assembly netFramework = typeof(ExtensionManager).Assembly;

            var extNetFramework = new ExtensionAssembly(netFramework.Location, false);
            var extNetStandard = new ExtensionAssembly(Path.Combine(TestContext.CurrentContext.TestDirectory, "testcentric.engine.core.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetFramework)).SetName("ValidCombo(.NET Framework, .NET Framework)");
            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetStandard)).SetName("ValidCombo(.NET Framework, .NET Standard)");
#endif
        }

        public static IEnumerable<TestCaseData> InvalidTargetFrameworkCombos()
        {
#if NETCOREAPP2_1
            Assembly netstandard = typeof(ExtensionManager).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetNetFrameworkSiblingDirectory(), "testcentric.engine.core.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetFramework)).SetName("InvalidCombo(.NET Core, .NET Framework)");
#else
            Assembly netFramework = typeof(ExtensionManager).Assembly;


            var netCoreAppDir = GetNetCoreSiblingDirectory();
            var extNetStandard = new ExtensionAssembly(Path.Combine(netCoreAppDir, "testcentric.engine.core.dll"), false);
            var extNetCoreApp = new ExtensionAssembly(Path.Combine(netCoreAppDir, "testcentric.engine.core.tests.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetCoreApp)).SetName("InvalidCombo(.NET Framework, .NET Core)");
#endif

        }

        public static IEnumerable<TestCaseData> InvalidRunnerCombos()
        {
#if NETCOREAPP2_1
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
            var file = new FileInfo(typeof(ExtensionManagerTests).Assembly.Location);
            return Path.Combine(file.Directory.Parent.FullName, dir);
        }

        private static string GetNetFrameworkSiblingDirectory()
        {
            return GetSiblingDirectory("net462");
        }

        private static string GetNetCoreSiblingDirectory()
        {
            return GetSiblingDirectory("netcoreapp2.1");
        }
    }  
}
#endif
