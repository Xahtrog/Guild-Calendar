using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace GuildCalendar
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        public enum GuildIcon { Guardian, Warrior, Engineer, Ranger, Thief, Elementalist, Mesmer, Necromancer, Revenant }

        private class GuildSetting
        {
            public SettingEntry<string> Name;
            public SettingEntry<string> Link;
            public SettingEntry<KeyBinding> Hotkey;
            public SettingEntry<bool> IsVisible;
            public SettingEntry<GuildIcon> IconPicker;
            public Tab Tab;
        }

        private List<GuildSetting> _guildSettings = new List<GuildSetting>();
        private SettingEntry<bool> _showEditorTools;
        private CalendarService _service;
        private TabbedWindow2 _window;
        private CornerIcon _icon;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            // UPDATED TOOLTIP HERE
            _showEditorTools = settings.DefineSetting(
                "ShowEditorTools_V30",
                false,
                () => "Enable Officer Tools",
                () => "WARNING: You cannot add events to the Guild Calendar unless authorized by the calendar owner.\nAny events created here will save to your PERSONAL local calendar only."
            );

            for (int i = 1; i <= 6; i++)
            {
                int id = i;
                var guild = new GuildSetting();
                guild.Name = settings.DefineSetting($"Guild{id}_Name_V30", $"Guild {id}", () => $"[Guild {id}] Name");
                guild.IsVisible = settings.DefineSetting($"Guild{id}_Visible_V30", true, () => $"[Guild {id}] Show in Window");
                var defaultIcon = (GuildIcon)((i - 1) % Enum.GetValues(typeof(GuildIcon)).Length);
                guild.IconPicker = settings.DefineSetting($"Guild{id}_IconSelect_V30", defaultIcon, () => $"[Guild {id}] Icon");
                guild.Link = settings.DefineSetting($"Guild{id}_Link_V30", "", () => $"[Guild {id}] iCal Link");
                guild.Hotkey = settings.DefineSetting($"Guild{id}_Key_V30", new KeyBinding(Keys.None), () => $"[Guild {id}] Hotkey");
                _guildSettings.Add(guild);
            }
        }

        protected override void Initialize() { _service = new CalendarService(); }

        protected override async Task LoadAsync()
        {
            var bgTexture = GameService.Content.DatAssetCache.GetTextureFromAssetId(155985);

            _window = new TabbedWindow2(
                bgTexture,
                new Rectangle(35, 36, 873, 691),
                new Rectangle(100, 50, 840, 630))
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "Guild Calendar",
                Emblem = AsyncTexture2D.FromAssetId(156022),
                Id = "GuildCalWindow",
                SavesPosition = true,
                SavesSize = false,
                CanResize = false,
                Size = new Point(960, 700)
            };

            foreach (var guildSetting in _guildSettings)
            {
                var iconType = guildSetting.IconPicker?.Value ?? GuildIcon.Guardian;
                guildSetting.Tab = new Tab(GetIconTexture(iconType), () => new SmartCalendarTab(guildSetting.Link, _service, () => _showEditorTools.Value), guildSetting.Name.Value);

                if (guildSetting.IsVisible.Value) _window.Tabs.Add(guildSetting.Tab);

                guildSetting.IsVisible.SettingChanged += (s, e) => { if (e.NewValue) _window.Tabs.Add(guildSetting.Tab); else _window.Tabs.Remove(guildSetting.Tab); };
                guildSetting.Name.SettingChanged += (s, e) => guildSetting.Tab.Name = e.NewValue;
                guildSetting.IconPicker.SettingChanged += (s, e) => guildSetting.Tab.Icon = GetIconTexture(e.NewValue);

                if (guildSetting.Hotkey != null)
                {
                    guildSetting.Hotkey.Value.Enabled = true;
                    guildSetting.Hotkey.Value.Activated += delegate { if (guildSetting.IsVisible.Value) { _window.ToggleWindow(); if (_window.Visible) _window.SelectedTab = guildSetting.Tab; } };
                }
            }

            _icon = new CornerIcon() { Icon = AsyncTexture2D.FromAssetId(156022), BasicTooltipText = "Guild Calendar", Priority = 5 };
            _icon.Click += delegate { _window.ToggleWindow(); };
        }

        private AsyncTexture2D GetIconTexture(GuildIcon iconSelection)
        {
            int assetId = 156633;
            switch (iconSelection)
            {
                case GuildIcon.Warrior: assetId = 156642; break;
                case GuildIcon.Engineer: assetId = 156631; break;
                case GuildIcon.Ranger: assetId = 156639; break;
                case GuildIcon.Thief: assetId = 103581; break;
                case GuildIcon.Elementalist: assetId = 156629; break;
                case GuildIcon.Mesmer: assetId = 156635; break;
                case GuildIcon.Necromancer: assetId = 156637; break;
                case GuildIcon.Revenant: assetId = 965717; break;
                default: assetId = 156633; break;
            }
            return AsyncTexture2D.FromAssetId(assetId);
        }

        protected override void Unload() { foreach (var g in _guildSettings) if (g.Hotkey != null) g.Hotkey.Value.Enabled = false; _window?.Dispose(); _icon?.Dispose(); }

        private class SmartCalendarTab : CalendarView
        {
            private SettingEntry<string> _linkSetting;
            private CalendarService _service;
            public SmartCalendarTab(SettingEntry<string> linkSetting, CalendarService service, Func<bool> canEdit) : base(canEdit) { _linkSetting = linkSetting; _service = service; _linkSetting.SettingChanged += (s, e) => _ = LoadData(); _ = LoadData(); }
            private async Task LoadData()
            {
                this.UpdateStatus("Checking...", Color.Yellow);
                if (string.IsNullOrWhiteSpace(_linkSetting.Value)) { this.UpdateStatus("No Link", Color.Red); return; }
                try { var events = await _service.FetchEvents(_linkSetting.Value); this.UpdateEvents(events); this.UpdateStatus(events.Count > 0 ? $"Loaded {events.Count}" : "No Events", events.Count > 0 ? Color.Green : Color.Orange); }
                catch (Exception ex) { this.UpdateStatus("Error", Color.Red); Logger.Warn(ex, "Load Failed"); }
            }
        }
    }
}