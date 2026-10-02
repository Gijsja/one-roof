"""Arithmetic design experiments, NOT production crisis tests or gameplay certification."""
import json
from decimal import Decimal as D
from pathlib import Path
import sys


def rent(rule, rent=12, wage=30, initial_cash=200, initial_debt=100, spend=5):
    cash, debt, held = initial_cash, initial_debt, 0
    rows = []
    for day in range(1, 4):
        cash += wage
        prior = debt
        debt += rent
        withheld = rent - (3 * rent // 4)
        request = prior + rent if rule == 'normal' else prior + 3 * rent // 4
        if rule == 'held':
            request = max(0, prior - held) + 3 * rent // 4
        paid = min(request, cash, debt)
        cash -= paid
        debt -= paid
        if rule == 'held':
            held = min(debt, held + withheld)
        cash -= min(spend, cash)
        burden = max(D(rent) / wage, 1 - max(D(0), min(D(1), D(cash-debt)/360)))
        cause = D('.55') * burden
        rows.append(dict(day=day, requested=request, paid=paid, cash=cash, debt=debt, held=held,
                         adjustedCausePressure=float(cause)))
    return rows


def wear(mode):
    condition, latched, failures, rows = D(1), False, [], []
    for tick in range(1, 501):
        if mode == 'preventive' and condition <= D('.70'):
            latched = True
        elif mode == 'repair50' and condition <= D('.30'):
            latched = True
        if mode == 'current':
            condition = min(D(1), condition + D('.045')) if condition <= D('.30') else max(D(0), condition-D('.006'))
        elif latched:
            condition = min(D(1), condition + D('.045'))
            if condition >= (D('.80') if mode == 'preventive' else D('.50')):
                latched = False
        else:
            condition = max(D(0), condition-D('.006'))
        if condition <= D('.30'):
            failures.append(tick)
        rows.append((tick, float(condition)))
    return dict(failedTicks=len(failures), firstFailures=failures[:12], conditionAt=dict(rows[49::50]))


def water(flow=True):
    loads = [100, 100]
    rows = []
    for tick in range(120, 721, 30):
        old = loads[:]
        new = []
        for index, previous in enumerate(old):
            upstream = 0 if index == 0 else old[index-1]
            clear = flow and upstream < 25
            own = max(0, previous-10) if clear else previous
            contribution = upstream if flow else 0
            new.append(max(own, contribution))
        loads = new
        rows.append(dict(tick=tick, loads=loads[:]))
    return rows


rent_cases = {name: rent(name) for name in ('normal', 'rolling', 'held')}
relief = rent('held', rent=8)
low_cash = rent('held', wage=5, initial_cash=0, initial_debt=0, spend=0)
wear_cases = {mode: wear(mode) for mode in ('current', 'repair50', 'preventive')}
flow_water, isolated_water = water(), water(False)
# These assertions discriminate rejected alternatives; they do not validate future production adapters.
assert [r['paid'] for r in rent_cases['rolling']] == [109, 12, 12]
assert [r['paid'] for r in rent_cases['held']] == [109, 9, 9]
assert [r['debt'] for r in rent_cases['held']] == [3, 6, 9]
assert [r['adjustedCausePressure'] <= .35 for r in relief] == [False, True, True]
assert [r['paid'] for r in low_cash] == [5, 5, 5]
assert wear_cases['current']['failedTicks'] == 46
assert wear_cases['repair50']['failedTicks'] == 10
assert wear_cases['preventive']['failedTicks'] == 0
assert next(r['tick'] for r in flow_water if max(r['loads']) <= 10) == 600
assert all(r['loads'] == [100, 100] for r in isolated_water)
assert 120 + 3 * 120 == 480  # four sampled checks span 360 from the first at120

result = dict(evidence='Standalone arithmetic design models; not production implementation or Unity acceptance',
              boycott=rent_cases, cap_relief=relief, low_cash=low_cash, wear=wear_cases,
              flowing_residual=flow_water, isolated_residual=isolated_water,
              crackdown_checks=[120, 240, 360, 480], warning_at=480,
              assumptions='Funded wage/rent/spend ledgers, one maintenance specialist, continuous flow after source removal at97. Decimal differs from production float near exact boundaries; current equipment rates taken from source.')
output = Path(sys.argv[1] if len(sys.argv)>1 else '/tmp/one-roof-c0-models.json')
output.write_text(json.dumps(result, indent=2)+'\n')
print('Discriminating arithmetic checks passed; '+str(output))
