﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Common.Core.Test.Utility;
using Microsoft.UnitTests.Core.XUnit;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.R.Interactive.Test.Utility {
    [ExcludeFromCodeCoverage]
    internal class ViewTreeDump {
        // change to true in debugger if you want all baseline tree files regenerated
        private static bool _regenerateBaselineFiles = false;

        public static void CompareVisualTrees(DeployFilesFixture fixture, VisualTreeObject actual, string fileName) {
            var testFileName = fileName + ".tree";

            var serializedActual = SerializeVisualTree(actual);
            var baselineFilePath = fixture.GetSourcePath(testFileName);
            if (_regenerateBaselineFiles) {
                TestFiles.UpdateBaseline(baselineFilePath, serializedActual);
            } else {
                TestFiles.CompareToBaseLine(baselineFilePath, serializedActual);
            }
        }


        private static string SerializeVisualTree(VisualTreeObject o) {
            var serializer = new JsonSerializer { Culture = CultureInfo.InvariantCulture };
            using (var sw = new StringWriter(CultureInfo.InvariantCulture)) {
                using (var writer = new JsonTextWriter(sw)) {
                    writer.Culture = CultureInfo.InvariantCulture;
                    writer.Formatting = Formatting.Indented;
                    serializer.Serialize(writer, o);
                    return sw.ToString();
                }
            }
        }
    }
}
