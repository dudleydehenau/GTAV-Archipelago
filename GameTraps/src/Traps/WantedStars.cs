using System;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class WantedStars : Trap
    {
        public WantedStars(TrapsConfig config) : base(config)
        {
        }

        private WantedConfig Settings => Config.Wanted;
        public override string Id => TrapIds.Wanted;
        public override string Title => "Wanted";
        public override string Subtitle => "Someone called the cops.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police;

        public override void Start()
        {
            int level = Math.Min(5, Game.Player.Wanted.WantedLevel + Math.Max(1, Settings.Stars));
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, level, false);
            Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
            Log.Info($"Wanted level set to {level}.");
        }

        public override bool Update() => false;

        public override void Stop(bool interrupted)
        {
        }
    }
}
