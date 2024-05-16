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
using System.Net.Http;
using Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.CertificateSynchronization.Extensions.DependencyInjection;

internal static class HttpClientExtensions
{
    public static IServiceCollection AddCertificatesHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient<HttpClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CertificateSynchronizationOptions>>();
            httpClient.BaseAddress = new Uri($"https://management.azure.com{options.Value.ApimServiceName}/certificates/");
        });
        return services;
    }
}
