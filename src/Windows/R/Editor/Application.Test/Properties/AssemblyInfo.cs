﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.R.Editor.Application.Test.Fixtures;
using Microsoft.UnitTests.Core.XUnit;

[assembly: TestFrameworkOverride]
[assembly: VsAssemblyLoader]
[assembly: AssemblyFixtureImport(typeof(REditorApplicationServicesFixture))]
