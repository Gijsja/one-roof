namespace OneRoof.Domain.Economy
{
    /// <summary>Outcome of one indivisible outside-market transaction.</summary>
    public readonly struct OutsidePurchaseResult
    {
        public OutsidePurchaseResult(bool accepted, long cashPaid, long creditIssued)
        {
            Accepted = accepted;
            CashPaid = cashPaid;
            CreditIssued = creditIssued;
        }

        public bool Accepted { get; }
        public long CashPaid { get; }
        public long CreditIssued { get; }
    }
}
