Feature: Swiping on Candidates
  As an actively looking user
  I want to swipe on candidate profiles
  So that I can express interest or disinterest

  Scenario: Swipe right to like a candidate
    Given I am logged in and actively looking
    And I am on the discover tab
    And there is a candidate profile shown
    When I swipe right on the candidate
    Then the swipe should be recorded as a like

  Scenario: Swipe left to dislike a candidate
    Given I am logged in and actively looking
    And I am on the discover tab
    And there is a candidate profile shown
    When I swipe left on the candidate
    Then the swipe should be recorded as a dislike
