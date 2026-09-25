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
