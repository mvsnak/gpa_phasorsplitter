//******************************************************************************************************
//  ConnectionStatusDto.cs - Gbtc
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
//  06/08/2026 - Marcos Vinicius Snak
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using GSF;

namespace StreamSplitter.Api.Models
{
    /// <summary>
    /// Lightweight data transfer object representing the current operational status of a
    /// <see cref="ProxyConnection"/> and its associated <see cref="StreamProxy"/>.
    /// Returned by <c>GET /api/connections/{id}/status</c>.
    /// </summary>
    public sealed class ConnectionStatusDto
    {
        #region [ Properties ]

        /// <summary>Gets the unique identifier of the connection.</summary>
        public Guid Id { get; private set; }

        /// <summary>Gets the display name of the connection.</summary>
        public string Name { get; private set; }

        /// <summary>Gets a value indicating whether the connection is enabled in configuration.</summary>
        public bool Enabled { get; private set; }

        /// <summary>Gets the current runtime <see cref="ConnectionState"/> of the connection.</summary>
        public ConnectionState ConnectionState { get; private set; }

        /// <summary>Gets a human-readable description of the current connection state.</summary>
        public string ConnectionStateDescription { get; private set; }

        /// <summary>
        /// Gets the source connection endpoint in the format <c>host:port/accessID</c>.
        /// <c>null</c> when no live <see cref="StreamProxy"/> exists (e.g. connection is disabled).
        /// </summary>
        public string ConnectionInfo { get; private set; }

        /// <summary>
        /// Gets the total number of bytes sent to downstream clients since the proxy started.
        /// Zero when no live <see cref="StreamProxy"/> exists.
        /// </summary>
        public long TotalBytesSent { get; private set; }

        /// <summary>
        /// Gets the total active run time of the proxy in seconds.
        /// Zero when no live <see cref="StreamProxy"/> exists.
        /// </summary>
        public double RunTime { get; private set; }

        /// <summary>
        /// Gets the most recent status messages produced by the proxy (up to 2 048 characters).
        /// <c>null</c> when no live <see cref="StreamProxy"/> exists.
        /// </summary>
        public string RecentStatusMessages { get; private set; }

        #endregion

        #region [ Static ]

        // Static Methods

        /// <summary>
        /// Creates a <see cref="ConnectionStatusDto"/> from the provided service-layer objects.
        /// </summary>
        /// <param name="id">Connection identifier.</param>
        /// <param name="connection">
        /// <see cref="ProxyConnection"/> that holds the configuration. Must not be <c>null</c>.
        /// </param>
        /// <param name="splitter">
        /// Live <see cref="StreamProxy"/> associated with <paramref name="connection"/>,
        /// or <c>null</c> when the connection is disabled or not yet materialized.
        /// </param>
        /// <returns>A populated <see cref="ConnectionStatusDto"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> is <c>null</c>.
        /// </exception>
        public static ConnectionStatusDto FromServiceHost(Guid id, ProxyConnection connection, StreamProxy splitter)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            ConnectionState state = splitter?.StreamProxyStatus.ConnectionState ?? ConnectionState.Disabled;

            return new ConnectionStatusDto
            {
                Id                         = id,
                Name                       = connection.Name,
                Enabled                    = connection.Enabled,
                ConnectionState            = state,
                ConnectionStateDescription = state.GetDescription(),
                ConnectionInfo             = splitter?.ConnectionInfo,
                TotalBytesSent             = splitter?.TotalBytesSent ?? 0L,
                RunTime                    = (double)(splitter?.RunTime ?? 0.0D),
                RecentStatusMessages       = splitter?.StreamProxyStatus.RecentStatusMessages
            };
        }

        #endregion
    }
}
