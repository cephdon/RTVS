﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.Common.Core.Extensions {
    public static partial class CompositionBatchExtensions {
        internal class FactoryReflectionComposablePart : ComposablePart {
            private readonly ComposablePartDefinition _composablePartDefinition;
            private readonly Lazy<ComposablePart> _composablePart;
            private readonly IDictionary<ImportDefinition, IEnumerable<Export>> _imports;

            public FactoryReflectionComposablePart(ComposablePartDefinition composablePartDefinition, Delegate valueFactory) {
                _composablePartDefinition = composablePartDefinition;
                _composablePart = new Lazy<ComposablePart>(() => CreatePart(valueFactory));
                _imports = new Dictionary<ImportDefinition, IEnumerable<Export>>();
            }

            public override IEnumerable<ExportDefinition> ExportDefinitions => _composablePartDefinition.ExportDefinitions;
            public override IEnumerable<ImportDefinition> ImportDefinitions => _composablePartDefinition.ImportDefinitions;
            public override IDictionary<string, object> Metadata => _composablePartDefinition.Metadata;

            public override void Activate() => _composablePart.Value.Activate();
            public override object GetExportedValue(ExportDefinition definition) => _composablePart.Value.GetExportedValue(definition);

            public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports) {
                if (_composablePart.IsValueCreated ||
                    _imports == null) {
                    _composablePart.Value.SetImport(definition, exports);
                } else {
                    _imports.Add(definition, exports);
                }
            }

            private ComposablePart CreatePart(Delegate valueFactory) {
                var args = GetArguments(valueFactory);
                var value = valueFactory.DynamicInvoke(args);
                var part = AttributedModelServices.CreatePart(_composablePartDefinition, value);
                foreach (var import in _imports) {
                    part.SetImport(import.Key, import.Value);
                }

                _imports.Clear();
                return part;
            }

            private object[] GetArguments(Delegate valueFactory) {
                var arguments = new object[valueFactory.GetMethodInfo().GetParameters().Length];

                var ctorImportDefinitions = ImportDefinitions.Where(ReflectionModelServices.IsImportingParameter);
                foreach (var ctorImportDefinition in ctorImportDefinitions) {
                    var parameterInfo = ReflectionModelServices.GetImportingParameter(ctorImportDefinition).Value;
                    if (_imports.TryGetValue(ctorImportDefinition, out var value)) {
                        arguments[parameterInfo.Position] = value.Single().Value;
                        _imports.Remove(ctorImportDefinition);
                    } else {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "There is no export for parameter of type {0}",
                            parameterInfo.ParameterType));
                    }
                }

                return arguments;
            }
        }
    }
}