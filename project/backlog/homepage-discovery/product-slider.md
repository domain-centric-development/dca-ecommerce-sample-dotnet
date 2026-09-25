---
id: product-slider
epic: homepage-discovery
status: approved
context: Product
title: Product slider on the homepage
depends_on: []
---

# Product slider on the homepage

## Story

As a shopper I want the homepage to show me a few products of the catalogue in a slider, so that I
can reach a product without going to the catalogue first.

## Acceptance criteria

"On the desktop" is the end-user suite's default browser window; "on a phone" is a 393 px wide
viewport, as in the shop's other phone checks.

### Rule: Directly below the hero the homepage shows a slider headed "Discover products"

#### shows-discover-products-slider-below-hero
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage
- Then a slider headed "Discover products" is shown directly below the hero
- And it comes before the section "Why Shop With Us"

### Rule: The slider holds up to four random products that have a price, drawn anew on every request

#### slider-holds-four-different-products
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage
- Then the slider holds 4 product cards
- And each card shows a different product of the sample catalog

#### products-are-drawn-anew-per-request
- Given the shopper has opened the homepage and noted the 4 products in the slider
- When they reload the homepage 10 times
- Then at least one reload shows a different selection of products

#### product-without-price-is-not-offered
- Given a product of the catalogue has no price
- When the shopper opens the homepage
- Then that product is not among the cards of the slider

#### shows-the-priced-products-there-are
- Given exactly 2 products of the catalogue have a price
- When the shopper opens the homepage
- Then the slider holds 2 product cards, one for each of them

### Rule: A card shows the product's image, name and price and leads to its product page

#### card-shows-image-name-and-price
- Given the shopper has opened the homepage
- When they look at a card of the slider
- Then it shows the product's image, its name and the price its product page shows

#### card-links-to-product-page
- Given the shopper has opened the homepage
- When they follow the link of a card
- Then the product page of that card's product is shown

### Rule: On the desktop the four cards stand side by side

#### desktop-shows-four-cards-side-by-side
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage on the desktop
- Then all 4 cards are in view side by side
- And "Previous" and "Next" are both disabled

### Rule: On a phone one card is in view; Previous and Next move by one card, by mouse or keyboard, and are disabled at the start and at the end

#### phone-shows-one-card-at-a-time
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage on a phone
- Then only the first card is in view
- And "Previous" is disabled

#### next-brings-the-following-card-into-view
- Given the shopper has opened the homepage on a phone and the first card is in view
- When they press "Next"
- Then the second card is in view instead of the first

#### previous-brings-the-preceding-card-into-view
- Given the shopper on a phone has pressed "Next" once and the second card is in view
- When they press "Previous"
- Then the first card is in view again

#### next-is-operable-by-keyboard
- Given the shopper on a phone has moved the keyboard focus to "Next" with the Tab key
- When they press Enter
- Then the second card is in view instead of the first

#### next-is-disabled-at-the-last-card
- Given the shopper has opened the homepage on a phone
- When they press "Next" three times
- Then the fourth card is in view
- And "Next" is disabled

#### slider-does-not-move-by-itself
- Given the shopper has opened the homepage on a phone and the first card is in view
- When they wait 10 seconds without touching the slider
- Then the first card is still in view

## Out of scope

- Popularity — "most popular products" is the later wish; this story shows random products.
- Personalisation — every shopper gets the same kind of selection.
- A Recommendation context — a later option, not this story.
- An add-to-cart button on the card — the card leads to the product page.
- The slider in the REST API or the MCP server — page only.
- The Java sample — it follows with its own story later.

## Assumptions

- answered: Where does the slider live? — In `Product`; the homepage shows it the way it shows the mini basket, and Portal references no other context.
- answered: Is the slider shown when no product has a price? — No, it is not shown. (Today's homepage has no slider, so this holds already and is not a criterion; the plan guards it.)
- answered: Are out-of-stock products offered? — Yes, as long as they have a price.
- answered: How many cards are in view at once? — 4 side by side on the desktop, 1 on a phone; Previous and Next move by one card.
- answered: What happens at the ends? — "Next" is disabled at the last card, "Previous" at the first; paging stops at the start as it stops at the end.
- answered: Fewer than 4 products with a price? — The slider shows those there are.
- answered: Where exactly? — Directly below the hero, before "Why Shop With Us".
- answered: Does the browser test need the shared specification? — Yes: the browser-observable scenarios are in the specification's `scenarios.md` under `scenario.home.slider-*`, with an exception for the Java side (owner shop-owner, "the Java twin follows as its own story", review 2026-10-31). The two scenarios that need a catalogue other than the seeded one (a product without a price, only 2 priced products) are not shared scenarios.
