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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.IntegrationEvents;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class OrganizationIntegrationEventsQueueService : IOrganizationIntegrationEventsQueueService
    {
        private readonly IDomainEventRepository _domainEventRepository;

        public OrganizationIntegrationEventsQueueService(
            IDomainEventRepository domainEventRepository)
        {
            _domainEventRepository = domainEventRepository;
        }

        public Task EnqueueOrganizationUpdatedEventAsync(Organization organization)
        {
            Guard.ThrowIfNull(organization, nameof(organization));

            var organizationUpdatedEvent = new OrganizationUpdatedIntegrationEvent
            {
                Address = organization.Address,
                Name = organization.Name,
                OrganizationId = organization.Id,
                BusinessRegisterIdentifier = organization.BusinessRegisterIdentifier
            };

            var domainEvent = new DomainEvent(organization.Id.Value, nameof(Organization), organizationUpdatedEvent);
            return _domainEventRepository.InsertAsync(domainEvent);
        }
    }
}
