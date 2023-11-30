using StardewModdingAPI;

namespace ScheduleViewer
{
    internal class ModConfig
    {
        public SButton ShowSchedulesKey { get; set; } = SButton.V;
        public string SortOrder { get; set; } = ModEntry.SortOrderOptions[0];
        public bool OnlyShowMetNPCs { get; set; } = false;
        public bool OnlyShowSocializableNPCs { get; set; } = true;
    }
}
