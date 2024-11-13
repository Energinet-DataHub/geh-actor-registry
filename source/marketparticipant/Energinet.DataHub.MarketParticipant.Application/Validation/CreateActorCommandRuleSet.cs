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

using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Application.Validation.Rules;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using FluentValidation;

namespace Energinet.DataHub.MarketParticipant.Application.Validation;

public sealed class CreateActorCommandRuleSet : AbstractValidator<CreateActorCommand>
{
    public CreateActorCommandRuleSet()
    {
        RuleFor(command => command.Actor)
            .NotNull()
            .ChildRules(validator =>
            {
                validator
                    .RuleFor(actor => actor.OrganizationId)
                    .NotEmpty();

                validator
                    .RuleFor(actor => actor.Name)
                    .NotNull()
                    .ChildRules(nameValidator =>
                    {
                        nameValidator
                            .RuleFor(actorNameDto => actorNameDto.Value)
                            .NotEmpty()
                            .Length(1, 512);
                    });

                validator
                    .RuleFor(actor => actor.ActorNumber)
                    .SetValidator(new GlobalLocationNumberValidationRule<CreateActorDto>())
                    .When(i => string.IsNullOrWhiteSpace(i.ActorNumber.Value) || i.ActorNumber.Value.Length <= 13);

                validator
                    .RuleFor(actor => actor.ActorNumber)
                    .SetValidator(new EnergyIdentificationCodeValidationRule<CreateActorDto>())
                    .When(i => string.IsNullOrWhiteSpace(i.ActorNumber.Value) || i.ActorNumber.Value.Length >= 14);

                validator
                    .RuleFor(actor => actor.MarketRole)
                    .ChildRules(gridAreaValidator =>
                        gridAreaValidator
                            .RuleFor(x => x.GridAreas)
                            .NotEmpty()
                            .When(marketRole => marketRole.EicFunction == EicFunction.GridAccessProvider));

                validator
                    .RuleFor(actor => actor.MarketRole)
                    .NotNull()
                    .ChildRules(roleValidator =>
                    {
                        roleValidator
                            .RuleFor(x => x.EicFunction)
                            .NotEmpty()
                            .IsInEnum();
                    });

                validator
                    .RuleFor(actor => actor.MarketRole)
                    .ChildRules(inlineValidator =>
                    {
                        inlineValidator
                            .RuleForEach(m => m.GridAreas)
                            .ChildRules(validationRules =>
                            {
                                validationRules
                                    .RuleFor(r => r.MeteringPointTypes)
                                    .NotNull()
                                    .ChildRules(v => v
                                        .RuleForEach(r => r)
                                        .NotEmpty()
                                        .IsEnumName(typeof(MeteringPointType), false));
                            });
                    });
            });
    }
}
