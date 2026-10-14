---
story: keep-the-profile-up-to-date
epic: account
---

# Findings — keep-the-profile-up-to-date

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/ProfilePageTest.cs:266 | minor | `Link` returns the anchor's `Text`, but no caller reads it. The "My Profile" link is found by its `data-test` only, so its label is never checked when it is followed | Assert `myProfile.Text == "My Profile"` at line 99, or drop `Text` from `Link` | done |
