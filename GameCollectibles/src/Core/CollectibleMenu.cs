using System;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace GameCollectibles.Core
{
    public sealed class CollectibleMenu : Script
    {
        private readonly ObjectPool _pool = new ObjectPool();
        private NativeMenu _menu;
        private Keys _key = Keys.None;
        private bool _initialized;

        public CollectibleMenu()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                CollectibleRunner core = CollectibleRunner.Instance;
                if (core == null || !core.IsReady) return;
                _initialized = true;
                if (!core.Config.EnableMenu)
                {
                    Tick -= OnTick;
                    KeyDown -= OnKeyDown;
                    return;
                }
                if (!Enum.TryParse(core.Config.MenuKey, true, out _key))
                {
                    Log.Warn($"Invalid MenuKey '{core.Config.MenuKey}', using F10.");
                    _key = Keys.F10;
                }
                _menu = new NativeMenu("Collectibles", "Archipelago checks");
                _pool.Add(_menu);
            }
            if (_menu != null) _pool.Process();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_menu == null || e.KeyCode != _key) return;
            if (_menu.Visible)
            {
                _menu.Visible = false;
                return;
            }
            Rebuild();
            _menu.Visible = true;
        }

        private void Rebuild()
        {
            _menu.Clear();
            CollectibleRunner core = CollectibleRunner.Instance;
            _menu.Add(new NativeItem("Total", string.Empty, $"{core.CollectedCount}/{core.Spots.Count}") { Enabled = false });
            foreach (CollectibleKind kind in Enum.GetValues(typeof(CollectibleKind)))
            {
                int total = core.Spots.Count(s => s.Kind == kind);
                if (total == 0) continue;
                int done = core.Spots.Count(s => s.Kind == kind && core.Store.Contains(s.Id));
                _menu.Add(new NativeItem(GameText.Kind(kind), string.Empty, $"{done}/{total}") { Enabled = false });
            }

            BlipMode[] modes = { BlipMode.None, BlipMode.Nearby, BlipMode.All };
            string[] names = { "None", "Nearby", "All" };
            var blips = new NativeListItem<string>("Map markers", "Show uncollected items on the map.", names);
            blips.SelectedIndex = Math.Max(0, Array.IndexOf(modes, core.Config.BlipMode));
            blips.ItemChanged += (s, a) =>
            {
                core.Config.BlipMode = modes[blips.SelectedIndex];
                core.RefreshBlips();
                core.Config.Save();
            };
            _menu.Add(blips);

            var nearest = new NativeItem("Route to the nearest one", "Sets a waypoint on the closest uncollected item.");
            nearest.Activated += (s, a) =>
            {
                Spot spot = core.Nearest();
                if (spot == null)
                {
                    GTA.UI.Notification.PostTicker("~g~Everything is collected!", false);
                    return;
                }
                Function.Call(Hash.SET_NEW_WAYPOINT, spot.Position.X, spot.Position.Y);
                GTA.UI.Notification.PostTicker($"~y~{GameText.Kind(spot.Kind)} #{spot.Number}", false);
                _menu.Visible = false;
            };
            _menu.Add(nearest);
        }
    }
}
