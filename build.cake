#tool NuGet.CommandLine&version=6.0.0

// Load the recipe
#load nuget:?package=TestCentric.Cake.Recipe&version=1.1.0-dev00048
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../TestCentric.Cake.Recipe/recipe/*.cake

var NUGET_ID = "TestCentric.Extensibility";

string Configuration = Argument("configuration", Argument("c", "Release"));

string PackageVersion;
string PackageName;
bool IsProductionRelease;
bool IsDevelopmentRelease;

//////////////////////////////////////////////////////////////////////
// INITIALIZE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

BuildSettings.Initialize(
	context: Context,
	title: "TestCentric Extensibility",
	solutionFile: "testcentric-extensibility.sln",
	githubRepository: "testcentric-extensibility");

BuildSettings.Packages.Add(new NuGetPackage(
	id: "TestCentric.Extensibility",
	source: "nuget/TestCentric.Extensibility.nuspec",
	checks: new PackageCheck[] {
		HasFiles(
			"LICENSE.txt", "README.md", "testcentric.png",
			"lib/net20/testcentric.extensibility.dll",
			"lib/net462/testcentric.extensibility.dll",
			"lib/netstandard2.0/testcentric.extensibility.dll"),
		HasDependency("TestCentric.Metadata")
			.WithFiles(
				"lib/net20/testcentric.engine.metadata.dll",
				"lib/net40/testcentric.engine.metadata.dll",
				"lib/netstandard1.6/testcentric.engine.metadata.dll",
				"lib/netstandard2.0/testcentric.engine.metadata.dll") }));

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
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("AppVeyor")
	.IsDependentOn("BuildTestAndPackage")
	.IsDependentOn("PublishToMyGet")
	.IsDependentOn("CreateProductionRelease");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Argument("target", Argument("t", "Default")));
