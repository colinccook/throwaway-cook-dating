Feature: Conversation Between Matches
  As a matched user
  I want to send messages to my match
  So that we can get to know each other

  Scenario: Send a message to a matched user
    Given I am matched with another user
    And I open the conversation with my match
    When I type and send a message
    Then the message should appear in the conversation
    And the other user should receive the message

  Scenario: Cannot message an unmatched user
    Given I am not matched with another user
    Then I should not be able to send them a message
