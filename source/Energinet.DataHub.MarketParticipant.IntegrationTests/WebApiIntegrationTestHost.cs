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
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Energinet.DataHub.MarketParticipant.Common.Configuration;
using Energinet.DataHub.MarketParticipant.EntryPoint.WebApi;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests;

public sealed class WebApiIntegrationTestHost : IAsyncDisposable
{
    private readonly Startup _startup;

    private WebApiIntegrationTestHost(IConfiguration configuration)
    {
        _startup = new Startup(configuration);
    }

    public static Task<WebApiIntegrationTestHost> InitializeAsync(MarketParticipantDatabaseFixture databaseFixture)
    {
        ArgumentNullException.ThrowIfNull(databaseFixture);

        var configuration = BuildConfig(databaseFixture.DatabaseManager.ConnectionString);
        var host = new WebApiIntegrationTestHost(configuration);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(configuration);
        host._startup.ConfigureServices(serviceCollection);
        serviceCollection
            .BuildServiceProvider()
            .UseSimpleInjector(host._startup.Container, o => o.Container.Options.EnableAutoVerification = false);

        host._startup.Container.Options.AllowOverridingRegistrations = true;
        InitUserIdProvider(host._startup.Container);
        return Task.FromResult(host);
    }

    public Scope BeginScope()
    {
        return AsyncScopedLifestyle.BeginScope(_startup.Container);
    }

    public async ValueTask DisposeAsync()
    {
        await _startup.DisposeAsync().ConfigureAwait(false);
    }

    private static IConfiguration BuildConfig(string dbConnectionString)
    {
        KeyValuePair<string, string>[] keyValuePairs =
        {
            new(Settings.SqlDbConnectionString.Key, dbConnectionString),
            new(Settings.ServiceBusTopicName.Key, "fake_value"),
            new(Settings.ExternalOpenIdUrl.Key, "fake_value"),
            new(Settings.BackendAppId.Key, "fake_value"),
            new(Settings.InternalOpenIdUrl.Key, "fake_value"),
            new(Settings.B2CTenant.Key, Guid.Empty.ToString()),
            new(Settings.B2CServicePrincipalNameId.Key, "fake_value"),
            new(Settings.B2CServicePrincipalNameSecret.Key, "fake_value")
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(keyValuePairs)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void InitUserIdProvider(Container container)
    {
        var mockUser = new FrontendUser(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false);
        var userIdProvider = new Mock<IUserContext<FrontendUser>>();
        userIdProvider.Setup(x => x.CurrentUser).Returns(mockUser);
        container.Register(() => userIdProvider.Object, Lifestyle.Singleton);
    }
}
