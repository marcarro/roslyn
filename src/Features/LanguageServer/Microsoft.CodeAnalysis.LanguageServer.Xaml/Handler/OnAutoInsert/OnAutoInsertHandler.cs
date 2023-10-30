﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.AutoInsert;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(OnAutoInsertHandler)), Shared]
[XamlMethod(VSInternalMethods.OnAutoInsertName)]
internal class OnAutoInsertHandler : ILspServiceDocumentRequestHandler<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OnAutoInsertHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalDocumentOnAutoInsertParams request) => request.TextDocument;

    public async Task<VSInternalDocumentOnAutoInsertResponseItem?> HandleRequestAsync(VSInternalDocumentOnAutoInsertParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.TextDocument;
        if (document is null)
        {
            return null;
        }

        var insertService = document.Project.Services.GetService<IXamlAutoInsertService>();
        if (insertService is null)
        {
            return null;
        }

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));
        var result = await insertService.GetAutoInsertAsync(document, request.Character[0], offset, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        Contract.ThrowIfNull(result.TextChange.NewText);
        var insertText = result.TextChange.NewText;
        var insertFormat = InsertTextFormat.Plaintext;
        if (result.CaretOffset.HasValue)
        {
            insertFormat = InsertTextFormat.Snippet;
            insertText = insertText.Insert(result.CaretOffset.Value, "$0");
        }

        return new VSInternalDocumentOnAutoInsertResponseItem
        {
            TextEditFormat = insertFormat,
            TextEdit = new TextEdit
            {
                NewText = insertText,
                Range = ProtocolConversions.TextSpanToRange(result.TextChange.Span, text)
            }
        };
    }
}
