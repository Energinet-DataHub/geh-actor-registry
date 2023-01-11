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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserOverviewRepository : IUserOverviewRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;
    private readonly IUserIdentityRepository _userIdentityRepository;

    public UserOverviewRepository(
        IMarketParticipantDbContext marketParticipantDbContext,
        IUserIdentityRepository userIdentityRepository)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
        _userIdentityRepository = userIdentityRepository;
    }

    public Task<int> GetTotalUserCountAsync(Guid? actorId)
    {
        var query = BuildUsersSearchQuery(actorId, null, null);
        return query.CountAsync();
    }

    public async Task<IEnumerable<UserOverviewItem>> GetUsersAsync(int pageNumber, int pageSize, Guid? actorId)
    {
        var query = BuildUsersSearchQuery(actorId, null, null);
        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.ExternalId })
            .ToListAsync()
            .ConfigureAwait(false);

        var userLookup = users.ToDictionary(
            x => new ExternalUserId(x.ExternalId),
            y => new
            {
                Id = new UserId(y.Id),
                ExternalId = new ExternalUserId(y.ExternalId),
            });

        var userIdentities = await _userIdentityRepository
            .GetUserIdentitiesAsync(userLookup.Keys)
            .ConfigureAwait(false);

        var test = userIdentities
            .Select(userIdentity =>
                {
                    var user = userLookup[userIdentity.Id];
                    return new UserOverviewItem(
                        user.Id,
                        userIdentity.Email,
                        userIdentity.Name,
                        userIdentity.PhoneNumber,
                        userIdentity.CreatedDate,
                        userIdentity.Enabled);
                })
            .OrderBy(x => x.Email.Address);

        return test;
    }

    public async Task<(IEnumerable<UserOverviewItem> Items, int TotalCount)> SearchUsersAsync(
        int pageNumber,
        int pageSize,
        Guid? actorId,
        string? searchText,
        bool? active,
        Collection<EicFunction>? eicFunctions)
    {
        // We need to do two searches and two lookup, since the queries in either our data or AD can return results not in the other, and we need AD data for both
        // Search and then Filter only users from the AD search that have an ID in our local data
        var searchUserIdentities = (await _userIdentityRepository
            .SearchUserIdentitiesAsync(searchText, active)
            .ConfigureAwait(false))
            .ToList();

        var knownLocalUsers = await BuildUserLookupQuery(actorId, searchUserIdentities.Select(x => x.Id))
            .Select(y => new { y.Id, y.ExternalId })
            .ToListAsync()
            .ConfigureAwait(false);
        var knownLocalIds = knownLocalUsers.Select(x => x.ExternalId);
        searchUserIdentities = searchUserIdentities
            .Where(x => knownLocalIds.Contains(x.Id.Value))
            .ToList();

        // Search local data and then fetch data from AD for results from our own data, that wasn't in the already found identities
        var searchQuery = await BuildUsersSearchQuery(actorId, eicFunctions, searchText)
            .Select(x => new { x.Id, x.ExternalId })
            .ToListAsync()
            .ConfigureAwait(false);

        var localUserIdentitiesLookup = await _userIdentityRepository
            .GetUserIdentitiesAsync(searchQuery
                .Select(x => x.ExternalId)
                .Except(knownLocalIds)
                .Select(x => new ExternalUserId(x)))
            .ConfigureAwait(false);

        if (active.HasValue)
        {
            localUserIdentitiesLookup = localUserIdentitiesLookup
                .Where(ident => ident.Enabled == active.Value);
        }

        //Combine results and create final search result
        var userLookup = searchQuery
            .Union(knownLocalUsers)
            .ToDictionary(x => x.ExternalId);
        var allIdentities = searchUserIdentities
            .Union(localUserIdentitiesLookup)
            .OrderBy(x => x.Email.Address);

        // Filter User Identities to only be from our user pool
        var items = allIdentities
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(userIdentity =>
            {
                var user = userLookup[userIdentity.Id.Value];
                return new UserOverviewItem(
                    new UserId(user.Id),
                    userIdentity.Email,
                    userIdentity.Name,
                    userIdentity.PhoneNumber,
                    userIdentity.CreatedDate,
                    userIdentity.Enabled);
            });

        return (items, userLookup.Count);
    }

    private IQueryable<UserEntity> BuildUsersSearchQuery(Guid? actorId, Collection<EicFunction>? eicFunctions, string? searchText)
    {
        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            join ur in _marketParticipantDbContext.UserRoles on r.UserRoleId equals ur.Id
            join actor in _marketParticipantDbContext.Actors on r.ActorId equals actor.Id
            where
                (actorId == null || r.ActorId == actorId)
                && (eicFunctions == null || !eicFunctions.Any() || ur.EicFunctions.All(q => eicFunctions.Contains(q.EicFunction)))
                && (searchText == null || actor.Name.Contains(searchText) || actor.ActorNumber.Contains(searchText) || ur.Name.Contains(searchText) || u.Email.Contains(searchText))
            select u;

        return query.OrderBy(x => x.Email).Distinct();
    }

    private IQueryable<UserEntity> BuildUserLookupQuery(Guid? actorId, IEnumerable<ExternalUserId> externalUserIds)
    {
        var guids = externalUserIds.Select(x => x.Value);
        var query =
            from u in _marketParticipantDbContext.Users
            join r in _marketParticipantDbContext.UserRoleAssignments on u.Id equals r.UserId
            where
                (actorId == null || r.ActorId == actorId)
                && guids.Contains(u.ExternalId)
            select u;

        return query.OrderBy(x => x.Email).Distinct();
    }
}
