using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.TimerBars;

namespace GameTraps.Core
{
    internal static class TrapTimer
    {
        private static readonly ObjectPool Pool = new ObjectPool();
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        private static TimerBarCollection _bars;

        public static void Start(string key, string title, int durationMs)
        {
            if (_bars == null)
            {
                _bars = new TimerBarCollection();
                Pool.Add(_bars);
            }
            if (!Entries.TryGetValue(key, out Entry entry))
            {
                entry = new Entry { Bar = new TimerBar(title, string.Empty) };
                Entries[key] = entry;
                _bars.Add(entry.Bar);
            }
            entry.Bar.Title = title;
            entry.Until = Game.GameTime + Math.Max(0, durationMs);
            _bars.Visible = true;
        }

        public static void Hide(string key)
        {
            if (!Entries.TryGetValue(key, out Entry entry)) return;
            _bars.Remove(entry.Bar);
            Entries.Remove(key);
            if (Entries.Count == 0) _bars.Visible = false;
        }

        public static void Process()
        {
            if (_bars == null || Entries.Count == 0) return;
            foreach (Entry entry in Entries.Values)
            {
                int remaining = Math.Max(0, entry.Until - Game.GameTime);
                int seconds = (remaining + 999) / 1000;
                entry.Bar.Info = (seconds / 60) + ":" + (seconds % 60).ToString("00");
            }
            Pool.Process();
        }

        private sealed class Entry
        {
            public TimerBar Bar;
            public int Until;
        }
    }

    internal static class Wallet
    {
        private static readonly string[] CashStats = { "SP0_TOTAL_CASH", "SP1_TOTAL_CASH", "SP2_TOTAL_CASH" };

        public static int Money => Game.Player.Money;

        public static int Take(int amount)
        {
            int money = Money;
            int taken = Math.Min(money, Math.Max(0, amount));
            if (taken <= 0) return 0;
            if (!EnvironmentBridge.GiveMoney(-taken)) SetCash(money - taken);
            return taken;
        }

        public static void Give(int amount)
        {
            if (amount <= 0) return;
            if (!EnvironmentBridge.GiveMoney(amount)) SetCash(Money + amount);
        }

        private static void SetCash(int value)
        {
            int model = Game.Player.Character.Model.Hash;
            int slot = model == new Model(PedHash.Franklin).Hash ? 1 : model == new Model(PedHash.Trevor).Hash ? 2 : model == new Model(PedHash.Michael).Hash ? 0 : -1;
            if (slot < 0)
            {
                Game.Player.Money = value;
                return;
            }
            Function.Call(Hash.STAT_SET_INT, unchecked((int)StringHash.AtStringHash(CashStats[slot], 0u)), Math.Max(0, value), true);
        }
    }

    internal static class EnvironmentBridge
    {
        private static Type _api;
        private static bool _searched;

        public static bool RemoveWeapon(WeaponHash weapon)
        {
            Type api = Find();
            if (api == null) return false;
            try
            {
                var method = api.GetMethod("RemoveWeapon", new[] { typeof(WeaponHash) });
                if (method == null) return false;
                object result = method.Invoke(null, new object[] { weapon });
                return result is bool removed && removed;
            }
            catch (Exception ex)
            {
                Log.Warn("GameEnvironment.RemoveWeapon failed: " + ex.GetBaseException().Message);
                return false;
            }
        }

        public static bool GiveMoney(int amount)
        {
            Type api = Find();
            if (api == null) return false;
            try
            {
                var method = api.GetMethod("GiveMoney", new[] { typeof(int), typeof(string).MakeByRefType() });
                if (method == null) return false;
                var args = new object[] { amount, null };
                object result = method.Invoke(null, args);
                if (result is bool ok && ok) return true;
                Log.Warn("GameEnvironment.GiveMoney refused: " + args[1]);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warn("GameEnvironment.GiveMoney failed: " + ex.GetBaseException().Message);
                return false;
            }
        }

        private static Type Find()
        {
            if (_searched) return _api;
            _searched = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "GameEnvironment") continue;
                _api = assembly.GetType("GameEnvironment.EnvironmentApi");
                break;
            }
            Log.Info(_api != null ? "GameEnvironment found: removed weapons are also removed from its state." : "GameEnvironment not found.");
            return _api;
        }
    }
}
