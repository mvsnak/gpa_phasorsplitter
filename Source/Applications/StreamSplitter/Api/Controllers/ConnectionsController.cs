//******************************************************************************************************
//  ConnectionsController.cs - Gbtc
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
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using GSF.Diagnostics;
using StreamSplitter.Api.Models;

namespace StreamSplitter.Api.Controllers
{
    /// <summary>
    /// Provides read access to the currently configured proxy connections.
    /// </summary>
    [RoutePrefix("api/connections")]
    public class ConnectionsController : ApiController
    {
        #region [ Members ]

        // Fields
        private static readonly LogPublisher s_log =
            Logger.CreatePublisher(typeof(ConnectionsController), MessageClass.Application);

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Returns all currently configured proxy connections.
        /// </summary>
        /// <returns>Array of <see cref="ConnectionDto"/> objects representing all configured connections.</returns>
        [HttpGet, Route("")]
        public IHttpActionResult GetConnections()
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "GetConnections",
                $"GET /api/connections requested. CorrelationId={correlationId}");

            ProxyConnectionCollection configuration = ServiceHost.Current?.CurrentConfiguration;

            if (configuration is null)
            {
                s_log.Publish(
                    MessageLevel.Warning,
                    "GetConnections",
                    $"Configuration not yet loaded. Returning empty list. CorrelationId={correlationId}");

                return Ok(Array.Empty<ConnectionDto>());
            }

            ConnectionDto[] dtos = configuration
                .Select(c => ConnectionDto.FromProxyConnection(c, ServiceHost.Current.GetRuntimeConnectionState(c.ID)))
                .ToArray();

            s_log.Publish(
                MessageLevel.Info,
                "GetConnections",
                $"Returning {dtos.Length} connection(s). CorrelationId={correlationId}");

            return Ok(dtos);
        }

        // Extracts the X-Correlation-Id header value, or generates a new GUID string if absent.
        private string GetCorrelationId()
        {
            if (Request.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string> values))
                return values.FirstOrDefault() ?? Guid.NewGuid().ToString();

            return Guid.NewGuid().ToString();
        }

        #endregion
    }
}
