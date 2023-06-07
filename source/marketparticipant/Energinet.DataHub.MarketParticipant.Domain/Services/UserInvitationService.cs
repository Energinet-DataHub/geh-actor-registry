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
    private readonly IUserRoleAssignmentAuditLogEntryRepository _userRoleAssignmentAuditLogEntryRepository;
    private readonly IUnitOfWorkProvider _unitOfWorkProvider;
    private readonly IUserStatusCalculator _userStatusCalculator;

    public UserInvitationService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IEmailEventRepository emailEventRepository,
        IOrganizationDomainValidationService organizationDomainValidationService,
        IUserInviteAuditLogEntryRepository userInviteAuditLogEntryRepository,
        IUserRoleAssignmentAuditLogEntryRepository userRoleAssignmentAuditLogEntryRepository,
        IUnitOfWorkProvider unitOfWorkProvider,
        IUserStatusCalculator userStatusCalculator)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _emailEventRepository = emailEventRepository;
        _organizationDomainValidationService = organizationDomainValidationService;
        _userInviteAuditLogEntryRepository = userInviteAuditLogEntryRepository;
        _userRoleAssignmentAuditLogEntryRepository = userRoleAssignmentAuditLogEntryRepository;
        _unitOfWorkProvider = unitOfWorkProvider;
        _userStatusCalculator = userStatusCalculator;
    }

    public async Task InviteUserAsync(UserInvitation invitation, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        await _organizationDomainValidationService
            .ValidateUserEmailInsideOrganizationDomainsAsync(invitation.AssignedActor, invitation.Email)
            .ConfigureAwait(false);

        var invitedUser = await GetUserAsync(invitation.Email).ConfigureAwait(false);
        if (invitedUser == null)
        {
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
        }

        var userInviteRoleAssignments = new List<UserRoleAssignment>();

        foreach (var assignedRole in invitation.AssignedRoles)
        {
            var assignment = new UserRoleAssignment(invitation.AssignedActor.Id, assignedRole.Id);
            invitedUser.RoleAssignments.Add(assignment);
            userInviteRoleAssignments.Add(assignment);
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
                .InsertAsync(new EmailEvent(invitation.Email, EmailEventType.UserInvite))
                .ConfigureAwait(false);

            await AuditLogUserInviteAsync(invitedUserId, invitationSentByUserId, invitation.AssignedActor.Id).ConfigureAwait(false);
            await AuditLogUserInviteAndUserRoleAssignmentsAsync(invitedUserId, userInviteRoleAssignments, invitationSentByUserId).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);
        }
    }

    public async Task ReInviteUserAsync(User user, UserId invitationSentByUserId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var userIdentity = await _userIdentityRepository
            .GetAsync(user.ExternalId)
            .ConfigureAwait(false);

        if (userIdentity == null)
        {
            throw new NotFoundValidationException($"The specified user identity {user.ExternalId} was not found.");
        }

        if (_userStatusCalculator.CalculateUserStatus(userIdentity.Status, user.InvitationExpiresAt) != UserStatus.InviteExpired)
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

            await AuditLogUserInviteAsync(invitedUserId, invitationSentByUserId, user.AdministratedBy).ConfigureAwait(false);

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

    private async Task AuditLogUserInviteAndUserRoleAssignmentsAsync(
        UserId invitedUserId,
        IEnumerable<UserRoleAssignment> invitedUserRoleAssignments,
        UserId invitationSentByUserId)
    {
        foreach (var assignment in invitedUserRoleAssignments)
        {
            await _userRoleAssignmentAuditLogEntryRepository.InsertAuditLogEntryAsync(
                invitedUserId,
                new UserRoleAssignmentAuditLogEntry(
                    invitedUserId,
                    assignment.ActorId,
                    assignment.UserRoleId,
                    invitationSentByUserId,
                    DateTimeOffset.UtcNow,
                    UserRoleAssignmentTypeAuditLog.Added)).ConfigureAwait(false);
        }
    }

    private Task AuditLogUserInviteAsync(UserId toUserId, UserId invitationSentByUserId, ActorId assignedActor)
    {
        var userInviteAuditLog = new UserInviteAuditLogEntry(
            toUserId,
            invitationSentByUserId,
            assignedActor,
            DateTimeOffset.UtcNow);

        return _userInviteAuditLogEntryRepository
            .InsertAuditLogEntryAsync(userInviteAuditLog);
    }
}
