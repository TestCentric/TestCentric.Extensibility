// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.IO;
using NUnit.Framework;

namespace TestCentric.Extensibility
{
    public class DirectoryFinderTests
    {
        private DirectoryInfo _baseDir;

        [SetUp]
        public void InitializeBaseDir()
        {
            _baseDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        }

        // TODO: These tests are fragile because they rely on the directory structure
        // of the project itself - we should find a better way to test.

        //[TestCase("addins", 1)]
        //[TestCase("net-*", 4)]
        //[TestCase("*/v2-tests", 1)]
        //[TestCase("add*/v?-*", 1)]
        //[TestCase("**/v2-tests", 1)]
        //[TestCase("addins/**", 2)]
        //[TestCase("addins/../net-*", 4)]
        //[TestCase("addins/v2-tests/", 1)]
        //[TestCase("addins//v2-tests/", 1)]
        //[TestCase("addins/./v2-tests/", 1)]
        public void GetDirectories(string pattern, int count)
        {
            var dirList = DirectoryFinder.GetDirectories(_baseDir, pattern);
            Assert.That(dirList.Count, Is.EqualTo(count));
        }

        //[TestCase("net-4.0/nunit.framework.dll", 1)]
        //[TestCase("net-*/nunit.framework.dll", 4)]
        //[TestCase("net-*/*.framework.dll", 4)]
        //[TestCase("*/v2-tests/*.dll", 2)]
        //[TestCase("add*/v?-*/*.dll", 2)]
        //[TestCase("**/v2-tests/*.dll", 2)]
        //[TestCase("addins/**/*.dll", 10)]
        //[TestCase("addins/../net-*/nunit.framework.dll", 4)]
        public void GetFiles(string pattern, int count)
        {
            var files = DirectoryFinder.GetFiles(_baseDir, pattern);
            Assert.That(files.Count, Is.EqualTo(count));
        }
    }
}
