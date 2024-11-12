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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class ActorFactoryService(
    IActorRepository actorRepository,
    IUnitOfWorkProvider unitOfWorkProvider,
    IOverlappingEicFunctionsRuleService overlappingEicFunctionsRuleService,
    IUniqueGlobalLocationNumberRuleService uniqueGlobalLocationNumberRuleService,
    IUniqueMarketRoleGridAreaRuleService uniqueMarketRoleGridAreaRuleService,
    IDomainEventRepository domainEventRepository,
    IEntityLock entityLock,
    IAllowedMarketRoleCombinationsForDelegationRuleService allowedMarketRoleCombinationsForDelegationRuleService)
    : IActorFactoryService
{
    public async Task<Actor> CreateAsync(
        Organization organization,
        ActorNumber actorNumber,
        ActorName actorName,
        ActorMarketRole? marketRole)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(actorNumber);
        ArgumentNullException.ThrowIfNull(actorName);

        var newActor = new Actor(organization.Id, actorNumber, actorName);

        if (marketRole is not null)
        {
            newActor.SetMarketRole(marketRole);
        }

        var uow = await unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            await entityLock.LockAsync(LockableEntity.Actor).ConfigureAwait(false);

            await uniqueGlobalLocationNumberRuleService
                .ValidateGlobalLocationNumberAvailableAsync(organization, actorNumber)
                .ConfigureAwait(false);

            await overlappingEicFunctionsRuleService
                .ValidateEicFunctionsAcrossActorsAsync(newActor)
                .ConfigureAwait(false);

            if (newActor.MarketRole is { } mr)
            {
                await allowedMarketRoleCombinationsForDelegationRuleService.ValidateAsync(organization.Id, mr.Function).ConfigureAwait(false);
            }

            var actorId = await SaveActorAsync(newActor).ConfigureAwait(false);

            var committedActor = (await actorRepository
                .GetAsync(actorId)
                .ConfigureAwait(false))!;

            await uniqueMarketRoleGridAreaRuleService
                .ValidateAndReserveAsync(committedActor)
                .ConfigureAwait(false);

            committedActor.Activate();

            await domainEventRepository
                .EnqueueAsync(committedActor)
                .ConfigureAwait(false);

            await SaveActorAsync(committedActor).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return committedActor;
        }
    }

    private async Task<ActorId> SaveActorAsync(Actor newActor)
    {
        var result = await actorRepository
            .AddOrUpdateAsync(newActor)
            .ConfigureAwait(false);

        result.ThrowOnError(ActorErrorHandler.HandleActorError);
        return result.Value;
    }
}
