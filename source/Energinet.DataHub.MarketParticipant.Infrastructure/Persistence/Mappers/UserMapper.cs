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

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Mappers
{
    internal sealed class UserMapper
    {
        public static void MapToEntity(User from, UserEntity to)
        {
            to.Email = from.Email.Address;
            to.Id = from.Id.Value;
            to.ExternalId = from.ExternalId.Value;
            to.RoleAssignments.Clear();
            foreach (var fromRoleAssignment in from.RoleAssignments)
            {
                to.RoleAssignments.Add(MapToEntity(fromRoleAssignment, from.Id));
            }
        }

        public static User MapFromEntity(UserEntity from)
        {
            return new User(
                new UserId(from.Id),
                new ExternalUserId(from.ExternalId),
                from.RoleAssignments.Select(MapFromEntity).ToList(),
                new EmailAddress(from.Email));
        }

        private static UserRoleAssignmentEntity MapToEntity(UserRoleAssignment fromRoleAssignment, UserId fromId)
        {
            return new UserRoleAssignmentEntity()
            {
                ActorId = fromRoleAssignment.ActorId,
                UserId = fromId.Value,
                UserRoleTemplateId = fromRoleAssignment.TemplateId.Value,
            };
        }

        private static UserRoleAssignment MapFromEntity(UserRoleAssignmentEntity from)
        {
            return new UserRoleAssignment(
                new UserId(from.UserId),
                from.ActorId,
                new UserRoleTemplateId(from.UserRoleTemplateId));
        }
    }
}
