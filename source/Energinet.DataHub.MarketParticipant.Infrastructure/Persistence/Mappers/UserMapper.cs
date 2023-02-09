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

using System.Linq;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers;

internal sealed class UserMapper
{
    public static void MapToEntity(User from, UserEntity to)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        to.Email = from.Email.Address;
#pragma warning restore CS0618 // Type or member is obsolete
        to.Id = from.Id.Value;
        to.ExternalId = from.ExternalId.Value;
        to.InviteStatus = (int?)from.InviteStatus;

        var newAssignments = from
            .RoleAssignments
            .Select(newRa =>
            {
                var existing = to.RoleAssignments
                    .FirstOrDefault(oldRa => oldRa.ActorId == newRa.ActorId && oldRa.UserRoleId == newRa.UserRoleId.Value);

                return existing ?? MapToEntity(newRa, from.Id);
            })
            .ToList();

        to.RoleAssignments.Clear();

        foreach (var userRoleAssignment in newAssignments)
        {
            to.RoleAssignments.Add(userRoleAssignment);
        }
    }

    public static User MapFromEntity(UserEntity from)
    {
        return new User(
            new UserId(from.Id),
            new ExternalUserId(from.ExternalId),
            new EmailAddress(from.Email),
            from.RoleAssignments.Select(MapFromEntity),
            (UserInviteStatus?)from.InviteStatus);
    }

    private static UserRoleAssignmentEntity MapToEntity(UserRoleAssignment fromRoleAssignment, UserId fromId)
    {
        return new UserRoleAssignmentEntity
        {
            ActorId = fromRoleAssignment.ActorId,
            UserId = fromId.Value,
            UserRoleId = fromRoleAssignment.UserRoleId.Value,
        };
    }

    private static UserRoleAssignment MapFromEntity(UserRoleAssignmentEntity from)
    {
        return new UserRoleAssignment(from.ActorId, new UserRoleId(from.UserRoleId));
    }
}
