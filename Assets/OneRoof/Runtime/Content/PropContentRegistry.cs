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
                    new[] { "residential", "lobby" }),

                new PropContentRecord(
                    "prop.commercial.shelf.v1",
                    "Retail Shelf Double",
                    "Props/prop_retail_shelf_double",
                    2, 1, "medium", "solid",
                    new[] { "browse-front" },
                    new[] { "commercial:retail" }),

                new PropContentRecord(
                    "prop.commercial.checkout.v1",
                    "Retail Checkout Counter",
                    "Props/prop_retail_checkout_counter",
                    2, 1, "medium", "solid",
                    new[] { "serve-front", "staff-back" },
                    new[] { "commercial:retail" }),

                new PropContentRecord(
                    "prop.commercial.rack.v1",
                    "Retail Garment Rack",
                    "Props/prop_retail_rack_round",
                    1, 1, "tall", "solid",
                    new[] { "browse-front" },
                    new[] { "commercial:retail" }),

                new PropContentRecord(
                    "prop.service.exambed.v1",
                    "Clinic Exam Bed",
                    "Props/prop_clinic_exam_bed",
                    2, 1, "medium", "solid",
                    new[] { "sleep-left" },
                    new[] { "service:clinic" }),

                new PropContentRecord(
                    "prop.service.pharmacabinet.v1",
                    "Clinic Supply Cabinet",
                    "Props/prop_clinic_cabinet_cross",
                    1, 1, "tall", "solid",
                    new[] { "file-front" },
                    new[] { "service:clinic" }),

                new PropContentRecord(
                    "prop.service.screen.v1",
                    "Clinic Privacy Screen",
                    "Props/prop_clinic_screen_privacy",
                    1, 1, "tall", "solid",
                    Array.Empty<string>(),
                    new[] { "service:clinic" }),

                new PropContentRecord(
                    "prop.service.workbench.v1",
                    "Maintenance Workbench",
                    "Props/prop_maint_workbench",
                    3, 1, "medium", "solid",
                    new[] { "work-front" },
                    new[] { "service:maintenance_workshop" }),

                new PropContentRecord(
                    "prop.service.toolcabinet.v1",
                    "Maintenance Tool Cabinet",
                    "Props/prop_maint_tool_cabinet",
                    1, 1, "tall", "solid",
                    new[] { "file-front" },
                    new[] { "service:maintenance_workshop" }),

                new PropContentRecord(
                    "prop.service.partsshelf.v1",
                    "Maintenance Parts Shelf",
                    "Props/prop_maint_parts_shelf",
                    2, 1, "medium", "solid",
                    new[] { "store-front" },
                    new[] { "service:maintenance_workshop" }),

                new PropContentRecord(
                    "prop.service.securitydesk.v1",
                    "Security Monitor Desk",
                    "Props/prop_security_monitor_desk",
                    2, 1, "medium", "solid",
                    new[] { "work-front" },
                    new[] { "service:security_station" }),

                new PropContentRecord(
                    "prop.service.lockerrow.v1",
                    "Security Locker Row",
                    "Props/prop_security_locker_row",
                    2, 1, "tall", "solid",
                    new[] { "store-front" },
                    new[] { "service:security_station" }),

                new PropContentRecord(
                    "prop.utility.substation.v1",
                    "Electrical Substation Cabinet",
                    "Props/prop_utility_substation_cabinet",
                    2, 2, "tall", "solid",
                    new[] { "work-front" },
                    new[] { "utility:electrical_substation", "utility:floor_transformer", "utility:electrical_riser" }),

                new PropContentRecord(
                    "prop.utility.pump.v1",
                    "Water Pump Skid",
                    "Props/prop_utility_pump_skid",
                    2, 1, "medium", "solid",
                    new[] { "work-front" },
                    new[] { "utility:water_pump", "utility:water_booster", "utility:water_riser" }),

                new PropContentRecord(
                    "prop.utility.pipechase.v1",
                    "Utility Pipe Chase",
                    "Props/prop_utility_pipe_chase",
                    1, 1, "tall", "solid",
                    Array.Empty<string>(),
                    new[] { "utility:water_riser", "utility:electrical_riser", "utility:waste_chute" }),

                new PropContentRecord(
                    "prop.utility.wastehopper.v1",
                    "Waste Collection Hopper",
                    "Props/prop_utility_waste_hopper",
                    2, 1, "tall", "solid",
                    new[] { "work-front" },
                    new[] { "utility:waste_collection", "utility:waste_chute" })
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
