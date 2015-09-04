using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.IO.FileSystem
{
	internal class DirectoryInfoProxy : IDirectoryInfo
	{
		private readonly DirectoryInfo _directoryInfo;

		public DirectoryInfoProxy(string directoryPath)
		{
			_directoryInfo = new DirectoryInfo(directoryPath);
		}

		public DirectoryInfoProxy(DirectoryInfo directoryInfo)
		{
			_directoryInfo = directoryInfo;
		}

		public bool Exists => _directoryInfo.Exists;
		public string FullName => _directoryInfo.FullName;
		public FileAttributes Attributes => _directoryInfo.Attributes;

		public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos()
		{
			return _directoryInfo
				.EnumerateFileSystemInfos()
				.Select(CreateFileSystemInfoProxy);
		}

		private static IFileSystemInfo CreateFileSystemInfoProxy(FileSystemInfo fileSystemInfo)
		{
			var directoryInfo = fileSystemInfo as DirectoryInfo;
			return directoryInfo != null ? (IFileSystemInfo)new DirectoryInfoProxy(directoryInfo) : new FileInfoProxy((FileInfo)fileSystemInfo);
		}
	}
}