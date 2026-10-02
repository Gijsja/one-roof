# Deep civil design validation — 2026-10-02

Read-only project investigation. This file contains revised recommendations for coordinator review, not an implemented or accepted runtime contract. No production tests were run. All numeric calculations below are design calculations of inspected production formulas, not execution evidence. This analysis supersedes my shallow proposal where explicitly identified.

## Material defects established from source

1. **The earlier boycott does not sustain withholding.** With funded rent12/day, request old debt100% plus new rent75% gives collections9,12,12 and debt3,3,3; three-day cumulative delay is3, not9. Calling each day's3 attribution cumulative financial loss is misleading. It is gross intended withholding, repeatedly offset by old-debt recovery.
2. **Earlier recovery self-locks.** RentArrearsDays rises whenever rent debt remains, and FactionState uses ArrearsDays/3 as up to .35 tenant pressure. Even a financially healthy household cannot reach proposed .25 raw pressure while boycott creates ongoing arrears: pressure floor .35 once debt persists three days. Active ending is therefore generally containment regardless of effective policy.
3. **A .7 rent cap is not guaranteed relief.** Rent burden=max(rent/income,1-Budget), Budget=clamp((cash-outsideCredit-rentDebt)/(members*360)). With low reserves, the 30-day reserve term dominates. Lower nominal rent does not instantly fix it. Net position is unchanged by withholding because retained cash and debt cancel.
4. **Labor does not cover arbitrary service workers.** FactionState computes Corporate Coalition score only for content starting `commercial:office`. Diner/clinic/workshop workers have corporate score0 and cannot organically organize this chain. My prior generic-workplace proposal overstated coverage.
5. **Service-only labor eligibility is impossible at existing organization threshold.** Corporate score=.5*serviceStress+.3*commuteStress+.2*businessStress. Maximum service-only score is.5, below direct-pressure .55 and membership .55. Business-only max.2 and commute-only max.3. A chain requires combined adverse causes.
6. **Post-settlement faction evaluation uses pre-settlement wellbeing.** TowerSimulation.AdvanceOneTick updates Wellbeing before payroll/rent, then later evaluates Factions after rent/housing. Factions reads pre-settlement affordability/commute/service plus freshly updated arrears/business cash: mixed observation provenance. A policy immediately before boundary changes new rent and wellbeing, but debt/cash recovery is not reflected until subsequent Wellbeing.Advance.
7. **Recovery cannot safely use Supports lookup as sole source.** FactionState drops supports whose smoothed value<.15; missing support is not absent Resident or measured pressure0. Recovering participants eventually lose supports. Recompute direct observations from live production metrics, independent of membership/support retention.
8. **Frozen scope needs business and employee identity.** Business re-leasing keeps room ID while replacing business ID; targeting the room alone penalizes a new tenant. Employee reconciliation can remove insolvency employees. A person moving to another workplace must not export a frozen incident there.
9. **Current global output modifier is recruitment, payroll and demand interference.** TowerSimulation uses CivilActions.BusinessOutputMultiplier at leasing pulses and payroll/business settlement. Replace all civil-global uses, not only ProcessBusinessCycle; otherwise unrelated wages/recruitment change.
10. **FactionPressureSystem does not exist under Assets.** `rg FactionPressureSystem Assets` returns no match. FactionState.Evaluate is authoritative direct-score generation. ResidentSocialInfluenceSystem changes satisfaction/social concerns, not the direct civil score.

## Exact existing production equations and provenance

Paths below are under Assets/OneRoof/Runtime/Domain unless stated otherwise.

