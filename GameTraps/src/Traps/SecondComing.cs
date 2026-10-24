using System;
using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class SecondComing : Trap
    {
        private const string VisionIn = "DrugsTrevorClownsFightIn";
        private const string Vision = "DrugsTrevorClownsFight";
        private const string VisionOut = "DrugsTrevorClownsFightOut";
        private const float MessiahDistance = 7f;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Ped> _disciples = new List<Ped>();
        private Ped _messiah;
        private Prop _anchor;
        private Vector3 _messiahPosition;
        private float _risen;
        private readonly List<Ped> _faithful = new List<Ped>();
        private List<Model> _fishModels = new List<Model>();
        private bool _resurrected;
        private int _resurrectAt;
        private int _nextFish;
        private int _fishCount;
        private Phase _phase;
        private int _phaseEnd;
        private int _until;

        public SecondComing(TrapsConfig config) : base(config)
        {
        }

        private enum Phase
        {
            Ascension,
            Pilgrimage,
        }

        private SecondComingConfig Settings => Config.SecondComing;
        public override string Id => TrapIds.SecondComing;
        public override string Title => "Second coming";
        public override string Subtitle => "Behold, he rises.";
        public override bool Enabled => Settings.Enabled;
        public override TrapChannel Channels => TrapChannel.Player | TrapChannel.Vision;

        public override bool CanStart(out string reason)
        {
            reason = PlayerInAircraft ? "waiting for you to land" : null;
            return reason == null;
        }

        public override void Start()
        {
            _disciples.Clear();
            _messiah = null;
            Ped player = Player;
            Model model = Spawner.LoadModel(Settings.Model);
            if (model.Hash != 0)
            {
                foreach (Ped ped in Spawner.AmbientPeds(Settings.Radius, Settings.MaxPeds, p => !p.IsInVehicle()))
                {
                    Ped disciple = Spawner.ReplacePed(ped, model);
                    if (disciple == null) continue;
                    _entities.Add(disciple);
                    _disciples.Add(disciple);
                    disciple.BlockPermanentEvents = true;
                    Spawner.Poof(disciple.Position);
                }

                Entity reference = player.IsInVehicle() ? (Entity)player.CurrentVehicle : player;
                Vector3 front = reference.Position + reference.ForwardVector * MessiahDistance;
                front.Z = Spawner.GroundZ(front + new Vector3(0f, 0f, 3f), reference.Position.Z - 1f);
                SpawnMessiah(model, front, Spawner.HeadingTowards(front, player.Position));
                Script.Wait(500);
                foreach (Ped disciple in _disciples)
                {
                    if (!disciple.Exists()) continue;
                    if (_messiah != null) disciple.Heading = Spawner.HeadingTowards(disciple.Position, _messiah.Position);
                    Emotes.PlayRandom(disciple, Settings.Emotes);
                }
            }
            ScreenEffects.Light(Settings.AscensionBlur);
            _until = StartTimer(Settings.VisionDuration);
            _phase = Phase.Ascension;
            _phaseEnd = Game.GameTime + Settings.AscendDuration;
            _risen = 0f;
            Log.Info($"Second coming: {_disciples.Count} disciples.");
        }

        public override bool Update()
        {
            int now = Game.GameTime;
            if (_phase == Phase.Ascension)
            {
                Ascend(Math.Min(0.05f, Game.LastFrameTime));
                if (now < _phaseEnd) return true;
                Pilgrimage();
                return true;
            }
            if (_phase == Phase.Pilgrimage) Miracles();
            return now < _until;
        }

        public override void Stop(bool interrupted)
        {
            RemoveMessiah();
            _disciples.Clear();
            _faithful.Clear();
            _entities.DeleteAll();
            ScreenEffects.FadeOut(Vision, VisionOut, 2500);
            if (GTA.UI.Screen.IsFadedOut || GTA.UI.Screen.IsFadingOut) GTA.UI.Screen.FadeIn(500);
        }

        private void Welcome()
        {
            Ped player = Player;
            player.Health = player.MaxHealth;
            player.Armor = 100;
            Spawner.Poof(player.Position);
            _faithful.Clear();
            _resurrected = false;
            _resurrectAt = Game.GameTime + Settings.ResurrectDelay;
            _nextFish = Game.GameTime + Settings.ResurrectDelay + 2500;
            _fishCount = 0;
            _fishModels = Spawner.LoadAll(Settings.FishModels);

            List<Model> models = Spawner.LoadAll(Settings.WorshipperModels);
            if (models.Count == 0) return;
            Vector3 center = player.Position + player.ForwardVector * 5f;
            for (int i = 0; i < Settings.Worshippers; i++)
            {
                double angle = Math.PI * 2 * i / Math.Max(1, Settings.Worshippers);
                var spot = new Vector3(center.X + (float)Math.Cos(angle) * 4f, center.Y + (float)Math.Sin(angle) * 4f, center.Z + 3f);
                spot.Z = Spawner.GroundZ(spot, center.Z) + 1f;
                if (spot.DistanceTo2D(player.Position) < 2f) continue;
                Ped body = Ped.Create(Spawner.Pick(models), spot, Spawner.HeadingTowards(spot, center));
                if (body == null) continue;
                _entities.Add(body);
                body.BlockPermanentEvents = true;
                body.Kill();
                _faithful.Add(body);
            }
            GTA.UI.Screen.ShowSubtitle("They are not dead... they are only sleeping.", 4000);
        }

        private void Miracles()
        {
            int now = Game.GameTime;
            if (!_resurrected && now >= _resurrectAt)
            {
                _resurrected = true;
                foreach (Ped body in _faithful)
                {
                    if (!body.Exists()) continue;
                    Function.Call(Hash.RESURRECT_PED, body);
                    Function.Call(Hash.REVIVE_INJURED_PED, body);
                    body.Health = 200;
                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, body);
                    body.BlockPermanentEvents = true;
                    Spawner.Poof(body.Position);
                    Emotes.PlayRandom(body, Settings.ResurrectEmotes);
                }
                GTA.UI.Screen.ShowSubtitle("~y~Miracle!~s~ Rise and dance!", 4000);
                Log.Info($"Second coming: {_faithful.Count} worshippers resurrected.");
            }
            if (_fishModels.Count == 0 || now < _nextFish || _fishCount >= Settings.FishCount || now > _until - 2000) return;
            _nextFish = now + Settings.FishInterval;
            _fishCount++;
            Ped player = Player;
            Vector3 drop = player.Position + new Vector3(Spawner.Range(-15f, 15f), Spawner.Range(-15f, 15f), Spawner.Range(18f, 30f));
            Ped fish = Ped.Create(Spawner.Pick(_fishModels), drop, Spawner.Range(0f, 360f));
            if (fish == null) return;
            _entities.Add(fish);
            fish.Velocity = new Vector3(0f, 0f, -6f);
            if (_fishCount == 1) GTA.UI.Screen.ShowSubtitle("~y~Miracle!~s~ It is raining fish.", 4000);
        }

        private void SpawnMessiah(Model model, Vector3 feet, float heading)
        {
            Model anchorModel = Spawner.LoadModel(Settings.AnchorModel);
            if (anchorModel.Hash == 0) return;
            _anchor = Prop.Create(anchorModel, feet, false, false);
            _messiah = Ped.Create(model, feet + new Vector3(0f, 0f, 1f), heading);
            if (_anchor == null || _messiah == null) return;
            _entities.Add(_anchor);
            _entities.Add(_messiah);
            _anchor.IsVisible = false;
            _anchor.IsCollisionEnabled = false;
            _anchor.IsPositionFrozen = true;
            _anchor.Heading = heading;
            _messiah.IsInvincible = true;
            _messiah.BlockPermanentEvents = true;
            _messiah.CanRagdoll = false;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _messiah, _anchor, 0, 0f, 0f, 1f, 0f, 0f, 0f, false, false, false, true, 2, true);
            _messiahPosition = feet;
            Spawner.Poof(feet + new Vector3(0f, 0f, 1f));
            if (Settings.MessiahEmote != null) Emotes.Play(_messiah, Settings.MessiahEmote);
        }

        private void RemoveMessiah()
        {
            if (_messiah != null && _messiah.Exists())
            {
                Function.Call(Hash.DETACH_ENTITY, _messiah, false, false);
                _messiah.Delete();
            }
            if (_anchor != null && _anchor.Exists()) _anchor.Delete();
            _messiah = null;
            _anchor = null;
        }

        private void Ascend(float delta)
        {
            if (_anchor == null || !_anchor.Exists()) return;
            _risen += Settings.AscendSpeed * delta;
            float eased = _risen < 3f ? _risen * _risen / 6f : _risen - 1.5f;
            Vector3 position = _messiahPosition + new Vector3(0f, 0f, eased);
            Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, _anchor, position.X, position.Y, position.Z, false, false, false);
            Vector3 p = position + new Vector3(0f, 0f, 1f);
            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, p.X, p.Y, p.Z, 255, 240, 200, 8f, 10f);
            Function.Call(Hash.DRAW_SPOT_LIGHT, p.X, p.Y, p.Z + 40f, 0f, 0f, -1f, 255, 245, 210, 60f, 25f, 0f, 6f, 1f);
            Function.Call(Hash.TASK_LOOK_AT_ENTITY, Player, _messiah, 500, 0, 2);
        }

        private void Pilgrimage()
        {
            GTA.UI.Screen.FadeOut(1500);
            int deadline = Environment.TickCount + 4000;
            while (!GTA.UI.Screen.IsFadedOut && Environment.TickCount < deadline) Script.Wait(0);
            RemoveMessiah();
            Destination destination = Spawner.Pick(Settings.Destinations);
            Spawner.Teleport(destination);
            Welcome();
            ScreenEffects.Start(VisionIn, false);
            foreach (string effect in Settings.ChurchEffects) ScreenEffects.Start(effect, true);
            ScreenEffects.Drunk(true);
            Script.Wait(300);
            GTA.UI.Screen.FadeIn(2000);
            Log.Info($"Player sent to {destination.Name}.");
            _phase = Phase.Pilgrimage;
        }
    }
}
