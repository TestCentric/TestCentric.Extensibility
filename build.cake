// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.3.3
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "TestCentric Extensibility",
	solutionFile: "TestCentric.Extensibility.sln",
	githubRepository: "TestCentric.Extensibility",
	unitTests: "**/*.Tests.exe");

BuildSettings.Packages.Add(new NuGetPackage(
	id: "TestCentric.Extensibility",
	source: "nuget/TestCentric.Extensibility.nuspec",
	checks: new PackageCheck[] {
		HasFiles("LICENSE.txt", "README.md", "testcentric.png"),
		HasDirectory("lib/net20")
			.WithFiles("testcentric.extensibility.dll", "testcentric.extensibility.api.dll", "nunit.engine.api.dll"),
		HasDirectory("lib/net462")
			.WithFiles("testcentric.extensibility.dll", "testcentric.extensibility.api.dll", "nunit.engine.api.dll"),
		HasDirectory("lib/netstandard2.0")
			.WithFiles("testcentric.extensibility.dll", "testcentric.extensibility.api.dll", "nunit.engine.api.dll"),
		HasDependency("TestCentric.Metadata")
			.WithFiles(
				"lib/net20/testcentric.engine.metadata.dll",
				"lib/net40/testcentric.engine.metadata.dll",
				"lib/netstandard1.6/testcentric.engine.metadata.dll",
				"lib/netstandard2.0/testcentric.engine.metadata.dll"),
		HasDependency("TestCentric.InternalTrace")
			.WithFiles(
				"lib/net20/TestCentric.InternalTrace.dll",
				"lib/net462/TestCentric.InternalTrace.dll",
				"lib/netstandard2.0/TestCentric.InternalTrace.dll") }));

BuildSettings.Packages.Add(new NuGetPackage(
	id: "TestCentric.Extensibility.Api",
	source: "nuget/TestCentric.Extensibility.Api.nuspec",
	checks: new PackageCheck[] {
		HasFiles(
			"LICENSE.txt", "README.md", "testcentric.png",
			"lib/net20/testcentric.extensibility.api.dll",
			"lib/net462/testcentric.extensibility.api.dll",
			"lib/netstandard2.0/testcentric.extensibility.api.dll") }));

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run();
