// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if false
using System;

// ExtensionPoint specified at assembly level - we use this technique for the NUnit V2 Fraemwork Driver.
[assembly: TestCentric.Extensibility.ExtensionPoint("/TestCentric/DoesSomething", typeof(TestCentric.Extensibility.IDoSomething),
    Description = "Extension point specified at assembly level")]
#endif
