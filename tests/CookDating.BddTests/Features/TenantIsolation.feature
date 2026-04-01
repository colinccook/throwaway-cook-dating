Feature: Tenant Isolation
    As a platform operator
    I want each tenant to have isolated authentication and data
    So that users on one tenant cannot access or interfere with another tenant

    @multi-tenant
    Scenario: Sign in on the wrong tenant is rejected
        Given a user is registered on the Cook Dating tenant
        When the user tries to sign in on the Tech Dating tenant
        Then the sign in should fail with an authentication error

    @multi-tenant
    Scenario: Users on different tenants cannot see each other as candidates
        Given an actively looking user on the Cook Dating tenant
        And an actively looking user on the Tech Dating tenant
        When the Cook Dating user views the discover page
        Then they should not see the Tech Dating user as a candidate
