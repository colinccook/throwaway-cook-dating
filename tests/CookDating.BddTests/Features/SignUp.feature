Feature: User Sign Up
  As a new user
  I want to create an account
  So that I can start using the dating app

  Scenario: Successful sign up with valid details
    Given I am on the sign up page
    When I enter valid sign up details
    And I submit the sign up form
    Then I should be redirected to the profile page
    And my profile should be created

  Scenario: Correcting an invalid date of birth retries sign up successfully
    Given I am on the sign up page
    When I enter sign up details with an underage date of birth
    And I submit the sign up form
    Then I should see a date of birth validation error
    And no account should be created for the invalid submission
    When I correct only the date of birth
    And I submit the sign up form
    Then I should not see a sign up error containing "Must be at least 18 years old"
    And I should not see a sign up error containing "Email already registered"
    And I should be redirected to the profile page
    And my profile should be created
    And the retry sign up should not hit an already registered error
