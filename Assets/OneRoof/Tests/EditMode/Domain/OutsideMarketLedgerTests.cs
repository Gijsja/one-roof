using NUnit.Framework;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class OutsideMarketLedgerTests
    {
        [Test]
        public void RecordWagePayment_TracksExternalFundingAndPaysHouseholdOnce()
        {
            var household = Household(cash: 25);
            var market = new OutsideMarketState(cashBalance: 100);

            market.RecordWagePayment(household, 18);

            Assert.That(household.CashBalance, Is.EqualTo(43));
            Assert.That(household.DailyIncome, Is.EqualTo(18));
            Assert.That(household.DailyOutsideWages, Is.EqualTo(18));
            Assert.That(market.ExternalContractRevenue, Is.EqualTo(18));
            Assert.That(market.WageOutflow, Is.EqualTo(18));
            Assert.That(market.CashBalance, Is.EqualTo(100));
        }

        [Test]
        public void TryPurchase_UsesCashThenBoundedCreditAndRejectsBeyondLimit()
        {
            var household = Household(cash: 1);
            var market = new OutsideMarketState();

            var purchase = market.TryPurchase(household, 5, 4, OutsideServiceCategory.EssentialFood);
            var rejected = market.TryPurchase(household, 1, 4, OutsideServiceCategory.EssentialFood);

            Assert.That(purchase.Accepted, Is.True);
            Assert.That(purchase.CashPaid, Is.EqualTo(1));
            Assert.That(purchase.CreditIssued, Is.EqualTo(4));
            Assert.That(household.CashBalance, Is.Zero);
            Assert.That(household.OutsideCreditBalance, Is.EqualTo(4));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(5));
            Assert.That(market.PurchaseRevenue, Is.EqualTo(5));
            Assert.That(market.CreditFunding, Is.EqualTo(4));
            Assert.That(market.CashBalance, Is.EqualTo(1));
            Assert.That(market.CreditReceivableBalance, Is.EqualTo(4));
            Assert.That(market.CashBalance + market.CreditReceivableBalance,
                Is.EqualTo(household.DailyOutsideEssentialSpend));

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.CashPaid, Is.Zero);
            Assert.That(rejected.CreditIssued, Is.Zero);
            Assert.That(household.OutsideCreditBalance, Is.EqualTo(4));
            Assert.That(market.PurchaseRevenue, Is.EqualTo(5));
        }

        [Test]
        public void OutsideMeals_LeaveTowerAllowanceAvailableButBothPurchasesDrawFromCash()
        {
            var household = new HouseholdRecord(new EntityId(1),
                new[] { new EntityId(2), new EntityId(3), new EntityId(4) },
                new EntityId(5), 0.5f, 1f, cashBalance: 50);
            var market = new OutsideMarketState();

            Assert.That(market.TryPurchase(household, 8, 0, OutsideServiceCategory.EssentialFood).Accepted, Is.True);
            Assert.That(market.TryPurchase(household, 8, 0, OutsideServiceCategory.EssentialFood).Accepted, Is.True);
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(16));
            Assert.That(household.AvailableServiceSpend, Is.EqualTo(15));

            var towerPayment = household.SpendOnService(15);
            household.CompleteDailyBudgetSettlement();

            Assert.That(towerPayment, Is.EqualTo(15));
            Assert.That(household.DailyServiceSpend, Is.EqualTo(15));
            Assert.That(household.AvailableServiceSpend, Is.Zero);
            Assert.That(household.CashBalance, Is.EqualTo(19));
            Assert.That(market.CashBalance, Is.EqualTo(16));
            Assert.That(household.DailyBudgetNetFlow, Is.EqualTo(-31));

            var restored = PopulationState.FromSaveData(
                new PopulationState(null, new[] { household }).ToSaveData()).GetHousehold(household.Id);
            Assert.That(restored.CashBalance, Is.EqualTo(19));
            Assert.That(restored.DailyOutsideEssentialSpend, Is.EqualTo(16));
            Assert.That(restored.DailyServiceSpend, Is.EqualTo(15));
            Assert.That(restored.AvailableServiceSpend, Is.Zero);
            Assert.That(restored.DailyBudgetNetFlow, Is.EqualTo(-31));
        }

        [Test]
        public void OutsideCredit_DoesNotFundTowerServicePurchases()
        {
            var household = Household(cash: 1);
            var market = new OutsideMarketState();

            var outsidePurchase = market.TryPurchase(household, 8, 7, OutsideServiceCategory.EssentialFood);

            Assert.That(outsidePurchase.Accepted, Is.True);
            Assert.That(household.OutsideCreditBalance, Is.EqualTo(7));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(8));
            Assert.That(household.AvailableServiceSpend, Is.Zero);
            Assert.That(household.SpendOnService(5), Is.Zero);
            Assert.That(household.DailyServiceSpend, Is.Zero);
            Assert.That(market.CashBalance + market.CreditReceivableBalance, Is.EqualTo(8));
        }

        [Test]
        public void RecordDailyIncome_RepaysCreditFromNewIncomeAndMarketRecordsTransfer()
        {
            var household = Household(cash: 0);
            var market = new OutsideMarketState();
            market.TryPurchase(household, 4, 8, OutsideServiceCategory.EssentialFood);

            household.RecordDailyIncome(10);
            market.RecordCreditRepayment(household, household.DailyCreditRepayment);

            Assert.That(household.CashBalance, Is.EqualTo(6));
            Assert.That(household.OutsideCreditBalance, Is.Zero);
            Assert.That(household.DailyCreditRepayment, Is.EqualTo(4));
            Assert.That(household.DailyIncome, Is.EqualTo(10));
            Assert.That(market.CashBalance, Is.EqualTo(4));
            Assert.That(market.CreditRepayment, Is.EqualTo(4));
            Assert.That(market.CreditReceivableBalance, Is.Zero);
        }

        [Test]
        public void ProcessRentCycle_AccruesOnlyUnpaidRentNotFoodCredit()
        {
            var household = Household(cash: 0);
            var population = new PopulationState(null, new[] { household });
            var market = new OutsideMarketState();
            market.TryPurchase(household, 2, 10, OutsideServiceCategory.EssentialFood);
            Assert.That(household.RentArrearsBalance, Is.Zero);
            Assert.That(household.RentArrearsDays, Is.Zero);

            household.RecordRentDue(12);
            var collected = household.RecordRentPayment(12);
            household.UpdateArrearsDays();

            Assert.That(collected, Is.Zero);
            Assert.That(household.OutsideCreditBalance, Is.EqualTo(2));
            Assert.That(household.RentArrearsBalance, Is.EqualTo(12));
            Assert.That(household.RentArrearsDays, Is.EqualTo(1));
            Assert.That(household.ArrearsDays, Is.EqualTo(1));
        }

        [Test]
        public void ProcessRentCycle_IncomeRecoveryPaysDownExistingRentArrears()
        {
            var household = Household(cash: 0);
            var population = new PopulationState(null, new[] { household });
            var treasury = new TowerEconomyState(initialTreasury: 0);
            var topology = BuildingTopologyState.CreateWithFixture();

            treasury.ProcessRentCycle(topology, population);
            household.UpdateArrearsDays();
            Assert.That(household.RentArrearsBalance, Is.EqualTo(12));

            household.RecordDailyIncome(100);
            var recoveredRent = treasury.ProcessRentCycle(topology, population);
            household.UpdateArrearsDays();

            Assert.That(recoveredRent, Is.EqualTo(24));
            Assert.That(household.RentArrearsBalance, Is.Zero);
            Assert.That(household.RentArrearsDays, Is.Zero);
            Assert.That(treasury.CashBalance, Is.EqualTo(24));
        }

        [Test]
        public void TwoOutsideEarners_ThirtyDayBudgetTracksSharedHouseholdExpenses()
        {
            var members = new[] { new EntityId(2), new EntityId(3) };
            var household = new HouseholdRecord(new EntityId(1), members, new EntityId(4),
                0.5f, 1f, cashBalance: 200);
            var market = new OutsideMarketState();

            for (var day = 0; day < 30; day++)
            {
                household.BeginDailySettlement();
                market.RecordWagePayment(household, 18);
                market.RecordWagePayment(household, 18);
                household.RecordRentDue(24);
                household.RecordRentPayment(24);
                market.TryPurchase(household, 8, 2 * 45L * 8, OutsideServiceCategory.EssentialFood);
                market.TryPurchase(household, 8, 2 * 45L * 8, OutsideServiceCategory.EssentialFood);
                household.UpdateArrearsDays();
                household.CompleteDailyBudgetSettlement();
            }

            Assert.That(household.CashBalance, Is.EqualTo(80));
            Assert.That(household.RentArrearsBalance, Is.Zero);
            Assert.That(market.WageOutflow, Is.EqualTo(30 * 2 * 18));
            Assert.That(household.DailyOutsideEssentialSpend, Is.EqualTo(16));
        }

        [Test]
        public void TemporaryOneWeekIncomeShock_UsesSavingsWithoutImmediateLeaseDefault()
        {
            var household = Household(cash: 200);
            var market = new OutsideMarketState();

            for (var day = 1; day <= 30; day++)
            {
                household.BeginDailySettlement();
                if (day < 11 || day > 17) market.RecordWagePayment(household, 18);
                household.RecordRentDue(12);
                household.RecordRentPayment(12);
                market.TryPurchase(household, 8, 45L * 8, OutsideServiceCategory.EssentialFood);
                household.UpdateArrearsDays();
                household.CompleteDailyBudgetSettlement();
            }

            Assert.That(household.CashBalance, Is.EqualTo(14));
            Assert.That(household.RentArrearsBalance, Is.Zero);
            Assert.That(household.RentArrearsDays, Is.Zero);
        }

        [Test]
        public void RollingBudget_TracksSevenAndThirtyDaysAndSurvivesSaveLoad()
        {
            var household = Household(cash: 200);
            var population = new PopulationState(null, new[] { household });
            var market = new OutsideMarketState();

            for (var day = 0; day < 35; day++)
            {
                household.BeginDailySettlement();
                market.RecordWagePayment(household, 18);
                household.RecordRentDue(12);
                household.RecordRentPayment(12);
                market.TryPurchase(household, 8, 360, OutsideServiceCategory.EssentialFood);
                household.CompleteDailyBudgetSettlement();
            }

            Assert.That(household.DailyBudgetNetFlow, Is.EqualTo(-2));
            Assert.That(household.BudgetHistoryDays, Is.EqualTo(30));
            Assert.That(household.Rolling7DayNetFlow, Is.EqualTo(-14));
            Assert.That(household.Rolling30DayNetFlow, Is.EqualTo(-60));
            Assert.That(household.HasPersistentBudgetStress, Is.True);

            var restored = PopulationState.FromSaveData(population.ToSaveData()).GetHousehold(household.Id);
            Assert.That(restored.DailyBudgetNetFlow, Is.EqualTo(-2));
            Assert.That(restored.Rolling7DayNetFlow, Is.EqualTo(-14));
            Assert.That(restored.Rolling30DayNetFlow, Is.EqualTo(-60));
        }

        [Test]
        public void EssentialShortfallExposure_RisesOnDeniedServiceAndRecoversSlowly()
        {
            var household = Household(cash: 0);

            household.RecordEssentialShortfall(8);
            household.CompleteDailyBudgetSettlement();
            Assert.That(household.DailyEssentialShortfall, Is.EqualTo(8));
            Assert.That(household.UnderprovisionExposure, Is.EqualTo(8));

            household.BeginDailySettlement();
            household.CompleteDailyBudgetSettlement();

            Assert.That(household.UnderprovisionExposure, Is.EqualTo(7));
        }

        [TestCase(30)]
        [TestCase(180)]
        [TestCase(365)]
        public void SingleOutsideWorker_LongRunBudgetConnectsEssentialsToHousingLifecycle(int horizon)
        {
            var household = Household(cash: 200);
            var population = new PopulationState(null, new[] { household });
            var market = new OutsideMarketState();
            var lifecycle = new HouseholdLeaseLifecycleState();
            var lifecycleSystem = new HouseholdHousingLifecycleSystem();
            var departed = false;

            for (var day = 1; day <= horizon && !departed; day++)
            {
                household.BeginDailySettlement();
                market.RecordWagePayment(household, 18);
                household.RecordRentDue(12);
                household.RecordRentPayment(12);
                market.TryPurchase(household, 8, 45L * 8, OutsideServiceCategory.EssentialFood);
                market.RecordCreditRepayment(household, household.DailyCreditRepayment);
                household.UpdateRentArrearsDays();
                household.CompleteDailyBudgetSettlement();

                var projection = lifecycleSystem.AdvanceDaily(lifecycle, population, day);
                Assert.That(projection.Count, Is.EqualTo(1));
                if (projection[0].IsMoveOutEligible)
                {
                    lifecycle.RecordDeparture(household.Id, day, household.CashBalance,
                        household.RentArrearsBalance, household.OutsideCreditBalance);
                    departed = true;
                }
            }

            if (horizon == 30)
            {
                Assert.That(household.CashBalance, Is.EqualTo(140));
                Assert.That(household.RentArrearsBalance, Is.Zero);
                Assert.That(departed, Is.False);
            }
            else
            {
                Assert.That(departed, Is.True, "A persistent outside-only deficit should reach the existing lease lifecycle.");
                Assert.That(household.NetFinancialPosition, Is.LessThanOrEqualTo(0));
                Assert.That(lifecycle.DepartureHistory.Count, Is.EqualTo(1));
                Assert.That(lifecycle.DepartureHistory[0].FinalOutsideCreditBalance,
                    Is.EqualTo(household.OutsideCreditBalance));
            }
        }

        private static HouseholdRecord Household(long cash) => new HouseholdRecord(
            new EntityId(1), new[] { new EntityId(2) }, new EntityId(3), 0f, 0.5f, cashBalance: cash);
    }
}
