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
using Energinet.DataHub.MarketParticipant.Application.Commands.User;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.User;

public sealed class GetUserPermissionsHandler
    : IRequestHandler<GetUserPermissionsCommand, GetUserPermissionsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserQueryRepository _userQueryRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserIdentityOpenIdLinkService _userIdentityOpenIdLinkService;

    public GetUserPermissionsHandler(
        IUserRepository userRepository,
        IUserQueryRepository userQueryRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserIdentityOpenIdLinkService userIdentityOpenIdLinkService)
    {
        _userRepository = userRepository;
        _userQueryRepository = userQueryRepository;
        _userIdentityRepository = userIdentityRepository;
        _userIdentityOpenIdLinkService = userIdentityOpenIdLinkService;
    }

    public async Task<GetUserPermissionsResponse> Handle(
        GetUserPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var externalUserId = new ExternalUserId(request.ExternalUserId);
        var user = await _userRepository
            .GetAsync(externalUserId)
            .ConfigureAwait(false);

        if (user == null)
        {
            var mergedIdentity = await _userIdentityOpenIdLinkService
                .ValidateAndSetupOpenIdAsync(externalUserId)
                .ConfigureAwait(false);

            user = await _userRepository
                .GetAsync(new ExternalUserId(mergedIdentity.Id.Value))
                .ConfigureAwait(false);
        }

        await ValidateOrClearLogonRequirementsAsync(user!).ConfigureAwait(false);

        var userIdentity = await _userIdentityRepository
            .GetAsync(externalUserId)
            .ConfigureAwait(false);

        if (userIdentity?.Status != UserIdentityStatus.Active)
            throw new NotFoundValidationException(request.ExternalUserId);

        var permissions = await _userQueryRepository
            .GetPermissionsAsync(new ActorId(request.ActorId), user!.ExternalId)
            .ConfigureAwait(false);

        var isFas = await _userQueryRepository
            .IsFasAsync(new ActorId(request.ActorId), user.ExternalId)
            .ConfigureAwait(false);

        return new GetUserPermissionsResponse(user.Id.Value, isFas, permissions.Select(permission => permission.Claim));
    }

    private async Task ValidateOrClearLogonRequirementsAsync(Domain.Model.Users.User user)
    {
        if (!user.ValidLogonRequirements)
        {
            throw new UnauthorizedAccessException("User invitation has expired");
        }

        if (user.InvitationExpiresAt.HasValue)
        {
            user.DeactivateUserExpiration();
            await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);
        }
    }
}
