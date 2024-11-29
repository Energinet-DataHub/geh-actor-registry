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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;

namespace Energinet.DataHub.MarketParticipant.Application.Services;

public sealed class ActorConsolidationService : IActorConsolidationService
{
    private readonly IActorRepository _actorRepository;
    private readonly IGridAreaRepository _gridAreaRepository;
    private readonly IActorConsolidationAuditLogRepository _actorConsolidationAuditLogRepository;
    private readonly IAuditIdentityProvider _auditIdentityProvider;
    private readonly IDomainEventRepository _domainEventRepository;

    public ActorConsolidationService(
        IActorRepository actorRepository,
        IGridAreaRepository gridAreaRepository,
        IActorConsolidationAuditLogRepository actorConsolidationAuditLogRepository,
        IAuditIdentityProvider auditIdentityProvider,
        IDomainEventRepository domainEventRepository)
    {
        _actorRepository = actorRepository;
        _gridAreaRepository = gridAreaRepository;
        _actorConsolidationAuditLogRepository = actorConsolidationAuditLogRepository;
        _auditIdentityProvider = auditIdentityProvider;
        _domainEventRepository = domainEventRepository;
    }

    public async Task ConsolidateAsync(ActorConsolidation actorConsolidation)
    {
        ArgumentNullException.ThrowIfNull(actorConsolidation);

        var fromActor = await _actorRepository.GetAsync(actorConsolidation.ActorFromId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(fromActor, actorConsolidation.ActorFromId.Value);

        var toActor = await _actorRepository.GetAsync(actorConsolidation.ActorToId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(toActor, actorConsolidation.ActorToId.Value);

        if (fromActor.MarketRole.Function is EicFunction.GridAccessProvider
            && toActor.MarketRole.Function is EicFunction.GridAccessProvider)
        {
            var gridAreasToTransfer = await GetGridAreasToTransferAsync(fromActor.MarketRole.GridAreas.Select(ga => ga.Id)).ConfigureAwait(false);

            await AuditLogConsolidationAsync(actorConsolidation, gridAreasToTransfer, GridAreaAuditedChange.ConsolidationRequested).ConfigureAwait(false);

            var newFromActor = UpdateActor(actor: fromActor, newGridAreas: []);
            fromActor.Deactivate();

            var newToActor = UpdateActor(actor: toActor, newGridAreas: toActor.MarketRole.GridAreas.Concat(fromActor.MarketRole.GridAreas));

            await _actorRepository
                .AddOrUpdateAsync(fromActor)
                .ConfigureAwait(false);

            await _domainEventRepository
                .EnqueueAsync(fromActor)
                .ConfigureAwait(false);

            await _actorRepository
                .AddOrUpdateAsync(toActor)
                .ConfigureAwait(false);

            await _domainEventRepository
                .EnqueueAsync(toActor)
                .ConfigureAwait(false);

            foreach (var gridArea in gridAreasToTransfer)
            {
                gridArea.ValidTo = actorConsolidation.ScheduledAt.ToDateTimeOffset();
                await _gridAreaRepository
                    .AddOrUpdateAsync(gridArea)
                    .ConfigureAwait(false);
            }

            await AuditLogConsolidationAsync(actorConsolidation, gridAreasToTransfer, GridAreaAuditedChange.ConsolidationCompleted).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException("Only grid access providers can be consolidated. Is this supposed to happen??");
        }
    }

    private static Actor UpdateActor(Actor actor, IEnumerable<ActorGridArea> newGridAreas)
    {
        return new Actor(
                actor.Id,
                actor.OrganizationId,
                actor.ExternalActorId,
                actor.ActorNumber,
                actor.Status,
                new ActorMarketRole(
                    actor.MarketRole.Function,
                    newGridAreas,
                    actor.MarketRole.Comment),
                actor.Name,
                actor.Credentials);
    }

    private async Task AuditLogConsolidationAsync(
        ActorConsolidation actorConsolidation,
        IEnumerable<GridArea> gridAreas,
        GridAreaAuditedChange gridAreaAuditedChange)
    {
        foreach (var gridArea in gridAreas)
        {
            await _actorConsolidationAuditLogRepository.AuditAsync(
                _auditIdentityProvider.IdentityId,
                gridAreaAuditedChange,
                actorConsolidation,
                gridArea.Id).ConfigureAwait(false);
        }
    }

    private async Task<IEnumerable<GridArea>> GetGridAreasToTransferAsync(IEnumerable<GridAreaId> gridAreaIdsToTransfer)
    {
        var gridAreas = await _gridAreaRepository.GetAsync().ConfigureAwait(false);
        var gridAreasToTransfer = gridAreas.Where(x => gridAreaIdsToTransfer.Contains(x.Id));
        return gridAreasToTransfer;
    }
}
