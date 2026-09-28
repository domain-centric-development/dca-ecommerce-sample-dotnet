# Backlog

New work is written as markdown with front matter, one file per item:

```
project/backlog/<epic>/epic.md        intent, goal, metric (an outcome event), domain_contact
project/backlog/<epic>/<story>.md     status, context, acceptance criteria with named keys, assumptions
```

The contract and the field meanings live with the pipeline, in the `factory-run` skill's
`reference/backlog-contract.md`. The story gate refuses a story whose epic is incomplete, whose
context is not on the designed map (`project/domain.md`), or which is still `status: draft`.

## The delivered work

This shop was built by porting the Java sample stage by stage; that work is recorded in the porting
log and the work packages, not as stories. Nothing is migrated into this folder — the contract
applies to **new** stories, the same brownfield rule the pipeline states for any project it enters.

What the shop does today comes in as **adopted** stories (`status: adopted`) from the shared specification's
replay backlog: the pipeline maps each scenario to a test that exists here and is green, writes a
characterization test where none does — shown to work by a break that turns it red — and a fresh judge
reads every test against its scenario. Adoption is incremental, epic by epic as new work needs it.
`browse-catalogue` is the first, adopted whole — CAT-01 to CAT-05 — with two changes the adoption found where
this shop differed from the Java shop: `catalogue-title` (the catalogue's title) and `not-found-title` (the
not-found page's title).
