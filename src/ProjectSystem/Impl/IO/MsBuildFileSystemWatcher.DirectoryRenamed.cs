﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core;
using Microsoft.Common.Core.IO;
using Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.Utilities;
using static System.FormattableString;

namespace Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.IO {
    public sealed partial class MsBuildFileSystemWatcher {
        private class DirectoryRenamed : IFileSystemChange {
            private readonly MsBuildFileSystemWatcherEntries _entries;
            private readonly string _rootDirectory;
            private readonly IFileSystem _fileSystem;
            private readonly IMsBuildFileSystemFilter _fileSystemFilter;
            private readonly string _oldFullPath;
            private readonly string _fullPath;

            public DirectoryRenamed(MsBuildFileSystemWatcherEntries entries, string rootDirectory, IFileSystem fileSystem, IMsBuildFileSystemFilter fileSystemFilter, string oldFullPath, string fullPath) {
                _entries = entries;
                _rootDirectory = rootDirectory;
                _fileSystem = fileSystem;
                _fileSystemFilter = fileSystemFilter;
                _oldFullPath = oldFullPath;
                _fullPath = fullPath;
            }

            public void Apply() {
                if (!_fullPath.StartsWithIgnoreCase(_rootDirectory)) {
                    DeleteInsteadOfRename();
                    return;
                }

                var newDirectoryInfo = _fileSystem.GetDirectoryInfo(_fullPath);
                var newRelativePath = PathHelper.EnsureTrailingSlash(PathHelper.MakeRelative(_rootDirectory, _fullPath));
                if (!newDirectoryInfo.Exists || !_fileSystemFilter.IsDirectoryAllowed(newRelativePath, newDirectoryInfo.Attributes)) {
                    DeleteInsteadOfRename();
                    return;
                }

                var oldRelativePath = PathHelper.MakeRelative(_rootDirectory, _oldFullPath);
                if (_entries.ContainsDirectoryEntry(oldRelativePath)) {
                    _entries.RenameDirectory(oldRelativePath, newRelativePath, _fileSystem.ToShortRelativePath(_fullPath, _rootDirectory));
                } else {
                    (new DirectoryCreated(_entries, _rootDirectory, _fileSystem, _fileSystemFilter, _fullPath)).Apply();
                }
            }

            private void DeleteInsteadOfRename() {
                if (!_oldFullPath.StartsWithIgnoreCase(_rootDirectory)) {
                    return;
                }
                _entries.DeleteDirectory(PathHelper.MakeRelative(_rootDirectory, _oldFullPath));
            }

            public override string ToString() {
                return Invariant($"Directory renamed: {_oldFullPath} -> {_fullPath}");
            }
        }
    }
}