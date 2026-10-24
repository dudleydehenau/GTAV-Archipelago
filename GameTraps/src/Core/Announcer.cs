using System;
using GTA;
using GTA.Native;

namespace GameTraps.Core
{
    internal sealed class Announcer
    {
        private const int Duration = 4000;
        private Scaleform _scaleform;
        private int _until = -1;

        public void Show(string title, string subtitle)
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "TIMER_STOP", "HUD_MINI_GAME_SOUNDSET", true);
            if (_scaleform == null) _scaleform = Scaleform.RequestMovie("MIDSIZED_MESSAGE");
            int deadline = Environment.TickCount + 1000;
            while (!_scaleform.IsLoaded && Environment.TickCount < deadline) Script.Wait(0);
            if (!_scaleform.IsLoaded)
            {
                GTA.UI.Notification.PostTicker("~r~" + title + "~s~~n~" + subtitle, true);
                return;
            }
            _scaleform.CallFunction("SHOW_SHARD_MIDSIZED_MESSAGE", "~r~" + title, subtitle);
            _until = Environment.TickCount + Duration;
        }

        public void Draw()
        {
            if (_until < 0) return;
            if (Environment.TickCount > _until)
            {
                _until = -1;
                return;
            }
            _scaleform.Render2D();
        }
    }
}
