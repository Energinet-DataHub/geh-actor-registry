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
using Energinet.DataHub.MarketParticipant.Application.Commands.Users;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Users;

public sealed class GetActorsAssociatedWithUserHandler
    : IRequestHandler<GetActorsAssociatedWithUserCommand, GetActorsAssociatedWithUserResponse>
{
    private readonly IUserRepository _userRepository;

    public GetActorsAssociatedWithUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetActorsAssociatedWithUserResponse> Handle(
        GetActorsAssociatedWithUserCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository
            .GetAsync(new UserId(request.UserId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(user, request.UserId);

        var actorAssignments = user
            .RoleAssignments
            .Select(roleAssignment => roleAssignment.ActorId.Value)
            .Append(user.AdministratedBy.Value)
            .Distinct();

        return new GetActorsAssociatedWithUserResponse(user.AdministratedBy.Value, actorAssignments);
    }
}
