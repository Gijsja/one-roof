using System;
using System.Collections.Generic;
using OneRoof.Domain.Dialogue;

namespace OneRoof.Application.Dialogue
{
    public sealed class LearnedReplyProjection
    {
        public string Id { get; }
        public string Text { get; }
        public int Tier { get; }
        internal LearnedReplyProjection(WordBattleReply reply) { Id = reply.Id; Text = reply.Text; Tier = reply.Tier; }
    }

    /// <summary>Immutable dialogue view. Matching families never escape to the view.</summary>
    public sealed class WordBattleProjection
    {
        public WordBattlePhase Phase { get; }
        public int Revision { get; }
        public string FactionId { get; }
        public int SpeakerId { get; }
        public int Tier { get; }
        public int StewardScore { get; }
        public int LeaderScore { get; }
        public string ChallengeText { get; }
        public IReadOnlyList<LearnedReplyProjection> Choices { get; }
        public IReadOnlyList<LearnedReplyProjection> LearnedReplies { get; }
        public IReadOnlyList<string> Transcript { get; }
        public string LastFeedback { get; }
        public string LearnedReplyId { get; }
        public bool Won { get; }
        public bool Practice { get; }
        public float ActualTensionReduction { get; }
        public int MasteredTiers { get; }
        public bool RepertoireComplete { get; }
        internal WordBattleProjection(WordBattleState state)
        {
            Phase = state.Phase; Revision = state.Revision; FactionId = state.FactionId; SpeakerId = state.SpeakerId;
            Tier = state.Tier; StewardScore = state.StewardScore; LeaderScore = state.LeaderScore;
            ChallengeText = state.ChallengeText; LastFeedback = state.LastFeedback; LearnedReplyId = state.LearnedReplyId;
            Won = state.Won; Practice = state.Practice; ActualTensionReduction = state.ActualTensionReduction;
            MasteredTiers = state.MasteredTiers; RepertoireComplete = state.RepertoireComplete;
            Choices = Copy(state.Choices); LearnedReplies = Copy(state.LearnedReplies);
            var transcript = new string[state.Transcript.Count];
            for (var i = 0; i < transcript.Length; i++) transcript[i] = state.Transcript[i];
            Transcript = Array.AsReadOnly(transcript);
        }
        private static IReadOnlyList<LearnedReplyProjection> Copy(IReadOnlyList<WordBattleReply> values)
        {
            var result = new LearnedReplyProjection[values.Count];
            for (var i = 0; i < result.Length; i++) result[i] = new LearnedReplyProjection(values[i]);
            return Array.AsReadOnly(result);
        }
    }
}
