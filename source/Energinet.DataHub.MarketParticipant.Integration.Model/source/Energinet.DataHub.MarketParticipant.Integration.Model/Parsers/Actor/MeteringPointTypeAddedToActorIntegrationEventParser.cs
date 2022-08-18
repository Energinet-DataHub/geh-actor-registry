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
using Enum = System.Enum;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor
{
    public sealed class MeteringPointTypeAddedToActorIntegrationEventParser : IMeteringPointTypeAddedToActorIntegrationEventParser
    {
        public byte[] Parse(MeteringPointTypeAddedToActorIntegrationEvent addedToActorIntegrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(addedToActorIntegrationEvent, nameof(addedToActorIntegrationEvent));

                var contract = new MeteringPointTypeAddedToActorIntegrationEventContract
                {
                    Id = addedToActorIntegrationEvent.EventId.ToString(),
                    ActorId = addedToActorIntegrationEvent.ActorId.ToString(),
                    OrganizationId = addedToActorIntegrationEvent.OrganizationId.ToString(),
                    EventCreated = Timestamp.FromDateTime(addedToActorIntegrationEvent.EventCreated),
                    MarketRoleFunction = (int)addedToActorIntegrationEvent.Function,
                    GridAreaId = addedToActorIntegrationEvent.GridAreaId.ToString(),
                    MeteringPointType = addedToActorIntegrationEvent.MeteringPointType,
                    Type = addedToActorIntegrationEvent.Type
                };

                return contract.ToByteArray();
            }
            catch (Exception e) when (e is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(MeteringPointTypeAddedToActorIntegrationEventContract)}", e);
            }
        }

        internal static MeteringPointTypeAddedToActorIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = MeteringPointTypeAddedToActorIntegrationEventContract.Parser.ParseFrom(protoContract);

                return MapContract(contract);
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(ActorStatusChangedIntegrationEvent)}", ex);
            }
        }

        internal static MeteringPointTypeAddedToActorIntegrationEvent Parse(MeteringPointTypeAddedToActorIntegrationEventContract protoContract)
        {
            return MapContract(protoContract);
        }

        private static MeteringPointTypeAddedToActorIntegrationEvent MapContract(MeteringPointTypeAddedToActorIntegrationEventContract contract)
        {
            var integrationEvent = new MeteringPointTypeAddedToActorIntegrationEvent(
                Guid.Parse(contract.Id),
                Guid.Parse(contract.ActorId),
                Guid.Parse(contract.OrganizationId),
                Enum.IsDefined(typeof(EicFunction), contract.MarketRoleFunction) ? (EicFunction)contract.MarketRoleFunction : throw new FormatException(nameof(contract.MarketRoleFunction)),
                Guid.Parse(contract.GridAreaId),
                contract.EventCreated.ToDateTime(),
                contract.MeteringPointType);

            if (integrationEvent.Type != contract.Type)
            {
                throw new FormatException("Invalid Type");
            }

            return integrationEvent;
        }
    }
}
