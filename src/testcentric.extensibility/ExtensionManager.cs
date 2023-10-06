// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCentric.Metadata;

namespace TestCentric.Extensibility
{
    public class ExtensionManager : IExtensionManager
    {
        static Logger log = InternalTrace.GetLogger(typeof(ExtensionManager));
        const string DEFAULT_TYPE_EXTENSION_PREFIX = "/TestCentric/TypeExtensions/";
        const string EXTENSION_ATTRIBUTE = "TestCentric.Extensibility.ExtensionAttribute";
        const string EXTENSION_PROPERTY_ATTRIBUTE = "TestCentric.Extensibility.ExtensionPropertyAttribute";

        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();
        private readonly Dictionary<string, ExtensionPoint> _pathIndex = new Dictionary<string, ExtensionPoint>();

        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();
        private readonly List<ExtensionAssembly> _extensionAssemblies = new List<ExtensionAssembly>();

        #region Construction and Initialization

        public ExtensionManager(params Assembly[] rootAssemblies)
        {
            Guard.ArgumentNotNull(rootAssemblies, nameof(rootAssemblies));
            Guard.ArgumentValid(rootAssemblies.Length > 0, "Must be a non-empty array", nameof(rootAssemblies));

            RootAssemblies = rootAssemblies;

            // Set default property values - may be changed before Initialization
            DefaultTypeExtensionPrefix = DEFAULT_TYPE_EXTENSION_PREFIX;
            InitialAddinsDirectory = Path.GetDirectoryName(RootAssemblies[0].Location);
        }

        #endregion

        #region IExtensionService Implementation

        #region Properties

        /// <summary>
        /// Array of assemblies whose ExtensionPoints are to be manaaged.
        /// </summary>
        public IList<Assembly> RootAssemblies { get; private set; }

        /// <summary>
        /// Prefix used if the Path is not specified for a TypeExtensionPoint
        /// </summary>
        public string DefaultTypeExtensionPrefix { get; set; }

        /// <summary>
        /// Directory containing the initial .addins files used to locate extensions
        /// </summary>
        public string InitialAddinsDirectory { get; set; }

