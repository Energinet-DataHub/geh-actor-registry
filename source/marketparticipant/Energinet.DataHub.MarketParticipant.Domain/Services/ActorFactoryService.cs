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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class ActorFactoryService : IActorFactoryService
{
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IOverlappingEicFunctionsRuleService _overlappingEicFunctionsRuleService;
    private readonly IUniqueGlobalLocationNumberRuleService _uniqueGlobalLocationNumberRuleService;
    private readonly IUniqueMarketRoleGridAreaRuleService _uniqueMarketRoleGridAreaRuleService;

    public ActorFactoryService(
        IActorRepository actorRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IOverlappingEicFunctionsRuleService overlappingEicFunctionsRuleService,
        IUniqueGlobalLocationNumberRuleService uniqueGlobalLocationNumberRuleService,
        IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleService)
    {
        _actorRepository = actorRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _overlappingEicFunctionsRuleService = overlappingEicFunctionsRuleService;
        _uniqueGlobalLocationNumberRuleService = uniqueGlobalLocationNumberRuleService;
        _uniqueMarketRoleGridAreaRuleService = uniqueMarketRoleGridAreaRuleService;
    }

    public async Task<Actor> CreateAsync(
        Organization organization,
        ActorNumber actorNumber,
        ActorName actorName,
        IReadOnlyCollection<ActorMarketRole> marketRoles)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(actorNumber);
        ArgumentNullException.ThrowIfNull(marketRoles);

        await _uniqueGlobalLocationNumberRuleService
            .ValidateGlobalLocationNumberAvailableAsync(organization, actorNumber)
            .ConfigureAwait(false);

        var newActor = new Actor(organization.Id, actorNumber) { Name = actorName };

        foreach (var marketRole in marketRoles)
            newActor.AddMarketRole(marketRole);

        var existingActors = await _actorRepository
            .GetActorsAsync(organization.Id)
            .ConfigureAwait(false);

        _overlappingEicFunctionsRuleService.ValidateEicFunctionsAcrossActors(existingActors.Append(newActor));

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var savedActor = await SaveActorAsync(newActor).ConfigureAwait(false);

            await _uniqueMarketRoleGridAreaRuleService
                .ValidateAsync(savedActor)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return savedActor;
        }
    }

    private async Task<Actor> SaveActorAsync(Actor newActor)
    {
        var actorId = await _actorRepository
            .AddOrUpdateAsync(newActor)
            .ConfigureAwait(false);

        return (await _actorRepository
            .GetAsync(actorId)
            .ConfigureAwait(false))!;
    }
}
