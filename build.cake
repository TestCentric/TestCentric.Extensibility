#tool NuGet.CommandLine&version=6.0.0

// Load the recipe
//#load nuget:?package=TestCentric.Cake.Recipe&version=1.0.1-dev00025
// Comment out above line and uncomment below for local tests of recipe changes
#load ../TestCentric.Cake.Recipe/recipe/*.cake

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
	source: "src/testcentric.extensibility/testcentric.extensibility.csproj",
	checks: new PackageCheck[] {
		HasFiles(
			"LICENSE.txt", "README.md", "testcentric.png",
			"lib/net20/testcentric.extensibility.dll",
			"lib/net462/testcentric.extensibility.dll",
			"lib/netstandard2.0/testcentric.extensibility.dll"),
		// Minimal checks for dependent packages
		HasDependency("NUnit.Engine.Api")
			.WithFiles(
				"lib/net20/nunit.engine.api.dll",
				"lib/netstandard2.0/nunit.engine.api.dll"),
		HasDependency("TestCentric.Metadata")
			.WithFiles(
				"lib/net20/testcentric.engine.metadata.dll",
				"lib/net40/testcentric.engine.metadata.dll",
				"lib/netstandard1.6/testcentric.engine.metadata.dll",
				"lib/netstandard2.0/testcentric.engine.metadata.dll") }));

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
