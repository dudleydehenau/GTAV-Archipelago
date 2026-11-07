using GTA;
using GTA.Math;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal sealed class ActivityHints
    {
        private const float Range = 30f;
        private const float ShopRange = 14f;
        private const int Interval = 500;
        private static readonly int[] StreetRaces = { 86, 87, 88, 89, 90 };
        private static readonly Vector3 NightOffroad = new Vector3(2996.8f, 2774.1f, 43.3f);
        private static readonly int[] Watched =
        {
            (int)BlipSprite.Golf, (int)BlipSprite.GolfFlag, (int)BlipSprite.Tennis, (int)BlipSprite.Yoga, (int)BlipSprite.Dart,
            (int)BlipSprite.RaceCar, (int)BlipSprite.RaceBike, (int)BlipSprite.OffRoadRaceFinish, (int)BlipSprite.Race,
            (int)BlipSprite.RaceFinish, (int)BlipSprite.RaceSea, (int)BlipSprite.Triathlon, (int)BlipSprite.ShootingRange,
            (int)BlipSprite.AmmuNationShootingRange, (int)BlipSprite.Airport, (int)BlipSprite.RaceAir, (int)BlipSprite.BaseJump,
            (int)BlipSprite.Parachute, (int)BlipSprite.MovieTheater, (int)BlipSprite.Lift, (int)BlipSprite.CarWash,
        };

        private readonly StoryGlobals _globals;
        private int _next;
        private string _hint;
        private string _logged;

        public ActivityHints(StoryGlobals globals)
        {
            _globals = globals;
        }

        public void Tick(int now)
        {
            if (now >= _next)
            {
                _next = now + Interval;
                _hint = Find();
                if (_hint != null && _hint != _logged) Log.Info("Activity hint: " + _hint);
                _logged = _hint;
            }
            if (_hint != null) GTA.UI.Screen.ShowHelpTextThisFrame(_hint, false);
        }

        public void Clear()
        {
            _hint = null;
            _logged = null;
        }

        private string Find()
        {
            if (!_globals.StaticBlipsSupported || Function.Call<bool>(Hash.GET_MISSION_FLAG)) return null;
            Ped player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return null;
            Vector3 here = player.Position;
            ShopKind? shop = GameShopWatcher.LockedNear(here, ShopRange);
            if (shop.HasValue) return ShopHint(shop.Value);
            int best = -1;
            float bestDistance = Range;
            for (int i = 0; i < _globals.StaticBlipCountValue; i++)
            {
                if (System.Array.IndexOf(Watched, _globals.StaticBlipSprite(i)) < 0) continue;
                Vector3 at = _globals.StaticBlipPosition(i);
                if (at == Vector3.Zero) continue;
                float distance = at.DistanceTo(here);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            if (best < 0) return null;
            return Condition(best, _globals.StaticBlipSprite(best), _globals.StaticBlipPosition(best), player);
        }

        private static string Condition(int index, int sprite, Vector3 at, Ped player)
        {
            int hour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            int minute = Function.Call<int>(Hash.GET_CLOCK_MINUTES);
            string now = $"{hour:00}:{minute:00}";
            PedHash who = (PedHash)player.Model.Hash;
            string name = Name(index, sprite);

            if (System.Array.IndexOf(StreetRaces, index) >= 0)
            {
                if (who != PedHash.Franklin) return "Street races are Franklin's only. Switch to Franklin to race here.";
                if (!(hour >= 20 || hour < 5)) return $"Street races only run at night, from 20:00 to 05:00 (it is {now}). Come back tonight or sleep at a safehouse.";
            }
            else if (at.DistanceTo(NightOffroad) < 60f)
            {
                if (!(hour >= 20 || hour < 3)) return $"This off-road race only runs at night, from 20:00 to 03:00 (it is {now}). Come back tonight or sleep at a safehouse.";
            }
            else if (sprite == (int)BlipSprite.Golf || sprite == (int)BlipSprite.GolfFlag)
            {
                if (hour < 6 || hour >= 18) return $"The golf club is open from 06:00 to 18:00 (it is {now}). Come back in the daytime or sleep at a safehouse.";
            }
            else if (sprite == (int)BlipSprite.Tennis)
            {
                if (who == PedHash.Franklin) return "Franklin does not play tennis. Switch to Michael or Trevor to play here.";
                if (hour < 6 || hour > 18) return $"Tennis players are only here from 06:00 to 19:00 (it is {now}). Come back in the daytime or sleep at a safehouse.";
                if (Function.Call<float>(Hash.GET_RAIN_LEVEL) + Function.Call<float>(Hash.GET_SNOW_LEVEL) > 0.1f) return "Nobody plays tennis in the rain. Come back when the weather clears.";
            }
            else if (sprite == (int)BlipSprite.Yoga)
            {
                if (who != PedHash.Michael) return "Yoga is Michael's only. Switch to Michael to do it here.";
            }
            else if (sprite == (int)BlipSprite.CarWash)
            {
                if (!player.IsInVehicle()) return "Drive in with a car to use the car wash.";
            }

            if (Game.Player.WantedLevel > 0) return $"Lose the cops first: {name} cannot start while you are wanted.";
            return null;
        }

        private static string ShopHint(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Barber: return "Barbers are closed: you have not received \"Shop: Barber\" from Archipelago yet. Every barber opens when it arrives.";
                case ShopKind.Clothes: return "Clothes stores are closed: you have not received \"Shop: Clothes Store\" from Archipelago yet. Every clothes store opens when it arrives.";
                case ShopKind.Tattoo: return "Tattoo parlors are closed: you have not received \"Shop: Tattoo Parlor\" from Archipelago yet. Every tattoo parlor opens when it arrives.";
                default: return "Los Santos Customs is closed: you have not received \"Shop: Los Santos Customs\" from Archipelago yet. Every garage opens when it arrives.";
            }
        }

        private static string Name(int index, int sprite)
        {
            if (System.Array.IndexOf(StreetRaces, index) >= 0) return "the street race";
            switch ((BlipSprite)sprite)
            {
                case BlipSprite.Golf:
                case BlipSprite.GolfFlag: return "golf";
                case BlipSprite.Tennis: return "tennis";
                case BlipSprite.Yoga: return "yoga";
                case BlipSprite.Dart: return "darts";
                case BlipSprite.Triathlon: return "the triathlon";
                case BlipSprite.ShootingRange:
                case BlipSprite.AmmuNationShootingRange: return "the shooting range";
                case BlipSprite.Airport:
                case BlipSprite.RaceAir: return "Flight School";
                case BlipSprite.BaseJump:
                case BlipSprite.Parachute: return "the jump";
                case BlipSprite.MovieTheater: return "the movie";
                case BlipSprite.Lift: return "the cable car";
                case BlipSprite.CarWash: return "the car wash";
                default: return "the race";
            }
        }
    }
}
