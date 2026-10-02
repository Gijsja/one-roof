using System;
namespace OneRoof.Domain.Persistence
{
    [Serializable] public sealed class WordBattleSaveData
    {
        public string contentVersion;
        public int revision, phase, speakerId, tier, stewardScore, leaderScore, exchange, sequence;
        public string factionId, learnedReplyId, lastFeedback;
        public bool practice, won;
        public float actualTensionReduction;
        public long retryDay = -1;
        public long lastLessonDay = -1;
        public string[] learnedIds, victoryIds, transcript;
    }
}
