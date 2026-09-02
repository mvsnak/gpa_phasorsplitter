//******************************************************************************************************
//  BoundedMultipartMemoryStreamProviderTests.cs - Gbtc
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
//       Generated original version of source code (Story 7.4.8 - Copilot Review follow-up).
//
//******************************************************************************************************

using System.Threading;
using System.Threading.Tasks;
using StreamSplitter.Api;
using Xunit;

namespace StreamSplitter.Api.Tests
{
    public class BoundedMultipartMemoryStreamProviderTests
    {
        #region [ Methods ]

        [Fact]
        public void MaxLengthMemoryStream_WhenWriteWithinLimit_Succeeds()
        {
            using MaxLengthMemoryStream stream = new(maxLength: 10);
            byte[] data = { 1, 2, 3 };

            stream.Write(data, 0, data.Length);

            Assert.Equal(3, stream.Length);
        }

        [Fact]
        public void MaxLengthMemoryStream_WhenSingleWriteExceedsLimit_ThrowsMaxLengthExceededException()
        {
            using MaxLengthMemoryStream stream = new(maxLength: 2);
            byte[] data = { 1, 2, 3 };

            Assert.Throws<MaxLengthExceededException>(() => stream.Write(data, 0, data.Length));
        }

        [Fact]
        public void MaxLengthMemoryStream_WhenCumulativeWritesExceedLimit_ThrowsMaxLengthExceededException()
        {
            using MaxLengthMemoryStream stream = new(maxLength: 4);
            byte[] chunk = { 1, 2, 3 };

            // First write is within the limit (3 <= 4)...
            stream.Write(chunk, 0, chunk.Length);

            // ...but the second pushes the cumulative total (6) past it, guarding against a client
            // that trickles data in small chunks to dodge a naive single-write size check.
            Assert.Throws<MaxLengthExceededException>(() => stream.Write(chunk, 0, chunk.Length));
        }

        [Fact]
        public async Task MaxLengthMemoryStream_WhenWriteAsyncExceedsLimit_ThrowsMaxLengthExceededException()
        {
            using MaxLengthMemoryStream stream = new(maxLength: 2);
            byte[] data = { 1, 2, 3 };

            await Assert.ThrowsAsync<MaxLengthExceededException>(
                () => stream.WriteAsync(data, 0, data.Length, CancellationToken.None));
        }

        [Fact]
        public void GetStream_ReturnsStreamBoundedByConfiguredMaxLength()
        {
            BoundedMultipartMemoryStreamProvider provider = new(maxPartLengthBytes: 2);

            using System.IO.Stream stream = provider.GetStream(parent: null, headers: null);
            byte[] data = { 1, 2, 3 };

            Assert.Throws<MaxLengthExceededException>(() => stream.Write(data, 0, data.Length));
        }

        #endregion [ Methods ]
    }
}