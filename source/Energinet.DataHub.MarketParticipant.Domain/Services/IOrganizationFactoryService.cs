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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    /// <summary>
    /// A factory service ensuring correct construction of an Organization.
    /// </summary>
    public interface IOrganizationFactoryService
    {
        /// <summary>
        /// Creates an organization.
        /// </summary>
        /// <param name="gln">The global location number of the new organization.</param>
        /// <param name="name">The name of the new organization.</param>
        /// <returns>The created organization.</returns>
        Task<Organization> CreateAsync(GlobalLocationNumber gln, string name);
    }
}
