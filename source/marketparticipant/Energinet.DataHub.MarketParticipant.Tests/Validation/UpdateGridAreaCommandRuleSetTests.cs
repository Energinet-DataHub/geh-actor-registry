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
using Energinet.DataHub.MarketParticipant.Application.Commands.GridAreas;
using Energinet.DataHub.MarketParticipant.Application.Validation;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Validation;

[UnitTest]
public sealed class UpdateGridAreaCommandRuleSetTests
{
    [Fact]
    public async Task Validate_Id_ValidatesProperty()
    {
        // Arrange
        const string propertyName = nameof(UpdateGridAreaCommand.Id);

        var updateGridAreaDto = new ChangeGridAreaDto("fake_value");

        var target = new UpdateGridAreaCommandRuleSet();
        var command = new UpdateGridAreaCommand(Guid.Empty, updateGridAreaDto);

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
    [InlineData("ValidName", true)]
    [InlineData("Valid Name", true)]
    [InlineData("50aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("51aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaX", false)]
    public async Task Validate_Name_ValidatesProperty(string? newName, bool isValid)
    {
        // Arrange
        var propertyName = $"{nameof(UpdateGridAreaCommand.GridAreaDto)}.{nameof(ChangeGridAreaDto.Name)}";
        var gridAreaId = Guid.NewGuid();

        var updateGridAreaDto = new ChangeGridAreaDto(newName!);

        var target = new UpdateGridAreaCommandRuleSet();
        var command = new UpdateGridAreaCommand(gridAreaId, updateGridAreaDto);

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
