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
    /// Ensures that grid areas are allowed for the given marked roles.
    /// </summary>
    public interface IAllowedGridAreasRuleService
    {
        /// <summary>
        /// Ensures that grid areas are allowed for the given market roles.
        /// </summary>
        /// <param name="gridAreas">The grid areas to validate.</param>
        /// <param name="marketRoles">The market roles applied to the given grid areas.</param>
        void ValidateGridAreas(
            IEnumerable<GridAreaId> gridAreas,
            IEnumerable<MarketRole> marketRoles);
    }
}
