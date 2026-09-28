---
id: not-found-title
epic: browse-catalogue
status: approved
context: portal
title: The not-found page carries the shop's name as its title
depends_on: []
---

# The not-found page carries the shop's name as its title

## Story

As a visitor who lands on a page that does not exist I want the tab to still name the shop, so that the tab
reads the same way in both shops.

## Acceptance criteria

### Rule: The not-found page carries the shop's name as its title

#### not-found-tab-reads-the-shop-name (happy path)
Title: The not-found page is titled with the shop's name
- Given the seeded sample catalogue, in which no product has the id "no-such-product"
- When a visitor opens the product page `/products/no-such-product`
- Then the browser tab title is "domaincentric.commerce"

## Changed expectations

- The not-found page's title reads "Page Not Found" now and "domaincentric.commerce" after.

## Assumptions

- answered: the "404" code, the heading "Page Not Found", the message and both links stay as they are — CAT-03-01.
