//******************************************************************************************************
//  ServiceHostAuthenticationTests.cs - Gbtc
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

using StreamSplitter;
using Xunit;

namespace StreamSplitter.Api.Tests
{
    public class ServiceHostAuthenticationTests
    {
        #region [ Methods ]

        [Theory]
        [InlineData("Anonymous")]
        [InlineData("anonymous")]
        [InlineData("ANONYMOUS")]
        [InlineData("  Anonymous  ")]
        public void IsAuthenticationSchemeSupported_WhenAnonymousInAnyCase_ReturnsTrue(string scheme)
        {
            bool result = ServiceHost.IsAuthenticationSchemeSupported(scheme);

            Assert.True(result);
        }

        [Fact]
        public void IsAuthenticationSchemeSupported_WhenNull_ReturnsFalse()
        {
            bool result = ServiceHost.IsAuthenticationSchemeSupported(null);

            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Basic")]
        [InlineData("Windows")]
        [InlineData("Anonymously")]
        public void IsAuthenticationSchemeSupported_WhenUnsupportedValue_ReturnsFalse(string scheme)
        {
            bool result = ServiceHost.IsAuthenticationSchemeSupported(scheme);

            Assert.False(result);
        }

        #endregion
    }
}
