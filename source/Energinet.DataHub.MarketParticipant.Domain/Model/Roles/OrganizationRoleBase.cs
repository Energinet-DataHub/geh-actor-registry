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
using System.Linq;

namespace Energinet.DataHub.MarketParticipant.Domain.Model.Roles
{
    public abstract class OrganizationRoleBase
    {
        protected OrganizationRoleBase()
        {
            Id = Guid.Empty;
            Status = RoleStatus.New;
        }

        protected OrganizationRoleBase(Guid id, RoleStatus status)
        {
            Id = id;
            Status = status;
        }

        public Guid Id { get; }

        public RoleStatus Status { get; private set; }

        public void Activate()
        {
            EnsureCorrectState(RoleStatus.Active, RoleStatus.New, RoleStatus.Inactive, RoleStatus.Passive);
            Status = RoleStatus.Active;
        }

        public void Deactivate()
        {
            EnsureCorrectState(RoleStatus.Inactive, RoleStatus.Active, RoleStatus.Passive);
            Status = RoleStatus.Inactive;
        }

        public void SetAsPassive()
        {
            EnsureCorrectState(RoleStatus.Passive, RoleStatus.Active, RoleStatus.Inactive);
            Status = RoleStatus.Passive;
        }

        public void Delete()
        {
            EnsureCorrectState(RoleStatus.Deleted, RoleStatus.New, RoleStatus.Active, RoleStatus.Inactive, RoleStatus.Passive);
            Status = RoleStatus.Deleted;
        }

        private void EnsureCorrectState(RoleStatus targetState, params RoleStatus[] allowedStates)
        {
            if (!allowedStates.Contains(Status) && targetState != Status)
            {
                throw new InvalidOperationException($"Cannot change state from {Status} to {targetState}.");
            }
        }
    }
}
