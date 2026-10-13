---
id: stock-checks-answer-on-the-page-accept-1
story: stock-checks-answer-on-the-page
stage: document
kind: acceptance
asked: 2026-10-13T13:12:12Z
digest: c025d9312cab5eef821651c29aacae9320b5f0adc7b7c700011fadc1395f8147
---

# Accept stock-checks-answer-on-the-page?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- start-from-an-outdated-cart-page-is-refused: Given a guest whose cart page, opened while it held "Wooden Hexagon Desk Model" five times with 8 in stock,; And another visitor has since bought it five times, leaving 3; When the guest presses "Proceed to Checkout" on that page; Then the cart page opens with the error "Cannot start checkout, 1 item(s) unavailable or out of stock" — `DcaShop.IntegrationTests.OutdatedCartStartTest#StartingFromACartWhoseLineRanShortOfStockIsRefusedNamingThatLine`
- place-order-is-refused-when-stock-ran-short: Given a guest at the review with "Wooden Hexagon Desk Model" five times; And another visitor has since bought it five times, leaving 3; When the guest presses "Place Order"; Then the review step stays open at `/checkout/review` and shows an error; And no confirmation is shown; And the product page still reads "3 units available" and the guest's cart still holds the product five — `DcaShop.E2eTests.PlaceOrderRefusedE2eTest#PlaceOrderForMoreThanTheStockLeftKeepsTheReviewOpenAndChangesNothing`
- place-order-is-refused-when-sold-out: Given a guest at the review with "Wooden Hexagon Desk Model" twice; And another visitor has since bought all 8; When the guest presses "Place Order"; Then the review step stays open at `/checkout/review` and shows an error; And no confirmation is shown — `DcaShop.IntegrationTests.PlaceOrderRefusalPageTest#ARefusedPlaceOrderOpensTheReviewAgainWithItsError`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Blömer
at: 2026-10-13T17:07:26Z
