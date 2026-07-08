* Perform code review for all projects (including tests)
* Analyze architecture: 
  * is it clear and consistent? 
  * is logic implemented in right place?
    * No helper functions (eg in tests) that actually fix problems in main logic implementation
    * No logic that can be generalized and moved up.
* Check code-level problems: code duplicates, signatures inconsistency, unclear structure, names.
* Check that all tests checks all appropriate properties of result.
* Check that no stubbed tests: no Assert(true) and similar, no commented checks, no empty test body, no tests without checks.
* Write report to code_review.md. Align new report with existing one. 
* Do not try to fix anything. Just create report.