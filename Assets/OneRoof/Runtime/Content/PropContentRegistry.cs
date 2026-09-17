using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneRoof.Content
{
    /// <summary>
    /// Canonical registry of all validated and released environment props adhering to Docs/05_ASSET_PIPELINE.md.
    /// </summary>
    public static class PropContentRegistry
    {
        private static readonly Dictionary<string, PropContentRecord> RecordsById;
        private static readonly ReadOnlyCollection<PropContentRecord> AllRecordsList;

        static PropContentRegistry()
        {
            var records = new List<PropContentRecord>
            {
                new PropContentRecord(
                    "prop.furniture.sofa.v1",
                    "2-Seat Sofa",
                    "Props/prop_residential_sofa_2seat",
                    2, 1, "low", "solid",
                    new[] { "seat-left", "seat-right" },
                    new[] { "residential" }),

                new PropContentRecord(
                    "prop.furniture.bed.v1",
                    "Double Bed",
                    "Props/prop_residential_double_bed",
                    2, 2, "medium", "solid",
                    new[] { "sleep-left", "sleep-right" },
                    new[] { "residential" }),

                new PropContentRecord(
                    "prop.furniture.bookcase.v1",
                    "Tall Bookcase",
                    "Props/prop_residential_bookcase_tall",
                    1, 1, "tall", "solid",
                    new[] { "browse-front" },
                    new[] { "residential", "office" }),

                new PropContentRecord(
                    "prop.decor.planter.v1",
                    "Monstera Planter",
                    "Props/prop_interior_monstera_planter",
                    1, 1, "tall", "solid",
                    Array.Empty<string>(),
                    new[] { "residential", "office", "lobby", "retail" }),

                new PropContentRecord(
                    "prop.workplace.desk.v1",
                    "Office Desk with Dual Monitors",
                    "Props/prop_office_desk_monitor",
                    2, 1, "medium", "solid",
                    new[] { "work-front" },
                    new[] { "office" }),

                new PropContentRecord(
                    "prop.workplace.chair.v1",
                    "Wheeled Office Chair",
                    "Props/prop_office_chair_wheeled",
                    1, 1, "medium", "solid",
                    new[] { "seat" },
                    new[] { "office" }),

                new PropContentRecord(
                    "prop.commercial.booth.v1",
                    "Diner Booth",
                    "Props/prop_diner_booth_2seat",
                    2, 1, "medium", "solid",
                    new[] { "seat-left", "seat-right" },
                    new[] { "restaurant", "diner" }),

                new PropContentRecord(
                    "prop.commercial.counter.v1",
                    "Diner Service Counter",
                    "Props/prop_diner_service_counter",
                    3, 1, "medium", "solid",
                    new[] { "serve-front", "staff-back" },
                    new[] { "restaurant", "diner", "retail" }),

                new PropContentRecord(
                    "prop.civic.reception.v1",
                    "Lobby Reception Desk",
                    "Props/prop_lobby_reception_desk",
                    3, 1, "medium", "solid",
                    new[] { "visitor-front", "staff-back" },
                    new[] { "lobby", "clinic" }),

                new PropContentRecord(
                    "prop.domestic.kitchenette.v1",
                    "Compact Kitchenette",
                    "Props/prop_compact_kitchenette",
                    3, 1, "tall", "solid",
                    new[] { "cook-front" },
                    new[] { "residential", "restaurant" }),

                new PropContentRecord(
                    "prop.workplace.filing.v1",
                    "Office Filing Cabinet",
                    "Props/prop_office_filing_cabinet",
                    1, 1, "medium", "solid",
                    new[] { "file-front" },
                    new[] { "office", "clinic", "maintenance" }),

                new PropContentRecord(
                    "prop.civic.coatrack.v1",
                    "Lobby Coat Rack",
                    "Props/prop_lobby_coat_rack",
                    1, 1, "tall", "solid",
                    new[] { "store-front" },
                    new[] { "lobby", "office", "residential" }),

                new PropContentRecord(
                    "prop.furniture.table.v1",
                    "Residential Coffee Table",
                    "Props/prop_residential_coffee_table",
                    2, 1, "low", "solid",
                    Array.Empty<string>(),
                    new[] { "residential", "lobby" })
            };

            RecordsById = new Dictionary<string, PropContentRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in records)
            {
                RecordsById[r.ContentId] = r;
            }
            AllRecordsList = new ReadOnlyCollection<PropContentRecord>(records);
        }

        public static int Count => AllRecordsList.Count;

        public static IReadOnlyList<PropContentRecord> GetAll() => AllRecordsList;

        public static PropContentRecord GetById(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return null;
            RecordsById.TryGetValue(contentId, out var record);
            return record;
        }

        public static IReadOnlyList<PropContentRecord> GetByTheme(string theme)
        {
            var result = new List<PropContentRecord>();
            for (var i = 0; i < AllRecordsList.Count; i++)
            {
                if (AllRecordsList[i].SupportsTheme(theme))
                {
                    result.Add(AllRecordsList[i]);
                }
            }
            return result;
        }
    }
}
