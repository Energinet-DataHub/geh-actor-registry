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

using Azure.Identity;
using Energinet.DataHub.MarketParticipant.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.Common
{
    internal static class GraphServiceClientRegistration
    {
        public static void AddGraphServiceClient(this Container container)
        {
            container.Register(
                () =>
                {
                    var configuration = container.GetService<IConfiguration>();
                    var clientSecretCredential = new ClientSecretCredential(
                        configuration.GetValue<string>("TENANT"),
                        configuration.GetValue<string>("ResourceServicePrincipalAppId"),
                        configuration.GetValue<string>("ClientSecret"));

                    return new GraphServiceClient(clientSecretCredential, new[] { "https://graph.microsoft.com/.default" });
                },
                Lifestyle.Scoped);
        }
    }
}
