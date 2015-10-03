﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Languages.Core.Test.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Markdown.Editor.Test.Utility
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class MarkdownTestFilesSetup
    {
        static object _deploymentLock = new object();
        static bool _deployed = false;

        [AssemblyInitialize]
        public static void DeployFiles(TestContext context)
        {
            lock (_deploymentLock)
            {
                if (!_deployed)
                {
                    _deployed = true;

                    string srcFilesFolder;
                    string testFilesDir;

                    TestSetup.GetTestFolders(@"Markdown\Editor\Test\Files", CommonTestData.TestFilesRelativePath, context, out srcFilesFolder, out testFilesDir);
                    TestSetup.CopyDirectory(srcFilesFolder, testFilesDir);
                }
            }
        }
    }
}