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
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model.Events;

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed class Actor : IPublishDomainEvents
{
    private readonly DomainEventList _domainEvents;
    private readonly ActorStatusTransitioner _actorStatusTransitioner;
    private ExternalActorId? _externalActorId;
    private ActorCredentials? _credentials;

    public Actor(
        OrganizationId organizationId,
        ActorNumber actorNumber,
        ActorName actorName)
    {
        Id = new ActorId(Guid.Empty);
        OrganizationId = organizationId;
        ActorNumber = actorNumber;
        Name = actorName;
        _domainEvents = new DomainEventList();
        _actorStatusTransitioner = new ActorStatusTransitioner();
    }

    public Actor(
        ActorId id,
        OrganizationId organizationId,
        ExternalActorId? externalActorId,
        ActorNumber actorNumber,
        ActorStatus actorStatus,
        ActorMarketRole? marketRole,
        ActorName name,
        ActorCredentials? credentials)
    {
        Id = id;
        OrganizationId = organizationId;
        ActorNumber = actorNumber;
        Name = name;
        _domainEvents = new DomainEventList(Id.Value);
        _externalActorId = externalActorId;
        _actorStatusTransitioner = new ActorStatusTransitioner(actorStatus);
        MarketRole = marketRole;
        _credentials = credentials;
    }

    /// <summary>
    /// The internal id of actor.
    /// </summary>
    public ActorId Id { get; }

    /// <summary>
    /// The id of the organization the actor belongs to.
    /// </summary>
    public OrganizationId OrganizationId { get; }

    /// <summary>
    /// The external actor id for integrating Azure AD and domains.
    /// </summary>
    public ExternalActorId? ExternalActorId
    {
        get => _externalActorId;
        set
        {
            if (value != null && MarketRole != null)
            {
                _domainEvents.Add(new ActorActivated(ActorNumber, MarketRole.Function, value));
            }

            _externalActorId = value;
        }
    }

    /// <summary>
    /// The global location number of the current actor.
    /// </summary>
    public ActorNumber ActorNumber { get; }

    /// <summary>
    /// The status of the current actor.
    /// </summary>
    public ActorStatus Status
    {
        get => _actorStatusTransitioner.Status;
        set
        {
            if (value == ActorStatus.Active && value != _actorStatusTransitioner.Status)
            {
                Activate();
            }
            else
            {
                _actorStatusTransitioner.Status = value;
            }
        }
    }

    /// <summary>
    /// The Name of the current actor.
    /// </summary>
    public ActorName Name { get; set; }

    /// <summary>
    /// The credentials for the current actor.
    /// </summary>
    public ActorCredentials? Credentials
    {
        get => _credentials;
        set
        {
            if (_credentials != null && value != null)
            {
                throw new NotSupportedException("Cannot overwrite credentials. Remember to delete the credentials first using the appropriate service.");
            }

            if (Status == ActorStatus.Active && _credentials != value && MarketRole != null)
            {
                if (_credentials is ActorCertificateCredentials oldCredentials)
                {
                    _domainEvents.Add(new ActorCertificateCredentialsRemoved(
                        ActorNumber,
                        MarketRole.Function,
                        oldCredentials.CertificateThumbprint));
                }

                if (value is ActorCertificateCredentials newCredentials)
                {
                    _domainEvents.Add(new ActorCertificateCredentialsAssigned(
                        ActorNumber,
                        MarketRole.Function,
                        newCredentials.CertificateThumbprint));
                }
            }

            _credentials = value;
        }
    }

    /// <summary>
    /// The role (function and permissions) assigned to the current actor.
    /// </summary>
    public ActorMarketRole? MarketRole { get; private set; }

    IDomainEvents IPublishDomainEvents.DomainEvents => _domainEvents;

    private bool AreMarketRolesReadOnly => Status != ActorStatus.New;

    /// <summary>
    /// Sets a new role for the current actor.
    /// This is only allowed for 'New' actors.
    /// </summary>
    /// <param name="marketRole">The new market role to add.</param>
    public void SetMarketRole(ActorMarketRole marketRole)
    {
        ArgumentNullException.ThrowIfNull(marketRole);

        if (AreMarketRolesReadOnly)
        {
            throw new ValidationException("It is only allowed to modify market roles for actors marked as 'New'.");
        }

        MarketRole = marketRole;
    }

    /// <summary>
    /// Removes the existing role from the current actor.
    /// This is only allowed for 'New' actors.
    /// </summary>
    public void RemoveMarketRole()
    {
        if (AreMarketRolesReadOnly)
        {
            throw new ValidationException("It is only allowed to modify market roles for actors marked as 'New'.");
        }

        MarketRole = null;
    }

    /// <summary>
    /// Activates the current actor, the status changes to Active.
    /// Only New actors can be activated.
    /// </summary>
    public void Activate()
    {
        if (Id.Value == Guid.Empty)
            throw new NotSupportedException("Cannot activate uncommitted actor.");

        _actorStatusTransitioner.Activate();

        if (MarketRole is null)
        {
            return;
        }

        if (MarketRole.Function == EicFunction.GridAccessProvider)
        {
            foreach (var gridArea in MarketRole.GridAreas)
            {
                _domainEvents.Add(new GridAreaOwnershipAssigned(
                    ActorNumber,
                    MarketRole.Function,
                    gridArea.Id));
            }
        }

        if (Credentials is ActorCertificateCredentials acc)
        {
            _domainEvents.Add(new ActorCertificateCredentialsAssigned(
                ActorNumber,
                MarketRole.Function,
                acc.CertificateThumbprint));
        }
    }

    /// <summary>
    /// Deactivates the current actor, the status changes to Inactive.
    /// Only New, Active and Passive actors can be deactivated.
    /// </summary>
    public void Deactivate()
    {
        if (_credentials != null)
        {
            throw new ValidationException("Cannot disable actor with active credentials. Remove the credentials first, then try again.")
                .WithErrorCode("actor.credentials.still_active");
        }

        _actorStatusTransitioner.Deactivate();
    }

    /// <summary>
    /// Passive actors have certain domain-specific actions that can be performed.
    /// Only Active and New actors can be set to passive.
    /// </summary>
    public void SetAsPassive() => _actorStatusTransitioner.SetAsPassive();
}
