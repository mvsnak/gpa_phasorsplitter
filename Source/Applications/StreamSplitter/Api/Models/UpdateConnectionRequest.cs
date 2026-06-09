//******************************************************************************************************
//  UpdateConnectionRequest.cs - Gbtc
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
//  06/05/2026 - Marcos Vinicius Snak
//       Generated original version of source code.
//  06/08/2026 - Marcos Vinicius Snak
//       Removed ConnectionString field; PATCH now uses individual sub-fields only for
//       true partial-update semantics without conflicting override behaviour.
//
//******************************************************************************************************

namespace StreamSplitter.Api.Models
{
    /// <summary>
    /// Request body for partially updating an existing proxy connection via the REST API.
    /// All properties are optional — only the fields that are present (non-null) are applied;
    /// omitted fields retain their current values.
    /// </summary>
    public class UpdateConnectionRequest
    {
        #region [ Properties ]

        /// <summary>
        /// Gets or sets the display name of the connection.
        /// Null keeps the existing value.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection should be enabled.
        /// Null keeps the existing value.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the source-side settings sub-string.
        /// Null keeps the existing value; empty string removes the sub-string entirely.
        /// </summary>
        public string SourceSettings { get; set; }

        /// <summary>
        /// Gets or sets the proxy-side settings sub-string.
        /// Null keeps the existing value; empty string removes the sub-string entirely.
        /// </summary>
        public string ProxySettings { get; set; }

        #endregion
    }
}
