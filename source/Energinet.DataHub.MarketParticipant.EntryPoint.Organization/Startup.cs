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
using Dapper;
using DapperExtensions;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common;
using Energinet.DataHub.MarketParticipant.Infrastructure.Mappers;
using Energinet.DataHub.MarketParticipant.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization
{
    internal sealed class Startup : StartupBase
    {
        protected override void Configure(Container container)
        {
            RegisterActorConfig(container);
            container.Register<IOrganizationRepository, OrganizationRepository>(Lifestyle.Singleton);
            SetupDapper();
        }

        private static void RegisterActorConfig(Container container)
        {
            container.RegisterSingleton(() =>
            {
                var configuration = container.GetService<IConfiguration>();
                var connectionString = configuration.GetValue<string>("SQL_MP_DB_CONNECTION_STRING") ??
                                       throw new InvalidOperationException(
                                           "Market Participant datababase Connection string not found");
                return new ActorDbConfig(connectionString);
            });
        }

        private static void SetupDapper()
        {
            SqlMapper.AddTypeHandler(new GlnTypeHandler());
            SqlMapper.AddTypeHandler(new UUIDTypeHandler());
            DapperAsyncExtensions.SetMappingAssemblies(new[]
            {
                typeof (OrganizationMap).Assembly
            });
        }
    }
}
