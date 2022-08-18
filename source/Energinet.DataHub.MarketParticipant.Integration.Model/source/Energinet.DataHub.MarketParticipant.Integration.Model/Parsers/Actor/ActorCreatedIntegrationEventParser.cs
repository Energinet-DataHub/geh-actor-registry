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
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers.Actor
{
    public sealed class ActorCreatedIntegrationEventParser : IActorCreatedIntegrationEventParser
    {
        public byte[] Parse(ActorCreatedIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = new SharedIntegrationEventContract();
                contract.ActorCreatedIntegrationEvent = new ActorCreatedIntegrationEventContract
                {
                    Id = integrationEvent.Id.ToString(),
                    ActorId = integrationEvent.ActorId.ToString(),
                    OrganizationId = integrationEvent.OrganizationId.ToString(),
                    Status = (int)integrationEvent.Status,
                    ActorNumber = integrationEvent.ActorNumber,
                    Name = integrationEvent.Name,
                    ActorMarketRoles =
                    {
                        integrationEvent.ActorMarketRoles.Select(x => new ActorMarketRoleEventData
                        {
                            Function = (int)x.Function,
                            GridAreas =
                            {
                                x.GridAreas.Select(g => new ActorGridAreaEventData
                                {
                                    Id = g.Id.ToString(),
                                    MeteringPointTypes =
                                    {
                                        g.MeteringPointTypes
                                    }
                                })
                            }
                        })
                    },
                    EventCreated = Timestamp.FromDateTime(integrationEvent.EventCreated),
                    Type = integrationEvent.Type
                };
                foreach (var x in integrationEvent.BusinessRoles)
                {
                    contract.ActorCreatedIntegrationEvent.BusinessRoles.Add((int)x);
                }

                return contract.ToByteArray();
            }
            catch (Exception e) when (e is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(ActorCreatedIntegrationEventContract)}", e);
            }
        }

        internal static ActorCreatedIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = ActorCreatedIntegrationEventContract.Parser.ParseFrom(protoContract);

                return MapContract(contract);
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(ActorCreatedIntegrationEvent)}", ex);
            }
        }

        internal static ActorCreatedIntegrationEvent Parse(ActorCreatedIntegrationEventContract protoContract)
        {
            return MapContract(protoContract);
        }

        private static ActorCreatedIntegrationEvent MapContract(ActorCreatedIntegrationEventContract contract)
        {
            var integrationEvent = new ActorCreatedIntegrationEvent(
                Guid.Parse(contract.Id),
                Guid.Parse(contract.ActorId),
                Guid.Parse(contract.OrganizationId),
                Enum.IsDefined(typeof(ActorStatus), contract.Status) ? (ActorStatus)contract.Status : throw new FormatException(nameof(contract.Status)),
                contract.ActorNumber,
                contract.Name,
                contract.BusinessRoles
                    .Select(c => Enum.IsDefined(typeof(BusinessRoleCode), c) ? (BusinessRoleCode)c : throw new FormatException(nameof(contract.BusinessRoles)))
                    .ToList(),
                contract.ActorMarketRoles
                    .Select(x => new Dtos.ActorMarketRole(ParseOrThrowOnMarketRole().Invoke(x.Function), x.GridAreas.Select(
                        g => new Dtos.ActorGridArea(Guid.Parse(g.Id), g.MeteringPointTypes))))
                    .ToList(),
                contract.EventCreated.ToDateTime());

            if (integrationEvent.Type != contract.Type)
            {
                throw new FormatException("Invalid Type");
            }

            return integrationEvent;
        }

        private static Func<int, EicFunction> ParseOrThrowOnMarketRole() => i =>
            Enum.IsDefined(typeof(EicFunction), i) ? (EicFunction)i : throw new FormatException(nameof(EicFunction));
    }
}
