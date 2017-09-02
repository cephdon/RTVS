﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using System.Collections.Generic;
using Microsoft.Common.Core;
using Microsoft.Common.Core.IO;
using Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.Utilities;
using static System.FormattableString;

namespace Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.IO {
    public sealed partial class MsBuildFileSystemWatcher {
        private class DirectoryCreated : IFileSystemChange {
            private readonly MsBuildFileSystemWatcherEntries _entries;
            private readonly string _rootDirectory;
            private readonly IFileSystem _fileSystem;
            private readonly IMsBuildFileSystemFilter _fileSystemFilter;
            private readonly string _directoryFullPath;

            public DirectoryCreated(MsBuildFileSystemWatcherEntries entries, string rootDirectory, IFileSystem fileSystem, IMsBuildFileSystemFilter fileSystemFilter, string directoryFullPath) {
                _entries = entries;
                _rootDirectory = rootDirectory;
                _fileSystem = fileSystem;
                _fileSystemFilter = fileSystemFilter;
                _directoryFullPath = directoryFullPath;
            }

            public void Apply() {
                if (!_directoryFullPath.StartsWithIgnoreCase(_rootDirectory)) {
                    return;
                }

                Queue<string> directories = new Queue<string>();
                directories.Enqueue(_directoryFullPath);

                while (directories.Count > 0) {
                    var directoryPath = directories.Dequeue();
                    var directory = _fileSystem.GetDirectoryInfo(directoryPath);
                    var relativeDirectoryPath = PathHelper.MakeRelative(_rootDirectory, directoryPath);

                    if (!directory.Exists) {
                        continue;
                    }

                    // We don't want to add root directory
                    if (!string.IsNullOrEmpty(relativeDirectoryPath)) {
                        relativeDirectoryPath = PathHelper.EnsureTrailingSlash(relativeDirectoryPath);

                        // We don't add symlinks
                        if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint)) {
                            continue;
                        }

                        if (!_fileSystemFilter.IsDirectoryAllowed(relativeDirectoryPath, directory.Attributes)) {
                            continue;
                        }

                        _entries.AddDirectory(relativeDirectoryPath, _fileSystem.ToShortRelativePath(directoryPath, _rootDirectory));
                    }

                    foreach (var entry in directory.EnumerateFileSystemInfos()) {
                        if (entry is IDirectoryInfo) {
                            directories.Enqueue(entry.FullName);
                        } else {
                            var relativeFilePath = PathHelper.MakeRelative(_rootDirectory, entry.FullName);

                            if (_fileSystemFilter.IsFileAllowed(relativeFilePath, entry.Attributes)) {
                                _entries.AddFile(relativeFilePath, _fileSystem.ToShortRelativePath(entry.FullName, _rootDirectory));
                            }
                        }
                    }
                }
            }

            public override string ToString() {
                return Invariant($"Directory created: {_directoryFullPath}");
            }
        }
    }
}