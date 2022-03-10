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

using System.Collections.Generic;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Services.Rules
{
    /// <summary>
    /// Ensures that actors within an organization have valid roles.
    /// </summary>
    public interface IOverlappingBusinessRolesRuleService
    {
        /// <summary>
        /// Ensures that the given actors have unique roles and can exist in the same organization.
        /// Throws an exception, if the roles are invalid.
        /// </summary>
        /// <param name="actors">The list of actors in an organization.</param>
        void ValidateRolesAcrossActors(IEnumerable<Actor> actors);

        /// <summary>
        /// Ensures that the given actors have unique roles and can exist in the same organization.
        /// Throws an exception, if the roles are invalid.
        /// </summary>
        /// <param name="actors">The list of actors in an organization.</param>
        /// <param name="newActorRoles">The market roles of a new actor.</param>
        void ValidateRolesAcrossActors(IEnumerable<Actor> actors, IEnumerable<MarketRole> newActorRoles);
    }
}
