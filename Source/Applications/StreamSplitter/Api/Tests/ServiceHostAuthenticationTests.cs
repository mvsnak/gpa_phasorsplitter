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
//  09/02/2026 - Eduardo Oliveira
//       Copilot Review follow-up (Story 7.4.8): replaced the single-scheme assumption with coverage
//       for TryParseAuthenticationSchemes (Basic/Ntlm/Negotiate/IntegratedWindowsAuthentication and
//       combinations, now that they are genuinely enforced) and for SelectAuthenticationScheme (the
//       per-request anonymous-path carve-out wired into the OWIN HttpListener).
//
//******************************************************************************************************

using System.Net;
using System.Text.RegularExpressions;
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
        [InlineData("Windows")]
        [InlineData("Anonymously")]
        [InlineData("Digest")]
        [InlineData("None")]
        public void IsAuthenticationSchemeSupported_WhenUnsupportedValue_ReturnsFalse(string scheme)
        {
            bool result = ServiceHost.IsAuthenticationSchemeSupported(scheme);

            Assert.False(result);
        }

        [Theory]
        [InlineData("Basic", AuthenticationSchemes.Basic)]
        [InlineData("basic", AuthenticationSchemes.Basic)]
        [InlineData("Ntlm", AuthenticationSchemes.Ntlm)]
        [InlineData("Negotiate", AuthenticationSchemes.Negotiate)]
        [InlineData("IntegratedWindowsAuthentication", AuthenticationSchemes.IntegratedWindowsAuthentication)]
        [InlineData("Basic, Ntlm", AuthenticationSchemes.Basic | AuthenticationSchemes.Ntlm)]
        [InlineData("Anonymous", AuthenticationSchemes.Anonymous)]
        public void TryParseAuthenticationSchemes_WhenSupportedValue_ReturnsTrueAndParsedSchemes(string configuredValue, AuthenticationSchemes expected)
        {
            bool result = ServiceHost.TryParseAuthenticationSchemes(configuredValue, out AuthenticationSchemes schemes);

            Assert.True(result);
            Assert.Equal(expected, schemes);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Windows")]
        [InlineData("Anonymously")]
        [InlineData("Digest")]
        [InlineData("None")]
        [InlineData("Basic, Digest")]
        public void TryParseAuthenticationSchemes_WhenUnsupportedValue_ReturnsFalseAndAnonymous(string configuredValue)
        {
            bool result = ServiceHost.TryParseAuthenticationSchemes(configuredValue, out AuthenticationSchemes schemes);

            Assert.False(result);
            Assert.Equal(AuthenticationSchemes.Anonymous, schemes);
        }

        [Fact]
        public void SelectAuthenticationScheme_WhenPathMatchesAnonymousPattern_ReturnsAnonymous()
        {
            Regex pattern = new("^/api/");

            AuthenticationSchemes result = ServiceHost.SelectAuthenticationScheme("/api/connections", pattern, AuthenticationSchemes.Ntlm);

            Assert.Equal(AuthenticationSchemes.Anonymous, result);
        }

        [Fact]
        public void SelectAuthenticationScheme_WhenPathDoesNotMatchAnonymousPattern_ReturnsConfiguredSchemes()
        {
            Regex pattern = new("^/api/");

            AuthenticationSchemes result = ServiceHost.SelectAuthenticationScheme("/swagger", pattern, AuthenticationSchemes.Ntlm);

            Assert.Equal(AuthenticationSchemes.Ntlm, result);
        }

        [Fact]
        public void SelectAuthenticationScheme_WhenPatternIsNull_ReturnsConfiguredSchemes()
        {
            AuthenticationSchemes result = ServiceHost.SelectAuthenticationScheme("/api/connections", null, AuthenticationSchemes.Basic);

            Assert.Equal(AuthenticationSchemes.Basic, result);
        }

        [Fact]
        public void SelectAuthenticationScheme_WhenPathIsNull_DoesNotThrow()
        {
            Regex pattern = new("^/api/");

            AuthenticationSchemes result = ServiceHost.SelectAuthenticationScheme(null, pattern, AuthenticationSchemes.Basic);

            Assert.Equal(AuthenticationSchemes.Basic, result);
        }

        #endregion [ Methods ]
    }
}