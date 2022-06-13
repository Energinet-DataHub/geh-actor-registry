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

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents
{
    public sealed class ActorUpdatedIntegrationEvent : IntegrationEventBase
    {
        public Guid ActorId { get; set; }
        public OrganizationId OrganizationId { get; set; } = null!;
        public ExternalActorId? ExternalActorId { get; set; }
        public ActorNumber Gln { get; set; } = null!;
        public ActorStatus Status { get; set; }

        [JsonInclude]
        public ICollection<BusinessRoleCode> BusinessRoles { get; private set; } = new List<BusinessRoleCode>();

        [JsonInclude]
        public ICollection<EicFunction> MarketRoles { get; private set; } = new List<EicFunction>();

        [JsonInclude]
        public ICollection<GridAreaId> GridAreas { get; private set; } = new List<GridAreaId>();

        [JsonInclude]
        public ICollection<string> MeteringPointTypes { get; private set; } = new List<string>();
    }
}
