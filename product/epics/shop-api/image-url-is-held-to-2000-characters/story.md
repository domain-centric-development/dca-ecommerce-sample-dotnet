---
id: image-url-is-held-to-2000-characters
epic: shop-api
status: delivered
delivered: 2026-10-13T17:10:50Z
delivered_by: gate
context: product
feature: assortment
title: An image URL is held to 2000 characters
depends_on: [created-product-answers-with-its-price]
---

# An image URL is held to 2000 characters

## Story

As a staff member who creates a product I want an overlong image URL refused, so that the catalogue holds only
addresses a browser can load.

## Acceptance criteria

### Rule: A description and an image URL are at most 2000 characters long

#### an-overlong-image-url-is-refused (happy path)
- Given a client holding a staff token
- When it creates a product with SKU "IMG-001", name "Big Picture", an `imageUrl` of 2001 characters, price 9.99, category "Books" and stock 1
- Then the shop answers 400 with an `application/problem+json` body
- And its title is "Unacceptable value" and it carries no detail

## Changed expectations

- A product with an image URL of 2001 characters is created (201) now and refused 400 "Unacceptable value" after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `creating-a-product-refuses-a-bad-sku-01`).
