﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class CpsDiagnosticItemSource : BaseDiagnosticAndGeneratorItemSource, INotifyPropertyChanged
    {
        private readonly IVsHierarchyItem _item;
        private readonly string _projectDirectoryPath;

        private AnalyzerReference? _analyzerReference;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CpsDiagnosticItemSource(Workspace workspace, string projectPath, ProjectId projectId, IVsHierarchyItem item, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService analyzerService)
            : base(workspace, projectId, commandHandler, analyzerService)
        {
            _item = item;
            _projectDirectoryPath = Path.GetDirectoryName(projectPath);

            _analyzerReference = TryGetAnalyzerReference(Workspace.CurrentSolution);
            if (_analyzerReference == null)
            {
                // The ProjectId that was given to us was found by enumerating the list of projects in the solution, thus the project must have already
                // been added to the workspace at some point. As long as the project is still there, we're going to assume the reason we don't have the reference
                // yet is because while we have a project, we don't have all the references added yet. We'll wait until we see the reference and then connect to it.
                if (workspace.CurrentSolution.ContainsProject(projectId))
                {
                    Workspace.WorkspaceChanged += OnWorkspaceChangedLookForAnalyzer;
                }
            }
        }

        public IContextMenuController DiagnosticItemContextMenuController => CommandHandler.DiagnosticContextMenuController;

        public override object SourceItem => _item;

        public override AnalyzerReference? AnalyzerReference => _analyzerReference;

        private void OnWorkspaceChangedLookForAnalyzer(object sender, WorkspaceChangeEventArgs e)
        {
            // If the project has gone away in this change, it's not coming back, so we can stop looking at this point
            if (!e.NewSolution.ContainsProject(ProjectId))
            {
                Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
                return;
            }

            // Was this a change to our project, or a global change?
            if (e.ProjectId == ProjectId ||
                e.Kind == WorkspaceChangeKind.SolutionChanged)
            {
                _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                if (_analyzerReference != null)
                {
                    Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                }
            }
        }

        private AnalyzerReference? TryGetAnalyzerReference(Solution solution)
        {
            var project = solution.GetProject(ProjectId);

            if (project == null)
            {
                return null;
            }

            var canonicalName = _item.CanonicalName;
            var analyzerFilePath = CpsUtilities.ExtractAnalyzerFilePath(_projectDirectoryPath, canonicalName);

            if (string.IsNullOrEmpty(analyzerFilePath))
            {
                return null;
            }

            return project.AnalyzerReferences.FirstOrDefault(r => string.Equals(r.FullPath, analyzerFilePath, StringComparison.OrdinalIgnoreCase));
        }
    }
}
