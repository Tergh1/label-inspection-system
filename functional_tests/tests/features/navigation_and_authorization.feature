Feature: Navigation and authorization
  Public and protected navigation behaves correctly.

  Scenario: Public home shows sign-in and register actions
    Given I am logged out
    When I open the home page
    Then I should see public home actions

  Scenario Outline: Protected pages redirect unauthenticated users to login
    Given I am logged out
    When I open protected page "<path>"
    Then I should be redirected to login

    Examples:
      | path              |
      | /templates        |
      | /templates/upload |
      | /images           |
      | /images/upload    |
      | /reports          |
