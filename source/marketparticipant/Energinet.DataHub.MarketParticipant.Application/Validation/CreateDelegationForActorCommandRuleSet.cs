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
using Energinet.DataHub.MarketParticipant.Application.Commands.Delegations;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation
{
    public sealed class CreateMessageDelegationCommandRuleSet : AbstractValidator<CreateMessageDelegationCommand>
    {
        public CreateMessageDelegationCommandRuleSet()
        {
            RuleFor(command => command.CreateDelegation)
                .NotNull()
                .ChildRules(validator =>
                {
                    validator
                        .RuleFor(delegation => delegation.DelegatedFrom)
                        .NotEmpty();

                    validator
                        .RuleFor(delegation => delegation.DelegatedFrom.Value)
                        .NotEmpty();

                    validator
                        .RuleFor(delegation => delegation.DelegatedTo)
                        .NotEmpty();

                    validator
                        .RuleFor(delegation => delegation.DelegatedTo.Value)
                        .NotEmpty();

                    validator
                        .RuleFor(delegation => delegation.GridAreas)
                        .NotEmpty();

                    validator
                        .RuleForEach(delegation => delegation.GridAreas)
                        .NotEmpty()
                        .ChildRules(gridArea => gridArea.RuleFor(g => g.Value).NotEmpty());

                    validator
                        .RuleFor(delegation => delegation.MessageTypes)
                        .NotEmpty();

                    validator
                        .RuleForEach(delegation => delegation.MessageTypes)
                        .NotEmpty()
                        .Must(Enum.IsDefined);
                });
        }
    }
}
