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
