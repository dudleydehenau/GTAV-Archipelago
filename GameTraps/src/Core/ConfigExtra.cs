using System.Collections.Generic;

namespace GameTraps.Core
{
    internal sealed partial class TrapsConfig
    {
        public PhantomCarConfig PhantomCar { get; set; } = new PhantomCarConfig();
        public ConvoyConfig PhantomWedge { get; set; } = new ConvoyConfig();
        public RedLightConfig RedLight { get; set; } = new RedLightConfig();
        public SpeedConfig Speed { get; set; } = new SpeedConfig();
        public TimeMachineConfig TimeMachine { get; set; } = new TimeMachineConfig();
        public PurgeConfig Purge { get; set; } = new PurgeConfig();
        public TerminatorConfig Terminator { get; set; } = new TerminatorConfig();
        public MatrixConfig Matrix { get; set; } = new MatrixConfig();
        public ConvoyConfig MadMax { get; set; } = new ConvoyConfig();
        public JawsConfig Jaws { get; set; } = new JawsConfig();
        public SquadConfig MacheteGranny { get; set; } = new SquadConfig();
        public HereWeGoAgainConfig HereWeGoAgain { get; set; } = new HereWeGoAgainConfig();
        public RideConfig Bowling { get; set; } = new RideConfig();
        public ViceCityConfig ViceCity { get; set; } = new ViceCityConfig();
        public BigSmokeConfig BigSmoke { get; set; } = new BigSmokeConfig();
        public BattleRoyaleConfig BattleRoyale { get; set; } = new BattleRoyaleConfig();
        public MarioKartConfig MarioKart { get; set; } = new MarioKartConfig();
        public CameraTrapConfig TopDown { get; set; } = new CameraTrapConfig();
        public CameraTrapConfig UpsideDown { get; set; } = new CameraTrapConfig();
        public ClonesConfig Clones { get; set; } = new ClonesConfig();
        public RideConfig Kidnap { get; set; } = new RideConfig();
        public RainConfig ObjectRain { get; set; } = new RainConfig();
        public GiantBallsConfig GiantBalls { get; set; } = new GiantBallsConfig();
        public SquadConfig FailedHeroes { get; set; } = new SquadConfig();
        public MariachiConfig Mariachi { get; set; } = new MariachiConfig();
        public FloatingCarsConfig FloatingCars { get; set; } = new FloatingCarsConfig();
        public KamikazeConfig Kamikaze { get; set; } = new KamikazeConfig();
        public FlashMobConfig FlashMob { get; set; } = new FlashMobConfig();
        public InitialDConfig InitialD { get; set; } = new InitialDConfig();
        public ForrestGumpConfig ForrestGump { get; set; } = new ForrestGumpConfig();
        public WalkingDeadConfig WalkingDead { get; set; } = new WalkingDeadConfig();

