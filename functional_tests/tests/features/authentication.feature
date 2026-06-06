Feature: Authentication
  Users can register, log in, see authenticated navigation, and log out.

  Scenario: Register or reuse a generated user, log in, and log out
    Given I am logged in as a generated user
    Then I should see authenticated navigation
    When I log out
    Then I should see public navigation

  Scenario: Invalid login shows an error
    Given I am logged out
    When I try to log in with invalid credentials
    Then I should see an invalid login error
