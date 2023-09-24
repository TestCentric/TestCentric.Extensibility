// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;

namespace TestCentric.Extensibility
{
    public class ExtensionManagerTests
    {
        private static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionManagerTests).Assembly;
        private static readonly string THIS_ASSEMBLY_DIRECTORY = Path.GetDirectoryName(THIS_ASSEMBLY.Location);
        private static readonly ExtensionManagerTestData[] Examples = ExtensionManagerTestData.Examples;

        private ExtensionManager _extensionManager;

        [SetUp]
        public void CreateManager()
        {
            _extensionManager = new ExtensionManager(THIS_ASSEMBLY);
            var args = TestContext.CurrentContext.Test.Arguments;
            string prefix = args.Length == 0
                ? null
                : (args[0] as ExtensionManagerTestData)?.Prefix;
            if (prefix == null)
                _extensionManager.Initialize(THIS_ASSEMBLY_DIRECTORY);
            else
                _extensionManager.Initialize(THIS_ASSEMBLY_DIRECTORY, prefix);
        }

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(_extensionManager.ExtensionPoints.Select(ep => ep.Path), Is.EquivalentTo(Examples.Where(d => d.Prefix == null).Select(d => d.Path))) ;
        }

        [TestCaseSource(nameof(Examples))]
        public void CanGetExtensionPointByPath(ExtensionManagerTestData data)
        {
            var ep = _extensionManager.GetExtensionPoint(data.Path);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(data.Path));
            Assert.That(ep.TypeName, Is.EqualTo(data.ExtensionPointType.FullName));
        }

        [TestCaseSource(nameof(Examples))]
        public void CanGetExtensionPointByType(ExtensionManagerTestData data)
        {
            // Can't look up assembly attribtue specified path
            Assume.That(data.Path != "/TestCentric/DoesSomething");

            var ep = _extensionManager.GetExtensionPoint(data.ExtensionPointType);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(data.Path));
            Assert.That(ep.TypeName, Is.EqualTo(data.ExtensionPointType.FullName));
        }

        [TestCaseSource(nameof(Examples))]
        public void CanListExtensions(ExtensionManagerTestData data)
        {
            Assert.That(_extensionManager.Extensions,
                Has.Some.Property(nameof(ExtensionNode.TypeName)).EqualTo(data.ExtensionType.FullName));
        }

        [TestCaseSource(nameof(Examples))]
        public void ExtensionsAreAddedToExtensionPoint(ExtensionManagerTestData data)
        {
            var nodes = _extensionManager.Extensions.Where(n => n.TypeName == data.ExtensionType.FullName);
            Assert.That(nodes.Count, Is.EqualTo(1)); // Count of 1 is particular to our test setup
        }

        [Test]
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
            var file = new FileInfo(AssemblyHelper.GetAssemblyPath(typeof(ExtensionManagerTests).Assembly));
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
