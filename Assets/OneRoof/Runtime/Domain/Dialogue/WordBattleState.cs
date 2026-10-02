using System;
using System.Collections.Generic;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Social;
namespace OneRoof.Domain.Dialogue
{
    public enum WordBattlePhase { Idle, Active, Suspended, Result, ResidentChat }
    /// <summary>Event-driven learned wit aggregate. IDs and encounter order survive save/load.</summary>
    public sealed class WordBattleState
    {
        private readonly HashSet<string> _learned = new HashSet<string>();
        private readonly HashSet<string> _victories = new HashSet<string>();
        private readonly List<string> _transcript = new List<string>();
        private int _exchange, _sequence;
        private long _retryDay = -1;
        private long _lastLessonDay = -1;
        public WordBattleState() { WordBattleCatalog.Validate(WordBattleCatalog.Replies); _learned.Add(WordBattleCatalog.Replies[0].Id); }
        public WordBattlePhase Phase { get; private set; }
        public int Revision { get; private set; }
        public string FactionId { get; private set; }
        public int SpeakerId { get; private set; }
        public int Tier { get; private set; }
        public int StewardScore { get; private set; }
        public int LeaderScore { get; private set; }
        public bool Practice { get; private set; }
        public bool Won { get; private set; }
        public string LastFeedback { get; private set; } = "";
        public string LearnedReplyId { get; private set; }
        public float ActualTensionReduction { get; private set; }
        public bool OwnsInput => Phase == WordBattlePhase.Active || Phase == WordBattlePhase.Result || Phase == WordBattlePhase.ResidentChat;
        public bool RepertoireComplete => _learned.Count == WordBattleCatalog.Replies.Count;
        public int MasteredTiers { get { int count = 0; for (int i = 1; i <= 3; i++) if (_victories.Contains(Key(FactionIds.MerchantGuild, i))) count++; return count; } }
        public IReadOnlyList<string> Transcript => _transcript.AsReadOnly();
        public IReadOnlyList<WordBattleReply> LearnedReplies { get { var result = new List<WordBattleReply>(); foreach (var r in WordBattleCatalog.Replies) if (_learned.Contains(r.Id)) result.Add(r); return result.AsReadOnly(); } }
        private WordBattleReply Current => WordBattleCatalog.Replies[(Tier - 1) * 4 + (int)(((long)_sequence + _exchange) % 4)];
        public string ChallengeText => Phase == WordBattlePhase.ResidentChat ? "Words can share a burden, too. May I offer a line to you?" : Tier >= 1 && Tier <= 3 && (Phase == WordBattlePhase.Active || Phase == WordBattlePhase.Suspended) ? (_sequence % 2 == 0 ? Current.Challenge : Current.Variant) : "";
        public IReadOnlyList<WordBattleReply> Choices
        {
            get {
                var choices = new List<WordBattleReply>();
                if (Phase == WordBattlePhase.ResidentChat) choices.Add(new WordBattleReply("wit:listen", 0, "", "A fair point. Teach me the reply.", "", "", ""));
                else if (Phase == WordBattlePhase.Active) foreach (var r in WordBattleCatalog.Replies) if (r.Tier == Tier && _learned.Contains(r.Id)) choices.Add(r);
                return choices.AsReadOnly();
            }
        }
        private static string Key(string faction, int tier) => faction + ":" + tier;
        public int NextTier(string faction) { for (int tier = 1; tier <= 3; tier++) if (!_victories.Contains(Key(faction, tier))) return tier; return 3; }
        public CommandResult CanExecute(WordBattleCommand command, long day, bool speakerExists)
        {
            if (!Enum.IsDefined(typeof(WordBattleAction), command.Action)) return CommandResult.Fail("Unknown word battle action.");
            if (command.ExpectedRevision != Revision) return CommandResult.Fail("The conversation has moved on.", "dialogue:stale_revision");
            switch (command.Action)
            {
                case WordBattleAction.Start:
                    if (Phase != WordBattlePhase.Idle) return CommandResult.Fail("Finish or resume the existing conversation.");
                    if (command.FactionId != FactionIds.MerchantGuild) return CommandResult.Fail("This faction has no authored word battle yet.");
                    if (!speakerExists) return CommandResult.Fail("No Resident can speak for this faction.");
                    int tier = command.Tier == 0 ? NextTier(command.FactionId) : command.Tier;
                    if (tier < 1 || tier > NextTier(command.FactionId)) return CommandResult.Fail("Win the preceding tier first.");
                    bool defeated = _victories.Contains(Key(command.FactionId, tier));
                    if (command.Practice && !defeated) return CommandResult.Fail("Practice requires a previously defeated tier.");
                    if (!defeated && day <= _retryDay) return CommandResult.Fail("Return on the next in-game day to try again.");
                    return CommandResult.Success();
                case WordBattleAction.Teach:
                    if (day <= _lastLessonDay) return CommandResult.Fail("A Resident has already shared a reply today. Return tomorrow.", "dialogue:lesson_daily_limit");
                    return Phase == WordBattlePhase.Idle && speakerExists && !RepertoireComplete ? CommandResult.Success() : CommandResult.Fail("No new Resident lesson is available.");
                case WordBattleAction.Choose:
                    if (!speakerExists) return CommandResult.Fail("The spokesperson is no longer here.");
                    if (Phase == WordBattlePhase.ResidentChat && day <= _lastLessonDay) return CommandResult.Fail("A Resident has already shared a reply today. Return tomorrow.", "dialogue:lesson_daily_limit");
                    if (Phase == WordBattlePhase.ResidentChat) return command.ReplyId == "wit:listen" ? CommandResult.Success() : CommandResult.Fail("Choose the offered acknowledgement.");
                    if (Phase != WordBattlePhase.Active) return CommandResult.Fail("There is no active exchange.");
                    if (string.IsNullOrEmpty(command.ReplyId)) return CommandResult.Success();
                    foreach (var r in Choices) if (r.Id == command.ReplyId) return CommandResult.Success();
                    return CommandResult.Fail("That reply has not been learned for this tier.");
                case WordBattleAction.Suspend: return Phase == WordBattlePhase.Active ? CommandResult.Success() : CommandResult.Fail("There is no battle to suspend.");
                case WordBattleAction.Resume: return Phase == WordBattlePhase.Suspended && speakerExists ? CommandResult.Success() : CommandResult.Fail("The suspended battle cannot resume.");
                case WordBattleAction.Close: return Phase == WordBattlePhase.Result || Phase == WordBattlePhase.ResidentChat || ((Phase == WordBattlePhase.Suspended || Phase == WordBattlePhase.Active) && !speakerExists) ? CommandResult.Success() : CommandResult.Fail("Finish or suspend the battle first.");
                default: return CommandResult.Fail("Unknown word battle action.");
            }
        }
        internal void Execute(WordBattleCommand command, long day, int speaker, FactionState factions)
        {
            switch (command.Action)
            {
                case WordBattleAction.Start:
                    FactionId = command.FactionId; SpeakerId = speaker; Tier = command.Tier == 0 ? NextTier(FactionId) : command.Tier;
                    Practice = _victories.Contains(Key(FactionId, Tier)); ResetEncounter(); Phase = WordBattlePhase.Active; _transcript.Add(ChallengeText); break;
                case WordBattleAction.Teach:
                    FactionId = null; SpeakerId = speaker; Tier = 0; Practice = true; ResetEncounter(); Phase = WordBattlePhase.ResidentChat; _transcript.Add(ChallengeText); break;
                case WordBattleAction.Choose:
                    if (Phase == WordBattlePhase.ResidentChat) { Learn(null); _lastLessonDay = day; Phase = WordBattlePhase.Result; LastFeedback = "A Resident shared a new reply."; break; }
                    var challenge = Current;
                    _transcript.Add(string.IsNullOrEmpty(command.ReplyId) ? "I have no answer." : WordBattleCatalog.Find(command.ReplyId).Text);
                    bool match = challenge.Id == command.ReplyId;
                    if (match) StewardScore++; else LeaderScore++;
                    LastFeedback = match ? "Well turned. " + challenge.Explanation : "That answer misses the argument. " + challenge.Explanation;
                    _transcript.Add(LastFeedback);
                    if (StewardScore >= 2 || LeaderScore >= 2)
                    {
                        Won = StewardScore >= 2;
                        if (!Practice)
                        {
                            if (Won && _victories.Add(Key(FactionId, Tier)))
                            {
                                float before = Pressure(factions); factions.AwardWordBattleRespect(FactionId, Tier); ActualTensionReduction = before - Pressure(factions);
                            }
                            _retryDay = Won ? -1 : day;
                            Learn(FacedUnknown());
                        }
                        Phase = WordBattlePhase.Result; _sequence = _sequence == int.MaxValue ? 0 : _sequence + 1;
                        _transcript.Add(Practice ? (Won ? "Practice won. No new respect or replies awarded." : "Practice complete. No new respect or replies awarded.") : Won ? "The Guild yields this round. Respect is earned." : "The Guild takes this round. Your repertoire can still grow.");
                        _transcript.Add(LearnedReplyId == null ? (RepertoireComplete ? "Repertoire complete." : "Practice complete.") : "New reply learned: " + WordBattleCatalog.Find(LearnedReplyId).Text);
                    }
                    else { _exchange++; _transcript.Add(ChallengeText); }
                    break;
                case WordBattleAction.Suspend: Phase = WordBattlePhase.Suspended; break;
                case WordBattleAction.Resume: Phase = WordBattlePhase.Active; break;
                case WordBattleAction.Close: Phase = WordBattlePhase.Idle; break;
            }
            Revision++;
        }
        private float Pressure(FactionState factions) { foreach (var f in factions.Factions) if (f.Id == FactionId) return f.EffectivePressure; return 0; }
        private void ResetEncounter() { StewardScore = 0; LeaderScore = 0; _exchange = 0; Won = false; LearnedReplyId = null; ActualTensionReduction = 0; LastFeedback = ""; _transcript.Clear(); }
        private string FacedUnknown() { for (int i = 0; i <= _exchange; i++) { var id = WordBattleCatalog.Replies[(Tier - 1) * 4 + (int)(((long)_sequence + i) % 4)].Id; if (!_learned.Contains(id)) return id; } return null; }
        private void Learn(string preferred)
        {
            if (preferred == null && Tier > 0) foreach (var r in WordBattleCatalog.Replies) if (r.Tier == Tier && !_learned.Contains(r.Id)) { preferred = r.Id; break; }
            if (preferred == null && Tier > 0) foreach (var r in WordBattleCatalog.Replies) if (r.Tier > Tier && !_learned.Contains(r.Id)) { preferred = r.Id; break; }
            if (preferred == null) foreach (var r in WordBattleCatalog.Replies) if (!_learned.Contains(r.Id)) { preferred = r.Id; break; }
            if (preferred != null && _learned.Add(preferred)) LearnedReplyId = preferred;
        }
        public WordBattleSaveData ToSaveData() => new WordBattleSaveData { contentVersion = WordBattleCatalog.Version, revision = Revision, phase = (int)Phase, speakerId = SpeakerId, tier = Tier, stewardScore = StewardScore, leaderScore = LeaderScore, exchange = _exchange, sequence = _sequence, factionId = FactionId, learnedReplyId = LearnedReplyId, lastFeedback = LastFeedback, practice = Practice, won = Won, actualTensionReduction = ActualTensionReduction, retryDay = _retryDay, lastLessonDay = _lastLessonDay, learnedIds = Sorted(_learned), victoryIds = Sorted(_victories), transcript = _transcript.ToArray() };
        private static string[] Sorted(HashSet<string> set) { var result = new List<string>(set); result.Sort(StringComparer.Ordinal); return result.ToArray(); }
        public static WordBattleState FromSaveData(WordBattleSaveData data, Func<int, bool> speakerExists)
        {
            var state = new WordBattleState(); if (data == null) return state;
            if (data.learnedIds != null) foreach (var id in data.learnedIds) if (WordBattleCatalog.Find(id) != null) state._learned.Add(id);
            if (data.victoryIds != null) foreach (var id in data.victoryIds) for (int t = 1; t <= 3; t++) if (id == Key(FactionIds.MerchantGuild, t)) state._victories.Add(id);
            state.Revision = Math.Max(0, data.revision); state._sequence = Math.Max(0, data.sequence); state._retryDay = data.retryDay; state._lastLessonDay = data.lastLessonDay;
            state.LastFeedback = data.lastFeedback ?? "";
            if (data.transcript != null) for (int i = 0; i < Math.Min(32, data.transcript.Length); i++) if (!string.IsNullOrEmpty(data.transcript[i])) state._transcript.Add(data.transcript[i]);
            bool activeValid = data.contentVersion == WordBattleCatalog.Version && speakerExists(data.speakerId) && data.tier >= 1 && data.tier <= 3 && data.factionId == FactionIds.MerchantGuild && data.exchange >= 0 && data.exchange <= 2 && data.stewardScore >= 0 && data.leaderScore >= 0 && data.stewardScore <= 2 && data.leaderScore <= 2 && data.stewardScore + data.leaderScore == data.exchange && (!data.practice || state._victories.Contains(Key(data.factionId, data.tier))) && data.tier <= state.NextTier(data.factionId);
            state.SpeakerId = data.speakerId; state.FactionId = data.factionId; state.Tier = data.tier; state.StewardScore = data.stewardScore; state.LeaderScore = data.leaderScore; state._exchange = data.exchange; state.Practice = data.phase == (int)WordBattlePhase.Active || data.phase == (int)WordBattlePhase.Suspended ? state._victories.Contains(Key(data.factionId, data.tier)) : data.practice; state.Won = data.won; state.LearnedReplyId = WordBattleCatalog.Find(data.learnedReplyId)?.Id; state.ActualTensionReduction = Math.Max(0, data.actualTensionReduction);
            if ((data.phase == (int)WordBattlePhase.Active || data.phase == (int)WordBattlePhase.Suspended) && activeValid && data.stewardScore < 2 && data.leaderScore < 2) state.Phase = WordBattlePhase.Suspended;
            else if (data.phase == (int)WordBattlePhase.Result) state.Phase = WordBattlePhase.Result;
            else if (data.phase == (int)WordBattlePhase.ResidentChat && data.contentVersion == WordBattleCatalog.Version && speakerExists(data.speakerId)) { state.Phase = WordBattlePhase.ResidentChat; state.Practice = true; }
            else { state.Phase = WordBattlePhase.Idle; state.LastFeedback = data.phase == 0 ? state.LastFeedback : "Conversation ended because its content or spokesperson is unavailable. Progress retained."; }
            return state;
        }
    }
}
