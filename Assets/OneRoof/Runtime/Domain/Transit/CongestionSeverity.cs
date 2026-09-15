namespace OneRoof.Domain.Transit
{
    public enum CongestionSeverity
    {
        Clear,
        Moderate,
        Heavy,
        Severe
    }

    public static class CongestionEvaluator
    {
        public static CongestionSeverity Evaluate(int queueLength, long maxWaitTicks)
        {
            if (queueLength >= 20 || maxWaitTicks >= 100)
            {
                return CongestionSeverity.Severe;
            }

            if (queueLength >= 10 || maxWaitTicks >= 50)
            {
                return CongestionSeverity.Heavy;
            }

            if (queueLength >= 4 || maxWaitTicks >= 20)
            {
                return CongestionSeverity.Moderate;
            }

            return CongestionSeverity.Clear;
        }
    }
}
