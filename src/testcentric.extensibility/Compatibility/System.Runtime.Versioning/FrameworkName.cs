// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NET20 || NET35
using System.Diagnostics;
using TestCentric.Extensibility;

namespace System.Runtime.Versioning
{
    /// <summary>
    /// Compatible implementation of FrameworkName, based on the corefx implementation
    /// </summary>
    internal sealed class FrameworkName : IEquatable<FrameworkName>
    {
        private const string FRAMEWORK_NAME_INVALID = "Invalid FrameworkName";
        private const string FRAMEWORK_NAME_VERSION_REQUIRED = "FramweworkName must include Version";
        private const string FRAMEWORK_NAME_VERSION_INVALID = "The specified Version is invalid";
        private const string FRAMEWORK_NAME_COMPONENT_COUNT = "FrameworkName must specify either two or three components";

        public string Identifier { get; }

        public Version Version { get; }

        public string Profile { get; }

        public string FullName { get; }
        //{
        //    get
        //    {
        //        if (_fullName == null)
        //        {

//        }

//        Debug.Assert(_fullName != null);
//        return _fullName;
//    }
//}

        public override bool Equals(object obj)
        {
            return Equals(obj as FrameworkName);
        }

        public bool Equals(FrameworkName? other)
        {
            if (other is null)
                return false;

            return Identifier == other.Identifier &&
                Version == other.Version &&
                Profile == other.Profile;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode() ^ Version.GetHashCode() ^ Profile.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }

        public FrameworkName(string identifier, Version version)
            : this(identifier, version, null)
        {
        }

        public FrameworkName(string identifier, Version version, string? profile = null)
        {
            Guard.ArgumentNotNull(identifier, nameof(identifier));
            Guard.ArgumentNotNull(version, nameof(version));

            identifier = identifier.Trim();
            Guard.ArgumentNotNullOrEmpty(identifier, nameof(identifier));

            Identifier = identifier;
            Version = version;
            Profile = (profile == null) ? string.Empty : profile.Trim();
            FullName = $"{Identifier},Version=v{Version.ToString()}";
            if (!string.IsNullOrEmpty(Profile))
                FullName += $",Profile={Profile}";
        }

        // Parses strings in the following format: "<identifier>, Version=[v|V]<version>, Profile=<profile>"
        //  - The identifier and version is required, profile is optional
        //  - Only three components are allowed.
        //  - The version string must be in the System.Version format; an optional "v" or "V" prefix is allowed
        public FrameworkName(string frameworkName)
        {
            Guard.ArgumentNotNullOrEmpty(frameworkName, nameof(frameworkName));

            string[] components = frameworkName.Split([',']);

            // Identifier and Version are required, Profile is optional.
            if (components.Length < 2 || components.Length > 3)
                throw new ArgumentException(FRAMEWORK_NAME_COMPONENT_COUNT, nameof(frameworkName));

            //
            // 1) Parse the "Identifier", which must come first. Trim any whitespace
            //
            Identifier = components[0].Trim();
            if (string.IsNullOrEmpty(Identifier))
                throw new ArgumentException(FRAMEWORK_NAME_INVALID, nameof(frameworkName));

            Profile = string.Empty;

            //
            // The required "Version" and optional "Profile" component can be in any order
            //
            for (int i = 1; i < components.Length; i++)
            {
                // Get the key/value pair separated by '='
                string component = components[i];
                int separatorIndex = component.IndexOf('=');

                Guard.ArgumentValid(separatorIndex >= 0 && separatorIndex == component.LastIndexOf('='),
                    FRAMEWORK_NAME_INVALID, nameof(frameworkName));

                // Get the key and value, trimming any whitespace
                string key = component.Substring(0, separatorIndex).Trim();
                string value = component.Substring(separatorIndex + 1).Trim();

                //
                // 2) Parse the required "Version" key value
                //
                if (key.Equals("Version", StringComparison.OrdinalIgnoreCase))
                {
                    // Allow the version to include a 'v' or 'V' prefix...
                    if (value.Length > 0 && (value[0] == 'v' || value[0] == 'V'))
                        value = value.Substring(1);

                    try
                    {
                        Version = new Version(value);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException(FRAMEWORK_NAME_VERSION_INVALID, nameof(frameworkName), e);
                    }
                }
                //
                // 3) Parse the optional "Profile" key value
                //
                else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase))
                {
                    if (value.Length > 0)
                    {
                        Profile = value.ToString();
                    }
                }
                else
                {
                    throw new ArgumentException(FRAMEWORK_NAME_INVALID, nameof(frameworkName));
                }
            }

            if (Version is null)
                throw new ArgumentException(FRAMEWORK_NAME_VERSION_REQUIRED, nameof(frameworkName));

            FullName = $"{Identifier},Version=v{Version.ToString()}";
            if (!string.IsNullOrEmpty(Profile))
                FullName += $",Profile={Profile}";
        }

        public static bool operator ==(FrameworkName left, FrameworkName right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(FrameworkName left, FrameworkName right)
        {
            return !(left == right);
        }
    }
}
#endif
