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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class UserInviteAuditLogEntryRepository : IUserInviteAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public UserInviteAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserInviteDetailsAuditLogEntry>> GetAsync(UserId userId)
        {
            var logQuery =
                from log in _context.UserInviteAuditLogEntries
                join actor in _context.Actors on log.ActorId equals actor.Id
                where log.UserId == userId.Value
                select new UserInviteDetailsAuditLogEntry(
                    new UserId(log.UserId),
                    new UserId(log.ChangedByUserId),
                    new ActorId(log.ActorId),
                    actor.Name,
                    log.Timestamp);

            return await logQuery.ToListAsync().ConfigureAwait(false);
        }

        public Task InsertAuditLogEntryAsync(UserInviteAuditLogEntry logEntry)
        {
            ArgumentNullException.ThrowIfNull(logEntry);

            var entity = new UserInviteAuditLogEntryEntity
            {
                UserId = logEntry.UserId.Value,
                Timestamp = logEntry.Timestamp,
                ChangedByUserId = logEntry.ChangedByUserId.Value,
                ActorId = logEntry.ActorId.Value
            };

            _context.UserInviteAuditLogEntries.Add(entity);
            return _context.SaveChangesAsync();
        }
    }
}
