# account — signin

### Rule: A visitor can reach the login from every shop page

#### header-offers-login
- Given a visitor with no cookies
- When they open the catalogue page `/products`
- Then the header shows a "Login" link to `/login` next to the "Register" link
Test: DcaShop.IntegrationTests.LoginPageTest#TheHeaderOfTheCatalogueOffersALoginLinkNextToTheRegisterLink

#### login-page-shows-the-form
- Given a visitor with no cookies
- When they open `/login`
- Then the page heading reads "Login"
- And the form has the fields "Email" and "Password" and the button "Login"
- And a link "Register here" leads to `/register`
Test: DcaShop.IntegrationTests.LoginPageTest#TheLoginPageAsksForEmailAndPasswordAndLeadsToTheRegistration

### Rule: A successful login starts the session and opens the cart, or the page the customer came for

#### login-opens-the-cart
- Given the account "anna.schmidt@example.com" with password "Secret123", registered earlier, and a browser with no cookies and an empty cart
- When the visitor logs in on `/login` with "anna.schmidt@example.com" and "Secret123"
- Then the cart page `/cart` opens and shows "Welcome back!"
- And the header shows "Welcome, anna.schmidt@example.com" and a "Logout" button instead of "Login" and "Register"
Test: DcaShop.E2eTests.LoginE2eTest#ALoginWithAnEmptyCartOpensTheCartAndGreetsTheCustomer

#### login-returns-to-the-given-page
- Given the account "anna.schmidt@example.com" and a browser with no cookies that opened `/login?returnUrl=/products`
- When the visitor logs in with "anna.schmidt@example.com" and "Secret123"
- Then the catalogue page `/products` opens
Test: DcaShop.IntegrationTests.LoginPageTest#ALoginOpenedForTheCatalogueReturnsThere

#### register-link-keeps-the-return-page
- Given a visitor with no cookies
- When they open `/login?returnUrl=/products`
- Then the "Register here" link leads to `/register?returnUrl=/products`
Test: DcaShop.IntegrationTests.LoginPageTest#TheRegisterLinkOfALoginOpenedForTheCatalogueKeepsTheCatalogue

### Rule: Logging out ends the session and says so

#### logout-confirms-on-the-login-page
- Given the customer "anna.schmidt@example.com" is logged in
- When they press "Logout" in the header
- Then the login page `/login?logout=true` opens and shows "You have been successfully logged out."
- And the header shows "Login" and "Register" again
Test: DcaShop.IntegrationTests.LoginPageTest#LoggingOutOpensTheLoginPageWithTheConfirmationAndTheHeaderOffersLoginAgain

### Rule: Every failed login shows the same message and keeps the typed email

#### wrong-password-is-refused
- Given the account "anna.schmidt@example.com" with password "Secret123" and a browser with no cookies on `/login`
- When the visitor logs in with "anna.schmidt@example.com" and "Secret999"
- Then the login page shows "Invalid email or password"
- And the Email field still holds "anna.schmidt@example.com" and the Password field is empty
- And the header still shows "Login" and "Register"
Test: DcaShop.E2eTests.LoginE2eTest#AWrongPasswordKeepsTheTypedEmailAndSaysOnlyThatTheLoginFailed

#### unknown-address-is-refused
- Given no account for "nobody@example.com" and a browser with no cookies on `/login`
- When the visitor logs in with "nobody@example.com" and "Secret123"
- Then the login page shows "Invalid email or password"
Test: DcaShop.IntegrationTests.AccountFlowTest#AnUnknownAddressIsRefusedWithTheSameMessage

#### malformed-address-is-refused
- Given a browser with no cookies on `/login`
- When the visitor logs in with "anna.schmidt@example" and "Secret123"
- Then the login page shows "Invalid email or password"
Test: DcaShop.IntegrationTests.LoginPageTest#AnAddressWithoutADomainEndingIsRefusedWithTheSameMessageAsAWrongPassword

### Rule: Logging in through the API hands out a token for the account

#### logging-in-hands-out-a-token
- Given an account "ada@example.com" with password "Secret123"
- When a client sends `POST /api/auth/login` with `{"email":"ada@example.com","password":"Secret123"}`
- Then the shop answers 200
- And the body has `success` true, a non-empty `token` and `email` "ada@example.com"
Test: DcaShop.IntegrationTests.LoginApiTest#LoggingInAnswersOkWithATokenAndTheAddress

#### a-wrong-password-is-refused
- Given an account "ada@example.com" with password "Secret123"
- When a client sends `POST /api/auth/login` with `{"email":"ada@example.com","password":"Wrong1234"}`
- Then the shop answers 401
Test: DcaShop.IntegrationTests.LoginApiTest#AWrongPasswordIsUnauthorized

