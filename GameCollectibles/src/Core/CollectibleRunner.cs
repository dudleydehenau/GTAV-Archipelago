using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GameCollectibles.Core
{
    internal sealed class Spot
    {
        public string Id;
        public CollectibleKind Kind;
        public int Number;
        public Vector3 Position;
        public Prop Prop;
        public Blip Blip;
    }

    public sealed class CollectibleRunner : Script
    {
        private const int StreamInterval = 500;
        private const int HideInterval = 5000;
        private static readonly string[] GameScripts = { "spaceshipparts", "letterscraps", "ambient_sonar", "ambient_diving" };
        private readonly List<Spot> _spots = new List<Spot>();
        private readonly Dictionary<string, Spot> _byId = new Dictionary<string, Spot>(StringComparer.OrdinalIgnoreCase);
        private HashSet<CollectibleKind> _disabled = new HashSet<CollectibleKind>();
        private Model _model;
        private int _nextStream;
        private int _nextHide;
        private int _lastErrorAt = -1;
        public CollectibleRunner()
        {
            if (Instance != null) return;
            Instance = this;
            string directory = Path.Combine(BaseDirectory, "GameCollectibles");
            Log.Init(directory);
            Config = CollectiblesConfig.Load(directory);
            Log.Verbose = Config.VerboseLog;
            Store = new Store(directory);
            if (Config.SpaceshipParts) Add(CollectibleKind.SpaceshipPart, "SpaceshipPart", Locations.SpaceshipParts);
            if (Config.LetterScraps) Add(CollectibleKind.LetterScrap, "LetterScrap", Locations.LetterScraps);
            if (Config.NuclearWaste) Add(CollectibleKind.NuclearWaste, "NuclearWaste", Locations.NuclearWastes);
            if (Config.SubmarineParts) Add(CollectibleKind.SubmarinePart, "SubmarinePart", Locations.SubmarineParts);
            Log.Info($"{_spots.Count} collectibles, {CollectedCount} already collected.");
            Tick += OnTick;
            Aborted += OnAborted;
        }

        internal static CollectibleRunner Instance { get; private set; }
        internal CollectiblesConfig Config { get; }
        internal Store Store { get; }
        internal bool IsReady { get; private set; }
        internal IReadOnlyList<Spot> Spots => _spots;
        internal int CollectedCount => _spots.Count(s => Store.Contains(s.Id));

        internal Spot Find(string id) => id != null && _byId.TryGetValue(id, out Spot spot) ? spot : null;

        internal IReadOnlyCollection<CollectibleKind> DisabledKinds => _disabled;

        internal bool IsActive(Spot spot) => !_disabled.Contains(spot.Kind) && !Store.Contains(spot.Id);

        internal void SetDisabled(IEnumerable<CollectibleKind> kinds)
        {
            var next = new HashSet<CollectibleKind>(kinds ?? Enumerable.Empty<CollectibleKind>());
            if (next.SetEquals(_disabled)) return;
            _disabled = next;
            foreach (Spot spot in _spots.Where(s => _disabled.Contains(s.Kind)))
            {
                Despawn(spot);
                RemoveBlip(spot);
            }
            _nextStream = 0;
            Log.Info("Disabled kinds: " + (_disabled.Count == 0 ? "none" : string.Join(", ", _disabled)) + ".");
        }

        internal bool MarkCollected(string id)
        {
            Spot spot = Find(id);
            if (spot == null || !Store.Add(spot.Id)) return false;
            Despawn(spot);
            return true;
        }

        internal bool Restore(string id)
        {
            Spot spot = Find(id);
            return spot != null && Store.Remove(spot.Id);
        }

        internal void ReplaceCollected(IEnumerable<string> ids)
        {
            Store.Replace(ids);
            foreach (Spot spot in _spots.Where(s => Store.Contains(s.Id))) Despawn(spot);
            _nextStream = 0;
        }

        internal Spot Nearest()
        {
            Vector3 player = Game.Player.Character.Position;
            return _spots.Where(IsActive).OrderBy(s => s.Position.DistanceToSquared(player)).FirstOrDefault();
        }

        internal void RefreshBlips()
        {
            foreach (Spot spot in _spots) RemoveBlip(spot);
            _nextStream = 0;
        }

        private void Add(CollectibleKind kind, string prefix, Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                var spot = new Spot { Id = $"{prefix}_{i + 1:00}", Kind = kind, Number = i + 1, Position = positions[i] };
                _spots.Add(spot);
                _byId[spot.Id] = spot;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)) return;
            IsReady = true;
            try
            {
                int now = Game.GameTime;
                if (Config.HideGameCollectibles && now >= _nextHide)
                {
                    _nextHide = now + HideInterval;
                    HideGameScripts();
                }
                if (now >= _nextStream)
                {
                    _nextStream = now + StreamInterval;
                    Stream();
                }
                Animate(now);
            }
            catch (Exception ex)
            {
                int now = Environment.TickCount;
                if (_lastErrorAt < 0 || now - _lastErrorAt > 5000)
                {
                    _lastErrorAt = now;
                    Log.Error("Tick error.", ex);
                }
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            foreach (Spot spot in _spots)
            {
                Despawn(spot);
                RemoveBlip(spot);
            }
            if (_model.Hash != 0) _model.MarkAsNoLongerNeeded();
        }

        private void HideGameScripts()
        {
            foreach (string name in GameScripts)
            {
                int hash = unchecked((int)StringHash.AtStringHash(name, 0u));
                if (Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, hash) <= 0) continue;
                Function.Call(Hash.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME, name);
                Log.Info($"Game collectible script stopped: {name}.");
            }
        }

        private void Stream()
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            Vector3 here = player.Position;
            float stream = Config.StreamDistance;
            BlipMode mode = Config.BlipMode;
            foreach (Spot spot in _spots)
            {
                float distance = spot.Position.DistanceTo(here);
                if (!IsActive(spot))
                {
                    Despawn(spot);
                    RemoveBlip(spot);
                    continue;
                }
                if (distance < stream && (spot.Prop == null || !spot.Prop.Exists())) Spawn(spot);
                else if (distance > stream + 25f) Despawn(spot);

                bool wantBlip = mode == BlipMode.All || (mode == BlipMode.Nearby && distance < Config.BlipDistance);
                bool hasBlip = spot.Blip != null && spot.Blip.Exists();
                if (wantBlip && !hasBlip) AddBlip(spot, mode == BlipMode.Nearby);
                else if (!wantBlip && spot.Blip != null) RemoveBlip(spot);
            }
        }

        private void Animate(int now)
        {
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            Vector3 here = player.IsInVehicle() ? player.CurrentVehicle.Position : player.Position;
            float radius = Radius(player);
            float bob = (float)Math.Sin(now / 400.0) * 0.12f;
            float heading = (now / 1000f * Config.SpinSpeed) % 360f;
            foreach (Spot spot in _spots)
            {
                if (spot.Prop == null || !spot.Prop.Exists()) continue;
                Vector3 position = spot.Position + new Vector3(0f, 0f, Config.HoverHeight + bob);
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, spot.Prop, position.X, position.Y, position.Z, false, false, false);
                spot.Prop.Heading = heading;
                EntityScale.Apply(spot.Prop, heading, Config.Size);
                if (Config.Glow) Function.Call(Hash.DRAW_LIGHT_WITH_RANGE, position.X, position.Y, position.Z + 0.3f, 255, 200, 40, 3f, 2f);
                if (spot.Position.DistanceTo(here) <= radius) Collect(spot);
            }
        }

        private float Radius(Ped player)
        {
            if (Function.Call<bool>(Hash.IS_ENTITY_IN_WATER, player) || (player.IsInVehicle() && player.CurrentVehicle.Model.IsSubmarine))
                return Config.UnderwaterPickupRadius;
            return player.IsInVehicle() ? Config.VehiclePickupRadius : Config.PickupRadius;
        }

        private void Collect(Spot spot)
        {
            if (_disabled.Contains(spot.Kind) || !Store.Add(spot.Id)) return;
            Vector3 at = spot.Prop != null && spot.Prop.Exists() ? spot.Prop.Position : spot.Position;
            Despawn(spot);
            RemoveBlip(spot);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
            Effect(at);
            int kindTotal = _spots.Count(s => s.Kind == spot.Kind);
            int kindDone = _spots.Count(s => s.Kind == spot.Kind && Store.Contains(s.Id));
            if (Config.Notify)
                GTA.UI.Notification.PostTicker($"~y~{GameText.Kind(spot.Kind)}~s~ {kindDone}/{kindTotal}~n~" +
                    $"Total: {CollectedCount}/{_spots.Count}", false);
            Log.Info($"Collected {spot.Id} ({CollectedCount}/{_spots.Count}).");
            try
            {
                CollectiblesApi.RaiseCollected(spot.Id);
            }
            catch (Exception ex)
            {
                Log.Error("A Collected event handler failed.", ex);
            }
        }

        private static void Effect(Vector3 at)
        {
            const string asset = "scr_rcbarry2";
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, asset);
            if (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, asset)) return;
            Function.Call(Hash.USE_PARTICLE_FX_ASSET, asset);
            Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "scr_clown_appears", at.X, at.Y, at.Z, 0f, 0f, 0f, 0.6f, false, false, false);
        }

        private void Spawn(Spot spot)
        {
            if (_model.Hash == 0)
            {
                var model = new Model(Config.Model);
                if (!model.IsInCdImage || !model.IsValid)
                {
                    Log.Warn($"Model '{Config.Model}' does not exist, using prop_ld_case_01.");
                    Config.Model = "prop_ld_case_01";
                    model = new Model(Config.Model);
                }
                if (!model.Request(3000)) return;
                _model = model;
            }
            Prop prop = Prop.Create(_model, spot.Position + new Vector3(0f, 0f, Config.HoverHeight), false, false);
            if (prop == null) return;
            prop.IsPositionFrozen = true;
            prop.IsCollisionEnabled = false;
            Function.Call(Hash.SET_ENTITY_LOD_DIST, prop, 200);
            spot.Prop = prop;
            Log.Debug($"Spawned {spot.Id}.");
        }

        private static void Despawn(Spot spot)
        {
            if (spot.Prop != null && spot.Prop.Exists()) spot.Prop.Delete();
            spot.Prop = null;
        }

        private static void AddBlip(Spot spot, bool shortRange)
        {
            Blip blip = Blip.Create(spot.Position);
            blip.Sprite = BlipSprite.Standard;
            blip.Color = BlipColor.Yellow;
            blip.Scale = 0.6f;
            blip.IsShortRange = shortRange;
            blip.Name = GameText.Kind(spot.Kind);
            spot.Blip = blip;
        }

        private static void RemoveBlip(Spot spot)
        {
            if (spot.Blip != null && spot.Blip.Exists()) spot.Blip.Delete();
            spot.Blip = null;
        }
    }
}
