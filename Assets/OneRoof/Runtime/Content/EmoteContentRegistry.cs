using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>
    /// Canonical content registry for approved resident emote balloons and reaction icons.
    /// Provides immutable authored records matching Art/SourceArt/Proposed/resident-emotes-v1.spec.json.
    /// </summary>
    public static class EmoteContentRegistry
    {
        private static readonly Dictionary<NpcEmoteKind, EmoteContentRecord> ByKind;
        private static readonly Dictionary<string, EmoteContentRecord> ByContentId;
        private static readonly ReadOnlyCollection<EmoteContentRecord> All;

        public static int Count => All.Count;
        public static IReadOnlyList<EmoteContentRecord> AllRecords => All;

        static EmoteContentRegistry()
        {
            var records = new List<EmoteContentRecord>
            {
                new EmoteContentRecord(NpcEmoteKind.Target, "emote.bubble.target.v1", "emote_target_focus", "Target Focus", "Emotes/emote_target_focus", "UXSelection", 0, 1, new[] { 0, 1, 2 }),
                new EmoteContentRecord(NpcEmoteKind.FrostSpark, "emote.bubble.frost.v1", "emote_frost_spark", "Cold / Frost Burst", "Emotes/emote_frost_spark", "Environment", 0, 1, new[] { 3, 4, 5 }),
                new EmoteContentRecord(NpcEmoteKind.MusicNote, "emote.bubble.music.v1", "emote_music_note", "Music Note", "Emotes/emote_music_note", "Routine", 0, 1, new[] { 6, 7, 8 }),
                new EmoteContentRecord(NpcEmoteKind.SwirlConfused, "emote.bubble.confused.v1", "emote_swirl_confused", "Confused Scribble", "Emotes/emote_swirl_confused", "Transit", 0, 1, new[] { 9, 10, 11 }),

                new EmoteContentRecord(NpcEmoteKind.ThumbsUp, "emote.bubble.thumbsup.v1", "emote_thumbs_up", "Thumbs Up", "Emotes/emote_thumbs_up", "Satisfaction", 2, 3, new[] { 0, 1, 2 }),
                new EmoteContentRecord(NpcEmoteKind.ThumbsDown, "emote.bubble.thumbsdown.v1", "emote_thumbs_down", "Thumbs Down", "Emotes/emote_thumbs_down", "Satisfaction", 2, 3, new[] { 3, 4, 5 }),
                new EmoteContentRecord(NpcEmoteKind.Sweat, "emote.bubble.sweat.v1", "emote_sweat_drops", "Sweat Drops", "Emotes/emote_sweat_drops", "Transit", 2, 3, new[] { 6, 7, 8 }),
                new EmoteContentRecord(NpcEmoteKind.Lightbulb, "emote.bubble.lightbulb.v1", "emote_lightbulb_idea", "Lightbulb Idea", "Emotes/emote_lightbulb_idea", "Routine", 2, 3, new[] { 9, 10, 11 }),

                new EmoteContentRecord(NpcEmoteKind.ArrowDown, "emote.bubble.arrowdown.v1", "emote_arrow_down_red", "Arrow Down Red", "Emotes/emote_arrow_down_red", "Economic", 4, 5, new[] { 0, 1, 2 }),
                new EmoteContentRecord(NpcEmoteKind.ArrowUp, "emote.bubble.arrowup.v1", "emote_arrow_up_cyan", "Arrow Up Cyan", "Emotes/emote_arrow_up_cyan", "Economic", 4, 5, new[] { 3, 4, 5 }),
                new EmoteContentRecord(NpcEmoteKind.Sleeping, "emote.bubble.sleep.v1", "emote_sleep_zzz", "Sleep Zzz", "Emotes/emote_sleep_zzz", "Routine", 4, 5, new[] { 6, 7, 8 }),
                new EmoteContentRecord(NpcEmoteKind.Skull, "emote.bubble.skull.v1", "emote_skull_danger", "Danger Skull", "Emotes/emote_skull_danger", "Crisis", 4, 5, new[] { 9, 10, 11 }),

                new EmoteContentRecord(NpcEmoteKind.Heart, "emote.bubble.heart.v1", "emote_heart_love", "Heart Affection", "Emotes/emote_heart_love", "Social", 6, 7, new[] { 0, 1, 2 }),
                new EmoteContentRecord(NpcEmoteKind.BrokenHeart, "emote.bubble.brokenheart.v1", "emote_broken_heart", "Broken Heart", "Emotes/emote_broken_heart", "Social", 6, 7, new[] { 3, 4, 5 }),
                new EmoteContentRecord(NpcEmoteKind.Blush, "emote.bubble.blush.v1", "emote_blush_stripes", "Blush Stripes", "Emotes/emote_blush_stripes", "Social", 6, 7, new[] { 6, 7, 8 }),
                new EmoteContentRecord(NpcEmoteKind.Anger, "emote.bubble.anger.v1", "emote_anger_vein", "Anger Vein", "Emotes/emote_anger_vein", "Transit", 6, 7, new[] { 9, 10, 11 }),

                new EmoteContentRecord(NpcEmoteKind.Exclamation, "emote.bubble.exclamation.v1", "emote_exclamation_alert", "Exclamation Alert", "Emotes/emote_exclamation_alert", "Alert", 8, 9, new[] { 0, 1, 2 }),
                new EmoteContentRecord(NpcEmoteKind.Question, "emote.bubble.question.v1", "emote_question_mark", "Question Mark", "Emotes/emote_question_mark", "Transit", 8, 9, new[] { 3, 4, 5 }),
                new EmoteContentRecord(NpcEmoteKind.ImpactCrest, "emote.bubble.impact.v1", "emote_impact_crest", "Impact Crest", "Emotes/emote_impact_crest", "Social", 8, 9, new[] { 6, 7, 8 }),
                new EmoteContentRecord(NpcEmoteKind.Steam, "emote.bubble.steam.v1", "emote_steam_waves", "Steam Waves", "Emotes/emote_steam_waves", "Environment", 8, 9, new[] { 9, 10, 11 }),

                new EmoteContentRecord(NpcEmoteKind.Interrobang, "emote.bubble.interrobang.v1", "emote_interrobang", "Interrobang", "Emotes/emote_interrobang", "Alert", 10, 11, new[] { 0, 1, 2 }),
                new EmoteContentRecord(NpcEmoteKind.Ellipsis, "emote.bubble.ellipsis.v1", "emote_ellipsis_wait", "Ellipsis Wait", "Emotes/emote_ellipsis_wait", "Transit", 10, 11, new[] { 3, 4, 5 }),
                new EmoteContentRecord(NpcEmoteKind.Sun, "emote.bubble.sun.v1", "emote_sun_daytime", "Sun Daytime", "Emotes/emote_sun_daytime", "Time", 10, 11, new[] { 6, 7, 8 }),
                new EmoteContentRecord(NpcEmoteKind.Moon, "emote.bubble.moon.v1", "emote_moon_night", "Moon Night", "Emotes/emote_moon_night", "Time", 10, 11, new[] { 9, 10, 11 })
            };

            ByKind = new Dictionary<NpcEmoteKind, EmoteContentRecord>();
            ByContentId = new Dictionary<string, EmoteContentRecord>();

            foreach (var r in records)
            {
                ByKind[r.EmoteKind] = r;
                ByContentId[r.ContentId] = r;
            }

            All = new ReadOnlyCollection<EmoteContentRecord>(records);
        }

        public static EmoteContentRecord GetByKind(NpcEmoteKind kind)
        {
            return ByKind.TryGetValue(kind, out var record) ? record : null;
        }

        public static EmoteContentRecord GetById(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return null;
            return ByContentId.TryGetValue(contentId, out var record) ? record : null;
        }
    }
}
