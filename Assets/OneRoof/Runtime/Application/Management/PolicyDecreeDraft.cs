using System;
using OneRoof.Application.Tower;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Management
{
    /// <summary>Transient Manage-mode draft. The simulation remains authoritative.</summary>
    public sealed class PolicyDecreeDraft
    {
        private readonly TowerSimulationSession _session;
        private PolicyDecreeState _draft;
        private long _enactedTick = -1;
        private long _baselineTreasury;
        private long _baselineRent;
        private long _baselineTax;
        private long _baselineSubsidy;
        private float _baselineScrutiny;
        private float _baselineSatisfaction;
        private float _baselineStrain;
        private long _baselineHouseholdCash;
        private long _baselineBusinessCash;
        private string _receipt = string.Empty;

        public PolicyDecreeDraft(TowerSimulationSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _draft = Current;
        }

        public PolicyDecreeState Current => _session.Simulation.Economy.Policy;
        public PolicyDecreeState Draft => _draft;
        public bool HasChanges => _draft != Current;
        public string Receipt => _receipt;
        public long CurrentDay => _session.CurrentTick / DailySchedule.TicksPerDay + 1;

        public void ResetDraft() => _draft = Current;

        public void SelectRent(float multiplier) => Replace(multiplier, _draft.CommercialTaxRate, _draft.TransitSubsidyEnabled, _draft.QuietHoursEnabled);
        public void SelectTax(float rate) => Replace(_draft.RentCapMultiplier, rate, _draft.TransitSubsidyEnabled, _draft.QuietHoursEnabled);
        public void SelectTransitSubsidy(bool enabled) => Replace(_draft.RentCapMultiplier, _draft.CommercialTaxRate, enabled, _draft.QuietHoursEnabled);
        public void SelectQuietHours(bool enabled) => Replace(_draft.RentCapMultiplier, _draft.CommercialTaxRate, _draft.TransitSubsidyEnabled, enabled);

        private void Replace(float rent, float tax, bool subsidy, bool quiet) =>
            _draft = new PolicyDecreeState(rent, tax, subsidy, quiet);

        public CommandResult CanConfirm()
        {
            if (!HasChanges) return CommandResult.Fail("Choose at least one change before confirming.", "policy:no_change");
            return _session.CanExecute(new SetPolicyDecreeCommand(_draft));
        }

        public CommandResult Confirm()
        {
            var validation = CanConfirm();
            if (!validation.Accepted) return validation;
            var old = Current;
            var before = Capture();
            var result = _session.ExecuteCommand(new SetPolicyDecreeCommand(_draft));
            if (!result.Accepted) return result;
            _enactedTick = _session.CurrentTick;
            _baselineTreasury = before.Treasury;
            _baselineRent = _session.TreasuryFlow.Rent;
            _baselineTax = _session.TreasuryFlow.Tax;
            _baselineSubsidy = _session.TreasuryFlow.Subsidy;
            _baselineScrutiny = before.Scrutiny;
            _baselineSatisfaction = before.Satisfaction;
            _baselineStrain = before.Strain;
            _baselineHouseholdCash = before.HouseholdCash;
            _baselineBusinessCash = before.BusinessCash;
            _receipt = $"DAY {CurrentDay}  /  DECREE ENACTED\n{DescribeChanges(old, _draft)}\nImmediate treasury change: $0. Scrutiny pressure may rise for high rent or tax. Daily outcomes pending settlement.";
            return result;
        }

        public string SettlementReceipt()
        {
            if (_enactedTick < 0 || _session.LastSettlementTick <= _enactedTick) return _receipt;
            var after = Capture();
            return _receipt + $"\nDAY {_session.LastSettlementTick / DailySchedule.TicksPerDay + 1}  /  OBSERVED SINCE ENACTMENT" +
                   $"\nTreasury {Signed(after.Treasury - _baselineTreasury)}; household cash {Signed(after.HouseholdCash - _baselineHouseholdCash)}; business cash {Signed(after.BusinessCash - _baselineBusinessCash)}." +
                   $"\nRent receipts {Signed(_session.TreasuryFlow.Rent - _baselineRent)}; business tax receipts {Signed(_session.TreasuryFlow.Tax - _baselineTax)}; subsidy expense {Signed(_session.TreasuryFlow.Subsidy - _baselineSubsidy)} vs previous settlement." +
                   $"\nAverage satisfaction {after.Satisfaction - _baselineSatisfaction:+0.000;-0.000;0}; strain {after.Strain - _baselineStrain:+0.000;-0.000;0}; scrutiny {after.Scrutiny - _baselineScrutiny:+0.000;-0.000;0}. Faction response: see faction inspector when available.";
        }

        public string Estimate()
        {
            var old = Current;
            var rent = _session.TreasuryFlow.Rent;
            var tax = _session.TreasuryFlow.Tax;
            var rentDelta = rent > 0 ? (long)Math.Round(rent * (_draft.RentCapMultiplier / old.RentCapMultiplier - 1f)) : 0;
            var taxDelta = old.CommercialTaxRate > 0f && tax > 0
                ? (long)Math.Round(tax * (_draft.CommercialTaxRate / old.CommercialTaxRate - 1f)) : 0;
            var subsidyDelta = _draft.DailyTransitSubsidy - old.DailyTransitSubsidy;
            var taxText = old.CommercialTaxRate == 0f && _draft.CommercialTaxRate > 0f
                ? "new tax receipts unknown until staffed business revenue is observed"
                : $"tax receipts {Signed(taxDelta)}";
            return $"Next daily cash estimate from prior receipts: rent {Signed(rentDelta)}, {taxText}, transit subsidy expense {Signed(subsidyDelta)}. Other conditions may change.";
        }

        public string Impacts()
        {
            var rent = _draft.RentCapMultiplier.CompareTo(Current.RentCapMultiplier);
            var tax = _draft.CommercialTaxRate.CompareTo(Current.CommercialTaxRate);
            var subsidy = _draft.TransitSubsidyEnabled.CompareTo(Current.TransitSubsidyEnabled);
            var quiet = _draft.QuietHoursEnabled.CompareTo(Current.QuietHoursEnabled);
            return (rent < 0 ? "Lower rent eases household costs and reduces rent receipts. Tenant Union likely benefits if burden falls.\n" : rent > 0 ? "Higher rent raises household costs and may increase arrears; Tenant Union pressure may rise.\n" : "") +
                   (tax > 0 ? "Higher tax costs staffed businesses and may increase treasury receipts; Merchant Guild and Corporate Coalition may object.\n" : tax < 0 ? "Lower tax eases business costs and reduces treasury receipts.\n" : "") +
                   (subsidy > 0 ? "Transit subsidy costs $40/day and may ease commuting strain.\n" : subsidy < 0 ? "Removing transit subsidy saves $40/day and may worsen commuting strain.\n" : "") +
                   (quiet > 0 ? "Quiet hours may help affected homes but reduce walk-in sales.\n" : quiet < 0 ? "Removing quiet hours may help walk-in sales but increase noise strain.\n" : "") +
                   (_draft.IsAggressive && HasChanges ? "High rent or tax adds scrutiny pressure on enactment. Faction direction is qualitative; measured response follows daily settlement." : "Faction direction is qualitative; measured response follows daily settlement.");
        }

        private static string DescribeChanges(PolicyDecreeState old, PolicyDecreeState next) =>
            $"Rent {old.RentCapMultiplier:0.0} -> {next.RentCapMultiplier:0.0}; tax {old.CommercialTaxRate:P0} -> {next.CommercialTaxRate:P0}; transit subsidy {(old.TransitSubsidyEnabled ? "on" : "off")} -> {(next.TransitSubsidyEnabled ? "on" : "off")}; quiet hours {(old.QuietHoursEnabled ? "on" : "off")} -> {(next.QuietHoursEnabled ? "on" : "off")}.";
        private static string Signed(long value) => value >= 0 ? $"+${value}" : $"-${-value}";

        private Snapshot Capture()
        {
            var sim = _session.Simulation;
            long householdCash = 0, businessCash = 0;
            float satisfaction = 0, strain = 0;
            foreach (var household in sim.Population.Households) householdCash += household.CashBalance;
            foreach (var business in sim.Businesses.Businesses) businessCash += business.CashBalance;
            foreach (var resident in sim.Population.Persons)
            {
                satisfaction += resident.Wellbeing.Satisfaction;
                strain += resident.Wellbeing.Strain;
            }
            var count = sim.Population.Persons.Count;
            return new Snapshot(_session.TreasuryBalance, householdCash, businessCash, sim.Scrutiny.Value,
                count == 0 ? 0 : satisfaction / count, count == 0 ? 0 : strain / count);
        }

        private readonly struct Snapshot
        {
            public Snapshot(long treasury, long householdCash, long businessCash, float scrutiny, float satisfaction, float strain)
            { Treasury = treasury; HouseholdCash = householdCash; BusinessCash = businessCash; Scrutiny = scrutiny; Satisfaction = satisfaction; Strain = strain; }
            public long Treasury { get; }
            public long HouseholdCash { get; }
            public long BusinessCash { get; }
            public float Scrutiny { get; }
            public float Satisfaction { get; }
            public float Strain { get; }
        }
    }
}
