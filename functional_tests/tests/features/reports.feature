Feature: Reports
  Reports show empty states, statistics, filters, and exports.

  Background:
    Given I am logged in as a generated user

  Scenario: Report empty state
    When I open the reports page
    Then I should see the reports empty state

  @ml
  Scenario: Inspected image appears in reports after ML decision
    Given I have uploaded a template
    And I have uploaded and completed an inspection image
    When I open the reports page
    Then the inspected image should appear in reports and statistics

  @ml
  Scenario: Report filters can be applied and cleared
    Given I have uploaded a template
    And I have uploaded and completed an inspection image
    When I apply and clear report filters
    Then the report filters should be cleared

  @ml @export
  Scenario: CSV and XLSX export links return downloadable responses
    Given I have uploaded a template
    And I have uploaded and completed an inspection image
    When I download the CSV and XLSX reports
    Then both report exports should be downloaded