- Population/HouseholdRecord.cs: CalculateRentBurden, Budget, NetFinancialPosition, RecordRentDue, RecordRentPayment, UpdateRentArrearsDays/UpdateArrearsDays. Rent payment=min(request,cash,rent debt), does not create negative household cash. DailyBudgetNetFlow subtracts full contractual DailyRentDue rather than paid rent; withholding cannot fake a positive operating household budget.
- Population/ResidentWellbeingSystem.cs: Advance computes rentDue=round(members*12*cap, AwayFromZero); affordability=1-burden. ArrearsDays>30 caps affordability at.2; seven-day negative budget flow caps at.55. Commute=clamp(1-.22 if currently commuting-averageElevatorWait*.01+.1 if transit subsidy). Service=clamp(.8*serviceEfficiency), capped to.6 at underprovision exposure>=32 and.35 at>=96. Critically ordinary wait affects every Resident, rather than local route wait.
- Social/FactionState.cs: tenant direct T=.55*rentStress+.35*min(ArrearsDays/3,1)+.1*commuteStress. Corporate C=.5*serviceStress+.3*commuteStress+.2*businessStress for office workers only. BusinessStress is1 insolvent,.7 cash<0,else0. Driver is maximum unweighted stress, ties prefer rent/service then arrears/commute. Driver must stay the same for three evaluations. Smoothed support S'= .6*S+.4*direct, with first-ever support=direct. Membership requires S>=.55 for two days. GrievanceDays requires direct>=.55 and unchanged driver for three days.
- Social/FactionState.BuildOrganizedSignals: candidate membership+three grievance days+direct>=.55; recent strong edge affinity>=.4/contact<=2880 ticks/same driver/home-floor distance<=1; largest component selected. SignalPressure=meanDirect-earnedRespect, floored0. Earned respect is .1 per unique validated Word Battle victory tier, maximum.3.
- Social/FactionState.Connect: first normal edge affinity0; daily positive decay.01, shared named grievance adds.04 after second consecutive contact. In an isolated repeatedly co-present shared-grievance pair, resulting affinities0,.04,.07,...,.40 at day14, not day3. Day14 Warning/day15 Active is feasible without artificially seeded friendship; Social interactions can strengthen earlier, so this is controlled lower-path fixture timing, not general timing.
- Population/ResidentSocialInfluenceSystem.cs: bounded one-hop prior-boundary grievances, max total.12/max issue.04; does not add to FactionState direct pressure.
- Economy/TowerEconomyState.cs: CalculateResidentialRentDue uses household membership and global cap .7/1/1.3; ProcessRentCycle currently applies global float multiplier to whole outstanding liability.
- Economy/BusinessState.cs: employee order prefers relevant specialization then ID; ProcessPayroll funds first productive-capacity employees if resulting business cash>=-100; otherwise no wages and no paid output. Wage rates None/Service30, Maintenance/Security35, Knowledge45. Contract rate office55; operating upkeep35; opening capital500. Insolvent business employees are cleared on reconciliation. Office rent8 per cell, cap applies; tax rounds gross*rate AwayFromZero.
- Population/HouseholdHousingLifecycle.cs: Worn7/Degraded14/AtRisk31/notice45 arrears days, notice7 days; insolvency can issue earlier notice. Departure requires eligible household and every member outside/not travelling. TowerSimulation.CompleteDepartedHouseholds preserves debt/cash in departure and outside-market ledger before removing members.

No individual household rent command exists in Domain/Commands. SetPolicyDecreeCommand validates through TowerSimulation.CanExecute; constructor PolicyDecreeState accepts caps .7,1,1.3, documented tax tiers, transit subsidy and quiet hours. ApplyPolicyDecree records aggressive scrutiny for high rent/high tax. Existing AddElevatorCar/UpgradeElevatorTier/topology commands affect transit capacity, subject to treasury/prerequisite checks. Capacity response changes actual wait indirectly; subsidy grants an immediate .1 wellbeing commute bonus, so it can lower civil pressure without measured queue reduction. Do not present that bonus as physical transit recovery.

## Sensitivity and attainability

Tenant threshold direct .55 is stringent without arrears. With commute0, arrears0, rentStress must be1. At rentStress.6 and max arrears, T=.68. With max arrears and good commute, T cannot fall below.35. At initial one-member cash200/no debt/income30/rent12: after payment cash218, Budget.606, burden max(.4,.394)=.4, T=.22; not eligible. At debt100/cash100/income30 after only cash-limited payment, debt persists, burden near1; T approaches .9.

