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
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor
{
    public sealed class GridAreaRemovedFromActorIntegrationEventParser : IGridAreaRemovedFromActorIntegrationEventParser
    {
        public byte[] Parse(GridAreaRemovedFromActorIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = new GridAreaRemovedFromActorIntegrationEventContract
                {
                    Id = integrationEvent.Id.ToString(),
                    EventCreated = Timestamp.FromDateTime(integrationEvent.EventCreated),
                    ActorId = integrationEvent.ActorId.ToString(),
                    OrganizationId = integrationEvent.OrganizationId.ToString(),
                    GridAreaId = integrationEvent.GridAreaId.ToString(),
                    MarketRoleFunction = (int)integrationEvent.Function,
                    GridAreaLinkId = integrationEvent.GridAreaLinkId.ToString(),
                    Type = integrationEvent.Type
                };

                return contract.ToByteArray();
            }
            catch (Exception e) when (e is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(GridAreaRemovedFromActorIntegrationEventContract)}", e);
            }
        }

        internal GridAreaRemovedFromActorIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = GridAreaRemovedFromActorIntegrationEventContract.Parser.ParseFrom(protoContract);

                var integrationEvent = new GridAreaRemovedFromActorIntegrationEvent(
                    Guid.Parse(contract.Id),
                    Guid.Parse(contract.ActorId),
                    Guid.Parse(contract.OrganizationId),
                    contract.EventCreated.ToDateTime(),
                    (EicFunction)contract.MarketRoleFunction,
                    Guid.Parse(contract.GridAreaId),
                    Guid.Parse(contract.GridAreaLinkId));

                if (integrationEvent.Type != contract.Type)
                {
                    throw new FormatException("Invalid Type");
                }

                return integrationEvent;
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(GridAreaRemovedFromActorIntegrationEvent)}", ex);
            }
        }
    }
}
