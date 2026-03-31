Feature: Match Detection
  As an actively looking user
  I want to be notified when a mutual match occurs
  So that I can start a conversation

  Scenario: Mutual like creates a match
    Given two users are actively looking
    And user A has swiped right on user B
    When user B swipes right on user A
    Then a match should be created between them
    And both users should be notified of the match
