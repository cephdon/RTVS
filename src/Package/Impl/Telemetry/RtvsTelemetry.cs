﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Common.Core.Services;
using Microsoft.Common.Core.Telemetry;
using Microsoft.Markdown.Editor.Settings;
using Microsoft.R.Components.Settings;
using Microsoft.R.Editor;
using Microsoft.R.Editor.Functions;
using Microsoft.R.Interpreters;
using Microsoft.VisualStudio.R.Package.Telemetry.Data;
using Microsoft.VisualStudio.R.Package.Telemetry.Definitions;
using Microsoft.VisualStudio.R.Package.Telemetry.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.R.Package.Telemetry {
    /// <summary>
    /// Represents telemetry operations in RTVS
    /// </summary>
    internal sealed class RtvsTelemetry : IRtvsTelemetry {
        private ToolWindowTracker _toolWindowTracker;
        private readonly IPackageIndex _packageIndex;
        private readonly IRSettings _settings;
        private readonly IREditorSettings _editorSettings;
        private readonly IRMarkdownEditorSettings _markdownSettings;

        public static IRtvsTelemetry Current { get; set; }

        internal class ConfigurationEvents {
            public const string RtvsVersion = "RTVS Version";
            public const string RInstallPath = "R Install Path";
            public const string REngine = "R Engine";
            public const string RROEngine = "RRO Engine";
            public const string MROEngine = "MRO Engine";
            public const string RPackages = "R Package";
            public const string RClientFound = "MRC Found";
            public const string RClientInstallYes = "MRC Install Yes";
            public const string RClientInstallCancel = "MRC Install Canceled";
            public const string RClientActive = "MRC Active";
            public const string RClientDownloadFailed = "MRC Download Failed";
            public const string LocalConnection = "Local Connection";
            public const string RemoteConnection = "Remote Connection";
            public const string ContainerConnection = "Container Connection";
        }

        internal class SettingEvents {
            public const string Settings = "Settings";
        }

        internal class WindowEvents {
            public const string ToolWindow = "Tool Window";
        }

        public static void Initialize(IPackageIndex packageIndex, IServiceContainer services) {
            if (Current == null) {
                var settings = services.GetService<IRSettings>();
                var editorSettings = services.GetService<IREditorSettings>();
                var markdownSettings = services.GetService<IRMarkdownEditorSettings>();
                var telemetryService = services.GetService<ITelemetryService>();
                Current = new RtvsTelemetry(packageIndex, settings, editorSettings, markdownSettings, telemetryService, new ToolWindowTracker(services));
            }
        }

        public RtvsTelemetry(IPackageIndex packageIndex
            , IRSettings settings
            , IREditorSettings editorSettings
            , IRMarkdownEditorSettings markdownSettings
            , ITelemetryService telemetryService = null
            , ToolWindowTracker toolWindowTracker = null) {
            _packageIndex = packageIndex;
            _settings = settings;
            _editorSettings = editorSettings;
            _markdownSettings = markdownSettings;
            TelemetryService = telemetryService;
            _toolWindowTracker = toolWindowTracker;
        }

        public ITelemetryService TelemetryService { get; }

        public void ReportConfiguration() {
            if (TelemetryService.IsEnabled) {
                try {
                    var thisAssembly = Assembly.GetExecutingAssembly();
                    TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.RtvsVersion, thisAssembly.GetName().Version.ToString());

                    ReportLocalRConfiguration();
                    ReportConnectionsConfiguration();

                } catch (Exception ex) {
                    Trace.Fail("Telemetry exception: " + ex.Message);
                }
            }
        }

        private void ReportLocalRConfiguration() {
            // Report local R installations
            var engines = new RInstallation().GetCompatibleEngines();
            foreach (var e in engines) {
                TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.RInstallPath, e.InstallPath);
            }

            var rClientPath = SqlRClientInstallation.GetRClientPath();
            if (rClientPath != null) {
                TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.RClientFound);
            }

            var rEngines = GetRSubfolders("R");
            foreach (var s in rEngines) {
                TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.REngine, s);
            }

            var rroEngines = GetRSubfolders("RRO");
            foreach (var s in rroEngines) {
                TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.RROEngine, s);
            }

            var mroEngines = GetRSubfolders("MRO");
            foreach (var s in mroEngines) {
                TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.MROEngine, s);
            }

            if (_packageIndex != null) {
                foreach (var p in _packageIndex.Packages) {
                    TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.RPackages, new TelemetryPiiProperty(p.Name));
                }
            }
        }

        private void ReportConnectionsConfiguration() {
            var connections = _settings.Connections;
            if (connections != null) {
                foreach (var c in connections) {
                    if (Uri.TryCreate(c.Path, UriKind.Absolute, out Uri uri)) {
                        if (uri.IsFile) {
                            // Do not report local since they are reported via installed R
                            // TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.LocalConnection, uri.ToString());
                        } else if (uri.IsLoopback) {
                            TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.ContainerConnection, uri.ToString());
                        } else {
                            TelemetryService.ReportEvent(TelemetryArea.Configuration, ConfigurationEvents.RemoteConnection, new TelemetryPiiProperty(uri.ToString()));
                        }
                    }
                }
            }
        }

        public void ReportSettings() {
            if (TelemetryService.IsEnabled) {
                try {
                    TelemetryService.ReportEvent(TelemetryArea.Configuration, SettingEvents.Settings,
                            new {
                                Cran = _settings.CranMirror,
                                Locale = _settings.RCodePage,
                                LoadRData = _settings.LoadRDataOnProjectLoad,
                                SaveRData = _settings.SaveRDataOnProjectUnload,
                                MultilineHistorySelection = _settings.MultilineHistorySelection,
                                AlwaysSaveHistory = _settings.AlwaysSaveHistory,
                                // R Editor
                                AutoFormat = _editorSettings.AutoFormat,
                                CommitOnEnter = _editorSettings.CommitOnEnter,
                                CommitOnSpace = _editorSettings.CommitOnSpace,
                                FormatOnPaste = _editorSettings.FormatOnPaste,
                                ShowCompletionOnFirstChar = _editorSettings.ShowCompletionOnFirstChar,
                                SignatureHelpEnabled = _editorSettings.SignatureHelpEnabled,
                                CompletionEnabled = _editorSettings.CompletionEnabled,
                                SyntaxCheckInRepl = _editorSettings.SyntaxCheckInRepl,
                                PartialArgumentNameMatch = _editorSettings.PartialArgumentNameMatch,

                                // Do not report command line arguments - they may contain 
                                // PII information and they are not that interesting anyway.

                                // R Linter
                                LinterEnabled = _editorSettings.LintOptions.Enabled,
                                LinterCamelCase = _editorSettings.LintOptions.CamelCase,
                                LinterPascalCase = _editorSettings.LintOptions.PascalCase,
                                LinterSnakeCase = _editorSettings.LintOptions.SnakeCase,
                                LinterUpperCase = _editorSettings.LintOptions.UpperCase,
                                LinterSemicolons = _editorSettings.LintOptions.Semicolons,
                                LinterMultipleDots = _editorSettings.LintOptions.MultipleDots,
                                LinterMultipleStatements = _editorSettings.LintOptions.MultipleStatements,
                                LinterNameLength = _editorSettings.LintOptions.NameLength,
                                LinterMaxNameLength = _editorSettings.LintOptions.MaxNameLength,
                                LinterAssignmentType = _editorSettings.LintOptions.AssignmentType,
                                LinterSpaceAroundComma = _editorSettings.LintOptions.SpacesAroundComma,
                                LinterSpaceBeforeOpenBrace = _editorSettings.LintOptions.SpaceBeforeOpenBrace,
                                LinterSpacesAroundOperators = _editorSettings.LintOptions.SpacesAroundOperators,
                                LinterSpacesInsideParenthesis = _editorSettings.LintOptions.SpacesInsideParenthesis,
                                LinterCloseCurlySeparateLine = _editorSettings.LintOptions.CloseCurlySeparateLine,
                                LinterNoSpaceAfterFunctionName = _editorSettings.LintOptions.NoSpaceAfterFunctionName,
                                LinterOpenCurlyPosition = _editorSettings.LintOptions.OpenCurlyPosition,
                                LinterNoTabs = _editorSettings.LintOptions.NoTabs,
                                LinterTrailingWhitespace = _editorSettings.LintOptions.TrailingWhitespace,
                                LinterTrailingBlankLines = _editorSettings.LintOptions.TrailingBlankLines,
                                LinterDoubleQuotes = _editorSettings.LintOptions.DoubleQuotes,
                                LinterLineLength = _editorSettings.LintOptions.LineLength,
                                LinterMaxLineLength = _editorSettings.LintOptions.MaxLineLength,

                                // R formatter
                                FormatterTabSize = _editorSettings.FormatOptions.TabSize,
                                FormatterIndentSize = _editorSettings.FormatOptions.IndentSize,
                                FormatterIndentType = _editorSettings.FormatOptions.IndentType,
                                FormatterBreakMultipleStatements = _editorSettings.FormatOptions.BreakMultipleStatements,
                                FormatterBracesOnNewLine = _editorSettings.FormatOptions.BracesOnNewLine,
                                FormatterSpaceAfterComma = _editorSettings.FormatOptions.SpaceAfterComma,
                                FormatterSpaceAfterKeyword = _editorSettings.FormatOptions.SpaceAfterKeyword,
                                FormatterSpaceBeforeCurly = _editorSettings.FormatOptions.SpaceBeforeCurly,
                                FormatterSpacesAroundEquals = _editorSettings.FormatOptions.SpacesAroundEquals,

                                // R Markdown
                                MarkdownPreviewEnabled = _markdownSettings.EnablePreview,
                                MarkdownPreviewRight = _markdownSettings.PreviewPosition == RMarkdownPreviewPosition.Right,
                                MarkdownPreviewSync = _markdownSettings.AutomaticSync,
                                MarkdownPreviewScrollPreview = _markdownSettings.ScrollPreviewWithEditor,
                                MarkdownPreviewScrollEditor = _markdownSettings.ScrollEditorWithPreview,
                            });
                } catch (Exception ex) {
                    Trace.Fail("Telemetry exception: " + ex.Message);
                }
            }
        }

        public void ReportWindowLayout(IVsUIShell shell) {
            if (TelemetryService.IsEnabled) {
                try {
                    var windows = ToolWindowData.GetToolWindowData(shell);
                    foreach (var w in windows) {
                        TelemetryService.ReportEvent(TelemetryArea.Configuration, WindowEvents.ToolWindow,
                                new { Caption = w.Caption, Left = w.X, Top = w.Y, Width = w.Width, Height = w.Height });
                    }
                } catch (Exception ex) {
                    Trace.Fail("Telemetry exception: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Retrieves all subfolders under R, RRO or MRO
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetRSubfolders(string directory) {
            var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            var baseRFolder = Path.Combine(root + @"Program Files\", directory);
            try {
                return FolderUtility.GetSubfolderRelativePaths(baseRFolder);
            } catch (IOException) {
                // Don't do anything if there is no RRO installed
            }
            return Enumerable.Empty<string>();
        }

        public void Dispose() {
            _toolWindowTracker?.Dispose();
            _toolWindowTracker = null;

            var disp = TelemetryService as IDisposable;
            disp?.Dispose();
        }
    }
}
