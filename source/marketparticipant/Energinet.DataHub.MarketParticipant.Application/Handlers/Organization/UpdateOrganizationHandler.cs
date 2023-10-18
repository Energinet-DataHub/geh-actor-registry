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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Organization
{
    public sealed class UpdateOrganizationHandler : IRequestHandler<UpdateOrganizationCommand>
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationExistsHelperService _organizationExistsHelperService;
        private readonly IUniqueOrganizationBusinessRegisterIdentifierService _uniqueOrganizationBusinessRegisterIdentifierService;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;

        public UpdateOrganizationHandler(
            IOrganizationRepository organizationRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IOrganizationExistsHelperService organizationExistsHelperService,
            IUniqueOrganizationBusinessRegisterIdentifierService uniqueOrganizationBusinessRegisterIdentifierService)
        {
            _organizationRepository = organizationRepository;
            _organizationExistsHelperService = organizationExistsHelperService;
            _uniqueOrganizationBusinessRegisterIdentifierService = uniqueOrganizationBusinessRegisterIdentifierService;
            _unitOfWorkProvider = unitOfWorkProvider;
        }

        public async Task Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var organization = await _organizationExistsHelperService
                .EnsureOrganizationExistsAsync(request.OrganizationId)
                .ConfigureAwait(false);

            organization.Name = request.Organization.Name;
            organization.BusinessRegisterIdentifier = new BusinessRegisterIdentifier(request.Organization.BusinessRegisterIdentifier);
            organization.Address = new Address(
                request.Organization.Address.StreetName,
                request.Organization.Address.Number,
                request.Organization.Address.ZipCode,
                request.Organization.Address.City,
                request.Organization.Address.Country);
            organization.Comment = request.Organization.Comment;
            organization.Status = Enum.Parse<OrganizationStatus>(request.Organization.Status, true);
            organization.Domain = new OrganizationDomain(request.Organization.Domain);

            await _uniqueOrganizationBusinessRegisterIdentifierService
                .EnsureUniqueBusinessRegisterIdentifierAsync(organization)
                .ConfigureAwait(false);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                await _organizationRepository
                    .AddOrUpdateAsync(organization)
                    .ConfigureAwait(false);

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
