﻿using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
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
        public static readonly string[] SortOrderOptions = new string[4] { "Alphabetical Ascending", "Alphabetical Descending", "Hearts Ascending", "Hearts Descending" };


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Console = this.Monitor;
            ModHelper = helper;
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }


        /*********
        ** Private methods
        *********/
        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            var locationSettings = this.Helper.GameContent.Load<Dictionary<string, JObject>>("Mods/Bouhm.NPCMapLocations/Locations");
            CustomLocationNames = locationSettings.Where(location => location.Value.SelectToken("MapTooltip.PrimaryText") != null).ToDictionary(location => location.Key, location => location.Value.SelectToken("MapTooltip.PrimaryText").Value<string>());
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
        }

        /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (e.Pressed.Contains(Config.ShowSchedulesKey))
            {
                Game1.activeClickableMenu = new SchedulePage();
            }
        }
    }
}