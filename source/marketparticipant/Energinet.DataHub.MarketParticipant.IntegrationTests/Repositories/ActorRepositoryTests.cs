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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Common;
using Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;
using Moq;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Repositories;

[Collection(nameof(IntegrationTestCollectionFixture))]
[IntegrationTest]
public sealed class ActorRepositoryTests
{
    private readonly MarketParticipantDatabaseFixture _fixture;
    private readonly IEntityLock _lock;

    public ActorRepositoryTests(MarketParticipantDatabaseFixture fixture)
    {
        _fixture = fixture;
        _lock = new Mock<IEntityLock>().Object;
    }

    [Fact]
    public async Task AddOrUpdateAsync_ResultsInCreateAndNoLockIsTaken_Throws()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var actorRepository = new ActorRepository(context, new EntityLock(context));
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));

        // act, assert
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => actorRepository.AddOrUpdateAsync(actor));
        Assert.Equal("Actor lock is required.", actual.Message);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ResultsInCreateAndLockIsTaken_Succeeds()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();

        var uowProvider = new UnitOfWorkProvider(context);
        await using var uow = await uowProvider.NewUnitOfWorkAsync();

        var entityLock = new EntityLock(context);
        await entityLock.LockAsync(LockableEntity.Actor);

        var actorRepository = new ActorRepository(context, entityLock);
        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));

        // act
        var actual = await actorRepository.AddOrUpdateAsync(actor);

        // assert
        Assert.NotNull(actual);
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneActor_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);
        var actorRepository2 = new ActorRepository(context2, _lock);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));

        // Act
        var result = await actorRepository.AddOrUpdateAsync(actor);
        var actual = await actorRepository2.GetAsync(result.Value);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ActorWithMarkedRolesAndGridAreas_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);
        var actorRepository2 = new ActorRepository(context2, _lock);
        var gridAreaRepository = new GridAreaRepository(context2);

        var gridAreaId = await gridAreaRepository.AddOrUpdateAsync(new GridArea(
            new GridAreaName("fake_value"),
            new GridAreaCode("000"),
            PriceAreaCode.Dk1,
            GridAreaType.Distribution,
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue));

        var organization = await _fixture.PrepareOrganizationAsync();
        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"));

        actor.AddMarketRole(new ActorMarketRole(EicFunction.BalanceResponsibleParty, new[]
        {
            new ActorGridArea(gridAreaId, new[] { MeteringPointType.D01VeProduction })
        }));

        // Act
        var result = await actorRepository.AddOrUpdateAsync(actor);
        var actual = await actorRepository2.GetAsync(result.Value);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
        Assert.Equal(actor.MarketRoles.Single().Function, actual.MarketRoles.Single().Function);
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneActor_WithCertificateCredentials_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);
        var actorRepository2 = new ActorRepository(context2, _lock);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actorCredentials = new ActorCertificateCredentials(
            "12345678",
            "secret",
            DateTime.UtcNow.AddYears(1).ToInstant());

        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"))
        {
            Credentials = actorCredentials
        };

        // Act
        var result = await actorRepository.AddOrUpdateAsync(actor);
        var actual = await actorRepository2.GetAsync(result.Value);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Credentials);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
        Assert.IsType<ActorCertificateCredentials>(actual.Credentials);
        Assert.Equal(actorCredentials.KeyVaultSecretIdentifier, (actual.Credentials as ActorCertificateCredentials)?.KeyVaultSecretIdentifier);
        Assert.Equal(actorCredentials.CertificateThumbprint, (actual.Credentials as ActorCertificateCredentials)?.CertificateThumbprint);
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneActor_WithCertificateCredentials_ReuseCertificate_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context1 = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        await using var context3 = _fixture.DatabaseManager.CreateDbContext();
        await using var context4 = _fixture.DatabaseManager.CreateDbContext();

        var actorRepository1 = new ActorRepository(context1, _lock);
        var actorRepository2 = new ActorRepository(context2, _lock);
        var actorRepository3 = new ActorRepository(context3, _lock);
        var actorRepository4 = new ActorRepository(context4, _lock);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actorCredentials = new ActorCertificateCredentials(
            "1234567899",
            "secret",
            DateTime.UtcNow.AddYears(1).ToInstant());

        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"))
        {
            Credentials = actorCredentials
        };

        // Act
        var createdActorIdWithCertificate = await actorRepository1.AddOrUpdateAsync(actor);

        var actorToClearCertificate = await actorRepository2.GetAsync(createdActorIdWithCertificate.Value);
        actorToClearCertificate!.Credentials = null;
        await actorRepository2.AddOrUpdateAsync(actorToClearCertificate);

        var actorToReUseCertificate = await actorRepository3.GetAsync(createdActorIdWithCertificate.Value);
        actorToReUseCertificate!.Credentials = actorCredentials;
        await actorRepository3.AddOrUpdateAsync(actorToReUseCertificate);

        var actual = await actorRepository4.GetAsync(createdActorIdWithCertificate.Value);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Credentials);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
        Assert.IsType<ActorCertificateCredentials>(actual.Credentials);
        Assert.Equal(actorCredentials.KeyVaultSecretIdentifier, (actual.Credentials as ActorCertificateCredentials)?.KeyVaultSecretIdentifier);
        Assert.Equal(actorCredentials.CertificateThumbprint, (actual.Credentials as ActorCertificateCredentials)?.CertificateThumbprint);
        Assert.True((actual.Credentials as ActorCertificateCredentials)?.ExpirationDate > SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(364)));
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneActor_WithClientSecretCredentials_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);
        var actorRepository2 = new ActorRepository(context2, _lock);

        var organization = await _fixture.PrepareOrganizationAsync();
        var endDate = DateTime.UtcNow.AddYears(1).ToInstant();
        var actorClientSecretCredentials = new ActorClientSecretCredentials(
            Guid.NewGuid(),
            Guid.NewGuid(),
            endDate);

        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"))
        {
            Credentials = actorClientSecretCredentials
        };

        actor.ExternalActorId = new ExternalActorId(actorClientSecretCredentials.ClientId);

        // Act
        var result = await actorRepository.AddOrUpdateAsync(actor);
        var actual = await actorRepository2.GetAsync(result.Value);

        // Assert
        Assert.NotNull(actual);
        Assert.NotNull(actual.Credentials);
        Assert.Equal(actor.OrganizationId, actual.OrganizationId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
        Assert.IsType<ActorClientSecretCredentials>(actual.Credentials);
        Assert.Equal(actorClientSecretCredentials.SecretIdentifier, (actual.Credentials as ActorClientSecretCredentials)?.SecretIdentifier);
        Assert.Equal(endDate, (actual.Credentials as ActorClientSecretCredentials)?.ExpirationDate);
    }

    [Fact]
    public async Task AddOrUpdateAsync_OneActor_IdenticalThumbprintCredentials_HasError()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        await using var context2 = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);
        var actorRepository2 = new ActorRepository(context2, _lock);

        var organization = await _fixture.PrepareOrganizationAsync();
        var actorCertificateCredentials = new ActorCertificateCredentials("123456784", "secret", DateTime.UtcNow.AddYears(1).ToInstant());
        var actorCertificateCredentials2 = new ActorCertificateCredentials("123456784", "secret2", DateTime.UtcNow.AddYears(1).ToInstant());

        var actor = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"))
        {
            Credentials = actorCertificateCredentials
        };
        var actor2 = new Actor(new OrganizationId(organization.Id), new MockedGln(), new ActorName("Mock"))
        {
            Credentials = actorCertificateCredentials2
        };

        // Act
        var resultOk = await actorRepository.AddOrUpdateAsync(actor);
        var resultError = await actorRepository2.AddOrUpdateAsync(actor2);

        // Assert
        Assert.NotNull(resultOk);
        Assert.NotNull(resultError);
        Assert.Null(resultOk.Error);
        Assert.Equal(ActorError.ThumbprintCredentialsConflict, resultError.Error);
    }

    [Fact]
    public async Task GetActorsAsync_All_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);

        var actor1 = await _fixture.PrepareActorAsync();
        var actor2 = await _fixture.PrepareActorAsync();

        // Act
        var actual = (await actorRepository.GetActorsAsync()).ToList();

        // Assert
        Assert.NotEmpty(actual);
        Assert.Contains(actual, a => a.Id.Value == actor1.Id);
        Assert.Contains(actual, a => a.Id.Value == actor2.Id);
    }

    [Fact]
    public async Task GetActorsAsync_ById_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);

        var actor1 = await _fixture.PrepareActorAsync();
        var actor2 = await _fixture.PrepareActorAsync();

        // Act
        var actual = await actorRepository.GetActorsAsync(new[] { new ActorId(actor1.Id), new ActorId(actor2.Id) });

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(2, actual.Count());
    }

    [Fact]
    public async Task GetActorsAsync_ForOrganization_CanReadBack()
    {
        // Arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);

        var actor1 = await _fixture.PrepareActorAsync();
        await _fixture.PrepareActorAsync();

        // Act
        var actual = await actorRepository.GetActorsAsync(new OrganizationId(actor1.OrganizationId));

        // Assert
        Assert.NotNull(actual);
        Assert.Single(actual);
    }

    [Fact]
    public async Task GetActorTokenDataAsync_ByActorId_CanReadBack()
    {
        // arrange
        await using var host = await WebApiIntegrationTestHost.InitializeAsync(_fixture);
        await using var scope = host.BeginScope();
        await using var context = _fixture.DatabaseManager.CreateDbContext();
        var actorRepository = new ActorRepository(context, _lock);

        var gridArea = await _fixture.PrepareGridAreaAsync();

        var marketRole = new MarketRoleEntity
        {
            Function = EicFunction.GridAccessProvider,
            GridAreas =
            {
                new MarketRoleGridAreaEntity
                {
                    GridAreaId = gridArea.Id,
                },
            },
        };

        var actor = await _fixture.PrepareActorAsync(
            TestPreparationEntities.ValidOrganization,
            TestPreparationEntities.ValidActiveActor,
            marketRole);

        // act
        var actual = await actorRepository.GetActorTokenDataAsync(new ActorId(actor.Id));

        // assert
        Assert.NotNull(actual);
        Assert.Equal(actor.Id, actual.ActorId);
        Assert.Equal(actor.ActorNumber, actual.ActorNumber);
        Assert.Equal(marketRole.Function, actual.MarketRoles.Single().Function);
        Assert.Equal(gridArea.Code, actual.MarketRoles.Single().GridAreas.Single().GridAreaCode);
    }
}
