using StardewModdingAPI;

namespace ScheduleViewer
{
    internal class ModConfig
    {
        public enum SortType : ushort
        {
            AlphabeticalAscending = 0,
            AlphabeticalDescending = 1,
            HeartsAscending = 2,
            HeartsDescending = 3
        }

        public SButton ShowSchedulesKey { get; set; } = SButton.V;
        public bool DisableHover { get; set; } = false;
        public bool UseLargerFontForScheduleDetails { get; set; } = false;
        public SortType NPCSortOrder { get; set; } = SortType.AlphabeticalAscending;
        public bool OnlyShowMetNPCs { get; set; } = false;
        public bool OnlyShowSocializableNPCs { get; set; } = true;
    }
}
