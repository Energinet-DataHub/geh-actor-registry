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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor;

public sealed class RemoveActorCertificateHandler : IRequestHandler<RemoveActorCertificateCommand>
{
    private readonly IActorRepository _actorRepository;
    private readonly ICertificateService _certificateService;
    public RemoveActorCertificateHandler(
        IActorRepository actorRepository,
        ICertificateService certificateService)
    {
        _actorRepository = actorRepository;
        _certificateService = certificateService;
    }

    public async Task Handle(RemoveActorCertificateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        // Find actor
        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        if (actor.Credentials is null)
            return;

        // Check that the actor has the correct type of credentials
        if (actor.Credentials is not ActorCertificateCredentials credentials)
        {
            throw new InvalidOperationException($"Actor with id {request.ActorId} does not have a certificate type credentials assigned");
        }

        // Remove certificate from key vault
        await _certificateService.RemoveCertificateAsync(credentials.KeyVaultSecretIdentifier).ConfigureAwait(false);

        // Remove certificate credentials from actor
        actor.Credentials = null;
        await _actorRepository
            .AddOrUpdateAsync(actor)
            .ConfigureAwait(false);
    }
}
