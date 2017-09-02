﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.Test.Controls;
using Microsoft.R.Components.Settings;
using Microsoft.UnitTests.Core.Threading;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.R.Interactive.Test.Utility;
using Microsoft.VisualStudio.R.Package.DataInspect;
using Microsoft.VisualStudio.R.Package.Test;
using Microsoft.VisualStudio.R.Package.Test.DataInspect;
using Xunit;

namespace Microsoft.VisualStudio.R.Interactive.Test.Data {
    [ExcludeFromCodeCoverage]
    [Category.Interactive]
    [Collection(CollectionNames.NonParallel)]
    public class VariableGridTest : InteractiveTest {
        private readonly TestFilesFixture _files;
        private readonly VariableRHostScript _hostScript;

        public VariableGridTest(IServiceContainer services, TestFilesFixture files) : base(services) {
            _files = files;
            _hostScript = new VariableRHostScript(Services);
        }

        public override Task DisposeAsync() {
            _hostScript.Dispose();
            return base.DisposeAsync();
        }

        [Test]
        public async Task ConstructorTest() {
            using (var script = new ControlTestScript(typeof(VariableGridHost), Services)) {
                await PrepareControl(_hostScript, script, "grid.test <- matrix(1:10, 2, 5)");
                var actual = VisualTreeObject.Create(script.Control);
                ViewTreeDump.CompareVisualTrees(_files, actual, "VariableGrid02");
            }
        }

        [Test]
        public async Task SortTest01() {
            using (var script = new ControlTestScript(typeof(VariableGridHost), Services)) {
                await PrepareControl(_hostScript, script, "grid.test <- matrix(1:10, 2, 5)");

                var header = await VisualTreeTestExtensions.FindFirstVisualChildOfType<HeaderTextVisual>(script.Control);
                header.Should().NotBeNull();

                var grid = await VisualTreeTestExtensions.FindFirstVisualChildOfType<VisualGrid>(script.Control);
                await UIThreadHelper.Instance.InvokeAsync(() => {
                    grid.ToggleSort(new GridIndex(0, header.ColumnIndex), false);
                    DoIdle(200);
                    grid.ToggleSort(new GridIndex(0, header.ColumnIndex), false);
                });
                DoIdle(200);

                var actual = VisualTreeObject.Create(script.Control);
                ViewTreeDump.CompareVisualTrees(_files, actual, "VariableGridSorted01");
            }
        }

        [Test]
        public async Task SortTest02() {
            using (var script = new ControlTestScript(typeof(VariableGridHost), Services)) {
                await PrepareControl(_hostScript, script, "grid.test <- mtcars");

                var header = await VisualTreeTestExtensions.FindFirstVisualChildOfType<HeaderTextVisual>(script.Control); // mpg
                header = await VisualTreeTestExtensions.FindNextVisualSiblingOfType<HeaderTextVisual>(header); // cyl
                header.Should().NotBeNull();

                var grid = await VisualTreeTestExtensions.FindFirstVisualChildOfType<VisualGrid>(script.Control);
                await UIThreadHelper.Instance.InvokeAsync(async () => {
                    grid.ToggleSort(new GridIndex(0, header.ColumnIndex), false);
                    DoIdle(200);

                    header = await VisualTreeTestExtensions.FindNextVisualSiblingOfType<HeaderTextVisual>(header); // disp
                    grid.ToggleSort(new GridIndex(0, header.ColumnIndex), add: true);

                    header = await VisualTreeTestExtensions.FindNextVisualSiblingOfType<HeaderTextVisual>(header); // hp
                    grid.ToggleSort(new GridIndex(0, header.ColumnIndex), add: true);

                    DoIdle(200);
                });

                var actual = VisualTreeObject.Create(script.Control);
                ViewTreeDump.CompareVisualTrees(_files, actual, "VariableGridSorted02");
            }
        }

        [CompositeTest]
        [InlineData(false, "VariableGridSnapshot")]
        [InlineData(true, "VariableGridDynamic")]
        public async Task EvalModeTest(bool dynamicMode, string baselineName) {
            using (var script = new ControlTestScript(typeof(VariableGridHost), Services)) {
                using (var inter = await _hostScript.Session.BeginInteractionAsync()) {
                    await inter.RespondAsync("grid.test <- matrix(1:10)");
                }
                await PrepareControl(_hostScript, script, "grid.test");

                Services.GetService<IRSettings>().GridDynamicEvaluation = dynamicMode;
                using (var inter = await _hostScript.Session.BeginInteractionAsync()) {
                    await inter.RespondAsync("grid.test <- matrix(1:20)");
                }

                await UIThreadHelper.Instance.InvokeAsync(() => {
                    DoIdle(500);
                });

                var actual = VisualTreeObject.Create(script.Control);
                ViewTreeDump.CompareVisualTrees(_files, actual, baselineName);
            }
        }

        private async Task PrepareControl(VariableRHostScript hostScript, ControlTestScript script, string expression) {
            DoIdle(100);

            var result = await hostScript.EvaluateAsync(expression);
            var wrapper = new VariableViewModel(result, Services);

            DoIdle(2000);
            wrapper.Should().NotBeNull();

            await UIThreadHelper.Instance.InvokeAsync(() => {
                var host = (VariableGridHost)script.Control;
                host.SetEvaluation(wrapper);
            });

            await WaitForControlReady(script);
        }

        private Task<bool> WaitForControlReady(ControlTestScript script)
            => Task.Run(async () => {
                var startTime = DateTime.Now;
                HeaderTextVisual header = null;
                while (header == null && (DateTime.Now - startTime).TotalMilliseconds < 5000) {
                    DoIdle(200);
                    header = await VisualTreeTestExtensions.FindFirstVisualChildOfType<HeaderTextVisual>(script.Control);
                }
                return header != null;
            });
    }
}
