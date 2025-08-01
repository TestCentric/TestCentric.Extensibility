// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCentric.Metadata;

using NUNIT = NUnit.Engine.Extensibility;

namespace TestCentric.Extensibility
{
    /// <summary>
    /// ExtensionManager provides a low-level implementation of the NUnit extension model.
    /// </summary>
    /// <remarks>
    /// Methods and properties returning extensions or extension points return the actual
    /// classes rather than the corresponding interfaces. It's up to the caller, e.g. the
    /// engine's ExtensionService to decide how much to make available publicly. This
    /// approach gives the most flexibility in using ExtensionManager for various purposes.
    /// </remarks>
    public class ExtensionManager : IExtensionManager
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionManager));

        // Default path prefix for Type Extensions if the caller does not supply
        // it as an argument to the constructor.
        private const string DEFAULT_TYPE_EXTENSION_PATH = "/TestCentric/TypeExtensions/";
        private const string NUNIT_TYPE_EXTENSIONS_PATH = "/NUnit/Engine/TypeExtensions/";

        private const string TESTCENTRIC_EXTENSION_ATTRIBUTE = "TestCentric.Extensibility.ExtensionAttribute";
        private const string TESTCENTRIC_EXTENSION_PROPERTY_ATTRIBUTE = "TestCentric.Extensibility.ExtensionPropertyAttribute";

        // TODO: These are NUnit V3 attributes. We should support as well
        private const string NUNIT_EXTENSION_ATTRIBUTE = "NUnit.Engine.Extensibility.ExtensionAttribute";
        private const string NUNIT_EXTENSION_PROPERTY_ATTRIBUTE = "NUnit.Engine.Extensibility.ExtensionPropertyAttribute";

        // List of all ExtensionPoints discovered
        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();

        // Index to ExtensionPoints based on the Path
        private readonly Dictionary<string, ExtensionPoint> _extensionPointIndex = new Dictionary<string, ExtensionPoint>();

        // List of ExtensionNodes for all extensions discovered.
        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();

        private bool _extensionsAreLoaded;

        // AssemblyTracker is a List of candidate ExtensionAssemblies, with built-in indexing
        // by file path and assembly name, eliminating the need to update indices separately.
        private readonly ExtensionAssemblyTracker _assemblies = new ExtensionAssemblyTracker();

        // List of all extensionDirectories specified on command-line or in environment,
        // used to ignore duplicate calls to FindExtensionAssemblies.
        private readonly List<string> _extensionDirectories = new List<string>();

        // Default values for PackagePrefixes and TypeExtensionPath, which may be set
        // by the caller when ExtensionManager is constructed.
        public string[] PackagePrefixes { get; set; } = ["NUnit.Extension.", "TestCentric.Extension"];
        public string TypeExtensionPath { get; set; } = DEFAULT_TYPE_EXTENSION_PATH;

        #region Construction

        public ExtensionManager()
        {
        }

        #endregion

        #region Extension Points

        /// <summary>
        /// Gets an enumeration of all ExtensionPoints in the engine.
        /// </summary>
        public IEnumerable<IExtensionPoint> ExtensionPoints
        {
            get { return _extensionPoints.ToArray(); }
        }

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// </summary>
        public void FindExtensionPoints(params Assembly[] targetAssemblies)
        {
            foreach (var assembly in targetAssemblies)
            {
                AssemblyName assemblyName = assembly.GetName();

                log.Info($"Assembly: {assemblyName.Name}");

                foreach (Type type in assembly.GetExportedTypes())
                {
                    try
                    {
                        foreach (TypeExtensionPointAttribute attr in type.GetCustomAttributes(typeof(TypeExtensionPointAttribute), false))
                            AddExtensionPoint(attr.Path ?? TypeExtensionPath + type.Name, type, assemblyName, attr.Description);

                        foreach (ExtensionPointAttribute attr in assembly.GetCustomAttributes(typeof(ExtensionPointAttribute), false))
                            AddExtensionPoint(attr.Path, attr.Type, assemblyName, attr.Description);

                        foreach (NUNIT.TypeExtensionPointAttribute attr in type.GetCustomAttributes(typeof(NUNIT.TypeExtensionPointAttribute), false))
                            AddExtensionPoint(attr.Path ?? NUNIT_TYPE_EXTENSIONS_PATH + type.Name, type, assemblyName, attr.Description);

                        foreach (NUNIT.ExtensionPointAttribute attr in assembly.GetCustomAttributes(typeof(NUNIT.ExtensionPointAttribute), false))
                            AddExtensionPoint(attr.Path, attr.Type, assemblyName, attr.Description);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            void AddExtensionPoint(string path, Type type, AssemblyName assemblyName, string? description = null)
            {
                if (_extensionPointIndex.ContainsKey(path))
                    throw new ExtensibilityException($"The Path {path} is already in use for another extension point.");

                var ep = new ExtensionPoint(path, type)
                {
                    Description = description,
                    AssemblyName = assemblyName
                };

                _extensionPoints.Add(ep);
                _extensionPointIndex.Add(ep.Path, ep);

                log.Info($"  Found Path={ep.Path}, Type={ep.TypeName}");
            }
        }

        /// <summary>
        /// Get an IExtensionPoint based on its unique identifying path.
        /// </summary>
        IExtensionPoint? IExtensionManager.GetExtensionPoint(string path)
        {
            return this.GetExtensionPoint(path);
        }

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        public ExtensionPoint? GetExtensionPoint(string path)
        {
            return _extensionPointIndex.ContainsKey(path) ? _extensionPointIndex[path] : null;
        }

        #endregion

        #region Extensions

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        public IEnumerable<IExtensionNode> Extensions
        {
            get
            {
                LoadExtensions();

                return _extensions.ToArray();
            }
        }

        /// <summary>
        /// Find ExtensionAssemblies for a host assembly using
        /// a built-in algorithm that searches in certain known locations.
        /// </summary>
        /// <param name="hostAssembly">An assembly that supports NUnit extensions.</param>
        public void FindExtensionAssemblies(Assembly hostAssembly)
        {
            log.Info($"FindExtensionAssemblies called for host {hostAssembly.FullName}");

            var extensionPatterns = new List<string>();
            foreach (string prefix in PackagePrefixes)
            {
                extensionPatterns.Add($"{prefix}*/**/tools/");
                extensionPatterns.Add($"{prefix}*/**/tools/*/");
            }

            var startDir = new DirectoryInfo(Path.GetDirectoryName(hostAssembly.Location));

            while (startDir is not null)
            {
                foreach (var pattern in extensionPatterns)
                    foreach (var dir in DirectoryFinder.GetDirectories(startDir, pattern))
                        ProcessDirectory(dir, true);

                startDir = startDir.Parent;
            }
        }

        /// <summary>
        /// Find extension assemblies starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        public void FindExtensionAssemblies(string startDir)
        {
            // Ignore a call for a directory we have already used
            if (!_extensionDirectories.Contains(startDir))
            {
                _extensionDirectories.Add(startDir);

                log.Info($"FindExtensionAssemblies examining extension directory {startDir}");

                // Create the list of possible extension assemblies,
                // eliminating duplicates, start in the provided directory.
                // In this top level directory, we only look at .addins files.
                ProcessAddinsFiles(new DirectoryInfo(startDir), false);
            }
        }

        /// <summary>
        /// Get extension objects for all nodes of a given type
        /// </summary>
        public IEnumerable<T> GetExtensions<T>()
        {
            foreach (var node in GetExtensionNodes<T>())
                yield return (T)((ExtensionNode)node).ExtensionObject;
        }

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        public void EnableExtension(string typeName, bool enabled)
        {
            LoadExtensions();

            foreach (var node in _extensions)
                if (node.TypeName == typeName)
                    node.Enabled = enabled;
        }

        /// <summary>
        /// We can only load extensions after all candidate assemblies are identified.
        /// This method may be called by the user after all "Find" calls are complete.
        /// If the user fails to call it and subsequently tries to examine extensions
        /// using other ExtensionManager properties or methods, it will be called
        /// but calls not going through ExtensionManager may fail.
        /// </summary>
        public void LoadExtensions()
        {
            if (!_extensionsAreLoaded)
            {
                _extensionsAreLoaded = true;

                foreach (var candidate in _assemblies)
                    FindExtensionsInAssembly(candidate);
            }
        }

        #endregion

        #region Extension Nodes

        /// <summary>
        /// Get all ExtensionNodes for a path
        /// </summary>
        public IEnumerable<IExtensionNode> GetExtensionNodes(string path)
        {
            LoadExtensions();

            var ep = GetExtensionPoint(path);
            if (ep is not null)
                foreach (var node in ep.Extensions)
                    yield return node;
        }

        /// <summary>
        /// Get the first or only ExtensionNode for a given ExtensionPoint
        /// </summary>
        /// <param name="path">The identifying path for an ExtensionPoint</param>
        public IExtensionNode? GetExtensionNode(string path)
        {
            LoadExtensions();

            var ep = GetExtensionPoint(path);

            return ep is not null && ep.Extensions.Count > 0 ? ep.Extensions[0] : null;
        }

        /// <summary>
        /// Get all extension nodes of a given Type.
        /// </summary>
        /// <param name="includeDisabled">If true, disabled nodes are included</param>
        public IEnumerable<IExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false)
        {
            LoadExtensions();

            var ep = GetExtensionPoint(typeof(T));
            if (ep is not null)
                foreach (var node in ep.Extensions)
                    if (includeDisabled || node.Enabled)
                        yield return node;
        }

        #endregion

        #region Public Class-level Properties and Methods

        // TODO: Is this used?
        public InternalTraceLevel InternalTraceLevel { get; set; }

        // TODO: Is this used?
        public string? WorkDirectory { get; set; }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get an ExtensionPoint based on the required Type for extensions.
        /// </summary>
        internal ExtensionPoint? GetExtensionPoint(Type type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        /// <summary>
        /// Get an ExtensionPoint based on a Cecil TypeReference.
        /// </summary>
        private ExtensionPoint? GetExtensionPoint(TypeReference type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        /// <summary>
        /// Deduce the extension point based on the Type of an extension.
        /// Returns null if no extension point can be found that would
        /// be satisfied by the provided Type.
        /// </summary>
        private ExtensionPoint? DeduceExtensionPointFromType(TypeReference typeRef)
        {
            var ep = GetExtensionPoint(typeRef);
            if (ep is not null)
                return ep;

            TypeDefinition typeDef = typeRef.Resolve();

            foreach (InterfaceImplementation iface in typeDef.Interfaces)
            {
                ep = DeduceExtensionPointFromType(iface.InterfaceType);
                if (ep is not null)
                    return ep;
            }

            TypeReference? baseType = typeDef.BaseType;
            return baseType is not null && baseType.FullName != "System.Object"
                ? DeduceExtensionPointFromType(baseType)
                : null;
        }

        /// <summary>
        /// Scans a directory for candidate addin assemblies. Note that assemblies in
        /// the directory are only scanned if no file of type .addins is found. If such
        /// a file is found, then those assemblies it references are scanned.
        /// </summary>
        private void ProcessDirectory(DirectoryInfo startDir, bool fromWildCard)
        {
            log.Info("Scanning directory {0} for extensions", startDir.FullName);

            if (ProcessAddinsFiles(startDir, fromWildCard) == 0)
                foreach (var file in startDir.GetFiles("*.dll"))
                    ProcessCandidateAssembly(file.FullName, true);
        }

        /// <summary>
        /// Process all .addins files found in a directory
        /// </summary>
        private int ProcessAddinsFiles(DirectoryInfo startDir, bool fromWildCard)
        {
            var addinsFiles = startDir.GetFiles("*.addins");

            if (addinsFiles.Length > 0)
                foreach (var file in addinsFiles)
                    ProcessAddinsFile(startDir, file.FullName, fromWildCard);

            return addinsFiles.Length;
        }

        /// <summary>
        /// Process a .addins type file. The file contains one entry per
        /// line. Each entry may be a directory to scan, an assembly
        /// path or a wildcard pattern used to find assemblies. Blank
        /// lines and comments started by # are ignored.
        /// </summary>
        private void ProcessAddinsFile(DirectoryInfo baseDir, string fileName, bool fromWildCard)
        {
            log.Info($"  Addins File: {Path.GetFileName(fileName)}");

            using (var rdr = new StreamReader(fileName))
            {
                while (!rdr.EndOfStream)
                {
                    var line = rdr.ReadLine();
                    if (line is null)
                        break;

                    line = line.Split(new char[] { '#' })[0].Trim();

                    if (line == string.Empty)
                        continue;

                    if (Path.DirectorySeparatorChar == '\\')
                        line = line.Replace(Path.DirectorySeparatorChar, '/');

                    bool isWild = fromWildCard || line.Contains("*");
                    if (line.EndsWith("/"))
                        foreach (var dir in DirectoryFinder.GetDirectories(baseDir, line))
                            ProcessDirectory(dir, isWild);
                    else
                        foreach (var file in DirectoryFinder.GetFiles(baseDir, line))
                            ProcessCandidateAssembly(file.FullName, isWild);
                }
            }
        }

        private void ProcessCandidateAssembly(string filePath, bool fromWildCard)
        {
            if (!Visited(filePath))
            {
                Visit(filePath);

                try
                {
                    var candidateAssembly = new ExtensionAssembly(filePath, fromWildCard);
                    string assemblyName = candidateAssembly.AssemblyName;

                    // Make sure we can load this assembly
                    if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), candidateAssembly))
                    {
                        log.Info($"{candidateAssembly.FilePath} cannot be loaded on this runtime");
                        return;
                    }

                    _assemblies.AddOrUpdate(candidateAssembly);
                    //// Check to see if this is a duplicate
                    //for (int i = 0; i < _extensionAssemblies.Count; i++)
                    //{
                    //    var assembly = _extensionAssemblies[i];

                    //    if (candidate.IsDuplicateOf(assembly))
                    //    {
                    //        if (candidate.IsBetterVersionOf(assembly))
                    //        {
                    //            _extensionAssemblies[i] = candidate;
                    //            log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, duplicate replacing original");
                    //        }
                    //        else
                    //            log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, duplicate ignored");

                    //        return;
                    //    }
                    //}

                    //log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, saved");
                    //_extensionAssemblies.Add(candidate);
                }
                catch (BadImageFormatException e)
                {
                    if (!fromWildCard)
                        throw new ExtensibilityException($"Specified extension {filePath} could not be read", e);
                }
                catch (Exception)
                {
                    if (!fromWildCard)
                        throw;
                }
            }
            else
                log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, already visited");
        }

        private Dictionary<string, bool> _visited = new Dictionary<string, bool>();

        private bool Visited(string filePath) => _visited.ContainsKey(filePath);

        private void Visit(string filePath) => _visited.Add(filePath, true);

        /// <summary>
        /// Scan a single assembly for extensions marked by ExtensionAttribute.
        /// For each extension, create an ExtensionNode and link it to the
        /// correct ExtensionPoint. Internal for testing.
        /// </summary>
        internal void FindExtensionsInAssembly(ExtensionAssembly extensionAssembly)
        {
            log.Info($"  Assembly: {Path.GetFileName(extensionAssembly.FilePath)}");

            if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), extensionAssembly))
            {
                log.Info($"{extensionAssembly.FilePath} cannot be loaded on this runtime");
                return;
            }

            foreach (var extensionType in extensionAssembly.MainModule.GetTypes())
            {
                CustomAttribute extensionAttr =
                    extensionType.GetAttribute(TESTCENTRIC_EXTENSION_ATTRIBUTE) ??
                    extensionType.GetAttribute(NUNIT_EXTENSION_ATTRIBUTE);

                if (extensionAttr is null)
                    log.Debug($"  Type: {extensionType.Name} - not an extension");
                else
                {
                    log.Info($"  Type: {extensionType.Name} - found ExtensionAttribute");

                    ExtensionNode extensionNode = BuildExtensionNode(extensionAttr, extensionType, extensionAssembly);
                    ExtensionPoint? extensionPoint = BuildExtensionPoint(extensionNode, extensionType, extensionAssembly.FromWildCard);

                    string? versionArg = extensionAttr.GetNamedArgument("EngineVersion") as string;
                    if (versionArg is not null && !CheckRequiredVersion(versionArg, extensionPoint))
                        log.Warning($"  Ignoring {extensionType.Name}. It requires version {versionArg}.");
                    else
                    {
                        AddExtensionPropertiesToNode(extensionType, extensionNode);

                        _extensions.Add(extensionNode);
                        extensionPoint.Install(extensionNode);
                        log.Info($"    Installed at path {extensionNode.Path}");
                    }
                }
            }
        }

        private ExtensionNode BuildExtensionNode(CustomAttribute attr, TypeDefinition type, ExtensionAssembly assembly)
        {
            object enabledArg = attr.GetNamedArgument("Enabled");
            var node = new ExtensionNode(assembly.FilePath, assembly.AssemblyVersion, type.FullName)
            {
                Path = (string)attr.GetNamedArgument("Path"),
                Description = attr.GetNamedArgument("Description") as string,
                Enabled = enabledArg is not null ? (bool)enabledArg : true
            };

            return node;
        }

        private ExtensionPoint BuildExtensionPoint(ExtensionNode node, TypeDefinition type, bool fromWildCard)
        {
            ExtensionPoint? ep;

            if (node.Path is null)
            {
                ep = DeduceExtensionPointFromType(type);
                if (ep is null)
                    throw new ExtensibilityException($"Unable to deduce ExtensionPoint for Type {type.FullName}. Specify Path on ExtensionAttribute to resolve.");

                log.Debug($"    Deduced Path {ep.Path}");
                node.Path = ep.Path;
            }
            else
            {
                ep = GetExtensionPoint(node.Path);
                if (ep is null)
                    throw new ExtensibilityException($"Unable to locate ExtensionPoint for Type {type.FullName}. The Path {node.Path} cannot be found.");
            }

            return ep;
        }

        private bool CheckRequiredVersion(string versionArg, ExtensionPoint ep)
        {
            int dash = versionArg.IndexOf('-');

            Version requiredVersion = dash > 0 ? new Version(versionArg.Substring(0, dash)) : new Version(versionArg);

            return requiredVersion <= ep.AssemblyName.Version;

            // TODO: Need to specify pre-release suffix for the engine in some way
            // and then compare here appropriately. For now, no action so any suffix
            // is actually ignored.

#if false
                    // Partial code as there's nothing to compare suffix to
                    if (requiredVersion == ep.AssemblyName.Version && dash > 0)
                    {
                        string suffix = versionArg.Substring(dash + 1);
                        string label = "";
                        foreach (char c in suffix)
                        {
                            if (!char.IsLetter(c))
                                break;
                            label += c;
                        }
                    }
#endif
        }

        private void AddExtensionPropertiesToNode(TypeDefinition type, ExtensionNode node)
        {
            // TODO: Review this
            // This code allows use of TestCentric and NUnit versions of the extension property
            // attribute for either type of extension. We may want to prevent that in future.
            var propAttrs = new[] { TESTCENTRIC_EXTENSION_PROPERTY_ATTRIBUTE, NUNIT_EXTENSION_PROPERTY_ATTRIBUTE };
            foreach (string attrName in propAttrs)
                foreach (var attr in type.GetAttributes(attrName))
                {
                    string? name = attr.ConstructorArguments[0].Value as string;
                    string? value = attr.ConstructorArguments[1].Value as string;

                    if (name is not null && value is not null)
                    {
                        node.AddProperty(name, value);
                        log.Info("        ExtensionProperty {0} = {1}", name, value);
                    }
                }
        }
        /// <summary>
        /// Checks that the target framework of the current runner can load the extension assembly. For example, .NET Core
        /// cannot load .NET Framework assemblies and vice-versa.
        /// </summary>
        /// <param name="runnerAsm">The executing runner</param>
        /// <param name="extensionAsm">The extension we are attempting to load</param>
        internal bool CanLoadTargetFramework(Assembly runnerAsm, ExtensionAssembly extensionAsm)
        {
            if (runnerAsm is null)
                return true;

            var runnerFrameworkName = GetTargetRuntime(runnerAsm.Location);
            var extensionFrameworkName = GetTargetRuntime(extensionAsm.FilePath);

            switch (runnerFrameworkName.Identifier)
            {
                case ".NETStandard":
                    throw new NUnit.Engine.NUnitEngineException($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");

                case ".NETCoreApp":
                    switch (extensionFrameworkName.Identifier)
                    {
                        case ".NETStandard":
                        case ".NETCoreApp":
                            return true;
                        case ".NETFramework":
                        default:
                            log.Info($".NET Core runners require .NET Core or .NET Standard extension for {extensionAsm.FilePath}");
                            return false;
                    }
                case ".NETFramework":
                default:
                    switch (extensionFrameworkName.Identifier)
                    {
                        case ".NETFramework":
                            return runnerFrameworkName.Version.Major == 4 || extensionFrameworkName.Version.Major < 4;
                        // For .NET Framework calling .NET Standard, we only support if framework is 4.7.2 or higher
                        case ".NETStandard":
                            return extensionFrameworkName.Version >= new Version(4, 7, 2);
                        case ".NETCoreApp":
                        default:
                            log.Info($".NET Framework runners cannot load .NET Core extension {extensionAsm.FilePath}");
                            return false;
                    }
            }
        }

        private System.Runtime.Versioning.FrameworkName GetTargetRuntime(string filePath)
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(filePath);
            var frameworkName = assemblyDef.GetFrameworkName();
            if (string.IsNullOrEmpty(frameworkName))
            {
                var runtimeVersion = assemblyDef.GetRuntimeVersion();
                frameworkName = $".NETFramework,Version=v{runtimeVersion.ToString(3)}";
            }
            return new System.Runtime.Versioning.FrameworkName(frameworkName);
        }

        internal void DumpExtensionPoints(string message)
        {
            Console.WriteLine(message);
            foreach (var extensionPoint in _extensionPoints)
                Console.WriteLine(extensionPoint.Path);
            Console.WriteLine();
        }

        #endregion
    }
}
