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
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Email;

public sealed class UserAssignedToActorEmailTemplate : EmailTemplate
{
    public UserAssignedToActorEmailTemplate(UserIdentity userIdentity, Organization organization, Actor actor)
        : base(EmailTemplateId.UserAssignedToActor, PrepareParameters(userIdentity, organization, actor))
    {
    }

    public UserAssignedToActorEmailTemplate(EmailTemplateId id, IReadOnlyDictionary<string, string> parameters)
        : base(id, parameters)
    {
    }

    private static Dictionary<string, string> PrepareParameters(UserIdentity userIdentity, Organization organization, Actor actor)
    {
        ArgumentNullException.ThrowIfNull(userIdentity);
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(actor);

        return new Dictionary<string, string>
        {
            { "first_name", userIdentity.FirstName },
            { "organization_name", organization.Name },
            { "actor_name", actor.Name.Value },
            { "actor_gln", actor.ActorNumber.Value }
        };
    }
}
