using System;
using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class ForrestGump : Trap
    {
        private static readonly string[] Extras = { "a_m_y_hippy_01", "a_f_y_hippie_01", "a_m_m_tourist_01", "a_f_y_tourist_01", "a_m_y_runner_01", "a_f_y_runner_01", "a_m_y_beach_01", "a_m_m_salton_01" };
        private readonly List<Ped> _followers = new List<Ped>();
        private readonly EntityTracker _entities = new EntityTracker();
        private int _until;
        private int _nextOrder;
        private int _nextLine;
        private int _line;

        public ForrestGump(TrapsConfig config) : base(config)
        {
        }

        private ForrestGumpConfig Settings => Config.ForrestGump;
        public override string Id => TrapIds.ForrestGump;
        public override string Title => "Run, Forrest, run!";
        public override string Subtitle => "You just felt like running.";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = Player.IsInVehicle() || PlayerInWater ? "waiting for you to be on foot" : null;
            return reason == null;
        }

        public override void Start()
        {
            _followers.Clear();
            _until = StartTimer(Settings.Duration);
            _nextOrder = 0;
            _line = 0;
            _nextLine = 0;
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            Ped player = Player;
            if (now >= _until || player.IsDead) return false;
            Function.Call(Hash.RESTORE_PLAYER_STAMINA, Game.Player, 1f);
            Lines(now);
            if (now < _nextOrder) return true;
            _nextOrder = now + 1200;
            _followers.RemoveAll(f => !f.Exists() || f.IsDead);
            if (_followers.Count < Settings.MaxPeds)
            {
                foreach (Ped ped in Spawner.AmbientPeds(Settings.Radius, Settings.MaxPeds - _followers.Count, p => !_followers.Contains(p) && !p.IsInVehicle()))
                {
                    ped.BlockPermanentEvents = true;
                    _followers.Add(ped);
                }
            }
            if (_followers.Count < Settings.MinFollowers)
            {
                Model model = Spawner.LoadModel(Spawner.Pick(Extras));
                if (model.Hash != 0)
                {
                    Vector3 spot = Spawner.GroundAround(player.Position - player.ForwardVector * 15f, Spawner.Range(2f, 8f));
                    Ped extra = Ped.Create(model, spot, player.Heading);
                    if (extra != null)
                    {
                        Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, extra, 0);
                        extra.BlockPermanentEvents = true;
                        _entities.Add(extra);
                        _followers.Add(extra);
                    }
                }
            }
            bool moving = player.Velocity.Length() > 1.5f;
            for (int i = 0; i < _followers.Count; i++)
            {
                Ped follower = _followers[i];
                if (moving)
                {
                    float back = Settings.FollowDistance + (i / 4) * 1.5f;
                    float side = ((i % 4) - 1.5f) * 1.2f;
                    Function.Call(Hash.TASK_FOLLOW_TO_OFFSET_OF_ENTITY, follower, player, side, -back, 0f, Settings.RunSpeed, -1, 1f, true);
                }
                else
                {
                    follower.Heading = Spawner.HeadingTowards(follower.Position, player.Position);
                    Function.Call(Hash.TASK_STAND_STILL, follower, 1500);
                }
            }
            return true;
        }

        public override void Stop(bool interrupted)
        {
            foreach (Ped follower in _followers)
            {
                if (!follower.Exists() || follower.IsDead) continue;
                follower.BlockPermanentEvents = false;
                Function.Call(Hash.CLEAR_PED_TASKS, follower);
                Function.Call(Hash.TASK_WANDER_STANDARD, follower, 10f, 10);
            }
            _followers.Clear();
            if (interrupted) _entities.DeleteAll();
            else _entities.Forget();
            if (!interrupted && !Player.IsDead)
                Overrides.Subtitle("~y~Forrest:~s~ I'm pretty tired. I think I'll go home now.", 6000);
        }

        private void Lines(int now)
        {
            if (now < _nextLine) return;
            _nextLine = now + 9000;
            string[] lines =
            {
                "~y~Jenny:~s~ Run, Forrest, run!",
                "~y~A follower:~s~ He must know something. Follow him!",
                "~y~Forrest:~s~ Life is like a box of chocolates.",
                "~y~A follower:~s~ Quiet, he's going to say something!",
            };
            if (_line >= lines.Length) return;
            GTA.UI.Screen.ShowSubtitle(lines[_line], 6000);
            _line++;
        }
    }

    internal sealed class WalkingDead : Trap
    {
        private const Hash SetPedMovementClipset = (Hash)0xAF8A94EDE7712BEFUL;
        private readonly Squad _walkers = new Squad();
        private readonly Ambience _ambience = new Ambience();
        private int _until;

        public WalkingDead(TrapsConfig config) : base(config)
        {
        }

        private WalkingDeadConfig Settings => Config.WalkingDead;
        public override string Id => TrapIds.WalkingDead;
        public override string Title => "The Walking Dead";
        public override string Subtitle => "Don't get bitten.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Police | TrapChannel.Weather;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft || PlayerInWater ? "waiting for you to be on land" : null;
            return reason == null;
        }

        public override void Start()
        {
            Model zombie = Spawner.LoadModel(Settings.Model);
            if (zombie.Hash == 0) return;
            _ambience.Night(Settings.Hour);
            _ambience.Weather(Settings.Weather);
            _ambience.Timecycle(Settings.Timecycle, Settings.TimecycleStrength);
            if (Settings.Blackout) _ambience.Blackout();
            NoPolice.Begin();
            Function.Call(Hash.REQUEST_CLIP_SET, Settings.Clipset);
            int deadline = Environment.TickCount + 1000;
            while (!Function.Call<bool>(Hash.HAS_CLIP_SET_LOADED, Settings.Clipset) && Environment.TickCount < deadline) Script.Wait(0);
            _walkers.Accuracy = 0;
            _walkers.MoveRate = Settings.MoveRate;
            _walkers.Poofs = false;
            _walkers.Blips = false;
            _walkers.Spawned = Infect;
            _walkers.Load(new List<string> { Settings.Model }, new List<string> { "WEAPON_UNARMED" });
            foreach (Ped ped in Spawner.AmbientPeds(Settings.Radius, Settings.Convert, p => !p.IsInVehicle()))
            {
                Ped turned = Spawner.ReplacePed(ped, zombie);
                if (turned != null) _walkers.Adopt(turned);
            }
            for (int i = _walkers.Alive; i < Settings.Count; i++) _walkers.SpawnAround(Settings.SpawnDistance);
            _until = StartTimer(Settings.Duration);
            Overrides.Subtitle("~r~Rick:~s~ They're everywhere. Don't let them bite you.", 6000);
            Log.Info($"Walking dead: {_walkers.Alive} walkers.");
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until || Player.IsDead) return false;
            NoPolice.Keep();
            _walkers.Process(Settings.Count, Settings.SpawnDistance, true);
            return true;
        }

        public override void Stop(bool interrupted)
        {
            NoPolice.End();
            _walkers.Spawned = null;
            _walkers.Clear(true);
            _ambience.Restore();
        }

        private void Infect(Ped walker)
        {
            walker.MaxHealth = Settings.Health;
            walker.Health = Settings.Health;
            Function.Call(Hash.SET_PED_RANDOM_COMPONENT_VARIATION, walker, 0);
            Function.Call(SetPedMovementClipset, walker, Settings.Clipset, 1f);
            Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, walker, 3);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, walker, 0);
            Function.Call(Hash.STOP_PED_SPEAKING, walker, true);
            Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, walker, true);
        }
    }
}