        private void NormalizeExtra()
        {
            PhantomCar = PhantomCar ?? new PhantomCarConfig();
            PhantomWedge = PhantomWedge ?? new ConvoyConfig();
            RedLight = RedLight ?? new RedLightConfig();
            Speed = Speed ?? new SpeedConfig();
            TimeMachine = TimeMachine ?? new TimeMachineConfig();
            Purge = Purge ?? new PurgeConfig();
            Terminator = Terminator ?? new TerminatorConfig();
            Matrix = Matrix ?? new MatrixConfig();
            MadMax = MadMax ?? new ConvoyConfig();
            Jaws = Jaws ?? new JawsConfig();
            MacheteGranny = MacheteGranny ?? new SquadConfig();
            HereWeGoAgain = HereWeGoAgain ?? new HereWeGoAgainConfig();
            Bowling = Bowling ?? new RideConfig();
            ViceCity = ViceCity ?? new ViceCityConfig();
            BigSmoke = BigSmoke ?? new BigSmokeConfig();
            BattleRoyale = BattleRoyale ?? new BattleRoyaleConfig();
            MarioKart = MarioKart ?? new MarioKartConfig();
            TopDown = TopDown ?? new CameraTrapConfig();
            UpsideDown = UpsideDown ?? new CameraTrapConfig();
            Clones = Clones ?? new ClonesConfig();
            Kidnap = Kidnap ?? new RideConfig();
            ObjectRain = ObjectRain ?? new RainConfig();
            GiantBalls = GiantBalls ?? new GiantBallsConfig();
            FailedHeroes = FailedHeroes ?? new SquadConfig();
            Mariachi = Mariachi ?? new MariachiConfig();
            FloatingCars = FloatingCars ?? new FloatingCarsConfig();
            Kamikaze = Kamikaze ?? new KamikazeConfig();
            FlashMob = FlashMob ?? new FlashMobConfig();
            InitialD = InitialD ?? new InitialDConfig();
            ForrestGump = ForrestGump ?? new ForrestGumpConfig();
            WalkingDead = WalkingDead ?? new WalkingDeadConfig();
            InitialD.Models = Fill(InitialD.Models, "futo");
            InitialD.Delivery = InitialD.Delivery ?? new Destination { Name = "Fujiwara Tofu Shop", X = 300f, Y = 180f, Z = 104f };
            InitialD.RivalModels = Fill(InitialD.RivalModels, "zr350", "elegy", "sultan");
            if (InitialD.Destinations == null || InitialD.Destinations.Count == 0) InitialD.Destinations = new List<Destination>
            {
                new Destination { Name = "Galileo Observatory", X = -411f, Y = 1176f, Z = 325.6f },
            };

            PhantomCar.Models = Fill(PhantomCar.Models, "romero", "hearse");

            PhantomWedge.Defaults(60000, 5, 0, 0);
            PhantomWedge.Waved(5, 9000);
            if (PhantomWedge.RamSpeed <= 0f) PhantomWedge.RamSpeed = 42f;
            PhantomWedge.VehicleModels = Fill(PhantomWedge.VehicleModels, "phantom2", "phantom");
            PhantomWedge.PedModels = Fill(PhantomWedge.PedModels, "s_m_y_dockwork_01", "s_m_m_trucker_01");

            RedLight.Weapon = string.IsNullOrEmpty(RedLight.Weapon) ? "WEAPON_SNIPERRIFLE" : RedLight.Weapon;

            Speed.BusModels = Fill(Speed.BusModels, "bus", "coach");

            TimeMachine.Models = Fill(TimeMachine.Models, "deluxo");
            TimeMachine.OldCars = Fill(TimeMachine.OldCars, "tornado", "tornado2", "peyote", "manana", "btype", "roosevelt", "virgo", "stinger", "casco", "ztype");
            TimeMachine.Past = TimeMachine.Past ?? new Destination { Name = "North Yankton", X = 3360.19f, Y = -4849.67f, Z = 111.8f };
            TimeMachine.Present = TimeMachine.Present ?? new Destination { Name = "Sandy Shores", X = 1850f, Y = 3700f, Z = 33f, Heading = 120f, Clearance = 4f };

            Purge.Models = Fill(Purge.Models, "g_m_y_lost_01", "g_m_y_lost_02", "g_m_y_lost_03", "g_m_y_salvagoon_01", "g_m_y_salvagoon_02");
            Purge.Weapons = Fill(Purge.Weapons, "WEAPON_MACHETE", "WEAPON_BAT", "WEAPON_HATCHET", "WEAPON_PISTOL", "WEAPON_SAWNOFFSHOTGUN");
            Purge.CrowdWeapons = Fill(Purge.CrowdWeapons, "WEAPON_BAT", "WEAPON_KNIFE", "WEAPON_GOLFCLUB", "WEAPON_CROWBAR", "WEAPON_PISTOL");

            Terminator.Model = string.IsNullOrEmpty(Terminator.Model) ? "s_m_m_bouncer_01" : Terminator.Model;
            Terminator.BikeModel = Terminator.BikeModel ?? "bagger";
            Terminator.Weapon = string.IsNullOrEmpty(Terminator.Weapon) ? "WEAPON_PUMPSHOTGUN" : Terminator.Weapon;

            Matrix.Models = Fill(Matrix.Models, "s_m_m_highsec_01", "s_m_m_highsec_02", "s_m_m_fiboffice_01");
            Matrix.Weapons = Fill(Matrix.Weapons, "WEAPON_PISTOL", "WEAPON_COMBATPISTOL");

            MadMax.Defaults(75000, 3, 2, 15);
            MadMax.Waved(5, 15000);
            MadMax.VehicleModels = Fill(MadMax.VehicleModels, "ratloader", "ratloader2", "dune", "bfinjection", "rebel", "technical");
            MadMax.PedModels = Fill(MadMax.PedModels, "a_m_m_hillbilly_01", "a_m_m_hillbilly_02", "a_m_y_methhead_01", "g_m_y_lost_01", "u_m_y_hippie_01");
            MadMax.Weapons = Fill(MadMax.Weapons, "WEAPON_SAWNOFFSHOTGUN", "WEAPON_MICROSMG", "WEAPON_MOLOTOV");

            Jaws.SharkModels = Fill(Jaws.SharkModels, "a_c_sharktiger", "a_c_sharkhammer");

            MacheteGranny.Defaults(60000, 5, 45f);
            MacheteGranny.Models = Fill(MacheteGranny.Models, "a_f_o_genstreet_01", "a_f_o_soucent_01", "a_f_o_soucent_02", "a_f_o_salton_01", "a_f_o_ktown_01", "a_f_o_indian_01");
            MacheteGranny.Weapons = Fill(MacheteGranny.Weapons, "WEAPON_MACHETE");
            if (MacheteGranny.MoveRate <= 0f) MacheteGranny.MoveRate = 1.2f;

            HereWeGoAgain.Defaults(60000, 2, 3, 20);
            HereWeGoAgain.Waved(3, 20000);
            HereWeGoAgain.VehicleModels = Fill(HereWeGoAgain.VehicleModels, "buccaneer", "voodoo", "emperor", "manana");
            HereWeGoAgain.PedModels = Fill(HereWeGoAgain.PedModels, "g_m_y_ballaorig_01", "g_m_y_ballaeast_01", "g_m_y_ballasout_01");
            HereWeGoAgain.Weapons = Fill(HereWeGoAgain.Weapons, "WEAPON_MICROSMG", "WEAPON_PISTOL");
            if (HereWeGoAgain.Color < 0) HereWeGoAgain.Color = 145;
            HereWeGoAgain.FriendModels = Fill(HereWeGoAgain.FriendModels, "g_m_y_famca_01", "g_m_y_famdnf_01", "g_m_y_famfor_01");
            HereWeGoAgain.FriendWeapons = Fill(HereWeGoAgain.FriendWeapons, "WEAPON_PISTOL", "WEAPON_MICROSMG");

            Bowling.Defaults(35000, "taxi", "s_m_m_gentransport", 22f, false);

            ViceCity.VehicleModels = Fill(ViceCity.VehicleModels, "infernus");
            ViceCity.CopModels = Fill(ViceCity.CopModels, "s_m_y_cop_01");

            BigSmoke.FoodModels = Fill(BigSmoke.FoodModels, "prop_cs_burger_01", "ng_proc_food_burg01a", "ng_proc_food_burg02a", "ng_proc_food_chips01a", "ng_proc_sodacan_01a");

            MarioKart.PeelModels = Fill(MarioKart.PeelModels, "ng_proc_food_nana1a", "prop_bananas", "prop_food_bs_chips");

            if (TopDown.Duration <= 0) TopDown.Duration = 30000;
            if (TopDown.Height <= 0f) TopDown.Height = 35f;
            if (UpsideDown.Duration <= 0) UpsideDown.Duration = 20000;

            Clones.Weapon = Clones.Weapon ?? string.Empty;
            Clones.PoliceCars = Fill(Clones.PoliceCars, "police", "police2", "police3");

            Kidnap.Defaults(30000, "asterope", "a_m_y_stlat_01", 45f, true);

            ObjectRain.Defaults(30000, 45, 400);
            ObjectRain.PropModels = Fill(ObjectRain.PropModels, "prop_toilet_01", "prop_ld_toilet_01", "prop_rub_trolley01a", "prop_bin_01a", "prop_tv_flat_01", "prop_couch_01", "prop_wheelchair_01", "prop_beach_ball_01");
            ObjectRain.PedModels = Fill(ObjectRain.PedModels, "a_c_cow", "a_c_pig", "a_c_hen");

            GiantBalls.Models = Fill(GiantBalls.Models, "stt_prop_stunt_soccer_lball", "stt_prop_stunt_soccer_ball", "stt_prop_stunt_soccer_sball", "prop_beach_ball_01");

            FailedHeroes.Defaults(60000, 4, 40f);
            FailedHeroes.Models = Fill(FailedHeroes.Models, "u_m_y_imporage", "u_m_y_pogo_01");
            FailedHeroes.Weapons = Fill(FailedHeroes.Weapons, "WEAPON_UNARMED", "WEAPON_BAT");
            if (FailedHeroes.MoveRate <= 0f) FailedHeroes.MoveRate = 1.1f;

            Kamikaze.Models = Fill(Kamikaze.Models, "a_m_y_hipster_01", "a_m_m_tourist_01", "a_f_y_tourist_01", "a_m_y_beach_01", "a_m_m_fatlatin_01");

            Mariachi.Models = Fill(Mariachi.Models, "s_m_m_mariachi_01");

            if (FlashMob.Emotes == null || FlashMob.Emotes.Count == 0) FlashMob.Emotes = new List<Emote>
            {
                new Emote { Dict = "special_ped@mountain_dancer@monologue_3@monologue_3a", Name = "mnt_dnc_buttwag" },
                new Emote { Dict = "missfbi3_sniping", Name = "dance_m_default" },
                new Emote { Dict = "anim@mp_player_intcelebrationmale@chicken_taunt", Name = "chicken_taunt" },
                new Emote { Scenario = "WORLD_HUMAN_PARTYING" },
            };
        }
    }

