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
using Energinet.DataHub.MarketParticipant.Application.Commands.Organizations;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation;

[UnitTest]
public sealed class UpdateOrganizationCommandRuleSetTests
{
    private const string ValidName = "Company Name";

    private static readonly Guid _validOrganizationId = Guid.NewGuid();

    [Fact]
    public async Task Validate_OrganizationId_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(UpdateOrganizationCommand.OrganizationId);

        var organizationDto = new ChangeOrganizationDto(
            ValidName,
            "Active",
            ["testDomain.dk"]);

        var target = new UpdateOrganizationCommandRuleSet();
        var command = new UpdateOrganizationCommand(Guid.Empty, organizationDto);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Fact]
    public async Task Validate_OrganizationDto_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(UpdateOrganizationCommand.Organization);

        var target = new UpdateOrganizationCommandRuleSet();
        var command = new UpdateOrganizationCommand(_validOrganizationId, null!);

        // Act
        var result = await target.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(propertyName, result.Errors.Select(x => x.PropertyName));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("Some Name", true)]
    [InlineData("Maximum looooooooooooooooooooooooooooooooooooooong", true)]
    [InlineData("Toooooo loooooooooooooooooooooooooooooooooooooooong", false)]
    public async Task Validate_OrganizationName_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        var propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ChangeOrganizationDto.Name)}";

        var organizationDto = new ChangeOrganizationDto(
            value!,
            "Active",
            ["testDomain.dk"]);

        var target = new UpdateOrganizationCommandRuleSet();
        var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

        // Act
        var result = await target.ValidateAsync(command);

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

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("1", false)]
    [InlineData("New", true)]
    [InlineData("Active", true)]
    [InlineData("Blocked", true)]
    [InlineData("Deleted", true)]
    public async Task Validate_OrganizationStatus_ValidatesProperty(string? status, bool isValid)
    {
        // Arrange
        const string propertyName = $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ChangeOrganizationDto.Status)}";

        var organizationDto = new ChangeOrganizationDto(
            "fake_value",
            status!,
            ["testDomain.dk"]);

        var target = new UpdateOrganizationCommandRuleSet();
        var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

        // Act
        var result = await target.ValidateAsync(command);

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

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("  ", false)]
    [InlineData("testdomain.dk", true)]
    public async Task Validate_OrganizationDomain_ValidatesProperty(string? value, bool isValid)
    {
        // Arrange
        var propertyName =
            $"{nameof(UpdateOrganizationCommand.Organization)}.{nameof(ChangeOrganizationDto.Domains)}";

        var organizationDto = new ChangeOrganizationDto(
            ValidName,
            "Active",
            [value!]);

        var target = new UpdateOrganizationCommandRuleSet();
        var command = new UpdateOrganizationCommand(_validOrganizationId, organizationDto);

        // Act
        var result = await target.ValidateAsync(command);

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
