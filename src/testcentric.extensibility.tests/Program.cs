// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Diagnostics;
using System.Reflection;
using NUnitLite;

namespace TestCentric.Extensibility
{
    internal class Program
    {
        public static int Main(string[] args)
        {
#if NETFRAMEWORK
            return new AutoRun().Execute(args);
#else
            return new TextRunner(typeof(Program).GetTypeInfo().Assembly).Execute(args);
#endif
        }
    }
}
