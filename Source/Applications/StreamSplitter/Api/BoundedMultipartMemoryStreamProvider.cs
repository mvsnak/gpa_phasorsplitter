//******************************************************************************************************
//  BoundedMultipartMemoryStreamProvider.cs - Gbtc
//
//  Copyright © 2026, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  09/02/2026 - Eduardo Oliveira
//       Generated original version of source code (Copilot Review follow-up). The
//       Content-Length pre-check in ConnectionsController.ImportFromFile only rejects uploads that
//       honestly report their size; a chunked request with no Content-Length would still be buffered
//       in full by MultipartMemoryStreamProvider. This provider enforces the size cap while the body
//       is being read, independent of any client-supplied header.
//
//******************************************************************************************************

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace StreamSplitter.Api
{
    /// <summary>
    /// Thrown by <see cref="MaxLengthMemoryStream"/> when writing would exceed the configured
    /// maximum length, so callers can distinguish an oversized upload from other stream failures.
    /// </summary>
    [Serializable]
    public sealed class MaxLengthExceededException : IOException
    {
        public MaxLengthExceededException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// A <see cref="MultipartMemoryStreamProvider"/> that caps each buffered part at <paramref
    /// name="maxPartLengthBytes"/>, so an upload is rejected as soon as it exceeds the configured
    /// maximum instead of being fully buffered first.
    /// </summary>
    internal sealed class BoundedMultipartMemoryStreamProvider : MultipartMemoryStreamProvider
    {
        private readonly int m_maxPartLengthBytes;

        public BoundedMultipartMemoryStreamProvider(int maxPartLengthBytes)
        {
            m_maxPartLengthBytes = maxPartLengthBytes;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            return new MaxLengthMemoryStream(m_maxPartLengthBytes);
        }
    }

    /// <summary>
    /// A <see cref="MemoryStream"/> that throws <see cref="MaxLengthExceededException"/> instead of
    /// growing past <paramref name="maxLength"/> bytes.
    /// </summary>
    internal sealed class MaxLengthMemoryStream : MemoryStream
    {
        private readonly int m_maxLength;

        public MaxLengthMemoryStream(int maxLength)
        {
            m_maxLength = maxLength;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Length + count > m_maxLength)
                throw new MaxLengthExceededException($"Upload exceeds the maximum allowed size of {m_maxLength} bytes.");

            base.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (Length + count > m_maxLength)
                return Task.FromException(new MaxLengthExceededException($"Upload exceeds the maximum allowed size of {m_maxLength} bytes."));

            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}