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
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.Common.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.Common.ActiveDirectory
{
    public static class AzureAdConfigurationRegistration
    {
        public static void AddAzureAdConfiguration(this Container container)
        {
            ArgumentNullException.ThrowIfNull(container);

            container.RegisterSingleton(() =>
            {
                var configuration = container.GetService<IConfiguration>();
                var servicePrincipalObjectId = configuration.GetSetting(Settings.B2CBackendServicePrincipalNameObjectId);
                var backendAppId = configuration.GetSetting(Settings.B2CBackendId);
                return new AzureAdConfig(servicePrincipalObjectId, backendAppId);
            });
        }
    }
}
