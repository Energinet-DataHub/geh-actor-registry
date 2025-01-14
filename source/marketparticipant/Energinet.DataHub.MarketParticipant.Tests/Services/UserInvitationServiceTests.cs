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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Energinet.DataHub.MarketParticipant.Tests.Handlers;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class UserInvitationServiceTests
{
    private static readonly Organization _validOrganization = new(
        new OrganizationId(Guid.NewGuid()),
        "Organization Name",
        MockedBusinessRegisterIdentifier.New(),
        new Address(null, null, null, null, "DK"),
        [new OrganizationDomain("test.datahub.dk")],
        OrganizationStatus.Active);

    private static readonly Actor _validActor = new(
        new ActorId(Guid.NewGuid()),
        _validOrganization.Id,
        new ExternalActorId(Guid.NewGuid()),
        new MockedGln(),
        ActorStatus.Active,
        new ActorMarketRole(EicFunction.BalanceResponsibleParty),
        new ActorName("Actor Name"),
        null);

    private readonly UserInvitation _validInvitation = new(
        new RandomlyGeneratedEmailAddress(),
        new InvitationUserDetails(
            "John",
            "Doe",
            new PhoneNumber("+45 00000000"),
            new SmsAuthenticationMethod(new PhoneNumber("+45 00000000"))),
        _validActor,
        [
            new UserRole(
                new UserRoleId(Guid.NewGuid()),
                "fake_value",
                "fake_value",
                UserRoleStatus.Active,
                Array.Empty<PermissionId>(),
                EicFunction.BalanceResponsibleParty),
        ]);

    private readonly UserId _validInvitedByUserId = new(Guid.NewGuid());

    [Fact]
    public async Task InviteUserAsync_NoUser_CreatesAndSavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
        VerifyUserIdentityCreatedCorrectly(userIdentityRepositoryMock);
        VerifyUserInvitationExpirationCorrectly(userRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_NewUserWithEmailOutsideOfOrganizationDomain_DoesNotCreateUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        organizationDomainValidationServiceMock
            .Setup(organizationDomainValidationService =>
                organizationDomainValidationService.ValidateUserEmailInsideOrganizationDomainsAsync(
                    It.IsAny<Actor>(),
                    _validInvitation.Email))
            .ThrowsAsync(new ValidationException());

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        var invitation = _validInvitation;

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => target.InviteUserAsync(invitation, _validInvitedByUserId));

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.IsAny<User>()),
            Times.Never);

        userIdentityRepositoryMock.Verify(
            userIdentityRepository => userIdentityRepository.CreateAsync(It.IsAny<UserIdentity>()),
            Times.Never);

        emailEventRepositoryMock.Verify(
            emailEventRepository => emailEventRepository.InsertAsync(It.IsAny<EmailEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task InviteUserAsync_ExistingUserWithEmailOutsideOfOrganizationDomain_Success()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        organizationDomainValidationServiceMock
            .Setup(organizationDomainValidationService =>
                organizationDomainValidationService.ValidateUserEmailInsideOrganizationDomainsAsync(
                    It.IsAny<Actor>(),
                    _validInvitation.Email))
            .ThrowsAsync(new ValidationException());

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                Array.Empty<UserRoleAssignment>(),
                null,
                null,
                null));

        var userIdentity = new UserIdentity(
            externalId,
            _validInvitation.Email,
            UserIdentityStatus.Active,
            _validInvitation.InvitationUserDetails!.FirstName,
            _validInvitation.InvitationUserDetails.LastName,
            _validInvitation.InvitationUserDetails.PhoneNumber,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(userIdentity);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(externalId))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        // Act + Assert
        await target.InviteUserAsync(_validInvitation, _validInvitedByUserId);

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.IsAny<User>()),
            Times.Once);

        userIdentityRepositoryMock.Verify(
            userIdentityRepository => userIdentityRepository.CreateAsync(It.IsAny<UserIdentity>()),
            Times.Never);

        emailEventRepositoryMock.Verify(
            emailEventRepository => emailEventRepository.InsertAsync(It.IsAny<EmailEvent>()),
            Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_HasUserIdentityButNotLocalUser_CreatesAndSavesUser()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(new UserIdentity(
                new ExternalUserId(Guid.NewGuid()),
                _validInvitation.Email,
                UserIdentityStatus.Active,
                _validInvitation.InvitationUserDetails!.FirstName,
                _validInvitation.InvitationUserDetails.LastName,
                _validInvitation.InvitationUserDetails.PhoneNumber,
                DateTimeOffset.UtcNow,
                AuthenticationMethod.Undetermined,
                new Mock<IList<LoginIdentity>>().Object));

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
        VerifyUserIdentityCreatedCorrectly(userIdentityRepositoryMock);
        VerifyUserInvitationExpirationCorrectly(userRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_HasUser_SavesPermissionsOnly()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                Array.Empty<UserRoleAssignment>(),
                null,
                null,
                null));

        var userIdentity = new UserIdentity(
            externalId,
            _validInvitation.Email,
            UserIdentityStatus.Active,
            _validInvitation.InvitationUserDetails!.FirstName,
            _validInvitation.InvitationUserDetails.LastName,
            _validInvitation.InvitationUserDetails.PhoneNumber,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(userIdentity);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(externalId))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        VerifyUserCreatedCorrectly(userRepositoryMock);
    }

    [Fact]
    public async Task InviteUserAsync_HasUserWithUserRoles_AddsNewUserRoles()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                [new UserRoleAssignment(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid()))],
                null,
                null,
                null));

        var userIdentity = new UserIdentity(
            externalId,
            _validInvitation.Email,
            UserIdentityStatus.Active,
            _validInvitation.InvitationUserDetails!.FirstName,
            _validInvitation.InvitationUserDetails.LastName,
            _validInvitation.InvitationUserDetails.PhoneNumber,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(userIdentity);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(externalId))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        var expectedRole = _validInvitation.AssignedRoles.Single();
        var expectedActor = _validInvitation.AssignedActor;
        var expectedAssignment = new UserRoleAssignment(expectedActor.Id, expectedRole.Id);

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.Is<User>(user =>
                user.RoleAssignments.Count == 2 &&
                user.RoleAssignments.Contains(expectedAssignment))),
            Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_HasUser_SendsInvitationEmail()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var externalId = new ExternalUserId(Guid.NewGuid());

        userRepositoryMock
            .Setup(userRepository => userRepository.GetAsync(externalId))
            .ReturnsAsync(new User(
                new UserId(Guid.NewGuid()),
                new ActorId(Guid.Empty),
                externalId,
                [new UserRoleAssignment(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid())),],
                null,
                null,
                null));

        var userIdentity = new UserIdentity(
            externalId,
            _validInvitation.Email,
            UserIdentityStatus.Active,
            _validInvitation.InvitationUserDetails!.FirstName,
            _validInvitation.InvitationUserDetails.LastName,
            _validInvitation.InvitationUserDetails.PhoneNumber,
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new Mock<IList<LoginIdentity>>().Object);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(_validInvitation.Email))
            .ReturnsAsync(userIdentity);

        userIdentityRepositoryMock
            .Setup(userIdentityRepository => userIdentityRepository.GetAsync(externalId))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        var invitation = _validInvitation;

        // Act
        await target.InviteUserAsync(invitation, _validInvitedByUserId);

        // Assert
        emailEventRepositoryMock.Verify(emailEventRepository => emailEventRepository.InsertAsync(
            It.Is<EmailEvent>(emailEvent =>
                emailEvent.Email == _validInvitation.Email &&
                emailEvent.EmailTemplate.TemplateId == EmailTemplateId.UserAssignedToActor)));
    }

    [Fact]
    public async Task ReInviteUserAsync_NoUser_Throws()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var mockedUser = TestPreparationModels.MockedUser(Guid.NewGuid());

        userRepositoryMock
            .Setup(u => u.GetAsync(mockedUser.Id))
            .ReturnsAsync((User?)null);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        // act + assert
        var actual = await Assert.ThrowsAsync<NotFoundValidationException>(() =>
            target.ReInviteUserAsync(mockedUser.Id, _validInvitedByUserId));

        Assert.Contains(mockedUser.Id.ToString(), actual.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ReInviteUserAsync_LocksBeforeRetrievingUser()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();
        entityLock.Setup(x => x.LockAsync(LockableEntity.User)).ThrowsAsync(new InvalidOperationException("Break the flow"));

        var mockedUser = TestPreparationModels.MockedUser(Guid.NewGuid());

        userRepositoryMock
            .Setup(u => u.GetAsync(mockedUser.Id))
            .ReturnsAsync((User?)null);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        // act + assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            target.ReInviteUserAsync(mockedUser.Id, _validInvitedByUserId));

        userRepositoryMock.Verify(x => x.GetAsync(It.IsAny<UserId>()), Times.Never);
        userIdentityRepositoryMock.Verify(x => x.GetAsync(It.IsAny<ExternalUserId>()), Times.Never);
    }

    [Fact]
    public async Task ReInviteUserAsync_NoUserIdentity_Throws()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var mockedUser = TestPreparationModels.MockedUser(Guid.NewGuid());

        userRepositoryMock
            .Setup(u => u.GetAsync(mockedUser.Id))
            .ReturnsAsync(mockedUser);

        userIdentityRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync((UserIdentity?)null);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        // act + assert
        await Assert.ThrowsAsync<NotFoundValidationException>(() =>
            target.ReInviteUserAsync(mockedUser.Id, _validInvitedByUserId));

        userIdentityRepositoryMock.Verify(e => e.GetAsync(mockedUser.ExternalId), Times.Once);
    }

    [Fact]
    public async Task ReInviteUserAsync_UserStatusNotInviteExpired_Throws()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogEntryRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogEntryRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var user = new User(
            new UserId(Guid.NewGuid()),
            new ActorId(Guid.NewGuid()),
            new ExternalUserId(Guid.NewGuid()),
            [new UserRoleAssignment(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid()))],
            null,
            null,
            null);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new EmailAddress("test@test.dk"),
            UserIdentityStatus.Active,
            "FirstName",
            "LastName",
            new PhoneNumber("+45 12345678"),
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());

        userRepositoryMock
            .Setup(u => u.GetAsync(user.Id))
            .ReturnsAsync(new User(new ActorId(Guid.NewGuid()), new SharedUserReferenceId(), new ExternalUserId(Guid.NewGuid())));

        userIdentityRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogEntryRepository.Object,
            userIdentityAuditLogEntryRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        // act + assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            target.ReInviteUserAsync(user.Id, _validInvitedByUserId));
    }

    [Fact]
    public async Task ReInviteUserAsync_CompleteReInvite_Success()
    {
        // arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var userIdentityRepositoryMock = new Mock<IUserIdentityRepository>();
        var emailEventRepositoryMock = new Mock<IEmailEventRepository>();
        var actorRepositoryMock = new Mock<IActorRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        var organizationDomainValidationServiceMock = new Mock<IOrganizationDomainValidationService>();
        var userInviteAuditLogRepository = new Mock<IUserInviteAuditLogRepository>();
        var userIdentityAuditLogRepository = new Mock<IUserIdentityAuditLogRepository>();
        var userStatusCalculator = new UserStatusCalculator();
        var entityLock = new Mock<IEntityLock>();

        var user = new User(
            new UserId(Guid.NewGuid()),
            _validActor.Id,
            new ExternalUserId(Guid.NewGuid()),
            [new UserRoleAssignment(new ActorId(Guid.NewGuid()), new UserRoleId(Guid.NewGuid()))],
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            null);

        var userIdentity = new UserIdentity(
            new ExternalUserId(Guid.NewGuid()),
            new EmailAddress("test@test.dk"),
            UserIdentityStatus.Active,
            "FirstName",
            "LastName",
            new PhoneNumber("+45 12345678"),
            DateTimeOffset.UtcNow,
            AuthenticationMethod.Undetermined,
            new List<LoginIdentity>());

        userRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        userIdentityRepositoryMock
            .Setup(u => u.GetAsync(It.IsAny<ExternalUserId>()))
            .ReturnsAsync(userIdentity);

        var target = new UserInvitationService(
            userRepositoryMock.Object,
            userIdentityRepositoryMock.Object,
            emailEventRepositoryMock.Object,
            actorRepositoryMock.Object,
            organizationRepositoryMock.Object,
            organizationDomainValidationServiceMock.Object,
            userInviteAuditLogRepository.Object,
            userIdentityAuditLogRepository.Object,
            UnitOfWorkProviderMock.Create(),
            userStatusCalculator,
            entityLock.Object);

        organizationRepositoryMock
            .Setup(organizationRepository => organizationRepository.GetAsync(_validOrganization.Id))
            .ReturnsAsync(_validOrganization);

        actorRepositoryMock
            .Setup(actorRepository => actorRepository.GetAsync(_validActor.Id))
            .ReturnsAsync(_validActor);

        await target.ReInviteUserAsync(user.Id, _validInvitedByUserId);

        // act + assert
        userIdentityRepositoryMock
            .Verify(u => u.EnableUserAccountAsync(userIdentity.Id), Times.Once);
        userRepositoryMock
            .Verify(u => u.AddOrUpdateAsync(user), Times.Once);
        emailEventRepositoryMock
            .Verify(e => e.InsertAsync(It.IsAny<EmailEvent>()), Times.Once);
        userInviteAuditLogRepository
            .Verify(a => a.AuditAsync(user.Id, new AuditIdentity(_validInvitedByUserId.Value), user.AdministratedBy), Times.Once);
    }

    private static void VerifyUserInvitationExpirationCorrectly(Mock<IUserRepository> userRepositoryMock)
    {
        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.Is<User>(user => user.InvitationExpiresAt != null && user.InvitationExpiresAt > DateTimeOffset.UtcNow)),
            Times.Once);
    }

    private void VerifyUserCreatedCorrectly(Mock<IUserRepository> userRepositoryMock)
    {
        var expectedRole = _validInvitation.AssignedRoles.Single();
        var expectedActor = _validInvitation.AssignedActor;
        var expectedAssignment = new UserRoleAssignment(expectedActor.Id, expectedRole.Id);

        userRepositoryMock.Verify(
            userRepository => userRepository.AddOrUpdateAsync(It.Is<User>(user => user.RoleAssignments.Single() == expectedAssignment)),
            Times.Once);
    }

    private void VerifyUserIdentityCreatedCorrectly(Mock<IUserIdentityRepository> userIdentityRepositoryMock)
    {
        userIdentityRepositoryMock.Verify(
            userIdentityRepository => userIdentityRepository.CreateAsync(It.Is<UserIdentity>(userIdentity =>
                userIdentity.Email == _validInvitation.Email &&
                userIdentity.PhoneNumber == _validInvitation.InvitationUserDetails!.PhoneNumber &&
                userIdentity.FirstName == _validInvitation.InvitationUserDetails.FirstName &&
                userIdentity.LastName == _validInvitation.InvitationUserDetails.LastName)),
            Times.Once);
    }
}
