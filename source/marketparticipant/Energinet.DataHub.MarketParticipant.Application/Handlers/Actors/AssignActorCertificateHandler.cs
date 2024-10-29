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
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class AssignActorCertificateHandler : IRequestHandler<AssignActorCertificateCommand>
{
    private readonly IActorRepository _actorRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IActorCertificateExpirationService _actorCertificateExpirationService;
    private readonly ICertificateService _certificateService;

    public AssignActorCertificateHandler(
        IActorRepository actorRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IDomainEventRepository domainEventRepository,
        IActorCertificateExpirationService actorCertificateExpirationService,
        ICertificateService certificateService)
    {
        _actorRepository = actorRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _domainEventRepository = domainEventRepository;
        _actorCertificateExpirationService = actorCertificateExpirationService;
        _certificateService = certificateService;
    }

    public async Task Handle(AssignActorCertificateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        var x509Certificate = _certificateService.CreateAndValidateX509Certificate(request.Certificate);
        var certificateLookupIdentifier = $"{actor.ActorNumber.Value}-{x509Certificate.Thumbprint}";

        if (actor.Credentials is not null)
            throw new ValidationException("Credentials have already been assigned.");

        var expirationDate = await _actorCertificateExpirationService
            .CalculateExpirationDateAsync(x509Certificate)
            .ConfigureAwait(false);

        actor.Credentials = new ActorCertificateCredentials(
            x509Certificate.Thumbprint,
            certificateLookupIdentifier,
            expirationDate);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var result = await _actorRepository
                .AddOrUpdateAsync(actor)
                .ConfigureAwait(false);

            result.ThrowOnError(ActorErrorHandler.HandleActorError);

            await _domainEventRepository
                .EnqueueAsync(actor)
                .ConfigureAwait(false);

            await _certificateService
                .SaveCertificateAsync(certificateLookupIdentifier, x509Certificate, expirationDate)
                .ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
