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
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services;

public sealed class UserInvitationService : IUserInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IEmailEventRepository _emailEventRepository;
    private readonly IOrganizationDomainValidationService _organizationDomainValidationService;
    private readonly IUserInviteAuditLogEntryRepository _userInviteAuditLogEntryRepository;
    private readonly IUserIdentityAuditLogEntryRepository _userIdentityAuditLogEntryRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IUserStatusCalculator _userStatusCalculator;

    public UserInvitationService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IEmailEventRepository emailEventRepository,
        IOrganizationDomainValidationService organizationDomainValidationService,
        IUserInviteAuditLogEntryRepository userInviteAuditLogEntryRepository,
        IUserIdentityAuditLogEntryRepository userIdentityAuditLogEntryRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IUserStatusCalculator userStatusCalculator)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _emailEventRepository = emailEventRepository;
        _organizationDomainValidationService = organizationDomainValidationService;
        _userInviteAuditLogEntryRepository = userInviteAuditLogEntryRepository;
        _userIdentityAuditLogEntryRepository = userIdentityAuditLogEntryRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _userStatusCalculator = userStatusCalculator;
    }

    public async Task InviteUserAsync(UserInvitation invitation, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        var mailEventType = EmailEventType.UserAssignedToActor;

        var invitedUser = await GetUserAsync(invitation.Email).ConfigureAwait(false);

        if (invitedUser == null)
        {
            await _organizationDomainValidationService
                .ValidateUserEmailInsideOrganizationDomainsAsync(invitation.AssignedActor, invitation.Email)
                .ConfigureAwait(false);

            var sharedId = new SharedUserReferenceId();

            var userIdentity = new UserIdentity(
                sharedId,
                invitation.Email,
                invitation.FirstName,
                invitation.LastName,
                invitation.PhoneNumber,
                invitation.RequiredAuthentication);

            var userIdentityId = await _userIdentityRepository
                .CreateAsync(userIdentity)
                .ConfigureAwait(false);

            invitedUser = new User(invitation.AssignedActor.Id, sharedId, userIdentityId);
            invitedUser.ActivateUserExpiration();
            mailEventType = EmailEventType.UserInvite;
        }

        foreach (var assignedRole in invitation.AssignedRoles)
        {
            var assignment = new UserRoleAssignment(invitation.AssignedActor.Id, assignedRole.Id);
            invitedUser.RoleAssignments.Add(assignment);
        }

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var invitedUserId = await _userRepository
                .AddOrUpdateAsync(invitedUser)
                .ConfigureAwait(false);

            await _emailEventRepository
                .InsertAsync(new EmailEvent(invitation.Email, mailEventType))
                .ConfigureAwait(false);

            var auditIdentity = new AuditIdentity(invitationSentByUserId);

            await AuditLogUserIdentityAsync(invitedUserId, auditIdentity, invitation).ConfigureAwait(false);
            await AuditLogUserInviteAsync(invitedUserId, auditIdentity, invitation.AssignedActor.Id).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    public async Task ReInviteUserAsync(User user, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userIdentity = await _userIdentityRepository.GetAsync(user.ExternalId).ConfigureAwait(false);
        NotFoundValidationException.ThrowIfNull(userIdentity, $"The specified user identity {user.ExternalId} was not found.");

        var userStatus = _userStatusCalculator.CalculateUserStatus(user, userIdentity);
        if (userStatus != UserStatus.Invited && userStatus != UserStatus.InviteExpired)
        {
            throw new ValidationException($"The current user invitation for user {user.Id} is not expired and cannot be re-invited.");
        }

        user.ActivateUserExpiration();

        await _userIdentityRepository
            .EnableUserAccountAsync(userIdentity.Id)
            .ConfigureAwait(false);

        var uow = await _unitOfWorkProvider
            .NewUnitOfWorkAsync()
            .ConfigureAwait(false);

        await using (uow.ConfigureAwait(false))
        {
            var invitedUserId = await _userRepository
                .AddOrUpdateAsync(user)
                .ConfigureAwait(false);

            await _emailEventRepository
                .InsertAsync(new EmailEvent(userIdentity.Email, EmailEventType.UserInvite))
                .ConfigureAwait(false);

            await AuditLogUserInviteAsync(invitedUserId, new AuditIdentity(invitationSentByUserId), user.AdministratedBy).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    private async Task<User?> GetUserAsync(EmailAddress email)
    {
        var invitedIdentity = await _userIdentityRepository
            .GetAsync(email)
            .ConfigureAwait(false);

        return invitedIdentity != null
            ? await _userRepository.GetAsync(invitedIdentity.Id).ConfigureAwait(false)
            : null;
    }

    private Task AuditLogUserInviteAsync(UserId toUserId, AuditIdentity invitationSentBy, ActorId assignedActor)
    {
        var userInviteAuditLog = new UserInviteAuditLogEntry(
            toUserId,
            assignedActor,
            invitationSentBy,
            DateTimeOffset.UtcNow);

        return _userInviteAuditLogEntryRepository
            .InsertAuditLogEntryAsync(userInviteAuditLog);
    }

    private async Task AuditLogUserIdentityAsync(UserId invitedUserId, AuditIdentity invitationSentBy, UserInvitation invitation)
    {
        await _userIdentityAuditLogEntryRepository
            .InsertAuditLogEntryAsync(new UserIdentityAuditLogEntry(
                invitedUserId,
                invitation.FirstName,
                string.Empty,
                invitationSentBy,
                DateTimeOffset.UtcNow,
                UserIdentityAuditLogField.FirstName))
            .ConfigureAwait(false);

        await _userIdentityAuditLogEntryRepository
            .InsertAuditLogEntryAsync(new UserIdentityAuditLogEntry(
                invitedUserId,
                invitation.LastName,
                string.Empty,
                invitationSentBy,
                DateTimeOffset.UtcNow,
                UserIdentityAuditLogField.LastName))
            .ConfigureAwait(false);

        await _userIdentityAuditLogEntryRepository
            .InsertAuditLogEntryAsync(new UserIdentityAuditLogEntry(
                invitedUserId,
                invitation.PhoneNumber.Number,
                string.Empty,
                invitationSentBy,
                DateTimeOffset.UtcNow,
                UserIdentityAuditLogField.PhoneNumber))
            .ConfigureAwait(false);
    }
}
