Feature: Templates
  Users can list, open actions for, and delete templates.

  Background:
    Given I am logged in as a generated user

  Scenario: List templates, open row actions, and delete a template
    Given I have uploaded a template
    When I open the template row actions
    And I delete the template
    Then the template deletion should succeed

  Scenario: Empty template state is shown for a new user
    When I open the templates page
    Then I should see the templates empty state
