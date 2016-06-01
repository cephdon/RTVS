﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Microsoft.Common.Core.Install {
    public sealed class SupportedRVersionList : ISupportedRVersionList {
        // TODO: this probably needs configuration file
        // or another dynamic source of supported versions.
        public int MinMajorVersion { get; }
        public int MinMinorVersion { get; }
        public int MaxMajorVersion { get; }
        public int MaxMinorVersion { get; }

        public SupportedRVersionList() : this(3, 2, 3, 9) { }

        public SupportedRVersionList(int minVersionMajorPart, int minVersionMinorPart, int maxVersionMajorPart, int maxVersionMinorPart) {
            MinMajorVersion = minVersionMajorPart;
            MinMinorVersion = minVersionMinorPart;
            MaxMajorVersion = maxVersionMajorPart;
            MaxMinorVersion = maxVersionMinorPart;
        }

        public bool IsCompatibleVersion(Version v) {
            var minVersion = new Version(MinMajorVersion, MinMinorVersion);
            var maxVersion = new Version(MaxMajorVersion, MaxMinorVersion);

            var verMajorMinor = new Version(v.Major, v.Minor);
            return verMajorMinor >= minVersion && verMajorMinor <= maxVersion;
        }
    }
}
