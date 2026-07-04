Feature: Upload image
  Users can upload images for ML-backed inspection and receive validation errors.

  Background:
    Given I am logged in as a generated user
    And I have uploaded a template

  @ml
  Scenario: Upload an image and verify the final ML decision
    When I upload a valid inspection image
    Then the image upload should be queued
    And the inspected image should complete with an inspection decision

  Scenario: No selected image is rejected
    When I submit the image upload form without an image
    Then I should see an image upload error containing "Please select at least one image file"

  Scenario: No template available disables image upload
    Given I remove the existing template
    When I open the image upload page
    Then I should see the no-template warning

  Scenario: Invalid image tolerance is rejected
    When I submit the image upload form with invalid tolerance
    Then I should see an image upload field error containing "Value must be less than or equal to 100."