### Rule: Logging out through the API is the client discarding its token

#### logging-out-answers-no-content
- Given a client holding the token of the account "ada@example.com"
- When it sends `POST /api/auth/logout`
- Then the shop answers 204 with an empty body
Test: DcaShop.IntegrationTests.LoginApiTest#LoggingOutAnswersNoContentWithAnEmptyBody

### Rule: Registering keeps the guest cart: the new account takes it over

#### registering-keeps-the-guest-cart
- Given a guest who added "Domain-Driven Design" to the cart
- When they register as Anna Schmidt, born 1990-05-17, with "anna.schmidt@example.com" and password "Secret123"
- Then the cart page shows one line "Domain-Driven Design" with quantity 1
- And the header shows "Welcome, anna.schmidt@example.com"
Test: DcaShop.E2eTests.RegistrationKeepsTheCartE2eTest#TheCartAGuestFilledBecomesTheNewAccountsCart

#### registering-from-the-checkout-keeps-the-checkout
- Given a guest whose cart holds "Domain-Driven Design", who pressed "Proceed to Checkout" and then opened `/register?returnUrl=/checkout/buyer`
- When they register as Anna Schmidt, born 1990-05-17, with "anna.schmidt@example.com" and password "Secret123"
- Then the buyer step `/checkout/buyer` opens
- And its order summary shows "Domain-Driven Design x1"
Test: DcaShop.IntegrationTests.CartStaysWithThePersonTest#RegisteringFromTheCheckoutReturnsToTheBuyerStepWithTheGuestsOrder

#### registered-customer-adds-to-the-cart
- Given a visitor who just registered with "anna.schmidt@example.com"
- When they press "Add to Cart" on the product page of "Clean Architecture"
- Then the cart holds "Clean Architecture"
Test: DcaShop.IntegrationTests.CartStaysWithThePersonTest#ACustomerWhoJustRegisteredAddsToTheirCart

#### taken-over-cart-comes-back-at-the-next-login
- Given a guest who added "Domain-Driven Design", registered with "anna.schmidt@example.com" and then logged out
- When they log in again with "anna.schmidt@example.com" and "Secret123"
- Then the cart shows one line "Domain-Driven Design" with quantity 1
Test: DcaShop.IntegrationTests.CartStaysWithThePersonTest#TheCartTakenOverAtRegistrationComesBackAtTheNextLogin

### Rule: Logging out gives the browser a fresh, empty guest cart

#### logout-leaves-an-empty-cart
- Given the customer "anna.schmidt@example.com" is logged in and their cart holds "Domain-Driven Design"
- And they pressed "Logout" in the header
- When they open the cart page
- Then the cart page shows "Your cart is empty"
Test: DcaShop.IntegrationTests.CartStaysWithThePersonTest#LoggingOutLeavesTheBrowserWithAnEmptyCart

### Rule: If a login session lapses, the visitor keeps the cart they had

#### lapsed-login-keeps-the-cart
- Given the account "anna.schmidt@example.com", registered earlier, and a browser in which a guest added `"Ubiquitous Language" Mug` and then logged in to that account
- When the login session expires after its 7 days, before the browser's visitor identity does
- Then the header shows "Login" and "Register" again
- And the cart page still shows `"Ubiquitous Language" Mug` with quantity 1
Test: DcaShop.IntegrationTests.CartStaysWithThePersonTest#ALapsedLoginOffersTheLoginAgainAndKeepsTheCart

### Rule: A browser session of the pages does not authenticate an API call

#### a-page-session-does-not-open-the-api
- Given a customer logged in on the shop's pages, holding the pages' session cookies
- When the same browser sends `GET /api/carts` with those cookies and no `Authorization` header
- Then the shop answers 401 with the header `WWW-Authenticate: Bearer`
Test: DcaShop.IntegrationTests.TokenOnlyApiTest#ThePagesSessionCookiesDoNotAuthenticateAnApiCall

### Rule: The API hands out no cookies

#### api-answers-set-no-cookie
- Given a client without any cookies
- When it registers "ada@example.com" through `POST /api/auth/register`
- Then the answer is 201 and carries no `Set-Cookie` header
Test: DcaShop.IntegrationTests.TokenOnlyApiTest#RegisteringThroughTheApiHandsOutNoCookie

### Rule: A token the shop did not sign grants nothing

#### an-altered-token-is-a-stranger
- Given the token of the customer "ada@example.com" with one character of its signature changed
- When a client sends `POST /api/products` with a valid product and that token as `Authorization: Bearer`
- Then the shop answers 401 with the header `WWW-Authenticate: Bearer`
Test: DcaShop.IntegrationTests.TokenOnlyApiTest#ATokenWithAnAlteredSignatureIsChallengedLikeNoToken
