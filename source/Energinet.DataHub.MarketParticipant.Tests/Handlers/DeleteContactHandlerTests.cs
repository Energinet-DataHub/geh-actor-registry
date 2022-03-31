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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Contact;
using Energinet.DataHub.MarketParticipant.Application.Handlers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers
{
    [UnitTest]
    public sealed class DeleteContactHandlerTests
    {
        private readonly Address _validAddress = new(
            "test Street",
            "1",
            "1111",
            "Test City",
            "Test Country");

        private readonly CVRNumber _validCvr = new("12345678");

        [Fact]
        public async Task Handle_NullArgument_ThrowsException()
        {
            // Arrange
            var target = new DeleteContactHandler(
                new Mock<IOrganizationExistsHelperService>().Object,
                new Mock<IContactRepository>().Object);

            // Act + Assert
            await Assert
                .ThrowsAsync<ArgumentNullException>(() => target.Handle(null!, CancellationToken.None))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_NoContact_DoesNothing()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var contactRepository = new Mock<IContactRepository>();
            var target = new DeleteContactHandler(
                organizationExistsHelperService.Object,
                contactRepository.Object);

            var organizationId = new OrganizationId(Guid.NewGuid());
            var contactId = new ContactId(Guid.NewGuid());

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId.Value))
                .ReturnsAsync(new Organization("fake_value", _validCvr, _validAddress));

            contactRepository
                .Setup(x => x.GetAsync(contactId))
                .ReturnsAsync((Contact?)null);

            var command = new DeleteContactCommand(organizationId.Value, contactId.Value);

            // Act + Assert
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task Handle_OneContact_IsDeleted()
        {
            // Arrange
            var organizationExistsHelperService = new Mock<IOrganizationExistsHelperService>();
            var contactRepository = new Mock<IContactRepository>();
            var target = new DeleteContactHandler(
                organizationExistsHelperService.Object,
                contactRepository.Object);

            var organizationId = new OrganizationId(Guid.NewGuid());
            var contactId = new ContactId(Guid.NewGuid());

            organizationExistsHelperService
                .Setup(x => x.EnsureOrganizationExistsAsync(organizationId.Value))
                .ReturnsAsync(new Organization("fake_value", _validCvr, _validAddress));

            var contactToDelete = new Contact(
                contactId,
                organizationId,
                "fake_value",
                ContactCategory.EndOfSupply,
                new EmailAddress("fake@value"),
                null);

            contactRepository
                .Setup(x => x.GetAsync(contactId))
                .ReturnsAsync(contactToDelete);

            var command = new DeleteContactCommand(organizationId.Value, contactId.Value);

            // Act
            await target.Handle(command, CancellationToken.None).ConfigureAwait(false);

            // Assert
            contactRepository.Verify(x => x.RemoveAsync(contactToDelete), Times.Once);
        }
    }
}
