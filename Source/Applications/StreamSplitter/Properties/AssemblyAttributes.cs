//******************************************************************************************************
//  AssemblyAttributes.cs - Gbtc
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
//       Copilot Review follow-up (Story 7.4.8): added to expose internal members (ServiceHost,
//       ConnectionsController) to the new StreamSplitter.Api.Tests unit test project. Kept separate
//       from AssemblyInfo.cs, which is owned by the build for versioning.
//
//******************************************************************************************************

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StreamSplitter.Api.Tests")]
