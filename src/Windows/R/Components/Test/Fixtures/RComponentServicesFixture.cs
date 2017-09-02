﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.Test.Fixtures;
using Microsoft.R.Components.Plots;
using Microsoft.R.Components.Settings;
using Microsoft.R.Components.StatusBar;
using Microsoft.R.Components.Test.Fakes.StatusBar;
using Microsoft.R.Components.Test.StubFactories;
using Microsoft.R.Components.Test.Stubs;
using Microsoft.R.Host.Client;
using Microsoft.R.Interpreters;
using Microsoft.UnitTests.Core.XUnit;

namespace Microsoft.R.Components.Test.Fixtures {
    public class RComponentsServicesFixture : ServiceManagerWithMefFixture {
        protected override IEnumerable<string> GetAssemblyNames() => new[] {
            "Microsoft.VisualStudio.CoreUtility.dll",
            "Microsoft.VisualStudio.Text.Data.dll",
            "Microsoft.VisualStudio.Text.Logic.dll",
            "Microsoft.VisualStudio.Text.UI.dll",
            "Microsoft.VisualStudio.Text.UI.Wpf.dll",
            "Microsoft.VisualStudio.InteractiveWindow.dll",
            "Microsoft.VisualStudio.Editor.dll",
            "Microsoft.VisualStudio.Language.Intellisense.dll",
            "Microsoft.VisualStudio.Platform.VSEditor.dll",
            "Microsoft.R.Components.dll",
            "Microsoft.R.Components.Windows.dll",
            "Microsoft.R.Components.Test.dll"
        };

        protected override void SetupServices(IServiceManager serviceManager, ITestInput testInput) {
            base.SetupServices(serviceManager, testInput);
            serviceManager
                .AddWindowsRInterpretersServices()
                .AddWindowsHostClientServices()
                .AddWindowsRComponentsServices()
                .AddService<IStatusBar, TestStatusBar>()
                .AddService<IRPlotExportDialog, TestPlotExportDialog>()
                .AddService(RSettingsStubFactory.CreateForExistingRPath(testInput.FileSytemSafeName));
        }
    }
}
