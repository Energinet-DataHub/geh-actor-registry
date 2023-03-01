﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using Energinet.DataHub.Core.App.Common.Security;

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class PermissionDetails
    {
        public PermissionDetails(
            Permission permission,
            string description,
            IEnumerable<EicFunction> eicFunctions)
        {
            Permission = permission;
            Description = description;
            EicFunctions = eicFunctions;
        }

        public Permission Permission { get; }
        public string Description { get; set; }
        public IEnumerable<EicFunction> EicFunctions { get; }
    }
}
