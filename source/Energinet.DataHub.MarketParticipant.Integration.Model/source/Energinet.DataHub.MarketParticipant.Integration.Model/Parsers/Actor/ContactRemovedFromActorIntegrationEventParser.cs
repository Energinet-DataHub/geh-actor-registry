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
    public sealed class ContactRemovedFromActorIntegrationEventParser : IContactRemovedFromActorIntegrationEventParser
    {
        public byte[] Parse(ContactRemovedFromActorIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));

                var contract = MapEvent(integrationEvent);

                return contract.ToByteArray();
            }
            catch (Exception e) when (e is InvalidProtocolBufferException)
            {
                throw new MarketParticipantException($"Error parsing {nameof(ContactRemovedFromActorIntegrationEventContract)}", e);
            }
        }

        public byte[] ParseToSharedIntegrationEvent(ContactRemovedFromActorIntegrationEvent integrationEvent)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(integrationEvent, nameof(integrationEvent));
                var eventContract = MapEvent(integrationEvent);
                var contract = new SharedIntegrationEventContract { ContactRemovedFromActorIntegrationEvent = eventContract };
                return contract.ToByteArray();
            }
            catch (Exception ex)
            {
                throw new MarketParticipantException($"Error parsing {nameof(ActorUpdatedIntegrationEvent)}", ex);
            }
        }

        internal static ContactRemovedFromActorIntegrationEvent Parse(byte[] protoContract)
        {
            try
            {
                var contract = ContactRemovedFromActorIntegrationEventContract.Parser.ParseFrom(protoContract);

                return MapContract(contract);
            }
            catch (Exception ex) when (ex is InvalidProtocolBufferException or FormatException)
            {
                throw new MarketParticipantException($"Error parsing byte array for {nameof(ContactRemovedFromActorIntegrationEvent)}", ex);
            }
        }

        internal static ContactRemovedFromActorIntegrationEvent Parse(ContactRemovedFromActorIntegrationEventContract protoContract)
        {
            return MapContract(protoContract);
        }

        private static ContactRemovedFromActorIntegrationEvent MapContract(ContactRemovedFromActorIntegrationEventContract contract)
        {
            var integrationEvent = new ContactRemovedFromActorIntegrationEvent(
                Guid.Parse(contract.Id),
                Guid.Parse(contract.ActorId),
                Guid.Parse(contract.OrganizationId),
                contract.EventCreated.ToDateTime(),
                new ActorContact(
                    contract.Contact.Name,
                    contract.Contact.Email,
                    Enum.IsDefined(typeof(ContactCategory), contract.Contact.Category) ? (ContactCategory)contract.Contact.Category : throw new FormatException(nameof(contract.Contact.Category)),
                    contract.Contact.HasPhone ? contract.Contact.Phone : null));

            if (integrationEvent.Type != contract.Type)
            {
                throw new FormatException("Invalid Type");
            }

            return integrationEvent;
        }

        private static ContactRemovedFromActorIntegrationEventContract MapEvent(
            ContactRemovedFromActorIntegrationEvent integrationEvent)
        {
            var contract = new ContactRemovedFromActorIntegrationEventContract
            {
                Id = integrationEvent.Id.ToString(),
                EventCreated = Timestamp.FromDateTime(integrationEvent.EventCreated),
                ActorId = integrationEvent.ActorId.ToString(),
                OrganizationId = integrationEvent.OrganizationId.ToString(),
                Contact = new ActorContactEventData
                {
                    Name = integrationEvent.Contact.Name,
                    Email = integrationEvent.Contact.Email,
                    Category = (int)integrationEvent.Contact.Category
                },
                Type = integrationEvent.Type
            };

            if (integrationEvent.Contact.Phone != null)
            {
                contract.Contact.Phone = integrationEvent.Contact.Phone;
            }

            return contract;
        }
    }
}
