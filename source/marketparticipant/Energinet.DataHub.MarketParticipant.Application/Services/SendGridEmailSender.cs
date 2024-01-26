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
using Energinet.DataHub.MarketParticipant.Application.Services.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using EmailAddress = Energinet.DataHub.MarketParticipant.Domain.Model.EmailAddress;

namespace Energinet.DataHub.MarketParticipant.Application.Services
{
    public sealed class SendGridEmailSender : IEmailSender
    {
        private readonly InviteConfig _config;
        private readonly ILogger<SendGridEmailSender> _logger;
        private readonly ISendGridClient _client;
        private readonly IEmailContentGenerator _emailHtmlGenerator;

        public SendGridEmailSender(
            InviteConfig config,
            ISendGridClient sendGridClient,
            IEmailContentGenerator emailHtmlGenerator,
            ILogger<SendGridEmailSender> logger)
        {
            _config = config;
            _logger = logger;
            _client = sendGridClient;
            _emailHtmlGenerator = emailHtmlGenerator;
        }

        public async Task<bool> SendEmailAsync(EmailAddress emailAddress, EmailEvent emailEvent)
        {
            ArgumentNullException.ThrowIfNull(emailAddress);
            ArgumentNullException.ThrowIfNull(emailEvent);

            var template = SelectTemplate(emailEvent);
            var parameters = GatherTemplateParameters(emailEvent);

            var generatedEmail = await _emailHtmlGenerator
                .GenerateAsync(template, parameters)
                .ConfigureAwait(false);

            return await SendAsync(
                new SendGrid.Helpers.Mail.EmailAddress(_config.UserInviteFromEmail),
                new SendGrid.Helpers.Mail.EmailAddress(emailAddress.Address),
                generatedEmail.Subject,
                generatedEmail.HtmlContent)
                .ConfigureAwait(false);
        }

        private static EmailTemplate SelectTemplate(EmailEvent emailEvent)
        {
            return emailEvent.EmailEventType switch
            {
                EmailEventType.UserInvite => EmailTemplate.UserInvite,
                EmailEventType.UserAssignedToActor => EmailTemplate.UserAssignedToActor,
                _ => throw new NotFoundException("EmailEventType not recognized"),
            };
        }

        private IReadOnlyDictionary<string, string> GatherTemplateParameters(EmailEvent emailEvent)
        {
            // TODO: Read data from somewhere.
            return new Dictionary<string, string>
            {
                { "environment", _config.EnvironmentDescription ?? string.Empty },
                { "user_name", "Test User Name" },
                { "actor_org", "ActorOrg" },
                { "actor_gln", "ActorGln" },
                { "actor_name", "ActorName" },
                { "invite_link", _config.UserInviteFlow + "&nonce=defaultNonce&scope=openid&response_type=code&prompt=login&code_challenge_method=S256&code_challenge=defaultCodeChallenge" },
            };
        }

        private async Task<bool> SendAsync(
            SendGrid.Helpers.Mail.EmailAddress from,
            SendGrid.Helpers.Mail.EmailAddress to,
            string subject,
            string htmlContent)
        {
            var msg = MailHelper.CreateSingleEmail(from, to, subject, string.Empty, htmlContent);
            msg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(_config.UserInviteBccEmail));

            var response = await _client.SendEmailAsync(msg).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("User invite email sent successfully to {Address}.", to.Email);
            }
            else
            {
                throw new NotSupportedException("User invite email return error response code:  " + response.StatusCode);
            }

            return response.IsSuccessStatusCode;
        }
    }
}
