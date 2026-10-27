using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace GameTraps.Core
{
    internal sealed partial class TrapsConfig
    {
        public bool EnableMenu { get; set; }
        public string MenuKey { get; set; } = "F9";
        public bool VerboseLog { get; set; }
        public bool Announce { get; set; } = true;
        public bool RevealNames { get; set; } = true;
        public int GapBetweenTraps { get; set; } = 1500;
        public bool StackTraps { get; set; } = true;
        public int MaxActive { get; set; } = 4;
        public bool DebugAllTraps { get; set; }
        public string DebugKey { get; set; } = "F11";
        public bool AllowInMissions { get; set; }
        public List<string> MissionTraps { get; set; }
        public bool AnnounceQueue { get; set; } = true;
        public bool StreamSafe { get; set; }
        public int MaxWait { get; set; } = 180000;
        public ScooterBrotherConfig ScooterBrother { get; set; } = new ScooterBrotherConfig();
        public RampConfig Ramp { get; set; } = new RampConfig();
        public AnimalHuntConfig AnimalHunt { get; set; } = new AnimalHuntConfig();
        public AnimalTransformConfig AnimalTransform { get; set; } = new AnimalTransformConfig();
        public FallingPlanesConfig FallingPlanes { get; set; } = new FallingPlanesConfig();
        public TurboTrafficConfig TurboTraffic { get; set; } = new TurboTrafficConfig();
        public AlienAbductionConfig AlienAbduction { get; set; } = new AlienAbductionConfig();
        public IrresistibleConfig Irresistible { get; set; } = new IrresistibleConfig();
        public SecondComingConfig SecondComing { get; set; } = new SecondComingConfig();
        public WantedConfig Wanted { get; set; } = new WantedConfig();
        public DrunkConfig Drunk { get; set; } = new DrunkConfig();
        public SimpleTrapConfig FlatTires { get; set; } = new SimpleTrapConfig();
        public SimpleTrapConfig Disarmed { get; set; } = new SimpleTrapConfig();
        public LesterTaxConfig LesterTax { get; set; } = new LesterTaxConfig();
        public PaparazziConfig Paparazzi { get; set; } = new PaparazziConfig();
        public ClownsConfig Clowns { get; set; } = new ClownsConfig();
        public MoonGravityConfig MoonGravity { get; set; } = new MoonGravityConfig();
        public BlizzardConfig Blizzard { get; set; } = new BlizzardConfig();
        public NightfallConfig Nightfall { get; set; } = new NightfallConfig();
        public RagdollConfig Ragdoll { get; set; } = new RagdollConfig();
        public LiftOffConfig LiftOff { get; set; } = new LiftOffConfig();

        public static TrapsConfig Load(string directory)
        {
            string path = Path.Combine(directory, "config.json");
            TrapsConfig config;
            if (!File.Exists(path))
            {
                Log.Info("config.json not found, using defaults.");
                config = new TrapsConfig();
            }
            else
            {
                try
                {
                    config = new JavaScriptSerializer().Deserialize<TrapsConfig>(File.ReadAllText(path)) ?? new TrapsConfig();
                }
                catch (Exception ex)
                {
                    Log.Error("config.json is invalid, using defaults.", ex);
                    config = new TrapsConfig();
                }
            }
            config.Normalize();
            return config;
        }

        private void Normalize()
        {
            if (GapBetweenTraps < 0) GapBetweenTraps = 0;
            ScooterBrother = ScooterBrother ?? new ScooterBrotherConfig();
            Ramp = Ramp ?? new RampConfig();
            AnimalHunt = AnimalHunt ?? new AnimalHuntConfig();
            AnimalTransform = AnimalTransform ?? new AnimalTransformConfig();
            FallingPlanes = FallingPlanes ?? new FallingPlanesConfig();
            TurboTraffic = TurboTraffic ?? new TurboTrafficConfig();
            AlienAbduction = AlienAbduction ?? new AlienAbductionConfig();
            Irresistible = Irresistible ?? new IrresistibleConfig();
            SecondComing = SecondComing ?? new SecondComingConfig();
            Wanted = Wanted ?? new WantedConfig();
            Drunk = Drunk ?? new DrunkConfig();
            Drunk.BottleModels = Fill(Drunk.BottleModels, "ng_proc_beerbottle_01a", "ng_proc_beerbottle_01b", "prop_wine_bot_01", "prop_amb_beer_bottle");
            FlatTires = FlatTires ?? new SimpleTrapConfig();
            Disarmed = Disarmed ?? new SimpleTrapConfig();
            LesterTax = LesterTax ?? new LesterTaxConfig();
            Paparazzi = Paparazzi ?? new PaparazziConfig();
            Clowns = Clowns ?? new ClownsConfig();
            MoonGravity = MoonGravity ?? new MoonGravityConfig();
            Blizzard = Blizzard ?? new BlizzardConfig();
            Nightfall = Nightfall ?? new NightfallConfig();
            Ragdoll = Ragdoll ?? new RagdollConfig();
            LiftOff = LiftOff ?? new LiftOffConfig();
            if (MaxWait < 0) MaxWait = 0;
            NormalizeExtra();
            if (MissionTraps == null || MissionTraps.Count == 0)
                MissionTraps = new List<string>
                {
                    "FallingPlanes", "Matrix", "Terminator", "WalkingDead", "PhantomCar", "Jaws", "ObjectRain",
                    "Clowns", "MacheteGranny", "FailedHeroes", "Drunk", "MoonGravity", "Blizzard", "Nightfall", "TurboTraffic",
                    "Ragdoll", "FlatTires", "Ramp", "LiftOff", "FloatingCars", "Kamikaze", "GiantBalls", "Mariachi", "Paparazzi",
                    "UpsideDown", "LesterTax", "AnimalHunt",
                };

            ScooterBrother.Line = ScooterBrother.Line ?? new DialogueLine();
            ScooterBrother.Models = Fill(ScooterBrother.Models, "faggio2", "faggio", "faggio3");
            Ramp.Models = Fill(Ramp.Models, "prop_mp_ramp_03");
            AnimalHunt.Models = Fill(AnimalHunt.Models, "a_c_mtlion", "a_c_coyote", "a_c_rottweiler", "a_c_shepherd", "a_c_husky");
            AnimalTransform.Models = Fill(AnimalTransform.Models,
                "a_c_cat_01", "a_c_hen", "a_c_pig", "a_c_cow", "a_c_deer", "a_c_boar", "a_c_coyote", "a_c_rabbit_01",
                "a_c_chimp", "a_c_westy", "a_c_pug", "a_c_poodle", "a_c_retriever", "a_c_husky", "a_c_mtlion", "a_c_rat");
            FallingPlanes.Models = Fill(FallingPlanes.Models, "jet", "cuban800", "duster", "mammatus", "titan", "luxor", "shamal");
            if (Irresistible.Emotes == null || Irresistible.Emotes.Count == 0) Irresistible.Emotes = Emote.IrresistibleDefaults();
            Irresistible.DancerModels = Fill(Irresistible.DancerModels, "s_f_y_stripper_01", "s_f_y_stripper_02", "s_f_y_stripperlite", "mp_f_stripperlite");
            Irresistible.SafeDancerModels = Fill(Irresistible.SafeDancerModels, "a_f_y_clubcust_01", "a_f_y_clubcust_02", "a_f_y_clubcust_03", "a_f_y_clubcust_04", "a_f_y_bevhills_01");
            if (SecondComing.Emotes == null || SecondComing.Emotes.Count == 0) SecondComing.Emotes = Emote.PrayerDefaults();
            AlienAbduction.AlienWeapons = Fill(AlienAbduction.AlienWeapons, "WEAPON_RAYPISTOL");
            AlienAbduction.PlayerWeapons = Fill(AlienAbduction.PlayerWeapons, "WEAPON_RAILGUN", "WEAPON_RAYPISTOL", "WEAPON_RAYMINIGUN");
            if (AlienAbduction.Destinations == null || AlienAbduction.Destinations.Count == 0) AlienAbduction.Destinations = Destination.AbductionDefaults();
            SecondComing.WorshipperModels = Fill(SecondComing.WorshipperModels, "u_m_m_jesus_01", "a_m_y_hippy_01", "a_f_y_hippie_01", "a_m_m_salton_01", "a_f_m_salton_01");
            SecondComing.FishModels = Fill(SecondComing.FishModels, "a_c_fish", "a_c_fish", "a_c_fish", "a_c_sharktiger", "a_c_dolphin");
            if (SecondComing.ResurrectEmotes == null || SecondComing.ResurrectEmotes.Count == 0) SecondComing.ResurrectEmotes = new List<Emote>
            {
                new Emote { Scenario = "WORLD_HUMAN_CHEERING" },
                new Emote { Scenario = "WORLD_HUMAN_PARTYING" },
                new Emote { Dict = "special_ped@mountain_dancer@monologue_3@monologue_3a", Name = "mnt_dnc_buttwag" },
                new Emote { Dict = "missfbi3_sniping", Name = "dance_m_default" },
            };
            SecondComing.ChurchEffects = Fill(SecondComing.ChurchEffects, "DrugsTrevorClownsFight", "DMT_flight");
            if (SecondComing.Destinations == null || SecondComing.Destinations.Count == 0) SecondComing.Destinations = Destination.PilgrimageDefaults();
            if (SecondComing.AscendDuration < 1000) SecondComing.AscendDuration = 1000;
            if (SecondComing.VisionDuration < SecondComing.AscendDuration + 3000) SecondComing.VisionDuration = SecondComing.AscendDuration + 3000;
        }

        private static List<string> Fill(List<string> list, params string[] defaults) =>
            list == null || list.Count == 0 ? new List<string>(defaults) : list;
    }

    internal sealed class ScooterBrotherConfig
    {
        public bool Enabled { get; set; } = true;
        public float Radius { get; set; } = 100f;
        public int MaxVehicles { get; set; } = 40;
        public List<string> Models { get; set; }
        public DialogueLine Line { get; set; } = new DialogueLine();
    }

    internal sealed class DialogueLine
    {
        public bool Enabled { get; set; } = true;
        public string Block { get; set; } = "PRSAUD";
        public int Slot { get; set; } = 14;
        public string Root { get; set; } = "PST_CHSCRd";
        public string Voice { get; set; } = "TREVOR";
        public int Speaker { get; set; } = 2;
    }

    internal sealed class RampConfig
    {
        public bool Enabled { get; set; } = true;
        public List<string> Models { get; set; }
        public float HeadingOffset { get; set; }
        public float MinSpeed { get; set; } = 8f;
        public float MinDistance { get; set; } = 6f;
        public float MaxDistance { get; set; } = 20f;
        public float SecondsAhead { get; set; } = 0.45f;
        public int Lifetime { get; set; } = 20000;
    }

    internal sealed class AnimalHuntConfig
    {
        public bool Enabled { get; set; } = true;
        public int Count { get; set; } = 4;
        public float SpawnDistance { get; set; } = 35f;
        public int Duration { get; set; } = 60000;
        public List<string> Models { get; set; }
    }

    internal sealed class AnimalTransformConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public List<string> Models { get; set; }
    }

    internal sealed class FallingPlanesConfig
    {
        public bool Enabled { get; set; } = true;
        public int Count { get; set; } = 5;
        public float Height { get; set; } = 160f;
        public float SpawnRadius { get; set; } = 120f;
        public float TargetRadius { get; set; } = 25f;
        public float Speed { get; set; } = 55f;
        public int Interval { get; set; } = 1500;
        public List<string> Models { get; set; }
    }

    internal sealed class TurboTrafficConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 30000;
        public float Radius { get; set; } = 300f;
        public float Speed { get; set; } = 60f;
        public float Boost { get; set; } = 8f;
    }

    internal sealed class AlienAbductionConfig
    {
        public bool Enabled { get; set; } = true;
        public string UfoModel { get; set; } = "p_spinning_anus_s";
        public string AlienModel { get; set; } = "s_m_m_movalien_01";
        public List<string> AlienWeapons { get; set; }
        public int AlienAccuracy { get; set; } = 25;
        public List<string> PlayerWeapons { get; set; }
        public int PlayerAmmo { get; set; } = 9999;
        public bool RemoveWeaponsAfter { get; set; } = true;
        public bool InvinciblePlayer { get; set; }
        public bool CannotDie { get; set; } = true;
        public int FightHealth { get; set; } = 1000;
        public float KnockbackForce { get; set; } = 9f;
        public List<OutfitPart> AlienOutfit { get; set; }
        public int SpawnProtection { get; set; } = 5000;
        public int Aliens { get; set; } = 5;
        public float MinDistance { get; set; } = 12f;
        public float MaxDistance { get; set; } = 40f;
        public float UfoHeight { get; set; } = 35f;
        public int BeamDuration { get; set; } = 5000;
        public int FightDuration { get; set; } = 60000;
        public List<Destination> Destinations { get; set; }
    }

    internal sealed class IrresistibleConfig
    {
        public bool Enabled { get; set; } = true;
        public float Radius { get; set; } = 80f;
        public int MaxPeds { get; set; } = 10;
        public int Duration { get; set; } = 45000;
        public int EmoteChange { get; set; } = 9000;
        public int TipPerDancer { get; set; } = 10;
        public int TipInterval { get; set; } = 5000;
        public float TipDistance { get; set; } = 3f;
        public Emote TipEmote { get; set; } = new Emote { Dict = "anim@mp_player_intcelebrationmale@raining_cash", Name = "raining_cash" };
        public string TipEffectAsset { get; set; } = "scr_xs_celebration";
        public string TipEffect { get; set; } = "scr_xs_money_rain";
        public List<string> DancerModels { get; set; }
        public List<string> SafeDancerModels { get; set; }
        public List<Emote> Emotes { get; set; }
    }

    internal sealed class SecondComingConfig
    {
        public bool Enabled { get; set; } = true;
        public string Model { get; set; } = "u_m_m_jesus_01";
        public float Radius { get; set; } = 60f;
        public int MaxPeds { get; set; } = 20;
        public int AscendDuration { get; set; } = 10000;
        public float AscendSpeed { get; set; } = 1.2f;
        public string AnchorModel { get; set; } = "prop_golf_ball";
        public float AscensionBlur { get; set; } = 0.6f;
        public int Worshippers { get; set; } = 8;
        public int ResurrectDelay { get; set; } = 5000;
        public List<Emote> ResurrectEmotes { get; set; }
        public List<string> FishModels { get; set; }
        public int FishCount { get; set; } = 40;
        public int FishInterval { get; set; } = 350;
        public List<string> WorshipperModels { get; set; }
        public List<string> ChurchEffects { get; set; }
        public Emote MessiahEmote { get; set; } = new Emote { Dict = "amb@world_human_yoga@male@base", Name = "base_a" };
        public int VisionDuration { get; set; } = 45000;
        public List<Emote> Emotes { get; set; }
        public List<Destination> Destinations { get; set; }
    }

    internal sealed class SimpleTrapConfig
    {
        public bool Enabled { get; set; } = true;
    }

    internal sealed class WantedConfig
    {
        public bool Enabled { get; set; } = true;
        public int Stars { get; set; } = 2;
    }

    internal sealed class DrunkConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public string Clipset { get; set; } = "move_m@drunk@verydrunk";
        public List<string> BottleModels { get; set; }
        public string Timecycle { get; set; } = "Drunk";
    }

    internal sealed class LesterTaxConfig
    {
        public bool Enabled { get; set; } = true;
        public int Percent { get; set; } = 10;
        public int Minimum { get; set; }
    }

    internal sealed class PaparazziConfig
    {
        public bool Enabled { get; set; } = true;
        public int Count { get; set; } = 6;
        public int Cars { get; set; } = 2;
        public int Duration { get; set; } = 45000;
        public float SpawnDistance { get; set; } = 30f;
        public string Model { get; set; } = "a_m_m_paparazzi_01";
        public string CarModel { get; set; } = "blista";
    }

    internal sealed class ClownsConfig
    {
        public bool Enabled { get; set; } = true;
        public int Count { get; set; } = 4;
        public int Duration { get; set; } = 90000;
        public float SpawnDistance { get; set; } = 60f;
        public string VanModel { get; set; } = "speedo2";
        public string ClownModel { get; set; } = "s_m_y_clown_01";
        public string Weapon { get; set; } = "WEAPON_BAT";
        public int VisionDuration { get; set; }
    }

    internal sealed class LiftOffConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 10000;
        public float Radius { get; set; } = 120f;
        public float UpSpeed { get; set; } = 2.5f;
        public bool ProtectPlayer { get; set; } = true;
    }

    internal sealed class MoonGravityConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 30000;
        public int Level { get; set; } = 1;
    }

    internal sealed class BlizzardConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 120000;
        public string Weather { get; set; } = "XMAS";
        public bool SnowTracks { get; set; } = true;
        public bool GroundSnow { get; set; } = true;
        public bool IcyRoads { get; set; } = true;
        public float IcyRadius { get; set; } = 250f;
        public int GripLevel { get; set; } = 3;
    }

    internal sealed class NightfallConfig
    {
        public bool Enabled { get; set; } = true;
        public int Hour { get; set; }
    }

    internal sealed class RagdollConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 4000;
    }

    internal sealed class OutfitPart
    {
        public int Slot { get; set; }
        public int Drawable { get; set; }
        public int Texture { get; set; }
    }

    internal sealed class Emote
    {
        public string Scenario { get; set; }
        public string Dict { get; set; }
        public string Name { get; set; }

        public static List<Emote> IrresistibleDefaults() => new List<Emote>
        {
            new Emote { Dict = "mini@strip_club@private_dance@part1", Name = "priv_dance_p1" },
            new Emote { Dict = "mini@strip_club@private_dance@part2", Name = "priv_dance_p2" },
            new Emote { Dict = "mini@strip_club@private_dance@part3", Name = "priv_dance_p3" },
            new Emote { Dict = "mini@strip_club@lap_dance@ld_girl_a_song_a_p1", Name = "ld_girl_a_song_a_p1_f" },
            new Emote { Dict = "mini@strip_club@lap_dance@ld_girl_a_song_a_p2", Name = "ld_girl_a_song_a_p2_f" },
            new Emote { Dict = "mini@strip_club@pole_dance@pole_dance1", Name = "pd_dance_01" },
            new Emote { Dict = "mini@strip_club@pole_dance@pole_dance2", Name = "pd_dance_02" },
            new Emote { Dict = "mini@strip_club@pole_dance@pole_dance3", Name = "pd_dance_03" },
            new Emote { Dict = "anim@mp_player_intcelebrationfemale@blow_kiss", Name = "blow_kiss" },
        };

        public static List<Emote> PrayerDefaults() => new List<Emote>
        {
            new Emote { Dict = "amb@medic@standing@kneel@idle_a", Name = "idle_a" },
            new Emote { Dict = "rcmepsilonism8", Name = "worship_base" },
            new Emote { Scenario = "WORLD_HUMAN_YOGA" },
        };

        public override string ToString() => Scenario ?? (Dict + "/" + Name);
    }

    internal sealed class Destination
    {
        public string Name { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Heading { get; set; }
        public float Clearance { get; set; }

        public static List<Destination> PilgrimageDefaults() => new List<Destination>
        {
            new Destination { Name = "Hill Valley Church (Pacific Bluffs)", X = -1704.29f, Y = -297.78f, Z = 50.89f, Heading = 317f, Clearance = 0f },
        };

        public static List<Destination> AbductionDefaults() => new List<Destination>
        {
            new Destination { Name = "Mount Chiliad", X = 501.8f, Y = 5604.0f, Z = 797.9f, Heading = 175f },
        };
    }
}
