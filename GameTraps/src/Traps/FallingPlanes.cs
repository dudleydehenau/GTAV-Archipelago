using System;
using System.Collections.Generic;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class FallingPlanes : Trap
    {
        private const int Linger = 12000;
        private readonly EntityTracker _entities = new EntityTracker();
        private List<Model> _models = new List<Model>();
        private int _spawned;
        private int _nextSpawn;
        private int _until;

        public FallingPlanes(TrapsConfig config) : base(config)
        {
        }

        private FallingPlanesConfig Settings => Config.FallingPlanes;
        public override string Id => TrapIds.FallingPlanes;
        public override string Title => "Mayday!";
        public override string Subtitle => "Look up.";
        public override bool Enabled => Settings.Enabled;

        public override void Start()
        {
            _models = Spawner.LoadAll(Settings.Models);
            _spawned = 0;
            _nextSpawn = Game.GameTime + 1500;
            _until = Game.GameTime + 1500 + Settings.Count * Settings.Interval + Linger;
        }

        public override bool Update()
        {
            if (_models.Count == 0) return false;
            if (_spawned < Settings.Count && Game.GameTime >= _nextSpawn)
            {
                _nextSpawn = Game.GameTime + Settings.Interval;
                _spawned++;
                Drop(Spawner.Pick(_models));
            }
            return Game.GameTime < _until;
        }

        public override void Stop(bool interrupted)
        {
            if (interrupted) _entities.DeleteAll();
            else _entities.Forget();
        }

        private void Drop(Model model)
        {
            Ped player = Player;
            Vector3 center = player.Position;
            double angle = Spawner.Random.NextDouble() * Math.PI * 2;
            float distance = Spawner.Range(Settings.SpawnRadius * 0.6f, Settings.SpawnRadius);
            var start = new Vector3(center.X + (float)Math.Cos(angle) * distance, center.Y + (float)Math.Sin(angle) * distance, center.Z + Settings.Height);
            Vector3 target = Spawner.GroundAround(center, Spawner.Range(4f, Settings.TargetRadius));
            Vector3 direction = (target - start).Normalized;
            float heading = (float)(Math.Atan2(-direction.X, direction.Y) * 180.0 / Math.PI);
            float pitch = (float)(Math.Asin(direction.Z) * 180.0 / Math.PI);

            Vehicle plane = Vehicle.Create(model, start, heading);
            if (plane == null) return;
            _entities.Add(plane);
            Function.Call(Hash.SET_ENTITY_ROTATION, plane, pitch, 0f, heading, 2, true);
            Function.Call(Hash.SET_VEHICLE_ENGINE_ON, plane, true, true, false);
            Function.Call(Hash.CONTROL_LANDING_GEAR, plane, 3);
            Function.Call(Hash.SET_VEHICLE_OUT_OF_CONTROL, plane, false, true);
            plane.EngineHealth = 150f;
            plane.Velocity = direction * Settings.Speed;
            Log.Debug($"Plane {model.Hash} dropped {distance:0} m away.");
        }
    }
}
