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
using Energinet.DataHub.MarketParticipant.Application.Commands.UserRoles;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.UserRoles;

public sealed class DeactivateUserRoleHandler : IRequestHandler<DeactivateUserRoleCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;

    public DeactivateUserRoleHandler(
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWorkProvider unitOfWorkProvider)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
    }

    public async Task Handle(DeactivateUserRoleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userRoleId = new UserRoleId(request.UserRoleId);
        var userRole = await _userRoleRepository.GetAsync(userRoleId).ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(userRole, $"User role with id: {userRoleId} was not found");

        var users = await _userRepository
            .GetToUserRoleAsync(userRoleId)
            .ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            foreach (var user in users)
            {
                var role = user.RoleAssignments.Single(x => x.UserRoleId == userRoleId);
                user.RoleAssignments.Remove(role);

                await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);
            }

            userRole.Status = UserRoleStatus.Inactive;
            await _userRoleRepository.UpdateAsync(userRole).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }
}
