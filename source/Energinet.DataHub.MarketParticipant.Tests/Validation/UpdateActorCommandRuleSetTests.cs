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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation
{
    [UnitTest]
    public sealed class UpdateActorCommandRuleSetTests
    {
        private const ActorStatus ValidStatus = ActorStatus.Active;

        private static readonly Guid _validOrganizationId = Guid.NewGuid();
        private static readonly Guid _validActorId = Guid.NewGuid();

        [Fact]
        public async Task Validate_ActorDto_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateActorCommand.ChangeActor);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, null!);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_OrganizationId_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateActorCommand.OrganizationId);

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(Guid.Empty, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_ActorId_ValidatesProperty()
        {
            // Arrange
            const string propertyName = nameof(UpdateActorCommand.ActorId);

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, Guid.Empty, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData(ActorStatus.Active, true)]
        [InlineData(ActorStatus.Inactive, true)]
        [InlineData(ActorStatus.Passive, true)]
        [InlineData(ActorStatus.New, true)]
        public async Task Validate_ActorStatus_ValidatesProperty(ActorStatus value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.Status)}";

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var actorDto = new ChangeActorDto(value, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }

        [Fact]
        public async Task Validate_MarketRole_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}";

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), null!);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NoMarketRoles_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}";

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), null!);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NullMarketRole_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0]";

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), new ActorMarketRoleDto[] { null! });

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData(EicFunction.GridAccessProvider, true)]
        [InlineData(EicFunction.ProductionResponsibleParty, true)]
        [InlineData(EicFunction.Consumer, true)]
        public async Task Validate_MarketRoleFunction_ValidatesProperty(EicFunction value, bool isValid)
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].{nameof(ActorMarketRoleDto.EicFunction)}";

            var validMeteringPointTypes = new[] { MeteringPointType.D05NetProduction };
            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), validMeteringPointTypes) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(value, validGridAreas) };

            var actorDto = new ChangeActorDto(
                ValidStatus,
                new ActorNameDto("fake_name"),
                validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }

        [Fact]
        public async Task Validate_MeteringPoints_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].GridAreas[0].MeteringPointTypes";

            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), null!) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_NoMeteringPoints_ValidatesProperty()
        {
            // Arrange
            const string propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].GridAreas[0].MeteringPointTypes";

            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), Array.Empty<MeteringPointType>()) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var actorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, actorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Fact]
        public async Task Validate_MeteringPointType_ValidatesProperty()
        {
            // Arrange
            var propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].GridAreas[0].MeteringPointTypes";

            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), null!) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var changeActorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, changeActorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
        }

        [Theory]
        [InlineData(MeteringPointType.D06SupplyToGrid, true)]
        [InlineData(MeteringPointType.D07ConsumptionFromGrid, true)]
        [InlineData(MeteringPointType.D09OwnProduction, true)]
        public async Task Validate_MeteringPointTypes_ValidatesProperty(MeteringPointType value, bool isValid)
        {
            // Arrange
            var propertyName = $"{nameof(UpdateActorCommand.ChangeActor)}.{nameof(ChangeActorDto.MarketRoles)}[0].GridAreas[0].MeteringPointTypes[0]";

            var validGridAreas = new List<ActorGridAreaDto> { new(Guid.NewGuid(), new[] { value }) };
            var validMarketRoles = new List<ActorMarketRoleDto> { new(EicFunction.CapacityTrader, validGridAreas) };

            var changeActorDto = new ChangeActorDto(ValidStatus, new ActorNameDto("fake_name"), validMarketRoles);

            var target = new UpdateActorCommandRuleSet();
            var command = new UpdateActorCommand(_validOrganizationId, _validActorId, changeActorDto);

            // Act
            var result = await target.ValidateAsync(command).ConfigureAwait(false);

            // Assert
            if (isValid)
            {
                Assert.True(result.IsValid);
                Assert.DoesNotContain(propertyName, result.Errors.Select(x => x.PropertyName));
            }
            else
            {
                Assert.False(result.IsValid);
                Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
            }
        }
    }
}
