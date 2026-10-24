using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;

namespace MissionLauncher.Core
{
    public sealed class MissionMenu : Script
    {
        private readonly ObjectPool _pool = new ObjectPool();
        private NativeMenu _menu;
        private NativeMenu _storyMenu;
        private NativeMenu _strangersMenu;
        private Keys _menuKey = Keys.None;
        private bool _initialized;

        public MissionMenu()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                Launcher launcher = Launcher.Instance;
                if (launcher == null || !launcher.IsReady) return;
                _initialized = true;

                if (!launcher.Config.EnableMenu)
                {
                    Log.Info("Menu disabled (EnableMenu = false).");
                    Tick -= OnTick;
                    KeyDown -= OnKeyDown;
                    return;
                }
                if (!Enum.TryParse(launcher.Config.MenuKey, true, out _menuKey))
                {
                    Log.Warn($"Invalid MenuKey '{launcher.Config.MenuKey}', using F7.");
                    _menuKey = Keys.F7;
                }

                _menu = new NativeMenu("Missions", "Replay a mission");
                _storyMenu = new NativeMenu("Missions", "Story missions");
                _strangersMenu = new NativeMenu("Missions", "Strangers and Freaks");
                _pool.Add(_menu);
                _pool.Add(_storyMenu);
                _pool.Add(_strangersMenu);
                Log.Info($"Menu ready, key {_menuKey}.");
            }

            if (_menu != null) _pool.Process();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_menu == null || _menuKey == Keys.None || e.KeyCode != _menuKey) return;

            bool open = _menu.Visible || _storyMenu.Visible || _strangersMenu.Visible;
            if (open)
            {
                CloseAll();
                return;
            }
            Rebuild();
            _menu.Visible = true;
        }

        private void CloseAll()
        {
            _menu.Visible = false;
            _storyMenu.Visible = false;
            _strangersMenu.Visible = false;
        }

        private void Rebuild()
        {
            _menu.Clear();

            if (MissionApi.IsReplayActive)
            {
                ReplayMission current = MissionApi.CurrentReplay;
                _menu.Add(new NativeItem("Replay in progress", current?.ToString() ?? string.Empty) { Enabled = false });
                return;
            }

            Launcher launcher = Launcher.Instance;
            ReplayCatalog catalog = launcher.Catalog;
            catalog.RefreshCompletion();
            catalog.RefreshTitles();
            bool onlyCompleted = launcher.Config.OnlyCompletedMissions;
            bool completionOrder = string.Equals(launcher.Config.MenuOrder, "Completion", StringComparison.OrdinalIgnoreCase);
            IReadOnlyList<ReplayMission> story = catalog.Visible(ReplayCategory.Story, onlyCompleted, completionOrder);
            IReadOnlyList<ReplayMission> strangers = catalog.Visible(ReplayCategory.StrangersAndFreaks, onlyCompleted, completionOrder);
            Fill(_storyMenu, story);
            Fill(_strangersMenu, strangers);
            _menu.AddSubMenu(_storyMenu, story.Count.ToString());
            _menu.AddSubMenu(_strangersMenu, strangers.Count.ToString());
        }

        private void Fill(NativeMenu menu, IReadOnlyList<ReplayMission> missions)
        {
            menu.Clear();
            if (missions.Count == 0)
            {
                menu.Add(new NativeItem("No mission completed yet") { Enabled = false });
                return;
            }
            foreach (ReplayMission mission in missions)
            {
                ReplayMission m = mission;
                string locked = MissionApi.LockReason(m);
                var item = locked == null
                    ? new NativeItem($"{m.Id}  {m.Title}", $"Replay \"{m.Title}\".")
                    : new NativeItem($"{m.Id}  {m.Title}", locked, "Locked") { Enabled = false };
                item.Activated += (s, a) =>
                {
                    if (MissionApi.RequestReplay(m.Number, out string error)) CloseAll();
                    else GTA.UI.Notification.PostTicker("~y~" + error, false);
                };
                menu.Add(item);
            }
        }
    }
}
