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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using Energinet.DataHub.MarketParticipant.Utilities;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class OrganizationFactoryService : IOrganizationFactoryService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IOrganizationIntegrationEventsQueueService _organizationIntegrationEventsQueueService;

        public OrganizationFactoryService(
            IOrganizationRepository organizationRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IOrganizationIntegrationEventsQueueService organizationIntegrationEventsQueueService)
        {
            _organizationRepository = organizationRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _organizationIntegrationEventsQueueService = organizationIntegrationEventsQueueService;
        }

        public async Task<Organization> CreateAsync(
            string name,
            BusinessRegisterIdentifier businessRegisterIdentifier,
            Address address)
        {
            Guard.ThrowIfNull(name, nameof(name));
            Guard.ThrowIfNull(businessRegisterIdentifier, nameof(businessRegisterIdentifier));
            Guard.ThrowIfNull(address, nameof(address));

            var newOrganization = new Organization(name, businessRegisterIdentifier, address);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            var savedOrganization = await SaveOrganizationAsync(newOrganization).ConfigureAwait(false);

            await _organizationIntegrationEventsQueueService
                .EnqueueOrganizationUpdatedEventAsync(savedOrganization)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return savedOrganization;
        }

        private async Task<Organization> SaveOrganizationAsync(Organization organization)
        {
            await _organizationRepository
                .AddOrUpdateAsync(organization)
                .ConfigureAwait(false);

            var savedOrganization = await _organizationRepository
                .GetAsync(organization.Id)
                .ConfigureAwait(false);

            return savedOrganization!;
        }
    }
}
