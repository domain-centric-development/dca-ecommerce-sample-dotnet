---
story: a-date-of-birth-lies-in-the-past
epic: shop-api
---

# Findings — a-date-of-birth-lies-in-the-past

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Account/Adapter/Incoming/Web/ProfilePageController.cs:21 | minor | The profile page refuses today's date with words that say only future dates are refused. | The rule now refuses today, but the profile's refusal still reads "Please enter a date of birth that is not in the future." The plan keeps it on purpose (open assumption 1), so this does not deviate from the plan. | In a profile story, reword it to "Please enter a date of birth in the past." and change the profile scenario with it. | open |
| 2 | ddd | src/DcaShop.Account/Application/ChangeProfile/ChangeProfileResult.cs:17 | minor | A code comment still describes the old date rule. | The doc says "missing or lies in the future", but today is now refused as well. | "The submitted date of birth is missing or does not lie in the past." | open |
| 3 | clean-code | src/DcaShop.Account/Domain/Specification/UsableDateOfBirth.cs:9 | minor | The note explaining why the date rule exists counts two users when there are now three. | The remark says "two components evaluate it", but `UsableDateOfBirthAttribute` now evaluates it as well. | Name the API request's validation as the third evaluator, or drop the count. | open |
