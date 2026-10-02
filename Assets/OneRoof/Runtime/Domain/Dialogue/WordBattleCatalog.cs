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
            new WordBattleReply("wit:share", 1, "burden", "Then lead by sharing costs today.", "We set the course; the rest must pay.", "For common good, the weak must pay.", "Leadership requires sharing the burden."),
            new WordBattleReply("wit:rank", 1, "authority", "A title cannot win this fight.", "Our rank alone proves we are right.", "Our Guild has rank; respect its might.", "Rank is no substitute for a sound argument."),
            new WordBattleReply("wit:pledge", 1, "promises", "A pledge alone won't keep the light.", "Just trust our pledge: the future's bright.", "Our future profits shine so bright.", "Promises need practical delivery."),
            new WordBattleReply("wit:choice", 1, "necessity", "Then say who bears it, who will gain.", "There is no choice; accept the strain.", "These costs are needed; don't complain.", "Necessity does not settle who bears the costs."),
            new WordBattleReply("wit:price", 2, "value", "A price is not what people are worth.", "The market sets your place on earth.", "Your purse decides your place on earth.", "Market value cannot measure human dignity."),
            new WordBattleReply("wit:profit", 2, "prosperity", "Who gets the feast, who pays the price?", "Our profits prove this policy is nice.", "Our growing margins should suffice.", "Private profit does not prove shared prosperity."),
            new WordBattleReply("wit:voice", 2, "representation", "Then let the quiet have their say.", "No one objects; we've won the day.", "Their silence tells us we're okay.", "Silence does not establish consent."),
            new WordBattleReply("wit:risk", 2, "risk", "But public hands must catch your fall.", "We take the risk, we earn it all.", "Our daring built this trading hall.", "Private rewards must account for public risk."),
            new WordBattleReply("wit:custom", 3, "tradition", "Old habits cannot make wrong right.", "We've always done it; end this fight.", "Our ancient charter grants this right.", "Tradition alone does not justify a policy."),
            new WordBattleReply("wit:unity", 3, "unity", "A shared roof leaves room to disagree.", "One Tower means you side with me.", "Divide our Guild and none are free.", "Unity does not require unquestioning agreement."),
            new WordBattleReply("wit:mercy", 3, "charity", "Fair terms should never beg for grace.", "Our mercy keeps you in your place.", "Be grateful for our kindly face.", "Basic fairness is not a personal favor."),
            new WordBattleReply("wit:order", 3, "order", "An unjust peace will crack once more.", "Keep quiet; order matters more.", "No grievances upon this floor.", "Suppressing complaints leaves their causes intact.") });
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
