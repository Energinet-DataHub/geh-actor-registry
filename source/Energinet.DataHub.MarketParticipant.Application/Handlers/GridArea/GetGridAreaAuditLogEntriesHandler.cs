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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using GridAreaAuditLogEntryField = Energinet.DataHub.MarketParticipant.Application.Commands.GridArea.GridAreaAuditLogEntryField;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea
{
    public sealed class GetGridAreaAuditLogEntriesHandler
        : IRequestHandler<GetGridAreaAuditLogEntriesCommand, GetGridAreaAuditLogEntriesResponse>
    {
        private readonly IGridAreaAuditLogEntryRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IUserIdentityRepository _userIdentityRepository;

        public GetGridAreaAuditLogEntriesHandler(
            IGridAreaAuditLogEntryRepository repository,
            IUserRepository userRepository,
            IUserIdentityRepository userIdentityRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
            _userIdentityRepository = userIdentityRepository;
        }

        public async Task<GetGridAreaAuditLogEntriesResponse> Handle(GetGridAreaAuditLogEntriesCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var entries = await _repository
                .GetAsync(new GridAreaId(request.GridAreaId))
                .ConfigureAwait(false);

            var entriesDto = new List<GridAreaAuditLogEntryDto>();
            var userNameLookup = new Dictionary<UserId, string?>();

            foreach (var entry in entries)
            {
                if (!userNameLookup.TryGetValue(entry.UserId, out var userName))
                {
                    var user = await _userRepository.GetAsync(entry.UserId).ConfigureAwait(false);
                    if (user != null)
                    {
                        var userIdentity = await _userIdentityRepository
                            .GetAsync(user.ExternalId)
                            .ConfigureAwait(false);

                        // TODO: Correct error message?
                        if (userIdentity == null)
                        {
                            userName = "[NOT FOUND IN AD]";
                        }
                        else
                        {
                            userName = userIdentity.FullName;
                        }
                    }

                    userNameLookup[entry.UserId] = userName;
                }

                entriesDto.Add(new GridAreaAuditLogEntryDto(
                    entry.Timestamp,
                    userName,
                    (GridAreaAuditLogEntryField)entry.Field,
                    entry.OldValue,
                    entry.NewValue,
                    entry.GridAreaId.Value));
            }

            return new GetGridAreaAuditLogEntriesResponse(entriesDto);
        }
    }
}
