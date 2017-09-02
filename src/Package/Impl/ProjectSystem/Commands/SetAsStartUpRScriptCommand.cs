﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Common.Core.Shell;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring;
using Microsoft.VisualStudio.R.Package.Commands;

namespace Microsoft.VisualStudio.R.Package.ProjectSystem.Commands {
    [ExportCommandGroup("AD87578C-B324-44DC-A12A-B01A6ED5C6E3")]
    [AppliesTo(ProjectConstants.RtvsProjectCapability)]
    internal sealed class SetAsStartUpRScriptCommand : IAsyncCommandGroupHandler {
        private readonly ConfiguredProject _configuredProject;
        private readonly IRInteractiveWorkflowProvider _interactiveWorkflowProvider;
        private readonly ICoreShell _coreShell;

        [ImportingConstructor]
        public SetAsStartUpRScriptCommand(ConfiguredProject configuredProject, IRInteractiveWorkflowProvider interactiveWorkflowProvider, ICoreShell coreShell) {
            _configuredProject = configuredProject;
            _interactiveWorkflowProvider = interactiveWorkflowProvider;
            _coreShell = coreShell;
        }

        public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string commandText, CommandStatus progressiveStatus) {
            if (commandId == RPackageCommandId.icmdSetAsStartUpRScript && nodes.IsSingleNodePath()) {
                return Task.FromResult(new CommandStatusResult(true, commandText, CommandStatus.Enabled | CommandStatus.Supported));
            }
            return Task.FromResult(CommandStatusResult.Unhandled);
        }


        public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut) {
            _coreShell.AssertIsOnMainThread();
            if (commandId != RPackageCommandId.icmdSetAsStartUpRScript) {
                return false;
            }
            var properties = _configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
            var path = nodes.GetSingleNodePath();
            var relativePath = _configuredProject.UnconfiguredProject.MakeRelative(path);
            await properties.SetStartupFileAsync(relativePath);
            return true;
        }
    }
}
