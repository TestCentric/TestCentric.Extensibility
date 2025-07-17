# TestCentric Extensibility

The **TestCentric.Extensibility** library is used to manage extensions supported
by TestCentric. Although it was designed specifically for our use,
it can be used in other applications as well.

Currently, the library is built for .NET Framework 2.0 and 4.6.2 and .NET
Standard 2.0. It is available as package **TestCentric.Extensibility**
on nuget.org. The package adds a reference to the library to any project in
which it is installed.

## History

The design of **TestCentric.Extensibility** originated in **NUnit** where it was
a part of the engine's ExtensionService. I ported that service to the TestCentric
engine, where it was then split, with most of the implementation moving into 
a separate ExtensibilityManager, used by the engine service itself. The code for
ExtensibilityManager, along with other classes that form the core of the engine's
extensibility features, became a separate assembly and package. Some of the changes
made were eventually ported back to to the NUnit engine itself.

The **TestCentric.Extensibility** package is now fully independent of it's use by
the TestCentric Engine. At this time, it is also used by TestCentric's pluggable 
agents and may also be used in the future by other components, such as the TestCentric GUI.

## Usage

A client wanting to use the package normally refers to it using a **PackageReference**
in the project file. After creating an instance of ExtensionManager, the client is
responsible for initializing it to locate all available ExtensionPoints and the
Extensions that work with them.

Typical initialization code may be as follows...

```
var extensionManager = new ExtensionManager("/APPLICATION/DEFAULT/PATH")
	.FindExtensionPoints(ASSEMBLY1, ASSEMBLY2, ASSEMBLY3)
	.FindExtensions("/DIRECTORY/SEARCH/PATH");
```

See the definition of the `IExtensionManager` interface for further details.

## Versioning

**TestCentric.Extensibility** follows semantic versioning. The current release
is version 3.0.2.

## Dependencies

The package is dependent on

* TestCentric.MetaData
* TestCentric.InternalTrace
* NUnit.Engine.Api

## Licensing

**TestCentric.Extensibility** is Open Source software, released under the MIT / X11 
license. See LICENSE.txt in the root of the distribution for a copy of the license.
