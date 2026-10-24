using System;
using System.Diagnostics;
using GTA;
using GTA.Native;

namespace GameArchipelago.Core
{
    public sealed class OnlineGuard : Script
    {
        private int _hits;
        private int _nextCheck;

        public OnlineGuard()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            ArchipelagoScript core = ArchipelagoScript.Instance;
            if (core == null || !core.IsReady) return;
            if (!core.Config.OnlineGuard)
            {
                Tick -= OnTick;
                return;
            }
            if (Game.GameTime < _nextCheck) return;
            _nextCheck = Game.GameTime + 250;
            bool online = Function.Call<bool>(Hash.NETWORK_IS_SESSION_STARTED) || Function.Call<bool>(Hash.NETWORK_IS_IN_TRANSITION);
            _hits = online ? _hits + 1 : 0;
            if (_hits < 3) return;
            Log.Warn("GTA Online session detected with the mods loaded: closing the game.");
            try
            {
                core.Client.Shutdown();
            }
            catch
            {
            }
            Process.GetCurrentProcess().Kill();
        }
    }
}
