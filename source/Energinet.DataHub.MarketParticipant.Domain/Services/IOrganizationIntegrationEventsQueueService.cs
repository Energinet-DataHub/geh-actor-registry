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
    /// Creates and enqueues integration events for Organizations.
    /// </summary>
    public interface IOrganizationIntegrationEventsQueueService
    {
        /// <summary>
        /// Creates and enqueues an Organization Updated integration event for the specified Organization.
        /// </summary>
        /// <param name="organization">The Organization to publish an integration event for.</param>
        Task EnqueueOrganizationUpdatedEventAsync(Organization organization);
    }
}
