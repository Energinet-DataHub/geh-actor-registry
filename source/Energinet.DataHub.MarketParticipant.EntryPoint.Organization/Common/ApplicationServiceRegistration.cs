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
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Infrastructure.Services;
using Energinet.DataHub.MarketParticipant.Integration.Model.Parsers;
using FluentValidation;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common
{
    internal static class ApplicationServiceRegistration
    {
        public static void AddApplicationServices(this Container container)
        {
            container.Register<IValidator<CreateOrganizationCommand>, CreateOrganizationCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<CreateActorCommand>, CreateActorCommandRuleSet>(Lifestyle.Scoped);
            container.Register<IActiveDirectoryService, ActiveDirectoryService>(Lifestyle.Scoped);
            container.Register<IOrganizationChangedEventParser, OrganizationChangedEventParser>(Lifestyle.Scoped);
            container.Register<IOrganizationEventDispatcher, OrganizationEventDispatcher>(Lifestyle.Scoped);
        }
    }
}