To attain previous recovery .25 with no arrears/commute, burden<=.454545, net financial reserve>=196.364 per member, and rent/income<=.454545. Full outside wage24 with normal rent12 has burden ratio.5 and T>=.275, so even high-reserve outside workers cannot reach.25 without cap relief. Knowledge45 normal rent12 gives ratio.267 and minimum T.147; generic30 gives.4 and minimum.22. Cap.7 gives rounded one-member rent8; outside24 ratio.333 and minT.183. For two members rent17; if only one outside income24, ratio.708 and minT.390.

Respect/entry table (direct component mean; membership still independent):

| direct | respect0 | respect.1 | respect.2 | respect.3 |
|---|---:|---:|---:|---:|
| .55 | .55 Active-eligible | .45 Active-eligible | .35 Warning only | .25 Clear |
| .60 | .60 | .50 | .40 Active boundary | .30 Warning boundary |
| .70 | .70 | .60 | .50 | .40 Active boundary |

Consequently raw .30/.40 civil thresholds cannot be hit through production candidates without respect: direct candidates are>=.55. Synthetic CivilActionSignal tests establish state boundaries, not production attainability. Word Battle can prevent activation despite unchanged material causes. Preserve that current earned agency; record RespectContained distinctly from CauseRelieved if later adopted. Do not call respect causal repair. This recommendation keeps active recovery independent of respect.

Corporate examples: healthy service stress.2, commute0, business0 gives.1. Max wait commuting stress1 plus negative cash .7 gives .1+.3+.14=.54 (still no membership/organizing). ServiceStress.22 with same other stresses gives.55 exact. ServiceStress.7, commuteStress.8, healthy business gives.35+.24=.59, service driver? commute.8 wins. Service driver case service.9/commute.4/business0 gives.57. Continuity driver business.7/service.4/commute.7 ties choose commute; business.7/service.6/commute.4 gives.3+.12+.14=.56, business driver .7. Insolvent score1 is possible, but no paid employee after reconciliation, so cannot assume labor membership remains actionable.

Boundary timing makes labor escalation measured after first affected settlement. At chosen direct .59, respect.2 gives.39 and warning cannot activate. Three-day timer leaves one slowdown and two strike intervals. Ensure explanation states this duration.

## Alternatives and chosen revised recommendation

### Money

A. Existing global .75 on all debt: smallest edit; overbroad and repeatedly suppresses prior arrears, unacceptable scope.
B. Previous new-rent75%+all-old-debt100%: scoped/conserved/simple, but financial effect is mostly a rolling one-day delay and attributed totals must be labelled gross instruction, not cumulative loss. Legitimate if design explicitly wants a collection delay instead of substantive boycott.
C. **Chosen recommendation: hold ONLY incident-attributed new-rent withholding until exit; still collect ordinary prior arrears100%.** Each day withhold ceil(R/4), accumulating saved held liability H; do not compound percentages on H. Record full R in existing rent debt. Request max(0,priorDebt-Hprior)+floor(3R/4). Payment remains cash-limited; prior ordinary unpaid debt remains ordinary debt. New H=Hprior+ceil(R/4), capped by actual debt. Once phase exits release hold, leaving debt unchanged and request all liability at next settlement. No refund, confiscation, escrow cash creation or forgiveness. This produces bounded meaningful lost cashflow9 across three rent12 days and preserves old arrears treatment. It requires a per-household civil hold adapter but no second authoritative debt ledger.

If coordinator insists B, document the rolling-delay outcome and remove any fixture expecting sustained loss. Do not accept B while describing C effects.

### Measurement and recovery

A. Current organized signal absent -> recovery: false healthy from loss of membership/contact/respect; reject.
B. Raw participant pressure<=.25: precise but self-generated arrears floor makes causal recovery impossible; reject unchanged.
C. **Chosen recommendation:** recompute current direct pressure for onset organizers using the same formula and actual measured affordability/commute/service, but substitute **ordinary rent-arrears consecutive days**, excluding incident-held debt only. Save ordinaryArrearsDays seeded to real days at onset; after each settlement ordinaryDebt=max(0,totalRentDebt-heldDebt), increment days if>0 else reset. Keep real household ArrearsDays/housing unchanged: boycott can still harm lease stability, honestly shown as aftermath. Measure adjustedPressure<=.35 AND reduction>=.20 from onset for two finalized observations. This is below organization.55 and requires material improvement, not merely contact withdrawal. .35 chosen rather than .25 enables moderate reserve recovery without claiming fully healthy affordability; Inspector must label ordinary-cause relief separately from residual debt/household pressure.

