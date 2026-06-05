//******************************************************************************************************
//  FileUploadOperationFilter.cs - Gbtc
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

using System.Collections.Generic;
using System.Web.Http.Description;
using Swashbuckle.Swagger;

namespace StreamSplitter.Api.Filters
{
    /// <summary>
    /// Swashbuckle operation filter that replaces the body parameter on the
    /// <c>ImportFromFile</c> action with a multipart/form-data file picker,
    /// enabling file upload directly from the Swagger UI.
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        #region [ Methods ]

        /// <inheritdoc />
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            bool isImport =
                apiDescription.ActionDescriptor.ControllerDescriptor.ControllerName == "Connections"
                && apiDescription.ActionDescriptor.ActionName == "ImportFromFile";

            if (!isImport)
                return;

            operation.consumes = new List<string> { "multipart/form-data" };
            operation.parameters = new List<Parameter>
            {
                new Parameter
                {
                    name        = "file",
                    @in         = "formData",
                    required    = true,
                    type        = "file",
                    description = "Configuration file (.s3config) exported from StreamSplitterManager."
                }
            };
        }

        #endregion
    }
}
