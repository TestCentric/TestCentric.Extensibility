// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Reflection;
using NUnit.Framework;

namespace TestCentric.Extensibility
{
    public class ExtensionAssemblyTests
    {
        private static readonly Assembly THIS_ASSEMBLY = Assembly.GetExecutingAssembly();
        private static readonly string THIS_ASSEMBLY_PATH = THIS_ASSEMBLY.Location;
        private static readonly string THIS_ASSEMBLY_FULL_NAME = THIS_ASSEMBLY.GetName().FullName;
        private static readonly string THIS_ASSEMBLY_NAME = THIS_ASSEMBLY.GetName().Name;
        private static readonly Version THIS_ASSEMBLY_VERSION = THIS_ASSEMBLY.GetName().Version;

        private ExtensionAssembly _ea;

        [OneTimeSetUp]
        public void CreateExtensionAssemblies()
        {
            _ea = new ExtensionAssembly(THIS_ASSEMBLY_PATH, false);
        }

        [Test]
        public void AssemblyDefinition()
        {
            Assert.That(_ea.Assembly.FullName, Is.EqualTo(THIS_ASSEMBLY_FULL_NAME));
        }

        [Test]
        public void MainModule()
        {
            Assert.That(_ea.MainModule.Assembly.FullName, Is.EqualTo(THIS_ASSEMBLY_FULL_NAME));
        }

        [Test]
        public void AssemblyName()
        {
            Assert.That(_ea.AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
        }

        [Test]
        public void AssemblyVersion()
        {
            Assert.That(_ea.AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }

#if NETFRAMEWORK
        [Test]
        public void TargetFramework()
        {
            var framework = _ea.FrameworkName;
            Assert.That(_ea.FrameworkName.Identifier, Is.EqualTo(".NETFramework"));
#if NET20 || NET35
            Assert.That(_ea.FrameworkName.Version, Is.EqualTo(new Version(2, 0)));
#else
            Assert.That(_ea.FrameworkName.Version, Is.EqualTo(new Version(4, 6, 2)));
#endif
        }
#endif
    }
}
