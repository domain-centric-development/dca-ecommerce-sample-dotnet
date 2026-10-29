---
id: the-mail-port-is-named-for-its-role
epic: guest-checkout
kind: task
status: draft
created: 2026-10-29T15:12:47Z
context: checkout
title: The mail port is named for its role
closes: []
depends_on: []
---

# The mail port is named for its role

`IOrderConfirmationMailPort` becomes `IOrderConfirmationMailer`: an output port is named for its role, not with
"Port" at its end — as the Java shop's `OrderConfirmationMailer` and the shop's other ports. The adapters, the
context's declaration and the glossary entries `Mail provider` and `MailProviderUnavailable` follow.

A change no actor sees: no acceptance criterion, no test stage. It runs plan, build, tidy, judge and document, and
the existing suite, run whole, is its evidence; a test that existed before it stays as it is.
