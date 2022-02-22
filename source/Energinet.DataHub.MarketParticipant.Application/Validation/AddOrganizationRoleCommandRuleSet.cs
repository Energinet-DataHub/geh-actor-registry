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

using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Validation.Rules;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation
{
    public sealed class AddOrganizationRoleCommandRuleSet : AbstractValidator<AddOrganizationRoleCommand>
    {
        public AddOrganizationRoleCommandRuleSet()
        {
            RuleFor(command => command.OrganizationId)
                .NotEmpty()
                .SetValidator(new GuidValidationRule<AddOrganizationRoleCommand>());

            RuleFor(command => command.Role)
                .NotNull()
                .ChildRules(validator =>
                {
                    validator
                        .RuleFor(role => role.BusinessRole)
                        .NotEmpty()
                        .IsEnumName(typeof(BusinessRoleCode), false);
                });
        }
    }
}
