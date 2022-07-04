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
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence
{
    /// <summary>
    ///     The interface used for the DB context for the MarketParticipant database
    /// </summary>
    public interface IMarketParticipantDbContext
    {
        /// <summary>
        ///     Represent access to the organization database table
        /// </summary>
        DbSet<OrganizationEntity> Organizations { get; }

        /// <summary>
        ///     Represent access to the GridAreas database table
        /// </summary>
        DbSet<GridAreaEntity> GridAreas { get; }

        /// <summary>
        ///     Represent access to the ActorContacts database table
        /// </summary>
        DbSet<ActorContactEntity> ActorContacts { get; }

        /// <summary>
        ///     Represent access to the DomainEvents database table
        /// </summary>
        DbSet<DomainEventEntity> DomainEvents { get; }

        /// <summary>
        ///     Represent access to the GridAreas database table
        /// </summary>
        DbSet<GridAreaLinkEntity> GridAreaLinks { get; }

        /// <summary>
        ///     Saves changes to the database.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
