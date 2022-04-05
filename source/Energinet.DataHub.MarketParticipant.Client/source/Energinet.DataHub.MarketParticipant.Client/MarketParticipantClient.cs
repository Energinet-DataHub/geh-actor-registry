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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Flurl.Http;

namespace Energinet.DataHub.MarketParticipant.Client
{
    public sealed class MarketParticipantClient : IMarketParticipantClient
    {
        private readonly IMarketParticipantOrganizationClient _marketParticipantOrganizationClient;
        private readonly IMarketParticipantActorClient _marketParticipantActorClient;
        private readonly IMarketParticipantContactClient _marketParticipantContactClient;

        public MarketParticipantClient(IFlurlClient client)
        {
            _marketParticipantOrganizationClient = new MarketParticipantOrganizationClient(client);
            _marketParticipantActorClient = new MarketParticipantActorClient(client);
            _marketParticipantContactClient = new MarketParticipantContactClient(client);
        }

        public Task<IEnumerable<OrganizationDto>> GetOrganizationsAsync()
        {
            return _marketParticipantOrganizationClient.GetOrganizationsAsync();
        }

        public Task<OrganizationDto?> GetOrganizationAsync(Guid organizationId)
        {
            return _marketParticipantOrganizationClient.GetOrganizationAsync(organizationId);
        }

        public Task<Guid> CreateOrganizationAsync(ChangeOrganizationDto organizationDto)
        {
            return _marketParticipantOrganizationClient.CreateOrganizationAsync(organizationDto);
        }

        public Task UpdateOrganizationAsync(Guid organizationId, ChangeOrganizationDto organizationDto)
        {
            return _marketParticipantOrganizationClient.UpdateOrganizationAsync(organizationId, organizationDto);
        }

        public Task<IEnumerable<ActorDto>> GetActorsAsync(Guid organizationId)
        {
            return _marketParticipantActorClient.GetActorsAsync(organizationId);
        }

        public Task<ActorDto?> GetActorAsync(Guid organizationId, Guid actorId)
        {
            return _marketParticipantActorClient.GetActorAsync(organizationId, actorId);
        }

        public Task<Guid> CreateActorAsync(Guid organizationId, CreateActorDto createActorDto)
        {
            return _marketParticipantActorClient.CreateActorAsync(organizationId, createActorDto);
        }

        public Task UpdateActorAsync(Guid organizationId, Guid actorId, ChangeActorDto changeActorDto)
        {
            return _marketParticipantActorClient.UpdateActorAsync(organizationId, actorId, changeActorDto);
        }

        public Task<IEnumerable<ContactDto>> GetContactsAsync(Guid organizationId)
        {
            return _marketParticipantContactClient.GetContactsAsync(organizationId);
        }

        public Task<Guid> CreateContactAsync(Guid organizationId, CreateContactDto contactDto)
        {
            return _marketParticipantContactClient.CreateContactAsync(organizationId, contactDto);
        }

        public Task DeleteContactAsync(Guid organizationId, Guid contactId)
        {
            return _marketParticipantContactClient.DeleteContactAsync(organizationId, contactId);
        }
    }
}
