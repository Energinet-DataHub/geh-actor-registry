# The 'views.dsl' file is intended as a mean for viewing and validating the model
# in the domain repository. It should
#   * Extend the base model and override the 'dh3' software system
#   * Include of the `model.dsl` files from each domain repository using an URL
#
# The `model.dsl` file must contain the actual model, and is the piece that must
# be reusable and included in other Structurizr files like `views.dsl` and
# deployment diagram files.

workspace extends https://raw.githubusercontent.com/Energinet-DataHub/opengeh-arch-diagrams/main/docs/diagrams/c4-model/dh-base-model.dsl {

    model {
        #
        # DataHub 3.0 (extends)
        #
        !ref dh3 {

            # IMPORTANT:
            # The order by which models are included is important for how the domain-to-domain relationships are specified.
            # A domain-to-domain relationship should be specified in the "client" of a "client->server" dependency, and
            # hence domains that doesn't depend on others, should be listed first.

            # Include Revision Log model
            !include https://raw.githubusercontent.com/Energinet-DataHub/opengeh-revision-log/main/docs/diagrams/c4-model/model.dsl?token=GHSAT0AAAAAACGAKLTETFXSE4SIW66TGTZ4ZVQ4XZQ

            # Include Market Participant model
            !include model.dsl

            # Include EDI model
            !include https://raw.githubusercontent.com/Energinet-DataHub/opengeh-edi/main/docs/diagrams/c4-model/model.dsl

            # Include Wholesale model
            !include https://raw.githubusercontent.com/Energinet-DataHub/opengeh-wholesale/main/docs/diagrams/c4-model/model.dsl

            # Include Frontend model
            !include https://raw.githubusercontent.com/Energinet-DataHub/greenforce-frontend/main/docs/diagrams/c4-model/model.dsl
        }
    }

    views {
        container dh3 "MarketParticipant" {
            title "[Container] DataHub 3.0 - Market Participant (Simplified)"
            include ->markpartDomain->
            include actorB2BSystem frontendSinglePageApplication dh3User
            exclude "element.tag==Intermediate Technology"
            exclude wholesaleApi
        }

        container dh3 "MarketParticipantDetailed" {
            title "[Container] DataHub 3.0 - Market Participant (Detailed with OAuth)"
            include ->markpartDomain->
            include actorB2BSystem ediApi bffApi frontendSinglePageApplication dh3User
            exclude wholesaleApi
        }

        component markpartApi "MarketParticipantAPIs" {
            title "[Component] DataHub 3.0 - Market Participant User Web API"
            include markpartUserIdentityRepositoryInMarkpartApi->
            include ->markpartUserController->
            include ->markpartPermissionController->
            include ->markpartUserRoleController->
            include ->markpartUserRoleAssignmentController->
            include ->markpartUserOverviewController->
            include ->markpartInvitationController->
        }

        component markpartOrganizationManager "MarkpartOrganizationManager" {
            title "[Component] DataHub 3.0 - Market Participant Organization Manager"
            include markpartUserIdentityRepositoryInOrganizationManager->
            include ->markpartMailDispatcher->
            include ->markpartUserInvitationExpiredTimerTrigger->
            include ->markpartIntegrationEvents->
        }
    }
}

