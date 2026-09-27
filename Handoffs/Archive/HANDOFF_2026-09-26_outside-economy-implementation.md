# ECON-005 — Outside Economy and Household Hardship

> Superseded by the completion audit in `Handoffs/Archive/HANDOFF_2026-09-26_econ005-completion-audit.md`. The earlier validation covered the then-current implementation, but follow-up review found gaps in counterparty accounting, rolling hardship, room consequences, and life-cycle effects.

## Scope

Implement the linked backlog slice for outside wages and essential purchases, separate
rent arrears, household lifecycle consequences, and eventual move-out. The authoritative
design is `Planning/OUTSIDE_ECONOMY_DESIGN.md`; canonical unit-economic rules remain in
`Docs/12_ECONOMY.md`.

## Delivered

- Added an aggregate outside-market ledger and household categories for outside wages,
  essentials, quality services, care, credit and repayments.
- Changed residential rent settlement to accrue contractual rent due and transfer only
  rent actually paid to the treasury. Rent arrears are separate from food credit.
- Added a one-time callback for completed Outside Food trips and an Outside meal price of
  8, above the current tower diner ticket of 6. Outside work pays 18 per worker-day.
- Routed Outside-assigned workers to food in the outside world even when a tower diner
  exists; bounded service credit prevents free meals after its limit is reached.
- Added derived room-condition/lease projections, saved seven-day notices, typed move-out
  trips, and bounded departure history. TowerSimulation integration removes a household
  from active population only after all members arrive Outside.
- Added household and room inspector explanations for outside debt, rent arrears, condition,
  and lease phase.
- Corrected rent recovery so available cash can pay down past-due rent; a notice clears when
  rent is current again. The inspector reports actual daily cash flow, including rent catch-up
  and credit repayment.
- Kept NPCs on the canonical procedural anatomy while the photo wardrobe pieces are disabled
  because they do not match the rig proportions. Updated presentation assertions to enforce one
  coherent body style and preserve skin tint and stature variation.

## Validation

- Unity 6000.3.24f1 standalone compile: exit 0 (`/tmp/one-roof-econ005-compile-final.log`).
- Full EditMode: exit 0, 659 passed, 0 failed (`/tmp/one-roof-econ005-editmode-final.xml`).
- Full PlayMode: exit 0, 8 passed, 0 failed (`/tmp/one-roof-econ005-playmode.xml`).
- `git diff --check`: clean.
- Validation used `/tmp/one-roof-econ005-validation`, an isolated copy of the current checkout.
  Creating the documented sibling Git worktree failed because `.git` is mounted read-only; the
  Unity runner also had to run outside the sandbox to start Package Manager.
- The first EditMode run found two incorrect assertions in the new tests and two stale wardrobe
  expectations (they required the mismatched photo layers that the visual fix intentionally
  hides). Those expectations were updated; the final suites are green.

## Scope notes

- Population data has no age or life-stage fields, so lifecycle consequences are household
  financial resilience, arrears, housing condition, notice, and departure rather than age-based
  costs.
- The outside counterparty is aggregate. This slice prices the Food service and wages; other
  outside quality and care categories are available in the ledger but not yet scheduled as
  purchasable activities.
