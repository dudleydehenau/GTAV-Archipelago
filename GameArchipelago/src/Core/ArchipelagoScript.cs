using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using GameCollectibles;
using GameEnvironment;
using GameTraps;
using GTA;
using GTA.Native;
using MissionLauncher;
using Newtonsoft.Json.Linq;

namespace GameArchipelago.Core
{
    public sealed class ArchipelagoScript : Script
    {
        private const int SyncInterval = 1000;
        private const int ResendInterval = 10000;
        private const int ReconnectDelay = 15000;
        private const int TrapLogSize = 30;
        private const int ChaosShuffleInterval = 600000;
        private const int StarterRevision = 3;
        private const int ChaosCooldown = 45000;

        private readonly Dictionary<int, MissionMarkerState> _markers = new Dictionary<int, MissionMarkerState>();
        private HashSet<long> _owned = new HashSet<long>();
        private HashSet<string> _slotMissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _ownedCount = -1;
        private bool _started;
        private bool _wantOnline;
        private bool _missionsHooked;
        private bool _collectiblesHooked;
        private bool _collectiblesSynced;
        private bool _wasDead;
        private bool _saveDirty;
        private int _nextSync;
        private int _nextResend;
        private int _reconnectAt = -1;
        private int _deathGraceUntil;
        private int _nextCharacterHint;
        private int _nextSwitchTry;
        private int _objectivePollAt = -1;
        private int _kitAt = -1;
        private int _nextSaveAt;
        private int _lastCheckAt = -1;
        private int _announceCalmSince = -1;
        private string _markerSnapshot;
        private string _kitMission;
        private readonly HashSet<uint> _kitGiven = new HashSet<uint>();
        private int _objectivePollUntil;
        private int _lastErrorAt = -1;
        private DeathLink _pendingDeath;
        private int _serverCheckedCount = -1;
        private bool _wasReplay;
        private int _missionGiftFrom = -1;
        private readonly HashSet<string> _chaosMissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _nextChaosShuffle;
        private int _nextChaosEvent;
        private readonly Random _random = new Random();

        public ArchipelagoScript()
        {
            if (Instance != null) return;
            Instance = this;
            DataDirectory = Path.Combine(BaseDirectory, "GameArchipelago");
            Log.Init(DataDirectory);
            History.Init(DataDirectory);
            Config = ClientConfig.Load(DataDirectory);
            Log.Verbose = Config.VerboseLog;
            Connection = ConnectionSettings.Load(DataDirectory);
            Store = new SaveStore(DataDirectory);
            Client = new ApClient();
            Notifier = new Notifier(Config.NotificationDelay, Config.LogSize);
            Gifts = new GiftCatalog();
            Gifts.Load(DataDirectory);
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisconnected;
            Client.Message += OnMessage;
            Client.DeathReceived += OnDeathReceived;
            SlotSave last = Store.Load(Connection.LastSave);
            if (last != null) Activate(last);
            Tick += OnTick;
            Aborted += OnAborted;
            Log.Info("GameArchipelago ready" + (Save != null ? $", last game {Save.Key}." : "."));
        }

        internal static ArchipelagoScript Instance { get; private set; }
        internal string DataDirectory { get; }
        internal ClientConfig Config { get; }
        internal ConnectionSettings Connection { get; }
        internal SaveStore Store { get; }
        internal ApClient Client { get; }
        internal Notifier Notifier { get; }
        internal GiftCatalog Gifts { get; }
        internal SlotSave Save { get; private set; }
        internal int UnlockedMask { get; private set; }
        internal bool IsReady { get; private set; }

        internal bool Owns(long item) => _owned.Contains(item);

        internal bool InWorld(MissionInfo mission) => mission != null && _slotMissions.Contains(mission.Code);

        internal bool IsPassed(MissionInfo mission) =>
            Save != null && mission != null && (Save.Passed.Contains(mission.Code, StringComparer.OrdinalIgnoreCase) || Save.IsDone(mission.Location));

        internal bool IsPlayable(MissionInfo mission) =>
            InWorld(mission) && Owns(mission.Item) && mission.PlayableWith(UnlockedMask);

        internal bool HasRadar => Save != null && (Save.RadarFromStart || Owns(ApData.CollectibleRadarItem));

        internal bool DeathLinkActive => Save != null && (Save.DeathLinkMode < 0 ? Save.DeathLink : Save.DeathLinkMode == 1);

        internal void SetDeathLinkMode(bool enabled)
        {
            if (Save == null) return;
            Save.DeathLinkMode = enabled == Save.DeathLink ? -1 : (enabled ? 1 : 0);
            _saveDirty = true;
            Client.SetDeathLink(DeathLinkActive);
            Log.Info($"DeathLink {(DeathLinkActive ? "on" : "off")} (set in game).");
            History.Write("DEATHLINK", "enabled", DeathLinkActive);
        }

        internal bool ChaosMode => Save != null && Save.GoalDone && Save.PostGoalChaos;

        internal string GoalCollectibleName
        {
            get
            {
                string id = Save?.GoalCollectible;
                if (string.IsNullOrEmpty(id)) return "the goal collectible";
                int split = id.LastIndexOf('_');
                CollectibleSet set = split > 0 ? ApData.CollectibleByKey(id.Substring(0, split)) : null;
                return set == null ? id : $"{set.Name} #{id.Substring(split + 1).TrimStart('0')}";
            }
        }

        internal MissionInfo GoalMissionInfo => Save == null ? null : ApData.MissionByCode(Save.GoalMission);

        internal IEnumerable<MissionInfo> WorldMissions => ApData.Missions.Where(InWorld);

        internal void Connect()
        {
            if (!Connection.IsComplete)
            {
                Notifier.Show("~y~Set the server and the slot name first.");
                return;
            }
            _wantOnline = true;
            _reconnectAt = -1;
            Client.Connect(Connection.Server, Connection.Slot, Connection.Password);
        }

        internal void Disconnect()
        {
            _wantOnline = false;
            _reconnectAt = -1;
            Client.Disconnect();
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                IsReady = true;
                Client.Process();
                if (!_started)
                {
                    _started = true;
                    if (Config.AutoConnect && Connection.IsComplete) Connect();
                }
                if (_wantOnline && Client.State == LinkState.Offline && _reconnectAt >= 0 && Game.GameTime >= _reconnectAt)
                {
                    _reconnectAt = -1;
                    Client.Connect(Connection.Server, Connection.Slot, Connection.Password);
                }
                Hook();
                if (Save != null)
                {
                    if (Client.IsConnected) PullItems();
                    RefreshOwned();
                    Announce();
                    WatchReplay();
                    ApplyGifts();
                    ApplyMissionGifts();
                    GiveMissionKit();
                    PollObjectives();
                    ApplyTraps();
                    if (Game.GameTime >= _nextSync)
                    {
                        _nextSync = Game.GameTime + SyncInterval;
                        ApplySlotOptions();
                        SyncServer();
                        SyncCollectibles();
                        SyncRoster();
                        SyncMarkers();
                        CheckGoal();
                        ShuffleChaos();
                    }
                    WatchDeath();
                    if (_saveDirty && Game.GameTime >= _nextSaveAt)
                    {
                        _saveDirty = false;
                        _nextSaveAt = Game.GameTime + 2000;
                        Store.Save(Save);
                    }
                }
                SubmarineDock.Process(Notifier);
                Notifier.Process();
            }
            catch (Exception ex)
            {
                int now = System.Environment.TickCount;
                if (_lastErrorAt < 0 || now - _lastErrorAt > 5000)
                {
                    _lastErrorAt = now;
                    Log.Error("Tick error.", ex);
                    History.Write("ERROR", "message", ex.GetBaseException().Message);
                }
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            try
            {
                if (Save != null) Store.Save(Save);
                MissionApi.ReplayGate = null;
                MissionApi.UseApiMarkers = false;
                SubmarineDock.Release();
                Client.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Error("Error while stopping.", ex);
            }
        }

