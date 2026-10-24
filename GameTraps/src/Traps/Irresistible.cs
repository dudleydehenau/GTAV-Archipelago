using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GameTraps.Core;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace GameTraps.Traps
{
    internal sealed class Irresistible : Trap
    {
        private const float CloseDistance = 2.2f;
        private readonly EntityTracker _entities = new EntityTracker();
        private readonly List<Admirer> _admirers = new List<Admirer>();
        private readonly TextElement _heart = new TextElement("<3", PointF.Empty, 0.55f, Color.FromArgb(240, 255, 105, 180), GTA.UI.Font.ChaletComprimeCologne, Alignment.Center, true, true);
        private int _until;
        private int _nextTip;
        private int _tipped;

        public Irresistible(TrapsConfig config) : base(config)
        {
        }

        private IrresistibleConfig Settings => Config.Irresistible;
        public override string Id => TrapIds.Irresistible;
        public override string Title => "Irresistible";
        public override string Subtitle => "Private dances, Vanilla Unicorn style.";
        public override bool Enabled => Settings.Enabled;

        public override bool CanStart(out string reason)
        {
            reason = null;
            if (Spawner.AmbientPeds(Settings.Radius, 1, p => !p.IsInVehicle()).Count == 0)
                reason = "nobody around";
            return reason == null;
        }

        public override void Start()
        {
            _admirers.Clear();
            List<string> names = Config.StreamSafe ? Settings.SafeDancerModels.Where(m => !IsRisky(m)).ToList() : Settings.DancerModels;
            List<Model> models = Spawner.LoadAll(names);
            if (models.Count == 0) return;
            var picked = Spawner.AmbientPeds(Settings.Radius, Settings.MaxPeds, p => !p.IsInVehicle() && !Function.Call<bool>(Hash.IS_PED_MALE, p));
            if (picked.Count < Settings.MaxPeds)
                picked.AddRange(Spawner.AmbientPeds(Settings.Radius, Settings.MaxPeds - picked.Count, p => !p.IsInVehicle() && Function.Call<bool>(Hash.IS_PED_MALE, p)));
            foreach (Ped ped in picked)
            {
                Ped dancer = Spawner.ReplacePed(ped, Spawner.Pick(models));
                if (dancer == null) continue;
                Spawner.Poof(dancer.Position);
                _entities.Add(dancer);
                dancer.BlockPermanentEvents = true;
                _admirers.Add(new Admirer { Ped = dancer, NextAction = Game.GameTime + 1500 });
                Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, dancer, "GENERIC_HI", "SPEECH_PARAMS_FORCE_SHOUTED");
            }
            _until = StartTimer(Settings.Duration);
            _nextTip = Game.GameTime + Settings.TipInterval;
            _tipped = 0;
            Log.Info($"Irresistible: {_admirers.Count} dancers ({(Config.StreamSafe ? "stream safe" : "club outfits")}).");
        }

        public override bool Update()
        {
            if (Game.GameTime >= _until) return false;
            Ped player = Player;
            Vector3 target = player.Position;
            bool anyone = false;
            int close = 0;
            foreach (Admirer admirer in _admirers)
            {
                Ped ped = admirer.Ped;
                if (!ped.Exists() || ped.IsDead) continue;
                anyone = true;
                DrawHeart(ped);
                if (ped.Position.DistanceTo(target) <= System.Math.Max(Settings.TipDistance, CloseDistance + 1.5f)) close++;
                if (Game.GameTime < admirer.NextAction || ped.IsInVehicle()) continue;
                float distance = ped.Position.DistanceTo(target);
                if (distance <= CloseDistance)
                {
                    ped.Heading = Spawner.HeadingTowards(ped.Position, player.Position);
                    Emotes.PlayRandom(ped, Settings.Emotes);
                    if (Spawner.Random.Next(3) == 0) Function.Call(Hash.PLAY_PED_AMBIENT_SPEECH_NATIVE, ped, "GENERIC_HOWS_IT_GOING", "SPEECH_PARAMS_FORCE");
                    admirer.NextAction = Game.GameTime + Settings.EmoteChange;
                }
                else
                {
                    Function.Call(Hash.TASK_GO_TO_ENTITY, ped, player, -1, CloseDistance * 0.7f, distance > 12f ? 3f : 2f, 0f, 0);
                    admirer.NextAction = Game.GameTime + 1000;
                }
            }
            if (Game.GameTime >= _nextTip)
            {
                _nextTip = Game.GameTime + Settings.TipInterval;
                if (close > 0) Tip(player, close);
            }
            return anyone;
        }

        public override void Stop(bool interrupted)
        {
            if (_tipped > 0) Log.Info($"Irresistible: ${_tipped} thrown at the dancers.");
            _admirers.Clear();
            _entities.ReleaseAll();
        }

        private void Tip(Ped player, int dancers)
        {
            int amount = Wallet.Take(dancers * Settings.TipPerDancer);
            if (amount <= 0) return;
            _tipped += amount;
            if (!player.IsInVehicle())
            {
                if (Settings.TipEmote != null && !string.IsNullOrEmpty(Settings.TipEmote.Dict) && LoadDict(Settings.TipEmote.Dict))
                    Function.Call(Hash.TASK_PLAY_ANIM, player, Settings.TipEmote.Dict, Settings.TipEmote.Name, 8f, -8f, 3000, 48, 0f, false, false, false);
                Spawner.Ptfx(Settings.TipEffectAsset, Settings.TipEffect, player.Position + player.ForwardVector * 0.4f + new Vector3(0f, 0f, 0.6f), 1f);
            }
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "LOCAL_PLYR_CASH_COUNTER_COMPLETE", "DLC_HEISTS_GENERAL_FRONTEND_SOUNDS", true);
        }

        private static bool LoadDict(string dict)
        {
            if (!Function.Call<bool>(Hash.DOES_ANIM_DICT_EXIST, dict)) return false;
            Function.Call(Hash.REQUEST_ANIM_DICT, dict);
            int deadline = System.Environment.TickCount + 1000;
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict) && System.Environment.TickCount < deadline) Script.Wait(0);
            return Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict);
        }

        private static bool IsRisky(string model)
        {
            string name = model.ToLowerInvariant();
            bool risky = name.Contains("stripper") || name.Contains("hooker") || name.Contains("topless");
            if (risky) Log.Warn($"Model {model} skipped (StreamSafe).");
            return risky;
        }

        private void DrawHeart(Ped ped)
        {
            Vector3 head = ped.Position + new Vector3(0f, 0f, 1.15f);
            var x = new OutputArgument();
            var y = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_SCREEN_COORD_FROM_WORLD_COORD, head.X, head.Y, head.Z, x, y)) return;
            _heart.Position = new PointF(x.GetResult<float>() * 1280f, y.GetResult<float>() * 720f);
            _heart.Draw();
        }

        private sealed class Admirer
        {
            public Ped Ped;
            public int NextAction;
        }
    }
}