        /// <summary>
        /// Gets an enumeration of all ExtensionPoints in the engine.
        /// </summary>
        public IEnumerable<IExtensionPoint> ExtensionPoints
        {
            get { return _extensionPoints.ToArray(); }
        }

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        public IEnumerable<IExtensionNode> Extensions {
            get { return _extensions.ToArray(); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize this instance of ExtensionManager by finding
        /// all extension points and extensions.
        /// </summary>
        public void Initialize()
        {
            // TODO: Find Callers expecting NUnitEngineException

            // Find all extension points
            log.Info("Initializing ExtensionPoints...");
            foreach (var assembly in RootAssemblies)
                FindExtensionPoints(assembly, DefaultTypeExtensionPrefix);

            // Find all extensions
            FindExtensions(InitialAddinsDirectory);
        }

        void FindExtensions(string startDir)
        {
            // Find potential extension assemblies
            log.Info("Initializing Extensions...");
            log.Info($"  Start Directory: {startDir}");
            ProcessAddinsFiles(new DirectoryInfo(startDir), false);

            // Check each assembly to see if it contains extensions
            foreach (var candidate in _extensionAssemblies)
                FindExtensionsInAssembly(candidate);
        }

        IExtensionPoint IExtensionManager.GetExtensionPoint(string path)
        {
            return this.GetExtensionPoint(path);
        }

        public IEnumerable<IExtensionNode> GetExtensionNodes(string path)
        {
            var ep = GetExtensionPoint(path);
            if (ep != null)
                foreach (var node in ep.Extensions)
                    yield return node;
        }

        public IEnumerable<IExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false)
        {
            var ep = GetExtensionPoint(typeof(T));
            if (ep != null)
                foreach (var node in ep.Extensions)
                    if (includeDisabled || node.Enabled)
                        yield return node;
        }

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        public void EnableExtension(string typeName, bool enabled)
        {
            foreach (var node in _extensions)
                if (node.TypeName == typeName)
                    node.Enabled = enabled;
        }

        #endregion

        #endregion

        public ExtensionPoint GetExtensionPoint(string path)
        {
            return _pathIndex.ContainsKey(path) ? _pathIndex[path] : null;
        }

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

        public ExtensionNode GetExtensionNode(string path)
        {
            // HACK
            var ep = GetExtensionPoint(path) as ExtensionPoint;

            return ep != null && ep.Extensions.Count > 0 ? ep.Extensions[0] : null;
        }

        public IEnumerable<T> GetExtensions<T>()
        {
            foreach (var node in GetExtensionNodes<T>())
                yield return (T)((ExtensionNode)node).ExtensionObject;
        }

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// </summary>
        internal void FindExtensionPoints(Assembly assembly, string typeExtensionPrefix)
        {
            AssemblyName assemblyName = assembly.GetName();

            log.Info($"Assembly: {assemblyName.Name}");

            foreach (Type type in assembly.GetExportedTypes())
            {
                foreach (TypeExtensionPointAttribute attr in type.GetCustomAttributes(typeof(TypeExtensionPointAttribute), false))
                {
                    // TODO: This ties the extensibility package too closely to NUnit and should be changed
                    string path = attr.Path ?? typeExtensionPrefix + type.Name;

                    if (_pathIndex.ContainsKey(path))
                        throw new Exception($"The Path {attr.Path} is already in use for another extension point.");

                    var ep = new ExtensionPoint(path, type) {
                        Description = attr.Description,
                        AssemblyName = assemblyName
                    };

                    _extensionPoints.Add(ep);
                    _pathIndex.Add(path, ep);

                    log.Info($"  Found Path={ep.Path}, Type={ep.TypeName}");
                }
            }

            foreach (ExtensionPointAttribute attr in assembly.GetCustomAttributes(typeof(ExtensionPointAttribute), false))
            {
                if (_pathIndex.ContainsKey(attr.Path))
                    throw new Exception($"The Path {attr.Path} is already in use for another extension point.");

                var ep = new ExtensionPoint(attr.Path, attr.Type) {
                    Description = attr.Description,
                    AssemblyName = assemblyName
                };

                _extensionPoints.Add(ep);
                _pathIndex.Add(ep.Path, ep);

                log.Info($"  Found Path={ep.Path}, Type={ep.TypeName}");
            }
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
                CustomAttribute extensionAttr = extensionType.GetAttribute(EXTENSION_ATTRIBUTE);

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
            foreach (var attr in type.GetAttributes(EXTENSION_PROPERTY_ATTRIBUTE))
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
        internal static bool CanLoadTargetFramework(Assembly runnerAsm, ExtensionAssembly extensionAsm)
        {
            if (runnerAsm == null)
                return true;

            var extensionFrameworkName = AssemblyDefinition.ReadAssembly(extensionAsm.FilePath).GetFrameworkName();
            var runnerFrameworkName = AssemblyDefinition.ReadAssembly(runnerAsm.Location).GetFrameworkName();
            if (runnerFrameworkName?.StartsWith(".NETStandard") == true)
            {
                throw new Exception($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");
            }
            else if (runnerFrameworkName?.StartsWith(".NETCoreApp") == true)
            {
                if (extensionFrameworkName?.StartsWith(".NETStandard") != true && extensionFrameworkName?.StartsWith(".NETCoreApp") != true)
                {
                    log.Info($".NET Core runners require .NET Core or .NET Standard extension for {extensionAsm.FilePath}");
                    return false;
                }
            }
            else if (extensionFrameworkName?.StartsWith(".NETCoreApp") == true)
            {
                log.Info($".NET Framework runners cannot load .NET Core extension {extensionAsm.FilePath}");
                return false;
            }

            return true;
        }
    }
}
