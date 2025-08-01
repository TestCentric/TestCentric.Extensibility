﻿// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

using System;

namespace TestCentric.Extensibility
{
    /// <summary>
    /// The ExtensionPropertyAttribute is used to specify named properties for an extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ExtensionPropertyAttribute : Attribute
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The property value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Construct an ExtensionPropertyAttribute
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <param name="value">The property value</param>
        public ExtensionPropertyAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
