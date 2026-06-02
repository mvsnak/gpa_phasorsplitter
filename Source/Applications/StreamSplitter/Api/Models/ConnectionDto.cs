//******************************************************************************************************
//  ConnectionDto.cs - Gbtc
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
//  06/01/2026 - Marcos Vinicius Snak
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using StreamSplitter;

namespace StreamSplitter.Api.Models
{
    /// <summary>
    /// Read-only data transfer object representing a <see cref="ProxyConnection"/>.
    /// </summary>
    public sealed class ConnectionDto
    {
        #region [ Constructors ]

        private ConnectionDto()
        {
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the unique identifier of the connection.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the display name of the connection.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the connection is enabled.
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Gets the current <see cref="ConnectionState"/> of the connection.
        /// </summary>
        public ConnectionState ConnectionState { get; private set; }

        /// <summary>
        /// Gets a human-readable description of the current connection state.
        /// </summary>
        public string ConnectionStateDescription { get; private set; }

        /// <summary>
        /// Gets the raw GSF connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets the source-side settings extracted from the connection string.
        /// </summary>
        public string SourceSettings { get; private set; }

        /// <summary>
        /// Gets the proxy-side settings extracted from the connection string.
        /// </summary>
        public string ProxySettings { get; private set; }

        #endregion

        #region [ Static ]

        // Static Methods

        /// <summary>
        /// Creates a new <see cref="ConnectionDto"/> from a <see cref="ProxyConnection"/>.
        /// </summary>
        /// <param name="connection">Source <see cref="ProxyConnection"/> to map from.</param>
        /// <returns>A populated <see cref="ConnectionDto"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <c>null</c>.</exception>
        public static ConnectionDto FromProxyConnection(ProxyConnection connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            return new ConnectionDto
            {
                Id                         = connection.ID,
                Name                       = connection.Name,
                Enabled                    = connection.Enabled,
                ConnectionState            = connection.ConnectionState,
                ConnectionStateDescription = connection.ConnectionStateDescription,
                ConnectionString           = connection.ConnectionString,
                SourceSettings             = connection.SourceSettings,
                ProxySettings              = connection.ProxySettings
            };
        }

        #endregion
    }
}
