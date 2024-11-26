using StardewModdingAPI;
using StardewModdingAPI.Utilities;

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

        public KeybindList ShowSchedulesKey { get; set; } = new KeybindList(SButton.V);
        public bool UseAddress { get; set; } = true;
        public bool DisableHover { get; set; } = false;
        public bool UseLargerFontForScheduleDetails { get; set; } = false;
        public SortType NPCSortOrder { get; set; } = SortType.AlphabeticalAscending;
        public bool OnlyShowMetNPCs { get; set; } = false;
        public bool OnlyShowSocializableNPCs { get; set; } = true;
        public string[] IgnoredNPCs { get; set; } = new string[3] { "Dwarf", "Krobus", "Wizard" };
    }
}
