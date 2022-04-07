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

using Energinet.DataHub.MarketParticipant.Integration.Model.Dtos;

namespace Energinet.DataHub.MarketParticipant.Integration.Model.Parsers
{
    /// <summary>
    /// Parses the protobuf contract.
    /// </summary>
    public interface IOrganizationUpdatedIntegrationEventParser
    {
        /// <summary>
        /// Parses the protobuf contract.
        /// </summary>
        /// <param name="protoContract">A byte array containing the <see cref="OrganizationUpdatedIntegrationEvent"/> protobuf contract.</param>
        /// <returns>The parsed <see cref="OrganizationUpdatedIntegrationEvent"/>.</returns>
        OrganizationUpdatedIntegrationEvent Parse(byte[] protoContract);

        /// <summary>
        /// Parses the event.
        /// </summary>
        /// <param name="integrationEvent">The event</param>
        /// <returns>The protobuf contract.</returns>
        byte[] Parse(OrganizationUpdatedIntegrationEvent integrationEvent);
    }
}
