// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TestCentric.Metadata;

using NUNIT = NUnit.Engine.Extensibility;

namespace TestCentric.Extensibility
{
    public class ExtensionManager : IExtensionManager
    {
        static Logger log = InternalTrace.GetLogger(typeof(ExtensionManager));
        const string DEFAULT_TYPE_EXTENSIONS_PATH = "/TestCentric/TypeExtensions/";
        private const string NUNIT_TYPE_EXTENSIONS_PATH = "/NUnit/Engine/TypeExtensions/";
        const string DEPRECATED = "It will be removed in a future release";

        const string TESTCENTRIC_EXTENSION_ATTRIBUTE = "TestCentric.Extensibility.ExtensionAttribute";
        const string TESTCENTRIC_EXTENSION_PROPERTY_ATTRIBUTE = "TestCentric.Extensibility.ExtensionPropertyAttribute";

        const string NUNIT_EXTENSION_ATTRIBUTE = "NUnit.Engine.Extensibility.ExtensionAttribute";
        const string NUNIT_EXTENSION_PROPERTY_ATTRIBUTE = "NUnit.Engine.Extensibility.ExtensionPropertyAttribute";

        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();
        private readonly Dictionary<string, ExtensionPoint> _pathIndex = new Dictionary<string, ExtensionPoint>();

        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();
        private readonly List<ExtensionAssembly> _extensionAssemblies = new List<ExtensionAssembly>();

        private string DefaultTypeExtensionsPath { get; set; }

        #region Construction

        public ExtensionManager() 
        {
            DefaultTypeExtensionsPath = DEFAULT_TYPE_EXTENSIONS_PATH;
        }

        public ExtensionManager(string defaultTypeExtensionsPath)
        {
            Guard.ArgumentNotNull(defaultTypeExtensionsPath, nameof(defaultTypeExtensionsPath));
            DefaultTypeExtensionsPath = defaultTypeExtensionsPath;
        }

        #endregion

        #region IExtensionManager Implementation

        #region Extension Points

        /// <inheritdoc/>
        public IEnumerable<IExtensionPoint> ExtensionPoints
        {
            get { return _extensionPoints.ToArray(); }
        }

        /// <inheritdoc/>
        public IExtensionManager FindExtensionPoints(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                AssemblyName assemblyName = assembly.GetName();

                log.Info($"Assembly: {assemblyName.Name}");

                foreach (Type type in assembly.GetExportedTypes())
                {
                    try
                    {
                        foreach (TypeExtensionPointAttribute attr in type.GetCustomAttributes(typeof(TypeExtensionPointAttribute), false))
                            AddExtensionPoint(attr.Path ?? DefaultTypeExtensionsPath + type.Name, type, assemblyName, attr.Description);

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

            return this;

            void AddExtensionPoint(string path, Type type, AssemblyName assemblyName, string description = null)
            {
                if (_pathIndex.ContainsKey(path))
                    throw new Exception($"The Path {path} is already in use for another extension point.");

                var ep = new ExtensionPoint(path, type)
                {
                    Description = description,
                    AssemblyName = assemblyName
                };

                _extensionPoints.Add(ep);
                _pathIndex.Add(ep.Path, ep);

                log.Info($"  Found Path={ep.Path}, Type={ep.TypeName}");
            }
        }

        private void FindTypeExtensions()
        {

        }

        /// <inheritdoc/>
        IExtensionPoint IExtensionManager.GetExtensionPoint(string path)
        {
            return this.GetExtensionPoint(path);
        }

        #endregion

        #region Extensions

        /// <inheritdoc/>
        public IEnumerable<IExtensionNode> Extensions
        {
            get { return _extensions.ToArray(); }
        }

        /// <inheritdoc/>
        public IExtensionManager FindExtensions(string startDir)
        {
            // Find potential extension assemblies
            log.Info("Initializing Extensions...");
            log.Info($"  Start Directory: {startDir}");
            ProcessAddinsFiles(new DirectoryInfo(startDir), false);

            // Check each assembly to see if it contains extensions
            foreach (var candidate in _extensionAssemblies)
                FindExtensionsInAssembly(candidate);

            return this;
        }

        /// <inheritdoc/>
        public IEnumerable<T> GetExtensions<T>()
        {
            foreach (var node in GetExtensionNodes<T>())
                yield return (T)((ExtensionNode)node).ExtensionObject;
        }

        /// <inheritdoc/>
        public void EnableExtension(string typeName, bool enabled)
        {
            foreach (var node in _extensions)
                if (node.TypeName == typeName)
                    node.Enabled = enabled;
        }

        #endregion

        #region Extension Nodes

        /// <inheritdoc/>
        public IExtensionNode GetExtensionNode(string path)
        {
            var ep = GetExtensionPoint(path);

            return ep != null && ep.Extensions.Count > 0 ? ep.Extensions[0] : null;
        }

        /// <inheritdoc/>
        public IEnumerable<IExtensionNode> GetExtensionNodes(string path)
        {
            var ep = GetExtensionPoint(path);
            if (ep != null)
                foreach (var node in ep.Extensions)
                    yield return node;
        }

        /// <inheritdoc/>
        public IEnumerable<IExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false)
        {
            var ep = GetExtensionPoint(typeof(T));
            if (ep != null)
                foreach (var node in ep.Extensions)
                    if (includeDisabled || node.Enabled)
                        yield return node;
        }

        #endregion

        #endregion

        #region Public Class-level Properties and Methods

        // TODO: Is this used?
        public InternalTraceLevel InternalTraceLevel { get; set; }

        // TODO: Is this used?
        public string WorkDirectory { get; set; }


        public ExtensionPoint GetExtensionPoint(string path)
        {
            return _pathIndex.ContainsKey(path) ? _pathIndex[path] : null;
        }

        #endregion

        /// <summary>
        /// Get an ExtensionPoint based on the required Type for extensions.
        /// </summary>
        internal ExtensionPoint GetExtensionPoint(Type type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        /// <summary>
        /// Get an ExtensionPoint based on a Cecil TypeReference.
        /// </summary>
        private ExtensionPoint GetExtensionPoint(TypeReference type)
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
        private ExtensionPoint DeduceExtensionPointFromType(TypeReference typeRef)
        {
            var ep = GetExtensionPoint(typeRef);
            if (ep != null)
                return ep;

            TypeDefinition typeDef = typeRef.Resolve();


            foreach (InterfaceImplementation iface in typeDef.Interfaces)
            {
                ep = DeduceExtensionPointFromType(iface.InterfaceType);
                if (ep != null)
                    return ep;
            }

            TypeReference baseType = typeDef.BaseType;
            return baseType != null && baseType.FullName != "System.Object"
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
                    if (line == null)
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
                    var candidate = new ExtensionAssembly(filePath, fromWildCard);

                    // Make sure we can load this assembly
                    if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), candidate))
                    {
                        log.Info($"{candidate.FilePath} cannot be loaded on this runtime");
                        return;
                    }

