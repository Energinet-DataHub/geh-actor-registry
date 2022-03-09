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

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    public sealed class Organization
    {
        public Organization(string name)
        {
            Id = new OrganizationId(Guid.Empty);
            Name = name;
            Actors = new Collection<Actor>();
        }

        public Organization(OrganizationId id, string name, IEnumerable<Actor> actors)
        {
            Id = id;
            Name = name;
            Actors = actors.ToList();
        }

        public OrganizationId Id { get; }

        public string Name { get; }

        public ICollection<Actor> Actors { get; }
    }
}
