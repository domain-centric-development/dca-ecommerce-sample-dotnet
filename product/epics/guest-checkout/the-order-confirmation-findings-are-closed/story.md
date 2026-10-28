---
id: the-order-confirmation-findings-are-closed
epic: guest-checkout
kind: task
status: approved
created: 2026-10-28T20:41:46Z
context: checkout
title: The order confirmation's open findings are closed
closes: [the-buyer-gets-an-order-confirmation#1, the-buyer-gets-an-order-confirmation#2, the-buyer-gets-an-order-confirmation#3, the-buyer-gets-an-order-confirmation#4, the-buyer-gets-an-order-confirmation#5]
depends_on: [the-buyer-gets-an-order-confirmation]
---

# The order confirmation's open findings are closed

Closes the five open findings of the order confirmation: the missing buyer is raised as a named failure, or answered by
the checkout session, instead of an unreachable guard with a platform exception; the processed-event record is written
inside the transaction boundary, like the other idempotent consumers, with the mail sent outside it; the comment that
cites an unrelated decision is dropped; the provider's key is named, with a note that it must stay stable; the mail
port's answer is said by its name instead of a bare boolean. Each row's fix stands in its findings row; the delivering
gate sets them done.

A change no actor sees: no acceptance criterion, no test stage. A row that turns out to change behaviour is a
`## needs-human` of the plan, and goes into a story of its own.