Preserve three/four affected settlements then containment and three-day cooldown; do not stretch timers silently. Effective response may legitimately end by containment if reserve stress is slow. Keep last rawPressure AND adjustedCausePressure plus excluded-held-debt provenance. Candidate thresholds unchanged. One underlying predicate evaluator should produce both raw and attributed observations, not duplicated competing scoring code.

### Labor and recruitment

Choose office-only beta labor scope consistent with current corporate model. State that isolated service disruption cannot trigger without a second measured cause. Broad workplace labor participation would require revised faction scoring/membership and belongs explicit contract expansion. Do not change .55 membership to force easy fixtures.

Keep frozen business+Resident identity; eligible employees are the actual payroll-selected set at onset, not all WorkplaceRoomId holders. Integrate productive staff by integer milli-worker sum: participating paid staff800 during slowdown,0 strike; ordinary paid staff1000. Office contract gross=floor(productiveMilliWorkers*55*ordinaryOccupancy/1000), with final deterministic rounding. Payroll uses ordinary occupancy/capacity and existing funding rules; employee reconciliation/leasing use ordinary occupancy, never civil modifier. Preserve wages of staff who would normally be paid; do not promise all employed staff wages regardless of capacity.

For office-only strike this narrow method does not change diner service budgets; offices losing contracts have no direct extra service multiplier. A broad restaurant variant must adapt actual productive walk-in capacity and service-budget allocation, rather than retaining PaidStaffCount as productivity. Current formula multiplying total headcount ratio into occupancy can floor away an entire worker or apply a worker's penalty to a paid subset they were not part of; reject that implementation shortcut.

## Worked multi-day ledgers (one member; no credit; spend5 after rent)

Rows report end cash/debt and treasury rent only; other transfers occur to employer/service provider and conserve money. Start cash200, prior debt100, wage30; normal rent12. Affordability reserve uses cash-debt net; withholding leaves net unchanged versus ordinary full collection when funded.

| Rule | Day1 paid/cash/debt | Day2 | Day3 | Total rent |
|---|---|---|---|---:|
| Full collection |112/113/0|12/126/0|12/139/0|136|
| Previous B |109/116/3|12/129/3|12/142/3|133|
| Chosen C |109/116/3(H3)|9/132/6(H6)|9/148/9(H9)|127|

Unchanged net reserves113,126,139 imply rent stress .686,.65,.614. Adjusted pressure .377,.3575,.3378; only final day <=.35, so no two-sample relief: **TimedContainment**. Day4 release hold, collect21, end cash152/debt0. Treasury totals catch up; this is liquidity disruption, not permanent wealth destruction. If the initial prior debt had zero recorded arrears days, raw pressure adds .1167,.2333,.35; if onset was pre-existing arrears>=3 (as in the relief example), it instead adds .35 on all three days. Raw pressure remains high during boycott.

Effective cap applied before Day1: new rent8, withholding2/day. Day1 paid106,cash119,debt2,H2; Day2 paid6,cash138,debt4,H4; Day3 paid6,cash157,debt6,H6. Net117,134,151 gives cause pressure .37125,.345278,.319306. Initial cash200-debt100=100: initial rent stress.722222 plus max arrears .35 gives onset pressure.747222 (assume onset pre-existing arrears>=3). Reduction>=.20 on Days2/3 and <=.35 twice -> CauseRelieved at Day3; residual debt6 remains. Day4 collect14, cash168,debt0. Policy does not fake cash or waive debt.

