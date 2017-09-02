﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.R.Components.Test.Fakes.Undo {
    /// <summary>
    /// These are the three states for the DelegatedUndoPrimitives. If Redoing or Undoing, a Redo or undo is in progress. In the 
    /// inactive case, it is illegal to send new operations to the primitive.
    /// </summary>
    internal enum DelegatedUndoPrimitiveState {
        /// <summary>
        /// No redo or undo is in progress, and it is illegal to send new operations to the primitive.
        /// </summary>
        Inactive,

        /// <summary>
        /// A redo is in progress. New operations go into the undo list.
        /// </summary>
        Redoing,

        /// <summary>
        /// An undo is in progress. New operations go into the redo list.
        /// </summary>
        Undoing
    }
}