        private void Hook()
        {
            if (!_missionsHooked && MissionApi.IsReady)
            {
                _missionsHooked = true;
                MissionApi.ReplayAttemptEnded += OnAttemptEnded;
                MissionApi.ReplayStarted += mission => History.Write("REPLAY", "code", mission?.Code);
                MissionApi.ReplayAttemptStarted += args =>
                {
                    _kitAt = Game.GameTime + 2500;
                    _kitMission = args?.Mission?.Code;
                    _objectivePollAt = Game.GameTime + 3000;
                    _objectivePollUntil = Game.GameTime + 120000;
                };
                MissionApi.ReplayGate = Gate;
                _markers.Clear();
                Log.Info("MissionLauncher linked.");
            }
            if (!_collectiblesHooked && CollectiblesApi.IsReady)
            {
                _collectiblesHooked = true;
                CollectiblesApi.Collected += OnCollected;
                TrapApi.TrapStarted += id => History.Write("TRAP_START", "id", id);
                TrapApi.TrapFinished += (id, interrupted) => History.Write("TRAP_END", "id", id, "interrupted", interrupted);
                Log.Info("GameCollectibles linked.");
            }
            if (_collectiblesHooked && !_collectiblesSynced && Save != null)
            {
                _collectiblesSynced = true;
                CollectiblesApi.SetCollected(Save.Collected);
                ApplySlotOptions();
            }
        }

        private void Activate(SlotSave save)
        {
            Save = save;
            History.Open(save.Key);
            History.Write("SESSION", "save", save.Key, "items", save.Items.Count, "checked", save.Checked.Count,
                "passed", save.Passed, "gold", save.Gold.Count, "objectives", save.Objectives.Count, "collected", save.Collected.Count,
                "versions", History.Versions());
            _slotMissions = new HashSet<string>(save.Missions, StringComparer.OrdinalIgnoreCase);
            _ownedCount = -1;
            _markers.Clear();
            _collectiblesSynced = false;
            MissionApi.UseApiMarkers = true;
            _serverCheckedCount = -1;
            _chaosMissions.Clear();
            ApplySlotOptions();
            RefreshOwned();
            StartCharacterHint();
        }

        private void ApplySlotOptions()
        {
            if (Save == null) return;
            if (TrapApi.IsReady) TrapApi.AllowInMissions = Save.MissionTraps;
            if (CollectiblesApi.IsReady)
            {
                var disabled = new List<CollectibleKind>();
                foreach (CollectibleSet set in ApData.Collectibles)
                {
                    if (Save.CollectibleSteps.TryGetValue(set.Key, out int step) && step > 0) continue;
                    if (Save.CollectibleSteps.Count == 0) continue;
                    if (!string.IsNullOrEmpty(Save.GoalCollectible) && Save.GoalCollectible.StartsWith(set.Key + "_", StringComparison.OrdinalIgnoreCase)) continue;
                    if (Enum.TryParse(set.Key, out CollectibleKind kind)) disabled.Add(kind);
                }
                CollectiblesApi.SetDisabledKinds(disabled);
            }
        }

        private void OnConnected()
        {
            string key = SlotSave.MakeKey(Client.Seed, Client.SlotName);
            SlotSave save = Save != null && Save.Key == key ? Save : Store.Load(key) ?? new SlotSave
            {
                Seed = Client.Seed,
                Slot = Client.SlotName,
            };
            save.Server = Connection.Server;
            ReadSlotData(save, Client.SlotData);
            save.Normalize();
            if (save != Save) Activate(save);
            else
            {
                _slotMissions = new HashSet<string>(save.Missions, StringComparer.OrdinalIgnoreCase);
                ApplySlotOptions();
            }
            Connection.LastSave = key;
            Connection.Save();
            PullItems();
            Client.SetDeathLink(DeathLinkActive);
            var all = new List<long>(Save.Checked);
            Client.SendChecks(all);
            _nextResend = Game.GameTime + ResendInterval;
            if (Save.GoalDone)
            {
                Client.SendGoal();
                DropTrapsAfterGoal();
            }
            Store.Save(Save);
            StartCharacterHint();
            History.Write("CONNECT", "server", Connection.Server, "slot", Client.SlotName, "seed", Client.Seed, "goal", Save.Goal,
                "goal_missions", Save.GoalMissions, "start", Save.StartCharacter, "missions", Save.Missions.Count,
                "checks_per_mission", Save.ChecksPerMission, "gold_checks", Save.GoldChecks, "objective_checks", Save.ObjectiveChecks,
                "steps", string.Join(",", Save.CollectibleSteps.Select(kv => kv.Key + ":" + kv.Value)), "deathlink", Save.DeathLink,
                "goal_mission", Save.GoalMission, "checks_per_collectible", Save.ChecksPerCollectible, "radar_start", Save.RadarFromStart,
                "vehicles", Save.GuaranteedVehicles, "mission_traps", Save.MissionTraps, "chaos", Save.PostGoalChaos,
                "server_checked", Client.CheckedLocations.Count, "pending", Save.Pending);
            Notifier.Post($"~g~Connected~s~ as {Notifier.Escape(Client.SlotName)}.", true);
            if (Save.Goal >= 3 && !Save.GoalDone)
                Notifier.Message(null, "Your goal", Notifier.Escape(GoalText()) + ".", true);
            Notifier.Remember($"Connected to {Connection.Server} as {Client.SlotName}.");
        }

        private void OnDisconnected(string reason)
        {
            History.Write("DISCONNECT", "reason", reason);
            Notifier.Post("~y~Archipelago: " + Notifier.Escape(reason), true);
            Notifier.Remember("Disconnected: " + reason);
            if (_wantOnline) _reconnectAt = Game.GameTime + ReconnectDelay;
        }

        private static void ReadSlotData(SlotSave save, Dictionary<string, object> data)
        {
            if (data == null) return;
            save.Goal = ReadInt(data, "goal", save.Goal);
            save.GoalMissions = ReadInt(data, "goal_missions", save.GoalMissions);
            save.StartCharacter = ReadString(data, "starting_character", save.StartCharacter);
            save.ChecksPerMission = ReadInt(data, "checks_per_mission", save.ChecksPerMission);
            save.GoldChecks = ReadInt(data, "gold_medal_checks", save.GoldChecks);
            save.ObjectiveChecks = ReadInt(data, "objective_checks", save.ObjectiveChecks);
            save.MoneyMultiplier = ReadInt(data, "money_multiplier", save.MoneyMultiplier);
            save.DeathLink = ReadInt(data, "death_link", save.DeathLink ? 1 : 0) != 0;
            save.GoalMission = ReadString(data, "goal_mission", save.GoalMission);
            save.GoalCollectible = ReadString(data, "goal_collectible", save.GoalCollectible);
            save.GoalLocation = ReadLong(data, "goal_location", save.GoalLocation);
            save.ChecksPerCollectible = ReadInt(data, "checks_per_collectible", save.ChecksPerCollectible);
            save.RadarFromStart = ReadString(data, "collectible_radar", save.RadarFromStart ? "start" : "item") != "item";
            save.GuaranteedVehicles = ReadInt(data, "guaranteed_vehicles", save.GuaranteedVehicles ? 1 : 0) != 0;
            save.MissionTraps = ReadInt(data, "mission_traps", save.MissionTraps ? 1 : 0) != 0;
            save.MissionWeapons = ReadInt(data, "mission_weapons", save.MissionWeapons ? 1 : 0) != 0;
            save.PostGoalChaos = ReadInt(data, "post_goal_chaos", save.PostGoalChaos ? 1 : 0) != 0;
            save.EstimatedMinutes = ReadInt(data, "estimated_minutes", save.EstimatedMinutes);
            save.EstimatedGoalMinutes = ReadInt(data, "estimated_goal_minutes", save.EstimatedGoalMinutes);
            if (data.TryGetValue("missions", out object missions) && missions is JArray list)
                save.Missions = list.Select(t => t.ToString()).ToList();
            if (data.TryGetValue("collectible_steps", out object steps) && steps is JObject table)
                save.CollectibleSteps = table.Properties().ToDictionary(p => p.Name, p => p.Value.Type == JTokenType.Integer ? (int)p.Value : 0, StringComparer.OrdinalIgnoreCase);
        }

