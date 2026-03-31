Feature: Profile Management
  As a registered user
  I want to manage my dating profile
  So that potential matches can learn about me

  Scenario: Toggle looking status to actively looking
    Given I am logged in
    And I am on the profile tab
    When I toggle my status to "Actively Looking"
    Then my status should show "Actively Looking"
    And a looking status changed event should be raised
