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
//  08/22/2026 - Eduardo Oliveira
//       Copilot Review follow-up: GetConnections/GetConnectionById/UpdateConnection now
//       read connections through ServiceHost's thread-safe snapshot accessors instead of enumerating
//       CurrentConfiguration directly. ImportFromFile enforces ServiceHost.MaxImportFileSizeBytes and
//       returns 413 when exceeded. UpdateConnection now uses ToLowerInvariant() for boolean settings.
//  09/02/2026 - Eduardo Oliveira
//       Copilot Review follow-up: ImportFromFile now reads the upload through
//       BoundedMultipartMemoryStreamProvider, which enforces MaxImportFileSizeBytes while the body is
//       being buffered - the prior Content-Length pre-check alone did not protect against a chunked
//       request that omits or misreports that header.
//
//******************************************************************************************************

using GSF;
using GSF.Diagnostics;
using StreamSplitter.Api;
using StreamSplitter.Api.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

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

        #endregion [ Members ]

        #region [ Methods ]

        /// <summary>
        /// Creates one or more proxy connections from a JSON array. All items are validated before
        /// any connection is created (all-or-nothing semantics).
        /// </summary>
        /// <param name="requests">
        /// Array of connection data. <c>connectionString</c> is required on each item.
        /// </param>
        /// <returns>
        /// 201 Created with the array of created <see cref="ConnectionDto"/> objects, or 400 Bad
        /// Request if any item fails validation.
        /// </returns>
        /// <remarks>
        /// The <c>connectionString</c> uses the GSF key=value pair format, with nested
        /// <c>sourceSettings</c> and <c>proxySettings</c> sub-strings. Example:
        /// <code>
        ///[
        ///{
        ///"connectionString": "name=PDC-001;enabled=true;sourceSettings={server=192.168.1.1;port=4712;phasorProtocol=IEEEC37_118V2;accessID=1};proxySettings={port=4713}"
        ///}
        ///]
        /// </code>
        /// </remarks>
        /// <response code="201">
        /// Array of created connections, each with a server-generated <c>id</c>.
        /// </response>
        /// <response code="400">One or more items are missing a required <c>connectionString</c>.</response>
        /// <response code="503">Service configuration not yet loaded.</response>
        [HttpPost, Route("")]
        [ResponseType(typeof(ConnectionDto[]))]
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
                    title = "Bad Request",
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
                        title = "Bad Request",
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
                        title = "Service Unavailable",
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
        /// Removes the proxy connection identified by <paramref name="id"/>, stops its data flow,
        /// and persists the updated configuration.
        /// </summary>
        /// <param name="id">Unique identifier of the connection to remove.</param>
        /// <returns>204 No Content on success; 404 if not found.</returns>
        /// <response code="204">Connection removed and data flow stopped.</response>
        /// <response code="404">No connection found with the given ID.</response>
        /// <response code="503">Service configuration not yet loaded.</response>
        [HttpDelete, Route("{id:guid}")]
        [ResponseType(typeof(void))]
        public IHttpActionResult DeleteConnection(Guid id)
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "DeleteConnection",
                $"DELETE /api/connections/{id} requested. CorrelationId={correlationId}");

            bool removed;

            try
            {
                removed = ServiceHost.Current.RemoveConnection(id);
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.ServiceUnavailable, new
                {
                    status = 503,
                    title = "Service Unavailable",
                    detail = ex.Message
                });
            }

            if (!removed)
            {
                s_log.Publish(
                    MessageLevel.Info,
                    "DeleteConnection",
                    $"Connection {id} not found. CorrelationId={correlationId}");

                return Content(HttpStatusCode.NotFound, new
                {
                    status = 404,
                    title = "Not Found",
                    detail = $"Connection with ID '{id}' was not found."
                });
            }

            s_log.Publish(
                MessageLevel.Info,
                "DeleteConnection",
                $"Connection {id} removed. CorrelationId={correlationId}");

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Returns the proxy connection identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">Unique identifier of the connection.</param>
        /// <returns>
        /// <see cref="ConnectionDto"/> when found; 404 with a problem detail body when not found.
        /// </returns>
        /// <response code="200">The requested connection with its current runtime state.</response>
        /// <response code="404">No connection found with the given ID.</response>
        [HttpGet, Route("{id:guid}")]
        [ResponseType(typeof(ConnectionDto))]
        public IHttpActionResult GetConnectionById(Guid id)
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "GetConnectionById",
                $"GET /api/connections/{id} requested. CorrelationId={correlationId}");

            // Obtained under ServiceHost's own lock so this cannot race with AddConnection/
            // RemoveConnection/configuration reload while the DTO is being built.
            ConnectionDto dto = ServiceHost.Current?.GetConnectionSnapshot(id);

            if (dto is null)
            {
                s_log.Publish(
                    MessageLevel.Info,
                    "GetConnectionById",
                    $"Connection {id} not found. CorrelationId={correlationId}");

                return Content(HttpStatusCode.NotFound, new
                {
                    status = 404,
                    title = "Not Found",
                    detail = $"Connection with ID '{id}' was not found."
                });
            }

            s_log.Publish(
                MessageLevel.Info,
                "GetConnectionById",
                $"Returning connection '{dto.Name}'. CorrelationId={correlationId}");

            return Ok(dto);
        }

        /// <summary>
        /// Returns all currently configured proxy connections.
        /// </summary>
        /// <returns>
        /// Array of <see cref="ConnectionDto"/> objects representing all configured connections.
        /// </returns>
        /// <response code="200">Array of connections. Empty array when no connections are configured.</response>
        [HttpGet, Route("")]
        [ResponseType(typeof(ConnectionDto[]))]
        public IHttpActionResult GetConnections()
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "GetConnections",
                $"GET /api/connections requested. CorrelationId={correlationId}");

            // Obtained under ServiceHost's own lock so this cannot race with AddConnection/
            // RemoveConnection/configuration reload while the collection is being enumerated.
            ConnectionDto[] dtos = ServiceHost.Current?.GetConnectionsSnapshot() ?? Array.Empty<ConnectionDto>();

            if (ServiceHost.Current?.CurrentConfiguration is null)
            {
                s_log.Publish(
                    MessageLevel.Warning,
                    "GetConnections",
                    $"Configuration not yet loaded. Returning empty list. CorrelationId={correlationId}");

                return Ok(dtos);
            }

            s_log.Publish(
                MessageLevel.Info,
                "GetConnections",
                $"Returning {dtos.Length} connection(s). CorrelationId={correlationId}");

            return Ok(dtos);
        }

        /// <summary>
        /// Returns the current operational status of the proxy connection identified by <paramref
        /// name="id"/> without retrieving its full configuration.
        /// </summary>
        /// <param name="id">Unique identifier of the connection.</param>
        /// <returns>200 OK with <see cref="ConnectionStatusDto"/>; 404 if not found.</returns>
        /// <response code="200">
        /// Current operational status including state, metrics, and recent messages.
        /// </response>
        /// <response code="404">No connection found with the given ID.</response>
        [HttpGet, Route("{id:guid}/status")]
        [ResponseType(typeof(ConnectionStatusDto))]
        public IHttpActionResult GetConnectionStatus(Guid id)
        {
            string correlationId = GetCorrelationId();

            s_log.Publish(
                MessageLevel.Info,
                "GetConnectionStatus",
                $"GET /api/connections/{id}/status requested. CorrelationId={correlationId}");

            ConnectionStatusDto status = ServiceHost.Current?.GetConnectionStatus(id);

            if (status is null)
            {
                s_log.Publish(
                    MessageLevel.Info,
                    "GetConnectionStatus",
                    $"Connection {id} not found. CorrelationId={correlationId}");

                return Content(HttpStatusCode.NotFound, new
                {
                    status = 404,
                    title = "Not Found",
                    detail = $"Connection with ID '{id}' was not found."
                });
            }

            return Ok(status);
        }

        /// <summary>
        /// Imports connections from a <c>.s3config</c> file sent as multipart/form-data. Each
        /// connection in the file is assigned a new server-generated ID to ensure CREATE semantics
        /// regardless of the IDs in the file.
        /// </summary>
        /// <returns>
        /// 201 Created with the array of imported <see cref="ConnectionDto"/> objects, or 400 Bad
        /// Request if the file is missing, empty, or not a valid .s3config.
        /// </returns>
        /// <remarks>
        /// Upload a <c>.s3config</c> file exported from <c>StreamSplitterManager</c> using File →
        /// Save Configuration. The file is a SOAP-serialized <c>ProxyConnectionCollection</c>. All
        /// connections in the file are created with new server-generated IDs.
        /// </remarks>
        /// <response code="201">
        /// Array of imported connections, each with a new server-generated <c>id</c>.
        /// </response>
        /// <response code="400">File missing, empty, or not a valid .s3config.</response>
        /// <response code="413">Uploaded file exceeds the configured maximum size ( <c>MaxImportFileSizeBytes</c>).</response>
        [HttpPost, Route("import")]
        [ResponseType(typeof(ConnectionDto[]))]
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
                    title = "Bad Request",
                    detail = "Expected multipart/form-data with a .s3config file."
                });
            }

            // Reject oversized uploads before buffering the multipart body in memory.
            int maxImportFileSizeBytes = ServiceHost.MaxImportFileSizeBytes;
            long? contentLength = Request.Content.Headers.ContentLength;

            if (ExceedsMaxImportSize(contentLength, maxImportFileSizeBytes))
            {
                s_log.Publish(
                    MessageLevel.Warning,
                    "ImportFromFile",
                    $"Upload rejected: Content-Length {contentLength} exceeds the maximum allowed size of {maxImportFileSizeBytes} bytes. CorrelationId={correlationId}");

                return Content(HttpStatusCode.RequestEntityTooLarge, new
                {
                    status = 413,
                    title = "Payload Too Large",
                    detail = $"The uploaded file exceeds the maximum allowed size of {maxImportFileSizeBytes} bytes."
                });
            }

            // Bounded even when the client omits or lies about Content-Length (e.g. chunked
            // transfer): the provider rejects the upload mid-read instead of buffering it in full.
            BoundedMultipartMemoryStreamProvider provider = new BoundedMultipartMemoryStreamProvider(maxImportFileSizeBytes);

            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);
            }
            catch (IOException ex) when (ex is MaxLengthExceededException || ex.InnerException is MaxLengthExceededException)
            {
                s_log.Publish(
                    MessageLevel.Warning,
                    "ImportFromFile",
                    $"Upload rejected: content exceeded the maximum allowed size of {maxImportFileSizeBytes} bytes while reading the request body. CorrelationId={correlationId}");

                return Content(HttpStatusCode.RequestEntityTooLarge, new
                {
                    status = 413,
                    title = "Payload Too Large",
                    detail = $"The uploaded file exceeds the maximum allowed size of {maxImportFileSizeBytes} bytes."
                });
            }

            HttpContent filePart = provider.Contents.FirstOrDefault();

            if (filePart is null)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title = "Bad Request",
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
                    title = "Bad Request",
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
                    title = "Bad Request",
                    detail = "The file could not be read as a valid .s3config configuration."
                });
            }

            if (imported.Count == 0)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    status = 400,
                    title = "Bad Request",
                    detail = "The .s3config file contains no connections."
                });
            }

            List<ConnectionDto> created = new List<ConnectionDto>();

            foreach (ProxyConnection source in imported)
            {
                // Assign a new ID to guarantee CREATE semantics — never UPDATE an existing connection.
                ProxyConnection connection = new ProxyConnection
                {
                    ConnectionString = source.ConnectionString,
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
        /// Partially updates a proxy connection. Only the fields present in the request body are
        /// changed; omitted fields retain their current values. The connection is automatically
        /// restarted after the update.
        /// </summary>
        /// <param name="id">Unique identifier of the connection to update.</param>
        /// <param name="request">Fields to update. All properties are optional.</param>
        /// <returns>200 OK with the updated <see cref="ConnectionDto"/>, or 404 if not found.</returns>
        /// <remarks>
        /// Send only the fields that need to change; all others are preserved.
        /// <para>Disable a connection:</para>
        /// <code>{ "enabled": false }</code>
        /// <para>Rename a connection:</para>
        /// <code>{ "name": "PDC-001-updated" }</code>
        /// <para>Update the source settings:</para>
        /// <code>{ "sourceSettings": "server=192.168.1.2;port=4712;phasorProtocol=IEEEC37_118V2;accessID=1" }</code>
        /// <para>
        /// Each field is independent — you can combine any of them in the same request. For
        /// <c>sourceSettings</c> and <c>proxySettings</c>, sending an empty string removes the sub-string.
        /// </para>
        /// </remarks>
        /// <response code="200">Updated connection with its new runtime state.</response>
        /// <response code="400">Request body is missing.</response>
        /// <response code="404">No connection found with the given ID.</response>
        /// <response code="503">Service configuration not yet loaded.</response>
        [HttpPatch, Route("{id:guid}")]
        [ResponseType(typeof(ConnectionDto))]
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
                    title = "Bad Request",
                    detail = "Request body is required."
                });
            }

            // Obtained under ServiceHost's own lock to avoid reading a connection that is
            // concurrently being added/removed/updated.
            ProxyConnection existing = ServiceHost.Current?.GetConfiguredConnection(id);

            if (existing is null)
            {
                s_log.Publish(
                    MessageLevel.Info,
                    "UpdateConnection",
                    $"Connection {id} not found. CorrelationId={correlationId}");

                return Content(HttpStatusCode.NotFound, new
                {
                    status = 404,
                    title = "Not Found",
                    detail = $"Connection with ID '{id}' was not found."
                });
            }

            // Merge only the provided sub-fields into the existing connection string. Omitted
            // fields (null) retain their current values.
            string updatedConnectionString = MergeConnectionString(existing.ConnectionString, request);

            ProxyConnection updated = new ProxyConnection
            {
                ConnectionString = updatedConnectionString,
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
                    title = "Service Unavailable",
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

        // Determines whether the given Content-Length exceeds the configured maximum import file
        // size. A null Content-Length is not rejected here; ImportFromFile still guards against an
        // empty stream once the body has been read.
        internal static bool ExceedsMaxImportSize(long? contentLength, int maxSizeBytes)
        {
            return contentLength.HasValue && contentLength.Value > maxSizeBytes;
        }

        // Merges only the provided sub-fields of `request` into `existingConnectionString`. Omitted
        // fields (null) retain their current value; sending an empty string for sourceSettings/
        // proxySettings removes that sub-string entirely. Extracted from UpdateConnection as a pure
        // function so the partial-update/no-overwrite semantics (Task 7.4.8.8) can be unit tested
        // without a live ServiceHost.
        internal static string MergeConnectionString(string existingConnectionString, UpdateConnectionRequest request)
        {
            Dictionary<string, string> settings = existingConnectionString.ParseKeyValuePairs();

            if (request.Name != null)
                settings["name"] = request.Name;

            if (request.Enabled.HasValue)
                settings["enabled"] = request.Enabled.Value.ToString().ToLowerInvariant();

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

            return settings.JoinKeyValuePairs();
        }

        // Extracts the X-Correlation-Id header value, or generates a new GUID string if absent.
        private string GetCorrelationId()
        {
            if (Request.Headers.TryGetValues("X-Correlation-Id", out IEnumerable<string> values))
                return values.FirstOrDefault() ?? Guid.NewGuid().ToString();

            return Guid.NewGuid().ToString();
        }

        #endregion [ Methods ]
    }
}