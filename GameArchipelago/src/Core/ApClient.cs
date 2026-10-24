using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;

namespace GameArchipelago.Core
{
    internal enum LinkState
    {
        Offline,
        Connecting,
        Connected,
    }

    internal sealed class ApClient
    {
        private readonly ConcurrentQueue<Action> _inbox = new ConcurrentQueue<Action>();
        private ArchipelagoSession _session;
        private DeathLinkService _deathLink;
        private int _attempt;

        public ApClient()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | (SecurityProtocolType)12288;
            }
            catch
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
        }

        public LinkState State { get; private set; } = LinkState.Offline;
        public string LastError { get; private set; }
        public string Seed { get; private set; }
        public string SlotName { get; private set; }
        public int SlotNumber { get; private set; }
        public Dictionary<string, object> SlotData { get; private set; }
        public bool IsConnected => State == LinkState.Connected && _session != null;

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<LogMessage> Message;
        public event Action<DeathLink> DeathReceived;

        public void Connect(string server, string slot, string password)
        {
            if (State == LinkState.Connecting) return;
            Close(null);
            State = LinkState.Connecting;
            LastError = null;
            int attempt = ++_attempt;
            string host = (server ?? string.Empty).Trim();
            string name = (slot ?? string.Empty).Trim();
            string pass = string.IsNullOrEmpty(password) ? null : password;
            Log.Info($"Connecting to {host} as {name}.");
            Task.Run(() =>
            {
                ArchipelagoSession session = null;
                try
                {
                    session = ArchipelagoSessionFactory.CreateSession(host);
                    session.MessageLog.OnMessageReceived += message => _inbox.Enqueue(() =>
                    {
                        if (attempt == _attempt) Message?.Invoke(message);
                    });
                    session.Socket.SocketClosed += reason => _inbox.Enqueue(() =>
                    {
                        if (attempt == _attempt && _session == session) Close(string.IsNullOrEmpty(reason) ? "Connection closed." : reason);
                    });
                    LoginResult result = session.TryConnectAndLogin(ApData.Game, name, ItemsHandlingFlags.AllItems, null,
                        new[] { "AP" }, null, pass, true);
                    if (result is LoginSuccessful success)
                    {
                        ArchipelagoSession connected = session;
                        _inbox.Enqueue(() => OnConnected(attempt, connected, success, name));
                    }
                    else
                    {
                        string error = result is LoginFailure failure && failure.Errors != null && failure.Errors.Length > 0
                            ? string.Join(" ", failure.Errors)
                            : "Login refused.";
                        _inbox.Enqueue(() => OnFailed(attempt, error));
                        TryDisconnect(session);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Connection failed.", ex);
                    string error = ex.GetBaseException().Message;
                    _inbox.Enqueue(() => OnFailed(attempt, error));
                    TryDisconnect(session);
                }
            });
        }

        public void Process()
        {
            int budget = 200;
            while (budget-- > 0 && _inbox.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Log.Error("Archipelago event failed.", ex);
                }
            }
        }

        public void Disconnect() => Close("Disconnected.");

        public IReadOnlyList<ItemInfo> ReceivedItems =>
            _session?.Items.AllItemsReceived ?? (IReadOnlyList<ItemInfo>)Array.Empty<ItemInfo>();

        public IReadOnlyList<long> CheckedLocations =>
            _session?.Locations.AllLocationsChecked ?? (IReadOnlyList<long>)Array.Empty<long>();

        public IReadOnlyList<long> AllLocations =>
            _session?.Locations.AllLocations ?? (IReadOnlyList<long>)Array.Empty<long>();

        public void SendChecks(IEnumerable<long> ids)
        {
            if (!IsConnected) return;
            long[] array = ids.Distinct().ToArray();
            if (array.Length == 0) return;
            ArchipelagoSession session = _session;
            Task.Run(() =>
            {
                try
                {
                    session.Locations.CompleteLocationChecks(array);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not send location checks.", ex);
                }
            });
        }

        public void Say(string text)
        {
            if (!IsConnected || string.IsNullOrWhiteSpace(text)) return;
            ArchipelagoSession session = _session;
            Task.Run(() =>
            {
                try
                {
                    session.Say(text.Trim());
                }
                catch (Exception ex)
                {
                    Log.Error("Could not send a message.", ex);
                }
            });
        }

        public void SendGoal()
        {
            if (!IsConnected) return;
            ArchipelagoSession session = _session;
            Task.Run(() =>
            {
                try
                {
                    session.SetGoalAchieved();
                }
                catch (Exception ex)
                {
                    Log.Error("Could not send the goal.", ex);
                }
            });
        }

        public void SetDeathLink(bool enabled)
        {
            if (_deathLink == null) return;
            try
            {
                if (enabled) _deathLink.EnableDeathLink();
                else _deathLink.DisableDeathLink();
            }
            catch (Exception ex)
            {
                Log.Error("Could not change DeathLink.", ex);
            }
        }

        public void SendDeath(string cause)
        {
            if (!IsConnected || _deathLink == null) return;
            DeathLinkService service = _deathLink;
            string source = SlotName;
            Task.Run(() =>
            {
                try
                {
                    service.SendDeathLink(new DeathLink(source, cause));
                }
                catch (Exception ex)
                {
                    Log.Error("Could not send a DeathLink.", ex);
                }
            });
        }

        public string PlayerName(int slot)
        {
            if (slot == 0) return "Archipelago";
            try
            {
                return _session?.Players.GetPlayerAlias(slot) ?? $"Player {slot}";
            }
            catch
            {
                return $"Player {slot}";
            }
        }

        public IReadOnlyList<PlayerInfo> Players
        {
            get
            {
                try
                {
                    return _session?.Players.AllPlayers.Where(p => p.Slot > 0).ToList() ?? new List<PlayerInfo>();
                }
                catch
                {
                    return new List<PlayerInfo>();
                }
            }
        }

        public void Shutdown()
        {
            _attempt++;
            ArchipelagoSession session = _session;
            _session = null;
            _deathLink = null;
            State = LinkState.Offline;
            if (session == null) return;
            try
            {
                if (session.Socket.Connected) session.Socket.DisconnectAsync().Wait(1500);
            }
            catch
            {
            }
        }

        private void OnConnected(int attempt, ArchipelagoSession session, LoginSuccessful success, string name)
        {
            if (attempt != _attempt)
            {
                TryDisconnect(session);
                return;
            }
            _session = session;
            SlotName = name;
            SlotNumber = success.Slot;
            SlotData = success.SlotData ?? new Dictionary<string, object>();
            Seed = session.RoomState.Seed;
            State = LinkState.Connected;
            try
            {
                _deathLink = session.CreateDeathLinkService();
                _deathLink.OnDeathLinkReceived += link => _inbox.Enqueue(() =>
                {
                    if (attempt == _attempt) DeathReceived?.Invoke(link);
                });
            }
            catch (Exception ex)
            {
                Log.Error("DeathLink is not available.", ex);
                _deathLink = null;
            }
            Log.Info($"Connected to seed {Seed} as {name} (slot {SlotNumber}).");
            Connected?.Invoke();
        }

        private void OnFailed(int attempt, string error)
        {
            if (attempt != _attempt) return;
            State = LinkState.Offline;
            LastError = error;
            Log.Warn("Connection failed: " + error);
            Disconnected?.Invoke(error);
        }

        private void Close(string reason)
        {
            bool wasOnline = State != LinkState.Offline;
            _attempt++;
            ArchipelagoSession session = _session;
            _session = null;
            _deathLink = null;
            State = LinkState.Offline;
            TryDisconnect(session);
            if (wasOnline && reason != null)
            {
                LastError = reason;
                Log.Info("Disconnected: " + reason);
                Disconnected?.Invoke(reason);
            }
        }

        private static void TryDisconnect(ArchipelagoSession session)
        {
            if (session == null) return;
            Task.Run(async () =>
            {
                try
                {
                    if (session.Socket.Connected) await session.Socket.DisconnectAsync();
                }
                catch
                {
                }
            });
        }
    }
}
