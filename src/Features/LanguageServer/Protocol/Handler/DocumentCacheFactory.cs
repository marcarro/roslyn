﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(DocumentCache)), Shared]
    internal class DocumentCacheFactory : ILspServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentCacheFactory()
        {
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind) => new DocumentCache();
    }
}