    internal sealed class SquadConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; }
        public int Count { get; set; }
        public float SpawnDistance { get; set; }
        public bool Respawn { get; set; } = true;
        public float MoveRate { get; set; }
        public int Accuracy { get; set; } = 20;
        public List<string> Models { get; set; }
        public List<string> Weapons { get; set; }

        public void Defaults(int duration, int count, float distance)
        {
            if (Duration <= 0) Duration = duration;
            if (Count <= 0) Count = count;
            if (SpawnDistance <= 0f) SpawnDistance = distance;
        }
    }

    internal class ConvoyConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; }
        public int Cars { get; set; }
        public int Passengers { get; set; } = -1;
        public int Accuracy { get; set; } = -1;
        public int Color { get; set; } = -1;
        public float SpawnDistance { get; set; } = 70f;
        public float Boost { get; set; } = 4f;
        public float RamSpeed { get; set; }
        public int MaxVehicles { get; set; } = 30;
        public int Waves { get; set; }
        public int WaveInterval { get; set; }
        public List<string> VehicleModels { get; set; }
        public List<string> PedModels { get; set; }
        public List<string> Weapons { get; set; }

        public void Waved(int waves, int interval)
        {
            if (Waves <= 0) Waves = waves;
            if (WaveInterval <= 0) WaveInterval = interval;
        }

        public void Defaults(int duration, int cars, int passengers, int accuracy)
        {
            if (Duration <= 0) Duration = duration;
            if (Cars <= 0) Cars = cars;
            if (Passengers < 0) Passengers = passengers;
            if (Accuracy < 0) Accuracy = accuracy;
        }
    }

    internal sealed class HereWeGoAgainConfig : ConvoyConfig
    {
        public int Friends { get; set; } = 2;
        public List<string> FriendModels { get; set; }
        public List<string> FriendWeapons { get; set; }
    }

    internal sealed class RainConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; }
        public int Count { get; set; }
        public int Interval { get; set; }
        public float Spread { get; set; } = 18f;
        public float MinHeight { get; set; } = 18f;
        public float MaxHeight { get; set; } = 32f;
        public List<string> PropModels { get; set; }
        public List<string> PedModels { get; set; }

        public void Defaults(int duration, int count, int interval)
        {
            if (Duration <= 0) Duration = duration;
            if (Count <= 0) Count = count;
            if (Interval <= 0) Interval = interval;
        }
    }

    internal sealed class RideConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; }
        public string VehicleModel { get; set; }
        public string DriverModel { get; set; }
        public float Speed { get; set; }
        public bool Crazy { get; set; }

        public void Defaults(int duration, string vehicle, string driver, float speed, bool crazy)
        {
            if (Duration <= 0) Duration = duration;
            if (string.IsNullOrEmpty(VehicleModel)) VehicleModel = vehicle;
            if (string.IsNullOrEmpty(DriverModel)) DriverModel = driver;
            if (Speed <= 0f)
            {
                Speed = speed;
                Crazy = crazy;
            }
        }
    }

    internal sealed class CameraTrapConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; }
        public float Height { get; set; }
    }

    internal sealed class PhantomCarConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public float Speed { get; set; } = 30f;
        public float ChargeDistance { get; set; } = 45f;
        public int VanishInterval { get; set; } = 12000;
        public string Timecycle { get; set; } = "glasses_red";
        public string Weather { get; set; } = "FOGGY";
        public int Hour { get; set; } = 0;
        public List<string> Models { get; set; }
    }

    internal sealed class RedLightConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public int MinGreen { get; set; } = 3000;
        public int MaxGreen { get; set; } = 6000;
        public int MinRed { get; set; } = 3000;
        public int MaxRed { get; set; } = 5000;
        public int Grace { get; set; } = 700;
        public float MoveTolerance { get; set; } = 0.6f;
        public int Damage { get; set; } = 25;
        public int ShotInterval { get; set; } = 600;
        public string Weapon { get; set; }
    }

    internal sealed class SpeedConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 45000;
        public float MinSpeedKmh { get; set; } = 60f;
        public int ArmDelay { get; set; } = 15000;
        public int Tolerance { get; set; } = 1500;
        public bool SurviveBlast { get; set; }
        public List<string> BusModels { get; set; }
    }

    internal sealed class TimeMachineConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 90000;
        public float TargetMph { get; set; } = 88f;
        public float Boost { get; set; } = 7f;
        public float PowerMultiplier { get; set; } = 2f;
        public bool FireTrails { get; set; } = true;
        public int PastDuration { get; set; } = 120000;
        public string PastTimecycle { get; set; } = "NG_filmnoir_BW01";
        public string PastWeather { get; set; } = "SNOWLIGHT";
        public int Year { get; set; } = 1955;
        public int OldCarCount { get; set; } = 10;
        public string OldDriver { get; set; } = "a_m_m_farmer_01";
        public int FarWestYear { get; set; } = 1885;
        public string FarWestTimecycle { get; set; } = "glasses_brown";
        public List<string> Models { get; set; }
        public List<string> OldCars { get; set; }
        public Destination Past { get; set; }
        public Destination Present { get; set; }
    }

    internal sealed class PurgeConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public int Hour { get; set; } = 23;
        public int Hunters { get; set; } = 4;
        public float CrowdRadius { get; set; } = 80f;
        public int CrowdMax { get; set; } = 20;
        public bool Alarm { get; set; } = true;
        public List<string> Models { get; set; }
        public List<string> Weapons { get; set; }
        public List<string> CrowdWeapons { get; set; }
    }

    internal sealed class TerminatorConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public string Model { get; set; }
        public string BikeModel { get; set; }
        public string Weapon { get; set; }
        public int Accuracy { get; set; } = 8;
        public float SpawnDistance { get; set; } = 30f;
        public float CatchUpDistance { get; set; } = 120f;
    }

    internal sealed class MatrixConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 40000;
        public float TimeScale { get; set; } = 0.45f;
        public int Agents { get; set; } = 3;
        public int MaxAgents { get; set; } = 10;
        public int Accuracy { get; set; } = 15;
        public string Timecycle { get; set; } = "glasses_green";
        public List<string> Models { get; set; }
        public List<string> Weapons { get; set; }
    }

    internal sealed class JawsConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 40000;
        public int RainInterval { get; set; } = 600;
        public float Lead { get; set; } = 1.8f;
        public int MaxFallen { get; set; } = 30;
        public List<string> SharkModels { get; set; }
    }

    internal sealed class ViceCityConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public string Timecycle { get; set; } = "glasses_pink";
        public string Weather { get; set; } = "RAIN";
        public int Hour { get; set; } = 22;
        public int Cars { get; set; } = 2;
        public int Stars { get; set; } = 2;
        public int Lights { get; set; } = 12;
        public float NeonRadius { get; set; } = 80f;
        public List<string> VehicleModels { get; set; }
        public List<string> CopModels { get; set; }
    }

    internal sealed class BigSmokeConfig
    {
        public bool Enabled { get; set; } = true;
        public string Model { get; set; } = "u_m_y_burgerdrug_01";
        public int Count { get; set; } = 40;
        public int Interval { get; set; } = 250;
        public float ThrowSpeed { get; set; } = 14f;
        public List<string> FoodModels { get; set; }
    }

    internal sealed class BattleRoyaleConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 90000;
        public float StartRadius { get; set; } = 200f;
        public float EndRadius { get; set; } = 30f;
        public float MinOffset { get; set; } = 320f;
        public float MaxOffset { get; set; } = 450f;
        public string EnemyModel { get; set; } = "ig_jimmydisanto";
        public string EnemyName { get; set; } = "Jimmy";
        public string EnemyWeapon { get; set; } = "WEAPON_PISTOL";
        public int EnemyHealth { get; set; } = 400;
        public int Grace { get; set; } = 30000;
        public int DamagePerTick { get; set; } = 4;
        public int DamageInterval { get; set; } = 600;
    }

    internal sealed class MarioKartConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 45000;
        public int Peels { get; set; } = 12;
        public int PeelInterval { get; set; } = 2500;
        public int SpinTime { get; set; } = 1500;
        public bool BlueShell { get; set; } = true;
        public int ShellDelay { get; set; } = 20000;
        public float ShellSpeed { get; set; } = 32f;
        public float ShellTurnRate { get; set; } = 1.2f;
        public int ShellLifetime { get; set; } = 15000;
        public List<string> PeelModels { get; set; }
    }

    internal sealed class ClonesConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public int Count { get; set; } = 10;
        public int Stars { get; set; } = 2;
        public int PoliceUnits { get; set; } = 3;
        public string CopModel { get; set; } = "s_m_y_cop_01";
        public List<string> PoliceCars { get; set; }
        public string Weapon { get; set; }
    }

    internal sealed class GiantBallsConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 30000;
        public int Interval { get; set; } = 3500;
        public float Distance { get; set; } = 45f;
        public float Speed { get; set; } = 16f;
        public int MaxBalls { get; set; } = 8;
        public List<string> Models { get; set; }
    }

    internal sealed class MariachiConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public int Count { get; set; } = 3;
        public float FollowDistance { get; set; } = 9f;
        public string Scenario { get; set; } = "WORLD_HUMAN_MUSICIAN";
        public List<string> Models { get; set; }
    }

    internal sealed class FloatingCarsConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 25000;
        public float Radius { get; set; } = 120f;
        public float HoverHeight { get; set; } = 6f;
        public float UpSpeed { get; set; } = 1.5f;
        public bool IncludePlayer { get; set; }
    }

    internal sealed class KamikazeConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 30000;
        public int Interval { get; set; } = 1200;
        public float MinAhead { get; set; } = 25f;
        public float MaxAhead { get; set; } = 45f;
        public List<string> Models { get; set; }
    }

    internal sealed class FlashMobConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 40000;
        public float Radius { get; set; } = 60f;
        public int MaxPeds { get; set; } = 25;
        public int SongLength { get; set; } = 8000;
        public float FollowDistance { get; set; } = 7f;
        public List<Emote> Emotes { get; set; }
    }

    internal sealed class InitialDConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 120000;
        public int Hour { get; set; } = 4;
        public bool Drift { get; set; } = true;
        public float DriftSlide { get; set; } = 0.25f;
        public float MaxDriftAngle { get; set; } = 35f;
        public float MaxYawRate { get; set; } = 2.2f;
        public int Reward { get; set; } = 5000;
        public float DeliveryRadius { get; set; } = 8f;
        public Destination Delivery { get; set; }
        public float SpillThreshold { get; set; } = 10f;
        public float SpillRate { get; set; } = 1.2f;
        public string RivalDriver { get; set; } = "a_m_y_ktown_01";
        public List<string> Models { get; set; }
        public List<string> RivalModels { get; set; }
        public List<Destination> Destinations { get; set; }
    }

    internal sealed class ForrestGumpConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 45000;
        public float Radius { get; set; } = 80f;
        public int MaxPeds { get; set; } = 20;
        public int MinFollowers { get; set; } = 10;
        public float FollowDistance { get; set; } = 3f;
        public float RunSpeed { get; set; } = 3f;
    }

    internal sealed class WalkingDeadConfig
    {
        public bool Enabled { get; set; } = true;
        public int Duration { get; set; } = 60000;
        public string Model { get; set; } = "u_m_y_zombie_01";
        public string Clipset { get; set; } = "move_m@drunk@verydrunk";
        public int Count { get; set; } = 14;
        public int Convert { get; set; } = 10;
        public float Radius { get; set; } = 60f;
        public float SpawnDistance { get; set; } = 35f;
        public float MoveRate { get; set; } = 0.85f;
        public int Health { get; set; } = 250;
        public int Hour { get; set; } = 23;
        public string Weather { get; set; } = "HALLOWEEN";
        public bool Blackout { get; set; } = true;
        public string Timecycle { get; set; } = "NG_filmnoir_BW01";
        public float TimecycleStrength { get; set; } = 0.35f;
    }
}
