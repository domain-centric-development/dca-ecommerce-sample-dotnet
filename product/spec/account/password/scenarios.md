# account — password

### Rule: The change-password page asks for the current password and the new one twice

#### change-password-page-shows-the-form
- Given the customer "anna.schmidt@example.com" is logged in
- When they follow "Change Password" in the account navigation
- Then `/account/change-password` shows the heading "Change Password" and "Change Password" marked as the current page
- And the form has the empty fields "Current Password", "New Password" and "Confirm New Password" and the button "Change Password"
- And "New Password" carries the hint "Password must be at least 8 characters and contain an uppercase letter, a lowercase letter and a digit"
- And the new-password fields accept at most 72 characters
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#TheAccountNavigationLeadsToAnEmptyFormForTheCurrentPasswordAndTheNewOneTwice

### Rule: A successful change keeps the customer logged in

#### changing-the-password
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123", New Password "Better456" and Confirm New Password "Better456"
- Then `/account/change-password` opens again and shows "Your password has been changed."
- And all three password fields are empty
- And the header still shows "Welcome, anna.schmidt@example.com"
Test: DcaShop.E2eTests.ChangePasswordE2eTest#AChangedPasswordIsConfirmedOnAnEmptyFormAndTheCustomerStaysLoggedIn

#### keeping-the-same-password-is-allowed
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit "Secret123" as current, new and confirmed password
- Then the page shows "Your password has been changed."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#KeepingTheCurrentPasswordAsTheNewOneIsAChangeLikeAnyOther

### Rule: From then on only the new password logs in

#### new-password-logs-in
- Given the customer "anna.schmidt@example.com" changed the password from "Secret123" to "Better456", and a browser with no cookies
- When they log in with "anna.schmidt@example.com" and "Better456"
- Then the cart page opens and the header shows "Welcome, anna.schmidt@example.com"
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#TheNewPasswordLogsInFromABrowserWithNoCookies

#### old-password-no-longer-logs-in
- Given the customer "anna.schmidt@example.com" changed the password from "Secret123" to "Better456", and a browser with no cookies
- When they log in with "anna.schmidt@example.com" and "Secret123"
- Then the login page shows "Invalid email or password"
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#TheOldPasswordNoLongerLogsIn

### Rule: The change-password page needs a login

#### change-password-sends-a-visitor-to-the-login
- Given a visitor with no cookies
- When they open `/account/change-password`
- Then they are sent to `/login?returnUrl=%2Faccount%2Fchange-password`
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#AVisitorWithoutCookiesIsSentFromTheChangePasswordPageToTheLogin

### Rule: The confirmation must match the new password — checked first

#### differing-confirmation-is-refused
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123", New Password "Better456" and Confirm New Password "Better457"
- Then the page shows "New password and confirmation do not match"
- And all three password fields are empty
Test: DcaShop.E2eTests.ChangePasswordE2eTest#AConfirmationThatDiffersFromTheNewPasswordIsRefusedOnAnEmptyForm

#### differing-confirmation-wins-over-a-wrong-current-password
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Wrong999", New Password "Better456" and Confirm New Password "Better457"
- Then the page shows "New password and confirmation do not match"
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#AConfirmationThatDiffersIsNamedEvenWhenTheCurrentPasswordIsWrong

### Rule: The current password must be correct — checked before the new password's strength

#### wrong-current-password-is-refused
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Wrong999", New Password "Better456" and Confirm New Password "Better456"
- Then the page shows "Current password is not correct"
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#AWrongCurrentPasswordIsRefusedWithItsOwnSentence

#### wrong-current-password-wins-over-a-weak-new-one
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Wrong999", New Password "weakpass" and Confirm New Password "weakpass"
- Then the page shows "Current password is not correct"
Test: DcaShop.IntegrationTests.ChangePasswordRefusalTest#AWrongCurrentPasswordIsReportedBeforeAWeakNewOne

### Rule: After a refusal the old password still holds

#### refused-change-keeps-the-old-password
- Given the customer "anna.schmidt@example.com" tried to change "Secret123" with Current Password "Wrong999" and was refused, and a browser with no cookies
- When they log in with "anna.schmidt@example.com" and "Secret123"
- Then the cart page opens and the header shows "Welcome, anna.schmidt@example.com"
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#AfterARefusedChangeTheOldPasswordStillLogsIn

### Rule: At least 8 characters — checked before anything else

#### short-new-password-is-refused
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123" and "Abc1234" as new and confirmed password
- Then the page shows "Password must be at least 8 characters long."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#ANewPasswordShorterThanEightCharactersIsRefusedForItsLength

#### length-is-checked-before-the-characters
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123" and "abcdefg" as new and confirmed password
- Then the page shows "Password must be at least 8 characters long."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#AShortPasswordIsRefusedForItsLengthBeforeItsCharacters

### Rule: At most 72 bytes in UTF-8

#### password-over-72-bytes-is-refused
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123" and, as new and confirmed password, "Aa1" followed by 35 times "ä" (38 characters, 73 bytes)
- Then the page shows "Password must not be longer than 72 bytes."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#ANewPasswordOverSeventyTwoBytesIsRefusedThoughItHasFewerCharacters

#### password-of-72-bytes-is-accepted
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123" and, as new and confirmed password, "Aa1" followed by 34 times "ä" and one "b" (38 characters, 72 bytes)
- Then the page shows "Your password has been changed."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#ANewPasswordOfExactlySeventyTwoBytesIsAccepted

### Rule: At least one uppercase letter, one lowercase letter and one digit

#### password-without-uppercase-is-refused
- Given a visitor with no cookies on `/register`
- When they submit a registration for "anna.schmidt@example.com" with Password and Confirm Password "secret123"
- Then the registration page shows "Password must contain an uppercase letter."
- And the visitor is not logged in
Test: DcaShop.E2eTests.PasswordPolicyE2eTest#APasswordWithoutAnUppercaseLetterIsRefusedAndTheVisitorStaysLoggedOut

#### new-password-without-lowercase-is-refused
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123" and "SECRET123" as new and confirmed password
- Then the page shows "Password must contain a lowercase letter."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#ANewPasswordWithoutALowercaseLetterIsRefusedForIt

#### new-password-without-digit-is-refused
- Given the customer "anna.schmidt@example.com" with password "Secret123" is logged in on `/account/change-password`
- When they submit Current Password "Secret123" and "SecretPass" as new and confirmed password
- Then the page shows "Password must contain a digit."
Test: DcaShop.IntegrationTests.ChangePasswordPageTest#ANewPasswordWithoutADigitIsRefusedForIt
