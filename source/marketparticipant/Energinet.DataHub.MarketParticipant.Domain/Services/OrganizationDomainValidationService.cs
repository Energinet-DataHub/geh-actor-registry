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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class OrganizationDomainValidationService : IOrganizationDomainValidationService
{
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationDomainValidationService(IOrganizationRepository organizationRepository)
    {
        _organizationRepository = organizationRepository;
    }

    public async Task ValidateUserEmailInsideOrganizationDomainsAsync(Actor actor, EmailAddress userInviteEmail)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(userInviteEmail);

        var organization = await _organizationRepository
            .GetAsync(actor.OrganizationId)
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(
            organization,
            actor.OrganizationId.Value,
            $"The specified organization {actor.OrganizationId} was not found.");

        if (organization.Domains.All(d => !userInviteEmail.Address.EndsWith("@" + d.Value, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException("User email not valid, should match organization domains.")
                .WithErrorCode("user.authentication.email_domain_mismatch");
        }
    }
}
