using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleViewer
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        public static ModConfig Config;
        public static IMonitor Console;
        public static IModHelper ModHelper;
        public static Dictionary<string, string> CustomLocationNames = new();
        public static readonly string[] SortOrderOptions = new string[4];
        /// <summary>Current player's mods don't match host's. Null if host doesn't have SMAPI.</summary>
        public static bool? HasMismatchedMods = false;
        /// <summary>Current player's GameVersion doesn't match host's.</summary>
        public static bool HasMismatchedGameVersion = false;
        /// <summary>Host player has this mod. Null if host has a different version.</summary>
        public static bool? HostHasThisMod = true;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Console = this.Monitor;
            ModHelper = helper;
            for (int i = 0; i < SortOrderOptions.Length; i++)
            {
                SortOrderOptions[i] = this.Helper.Translation.Get($"config.option.sort_options.option_{i}");
            }
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            if (helper.ModRegistry.IsLoaded("Bouhm.NPCMapLocations"))
            {
                helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            }
        }


        /*********
        ** Private methods
        *********/
        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // check for mismatched GameVersion and Mods between host and current player
            bool foundHost = !this.Helper.Multiplayer.GetConnectedPlayers().Any();
            foreach (IMultiplayerPeer peer in this.Helper.Multiplayer.GetConnectedPlayers())
            {
                if (peer.IsHost)
                {
                    foundHost = true;
                    if (!peer.HasSmapi)
                    {
                        ModEntry.HasMismatchedMods = null;
                    } else
                    {
                        string modID = this.Helper.ModRegistry.ModID;
                        ModEntry.HasMismatchedGameVersion = peer.GameVersion.ToString() != Game1.version;
                        ModEntry.HasMismatchedMods = peer.Mods.Any(mod => !this.Helper.ModRegistry.IsLoaded(mod.ID));
                        IMultiplayerPeerMod hostScheduleViewer = peer.GetMod(modID);
                        if (hostScheduleViewer != null)
                        {
                            ModEntry.HostHasThisMod = hostScheduleViewer.Version.Equals(this.Helper.ModRegistry.Get(modID).Manifest.Version) ? true : null;
                        } else
                        {
                            ModEntry.HostHasThisMod = false;
                        }
                    }
                    break;
                }
            }
            // try loading in display names from NPC Map Locations
            try
            {
                var locationSettings = this.Helper.GameContent.Load<Dictionary<string, JObject>>("Mods/Bouhm.NPCMapLocations/Locations");
                CustomLocationNames = locationSettings.Where(location => location.Value.SelectToken("MapTooltip.PrimaryText") != null).ToDictionary(location => location.Key, location => location.Value.SelectToken("MapTooltip.PrimaryText").Value<string>());
            }
            catch (Exception) { }
        }

        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            // register mod
            configMenu.Register(
                ModManifest,
                () => Config = new ModConfig(),
                () => Helper.WriteConfig(Config)
            );

            // add some config options
            configMenu.AddKeybind(
                ModManifest,
                name: () => this.Helper.Translation.Get("config.option.show_schedule_key.name"),
                getValue: () => Config.ShowSchedulesKey,
                setValue: value => Config.ShowSchedulesKey = value
            );
            configMenu.AddTextOption(
                ModManifest,
                name: () => this.Helper.Translation.Get("config.option.sort_options.name"),
                tooltip: () => this.Helper.Translation.Get("config.option.sort_options.description"),
                getValue: () => Config.SortOrder,
                setValue: value => Config.SortOrder = value,
                allowedValues: SortOrderOptions
            );
            configMenu.AddBoolOption(
                ModManifest,
                name: () => this.Helper.Translation.Get("config.option.only_show_met_npcs.name"),
                tooltip: () => this.Helper.Translation.Get("config.option.only_show_met_npcs.description"),
                getValue: () => Config.OnlyShowMetNPCs,
                setValue: value => Config.OnlyShowMetNPCs = value
            );
            configMenu.AddBoolOption(
                ModManifest,
                name: () => this.Helper.Translation.Get("config.option.only_show_socializable_npcs.name"),
                tooltip: () => this.Helper.Translation.Get("config.option.only_show_socializable_npcs.description"),
                getValue: () => Config.OnlyShowSocializableNPCs,
                setValue: value => Config.OnlyShowSocializableNPCs = value
            );
        }

        /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                return;
            }
            try
            {
                // open menu
                if (e.Pressed.Contains(Config.ShowSchedulesKey))
                {
                    // open if no conflict
                    if (Game1.activeClickableMenu == null)
                    {
                        if (Context.IsPlayerFree && !Game1.player.UsingTool && !Game1.player.isEating)
                        {
                            Game1.activeClickableMenu = new SchedulesPage();
                        }  
                    }
                    // open from GameMenu if it's safe to close the GameMenu
                    else if (Game1.activeClickableMenu is GameMenu)
                    {
                        if (Game1.activeClickableMenu.readyToClose())
                        {
                            Game1.activeClickableMenu = new SchedulesPage();
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.Log("Error handling key input.", LogLevel.Error);
            }
        }
    }
}