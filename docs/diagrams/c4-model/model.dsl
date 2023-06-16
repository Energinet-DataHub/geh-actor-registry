# Read description in the 'views.dsl' file.

markpartDomain = group "Market Participant" {
    #
    # Common (managed by Market Participant)
    #
    commonSendGrid = container "SendGrid" {
        description "EMail dispatcher"
        technology "Twilio SendGrid"
        tags "Intermediate Technology" "SaaS" "Microsoft Azure - SendGrid Accounts"

        # Base model relationships
        this -> dh3User "Sends invitation mail"
    }
    commonB2C = container "App Registrations" {
        description "Cloud identity directory"
        technology "Azure AD B2C"
        tags "Microsoft Azure - Azure AD B2C"

        # Base model relationships
        actorB2BSystem -> this "Request OAuth token" "https" {
            tags "OAuth"
        }
    }

    #
    # Domain
    #
    markpartDb = container "Actors Database" {
        description "Stores data regarding actors, users and permissions."
        technology "SQL Database Schema"
        tags "Data Storage" "Microsoft Azure - SQL Database"
    }
    markpartApi = container "Market Participant API" {
        description "Multi-tenant API for managing actors, users and permissions."
        technology "Asp.Net Core Web API"
        tags "Microsoft Azure - App Services"

        # Domain relationships
        this -> markpartDb "Reads and writes actor/user data." "EF Core"
    }
    markpartOrganizationManager = container "Market Participant <Organization Manager>" {
        description "<add description>"
        technology "Azure function, C#"
        tags "Microsoft Azure - Function Apps"

        markpartEventActorSynchronizer = component "Actor Synchronizer" {
            description "Creates B2C application registration for newly created actors."
            technology "Timer Trigger"
            tags "Microsoft Azure - Function Apps"

            # Common relationships
            this -> commonB2C "Creates B2C App Registration" "Microsoft.Graph/https"

            # Domain relationships
            this -> markpartDb "Updates actors with external B2C id." "EF Core"
        }
        markpartMailDispatcher = component "Mail Dispatcher" {
            description "Responsible for sending user invitations."
            technology "Timer Trigger"
            tags "Microsoft Azure - Function Apps"

            # Base model relationships
            this -> dh3User "Sends invitation mail" {
                tags "Simple View"
            }

            # Common relationships
            this -> commonSendGrid "Sends invitation mail" "SendGrid/https"

            # Domain relationships
            this -> markpartDb "Reads data regarding newly invited users." "EF Core"
        }
    }
}
