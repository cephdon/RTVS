// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.Common.Core.UI.Commands;
using Microsoft.R.Components.History;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.R.Package.History.Commands {
    internal sealed class HistoryWindowVsStd2KCmdIdLeftExt : NavigationCommandBase {
        public HistoryWindowVsStd2KCmdIdLeftExt(ITextView textView, IRHistoryProvider historyProvider) 
            : base(textView, historyProvider, VSConstants.VSStd2KCmdID.LEFT_EXT) {}

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            History.ToggleTextSelectionLeft();
            return CommandResult.Executed;
        }
    }
}