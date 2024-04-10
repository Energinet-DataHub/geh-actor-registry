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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class GetActorsHandler : IRequestHandler<GetActorsCommand, GetActorsResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IOrganizationExistsHelperService _organizationExistsHelperService;

    public GetActorsHandler(
        IActorRepository actorRepository,
        IOrganizationExistsHelperService organizationExistsHelperService)
    {
        _actorRepository = actorRepository;
        _organizationExistsHelperService = organizationExistsHelperService;
    }

    public async Task<GetActorsResponse> Handle(
        GetActorsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var organization = await _organizationExistsHelperService
            .EnsureOrganizationExistsAsync(request.OrganizationId)
            .ConfigureAwait(false);

        var actors = await _actorRepository
            .GetActorsAsync(organization.Id)
            .ConfigureAwait(false);

        return new GetActorsResponse(actors.Select(OrganizationMapper.Map));
    }
}
