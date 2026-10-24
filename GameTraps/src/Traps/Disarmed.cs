using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Disarmed : Trap
    {
        private const uint Unarmed = 0xA2719263;

        public Disarmed(TrapsConfig config) : base(config)
        {
        }

        public override string Id => TrapIds.Disarmed;
        public override string Title => "Butterfingers";
        public override string Subtitle => "Your weapon is gone.";
        public override bool Enabled => Config.Disarmed.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = Target(Player) == Unarmed ? "waiting for you to own a weapon" : null;
            return reason == null;
        }

        public override void Start()
        {
            Ped player = Player;
            uint weapon = Target(player);
            if (weapon == Unarmed) return;
            bool tracked = EnvironmentBridge.RemoveWeapon((WeaponHash)weapon);
            if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, player, weapon, false)) Function.Call(Hash.REMOVE_WEAPON_FROM_PED, player, weapon);
            Function.Call(Hash.SET_CURRENT_PED_WEAPON, player, Unarmed, true);
            Log.Info($"Weapon {(WeaponHash)weapon} removed{(tracked ? " (also from GameEnvironment)" : string.Empty)}.");
        }

        public override bool Update() => false;

        public override void Stop(bool interrupted)
        {
        }

        private static uint Target(Ped ped)
        {
            uint selected = unchecked((uint)Function.Call<int>(Hash.GET_SELECTED_PED_WEAPON, ped));
            if (selected != Unarmed && selected != 0) return selected;
            uint best = unchecked((uint)Function.Call<int>(Hash.GET_BEST_PED_WEAPON, ped, false));
            return best == 0 ? Unarmed : best;
        }
    }
}
