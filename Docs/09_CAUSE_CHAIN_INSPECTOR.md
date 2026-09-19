# Cause-Chain Inspector

## Purpose

The inspector is the diagnostic half of the core product loop. It turns an observable problem into an explainable, systems-level response:

```text
World symptom → overlay or alert → inspector cause → player lever → measured feedback
```

It never ends in a dead-end statistic and never offers direct orders to individual residents.

## Entry points

| Entry | Result |
| --- | --- |
| Overlay | Select an affected resident, room, floor, or system for a focused diagnosis. |
| Alert or event | Open the relevant inspector filtered to the event's contributing causes. |
| Direct selection | Open the full inspector for the selected resident, room, or system. |
| System panel | Show aggregate state, trends, and links to representative affected entities. |

Overlays are the primary discovery tool; inspectors are the diagnosis tool.

## Resident inspector

Organise the resident view as a vertical cause chain rather than a flat stat dump:

1. **Summary:** identity, household and workplace links, plus Focus, Satisfaction, and Strain.
2. **Symptom:** a short, plain-language statement and the small active set of Thoughts and Grievances.
3. **Cause breakdown:** Needs; Satisfaction contributors; Strain drivers and visible personality-facet deltas; spatial context; and relevant social or faction context.
4. **Systems-level levers:** contextual routes to capacity, construction, policy, leasing, services, training, or zone-level priorities.
5. **Trajectory when available:** a bounded forecast explaining the likely threshold and the recovery condition.

Every adverse value must be expandable or expose a clear “why?” route. Personality is a filter, not a mystery: the inspector shows both the facet and the effect it applied.

## Room and system inspectors

The same chain applies to every subject.

- **Room:** occupancy, noise, utilities, service or production output, affected residents, and levers such as capacity, service, lease, or upgrade changes.
- **Transit:** load, wait, and transfer-lobby congestion; the downstream commute effect on Satisfaction and Strain; capacity and routing levers.
- **Scrutiny:** current value, trend, contributing factors, constraints, external-event pressure, and policy/service/crisis-response levers.

## Presentation and data rules

- Inspectors consume immutable projections and do no simulation work when opened or refreshed.
- Explanation projections include the values, contributors, driver weights, stable subject IDs, and deterministic ordering needed to answer “why?”.
- At 300 residents, system views aggregate and sample representative cases while preserving individual drill-down.
- Meaning must not depend on colour alone. Copy is direct and personal but never sarcastic, villainous, or hopeless.

## Delivery sequence

The existing transit proof establishes the canonical congestion chain. M6 work extends it as follows:

| Milestone | Inspector capability |
| --- | --- |
| OR-601 | Needs and their short-term activity effects are inspectable. |
| OR-601B / OR-602 | Focus, Satisfaction, Thoughts, Grievances, Strain, personality deltas, and contributor explanations are projected. |
| OR-603 / OR-604 | Population and Scrutiny overlays link into filtered inspectors. |
| Later systems | Services, factions, utilities, and crisis effects retain the same chain. |

## Acceptance rule

For every major failure, a player can reproduce the sequence: observe the symptom, find its pattern through an overlay or alert, identify the contributing causes in an inspector, take a systems-level action, and observe the result. The canonical elevator example must remain demonstrable end to end.
