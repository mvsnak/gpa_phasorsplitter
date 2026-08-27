//******************************************************************************************************
//  ConnectionsControllerTests.cs - Gbtc
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
//  08/22/2026 - Eduardo Oliveira
//       Generated original version of source code (Story 7.4.8 - Copilot Review follow-up).
//
//******************************************************************************************************

using System.Collections.Generic;
using GSF;
using StreamSplitter.Api.Controllers;
using StreamSplitter.Api.Models;
using Xunit;

namespace StreamSplitter.Api.Tests
{
    public class ConnectionsControllerTests
    {
        #region [ Methods ]

        // ExceedsMaxImportSize

        [Fact]
        public void ExceedsMaxImportSize_WhenContentLengthIsNull_ReturnsFalse()
        {
            bool result = ConnectionsController.ExceedsMaxImportSize(null, 100);

            Assert.False(result);
        }

        [Fact]
        public void ExceedsMaxImportSize_WhenContentLengthBelowMax_ReturnsFalse()
        {
            bool result = ConnectionsController.ExceedsMaxImportSize(50L, 100);

            Assert.False(result);
        }

        [Fact]
        public void ExceedsMaxImportSize_WhenContentLengthEqualsMax_ReturnsFalse()
        {
            bool result = ConnectionsController.ExceedsMaxImportSize(100L, 100);

            Assert.False(result);
        }

        [Fact]
        public void ExceedsMaxImportSize_WhenContentLengthAboveMax_ReturnsTrue()
        {
            bool result = ConnectionsController.ExceedsMaxImportSize(101L, 100);

            Assert.True(result);
        }

        // MergeConnectionString (Task 7.4.8.8 - partial update without overwrite)

        private const string ExistingConnectionString =
            "name=PDC-001;enabled=true;sourceSettings={server=192.168.1.1;port=4712};proxySettings={port=4713};maximumConnectionAttempts=-1";

        [Fact]
        public void MergeConnectionString_WhenOnlyEnabledProvided_PreservesOtherFieldsAndExtraKey()
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest { Enabled = false };

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();

            Assert.Equal("PDC-001", settings["name"]);
            Assert.Equal("false", settings["enabled"]);
            Assert.Equal("server=192.168.1.1;port=4712", settings["sourceSettings"]);
            Assert.Equal("port=4713", settings["proxySettings"]);
            Assert.Equal("-1", settings["maximumConnectionAttempts"]);
        }

        [Fact]
        public void MergeConnectionString_WhenOnlyNameProvided_PreservesEnabledAndSettings()
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest { Name = "PDC-001-renamed" };

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();

            Assert.Equal("PDC-001-renamed", settings["name"]);
            Assert.Equal("true", settings["enabled"]);
            Assert.Equal("server=192.168.1.1;port=4712", settings["sourceSettings"]);
            Assert.Equal("port=4713", settings["proxySettings"]);
        }

        [Fact]
        public void MergeConnectionString_WhenSourceSettingsIsEmptyString_RemovesSourceSettings()
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest { SourceSettings = string.Empty };

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();

            Assert.False(settings.ContainsKey("sourceSettings"));
            Assert.Equal("PDC-001", settings["name"]);
            Assert.Equal("true", settings["enabled"]);
            Assert.Equal("port=4713", settings["proxySettings"]);
        }

        [Fact]
        public void MergeConnectionString_WhenProxySettingsIsEmptyString_RemovesProxySettings()
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest { ProxySettings = string.Empty };

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();

            Assert.False(settings.ContainsKey("proxySettings"));
            Assert.Equal("server=192.168.1.1;port=4712", settings["sourceSettings"]);
        }

        [Fact]
        public void MergeConnectionString_WhenSourceSettingsProvided_ReplacesSourceSettingsOnly()
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest
            {
                SourceSettings = "server=192.168.1.2;port=4712"
            };

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();

            Assert.Equal("server=192.168.1.2;port=4712", settings["sourceSettings"]);
            Assert.Equal("port=4713", settings["proxySettings"]);
            Assert.Equal("PDC-001", settings["name"]);
            Assert.Equal("true", settings["enabled"]);
        }

        [Fact]
        public void MergeConnectionString_WhenNoFieldsProvided_ReturnsEquivalentConnectionString()
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest();

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();
            Dictionary<string, string> originalSettings = ExistingConnectionString.ParseKeyValuePairs();

            Assert.Equal(originalSettings["name"], settings["name"]);
            Assert.Equal(originalSettings["enabled"], settings["enabled"]);
            Assert.Equal(originalSettings["sourceSettings"], settings["sourceSettings"]);
            Assert.Equal(originalSettings["proxySettings"], settings["proxySettings"]);
            Assert.Equal(originalSettings["maximumConnectionAttempts"], settings["maximumConnectionAttempts"]);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void MergeConnectionString_WhenEnabledProvided_SerializesAsLowerInvariant(bool enabled, string expected)
        {
            UpdateConnectionRequest request = new UpdateConnectionRequest { Enabled = enabled };

            string merged = ConnectionsController.MergeConnectionString(ExistingConnectionString, request);
            Dictionary<string, string> settings = merged.ParseKeyValuePairs();

            Assert.Equal(expected, settings["enabled"]);
        }

        #endregion
    }
}
