using System;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;

namespace GameTraps.Core
{
    public sealed class TrapMenu : Script
    {
        private readonly ObjectPool _pool = new ObjectPool();
        private NativeMenu _menu;
        private Keys _menuKey = Keys.None;
        private bool _initialized;

        public TrapMenu()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                TrapRunner core = TrapRunner.Instance;
                if (core == null || !core.IsReady) return;
                _initialized = true;
                if (!core.Config.EnableMenu)
                {
                    Log.Info("Menu disabled (EnableMenu = false).");
                    Tick -= OnTick;
                    KeyDown -= OnKeyDown;
                    return;
                }
                if (!Enum.TryParse(core.Config.MenuKey, true, out _menuKey))
                {
                    Log.Warn($"Invalid MenuKey '{core.Config.MenuKey}', using F9.");
                    _menuKey = Keys.F9;
                }
                _menu = new NativeMenu("Traps", "Trigger a trap");
                _pool.Add(_menu);
                Log.Info($"Menu ready, key {_menuKey}.");
            }
            if (_menu != null) _pool.Process();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_menu == null || _menuKey == Keys.None || e.KeyCode != _menuKey) return;
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
            TrapRunner core = TrapRunner.Instance;
            int running = core.ActiveIds.Count;
            string status = running > 0
                ? "Running: " + running
                : "No trap running";
            _menu.Add(new NativeItem(status, string.Empty, "Waiting: " + core.Pending) { Enabled = false });

            foreach (Trap trap in core.Traps)
            {
                string id = trap.Id;
                var item = new NativeItem(trap.Title, trap.Subtitle, trap.Enabled ? string.Empty : "Disabled") { Enabled = trap.Enabled };
                item.Activated += (s, a) =>
                {
                    if (TrapApi.Trigger(id, out string error)) _menu.Visible = false;
                    else GTA.UI.Notification.PostTicker("~y~" + error, false);
                };
                _menu.Add(item);
            }

            var random = new NativeItem("Random trap");
            random.Activated += (s, a) =>
            {
                TrapApi.TriggerRandom();
                _menu.Visible = false;
            };
            _menu.Add(random);

            var stream = new NativeCheckboxItem("Stream safe",
                "No dancer or hooker models that may show nudity (Irresistible uses nightclub customers).",
                core.Config.StreamSafe);
            stream.CheckboxChanged += (s, a) => core.SetStreamSafe(stream.Checked);
            _menu.Add(stream);

            var clear = new NativeItem("Clear the queue");
            clear.Activated += (s, a) =>
            {
                TrapApi.ClearQueue();
                Rebuild();
            };
            _menu.Add(clear);
        }
    }
}