Low cash: startcash0/debt0,income5/rent12/no service purchase. Day1 request9,paid5,cash0,debt7,H3,ordinaryDebt4,ordinaryDays1. Day2 request13,paid5,debt14,H6,ordinaryDebt8,days2. Day3 request17,paid5,debt21,H9,ordinaryDebt12,days3. Without boycott payment is also5 each day: realized marginal collection loss0, while intended held rent9 is not all actual lost revenue. Net budget remains negative; no CauseRelieved. Explanation must expose requested vs actual; do not add punitive fees.

Respect: direct.6 and respect.2 still permits onset at effective.4; respect.3 blocks onset at.3 even though material pressure.6. After onset earned respect does not alter material recovery measurements or cancel liability; containment timer applies if cause remains. This preserves no individual orders and honest cause-chain accounting.

Recruitment: new third coworker/household after onset receives ordinary rent/payroll and productivity; joins no existing frozen scope. Later incident may include them if membership/contact/grievance requirements pass. Existing participant changing employers stops strike effect at old employer without exporting it to new employer; retains history. Replacement business in same room receives no inherited effect.

Moveout: if an onset household is already at arrears44, its next affected settlement may issue45-day notice; financial insolvency can do so earlier. Hold attribution never cancels household notice. On departure total rentDebt and outsideCredit travel through RecordDeparture/OutsideMarket.RecordHouseholdDeparture; remove civil live scope, retain incident H attribution as historical outstanding amount, never reconstruct collectable debt against a missing household. If all actionable participants gone, ScopeRemoved not CauseRelieved. Partial removal cannot improve historical averages by rewriting onset; current mean over remaining organizers must show removed count; record adverse departures separately for City gates.

## Exact integration map

| Production function/field | Required narrow change |
|---|---|
| FactionState.Evaluate scores/drivers loop | Extract reusable finite-validated direct observation evaluator; expose observations for every live requested Resident even when support dropped; no additional influence model. |
| FactionState.BuildOrganizedSignals | Chain-specific eligible candidate predicate before component selection; distinct household minimum, office valid payroll employment; retain stable component tie order. |
| TowerSimulation.AdvanceOneTick | Capture consistent pre-opening finalized observations; current Wellbeing-before-rent provenance must be deliberate. Recompute affordability metrics after settlement for cause evaluation without rerunning strain/thought side effects. Preserve actual tick/time ownership. |
| TowerEconomyState.ProcessRentCycle | Household-specific hold adapter; snapshot prior debt/H, record bill once, request ordinary old debt+75%new bill; actual payment receipts and held attribution idempotent. |
| HouseholdRecord.RecordRentDue/RecordRentPayment | Retain sole debt/cash authority. H never exceeds resulting debt; expose internal receipt data to adapter. |
| CivilActionState.Evaluate/records | Save incident IDs/frozen participants/households/businesses/payroll scope/H/ordinaryArrearsDays/raw and adjusted pressure/counters/last application boundary; no scene state. |
| BusinessState.ProcessPayroll / BusinessRecord.ProcessPayroll | Preserve ordinary capacity/funding, retain deterministic actual paid IDs, add productiveMilliWorkers distinct from PaidStaffCount. |
| BusinessRecord.CalculateContractRevenue | Apply productive milli-worker contribution once with ordinary occupancy; no global .8. |
| TowerSimulation leasing occupancy uses | Remove CivilActions.BusinessOutputMultiplier from Advance/MatchExistingOutsideWorkers/EvaluateLeasingDemand; strike does not recruit or evict staff. |
| BusinessState.Advance/ReconcileEmployees | Respect frozen business identity; distinguish payroll absence vs topology removal; no new-tenant inheritance. |
| HouseholdHousingLifecycleSystem.AdvanceDaily/CompleteDepartedHouseholds | Preserve real total arrears consequences; prune civil targets on actual departure, preserve historical debt. |
| CivilActionSaveData / TowerSaveData / DecisionRecordState | Serialized integration, old-save unknown provenance not invented H, last receipts prevent replay; new pressure fields unavailable until observed. |
| TowerInspectionService/TowerDataOverlays | Show raw versus adjusted cause, held ordinary debt breakdown, requested/actual receipts, payroll/productive slots, cause relief versus containment; FactionTension/Satisfaction for boycott, BusinessHealth/ElevatorWait for labor. |

