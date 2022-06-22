# Market Participant
[![codecov](https://codecov.io/gh/Energinet-DataHub/geh-market-participant/branch/main/graph/badge.svg?token=1VGVTZG6IT)](https://codecov.io/gh/Energinet-DataHub/geh-market-participant)

Welcome to the Market Participant domain of the [Green Energy Hub project](https://github.com/Energinet-DataHub/green-energy-hub).

- [Intro](#intro)
- [Communicating with Market Participant](#Communicating-with-Market-Participant)
  - [Integration Events](#Integration-events)

## Intro

Market Participant is where everything related to Organization, Actors, GridAreas and their relationships are handled.

## Communicating with Market Participant

Interaction with the Market Participant domain is done in two different ways, depending on whether you are looking to get data updates or want use the API to interact with the domain. 

A [Client Nuget Package](https://www.nuget.org/packages/Energinet.DataHub.MarketParticipant.Client/), which is the recommended way to interact with the domain is available, and exposes all the API's currently available

### API
We have the following exposed endpoints

#### GET:/Organization

```organization/```
returns all organizations

#### GET:/Organization

```organization/{organizationId:guid}```
returns an organization with the specified id if it exists.

#### POST:/Organization

```organization/```
returns and organization with the specified id if it exists.

Example BODY:

```json
{
  "name": "string",
  "businessRegisterIdentifier": "string",
  "address": {
    "streetName": "string",
    "number": "string",
    "zipCode": "string",
    "city": "string",
    "country": "string"
  },
  "comment": "string"
}
```

#### PUT:/Organization

```organization/{organizationId:guid}```
Updates an organization with the specified id if it exists.

Example BODY:

```json
{
  "name": "string",
  "businessRegisterIdentifier": "string",
  "address": {
    "streetName": "string",
    "number": "string",
    "zipCode": "string",
    "city": "string",
    "country": "string"
  },
  "comment": "string"
}
```


### Integration Events
Integration events are a way for interested parties to know when data changes in the Market Participant domain and be informed of these changes.

Integration events are published to a servicebus topic, where everyone who are interested can listen. What you do with these events are. A description of how to subscribe to these events are [here](#Subscribe-to-integration-events)

The following integrations events are available:

 - **Actor Events**
	 - [ActorCreated](#integration-event-actor-created)
	 - [ActorStatusChanged](#integration-event-actor-status-changed)
	 - [ActorRoleAdded](#integration-event-actor-role-added)
	 - [ActorRoleRemoved](#integration-event-actor-role-removed)
	 - [ActorContactAdded](#integration-event-actor-contact-added)
	 - [ActorContactRemoved](#integration-event-actor-contact-removed)
	 - [ActorGridAreaAdded](#integration-event-actor-gridarea-added)
	 - [ActorGridAreaRemoved](#integration-event-actor-gridarea-removed)
 - **Organization Events**
	 - [OrganizationCreated](#integration-event-organization-created)
	 - [OrganizationNameChanged](#integration-event-organization-name-changed)
	 - [OrganizationAddressChanged](#integration-event-organization-address-changed)
	 - [OrganizationCommentChanged](#integration-event-organization-comment-changed)
	 - [OrganizationBusinessRegisterIdentifierChanged](#integration-event-actor-created)
 - **Grid Areas**
	 - [GridAreaCreated](#integration-event-actor-created)
	 - [GridAreaNameChanged](#integration-event-actor-created)
	

