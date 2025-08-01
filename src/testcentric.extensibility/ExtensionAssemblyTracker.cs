﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System.Collections;
using System.Collections.Generic;

namespace TestCentric.Extensibility
{
    /// <summary>
    /// This is a simple utility class used by the ExtensionManager to keep track of ExtensionAssemblies.
    /// It maps assemblies by their name and keeps track of evaluated assembly paths.
    /// It allows writing tests to show that no duplicate extension assemblies are loaded.
    /// </summary>
    internal class ExtensionAssemblyTracker : IEnumerable<ExtensionAssembly>
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionAssemblyTracker));

#if NET20
        private readonly Dictionary<string, bool> _evaluatedPaths = new Dictionary<string, bool>();
        public bool ContainsPath(string path) => _evaluatedPaths.ContainsKey(path);
#else
        private readonly HashSet<string> _evaluatedPaths = new HashSet<string>();
        public bool ContainsPath(string path) => _evaluatedPaths.Contains(path);
#endif

        private readonly Dictionary<string, ExtensionAssembly> _byName = new Dictionary<string, ExtensionAssembly>();

        public int Count => +_byName.Count;

        public IEnumerator<ExtensionAssembly> GetEnumerator() => _byName.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddOrUpdate(ExtensionAssembly candidateAssembly)
        {
            string assemblyName = candidateAssembly.AssemblyName;
#if NET20
            _evaluatedPaths.Add(candidateAssembly.FilePath, true);
#else
            _evaluatedPaths.Add(candidateAssembly.FilePath);
#endif
            // Do we already have a copy of the same assembly at a different path?
            if (_byName.TryGetValue(assemblyName, out var existing))
            {
                if (candidateAssembly.IsBetterVersionOf(existing))
                {
                    _byName[assemblyName] = candidateAssembly;
                    log.Debug($"Newer version added for assembly: {assemblyName}");
                }
            }
            else
            {
                _byName[assemblyName] = candidateAssembly;
                log.Debug($"Assembly added: {assemblyName}");
            }
        }
    }
}
