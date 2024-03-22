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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Delegations;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

public sealed class AllowedMarketRoleCombinationsForDelegationRuleService : IAllowedMarketRoleCombinationsForDelegationRuleService
{
    private static readonly Dictionary<EicFunction, EicFunction[]> _forbiddenCombinations = new()
    {
        { EicFunction.EnergySupplier, [EicFunction.GridAccessProvider] },
        { EicFunction.GridAccessProvider, [EicFunction.EnergySupplier] },
    };

    private readonly IActorRepository _actorRepository;
    private readonly IProcessDelegationRepository _processDelegationRepository;

    public AllowedMarketRoleCombinationsForDelegationRuleService(
        IActorRepository actorRepository,
        IProcessDelegationRepository processDelegationRepository)
    {
        _actorRepository = actorRepository;
        _processDelegationRepository = processDelegationRepository;
    }

    public async Task ValidateAsync(OrganizationId organizationId, EicFunction newMarketRole)
    {
        var existingActors = await _actorRepository
            .GetActorsAsync(organizationId)
            .ConfigureAwait(false);

        var actorsList = existingActors.ToList();

        var allMarketRolesInOrganization = actorsList
            .SelectMany(actor => actor.MarketRoles)
            .Select(marketRole => marketRole.Function)
            .Append(newMarketRole)
            .ToHashSet();

        var delegatedActors = actorsList
            .Where(actor => actor.MarketRoles.Any(mr => mr.Function == EicFunction.Delegated));

        foreach (var delegatedActor in delegatedActors)
        {
            var existingDelegations = await _processDelegationRepository
                .GetDelegatedToActorAsync(delegatedActor.Id)
                .ConfigureAwait(false);

            foreach (var existingDelegation in existingDelegations)
            {
                var delegatedBy = await _actorRepository
                    .GetAsync(existingDelegation.DelegatedBy)
                    .ConfigureAwait(false);

                foreach (var delegationPeriod in existingDelegation.Delegations)
                {
                    if (actorsList.All(actor => actor.Id != delegationPeriod.DelegatedTo))
                        continue;

                    ValidateDelegation(delegatedBy!, allMarketRolesInOrganization);
                }
            }
        }
    }

    public async Task ValidateAsync(ProcessDelegation processDelegation)
    {
        ArgumentNullException.ThrowIfNull(processDelegation);

        var delegatedBy = await _actorRepository
            .GetAsync(processDelegation.DelegatedBy)
            .ConfigureAwait(false);

        var delegatedTo = processDelegation
            .Delegations
            .Select(d => d.DelegatedTo)
            .ToHashSet();

        var affectedActors = await _actorRepository
            .GetActorsAsync(delegatedTo)
            .ConfigureAwait(false);

        var affectedActorOrganizations = affectedActors
            .Select(actor => actor.OrganizationId)
            .ToHashSet();

        foreach (var organizationId in affectedActorOrganizations)
        {
            var actorsInOrganization = await _actorRepository
                .GetActorsAsync(organizationId)
                .ConfigureAwait(false);

            var rolesInOrg = actorsInOrganization
                .SelectMany(actor => actor.MarketRoles)
                .Select(mr => mr.Function)
                .ToList();

            ValidateDelegation(delegatedBy!, rolesInOrg);
        }
    }

    private static void ValidateDelegation(Actor delegatedBy, IReadOnlyCollection<EicFunction> delegatedTo)
    {
        foreach (var actorMarketRole in delegatedBy.MarketRoles)
        {
            if (!_forbiddenCombinations.TryGetValue(actorMarketRole.Function, out var forbidden))
                continue;

            foreach (var eicFunction in delegatedTo)
            {
                if (forbidden.Contains(eicFunction))
                {
                    throw new ValidationException($"Delegated '{actorMarketRole.Function}' cannot be used in an organization containing market role '{eicFunction}'.")
                        .WithErrorCode("process_delegation.market_role_forbidden")
                        .WithArgs(("assigned_market_role", actorMarketRole.Function.ToString()), ("conflicting_market_role", eicFunction.ToString()));
                }
            }
        }
    }
}
