// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using TestCentric.Extensibility;
using TestCentric.Engine.Services;

using System.Diagnostics;
using System;
using System.Reflection;
using System.Xml;
using System.IO;

namespace TestCentric.Engine.Extensibility
{
    // Extensions

    [Extension(Enabled = false)]
    public class FakeTestEventListener : ITestEventListener
    {
        public void OnTestEvent(string text)
        {
            throw new System.NotImplementedException();
        }
    }

    //[Extension]
    public class FakeService : IService
    {
        public IServiceLocator ServiceContext { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public ServiceStatus Status => throw new System.NotImplementedException();

        public void StartService()
        {
            throw new System.NotImplementedException();
        }

        public void StopService()
        {
            throw new System.NotImplementedException();
        }
    }

    [Extension]
    public class FakeAgentLauncher : TestCentric.Engine.Extensibility.IAgentLauncher
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

    //[Extension]
    public class FakeDriverFactory : IDriverFactory
    {
#if NETFRAMEWORK
        public IFrameworkDriver GetDriver(AppDomain domain, AssemblyName reference)
#else
        public IFrameworkDriver GetDriver(AssemblyName reference)
#endif
        {
            throw new NotImplementedException();
        }

        public bool IsSupportedTestFramework(AssemblyName reference)
        {
            throw new NotImplementedException();
        }
    }

    //[Extension]
    public class FakeResultWriter : IResultWriter
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
}

namespace NUnit.Engine.Extensibility
{
    //[Extension(Path = "/NUnit/Engine/TypeExtensions/IProjectLoader/")]
    [Extension]
    public class FakeProjectLoader : IProjectLoader
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
}
