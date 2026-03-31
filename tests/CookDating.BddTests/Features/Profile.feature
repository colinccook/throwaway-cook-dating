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

  Scenario: Update display name and bio
    Given I am logged in
    And I am on the profile tab
    When I change my display name to "Chef Supreme"
    And I change my bio to "I love cooking Italian food"
    And I save my profile changes
    Then I should see a profile success message "Profile saved!"
    And my profile changes should persist after reload

  Scenario: Update dating preferences
    Given I am logged in
    And I am on the profile tab
    When I change my min age to "25"
    And I change my max age to "40"
    And I change my max distance to "100"
    And I save my profile changes
    Then I should see a profile success message "Profile saved!"
    And my preference changes should persist after reload

  Scenario: Update gender preference
    Given I am logged in
    And I am on the profile tab
    When I change my preferred gender to "Female"
    And I save my profile changes
    Then I should see a profile success message "Profile saved!"
    And my preferred gender should show "Female" after reload
