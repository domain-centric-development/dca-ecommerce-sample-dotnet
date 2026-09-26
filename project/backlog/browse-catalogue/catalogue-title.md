---
id: catalogue-title
epic: browse-catalogue
status: approved
context: product
title: The catalogue page is titled "Product Catalog"
depends_on: []
---

# The catalogue page is titled "Product Catalog"

## Story

As a visitor I want the catalogue page's title to read "Product Catalog" so that the tab names the page the
same way in both shops.

## Acceptance criteria

### Rule: The catalogue page carries the catalogue's name as its title

#### catalogue-tab-reads-product-catalog (happy path)
Title: The catalogue page is titled Product Catalog
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the page title is "Product Catalog"

## Changed expectations

- The catalogue page's title reads "Products" now and "Product Catalog" after.

## Assumptions

- answered: the heading "Our Products" and the breadcrumb "Home / Products" stay as they are — CAT-01-01.
