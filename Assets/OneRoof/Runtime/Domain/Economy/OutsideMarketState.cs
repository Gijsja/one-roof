using System;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Population;

namespace OneRoof.Domain.Economy
{
    /// <summary>
    /// Aggregate ledger for external employers and service providers. The balance belongs to the
    /// market counterparty; outside wages are funded by recorded external contracts, while purchases
    /// transfer household cash or explicitly issued credit into the market.
    /// </summary>
    public sealed class OutsideMarketState
    {
        public OutsideMarketState(long cashBalance = 0, long externalContractRevenue = 0,
            long wageOutflow = 0, long purchaseRevenue = 0, long creditFunding = 0,
            long creditRepayment = 0, long creditReceivableBalance = 0, long towerContractOutflow = 0)
        {
            CashBalance = cashBalance;
            ExternalContractRevenue = Math.Max(0, externalContractRevenue);
            WageOutflow = Math.Max(0, wageOutflow);
            PurchaseRevenue = Math.Max(0, purchaseRevenue);
            CreditFunding = Math.Max(0, creditFunding);
            CreditRepayment = Math.Max(0, creditRepayment);
            CreditReceivableBalance = Math.Max(0, creditReceivableBalance);
            TowerContractOutflow = Math.Max(0, towerContractOutflow);
        }

        /// <summary>Retained outside-market cash after recorded transactions.</summary>
        public long CashBalance { get; private set; }
        public long ExternalContractRevenue { get; private set; }
        public long WageOutflow { get; private set; }
        public long PurchaseRevenue { get; private set; }
        public long CreditFunding { get; private set; }
        public long CreditRepayment { get; private set; }
        /// <summary>Outstanding outside-service credit owed to the market counterparty.</summary>
        public long CreditReceivableBalance { get; private set; }
        public long TowerContractOutflow { get; private set; }

        /// <summary>
        /// Posts a contract-funded outside wage. Contract revenue is explicit so the transfer has
        /// a named source and the household receives income exactly once through this call.
        /// </summary>
        public void RecordWagePayment(HouseholdRecord household, long amount)
        {
            if (household == null) throw new ArgumentNullException(nameof(household));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            ExternalContractRevenue = SaturatingAdd(ExternalContractRevenue, amount);
            CashBalance = SaturatingAdd(CashBalance, amount);
            household.RecordOutsideWage(amount);
            WageOutflow = SaturatingAdd(WageOutflow, amount);
            CashBalance = SaturatingAdd(CashBalance, -amount);
        }

        /// <summary>Records a tower purchase from an outside supplier after the treasury paid it.</summary>
        public void RecordTowerPurchase(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            CashBalance = SaturatingAdd(CashBalance, amount);
            PurchaseRevenue = SaturatingAdd(PurchaseRevenue, amount);
        }

        /// <summary>An external contract funds a payment to the tower operation.</summary>
        public void RecordTowerContractPayment(TowerEconomyState tower, long amount)
        {
            if (tower == null) throw new ArgumentNullException(nameof(tower));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            ExternalContractRevenue = SaturatingAdd(ExternalContractRevenue, amount);
            CashBalance = SaturatingAdd(CashBalance, amount);
            TowerContractOutflow = SaturatingAdd(TowerContractOutflow, amount);
            CashBalance = SaturatingAdd(CashBalance, -amount);
            tower.AddRevenue(amount);
        }

        /// <summary>
        /// Attempts one whole outside purchase. Cash is used first; bounded credit covers the
        /// remainder. A rejected purchase changes neither ledger.
        /// </summary>
        public OutsidePurchaseResult TryPurchase(HouseholdRecord household, long price,
            long creditLimit, OutsideServiceCategory category)
        {
            if (household == null) throw new ArgumentNullException(nameof(household));
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
            if (!household.TryPurchaseOutsideService(price, creditLimit, category,
                out var cashPaid, out var creditIssued))
                return new OutsidePurchaseResult(false, 0, 0);

            CashBalance = SaturatingAdd(CashBalance, cashPaid);
            PurchaseRevenue = SaturatingAdd(PurchaseRevenue, price);
            CreditFunding = SaturatingAdd(CreditFunding, creditIssued);
            CreditReceivableBalance = SaturatingAdd(CreditReceivableBalance, creditIssued);
            return new OutsidePurchaseResult(true, cashPaid, creditIssued);
        }

        /// <summary>Posts household credit repayments already withheld from earned income.</summary>
        public void RecordCreditRepayment(HouseholdRecord household, long amount)
        {
            if (household == null) throw new ArgumentNullException(nameof(household));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            var repayment = Math.Min(amount, CreditReceivableBalance);
            if (repayment <= 0) return;
            CashBalance = SaturatingAdd(CashBalance, repayment);
            CreditReceivableBalance -= repayment;
            CreditRepayment = SaturatingAdd(CreditRepayment, repayment);
        }

        public OutsideMarketSaveData ToSaveData() => new OutsideMarketSaveData
        {
            version = 2,
            cashBalance = CashBalance,
            externalContractRevenue = ExternalContractRevenue,
            wageOutflow = WageOutflow,
            purchaseRevenue = PurchaseRevenue,
            creditFunding = CreditFunding,
            creditRepayment = CreditRepayment,
            creditReceivableBalance = CreditReceivableBalance,
            towerContractOutflow = TowerContractOutflow
        };

        public static OutsideMarketState FromSaveData(OutsideMarketSaveData data)
        {
            if (data == null) return new OutsideMarketState();
            var creditReceivable = data.version >= 1
                ? Math.Max(0L, data.creditReceivableBalance)
                : Math.Max(0L, data.creditFunding - data.creditRepayment);
            // Version 0 added the credit-funded portion to cash at purchase and again at
            // repayment. Remove the original credit issuance when migrating that balance.
            var cashBalance = data.version >= 1
                ? data.cashBalance
                : SaturatingAdd(data.cashBalance, data.creditFunding == long.MinValue ? long.MaxValue : -data.creditFunding);
            return new OutsideMarketState(cashBalance, data.externalContractRevenue,
                data.wageOutflow, data.purchaseRevenue, data.creditFunding, data.creditRepayment,
                creditReceivable, data.version >= 2 ? data.towerContractOutflow : 0);
        }

        private static long SaturatingAdd(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right) return long.MaxValue;
            if (right < 0 && left < long.MinValue - right) return long.MinValue;
            return left + right;
        }
    }
}
