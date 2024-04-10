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
using Energinet.DataHub.MarketParticipant.Application.Commands.Contacts;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services.Rules;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers;

public sealed class CreateActorContactHandler : IRequestHandler<CreateActorContactCommand, CreateActorContactResponse>
{
    private readonly IActorRepository _actorRepository;
    private readonly IActorContactRepository _contactRepository;
    private readonly IOverlappingActorContactCategoriesRuleService _overlappingContactCategoriesRuleService;

    public CreateActorContactHandler(
        IActorRepository actorRepository,
        IActorContactRepository contactRepository,
        IOverlappingActorContactCategoriesRuleService overlappingContactCategoriesRuleService)
    {
        _actorRepository = actorRepository;
        _contactRepository = contactRepository;
        _overlappingContactCategoriesRuleService = overlappingContactCategoriesRuleService;
    }

    public async Task<CreateActorContactResponse> Handle(CreateActorContactCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        var existingContacts = await _contactRepository
            .GetAsync(actor.Id)
            .ConfigureAwait(false);

        var contact = CreateContact(actor.Id, request.Contact);

        _overlappingContactCategoriesRuleService
            .ValidateCategoriesAcrossContacts(existingContacts.Append(contact));

        var contactId = await _contactRepository
            .AddAsync(contact)
            .ConfigureAwait(false);

        return new CreateActorContactResponse(contactId.Value);
    }

    private static ActorContact CreateContact(ActorId actorId, CreateActorContactDto contactDto)
    {
        var optionalPhoneNumber = contactDto.Phone == null
            ? null
            : new PhoneNumber(contactDto.Phone);

        return new ActorContact(
            actorId,
            contactDto.Name,
            contactDto.Category,
            new EmailAddress(contactDto.Email),
            optionalPhoneNumber);
    }
}
