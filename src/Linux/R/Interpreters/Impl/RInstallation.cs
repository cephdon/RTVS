﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Common.Core;
using Microsoft.Common.Core.IO;
using Microsoft.Common.Core.OS;

namespace Microsoft.R.Interpreters { 
    public sealed class RInstallation : IRInstallationService {
        private readonly IFileSystem _fileSystem;

        public RInstallation() :
            this(new UnixFileSystem()) {
        }

        public RInstallation(IFileSystem fileSystem) {
            _fileSystem = fileSystem;
        }

        public IRInterpreterInfo CreateInfo(string name, string path) {
            var packagesInfo = InstalledPackageInfo.GetPackages(_fileSystem);
            string libRsoPath = Path.Combine(path, "lib/libR.so");

            // In linux there is no direct way to get version from binary. So, try and find a package that 
            // has this file in the package files list. 
            InstalledPackageInfo package = null;
            foreach (var pkg in packagesInfo) {
                var files = pkg.GetPackageFiles(_fileSystem);
                if (files.Contains(libRsoPath)) {
                    package = pkg;
                    break;
                }
            }

            if(package != null) {
                return new RInterpreterInfo(name, package, package.Version, package.GetVersion(), _fileSystem);
            }
            return new RInterpreterInfo(name, InstalledPackageInfo.EmptyPackage, string.Empty, null, _fileSystem);
        }

        public IEnumerable<IRInterpreterInfo> GetCompatibleEngines(ISupportedRVersionRange svl = null) {
            var packagesInfo = InstalledPackageInfo.GetPackages(_fileSystem);
            var interpreters = new List<IRInterpreterInfo>();

            interpreters.AddRange(GetInstalledMRO(packagesInfo, svl));
            interpreters.AddRange(GetInstalledCranR(packagesInfo, svl));
            return interpreters;
        }

        private IEnumerable<IRInterpreterInfo> GetInstalledMRO(IEnumerable<InstalledPackageInfo> packagesInfo, ISupportedRVersionRange svl) {
            var list = new List<IRInterpreterInfo>();
            var selectedPackages = packagesInfo.Where(p => p.PackageName.StartsWithIgnoreCase("microsoft-r-open-mro") && svl.IsCompatibleVersion(p.GetVersion()));
            foreach (var package in selectedPackages) {
                var files = package.GetPackageFiles(_fileSystem);
                list.Add(RInterpreterInfo.CreateFromPackage(package, "Microsoft R Open", _fileSystem));
            }
            return list;
        }

        private IEnumerable<IRInterpreterInfo> GetInstalledCranR(IEnumerable<InstalledPackageInfo> packagesInfo, ISupportedRVersionRange svl) {
            var list = new List<IRInterpreterInfo>();
            var selectedPackages = packagesInfo.Where(p => p.PackageName.EqualsIgnoreCase("r-base-core") && svl.IsCompatibleVersion(p.GetVersion()));
            foreach (var package in selectedPackages) {
                list.Add(RInterpreterInfo.CreateFromPackage(package, "CRAN R", _fileSystem));
            }
            return list;
        }
    }
}