                    // Check to see if this is a duplicate
                    for (int i = 0; i < _extensionAssemblies.Count; i++)
                    {
                        var assembly = _extensionAssemblies[i];

                        if (candidate.IsDuplicateOf(assembly))
                        {
                            if (candidate.IsBetterVersionOf(assembly))
                            {
                                _extensionAssemblies[i] = candidate;
                                log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, duplicate replacing original");
                            }
                            else
                                log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, duplicate ignored");

                            return;
                        }
                    }

                    log.Info($"  Assembly: {Path.GetFileName(filePath)} ,fromWildCard = {fromWildCard}, saved");
                    _extensionAssemblies.Add(candidate);
                }
                catch (BadImageFormatException e)
                {
                    if (!fromWildCard)
                        throw new Exception($"Specified extension {filePath} could not be read", e);
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

        private Dictionary<string, object> _visited = new Dictionary<string, object>();

        private bool Visited(string filePath)
        {
            return _visited.ContainsKey(filePath);
        }

        private void Visit(string filePath)
        {
            _visited.Add(filePath, null);
        }

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

                if (extensionAttr == null)
                    log.Debug($"  Type: {extensionType.Name} - not an extension");
                else
                {
                    log.Info($"  Type: {extensionType.Name} - found ExtensionAttribute");

                    ExtensionNode extensionNode = BuildExtensionNode(extensionAttr, extensionType, extensionAssembly);
                    ExtensionPoint extensionPoint = BuildExtensionPoint(extensionNode, extensionType, extensionAssembly.FromWildCard);

                    string versionArg = extensionAttr.GetNamedArgument("EngineVersion") as string;
                    if (versionArg != null && !CheckRequiredVersion(versionArg, extensionPoint))
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
            var node = new ExtensionNode(assembly.FilePath, assembly.AssemblyVersion, type.FullName) {
                Path = attr.GetNamedArgument("Path") as string,
                Description = attr.GetNamedArgument("Description") as string,
                Enabled = enabledArg != null ? (bool)enabledArg : true
            };

            return node;
        }

        private ExtensionPoint BuildExtensionPoint(ExtensionNode node, TypeDefinition type, bool fromWildCard)
        {
            ExtensionPoint ep;

            if (node.Path == null)
            {
                ep = DeduceExtensionPointFromType(type);
                if (ep == null)
                    throw new Exception($"Unable to deduce ExtensionPoint for Type {type.FullName}. Specify Path on ExtensionAttribute to resolve.");

                log.Debug($"    Deduced Path {ep.Path}");
                node.Path = ep.Path;
            }
            else
            {
                ep = GetExtensionPoint(node.Path);
                if (ep == null && !fromWildCard)
                    throw new Exception($"Unable to locate ExtensionPoint for Type {type.FullName}. The Path {node.Path} cannot be found.");
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
            foreach (string attrName in propAttrs )
                foreach (var attr in type.GetAttributes(attrName))
                {
                    string name = attr.ConstructorArguments[0].Value as string;
                    string value = attr.ConstructorArguments[1].Value as string;

                    if (name != null && value != null)
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
            if (runnerAsm == null)
                return true;

            var runnerFrameworkName = GetTargetRuntime(runnerAsm.Location);
            var extensionFrameworkName = GetTargetRuntime(extensionAsm.FilePath);

            switch (runnerFrameworkName.Identifier)
            {
                case ".NETStandard":
                    throw new Exception($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");

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
    }
}
