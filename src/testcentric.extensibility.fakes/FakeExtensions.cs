// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Diagnostics;
using System;
using System.Reflection;
using System.Xml;
using System.IO;

namespace TestCentric.Engine.Extensibility
{
    using TestCentric.Extensibility;

    [Extension]
    public class FakeAgentLauncher : IAgentLauncher
    {
        public TestAgentInfo AgentInfo => throw new NotImplementedException();

        public bool CanCreateProcess(TestPackage package)
        {
            throw new NotImplementedException();
        }

        public Process CreateProcess(Guid agentId, string agencyUrl, TestPackage package)
        {
            throw new NotImplementedException();
        }
    }

    [Extension(Enabled = false)]
    public class FakeAgentLauncher_ThrowsInConstructor : IAgentLauncher
    {
        public FakeAgentLauncher_ThrowsInConstructor()
        {
            throw new NotImplementedException();
        }

        public TestAgentInfo AgentInfo => throw new NotImplementedException();

        public bool CanCreateProcess(TestPackage package)
        {
            throw new NotImplementedException();
        }

        public Process CreateProcess(Guid agentId, string agencyUrl, TestPackage package)
        {
            throw new NotImplementedException();
        }
    }

    [Extension(Enabled = false, Path = "/TestCentric/Engine/TestEventListeners")]
    public class FakeTestEventListener : NUnit.Engine.ITestEventListener
    {
        public void OnTestEvent(string text)
        {
            throw new System.NotImplementedException();
        }
    }

    [Extension(Path = "/TestCentric/Engine/ProjectLoaders")]
    public class FakeProjectLoader : NUnit.Engine.Extensibility.IProjectLoader
    {
        public bool CanLoadFrom(string path)
        {
            throw new System.NotImplementedException();
        }

        public NUnit.Engine.Extensibility.IProject LoadFrom(string path)
        {
            throw new System.NotImplementedException();
        }
    }

//    [Extension]
//    public class FakeDriverFactory : IDriverFactory
//    {
//#if NETFRAMEWORK
//        public IFrameworkDriver GetDriver(AppDomain domain, AssemblyName reference)
//#else
//        public IFrameworkDriver GetDriver(AssemblyName reference)
//#endif
//        {
//            throw new NotImplementedException();
//        }

//        public bool IsSupportedTestFramework(AssemblyName reference)
//        {
//            throw new NotImplementedException();
//        }
//    }

    [Extension(Path="/TestCentric/Engine/ResultWriters")]
    public class FakeResultWriter : NUnit.Engine.Extensibility.IResultWriter
    {
        public void CheckWritability(string outputPath)
        {
            throw new NotImplementedException();
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            throw new NotImplementedException();
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    [Extension(Enabled = false, Path = "/TestCentric/Engine/TestEventListeners")]
    public class FakeNUnitExtension_ThrowsInConstructor : NUnit.Engine.ITestEventListener
    {
        public FakeNUnitExtension_ThrowsInConstructor()
        {
            throw new NotImplementedException();
        }

        public void OnTestEvent(string text)
        {
            throw new System.NotImplementedException();
        }
    }
}
