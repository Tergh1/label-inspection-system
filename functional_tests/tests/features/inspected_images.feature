Feature: Inspected images
  Users can list inspected images, verify decisions, inspect eligible failures again, show defects, and delete images.

  Background:
    Given I am logged in as a generated user

  @ml
  Scenario: List inspected images and delete an image
    Given I have uploaded a template
    And I have uploaded and completed an inspection image
    When I open the inspected images page
    Then the inspected image should show similarity and decision fields
    And I can show defects when available
    When I delete the inspected image
    Then the image deletion should succeed

  Scenario: Inspect again only where the app allows it
    Given I have uploaded a template
    And I have uploaded and completed an inspection image
    When I inspect again if the app allows it
    Then the inspect-again action should be conditionally handled

  Scenario: Empty image state is shown for a new user
    When I open the inspected images page
    Then I should see the inspected images empty state
