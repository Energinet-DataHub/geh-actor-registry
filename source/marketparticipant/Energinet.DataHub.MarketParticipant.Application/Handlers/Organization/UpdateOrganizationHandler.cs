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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organizations;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;

public sealed class UpdateOrganizationHandler : IRequestHandler<UpdateOrganizationCommand>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationExistsHelperService _organizationExistsHelperService;
    private readonly IUniqueOrganizationBusinessRegisterIdentifierRuleService _uniqueOrganizationBusinessRegisterIdentifierRuleService;

    public UpdateOrganizationHandler(
        IOrganizationRepository organizationRepository,
        IOrganizationExistsHelperService organizationExistsHelperService,
        IUniqueOrganizationBusinessRegisterIdentifierRuleService uniqueOrganizationBusinessRegisterIdentifierRuleService)
    {
        _organizationRepository = organizationRepository;
        _organizationExistsHelperService = organizationExistsHelperService;
        _uniqueOrganizationBusinessRegisterIdentifierRuleService = uniqueOrganizationBusinessRegisterIdentifierRuleService;
    }

    public async Task Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var organization = await _organizationExistsHelperService
            .EnsureOrganizationExistsAsync(request.OrganizationId)
            .ConfigureAwait(false);

        organization.Name = request.Organization.Name;
        organization.Status = Enum.Parse<OrganizationStatus>(request.Organization.Status, true);
        organization.Domain = new OrganizationDomain(request.Organization.Domain);

        await _uniqueOrganizationBusinessRegisterIdentifierRuleService
            .EnsureUniqueBusinessRegisterIdentifierAsync(organization)
            .ConfigureAwait(false);

        var result = await _organizationRepository
            .AddOrUpdateAsync(organization)
            .ConfigureAwait(false);

        result.ThrowOnError(OrganizationErrorHandler.HandleOrganizationError);
    }
}