## Feasible fixtures and honesty boundaries

Existing FactionStateTests.LocalOrganizationRequiresSustainedConnectedPressureAndRecoversOnRelief seeds save payloads; it proves serialization/filter logic, not organic organizing. FiftyResidentFixture relief injects cash (-10000 then+30000) and period1; useful stress model, not actual Steward response evidence. BusinessCycleTests payroll funding, opening capital and unpaid-output tests supply real economic prerequisites. TowerTreasuryAndPolicyTests rent-affordability/conservation/prolonged arrears tests should remain regressions.

1. Formula fixtures use actual HouseholdRecord/ResidentWellbeingSystem/FactionState.Evaluate, real office Room/BusinessRecord and funded EmployeeIds. Supply exact cash/debt/credit/income/actual wait/service efficiency; compare computed scores, no CivilActionSignal substituted for production proof.
2. Organic pair fixture: two separate one-member households on adjacent home floors, shared existing office/work bucket <=7 contacts, sustained same named rent grievance, full production1440 boundaries. Restrict unrelated social events to explain controlled day14 friendship; assert tie actually>=.4 before civil onset. If production Social strengthens earlier, assert causal prerequisite rather than hardcoding day14 outcome.
3. Existing friend fixture seeded valid save edge affinity.8 for focused chain integration; explicitly classify as load-continuation fixture, not organic acquisition. Run support/grievance windows from production observations, not saved ready counters.
4. Boycott scenarios use above funded/low-cash/relief ledgers through real ProcessRentCycle adapter. Policy relief is real SetPolicyDecreeCommand; record raw and adjusted pressure and actual debt. Add credit-first repayment scenario because RecordDailyIncome auto-repays outside credit, reducing available rent cash before collection.
5. Labor office fixture use actual paid Knowledge45/None30, initial capital sufficient, service efficiency .125 -> serviceStress.9, controlled average wait40 noncommuting -> commuteStress.4, business nonnegative -> C.57/service driver. Production service shortage plus wait; demonstrate service repair while commute remains (.5*.2+.3*.4=.22) reduces direct pressure. Do not use service-only .5 as positive eligibility.
6. Labor cash-stress fixture solvent businesscash<0 but >=-100 after payroll, sufficiently low wages/capacity; ensure payroll funds before asserting loss. A two-Knowledge-worker businesscash-1 cannot fund90 if condition cash-wages>=-100 succeeds (-91), but subsequent rent/upkeep likely insolvency: capture that explicitly, not pretend multi-day stable continuity. Capital/setup needed for long strike fixture.
7. Partial payroll fixture: employed4 but ordinary paid subset2, only one of paid two participates; productive slowdown1.8 worker equivalents, strike1.0; nonpaid participant causes no additional lost output. Verify unaffected business payroll/recruitment/contract unchanged.
8. Replay fixtures: before bill/after bill, before evaluation, severity transition, cause relief, timer exit, hold release, departure/release replacement. Compare real future transfers and ordered effects. Round rent8,12,16 and two-member17; no float withholding.
9. Threshold fixture distinguishes synthetic signal state tests (.30/.40) from achievable production respect cases .60-.20=.40 and .60-.30=.30. Float tolerance is test comparison only; exact runtime rounding policy must be fixed before implementation.
10. Commands/production runtime integration and graphical inspector paths remain NOT RUN. No seed bypass, test mock readiness or cash injection may be reported as a successful real systems intervention.

## Decision required before C0 can honestly remain DONE

The prior integrated contract is internally precise but economically misleading and recovery-impossible for self-created arrears. Coordinator must explicitly choose rolling delay B or sustained current-bill hold C, and decide whether attribution-adjusted recovery is acceptable. My recommendation is C plus saved ordinary-arrears cause measurement, .35/reduction.20 two-sample relief, office-only labor and paid-worker productivity. These change design, not implementation status. Downstream API owners need these decisions; they must not invent semantics during coding.
