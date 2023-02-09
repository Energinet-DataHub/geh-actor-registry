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
using Energinet.DataHub.MarketParticipant.Application.Commands;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Email
{
    public sealed class SendUserInviteEmailHandler : IRequestHandler<SendUserInviteEmailCommand, Unit>
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailEventRepository _emailEventRepository;
        private readonly IEmailSender _emailSender;
        private readonly IUserIdentityRepository _userIdentityRepository;
        private readonly ILogger<SendUserInviteEmailHandler> _logger;

        public SendUserInviteEmailHandler(
            IUserRepository userRepository,
            IEmailEventRepository emailEventRepository,
            IEmailSender emailSender,
            IUserIdentityRepository userIdentityRepository,
            ILogger<SendUserInviteEmailHandler> logger)
        {
            _userRepository = userRepository;
            _emailEventRepository = emailEventRepository;
            _emailSender = emailSender;
            _userIdentityRepository = userIdentityRepository;
            _logger = logger;
        }

        public async Task<Unit> Handle(SendUserInviteEmailCommand request, CancellationToken cancellationToken)
        {
            // Find email event to be sent
            var invitesToBeSent = await _emailEventRepository
                .GetAllEmailsToBeSentByTypeAsync(EmailEventType.UserInvite)
                .ConfigureAwait(false);

            foreach (var emailInvite in invitesToBeSent)
            {
                // find user in azure, if user = InActive log and continue
                var userIdentity = await _userIdentityRepository
                    .GetAsync(emailInvite.Email)
                    .ConfigureAwait(false);

                if (userIdentity is null)
                {
                    throw new NotSupportedException($"User with email '{emailInvite.Email}' was not found");
                }

                var user = await _userRepository.GetAsync(userIdentity.Id).ConfigureAwait(false);

                if (user is null)
                {
                    throw new NotSupportedException($"User with external id {userIdentity.Id.Value} was not found");
                }

                // Send email and update event state.
                await _emailSender.SendEmailAsync(userIdentity.Email, emailInvite).ConfigureAwait(false);
                await _emailEventRepository.MarkAsSentAsync(emailInvite).ConfigureAwait(false);

                // update user status
                user.InviteStatus = UserInviteStatus.InviteSent;
                await _userRepository.AddOrUpdateAsync(user).ConfigureAwait(false);

                // Log ?
            }

            return Unit.Value;
        }
    }
}
