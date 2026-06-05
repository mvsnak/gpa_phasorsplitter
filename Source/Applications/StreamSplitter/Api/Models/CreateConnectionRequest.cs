//******************************************************************************************************
//  CreateConnectionRequest.cs - Gbtc
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
//
//******************************************************************************************************

namespace StreamSplitter.Api.Models
{
    /// <summary>
    /// Request body for creating a new proxy connection via the REST API.
    /// </summary>
    public class CreateConnectionRequest
    {
        #region [ Properties ]

        /// <summary>
        /// Gets or sets the full GSF connection string (key=value pairs).
        /// The string must include all required parameters such as name, enabled,
        /// sourceSettings and proxySettings embedded in the standard GSF format.
        /// This is the only required field; all other connection attributes are
        /// parsed from it automatically.
        /// </summary>
        public string ConnectionString { get; set; }

        #endregion
    }
}
