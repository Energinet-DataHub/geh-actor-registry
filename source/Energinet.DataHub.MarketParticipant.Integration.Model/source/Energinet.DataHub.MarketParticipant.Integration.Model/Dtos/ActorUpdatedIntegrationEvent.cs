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

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Dtos
{
    public sealed record ActorUpdatedIntegrationEvent
    {
        /// <summary>
        /// An event representing an update to a given actor.
        /// </summary>
        /// <param name="id">Unique integration event ID.</param>
        /// <param name="actorId">The internal actor ID.</param>
        /// <param name="organizationId">Organization ID.</param>
        /// <param name="externalActorId">The external actor ID for integrating Azure AD and domains.</param>
        /// <param name="gln">GLN.</param>
        /// <param name="status">The status of the current actor.</param>
        /// <param name="businessRoles">The ebIX roles assigned to the actor.</param>
        /// <param name="marketRoles">The roles (functions) assigned to the current actor.</param>
        /// <param name="gridAreas">The roles (grid areas) assigned to the current actor.</param>
        public ActorUpdatedIntegrationEvent(
            Guid id,
            Guid actorId,
            Guid organizationId,
            Guid externalActorId,
            string gln,
            ActorStatus status,
            IEnumerable<BusinessRoleCode> businessRoles,
            IEnumerable<EicFunction> marketRoles,
            IEnumerable<Guid> gridAreas)
        {
            Id = id;
            ActorId = actorId;
            OrganizationId = organizationId;
            ExternalActorId = externalActorId;
            Gln = gln;
            Status = status;
            BusinessRoles = businessRoles;
            MarketRoles = marketRoles;
            GridAreas = gridAreas;
        }

        public Guid Id { get; }
        public Guid ActorId { get; }
        public Guid OrganizationId { get; }

        public Guid ExternalActorId { get; }
        public string Gln { get; }
        public ActorStatus Status { get; }

        public IEnumerable<BusinessRoleCode> BusinessRoles { get; }
        public IEnumerable<EicFunction> MarketRoles { get; }
        public IEnumerable<Guid> GridAreas { get; }
    }
}
