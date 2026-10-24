using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GameCollectibles;
using GameEnvironment;
using GameTraps;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;

namespace GameArchipelago.Core
{
    public sealed class ApMenu : Script
    {
        private const string Banner = "Archipelago";
        private readonly ObjectPool _pool = new ObjectPool();
        private readonly List<NativeMenu> _menus = new List<NativeMenu>();
        private NativeMenu _main;
        private NativeMenu _connection;
        private NativeMenu _progress;
        private NativeMenu _missions;
        private NativeMenu _traps;
        private NativeMenu _log;
        private NativeMenu _history;
        private NativeMenu _received;
        private NativeMenu _mine;
        private NativeMenu _world;
        private NativeMenu _hints;
        private NativeMenu _commands;
        private NativeMenu _settings;
        private NativeMenu _vehicles;
        private NativeMenu _boats;
        private NativeMenu _gold;
        private NativeMenu _collectibles;
        private Keys _key = Keys.None;
        private bool _initialized;

        public ApMenu()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private static ArchipelagoScript Core => ArchipelagoScript.Instance;

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                if (Core == null || !Core.IsReady) return;
                _initialized = true;
                if (!Core.Config.EnableMenu)
                {
                    Tick -= OnTick;
                    KeyDown -= OnKeyDown;
                    return;
                }
                if (!Enum.TryParse(Core.Config.MenuKey, true, out _key))
                {
                    Log.Warn($"Invalid MenuKey '{Core.Config.MenuKey}', using F6.");
                    _key = Keys.F6;
                }
                _main = Create("Multiworld");
                _connection = Create("Connection");
                _progress = Create("Progress");
                _missions = Create("Missions");
                _traps = Create("Recent traps");
                _log = Create("Notifications");
                _history = Create("Archipelago log");
                _received = Create("Items received");
                _mine = Create("My items");
                _world = Create("Whole multiworld");
                _hints = Create("Hints");
                _commands = Create("Commands");
                _settings = Create("Settings");
                _vehicles = Create("Vehicles");
                _boats = Create("Boats");
                _gold = Create("Replay for gold");
                _collectibles = Create("Collectibles");
                _connection.Opening += (s, a) => FillConnection();
                _progress.Opening += (s, a) => FillProgress();
                _missions.Opening += (s, a) => FillMissions();
                _traps.Opening += (s, a) => FillTraps();
                _log.Opening += (s, a) => FillLog();
                _history.Opening += (s, a) => FillHistory();
                _received.Opening += (s, a) => FillReceived();
                _mine.Opening += (s, a) => FillFeed(_mine, true);
                _world.Opening += (s, a) => FillFeed(_world, false);
                _hints.Opening += (s, a) => FillHints();
                _settings.Opening += (s, a) => FillSettings();
                _vehicles.Opening += (s, a) => FillVehicles();
                _boats.Opening += (s, a) => FillBoats();
                _gold.Opening += (s, a) => FillGold();
                _collectibles.Opening += (s, a) => FillCollectibles();
                FillCommands();
                Log.Info($"Menu ready, key {_key}.");
            }
            if (_main != null) _pool.Process();
        }

        private NativeMenu Create(string name)
        {
            var menu = new NativeMenu(Banner, name);
            _pool.Add(menu);
            _menus.Add(menu);
            return menu;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (_main == null || _key == Keys.None || e.KeyCode != _key) return;
            if (_menus.Any(m => m.Visible))
            {
                foreach (NativeMenu menu in _menus) menu.Visible = false;
                return;
            }
            FillMain();
            _main.Visible = true;
        }

        private void FillMain()
        {
            _main.Clear();
            ApClient client = Core.Client;
            string status;
            switch (client.State)
            {
                case LinkState.Connected:
                    status = "~g~Connected~s~ as " + client.SlotName;
                    break;
                case LinkState.Connecting:
                    status = "~y~Connecting...";
                    break;
                default:
                    status = "~r~Offline" + (Core.Save != null ? "~s~ (progress saved)" : string.Empty);
                    break;
            }
            _main.Add(new NativeItem(status, client.LastError ?? string.Empty, Core.Save?.Seed ?? string.Empty));
            _main.AddSubMenu(_connection);
            _main.AddSubMenu(_progress, Core.Save == null ? string.Empty : Core.GoalProgress());
            _main.AddSubMenu(_missions);
            _main.AddSubMenu(_traps, Core.Save?.TrapLog.Count.ToString() ?? "0");
            _main.AddSubMenu(_history, Core.Save?.Items.Count.ToString() ?? string.Empty);
            _main.AddSubMenu(_commands);
            _main.AddSubMenu(_gold);
            _main.AddSubMenu(_vehicles, EnvironmentApi.Vehicles.Count.ToString());
            if (Core.Save != null && Core.Save.Boats.Count > 0) _main.AddSubMenu(_boats, Core.Save.Boats.Count.ToString());
            List<CollectibleInfo> active = CollectiblesApi.All.Where(c => !CollectiblesApi.DisabledKinds.Contains(c.Kind)).ToList();
            _main.AddSubMenu(_collectibles, $"{active.Count(c => c.Collected)}/{active.Count}");
            _main.AddSubMenu(_settings);
        }

        private void FillConnection()
        {
            _connection.Clear();
            ConnectionSettings settings = Core.Connection;
            var server = new NativeItem("Server", "Address and port of the Archipelago room, for example archipelago.gg:38281.", settings.Server);
            server.Activated += (s, a) => Edit(text => settings.Server = text, settings.Server, server);
            var slot = new NativeItem("Slot name", "Your player name in the multiworld.", settings.Slot);
            slot.Activated += (s, a) => Edit(text => settings.Slot = text, settings.Slot, slot);
            var password = new NativeItem("Password", "Leave empty if the room has none.", string.IsNullOrEmpty(settings.Password) ? "None" : "****");
            password.Activated += (s, a) =>
            {
                string text = Game.GetUserInput(settings.Password ?? string.Empty);
                if (text == null) return;
                settings.Password = text.Trim();
                settings.Save();
                password.AltTitle = string.IsNullOrEmpty(settings.Password) ? "None" : "****";
            };
            _connection.Add(server);
            _connection.Add(slot);
            _connection.Add(password);
            if (Core.Client.State == LinkState.Offline)
            {
                var connect = new NativeItem("~g~Connect", "Connects with the settings above. They are kept for the next sessions.");
                connect.Activated += (s, a) =>
                {
                    Core.Connect();
                    _connection.Back();
                };
                _connection.Add(connect);
            }
            else
            {
                var disconnect = new NativeItem("~r~Disconnect", "Your progress stays saved and is sent at the next connection.");
                disconnect.Activated += (s, a) =>
                {
                    Core.Disconnect();
                    _connection.Back();
                };
                _connection.Add(disconnect);
            }
        }

        private static void Edit(Action<string> apply, string current, NativeItem item)
        {
            string text = Game.GetUserInput(current ?? string.Empty);
            if (string.IsNullOrWhiteSpace(text)) return;
            apply(text.Trim());
            Core.Connection.Save();
            item.AltTitle = text.Trim();
        }

        private void FillProgress()
        {
            _progress.Clear();
            SlotSave save = Core.Save;
            if (save == null)
            {
                _progress.Add(new NativeItem("Connect once to load your game.") { Enabled = false });
                return;
            }
            Info(_progress, "Goal", Core.GoalText(), Core.GoalProgress());
            if (save.EstimatedGoalMinutes > 0)
                Info(_progress, "Time to the goal", "Rough estimate to reach your goal, unlocks included (about 10 minutes per mission, 20 for a heist).",
                    $"~{save.EstimatedGoalMinutes / 60}h{save.EstimatedGoalMinutes % 60:00}");
            if (save.EstimatedMinutes > 0)
                Info(_progress, "Every location (max)", "Upper bound to check every location of your world: every mission, medal and collectible. You do not need all of them for the goal.",
                    $"{save.EstimatedMinutes / 60}h{save.EstimatedMinutes % 60:00}");
            if (Core.ChaosMode) Info(_progress, "Chaos mode", "Missions come back at random and the other players' checks set off traps and gifts.", "On");
            List<MissionInfo> world = Core.WorldMissions.ToList();
            int story = world.Count(m => !m.Strangers);
            int strangers = world.Count(m => m.Strangers);
            Info(_progress, "Missions passed", "All the missions of your world.", $"{world.Count(Core.IsPassed)}/{world.Count}");
            Info(_progress, "Story missions", string.Empty, $"{world.Count(m => !m.Strangers && Core.IsPassed(m))}/{story}");
            if (strangers > 0) Info(_progress, "Strangers and Freaks", string.Empty, $"{world.Count(m => m.Strangers && Core.IsPassed(m))}/{strangers}");
            Info(_progress, "Missions available", "Unlocked and playable with your characters.", $"{world.Count(Core.IsPlayable)}/{world.Count}");
            var characters = new List<string>();
            for (int c = 0; c < 3; c++)
                if ((Core.UnlockedMask & (1 << c)) != 0) characters.Add(ApData.CharacterNames[c]);
            Info(_progress, "Characters", "Characters you can play.", characters.Count == 0 ? "None" : string.Join(", ", characters));
            if (save.GoldChecks > 0)
                Info(_progress, "Gold medals", "Different missions passed with a gold medal.", $"{Math.Min(save.Gold.Count, save.GoldChecks)}/{save.GoldChecks}");
            if (save.ObjectiveChecks > 0)
                Info(_progress, "Objectives", "Different optional objectives passed.", $"{Math.Min(save.Objectives.Count, save.ObjectiveChecks)}/{save.ObjectiveChecks}");
            foreach (CollectibleSet set in ApData.Collectibles)
            {
                if (!save.CollectibleSteps.TryGetValue(set.Key, out int step) || step <= 0) continue;
                int count = Core.CollectedCount(set);
                int worth = Math.Max(1, save.ChecksPerCollectible);
                int checks = ArchipelagoScript.Milestones(set, step).Count() * worth;
                int done = ArchipelagoScript.Milestones(set, step).Count(m => m <= count) * worth;
                string description = (set.Underwater ? (save.GuaranteedVehicles ? "Underwater: use your Underwater Vehicle. " : "Underwater: find a way down. ") : string.Empty)
                    + $"One check every {step}" + (worth > 1 ? $", each worth {worth} locations." : ".");
                Info(_progress, set.Name, description, $"{count}/{set.Total}  ({done}/{checks} checks)");
            }
            IReadOnlyList<long> all = Core.Client.AllLocations;
            int checkedCount = Math.Max(save.Checked.Union(save.ServerChecked).Count(), 0);
            Info(_progress, "Locations checked", "Everything you sent to the multiworld.", all.Count > 0 ? $"{checkedCount}/{all.Count}" : checkedCount.ToString());
            _progress.AddSubMenu(_received, save.Items.Count.ToString());
            if (save.Pending.Count > 0) Info(_progress, "Waiting to be sent", "Sent automatically when the connection is back.", save.Pending.Count.ToString());
        }

        private static void Info(NativeMenu menu, string title, string description, string value)
        {
            menu.Add(new NativeItem(title, description ?? string.Empty, value ?? string.Empty));
        }

        private void FillMissions()
        {
            _missions.Clear();
            if (Core.Save == null)
            {
                _missions.Add(new NativeItem("Connect once to load your game.") { Enabled = false });
                return;
            }
            IEnumerable<MissionInfo> ordered = Core.WorldMissions
                .OrderBy(m => Core.IsPlayable(m) ? (Core.IsPassed(m) ? 1 : 0) : 2)
                .ThenBy(m => m.Strangers);
            foreach (MissionInfo mission in ordered)
            {
                MissionInfo m = mission;
                bool playable = Core.IsPlayable(m);
                bool passed = Core.IsPassed(m);
                string state = playable ? (passed ? "~c~Passed" : "~g~Available") : passed ? "~c~Passed (locked)" : "~r~Locked";
                string need;
                string with = m.Players != 0 ? " Played with " + ArchipelagoScript.PlayerNames(m) + "." : string.Empty;
                if (!Core.Owns(m.Item)) need = $"Needs the item \"Mission: {m.Name}\".{with}";
                else if (!playable) need = "Locked: " + Core.MissingCharacters(m);
                else need = "Started by " + ArchipelagoScript.TriggerNames(m) + "." + with + " Select to set a waypoint.";
                if (Core.GoalMissionInfo == m && Core.Save.Goal == 3) need = "~y~Your goal.~s~ " + need;
                var item = new NativeItem(m.Name, need, state);
                item.Activated += (s, a) => Waypoint(m);
                _missions.Add(item);
            }
        }

        private void Waypoint(MissionInfo mission)
        {
            int current = -1;
            Ped ped = Game.Player.Character;
            if (ped != null && ped.Exists())
            {
                int hash = ped.Model.Hash;
                if (hash == new Model(PedHash.Michael).Hash) current = 0;
                else if (hash == new Model(PedHash.Franklin).Hash) current = 1;
                else if (hash == new Model(PedHash.Trevor).Hash) current = 2;
            }
            MarkerPoint point = current >= 0 ? mission.Points[current] : null;
            if (point == null) point = mission.Points.FirstOrDefault(p => p != null);
            if (point == null)
            {
                Notifier.Show("~y~No start point for this mission.");
                return;
            }
            Function.Call(Hash.SET_NEW_WAYPOINT, point.X, point.Y);
            Notifier.Show("~y~Waypoint:~s~ " + mission.Name);
        }

        private void FillTraps()
        {
            _traps.Clear();
            SlotSave save = Core.Save;
            if (save == null || save.TrapLog.Count == 0)
            {
                _traps.Add(new NativeItem("No trap received yet.") { Enabled = false });
                return;
            }
            foreach (TrapRecord record in save.TrapLog)
            {
                TrapInfo trap = ApData.TrapById(record.Id);
                string name = trap?.Name ?? record.Id;
                string title = TrapApi.GetTitle(record.Id);
                string description = $"From {record.Sender} at {record.ReceivedAt}." + (string.IsNullOrEmpty(title) || title == name ? string.Empty : $" In game: \"{title}\".");
                _traps.Add(new NativeItem(name, description, Core.TrapStatus(record)));
            }
        }

        private void FillLog()
        {
            _log.Clear();
            List<string> lines = Core.Notifier.History.ToList();
            if (lines.Count == 0)
            {
                _log.Add(new NativeItem("Nothing yet.") { Enabled = false });
                return;
            }
            foreach (string line in lines)
            {
                string title = line.Length > 48 ? line.Substring(0, 45) + "..." : line;
                _log.Add(new NativeItem(title, line));
            }
        }

        private void FillHistory()
        {
            _history.Clear();
            SlotSave save = Core.Save;
            _history.AddSubMenu(_received, save?.Items.Count.ToString() ?? "0");
            _history.AddSubMenu(_mine, save?.Feed.Count(f => f.Kind == "in" || f.Kind == "out" || f.Kind == "self").ToString() ?? "0");
            _history.AddSubMenu(_hints, save?.Feed.Count(f => f.Kind == "hint").ToString() ?? "0");
            _history.AddSubMenu(_world, save?.Feed.Count.ToString() ?? "0");
            _history.AddSubMenu(_log);
        }

        private void FillReceived()
        {
            _received.Clear();
            SlotSave save = Core.Save;
            if (save == null || save.Items.Count == 0)
            {
                _received.Add(new NativeItem("Nothing received yet.") { Enabled = false });
                return;
            }
            for (int i = save.Items.Count - 1; i >= 0; i--)
            {
                ReceivedRecord record = save.Items[i];
                string name = ApData.ItemName(record.Item);
                if (ApData.TrapByItem(record.Item) != null) name = "A trap (details in Recent traps)";
                string sender = record.Player == 0 ? "Start" : record.Sender ?? "?";
                string where = string.IsNullOrEmpty(record.LocationName) || record.Player == 0 ? string.Empty : $" at {record.LocationName}";
                string when = string.IsNullOrEmpty(record.ReceivedAt) ? string.Empty : record.ReceivedAt + ", ";
                string description = $"#{i + 1}, {when}from {sender}{where}." + State(record);
                string title = name.Length > 40 ? name.Substring(0, 37) + "..." : name;
                _received.Add(new NativeItem(title, description, sender.Length > 14 ? sender.Substring(0, 13) + "." : sender));
            }
        }

        private static string State(ReceivedRecord record)
        {
            MissionInfo mission = ApData.MissionByItem(record.Item);
            if (mission != null)
            {
                if (!Core.InWorld(mission)) return " Not in your world.";
                if (Core.IsPassed(mission)) return " ~c~Passed.";
                if (Core.IsPlayable(mission)) return " ~g~Playable now.";
                return " ~r~Locked:~s~ " + (Core.MissingCharacters(mission) ?? "not available yet.");
            }
            if (ApData.CharacterByItem(record.Item) >= 0) return " ~g~Playable.";
            if (!string.IsNullOrEmpty(record.GiftName)) return " " + record.GiftName + ".";
            return string.Empty;
        }

        private void FillFeed(NativeMenu menu, bool mine)
        {
            menu.Clear();
            SlotSave save = Core.Save;
            List<FeedEntry> entries = save == null ? new List<FeedEntry>()
                : save.Feed.Where(f => !mine || f.Kind == "in" || f.Kind == "out" || f.Kind == "self").ToList();
            if (entries.Count == 0)
            {
                menu.Add(new NativeItem(mine ? "Nothing sent or received yet." : "Nothing yet. The log fills while you are connected.") { Enabled = false });
                return;
            }
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                FeedEntry entry = entries[i];
                string tag;
                switch (entry.Kind)
                {
                    case "in": tag = "~g~Received"; break;
                    case "out": tag = "~b~Sent"; break;
                    case "self": tag = "~g~Found"; break;
                    case "hint": tag = "~y~Hint"; break;
                    case "chat": tag = "~p~Chat"; break;
                    default: tag = "~c~Others"; break;
                }
                string title = entry.Text.Length > 44 ? entry.Text.Substring(0, 41) + "..." : entry.Text;
                menu.Add(new NativeItem(title, $"{entry.Time}: {entry.Text}", tag));
            }
        }

        private void FillHints()
        {
            _hints.Clear();
            SlotSave save = Core.Save;
            List<FeedEntry> hints = save == null ? new List<FeedEntry>() : save.Feed.Where(f => f.Kind == "hint").ToList();
            if (hints.Count == 0)
            {
                _hints.Add(new NativeItem("No hint yet.", "Hints you ask for (F6 > Commands) and hints about your world are kept here.") { Enabled = false });
                return;
            }
            var seen = new HashSet<string>();
            for (int i = hints.Count - 1; i >= 0; i--)
            {
                FeedEntry entry = hints[i];
                string text = entry.Text.StartsWith("[Hint]: ", StringComparison.Ordinal) ? entry.Text.Substring(8) : entry.Text;
                string key = text.Replace(" (found)", string.Empty).Replace(" (not found)", string.Empty);
                if (!seen.Add(key)) continue;
                bool found = text.EndsWith("(found)", StringComparison.Ordinal);
                string title = text.Length > 44 ? text.Substring(0, 41) + "..." : text;
                _hints.Add(new NativeItem(title, $"{entry.Time}: {text}", found ? "~c~Found" : "~y~To find"));
            }
        }

        private void FillCommands()
        {
            _commands.Clear();
            AddSay("Send a message", "Type a chat message or any server command.", null, true);
            AddSay("Hint an item", "Type the name of an item: !hint <item>. Costs hint points.", "!hint ", true);
            AddSay("Hint a location", "Asks which item is hidden at one of your locations (for example \"Gold Medal 3\" or \"Spaceship Parts: at 10 collected\"), so you know if it is worth doing now. Costs hint points.", "!hint_location ", true);
            AddSay("Hint points and hints", "!hint without argument: lists your hints and points.", "!hint", false);
            AddSay("Remaining items", "!remaining: items still to be found for you.", "!remaining", false);
            AddSay("Players", "!players: everyone in the room.", "!players", false);
            AddSay("Status", "!status: completion of each player.", "!status", false);
            AddSay("Release", "!release: sends all your remaining items to their owners (when allowed).", "!release", false);
            AddSay("Collect", "!collect: receives all your items from the other worlds (when allowed).", "!collect", false);
        }

        private void AddSay(string title, string description, string prefix, bool input)
        {
            var item = new NativeItem(title, description);
            item.Activated += (s, a) =>
            {
                if (!Core.Client.IsConnected)
                {
                    Notifier.Show("~y~Not connected.");
                    return;
                }
                string text = prefix ?? string.Empty;
                if (input)
                {
                    string typed = Game.GetUserInput(string.Empty);
                    if (string.IsNullOrWhiteSpace(typed)) return;
                    text += typed.Trim();
                }
                Core.Client.Say(text);
                Core.Notifier.Remember("> " + text);
                History.Write("SAY", "text", text);
            };
            _commands.Add(item);
        }

        private void FillSettings()
        {
            _settings.Clear();
            var stream = new NativeCheckboxItem("Stream safe", "No dancer or hooker models that may show nudity in the traps.", TrapApi.StreamSafe);
            stream.CheckboxChanged += (s, a) => TrapApi.StreamSafe = stream.Checked;
            _settings.Add(stream);
            if (Core.Save != null)
            {
                var death = new NativeCheckboxItem("DeathLink", "When you die, everyone with DeathLink dies, and the other way around. Starts from your Archipelago options.", Core.DeathLinkActive);
                death.CheckboxChanged += (s, a) => Core.SetDeathLinkMode(death.Checked);
                _settings.Add(death);
            }
            var report = new NativeItem("Create a bug report", "Zips your save, the event history, the settings and the logs of every mod into GameArchipelago/reports. Send that file with your bug.");
            report.Activated += (s, a) =>
            {
                try
                {
                    History.Write("REPORT");
                    string file = History.ExportReport(System.IO.Path.GetDirectoryName(Core.DataDirectory), Core.Save?.Key);
                    Notifier.Show("~g~Bug report saved:~s~ " + System.IO.Path.GetFileName(file));
                    Log.Info("Bug report: " + file);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not create the bug report.", ex);
                    Notifier.Show("~r~Could not create the bug report (see GameArchipelago.log).");
                }
            };
            _settings.Add(report);
        }

        private void FillVehicles()
        {
            _vehicles.Clear();
            var fix = new NativeItem("Repair the vehicle you are in", "Repairs one of your vehicles or boats on the spot, for free.");
            fix.Activated += (s, a) =>
            {
                if (SubmarineDock.RepairCurrent())
                {
                    Notifier.Show("~g~Boat repaired.");
                    return;
                }
                Notifier.Show(EnvironmentApi.RepairCurrentVehicle(out string error) ? "~g~Vehicle repaired." : "~y~" + error);
            };
            _vehicles.Add(fix);
            IReadOnlyList<OwnedVehicle> vehicles = EnvironmentApi.Vehicles;
            if (vehicles.Count == 0)
            {
                _vehicles.Add(new NativeItem("No vehicle yet", "Vehicles you receive are kept here.") { Enabled = false });
                return;
            }
            foreach (OwnedVehicle vehicle in vehicles)
            {
                int id = vehicle.Id;
                NativeItem item;
                if (vehicle.IsDestroyed)
                {
                    item = new NativeItem(vehicle.Name, $"Destroyed. Pay ${vehicle.RepairCost:#,0} to repair it and have it delivered.", $"Repair ${vehicle.RepairCost:#,0}");
                    item.Activated += (s, a) =>
                    {
                        if (!EnvironmentApi.RepairVehicle(id, out string error))
                        {
                            Notifier.Show("~y~" + error);
                            FillVehicles();
                            return;
                        }
                        if (EnvironmentApi.RequestDelivery(id, out error)) CloseAll();
                        else
                        {
                            Notifier.Show("~y~" + error);
                            FillVehicles();
                        }
                    };
                }
                else
                {
                    string state = vehicle.Status == VehicleStatus.Delivered ? "Delivered" : "Deliver";
                    item = new NativeItem(vehicle.Name, "Have it delivered next to you.", state);
                    item.Activated += (s, a) =>
                    {
                        if (EnvironmentApi.RequestDelivery(id, out string error)) CloseAll();
                        else Notifier.Show("~y~" + error);
                    };
                }
                _vehicles.Add(item);
            }
        }

        private void FillBoats()
        {
            _boats.Clear();
            SlotSave save = Core.Save;
            if (save == null || save.Boats.Count == 0)
            {
                _boats.Add(new NativeItem("No boat yet", "Boats and submarines from Vehicle Gifts are listed here.") { Enabled = false });
                return;
            }
            foreach (string model in save.Boats)
            {
                string name = Core.BoatName(model);
                var item = new NativeItem(name, "Spawns it in the nearest deep water and sets a waypoint.", "Call");
                item.Activated += (s, a) =>
                {
                    bool ok = SubmarineDock.Call(model, name, out string message);
                    Notifier.Show((ok ? "~g~" : "~y~") + message);
                    History.Write("BOAT", "model", model, "ok", ok, "message", message);
                    if (ok) CloseAll();
                };
                _boats.Add(item);
            }
        }

        private void FillGold()
        {
            _gold.Clear();
            SlotSave save = Core.Save;
            if (save == null)
            {
                _gold.Add(new NativeItem("Connect once to load your game.") { Enabled = false });
                return;
            }
            bool goldLeft = Enumerable.Range(1, Math.Max(0, save.GoldChecks)).Any(n => !save.IsDone(ApData.GoldBase + n));
            bool objectivesLeft = Enumerable.Range(1, Math.Max(0, save.ObjectiveChecks)).Any(n => !save.IsDone(ApData.ObjectiveBase + n));
            if (!goldLeft && !objectivesLeft)
            {
                _gold.Add(new NativeItem("Nothing to replay", "Every gold medal and objective check of your world is done.") { Enabled = false });
                return;
            }
            var entries = new List<Tuple<MissionInfo, int, int>>();
            foreach (MissionInfo mission in Core.WorldMissions)
            {
                if (!Core.IsPassed(mission) || !Core.IsPlayable(mission)) continue;
                if (save.Gold.Contains(mission.Code, StringComparer.OrdinalIgnoreCase)) continue;
                int done = save.Objectives.Count(o => o.StartsWith(mission.Code + "#", StringComparison.OrdinalIgnoreCase));
                int total = save.ObjectiveTotals.TryGetValue(mission.Code, out int known) ? known : 0;
                if (!goldLeft && total > 0 && done >= total) continue;
                entries.Add(Tuple.Create(mission, done, total));
            }
            if (entries.Count == 0)
            {
                _gold.Add(new NativeItem("Nothing to replay", "Every mission you passed already has its gold medal.") { Enabled = false });
                return;
            }
            foreach (Tuple<MissionInfo, int, int> entry in entries.OrderBy(e => e.Item3 > 0 ? e.Item3 - e.Item2 : 99).ThenBy(e => e.Item1.Name))
            {
                MissionInfo mission = entry.Item1;
                string left = entry.Item3 > 0 ? $"{entry.Item3 - entry.Item2} objective{(entry.Item3 - entry.Item2 == 1 ? string.Empty : "s")} left" : "No gold yet";
                var todo = new List<string>();
                var done = new List<string>();
                for (int position = 1; position <= entry.Item3; position++)
                {
                    string key = $"{mission.Code}#{position}";
                    string text = save.ObjectiveNames.TryGetValue(key, out string known) ? known : "Objective " + position;
                    if (save.Objectives.Contains(key)) done.Add(text);
                    else todo.Add(text);
                }
                string description = entry.Item3 > 0
                    ? (todo.Count > 0 ? "~y~To do:~s~ " + string.Join("; ", todo) + ". " : string.Empty)
                      + (done.Count > 0 ? "~c~Done once:~s~ " + string.Join("; ", done) + ". " : string.Empty)
                      + "Gold needs them all in one run. Select to replay it now."
                    : "The objectives are shown when the mission starts. Select to replay it now.";
                var item = new NativeItem(mission.Name, description, left);
                item.Activated += (s, a) => Replay(mission);
                _gold.Add(item);
            }
        }

        private void Replay(MissionInfo mission)
        {
            if (!MissionLauncher.MissionApi.IsReady)
            {
                Notifier.Show("~y~MissionLauncher is not ready.");
                return;
            }
            MissionLauncher.ReplayMission replay = MissionLauncher.MissionApi.Missions.FirstOrDefault(m => string.Equals(m.Code, mission.Code, StringComparison.OrdinalIgnoreCase));
            if (replay == null)
            {
                Notifier.Show("~y~This mission cannot be replayed right now.");
                return;
            }
            if (MissionLauncher.MissionApi.RequestReplay(replay.Number, out string error))
            {
                History.Write("GOLD_REPLAY", "code", mission.Code);
                CloseAll();
            }
            else Notifier.Show("~y~" + error);
        }

        private void FillCollectibles()
        {
            _collectibles.Clear();
            IReadOnlyList<CollectibleInfo> all = CollectiblesApi.All;
            IReadOnlyCollection<CollectibleKind> disabled = CollectiblesApi.DisabledKinds;
            foreach (CollectibleKind kind in Enum.GetValues(typeof(CollectibleKind)))
            {
                if (disabled.Contains(kind)) continue;
                int total = all.Count(c => c.Kind == kind);
                if (total == 0) continue;
                int done = all.Count(c => c.Kind == kind && c.Collected);
                _collectibles.Add(new NativeItem(KindName(kind), kind == CollectibleKind.NuclearWaste || kind == CollectibleKind.SubmarinePart ? "Underwater." : string.Empty, $"{done}/{total}"));
            }
            string[] modes = { "None", "Nearby", "All" };
            var markers = new NativeListItem<string>("Map markers", "Show the collectibles left on the map.", modes);
            markers.SelectedIndex = Math.Max(0, Array.FindIndex(modes, m => string.Equals(m, CollectiblesApi.MarkerMode, StringComparison.OrdinalIgnoreCase)));
            markers.ItemChanged += (s, a) => CollectiblesApi.MarkerMode = markers.SelectedItem;
            _collectibles.Add(markers);
            bool radar = Core.Save == null || Core.HasRadar;
            var nearest = new NativeItem("Route to the nearest one",
                radar ? "Sets a waypoint on the closest collectible left." : "Needs the \"Collectible Radar\" item from the multiworld.",
                radar ? string.Empty : "~r~Locked");
            nearest.Activated += (s, a) =>
            {
                if (Core.Save != null && !Core.HasRadar)
                {
                    Notifier.Show("~y~You need the Collectible Radar for that.");
                    return;
                }
                CollectibleInfo info = CollectiblesApi.Nearest();
                if (info == null)
                {
                    Notifier.Show("~g~Everything is collected!");
                    return;
                }
                Function.Call(Hash.SET_NEW_WAYPOINT, info.Position.X, info.Position.Y);
                Notifier.Show($"~y~{KindName(info.Kind)} #{info.Number}");
                CloseAll();
            };
            _collectibles.Add(nearest);
            SlotSave save = Core.Save;
            if (save != null && save.Goal == 4 && !string.IsNullOrEmpty(save.GoalCollectible) && !save.GoalDone)
            {
                var goal = new NativeItem("~y~Route to your goal", "Sets a waypoint on " + Core.GoalCollectibleName + ", the collectible that finishes your game.");
                goal.Activated += (s, a) =>
                {
                    CollectibleInfo info = CollectiblesApi.Get(save.GoalCollectible);
                    if (info == null)
                    {
                        Notifier.Show("~y~This collectible is not in the game.");
                        return;
                    }
                    Function.Call(Hash.SET_NEW_WAYPOINT, info.Position.X, info.Position.Y);
                    Notifier.Show("~y~Waypoint:~s~ " + Core.GoalCollectibleName);
                    CloseAll();
                };
                _collectibles.Add(goal);
            }
        }

        private static string KindName(CollectibleKind kind)
        {
            switch (kind)
            {
                case CollectibleKind.SpaceshipPart: return "Spaceship parts";
                case CollectibleKind.LetterScrap: return "Letter scraps";
                case CollectibleKind.NuclearWaste: return "Nuclear waste";
                default: return "Submarine parts";
            }
        }

        private void CloseAll()
        {
            foreach (NativeMenu menu in _menus) menu.Visible = false;
        }
    }
}