        private static int ReadInt(Dictionary<string, object> data, string key, int fallback)
        {
            if (!data.TryGetValue(key, out object value) || value == null) return fallback;
            try
            {
                if (value is JToken token)
                {
                    if (token.Type == JTokenType.Boolean) return (bool)token ? 1 : 0;
                    return (int)token;
                }
                if (value is bool b) return b ? 1 : 0;
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static long ReadLong(Dictionary<string, object> data, string key, long fallback)
        {
            if (!data.TryGetValue(key, out object value) || value == null) return fallback;
            try
            {
                if (value is JToken token) return (long)token;
                return Convert.ToInt64(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static string ReadString(Dictionary<string, object> data, string key, string fallback) =>
            data.TryGetValue(key, out object value) && value != null ? value.ToString() : fallback;

        private void PullItems()
        {
            IReadOnlyList<ItemInfo> items = Client.ReceivedItems;
            if (items.Count <= Save.Items.Count) return;
            for (int i = Save.Items.Count; i < items.Count; i++)
            {
                ItemInfo item = items[i];
                int player = item.Player?.Slot ?? 0;
                var record = new ReceivedRecord
                {
                    Item = item.ItemId,
                    Location = item.LocationId,
                    Player = player,
                    Sender = Client.PlayerName(player),
                    LocationName = SafeLocationName(item),
                    ReceivedAt = DateTime.Now.ToString("MM-dd HH:mm"),
                };
                Save.Items.Add(record);
                History.Write("ITEM", "index", i, "id", record.Item, "name", ApData.ItemName(record.Item), "from", record.Sender,
                    "player", record.Player, "location", record.Location);
            }
            _saveDirty = true;
        }

        private static string SafeLocationName(ItemInfo item)
        {
            try
            {
                return item.LocationDisplayName;
            }
            catch
            {
                return null;
            }
        }

        private void RefreshOwned()
        {
            if (Save == null || _ownedCount == Save.Items.Count) return;
            _ownedCount = Save.Items.Count;
            _owned = new HashSet<long>(Save.Items.Select(i => i.Item));
            int mask = 0;
            for (int c = 0; c < ApData.CharacterItems.Length; c++)
                if (_owned.Contains(ApData.CharacterItems[c])) mask |= 1 << c;
            UnlockedMask = mask;
            _nextSync = 0;
        }

        private bool CanAnnounce()
        {
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) || !GTA.UI.Screen.IsFadedIn) return false;
            if (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS) || Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING)) return false;
            if (MissionApi.IsReady && MissionApi.IsReplayActive)
            {
                if (!Function.Call<bool>(Hash.GET_MISSION_FLAG)) return false;
                if (_lastCheckAt >= 0 && Game.GameTime - _lastCheckAt < 120000) return false;
            }
            return true;
        }

        private void Announce()
        {
            if (Save.NotifiedIndex >= Save.Items.Count) return;
            if (!CanAnnounce())
            {
                if (_announceCalmSince >= 0) _announceCalmSince = -1;
                return;
            }
            if (_announceCalmSince < 0) _announceCalmSince = Game.GameTime;
            if (Game.GameTime - _announceCalmSince < 2000) return;
            for (int i = Save.NotifiedIndex; i < Save.Items.Count; i++)
            {
                ReceivedRecord record = Save.Items[i];
                string text = Describe(record, out string precise, out string sender);
                Notifier.Remember(precise);
                if (text != null && Config.ShowReceived) Notifier.Message(sender, "Archipelago", text);
            }
            Save.NotifiedIndex = Save.Items.Count;
            _saveDirty = true;
        }

        private string Describe(ReceivedRecord record, out string precise, out string sender)
        {
            bool own = record.Player == Client.SlotNumber && Client.SlotNumber != 0;
            bool start = record.Player == 0;
            string slot = string.IsNullOrEmpty(record.Sender) ? "Archipelago" : record.Sender;
            sender = start ? "Archipelago" : slot;
            string name = ApData.TrapByItem(record.Item) != null ? "a trap" : ApData.ItemName(record.Item);
            precise = start ? $"Starting item: {name}" : own ? $"You found {name} ({slot})" : $"{slot} sent you {name}";
            if (ApData.TrapByItem(record.Item) != null) return "~r~A trap!~s~ Watch your back.";
            MissionInfo mission = ApData.MissionByItem(record.Item);
            if (mission != null) return $"~g~New mission:~s~ {Notifier.Escape(mission.Name)}";
            int character = ApData.CharacterByItem(record.Item);
            if (character >= 0) return $"~g~{ApData.CharacterNames[character]}~s~ is now playable.";
            if (record.Item == ApData.CollectibleRadarItem) return "~g~Collectible Radar:~s~ F6 > Collectibles > Route to the nearest one.";
            if (record.Item == ApData.DriveBySmgItem) return "~g~Drive-By SMG:~s~ a Micro SMG with 600 rounds for every character.";
            if (ResolveGift(record))
            {
                precise += ": " + record.GiftName;
                string where = record.Gift.StartsWith("water:", StringComparison.Ordinal) ? " (F6 > Boats)"
                    : record.Item == ApData.WeaponGiftItem ? string.Empty : " (F6 > Vehicles)";
                return $"~g~{Notifier.Escape(name)}:~s~ {Notifier.Escape(record.GiftName)}{where}";
            }
            MoneyInfo money = ApData.MoneyByItemId(record.Item);
            if (money != null)
            {
                int amount = (int)Math.Min(int.MaxValue, (long)money.Amount * Save.MoneyMultiplier / 100);
                return $"~g~${amount:#,0}~s~ added to your account.";
            }
            return $"~g~{Notifier.Escape(name)}~s~";
        }

        private bool ResolveGift(ReceivedRecord record)
        {
            VehicleItem category = ApData.VehicleByItem(record.Item);
            bool vehicle = record.Item == ApData.VehicleGiftItem || category != null;
            bool weapon = record.Item == ApData.WeaponGiftItem;
            if (!vehicle && !weapon) return false;
            if (record.Gift != null) return true;
            int index = Save.Items.IndexOf(record);
            string seed = Save.Seed + "_" + Save.Slot;
            if (vehicle)
            {
                VehicleGift gift = category != null ? Gifts.PickCategory(category.Category, seed, index) : Gifts.PickVehicle(seed, index);
                record.Gift = (gift.Water ? "water:" : string.Empty) + gift.Model;
                record.GiftName = gift.Name;
            }
            else
            {
                WeaponGift gift = Gifts.PickWeapon(seed, index);
                record.Gift = gift.Weapon + ":" + Math.Max(0, gift.Ammo);
                record.GiftName = gift.Name;
            }
            _saveDirty = true;
            History.Write("GIFT_PICK", "index", index, "item", ApData.ItemName(record.Item), "gift", record.Gift, "name", record.GiftName);
            return true;
        }

        internal string BoatName(string model)
        {
            VehicleGift gift = Gifts.Vehicles.FirstOrDefault(v => string.Equals(v.Model, model, StringComparison.OrdinalIgnoreCase));
            return gift?.Name ?? model;
        }

        private bool EnvironmentLive => EnvironmentApi.IsReady && EnvironmentApi.IsSterile && !EnvironmentApi.IsReplayActive;

        private void ApplyGifts()
        {
            if (!EnvironmentLive || Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            if (!string.Equals(EnvironmentApi.Owner, Save.Key, StringComparison.Ordinal))
            {
                string previous = EnvironmentApi.Owner;
                if (EnvironmentApi.ResetProgress(Save.Key, out string resetError))
                {
                    History.Write("NEW_GAME", "previous", previous, "now", Save.Key);
                    if (previous != null)
                        Notifier.Message(null, "New Archipelago game", "The money, weapons and vehicles of your previous game were cleared.", true);
                }
                else Log.Warn("Could not start a fresh game: " + resetError);
                return;
            }
            string gameId = EnvironmentApi.GameId;
            if (gameId != null && Save.GameId != gameId)
            {
                History.Write("GAME", "old", Save.GameId, "new", gameId);
                if (Save.GameId != null)
                {
                    Save.GiftIndex = 0;
                    Notifier.Post("~y~New game detected~s~: your money, weapons and vehicles are given again.", true);
                    Log.Info($"Game changed ({Save.GameId} -> {gameId}), gifts re-applied.");
                }
                Save.GameId = gameId;
                _saveDirty = true;
            }
            GiveStarterWeapon(gameId);
            if (Save.GiftIndex >= Save.Items.Count) return;
            int budget = 5;
            while (Save.GiftIndex < Save.Items.Count && budget-- > 0)
            {
                ReceivedRecord record = Save.Items[Save.GiftIndex];
                string result = Gift(record);
                if (result != "skip") History.Write("GIFT", "index", Save.GiftIndex, "name", ApData.ItemName(record.Item), "result", result);
                Save.GiftIndex++;
                _saveDirty = true;
            }
        }

        private void GiveStarterWeapon(string gameId)
        {
            if (gameId == null || (Save.StarterGameId == gameId && Save.StarterRevision >= StarterRevision)) return;
            List<string> choices = Config.StarterWeapons.Where(w => Function.Call<bool>(Hash.IS_WEAPON_VALID, StringHash.AtStringHash(w, 0u))).ToList();
            if (choices.Count == 0) choices.Add("WEAPON_PISTOL50");
            if (string.IsNullOrEmpty(Save.StarterWeapon) || !choices.Contains(Save.StarterWeapon, StringComparer.OrdinalIgnoreCase))
                Save.StarterWeapon = choices[new Random(StableSeed(Save.Seed + "_" + Save.Slot)).Next(choices.Count)];
            var hash = (WeaponHash)StringHash.AtStringHash(Save.StarterWeapon, 0u);
            var errors = new List<string>();
            foreach (PlayableCharacter character in new[] { PlayableCharacter.Michael, PlayableCharacter.Franklin, PlayableCharacter.Trevor })
                if (!EnvironmentApi.GiveWeaponTo(character, hash, Config.StarterAmmo, out string error)) errors.Add(error);
            Save.StarterGameId = gameId;
            Save.StarterRevision = StarterRevision;
            _saveDirty = true;
            string name = Gifts.Weapons.FirstOrDefault(w => string.Equals(w.Weapon, Save.StarterWeapon, StringComparison.OrdinalIgnoreCase))?.Name ?? Save.StarterWeapon;
            History.Write("STARTER", "weapon", Save.StarterWeapon, "ammo", Config.StarterAmmo, "errors", errors);
            Notifier.Message(null, "Starter weapon", $"~g~{Notifier.Escape(name)}~s~ with {Config.StarterAmmo} rounds, for every character.");
        }

        private static int StableSeed(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        private string Gift(ReceivedRecord record)
        {
            string error = null;
            bool gift = false;
            if (record.Item == ApData.DriveBySmgItem)
            {
                gift = true;
                var smg = (WeaponHash)StringHash.AtStringHash("WEAPON_MICROSMG", 0u);
                foreach (PlayableCharacter character in new[] { PlayableCharacter.Michael, PlayableCharacter.Franklin, PlayableCharacter.Trevor })
                    if (!EnvironmentApi.GiveWeaponTo(character, smg, 600, out string failed)) error = failed;
            }
            MoneyInfo money = ApData.MoneyByItemId(record.Item);
            if (money != null)
            {
                gift = true;
                int amount = (int)Math.Min(int.MaxValue, (long)money.Amount * Save.MoneyMultiplier / 100);
                EnvironmentApi.GiveMoney(amount, out error);
            }
            if (ResolveGift(record))
            {
                gift = true;
                if (record.Item == ApData.WeaponGiftItem)
                {
                    string[] parts = record.Gift.Split(':');
                    int ammo = parts.Length > 1 && int.TryParse(parts[1], out int value) ? value : 100;
                    if (string.Equals(parts[0], "ARMOR", StringComparison.OrdinalIgnoreCase))
                    {
                        Ped ped = Game.Player.Character;
                        if (ped != null && ped.Exists()) ped.Armor = 100;
                    }
                    else
                    {
                        var hash = (WeaponHash)StringHash.AtStringHash(parts[0], 0u);
                        foreach (PlayableCharacter character in new[] { PlayableCharacter.Michael, PlayableCharacter.Franklin, PlayableCharacter.Trevor })
                            if (!EnvironmentApi.GiveWeaponTo(character, hash, ammo, out string failed)) error = failed;
                    }
                }
                else if (record.Gift.StartsWith("water:", StringComparison.Ordinal))
                {
                    string model = record.Gift.Substring(6);
                    if (!Save.Boats.Contains(model, StringComparer.OrdinalIgnoreCase)) Save.Boats.Add(model);
                }
                else
                {
                    EnvironmentApi.GrantVehicle(record.Gift, out error);
                }
            }
            if (error != null) Log.Warn($"Could not give {ApData.ItemName(record.Item)}: {error}");
            return !gift ? "skip" : error ?? "ok";
        }

        private void WatchReplay()
        {
            bool replay = EnvironmentApi.IsReady && EnvironmentApi.IsReplayActive;
            if (replay && !_wasReplay)
            {
                _missionGiftFrom = Save.Items.Count;
                History.Write("REPLAY_ON", "items", Save.Items.Count, "gift_index", Save.GiftIndex);
            }
            else if (!replay && _wasReplay)
            {
                _missionGiftFrom = -1;
                _kitAt = -1;
                TakeMissionKit();
                History.Write("REPLAY_OFF", "items", Save.Items.Count, "gift_index", Save.GiftIndex);
            }
            _wasReplay = replay;
        }

        private void ApplyMissionGifts()
        {
            if (_missionGiftFrom < 0 || _missionGiftFrom >= Save.Items.Count) return;
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) || !GTA.UI.Screen.IsFadedIn) return;
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead) return;
            ReceivedRecord record = Save.Items[_missionGiftFrom];
            _missionGiftFrom++;
            if (!ResolveGift(record)) return;
            string result;
            if (record.Item == ApData.WeaponGiftItem)
            {
                string[] parts = record.Gift.Split(':');
                int ammo = parts.Length > 1 && int.TryParse(parts[1], out int value) ? value : 100;
                if (string.Equals(parts[0], "ARMOR", StringComparison.OrdinalIgnoreCase)) ped.Armor = 100;
                else
                {
                    uint hash = StringHash.AtStringHash(parts[0], 0u);
                    if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false))
                        Function.Call(Hash.ADD_AMMO_TO_PED, ped, hash, Math.Max(ammo, 1));
                    else
                        Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, hash, Math.Max(ammo, 1), false, true);
                }
                result = "weapon";
            }
            else if (record.Gift.StartsWith("water:", StringComparison.Ordinal))
            {
                result = "boat later";
            }
            else
            {
                result = "garage later";
            }
            History.Write("MISSION_GIFT", "index", _missionGiftFrom - 1, "gift", record.Gift, "result", result);
        }

        private void GiveMissionKit()
        {
            if (_kitAt < 0 || Game.GameTime < _kitAt) return;
            if (!Save.MissionWeapons || !MissionApi.IsReady || !MissionApi.IsReplayActive)
            {
                _kitAt = -1;
                return;
            }
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead || !GTA.UI.Screen.IsFadedIn) return;
            _kitAt = -1;
            var entries = new List<string>(Config.MissionKit);
            if (_kitMission != null && Config.MissionExtras.TryGetValue(_kitMission, out List<string> extras)) entries.AddRange(extras);
            var given = new List<string>();
            foreach (string entry in entries)
            {
                string[] parts = entry.Split(':');
                uint hash = StringHash.AtStringHash(parts[0].Trim(), 0u);
                int ammo = parts.Length > 1 && int.TryParse(parts[1], out int value) ? value : 100;
                if (!Function.Call<bool>(Hash.IS_WEAPON_VALID, hash)) continue;
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false))
                {
                    if (Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, ped, hash) < ammo) Function.Call(Hash.SET_PED_AMMO, ped, hash, ammo, false);
                }
                else
                {
                    Function.Call(Hash.GIVE_WEAPON_TO_PED, ped, hash, ammo, false, false);
                    _kitGiven.Add(hash);
                }
                given.Add(parts[0].Trim());
            }
            History.Write("MISSION_KIT", "code", _kitMission, "weapons", given);
        }

        private void TakeMissionKit()
        {
            if (_kitGiven.Count == 0) return;
            Ped ped = Game.Player.Character;
            PlayableCharacter? current = EnvironmentApi.CurrentCharacter;
            var owned = new HashSet<uint>(current.HasValue
                ? EnvironmentApi.GetWeapons(current.Value).Select(w => unchecked((uint)w))
                : Enumerable.Empty<uint>());
            if (ped != null && ped.Exists())
                foreach (uint hash in _kitGiven)
                    if (!owned.Contains(hash) && Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, hash, false))
                        Function.Call(Hash.REMOVE_WEAPON_FROM_PED, ped, hash);
            History.Write("MISSION_KIT_BACK", "count", _kitGiven.Count);
            _kitGiven.Clear();
        }

        private void ApplyTraps()
        {
            if (!_goalQueueChecked && Save.GoalDone && TrapApi.IsReady)
            {
                _goalQueueChecked = true;
                DropTrapsAfterGoal();
            }
            if (Save.TrapIndex >= Save.Items.Count) return;
            if (!TrapApi.IsReady || !EnvironmentApi.IsReady || !EnvironmentApi.IsSterile) return;
            int skipped = 0;
            while (Save.TrapIndex < Save.Items.Count)
            {
                ReceivedRecord record = Save.Items[Save.TrapIndex];
                Save.TrapIndex++;
                _saveDirty = true;
                TrapInfo trap = ApData.TrapByItem(record.Item);
                if (trap == null) continue;
                bool own = record.Player == Client.SlotNumber;
                string source = own ? "yourself" : record.Sender;
                string status;
                if (Save.GoalDone && !Config.TrapsAfterGoal) status = "Skipped: goal complete";
                else if (Config.TrapQueueLimit > 0 && TrapApi.QueuedTraps.Count >= Config.TrapQueueLimit) status = "Skipped: too many traps waiting";
                else status = TrapApi.Trigger(trap.Id, source, out string error) ? "Queued" : "Skipped: " + error;
                if (status != "Queued") skipped++;
                Save.AddTrap(new TrapRecord
                {
                    Id = trap.Id,
                    Sender = own ? "You" : record.Sender,
                    ReceivedAt = DateTime.Now.ToString("HH:mm"),
                    Status = status,
                }, TrapLogSize);
                Log.Info($"Trap {trap.Id} from {record.Sender}: {status}.");
                History.Write("TRAP", "index", Save.TrapIndex - 1, "id", trap.Id, "from", record.Sender, "status", status);
            }
            if (skipped > 1) Notifier.Message(null, "Traps", $"{skipped} traps skipped " +
                (Save.GoalDone && !Config.TrapsAfterGoal ? "because your goal is complete." : "because too many were already waiting."));
        }

        private bool _goalQueueChecked;

        private void DropTrapsAfterGoal()
        {
            if (!Save.GoalDone || Config.TrapsAfterGoal || !TrapApi.IsReady) return;
            int waiting = TrapApi.QueuedTraps.Count;
            if (waiting == 0) return;
            TrapApi.ClearQueue();
            Log.Info($"Goal complete: {waiting} waiting traps dropped.");
            History.Write("TRAP_CLEAR", "count", waiting, "reason", "goal");
        }

        internal string TrapStatus(TrapRecord record)
        {
            if (record.Status != "Queued") return record.Status;
            if (TrapApi.ActiveTraps.Contains(record.Id)) return "Running";
            if (TrapApi.QueuedTraps.Contains(record.Id)) return "Waiting";
            return "Played";
        }

        private void SyncServer()
        {
            if (!Client.IsConnected) return;
            IReadOnlyList<long> server = Client.CheckedLocations;
            if (server.Count != Save.ServerChecked.Count)
            {
                Save.ServerChecked = server.ToList();
                _saveDirty = true;
            }
            if (Save.Pending.Count > 0)
            {
                var known = new HashSet<long>(server);
                int removed = Save.Pending.RemoveAll(known.Contains);
                if (removed > 0) _saveDirty = true;
                if (Save.Pending.Count > 0 && Game.GameTime >= _nextResend)
                {
                    _nextResend = Game.GameTime + ResendInterval;
                    Client.SendChecks(Save.Pending);
                }
            }
        }

        internal void Check(IEnumerable<long> ids)
        {
            if (Save == null) return;
            var fresh = ids.Where(id => !Save.Checked.Contains(id)).Distinct().ToList();
            if (fresh.Count == 0) return;
            Save.Checked.AddRange(fresh);
            Save.Pending.AddRange(fresh);
            _lastCheckAt = Game.GameTime;
            Store.Save(Save);
            Client.SendChecks(fresh);
            _nextResend = Game.GameTime + ResendInterval;
            _nextSync = 0;
            Log.Info("Checks: " + string.Join(", ", fresh));
            if (fresh.Count <= 3)
                foreach (long id in fresh) Notifier.Post("~g~Check:~s~ " + Notifier.Escape(ApData.LocationName(id)) + (Client.IsConnected ? string.Empty : " ~c~(sent when online)"));
            else
                Notifier.Post($"~g~{fresh.Count} checks~s~ sent" + (Client.IsConnected ? "." : " when online."));
            History.Write("CHECK", "ids", fresh, "names", fresh.Select(ApData.LocationName).ToList(), "connected", Client.IsConnected);
        }

        private string Gate(ReplayMission mission)
        {
            if (Save == null) return "Connect to Archipelago first (" + Config.MenuKey + ").";
            if (EnvironmentApi.Mode == EnvironmentMode.StorySave) return "Load your Archipelago game first (start a New Game).";
            MissionInfo info = ApData.MissionByCode(mission.Code);
            if (!InWorld(info)) return "This mission is not part of your Archipelago world.";
            if (!Owns(info.Item)) return $"Locked: you need \"Mission: {info.Name}\".";
            string missing = MissingCharacters(info);
            if (missing != null) return "Locked: " + missing;
            return null;
        }

        internal string MissingCharacters(MissionInfo info)
        {
            if ((info.Triggers & UnlockedMask) == 0) return "you need " + TriggerNames(info) + ".";
            var absent = new List<string>();
            for (int c = 0; c < 3; c++)
                if (info.Needs(c) && (UnlockedMask & (1 << c)) == 0) absent.Add(ApData.CharacterNames[c]);
            if (absent.Count == 0) return null;
            return $"this mission is played with {PlayerNames(info)}, you still need {string.Join(" and ", absent)}.";
        }

        internal static string PlayerNames(MissionInfo info)
        {
            var names = new List<string>();
            for (int c = 0; c < 3; c++)
                if (info.Needs(c)) names.Add(ApData.CharacterNames[c]);
            if (names.Count <= 1) return names.FirstOrDefault() ?? string.Empty;
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names.Last();
        }

        internal static string TriggerNames(MissionInfo info)
        {
            var names = new List<string>();
            for (int c = 0; c < 3; c++)
                if (info.TriggeredBy(c)) names.Add(ApData.CharacterNames[c]);
            return string.Join(" or ", names);
        }

        private void OnAttemptEnded(ReplayResultEventArgs args)
        {
            if (Save == null || args?.Mission == null) return;
            History.Write("MISSION", "code", args.Mission.Code, "attempt", args.Attempt, "outcome", args.Outcome.ToString(),
                "medal", args.Medal.ToString(), "percent", args.Percent, "time", (int)args.Duration.TotalSeconds,
                "objectives", args.Objectives.Select(o => $"{o.Position}:{(o.Passed && !o.Invalidated ? "ok" : "x")}").ToList(),
                "fail", args.FailReasonText);
            if (args.Outcome != ReplayOutcome.Passed) return;
            MissionInfo info = ApData.MissionByCode(args.Mission.Code);
            if (!InWorld(info)) return;
            if (!Save.Passed.Contains(info.Code, StringComparer.OrdinalIgnoreCase)) Save.Passed.Add(info.Code);
            if (args.Gold && !Save.Gold.Contains(info.Code, StringComparer.OrdinalIgnoreCase)) Save.Gold.Add(info.Code);
            if (args.Objectives.Count > 0) Save.ObjectiveTotals[info.Code] = args.Objectives.Count;
            foreach (MissionObjective objective in args.Objectives) RememberObjective(info.Code, objective);
            foreach (MissionObjective objective in args.Objectives)
            {
                if (!objective.Passed || objective.Invalidated) continue;
                string key = $"{info.Code}#{objective.Position}";
                if (!Save.Objectives.Contains(key)) Save.Objectives.Add(key);
            }
            var ids = new List<long> { info.Location };
            for (int b = 0; b < Math.Min(Save.ChecksPerMission - 1, info.Bonus.Length); b++) ids.Add(info.Bonus[b]);
            for (int n = 1; n <= Math.Min(Save.Gold.Count, Save.GoldChecks); n++) ids.Add(ApData.GoldBase + n);
            for (int n = 1; n <= Math.Min(Save.Objectives.Count, Save.ObjectiveChecks); n++) ids.Add(ApData.ObjectiveBase + n);
            Log.Info($"Mission passed: {info.Code} (gold {args.Gold}, {Save.Gold.Count} golds, {Save.Objectives.Count} objectives).");
            Check(ids);
        }

        private void PollObjectives()
        {
            if (_objectivePollAt < 0 || Game.GameTime < _objectivePollAt) return;
            if (Game.GameTime > _objectivePollUntil || !MissionApi.IsReady || !MissionApi.IsReplayActive)
            {
                _objectivePollAt = -1;
                return;
            }
            _objectivePollAt = Game.GameTime + 2000;
            ReplayMission replay = MissionApi.CurrentReplay;
            MissionInfo info = replay == null ? null : ApData.MissionByCode(replay.Code);
            if (info == null) return;
            IReadOnlyList<MissionObjective> objectives = MissionApi.CurrentObjectives;
            if (objectives.Count == 0) return;
            _objectivePollAt = -1;
            Save.ObjectiveTotals[info.Code] = objectives.Count;
            foreach (MissionObjective objective in objectives) RememberObjective(info.Code, objective);
            _saveDirty = true;
            var left = objectives.Where(o => !Save.Objectives.Contains($"{info.Code}#{o.Position}")).Select(o => Save.ObjectiveNames[$"{info.Code}#{o.Position}"]).ToList();
            bool gold = Save.Gold.Contains(info.Code, StringComparer.OrdinalIgnoreCase);
            History.Write("OBJECTIVES", "code", info.Code, "left", left);
            if (left.Count == 0 && gold)
            {
                string text = "Gold medal and every objective already done here.";
                Notifier.Message(null, "Gold objectives", text);
                Notifier.Remember($"{info.Name}: {text}");
                return;
            }
            List<string> shown = gold
                ? left
                : objectives.Select(o => Save.ObjectiveNames[$"{info.Code}#{o.Position}"] +
                    (Save.Objectives.Contains($"{info.Code}#{o.Position}") ? " ~c~(done once)~s~" : string.Empty)).ToList();
            for (int i = 0; i < shown.Count; i++)
                Notifier.Message(null, $"Gold objective {i + 1}/{shown.Count}", shown[i]);
            Notifier.Remember($"{info.Name}, objectives: " + Notifier.Clean(string.Join("; ", shown)));
        }

        private void RememberObjective(string code, MissionObjective objective)
        {
            string text = DescribeObjective(objective);
            if (!string.IsNullOrWhiteSpace(text)) Save.ObjectiveNames[$"{code}#{objective.Position}"] = text;
        }

        internal static string DescribeObjective(MissionObjective objective)
        {
            string text = string.IsNullOrWhiteSpace(objective.Text) ? "Objective " + objective.Position : objective.Text.Trim();
            switch (objective.StatType)
            {
                case 1:
                case 2:
                case 4:
                case 5:
                case 17:
                case 9:
                    if (objective.Target <= 0) return text;
                    return $"{text} ({(objective.LowerIsBetter ? "under" : "at least")} {objective.FormatValue(objective.Target)})";
                default:
                    return objective.Target > 1 ? $"{text} ({(objective.LowerIsBetter ? "under" : "at least")} {objective.Target})" : text;
            }
        }

        private void OnCollected(string id)
        {
            if (Save == null || EnvironmentApi.Mode == EnvironmentMode.StorySave)
            {
                CollectiblesApi.Restore(id);
                Notifier.Show(Save == null
                    ? "~y~Connect to Archipelago first: this item will come back."
                    : "~y~Load your Archipelago game first: this item will come back.");
                return;
            }
            if (!Save.Collected.Contains(id, StringComparer.OrdinalIgnoreCase)) Save.Collected.Add(id);
            History.Write("COLLECT", "id", id, "total", Save.Collected.Count);
            _saveDirty = true;
            CollectibleSet set = ApData.Collectibles.FirstOrDefault(c => id.StartsWith(c.Key + "_", StringComparison.OrdinalIgnoreCase));
            if (set != null)
            {
                int count = CollectedCount(set);
                int step = Save.CollectibleSteps.TryGetValue(set.Key, out int value) ? value : 0;
                int next = step > 0 ? Milestones(set, step).FirstOrDefault(m => m > count) : 0;
                int worth = Math.Max(1, Save.ChecksPerCollectible);
                string progress;
                if (step <= 0) progress = "not a check in this world";
                else if (Milestones(set, step).Contains(count)) progress = worth > 1 ? $"{worth} checks!" : "check!";
                else if (next > 0) progress = $"next check at {next}";
                else progress = "all checks done";
                Notifier.Post($"~y~{set.Name}~s~ {count}/{set.Total}: {progress}");
            }
            SyncCollectibles();
        }

        internal IEnumerable<long> CollectibleLocations(CollectibleSet set, int milestone)
        {
            yield return set.LocationFor(milestone);
            for (int b = 1; b < Math.Min(Save.ChecksPerCollectible, ApData.MaxCollectibleBonus + 1); b++)
                yield return set.LocationFor(milestone, b);
        }

        private void SyncCollectibles()
        {
            var ids = new List<long>();
            bool released = _serverCheckedCount != Save.ServerChecked.Count;
            _serverCheckedCount = Save.ServerChecked.Count;
            foreach (CollectibleSet set in ApData.Collectibles)
            {
                if (!Save.CollectibleSteps.TryGetValue(set.Key, out int step) || step <= 0) continue;
                int count = CollectedCount(set);
                bool allDone = true;
                foreach (int milestone in Milestones(set, step))
                {
                    foreach (long location in CollectibleLocations(set, milestone))
                    {
                        if (milestone <= count) ids.Add(location);
                        else if (!Save.IsDone(location)) allDone = false;
                    }
                }
                if (released && allDone && count < set.Total) MarkAllCollected(set);
            }
            if (ids.Count > 0) Check(ids);
        }

        private void MarkAllCollected(CollectibleSet set)
        {
            if (!CollectiblesApi.IsReady) return;
            int added = 0;
            foreach (CollectibleInfo info in CollectiblesApi.All)
            {
                if (!string.Equals(info.Kind.ToString(), set.Key, StringComparison.OrdinalIgnoreCase)) continue;
                if (Save.Collected.Contains(info.Id, StringComparer.OrdinalIgnoreCase)) continue;
                Save.Collected.Add(info.Id);
                added++;
            }
            if (added == 0) return;
            CollectiblesApi.SetCollected(Save.Collected);
            _saveDirty = true;
            History.Write("COLLECT_ALL", "kind", set.Key, "added", added);
            Notifier.Post($"~y~{set.Name}~s~: every check is done, the ones left were removed from the map.");
        }

        internal int CollectedCount(CollectibleSet set) =>
            Save?.Collected.Count(id => id.StartsWith(set.Key + "_", StringComparison.OrdinalIgnoreCase)) ?? 0;

        internal static IEnumerable<int> Milestones(CollectibleSet set, int step)
        {
            int last = 0;
            for (int m = step; m <= set.Total; m += step)
            {
                last = m;
                yield return m;
            }
            if (last != set.Total) yield return set.Total;
        }

        private void SyncRoster()
        {
            if (!EnvironmentLive || UnlockedMask == 0) return;
            var wanted = new List<PlayableCharacter>();
            for (int c = 0; c < 3; c++)
                if ((UnlockedMask & (1 << c)) != 0) wanted.Add((PlayableCharacter)c);
            IReadOnlyList<PlayableCharacter> current = EnvironmentApi.UnlockedCharacters;
            if (current.Count != wanted.Count || wanted.Any(c => !current.Contains(c)))
            {
                History.Write("ROSTER", "wanted", wanted.Select(c => c.ToString()).ToList(), "was", current.Select(c => c.ToString()).ToList());
                if (!EnvironmentApi.SetUnlockedCharacters(wanted, out string error)) Log.Warn("Character sync failed: " + error);
                else Log.Info("Characters synced: " + string.Join(", ", wanted));
            }
            PlayableCharacter? playing = EnvironmentApi.CurrentCharacter;
            if (playing.HasValue && (UnlockedMask & (1 << (int)playing.Value)) == 0 && Game.GameTime >= _nextSwitchTry)
            {
                _nextSwitchTry = Game.GameTime + 3000;
                PlayableCharacter target = wanted[0];
                int start = Array.FindIndex(ApData.CharacterNames, n => string.Equals(n, Save.StartCharacter, StringComparison.OrdinalIgnoreCase));
                if (start >= 0 && wanted.Contains((PlayableCharacter)start)) target = (PlayableCharacter)start;
                if (EnvironmentApi.SwitchCharacter(target, out string error))
                {
                    History.Write("SWITCH", "from", playing.Value.ToString(), "to", target.ToString());
                    Notifier.Message(null, "Characters", $"{playing.Value} is locked in your game: you now play ~g~{target}~s~.");
                }
                else if (Game.GameTime >= _nextCharacterHint)
                {
                    _nextCharacterHint = Game.GameTime + 120000;
                    Notifier.Post($"~y~{playing.Value} is locked in your Archipelago game~s~: {Notifier.Escape(error)} You will be switched to {target}.");
                }
            }
        }

        private void StartCharacterHint()
        {
            if (Save == null || !EnvironmentApi.IsReady) return;
            int index = Array.FindIndex(ApData.CharacterNames, n => string.Equals(n, Save.StartCharacter, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) EnvironmentApi.SetNextStartCharacter((PlayableCharacter)index);
        }

        private void SyncMarkers()
        {
            if (!MissionApi.IsReady) return;
            MissionApi.UseApiMarkers = true;
            bool story = EnvironmentApi.Mode == EnvironmentMode.StorySave;
            int available = 0;
            bool medalsLeft = Enumerable.Range(1, Math.Max(0, Save.GoldChecks)).Any(n => !Save.IsDone(ApData.GoldBase + n))
                || Enumerable.Range(1, Math.Max(0, Save.ObjectiveChecks)).Any(n => !Save.IsDone(ApData.ObjectiveBase + n));
            foreach (ReplayMission mission in MissionApi.Missions)
            {
                MissionInfo info = ApData.MissionByCode(mission.Code);
                MissionMarkerState state = MissionMarkerState.Hidden;
                if (!story && IsPlayable(info) && (!IsPassed(info) || (ChaosMode && _chaosMissions.Contains(info.Code))))
                    state = MissionMarkerState.Available;
                else if (!story && medalsLeft && IsPlayable(info) && IsPassed(info) && !Save.Gold.Contains(info.Code, StringComparer.OrdinalIgnoreCase))
                    state = MissionMarkerState.Replay;
                if (state == MissionMarkerState.Available) available++;
                _markers[mission.Number] = state;
                if (MissionApi.GetMissionMarker(mission.Number) != state) MissionApi.SetMissionMarker(mission.Number, state);
            }
            string snapshot = $"{available}|{EnvironmentApi.Mode}|{MissionApi.IsReplayActive}";
            if (snapshot != _markerSnapshot)
            {
                bool wasOpen = _markerSnapshot == null || !_markerSnapshot.StartsWith("0|", StringComparison.Ordinal);
                if (available == 0 && wasOpen && !story && !MissionApi.IsReplayActive && Save.Items.Count > 0 && !Save.GoalDone)
                    Notifier.Message(null, "No new mission",
                        "Every mission you own is done. Pick up collectibles, replay a mission for its gold medal (tick on the map, F6 > Replay for gold) or wait for items.", true);
                _markerSnapshot = snapshot;
                History.Write("MARKERS", "available", available, "mode", EnvironmentApi.Mode.ToString(), "replay", MissionApi.IsReplayActive);
            }
        }

        internal int PassedCount => WorldMissions.Count(IsPassed);

        private void CheckGoal()
        {
            if (Save.GoalDone) return;
            bool reached;
            switch (Save.Goal)
            {
                case 0:
                    reached = new[] { "FINA", "FINB", "FINC1" }.Any(code => IsPassed(ApData.MissionByCode(code)));
                    break;
                case 1:
                    reached = IsPassed(ApData.MissionByCode("FINC1"));
                    break;
                case 3:
                    reached = GoalMissionInfo != null && IsPassed(GoalMissionInfo);
                    break;
                case 4:
                    reached = !string.IsNullOrEmpty(Save.GoalCollectible) && Save.Collected.Contains(Save.GoalCollectible, StringComparer.OrdinalIgnoreCase);
                    break;
                case 5:
                    reached = Save.GoalLocation > 0 && Save.Checked.Contains(Save.GoalLocation);
                    break;
                default:
                    reached = Save.GoalMissions > 0 && PassedCount >= Save.GoalMissions;
                    break;
            }
            if (!reached) return;
            Save.GoalDone = true;
            _saveDirty = true;
            Client.SendGoal();
            Log.Info("Goal completed.");
            History.Write("GOAL", "passed", PassedCount);
            Notifier.Message(null, "Goal complete!", "Your Archipelago game is finished." +
                (Save.PostGoalChaos ? " Chaos mode: missions come back at random and the other players' checks hit you with traps and gifts." : string.Empty), true);
            Notifier.Remember("Goal completed.");
            DropTrapsAfterGoal();
            _nextChaosShuffle = 0;
        }

        private void ShuffleChaos()
        {
            if (!ChaosMode || Game.GameTime < _nextChaosShuffle) return;
            _nextChaosShuffle = Game.GameTime + ChaosShuffleInterval;
            List<MissionInfo> pool = WorldMissions.Where(m => IsPlayable(m) && IsPassed(m)).ToList();
            _chaosMissions.Clear();
            for (int i = 0; i < 4 && pool.Count > 0; i++)
            {
                MissionInfo pick = pool[_random.Next(pool.Count)];
                pool.Remove(pick);
                _chaosMissions.Add(pick.Code);
            }
            _markers.Clear();
            if (_chaosMissions.Count > 0)
            {
                History.Write("CHAOS_MISSIONS", "codes", _chaosMissions.ToList());
                Notifier.Message(null, "Chaos mode", "New missions on the map: " + string.Join(", ", _chaosMissions.Select(c => ApData.MissionByCode(c)?.Name ?? c)) + ".");
            }
        }

        private void ChaosEvent(string sender)
        {
            if (!ChaosMode || Game.GameTime < _nextChaosEvent) return;
            if (!EnvironmentApi.IsReady || !EnvironmentApi.IsSterile || Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            if (_random.Next(100) >= 40) return;
            _nextChaosEvent = Game.GameTime + ChaosCooldown;
            string who = Notifier.Escape(sender);
            int roll = _random.Next(100);
            if (roll < 60 && TrapApi.IsReady)
            {
                List<string> enabled = TrapApi.EnabledTraps.ToList();
                if (enabled.Count == 0) return;
                string id = enabled[_random.Next(enabled.Count)];
                string status = TrapApi.Trigger(id, sender, out string error) ? "queued" : error;
                History.Write("CHAOS_TRAP", "id", id, "from", sender, "status", status);
                Notifier.Message(who, "Chaos", "~r~Chaos!~s~ Their check just set off a trap.");
            }
            else if (roll < 85 && Gifts.Weapons.Count > 0)
            {
                WeaponGift gift = Gifts.PickWeapon(Guid.NewGuid().ToString(), _random.Next());
                string error = null;
                if (string.Equals(gift.Weapon, "ARMOR", StringComparison.OrdinalIgnoreCase))
                {
                    Ped ped = Game.Player.Character;
                    if (ped != null && ped.Exists()) ped.Armor = 100;
                }
                else EnvironmentApi.GiveWeapon((WeaponHash)StringHash.AtStringHash(gift.Weapon, 0u), Math.Max(1, gift.Ammo), out error);
                History.Write("CHAOS_GIFT", "weapon", gift.Weapon, "from", sender, "error", error);
                Notifier.Message(who, "Chaos", $"~g~Chaos gift:~s~ {Notifier.Escape(gift.Name)}");
            }
            else
            {
                int amount = 1000 * (1 + _random.Next(10));
                EnvironmentApi.GiveMoney(amount, out string error);
                History.Write("CHAOS_GIFT", "money", amount, "from", sender, "error", error);
                Notifier.Message(who, "Chaos", $"~g~Chaos gift:~s~ ${amount:#,0}");
            }
        }

        internal string GoalText()
        {
            if (Save == null) return "Not connected yet";
            switch (Save.Goal)
            {
                case 0:
                    return "Pass a final mission";
                case 1:
                    return "Pass The Third Way";
                case 3:
                    return "Pass " + (GoalMissionInfo?.Name ?? "the goal mission");
                case 4:
                    return "Pick up " + GoalCollectibleName;
                case 5:
                    return "Check " + (Save.GoalLocation > 0 ? ApData.LocationName(Save.GoalLocation) : "the goal location");
                default:
                    return $"Pass {Save.GoalMissions} missions";
            }
        }

        internal string GoalProgress()
        {
            if (Save == null) return string.Empty;
            if (Save.GoalDone) return "Done";
            if (Save.Goal == 2) return $"{PassedCount}/{Save.GoalMissions}";
            return "In progress";
        }

        private void OnMessage(LogMessage message)
        {
            string text = message.ToString();
            if (message is ItemSendLogMessage masked && !(message is HintItemSendLogMessage) && masked.IsReceiverTheActivePlayer
                && ApData.TrapByItem(masked.Item.ItemId) != null)
                text = $"{masked.Sender} sent you a trap ({SafeLocationName(masked.Item)})";
            Notifier.Remember(text);
            switch (message)
            {
                case HintItemSendLogMessage hint:
                    if (Save != null && hint.IsRelatedToActivePlayer && !Save.Feed.Any(f => f.Kind == "hint" && f.Text == text)) Feed("hint", text);
                    if (Config.ShowHints && hint.IsRelatedToActivePlayer) Notifier.Post("~y~Hint:~s~ " + Notifier.Escape(text));
                    break;
                case ItemSendLogMessage send:
                    if (Save != null)
                    {
                        string kind = send.IsSenderTheActivePlayer && send.IsReceiverTheActivePlayer ? "self"
                            : send.IsReceiverTheActivePlayer ? "in"
                            : send.IsSenderTheActivePlayer ? "out" : "other";
                        Feed(kind, text);
                    }
                    if (Config.ShowSent && send.IsSenderTheActivePlayer && !send.IsReceiverTheActivePlayer)
                        Notifier.Post($"Sent ~y~{Notifier.Escape(send.Item.ItemDisplayName)}~s~ to ~b~{Notifier.Escape(send.Receiver.ToString())}~s~");
                    if (!send.IsSenderTheActivePlayer && !send.IsReceiverTheActivePlayer) ChaosEvent(send.Sender.ToString());
                    break;
                case ChatLogMessage chat:
                    if (Save != null) Feed("chat", text);
                    if (Config.ShowChat) Notifier.Post($"~b~{Notifier.Escape(chat.Player.ToString())}:~s~ {Notifier.Escape(chat.Message)}");
                    break;
                case ServerChatLogMessage server:
                    if (Config.ShowChat) Notifier.Post("~p~Server:~s~ " + Notifier.Escape(server.Message));
                    break;
                case CommandResultLogMessage _:
                    Notifier.Post("~c~" + Notifier.Escape(text));
                    break;
            }
        }

        private void Feed(string kind, string text)
        {
            Save.AddFeed(kind, text, Config.FeedSize);
            _saveDirty = true;
        }

        private void OnDeathReceived(DeathLink link)
        {
            if (Save == null || !DeathLinkActive) return;
            if (string.Equals(link.Source, Client.SlotName, StringComparison.OrdinalIgnoreCase)) return;
            _pendingDeath = link;
            History.Write("DEATH_IN", "from", link.Source, "cause", link.Cause);
        }

        private void WatchDeath()
        {
            Ped ped = Game.Player.Character;
            if (ped == null || !ped.Exists()) return;
            bool dead = Function.Call<bool>(Hash.IS_PLAYER_DEAD, Game.Player) || ped.IsDead;
            if (_pendingDeath != null && !dead && !Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE))
            {
                DeathLink link = _pendingDeath;
                _pendingDeath = null;
                _deathGraceUntil = Game.GameTime + 10000;
                Notifier.Post($"~r~DeathLink~s~: {Notifier.Escape(link.Cause ?? link.Source + " died.")}", true);
                Notifier.Remember($"DeathLink from {link.Source}: {link.Cause}");
                ped.Kill();
                dead = true;
            }
            if (dead && !_wasDead && DeathLinkActive && Game.GameTime >= _deathGraceUntil)
            {
                string cause = $"{Client.SlotName} got wasted in Los Santos.";
                Client.SendDeath(cause);
                History.Write("DEATH_OUT");
                Notifier.Remember("DeathLink sent.");
            }
            _wasDead = dead;
        }

        internal void ForgetSave()
        {
            if (Save == null) return;
            Log.Info("Local save forgotten: " + Save.Key);
            Disconnect();
            Save = null;
            _slotMissions.Clear();
            _owned.Clear();
            UnlockedMask = 0;
            MissionApi.UseApiMarkers = false;
            Connection.LastSave = null;
            Connection.Save();
        }
    }
}
