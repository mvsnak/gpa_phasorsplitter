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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using GSF;
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

        /// <summary>
        /// Returns the proxy connection identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Unique identifier of the connection.</param>
        /// <returns>
        /// <see cref="ConnectionDto"/> when found; 404 with a problem detail body when not found.
        /// </returns>
        [HttpGet, Route("{id:guid}")]
        public IHttpActionResult GetConnectionById(Guid id)
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "GetConnectionById",
                $"GET /api/connections/{id} requested. CorrelationId={correlationId}");

            ProxyConnectionCollection configuration = ServiceHost.Current?.CurrentConfiguration;
            ProxyConnection connection = configuration?[id];

            if (connection is null)
            {
                s_log.Publish(
                    MessageLevel.Info,
                    "GetConnectionById",
                    $"Connection {id} not found. CorrelationId={correlationId}");

                return Content(HttpStatusCode.NotFound, new
                {
                    status = 404,
                    title  = "Not Found",
                    detail = $"Connection with ID '{id}' was not found."
                });
            }

            ConnectionDto dto = ConnectionDto.FromProxyConnection(
                connection,
                ServiceHost.Current.GetRuntimeConnectionState(id));

            s_log.Publish(
                MessageLevel.Info,
                "GetConnectionById",
                $"Returning connection '{connection.Name}'. CorrelationId={correlationId}");

            return Ok(dto);
        }

        /// <summary>
        /// Creates one or more proxy connections from a JSON array.
        /// All items are validated before any connection is created (all-or-nothing semantics).
        /// </summary>
        /// <param name="requests">
        /// Array of connection data. <c>connectionString</c> is required on each item.
        /// </param>
        /// <returns>
        /// 201 Created with the array of created <see cref="ConnectionDto"/> objects,
        /// or 400 Bad Request if any item fails validation.
        /// </returns>
        [HttpPost, Route("")]
        public IHttpActionResult CreateConnections([FromBody] CreateConnectionRequest[] requests)
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "CreateConnections",
                $"POST /api/connections requested ({requests?.Length ?? 0} item(s)). CorrelationId={correlationId}");

            if (requests is null || requests.Length == 0)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "The request body must be a non-empty JSON array of connection objects."
                });
            }

            // Validate all items before creating any — all-or-nothing semantics.
            for (int i = 0; i < requests.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(requests[i]?.ConnectionString))
                {
                    s_log.Publish(
                        MessageLevel.Warning,
                        "CreateConnections",
                        $"Validation failed at index {i}: connectionString is required. CorrelationId={correlationId}");

                    return Content(HttpStatusCode.BadRequest, new
                    {
                        status = 400,
                        title  = "Bad Request",
                        detail = $"Item at index {i} is missing a required connectionString."
                    });
                }
            }

            List<ConnectionDto> created = new List<ConnectionDto>();

            foreach (CreateConnectionRequest request in requests)
            {
                ProxyConnection connection = new ProxyConnection { ConnectionString = request.ConnectionString };

                try
                {
                    ServiceHost.Current.AddConnection(connection);
                }
                catch (InvalidOperationException ex)
                {
                    return Content(HttpStatusCode.ServiceUnavailable, new
                    {
                        status = 503,
                        title  = "Service Unavailable",
                        detail = ex.Message
                    });
                }

                created.Add(ConnectionDto.FromProxyConnection(
                    connection,
                    ServiceHost.Current.GetRuntimeConnectionState(connection.ID)));

                s_log.Publish(
                    MessageLevel.Info,
                    "CreateConnections",
                    $"Connection '{connection.Name}' created. Id={connection.ID}. CorrelationId={correlationId}");
            }

            return Created("/api/connections", created.ToArray());
        }

        /// <summary>
        /// Imports connections from a <c>.s3config</c> file sent as multipart/form-data.
        /// Each connection in the file is assigned a new server-generated ID to ensure
        /// CREATE semantics regardless of the IDs in the file.
        /// </summary>
        /// <returns>
        /// 201 Created with the array of imported <see cref="ConnectionDto"/> objects,
        /// or 400 Bad Request if the file is missing, empty, or not a valid .s3config.
        /// </returns>
        [HttpPost, Route("import")]
        public async Task<IHttpActionResult> ImportFromFile()
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "ImportFromFile",
                $"POST /api/connections/import requested. CorrelationId={correlationId}");

            if (!Request.Content.IsMimeMultipartContent())
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "Expected multipart/form-data with a .s3config file."
                });
            }

            MultipartMemoryStreamProvider provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);

            HttpContent filePart = provider.Contents.FirstOrDefault();

            if (filePart is null)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "No file was included in the request."
                });
            }

            // Read the part as a stream to avoid a redundant byte[] copy — the
            // MultipartMemoryStreamProvider already buffers content in a MemoryStream.
            using Stream fileStream = await filePart.ReadAsStreamAsync();

            if (fileStream.Length == 0)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "The uploaded file is empty."
                });
            }

            ProxyConnectionCollection imported;

            try
            {
                imported = ProxyConnectionCollection.DeserializeConfiguration(fileStream);
            }
            catch (Exception ex)
            {
                s_log.Publish(
                    MessageLevel.Warning,
                    "ImportFromFile",
                    $"Failed to deserialize file. Error={ex.Message}. CorrelationId={correlationId}");

                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "The file could not be read as a valid .s3config configuration."
                });
            }

            if (imported.Count == 0)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "The .s3config file contains no connections."
                });
            }

            List<ConnectionDto> created = new List<ConnectionDto>();

            foreach (ProxyConnection source in imported)
            {
                // Assign a new ID to guarantee CREATE semantics — never UPDATE an existing connection.
                ProxyConnection connection = new ProxyConnection
                {
                    ConnectionString     = source.ConnectionString,
                    ConnectionParameters = source.ConnectionParameters
                };

                ServiceHost.Current.AddConnection(connection);

                created.Add(ConnectionDto.FromProxyConnection(
                    connection,
                    ServiceHost.Current.GetRuntimeConnectionState(connection.ID)));
            }

            s_log.Publish(
                MessageLevel.Info,
                "ImportFromFile",
                $"Imported {created.Count} connection(s). CorrelationId={correlationId}");

            return Created("/api/connections", created.ToArray());
        }

        /// <summary>
        /// Partially updates a proxy connection. Only the fields present in the request body
        /// are changed; omitted fields retain their current values. The connection is
        /// automatically restarted after the update.
        /// </summary>
        /// <param name="id">Unique identifier of the connection to update.</param>
        /// <param name="request">Fields to update. All properties are optional.</param>
        /// <returns>
        /// 200 OK with the updated <see cref="ConnectionDto"/>, or 404 if not found.
        /// </returns>
        [HttpPatch, Route("{id:guid}")]
        public IHttpActionResult UpdateConnection(Guid id, [FromBody] UpdateConnectionRequest request)
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "UpdateConnection",
                $"PATCH /api/connections/{id} requested. CorrelationId={correlationId}");

            if (request is null)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title  = "Bad Request",
                    detail = "Request body is required."
                });
            }

            ProxyConnectionCollection configuration = ServiceHost.Current?.CurrentConfiguration;
            ProxyConnection existing = configuration?[id];

            if (existing is null)
            {
                s_log.Publish(
                    MessageLevel.Info,
                    "UpdateConnection",
                    $"Connection {id} not found. CorrelationId={correlationId}");

                return Content(HttpStatusCode.NotFound, new
                {
                    status = 404,
                    title  = "Not Found",
                    detail = $"Connection with ID '{id}' was not found."
                });
            }

            // Build the updated connection string from the partial request.
            string updatedConnectionString;

            if (!string.IsNullOrEmpty(request.ConnectionString))
            {
                // Consumer provided a complete connection string — use it directly.
                updatedConnectionString = request.ConnectionString;
            }
            else
            {
                // Consumer provided only sub-fields — merge into the existing connection string.
                Dictionary<string, string> settings = existing.ConnectionString.ParseKeyValuePairs();

                if (request.Name != null)
                    settings["name"] = request.Name;

                if (request.Enabled.HasValue)
                    settings["enabled"] = request.Enabled.Value.ToString().ToLower();

                if (request.SourceSettings != null)
                {
                    if (string.IsNullOrEmpty(request.SourceSettings))
                        settings.Remove("sourceSettings");
                    else
                        settings["sourceSettings"] = request.SourceSettings;
                }

                if (request.ProxySettings != null)
                {
                    if (string.IsNullOrEmpty(request.ProxySettings))
                        settings.Remove("proxySettings");
                    else
                        settings["proxySettings"] = request.ProxySettings;
                }

                updatedConnectionString = settings.JoinKeyValuePairs();
            }

            ProxyConnection updated = new ProxyConnection
            {
                ConnectionString     = updatedConnectionString,
                ConnectionParameters = existing.ConnectionParameters
            };

            updated.ID = existing.ID;

            try
            {
                ServiceHost.Current.AddConnection(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.ServiceUnavailable, new
                {
                    status = 503,
                    title  = "Service Unavailable",
                    detail = ex.Message
                });
            }

            ConnectionDto dto = ConnectionDto.FromProxyConnection(
                updated,
                ServiceHost.Current.GetRuntimeConnectionState(id));

            s_log.Publish(
                MessageLevel.Info,
                "UpdateConnection",
                $"Connection '{updated.Name}' updated. Id={id}. CorrelationId={correlationId}");

            return Ok(dto);
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
