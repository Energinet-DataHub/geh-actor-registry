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
using System.Linq;
using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;
using Energinet.DataHub.MarketParticipant.Integration.Model.Exceptions;
using Energinet.DataHub.MarketParticipant.Integration.Model.Protobuf;
using Google.Protobuf;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers
{
    public sealed class ActorUpdatedIntegrationEventParser : IActorUpdatedIntegrationEventParser
    {
        public byte[] Parse(ActorUpdatedIntegrationEvent integrationEvent)
        {
            try
            {
                Guard.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = new ActorUpdatedIntegrationEventContract
                {
                    Id = integrationEvent.Id.ToString(),
                    ActorId = integrationEvent.ActorId.ToString(),
                    ExternalActorId = integrationEvent.ExternalActorId.ToString(),
                    OrganizationId = integrationEvent.OrganizationId.ToString(),
                    Gln = integrationEvent.Gln,
                    Status = (int)integrationEvent.Status,
                    GridAreaIds = { integrationEvent.GridAreas.Select(x => x.ToString()) }
                };

                foreach (var x in integrationEvent.BusinessRoles)
                {
                    contract.BusinessRoles.Add((int)x);
                }

                foreach (var x in integrationEvent.MarketRoles)
                {
                    contract.MarketRoles.Add((int)x);
                }

                return contract.ToByteArray();
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(ActorUpdatedIntegrationEvent)}", ex);
            }
        }

        internal ActorUpdatedIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = ActorUpdatedIntegrationEventContract.Parser.ParseFrom(protoContract);

                return new ActorUpdatedIntegrationEvent(
                    Guid.Parse(contract.Id),
                    Guid.Parse(contract.ActorId),
                    Guid.Parse(contract.OrganizationId),
                    Guid.Parse(contract.ExternalActorId),
                    contract.Gln,
                    Enum.IsDefined((ActorStatus)contract.Status) ? (ActorStatus)contract.Status : throw new FormatException(nameof(contract.Status)),
                    contract.BusinessRoles.Select(
                        x => Enum.IsDefined((BusinessRoleCode)x) ? (BusinessRoleCode)x : throw new FormatException(nameof(contract.BusinessRoles))).ToList(),
                    contract.MarketRoles.Select(
                        x => Enum.IsDefined((EicFunction)x) ? (EicFunction)x : throw new FormatException(nameof(contract.MarketRoles))).ToList(),
                    contract.GridAreaIds.Select(x => Guid.Parse(x)));
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array  {nameof(ActorUpdatedIntegrationEvent)}", ex);
            }
        }
    }
}
