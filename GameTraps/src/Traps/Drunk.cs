using System;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class Drunk : Trap
    {
        private const Hash SetPedMovementClipset = (Hash)0xAF8A94EDE7712BEFUL;
        private readonly EntityTracker _entities = new EntityTracker();
        private Prop _bottle;
        private int _settleAt;
        private Ped _ped;
        private int _until;

        public Drunk(TrapsConfig config) : base(config)
        {
        }

        private DrunkConfig Settings => Config.Drunk;
        public override string Id => TrapIds.Drunk;
        public override string Title => "Last round";
        public override string Subtitle => "You had one too many.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player | TrapChannel.Vision;

        public override void Start()
        {
            _ped = Player;
            DropBottle(_ped);
            Apply(_ped);
            Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "DRUNK_SHAKE", 2f);
            if (!string.IsNullOrEmpty(Settings.Timecycle)) Function.Call(Hash.SET_TIMECYCLE_MODIFIER, Settings.Timecycle);
            _until = StartTimer(Settings.Duration);
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until) return false;
            if (_bottle != null && Game.GameTime >= _settleAt)
            {
                if (_bottle.Exists() && Function.Call<float>(Hash.GET_ENTITY_HEIGHT_ABOVE_GROUND, _bottle) > 0.25f)
                    Function.Call(Hash.PLACE_OBJECT_ON_GROUND_PROPERLY, _bottle);
                _bottle = null;
            }
            Ped player = Player;
            if (player != _ped)
            {
                _ped = player;
                Apply(_ped);
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            _bottle = null;
            _entities.Forget();
            Ped player = Player;
            Function.Call(Hash.RESET_PED_MOVEMENT_CLIPSET, player, 0.5f);
            Function.Call(Hash.SET_PED_IS_DRUNK, player, false);
            Function.Call(Hash.STOP_GAMEPLAY_CAM_SHAKING, true);
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
        }

        private void DropBottle(Ped ped)
        {
            if (ped.IsInVehicle()) return;
            Model model = Spawner.LoadAny(Settings.BottleModels);
            if (model.Hash == 0) return;
            GTA.Math.Vector3 position = ped.Position + ped.ForwardVector * 0.6f + new GTA.Math.Vector3(0f, 0f, 0.4f);
            Prop bottle = Prop.Create(model, position, true, false);
            if (bottle == null) return;
            _entities.Add(bottle);
            _bottle = bottle;
            _settleAt = Game.GameTime + 1200;
            bottle.IsPositionFrozen = false;
            Function.Call(Hash.SET_ENTITY_DYNAMIC, bottle, true);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, bottle, true);
            Function.Call(Hash.ACTIVATE_PHYSICS, bottle);
            bottle.Velocity = new GTA.Math.Vector3(0f, 0f, -1.5f);
            Function.Call(Hash.PLAY_SOUND_FROM_ENTITY, -1, "Drop", bottle, "HUD_FRONTEND_DEFAULT_SOUNDSET", false, 0);
        }

        private void Apply(Ped ped)
        {
            Function.Call(Hash.SET_PED_IS_DRUNK, ped, true);
            if (string.IsNullOrEmpty(Settings.Clipset)) return;
            Function.Call(Hash.REQUEST_CLIP_SET, Settings.Clipset);
            int deadline = Environment.TickCount + 1000;
            while (!Function.Call<bool>(Hash.HAS_CLIP_SET_LOADED, Settings.Clipset) && Environment.TickCount < deadline) Script.Wait(0);
            Function.Call(SetPedMovementClipset, ped, Settings.Clipset, 1f);
        }
    }
}
