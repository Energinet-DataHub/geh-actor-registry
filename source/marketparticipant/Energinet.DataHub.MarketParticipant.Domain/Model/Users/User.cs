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

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Users;

public sealed class User
{
    private readonly SharedUserReferenceId? _sharedId;

    public User(SharedUserReferenceId sharedId, ExternalUserId externalId)
    {
        _sharedId = sharedId;
        Id = new UserId(Guid.Empty);
        ExternalId = externalId;
        RoleAssignments = new HashSet<UserRoleAssignment>();
    }

    public User(
        UserId id,
        ExternalUserId externalId,
        IEnumerable<UserRoleAssignment> roleAssignments,
        DateTimeOffset? mitIdSignupInitiatedAt)
    {
        _sharedId = null;
        Id = id;
        ExternalId = externalId;
        RoleAssignments = roleAssignments.ToHashSet();
        MitIdSignupInitiatedAt = mitIdSignupInitiatedAt;
    }

    public UserId Id { get; }
    public ExternalUserId ExternalId { get; }
    public SharedUserReferenceId SharedId => _sharedId ?? throw new InvalidOperationException("The shared reference id is only available when creating the entity.");
    public ICollection<UserRoleAssignment> RoleAssignments { get; }
    public DateTimeOffset? MitIdSignupInitiatedAt { get; private set; }

    public void InitiateMitIdSignup()
    {
        MitIdSignupInitiatedAt = DateTimeOffset.UtcNow;
    }
}
