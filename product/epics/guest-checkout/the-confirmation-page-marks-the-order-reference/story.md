---
id: the-confirmation-page-marks-the-order-reference
epic: guest-checkout
kind: task
status: delivered
delivered: 2026-10-30T03:40:12Z
delivered_by: gate
created: 2026-10-28T20:41:46Z
context: checkout
title: The confirmation page marks the order reference for tests
closes: []
depends_on: [the-buyer-gets-an-order-confirmation]
---

# The confirmation page marks the order reference for tests

The confirmation page wraps the order reference in `data-test="order-reference"`, as the Java shop's page does since
the order confirmation, and the integration test reads the reference through it instead of a CSS class. The markup of
both shops stays one.

A change no actor sees: no acceptance criterion, no test stage. It runs plan, build, tidy, judge and document, and
the existing suite, run whole, is its evidence; a test that existed before it stays as it is.
