using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using LemonUI;
using LemonUI.Menus;

namespace GameEnvironment.Core
{
    public sealed class EnvironmentMenu : Script
    {
        private readonly ObjectPool _pool = new ObjectPool();
        private NativeMenu _menu;
        private NativeMenu _charactersMenu;
        private NativeMenu _vehiclesMenu;
        private Keys _menuKey = Keys.None;
        private bool _initialized;

        public EnvironmentMenu()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                EnvironmentScript core = EnvironmentScript.Instance;
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
                    Log.Warn($"Invalid MenuKey '{core.Config.MenuKey}', using F8.");
                    _menuKey = Keys.F8;
                }

                string title = "Environment";
                _menu = new NativeMenu(title, "Sterile world");
                _charactersMenu = new NativeMenu(title, "Characters");
                _vehiclesMenu = new NativeMenu(title, "Vehicles");
                _pool.Add(_menu);
                _pool.Add(_charactersMenu);
                _pool.Add(_vehiclesMenu);
                Log.Info($"Menu ready, key {_menuKey}.");
            }

            if (_menu != null) _pool.Process();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_menu == null || _menuKey == Keys.None || e.KeyCode != _menuKey) return;
            if (_menu.Visible || _charactersMenu.Visible || _vehiclesMenu.Visible)
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
            _charactersMenu.Visible = false;
            _vehiclesMenu.Visible = false;
        }

        private void Rebuild()
        {
            _menu.Clear();
            EnvironmentScript core = EnvironmentScript.Instance;

            if (core.Mode != EnvironmentMode.Sterile)
            {
                _menu.Add(new NativeItem(ModeText(core.Mode), "Start a new game to create the sterile world. Story saves are never modified.") { Enabled = false });
                return;
            }
            if (core.IsBusy)
            {
                _menu.Add(new NativeItem("Replay in progress") { Enabled = false });
                return;
            }

            PlayableCharacter? current = Characters.Current;
            if (current != null)
                _menu.Add(new NativeItem("Money", string.Empty, GameText.Money(Game.Player.Money)) { Enabled = false });
            _menu.Add(new NativeItem("Game completion", string.Empty, "???") { Enabled = false });

            FillCharacters(core);
            FillVehicles(core);
            _menu.AddSubMenu(_charactersMenu, $"{EnvironmentApi.UnlockedCharacters.Count}/3");
            _menu.AddSubMenu(_vehiclesMenu, core.Garage.Records.Count.ToString());
        }

        private void FillCharacters(EnvironmentScript core)
        {
            _charactersMenu.Clear();
            PlayableCharacter? current = Characters.Current;
            foreach (PlayableCharacter character in Characters.All)
            {
                bool unlocked = core.Roster.IsUnlocked(character);
                string status = character == current
                    ? "Current"
                    : unlocked ? "Unlocked" : "Locked";
                _charactersMenu.Add(new NativeItem(Characters.Name(character), string.Empty, status) { Enabled = unlocked });
            }
        }

        private void FillVehicles(EnvironmentScript core)
        {
            _vehiclesMenu.Clear();
            IReadOnlyList<VehicleRecord> records = core.Garage.Records;
            if (records.Count == 0)
            {
                _vehiclesMenu.Add(new NativeItem("No vehicle yet") { Enabled = false });
                return;
            }
            foreach (VehicleRecord record in records)
            {
                int id = record.Id;
                string name = Garage.DisplayName(record.Model);
                NativeItem item;
                switch (record.Status)
                {
                    case VehicleStatus.Destroyed:
                        int cost = core.Garage.RepairCost(record);
                        item = new NativeItem(name,
                            $"Destroyed. Pay {GameText.Money(cost)} to repair it.",
                            "Repair " + GameText.Money(cost));
                        item.Activated += (s, a) =>
                        {
                            if (!EnvironmentApi.RepairVehicle(id, out string error)) GTA.UI.Notification.PostTicker("~y~" + error, false);
                            FillVehicles(core);
                        };
                        break;
                    case VehicleStatus.Delivered:
                        item = new NativeItem(name, "Bring it next to you again.", "Delivered");
                        item.Activated += (s, a) => Deliver(id);
                        break;
                    default:
                        item = new NativeItem(name, "Have it delivered next to you.", "Deliver");
                        item.Activated += (s, a) => Deliver(id);
                        break;
                }
                _vehiclesMenu.Add(item);
            }
        }

        private void Deliver(int id)
        {
            if (EnvironmentApi.RequestDelivery(id, out string error)) CloseAll();
            else GTA.UI.Notification.PostTicker("~y~" + error, false);
        }

        private static string ModeText(EnvironmentMode mode)
        {
            switch (mode)
            {
                case EnvironmentMode.StorySave:
                    return "Story save: inactive";
                case EnvironmentMode.Disabled:
                    return "Disabled (unsupported game version)";
                default:
                    return "Waiting for the game";
            }
        }
    }
}
