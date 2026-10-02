namespace OneRoof.Domain.Commands
{
    public enum WordBattleAction { Start, Choose, Suspend, Resume, Close, Teach }
    public sealed class WordBattleCommand : ICommand
    {
        public WordBattleCommand(WordBattleAction action, string factionId = null, string replyId = null, int expectedRevision = -1, bool practice = false, int tier = 0, int speakerId = 0)
        { Action = action; FactionId = factionId; ReplyId = replyId; ExpectedRevision = expectedRevision; Practice = practice; Tier = tier; SpeakerId = speakerId; }
        public WordBattleAction Action { get; }
        public string FactionId { get; }
        public string ReplyId { get; }
        public int ExpectedRevision { get; }
        public bool Practice { get; }
        public int Tier { get; }
        public int SpeakerId { get; }
    }
}
