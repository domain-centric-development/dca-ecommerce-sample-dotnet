# backoffice — access

### Rule: The back office has its own login page

#### backoffice-login-page
- Given a browser with no cookies
- When the operator opens `/backoffice/login`
- Then the page heading reads "Backoffice Login"
- And the form has the fields "Username" and "Password" and the button "Login"
Test: DcaShop.IntegrationTests.BackofficeLoginPageTest#TheLoginPageAsksForUsernameAndPasswordUnderItsOwnHeading

### Rule: A successful operator login opens the event publication log

#### login-opens-the-event-log
- Given the backoffice login page and the demo operator credentials "admin" / "admin" of a development run
- When the operator logs in with username "admin" and password "admin"
- Then the event log `/backoffice/events` opens with the heading "Event Publication Log"
- And its summary shows the counters "Total Events", "Completed" and "Incomplete", the total not negative
Test: DcaShop.E2eTests.BackofficeE2eTest#LoginAndViewEventLog

### Rule: Wrong credentials keep the operator on the login page with one message

#### wrong-password-is-refused
- Given the backoffice login page
- When the operator logs in with username "admin" and password "wrong"
- Then the login page `/backoffice/login?error=true` shows "Invalid username or password."
Test: DcaShop.IntegrationTests.BackofficeLoginPageTest#AWrongPasswordLeadsBackToTheLoginPageWithTheMessage

#### unknown-username-is-refused
- Given the backoffice login page
- When the operator logs in with username "operator" and password "admin"
- Then the login page shows "Invalid username or password."
Test: DcaShop.IntegrationTests.BackofficeLoginPageTest#AnUnknownUsernameLeadsBackToTheLoginPageWithTheMessage

### Rule: Every back-office page needs an operator login

#### event-log-sends-a-visitor-to-the-login
- Given a browser with no cookies that is not logged in to the back office
- When it opens `/backoffice/events` directly
- Then the shop sends it to the backoffice login page `/backoffice/login`
Test: DcaShop.E2eTests.BackofficeE2eTest#UnauthenticatedAccessRedirectsToLogin

#### a-customer-login-does-not-open-the-back-office
- Given a visitor who has just registered the account "anna.schmidt@example.com" and is logged in to the shop
- When they open `/backoffice/events`
- Then the shop sends them to the backoffice login page `/backoffice/login`
Test: DcaShop.IntegrationTests.BackofficeAccessTest#ACustomerLoggedInToTheShopIsSentToTheBackofficeLogin

#### login-after-a-redirect-opens-the-event-log
- Given a browser that opened `/backoffice/settings` and was sent to the backoffice login page
- When the operator logs in with username "admin" and password "admin"
- Then the event log `/backoffice/events` opens
Test: DcaShop.IntegrationTests.BackofficeAccessTest#LoggingInAfterBeingSentFromAnUnknownAddressOpensTheEventLog

### Rule: Logging out closes the back office and says so

#### logout-confirms-on-the-login-page
- Given the operator is logged in on `/backoffice/events`
- When they press "Logout"
- Then the backoffice login page `/backoffice/login?logout=true` opens and shows "You have been logged out."
Test: DcaShop.IntegrationTests.BackofficeAccessTest#LoggingOutOpensTheLoginPageWithTheConfirmation

#### event-log-is-closed-after-logout
- Given the operator has logged out of the back office
- When they open `/backoffice/events` again
- Then the shop sends them to the backoffice login page `/backoffice/login`
Test: DcaShop.IntegrationTests.BackofficeAccessTest#AfterTheLogoutTheEventLogSendsTheOperatorToTheLoginAgain

### Rule: A back-office page's header shows the shop's name, "Back office" and the operator's logout — nothing of a shopper

#### the-back-office-header-offers-the-operators-logout
- Given the operator is logged in to the back office in a browser that is not logged in to the shop
- When they open `/backoffice/events`
- Then the page's header shows the shop's name, "Back office" and a "Logout" button
- And it shows neither "Login" nor "Register" nor a "Cart" entry
Test: DcaShop.E2eTests.BackofficeE2eTest#TheHeaderOfTheEventLogOffersTheOperatorsLogoutAndNothingOfAShopper

#### the-back-office-header-shows-no-shopper
- Given a browser logged in to the shop as "anna.schmidt@example.com" whose operator then logged in to the back office
- When they open `/backoffice/events`
- Then the page's header shows neither "Welcome, anna.schmidt@example.com" nor "My Account" nor a "Cart" entry
Test: DcaShop.IntegrationTests.BackofficeHeaderTest#TheEventLogsHeaderShowsNothingOfTheShopperLoggedInToTheShop
