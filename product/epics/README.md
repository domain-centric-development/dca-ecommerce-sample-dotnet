# Backlog

New work is written as markdown with front matter, one file per item:

```
product/epics/<epic>/epic.md                  intent, goal, metric (an outcome event), domain_contact, depends_on
product/epics/<epic>/<story>/story.md         status, context, acceptance criteria with named keys, assumptions
product/epics/<epic>/<story>/decisions/       the questions a run asked and the answers, the acceptances
```

The contract and the field meanings live with the pipeline, in the `factory-run` skill's
`reference/backlog-contract.md`. The story gate refuses a story whose epic is incomplete, whose
context is not on the designed map (`product/domain.md`), or which is still `status: draft`.

## The delivered work

This shop was built by porting the Java sample stage by stage; that work is recorded in the porting
log and the work packages, not as stories. Nothing is migrated into this folder — the contract
applies to **new** stories, the same brownfield rule the pipeline states for any project it enters.

Behaviour the shop already had is described by **adopted** stories (`status: adopted`), taken from the
shared specification's replay backlog: written from the shop's tests, pages and interfaces, with every epic's intent,
goal, metric and contact stated by the product owner, not reconstructed from a ticket. An adopted story is not
built again: the pipeline maps each scenario to a test that exists here and is green, writes a characterization test
where none does — shown to work by a break that turns it red — and a fresh judge reads every test against its
scenario; the adopt gate then delivers it, and it reads `status: delivered` like a built story. Every epic of the
replay backlog is in this folder, together with the stories built since.
