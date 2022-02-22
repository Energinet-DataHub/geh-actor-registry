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
using System.Collections.ObjectModel;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model
{
    public sealed class OrganizationEntity
    {
        public OrganizationEntity()
        {
            Gln = string.Empty;
            Name = string.Empty;
            Roles = new Collection<OrganizationRoleEntity>();
        }

        public Guid Id { get; set; }
        public Guid? ActorId { get; set; }
        public string Gln { get; set; }
        public string Name { get; set; }

        public Collection<OrganizationRoleEntity> Roles { get; }
    }
}
