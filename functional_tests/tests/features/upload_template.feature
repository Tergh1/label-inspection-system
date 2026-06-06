Feature: Upload template
  Users can upload reference templates and receive validation errors for bad input.

  Background:
    Given I am logged in as a generated user

  Scenario: Upload a valid template and verify it appears in Templates
    When I upload a valid template
    Then the template upload should succeed
    And the template should appear in Templates

  Scenario: Missing template file is rejected
    When I submit the template form without a file
    Then I should see a template upload error containing "Please select a template file"

  Scenario: Missing friendly name is rejected
    When I submit the template form without a friendly name
    Then I should see a template upload error containing "FriendlyName"

  Scenario: Invalid template tolerance is rejected
    When I submit the template form with invalid tolerance
    Then I should see a template upload error containing "between 0 and 100"
