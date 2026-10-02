using System;
using System.Collections.Generic;
namespace OneRoof.Domain.Dialogue
{
    public sealed class WordBattleReply
    {
        public WordBattleReply(string id, int tier, string familyId, string text, string challenge, string variant, string explanation)
        { Id = id; Tier = tier; FamilyId = familyId; Text = text; Challenge = challenge; Variant = variant; Explanation = explanation; }
        public string Id { get; }
        public int Tier { get; }
        public string FamilyId { get; }
        public string Text { get; }
        public string Challenge { get; }
        public string Variant { get; }
        public string Explanation { get; }
    }
    public static class WordBattleCatalog
    {
        public const string Version = "guild_wit_v1";
        public static IReadOnlyList<WordBattleReply> Replies { get; } = Array.AsReadOnly(new[] {
            new WordBattleReply("wit:share", 1, "burden", "Then why's your tailor on our pay?", "We all must tighten belts today.", "Each must contribute, come what may.", "Shared sacrifice should include the people demanding it."),
            new WordBattleReply("wit:rank", 1, "authority", "Does gold leaf teach a plaque to read?", "Our charter says we're fit to lead.", "Our titles show the skills you need.", "Authority can silence a question without answering it."),
            new WordBattleReply("wit:pledge", 1, "promises", "The brochure glows. Who pays for light?", "Our pledge will keep your future bright.", "Tomorrow's profits bring delight.", "A glowing forecast cannot power the Tower."),
            new WordBattleReply("wit:choice", 1, "necessity", "Your umbrella's dry. Explain.", "There is no cure without some pain.", "We all must weather this hard rain.", "A hardship called shared can still spare those imposing it."),
            new WordBattleReply("wit:price", 2, "value", "The lift works just as hard as you.", "Hard work has earned our splendid view.", "We earned our place; you could do too.", "Wealth and status do not prove effort or human worth."),
            new WordBattleReply("wit:profit", 2, "prosperity", "Your pie got big. Our slice got small.", "Our margins show we're serving all.", "Our growing profits help us all.", "A larger total does not mean a fairer share."),
            new WordBattleReply("wit:voice", 2, "representation", "You charged admission to have a say.", "Their silence means they like our way.", "No voices rose against us today.", "Silence means little when speaking has a price."),
            new WordBattleReply("wit:risk", 2, "risk", "You keep the gold. We catch the fall.", "We bear the risk; we've earned it all.", "Our daring built this trading hall.", "Private rewards can rest on risks passed to the Tower."),
            new WordBattleReply("wit:custom", 3, "tradition", "So has the damp. Shall we pay mould?", "This fee's been here since days of old.", "Our oldest dues are good as gold.", "A practice surviving for years does not make it worth keeping."),
            new WordBattleReply("wit:unity", 3, "unity", "Then why's agreement an entry fee?", "One roof means everyone agrees with me.", "We stand as one; dissent won't keep us free.", "Belonging under one roof should not require agreement with its leaders."),
            new WordBattleReply("wit:mercy", 3, "charity", "You raised the rent, then passed the bowl.", "Relieving hardship is our goal.", "Our generous gifts will make you whole.", "Charity deserves scrutiny when its benefactor created the hardship."),
            new WordBattleReply("wit:order", 3, "order", "You put complaints outside the lease.", "Our quiet halls show perfect peace.", "No one complains; disputes must cease.", "Banning complaints can hide hardship without resolving it.") });
        /// <summary>Validation seam for content compiled from future authoring assets.</summary>
        public static void Validate(IReadOnlyList<WordBattleReply> replies)
        {
            if (replies == null) throw new ArgumentNullException(nameof(replies));
            var ids = new HashSet<string>(); var families = new HashSet<string>(); var tiers = new int[3];
            foreach (var reply in replies)
            {
                if (reply == null || string.IsNullOrWhiteSpace(reply.Id) || !ids.Add(reply.Id) || string.IsNullOrWhiteSpace(reply.FamilyId) || !families.Add(reply.FamilyId)) throw new ArgumentException("Reply and family IDs must be unique and nonempty.", nameof(replies));
                if (reply.Tier < 1 || reply.Tier > 3 || string.IsNullOrWhiteSpace(reply.Text) || string.IsNullOrWhiteSpace(reply.Challenge) || string.IsNullOrWhiteSpace(reply.Variant) || string.IsNullOrWhiteSpace(reply.Explanation)) throw new ArgumentException("Each reply requires a valid tier and complete authored lines.", nameof(replies));
                tiers[reply.Tier - 1]++;
            }
            foreach (var count in tiers) if (count != 4) throw new ArgumentException("Each pilot tier requires four argument families.", nameof(replies));
        }
        public static WordBattleReply Find(string id) { foreach (var r in Replies) if (r.Id == id) return r; return null; }
    }
}
