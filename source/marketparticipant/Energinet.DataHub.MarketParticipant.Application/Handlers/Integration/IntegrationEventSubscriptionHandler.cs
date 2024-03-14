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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Integration;

public class IntegrationEventSubscriptionHandler(
    IIntegrationEventParser integrationEventParser,
    IEmailEventRepository emailEventRepository,
    EmailRecipientConfig emailRecipientConfig,
    ILogger<IntegrationEventSubscriptionHandler> logger)
    : IIntegrationEventHandler
{
    public Task HandleAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var domainEvent = integrationEventParser.Parse(integrationEvent);

        if (domainEvent is null)
        {
            logger.LogInformation("Integration event not supported. Event name: {EventName}", integrationEvent.EventName);
            return Task.CompletedTask;
        }

        logger.LogInformation("Integration event received. Event name: {EventName} EventId: {EventId}", integrationEvent.EventName, integrationEvent.EventIdentification);

        switch (domainEvent)
        {
            case BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged:
                return BuildAndSendEmailAsync(balanceResponsiblePartiesChanged);
            default:
                return Task.CompletedTask;
        }
    }

    private Task BuildAndSendEmailAsync(BalanceResponsiblePartiesChanged balanceResponsiblePartiesChanged)
    {
        return emailEventRepository.InsertAsync(
            new EmailEvent(
                new EmailAddress(emailRecipientConfig.BalanceResponsibleChangedNotificationToEmail),
                new BalanceResponsiblePartiesChangedEmailTemplate(balanceResponsiblePartiesChanged)));
    }
}
