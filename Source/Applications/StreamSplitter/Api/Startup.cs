//******************************************************************************************************
//  Startup.cs - Gbtc
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

using System.Web.Http;
using Newtonsoft.Json.Converters;
using Owin;
using Swashbuckle.Application;

namespace StreamSplitter.Api
{
    /// <summary>
    /// OWIN startup class for the Stream Splitter Web API.
    /// Configures attribute routing, JSON serialization, Swagger UI, and the Web API middleware.
    /// </summary>
    /// <remarks>
    /// This class is passed directly to <c>WebApp.Start&lt;Startup&gt;()</c> in
    /// <see cref="StreamSplitter.ServiceHost"/> — the <c>[assembly: OwinStartup]</c>
    /// auto-discovery attribute is intentionally omitted.
    /// </remarks>
    public class Startup
    {
        #region [ Methods ]

        /// <summary>
        /// Configures the OWIN middleware pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IAppBuilder"/> instance to configure.</param>
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();

            // Enable attribute-based routing ([RoutePrefix] / [Route] on controllers)
            config.MapHttpAttributeRoutes();

            // Serialize enum values as strings for readability in JSON responses
            config.Formatters.JsonFormatter.SerializerSettings.Converters
                .Add(new StringEnumConverter());

            // Remove XML formatter — this API speaks JSON only
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Configure Swashbuckle: single API version, enums as strings in schema
            config
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Stream Splitter API");
                    c.DescribeAllEnumsAsStrings();
                })
                .EnableSwaggerUi();

            app.UseWebApi(config);
        }

        #endregion
    }
}
