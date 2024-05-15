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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class ActorRequestSecretHandler : IRequestHandler<ActorRequestSecretCommand, ActorRequestSecretResponse>
{
    private readonly IActorClientSecretService _actorClientSecretService;
    private readonly IActorRepository _actorRepository;

    public ActorRequestSecretHandler(
        IActorClientSecretService actorClientSecretService,
        IActorRepository actorRepository)
    {
        _actorClientSecretService = actorClientSecretService;
        _actorRepository = actorRepository;
    }

    public async Task<ActorRequestSecretResponse> Handle(ActorRequestSecretCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        if (actor.Credentials is not null)
            throw new ValidationException("Credentials have already been assigned.");

        if (actor.ExternalActorId is null)
        {
            throw new ValidationException("Can't request a new secret, as the actor is either not Active or is still being created.")
                .WithErrorCode("actor.credentials.invalid_state");
        }

        var (clientId, secretId, secretText, expirationDate) = await _actorClientSecretService
            .CreateSecretForAppRegistrationAsync(actor)
            .ConfigureAwait(false);

        actor.Credentials = new ActorClientSecretCredentials(
            clientId,
            secretId,
            expirationDate);

        await _actorRepository
            .AddOrUpdateAsync(actor)
            .ConfigureAwait(false);

        return new ActorRequestSecretResponse(secretText);
    }
}
