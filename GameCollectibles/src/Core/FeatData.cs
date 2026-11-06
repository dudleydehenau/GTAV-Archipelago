using System.Collections.Generic;
using GTA.Math;

namespace GameCollectibles.Core
{
    internal sealed class Landmark
    {
        public Landmark(string key, string name, Vector3 position, float radius, float minHeight)
        {
            Id = "Landmark_" + key;
            Name = name;
            Position = position;
            Radius = radius;
            MinHeight = minHeight;
        }

        public string Id { get; }
        public string Name { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
        public float MinHeight { get; }
    }

    internal sealed class Skill
    {
        public Skill(string name, string stat)
        {
            Name = name;
            Stat = stat;
            Key = name.Replace(" ", string.Empty);
        }

        public string Name { get; }
        public string Stat { get; }
        public string Key { get; }
    }

    internal static class FeatData
    {
        public static readonly string[] Characters = { "Michael", "Franklin", "Trevor" };

        public static readonly int[] KnifeFlightBits = { 33, 34, 35, 36, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60 };

        public static readonly Vector3[] BridgePoints =
        {
            new Vector3(1083f, -231f, 60f),
            new Vector3(1024f, -325f, 60f),
            new Vector3(910f, -401f, 43f),
            new Vector3(721f, -457f, 26f),
            new Vector3(643f, -579f, 26f),
            new Vector3(590f, -851f, 26f),
            new Vector3(590f, -1023f, 16f),
            new Vector3(582f, -1205f, 24f),
            new Vector3(608f, -1335f, 16f),
            new Vector3(640f, -1434f, 16f),
            new Vector3(671f, -1742f, 20f),
            new Vector3(651f, -2046f, 16f),
            new Vector3(603f, -2505f, 9f),
            new Vector3(673f, -2582f, 4f),
            new Vector3(728f, -2594f, 10f),
            new Vector3(794f, -2624f, 27f),
            new Vector3(-2663f, 2594f, 7.5f),
            new Vector3(-1902f, 4617f, 30f),
            new Vector3(-513f, 4427f, 40f),
            new Vector3(126f, 3366f, 40f),
            new Vector3(143f, 3418f, 36f),
            new Vector3(2822f, 4978f, 40f),
            new Vector3(-162f, 4249f, 40f),
            new Vector3(-408f, 2964f, 20f),
            new Vector3(-181f, 2862f, 38f),
            new Vector3(2558f, 2201f, 24f),
            new Vector3(2950f, 803f, 8f),
            new Vector3(2369f, -409f, 80f),
            new Vector3(1906f, -755f, 84f),
            new Vector3(-403f, -2333f, 40f),
            new Vector3(-1429f, 2649f, 10f),
            new Vector3(219f, -2315f, 5f),
            new Vector3(350f, -2315f, 5f),
            new Vector3(-1848f, -333f, 75f),
            new Vector3(-693f, -608f, 69f),
            new Vector3(-1461f, -582f, 53f),
            new Vector3(-1553f, -546f, 59f),
            new Vector3(338f, -2758f, 23f),
            new Vector3(1985f, 6201f, 53f),
            new Vector3(-713f, -1538f, 13f),
            new Vector3(-659f, -1518f, 13f),
            new Vector3(-620f, -1502f, 16f),
            new Vector3(-445f, -1575f, 26f),
            new Vector3(-373f, -1680f, 19f),
            new Vector3(-212f, -1805f, 29f),
            new Vector3(47f, -2040f, 18f),
            new Vector3(-3080f, 766f, 25f),
            new Vector3(-1478f, 2400f, 20f),
            new Vector3(2308f, 1124f, 78f),
            new Vector3(2349f, 1174f, 79f),
            new Vector3(-1186f, -365f, 46f),
            new Vector3(-916f, -407f, 93f),
            new Vector3(-726f, 235f, 105f),
            new Vector3(-774f, 286f, 112f),
            new Vector3(271f, 134f, 125f),
            new Vector3(377f, -28f, 125f),
            new Vector3(121f, -703f, 150f),
            new Vector3(-204f, -784f, 74f),
            new Vector3(-287f, -774f, 72f),
            new Vector3(-272f, -824f, 71f),
            new Vector3(-230f, -723f, 80f),
            new Vector3(1822f, 2044f, 62f),
            new Vector3(2410f, 2907f, 44f),
            new Vector3(2686f, 4858f, 36f),
            new Vector3(-1046f, 4751f, 244f),
            new Vector3(-213f, -2463f, 38f),
            new Vector3(-597f, -2192f, 38f),
            new Vector3(1036f, -980f, 41f),
            new Vector3(980f, -837f, 42f),
            new Vector3(1208f, -1173f, 38f),
        };

        public static readonly Vector3[] StuntJumps =
        {
            new Vector3(8.38f, 1716.45f, 227.37f),
            new Vector3(-440.14f, -1193.4f, 55.06f),
            new Vector3(470.47f, 4323.81f, 61.98f),
            new Vector3(-164.24f, 6577.06f, 14.12f),
            new Vector3(-973.24f, 4184.59f, 136.01f),
            new Vector3(-7.06f, -1035.52f, 39.55f),
            new Vector3(-269.28f, -773f, 57.61f),
            new Vector3(-83.84f, -537.13f, 41.01f),
            new Vector3(-1599.1f, -744.71f, 23.24f),
            new Vector3(-241.54f, -210.94f, 48.58f),
            new Vector3(-1445.36f, 402.98f, 111.79f),
            new Vector3(3352.37f, 5152.42f, 21.17f),
            new Vector3(1680.75f, 2340.48f, 75.81f),
            new Vector3(-883.41f, -851.79f, 22.87f),
            new Vector3(398.35f, -1657.98f, 50.54f),
            new Vector3(51.43f, -782.09f, 44.7f),
            new Vector3(38.68f, 6519.65f, 35.59f),
            new Vector3(1786.27f, 2047.11f, 67.15f),
            new Vector3(-1070.92f, 9.55f, 52.86f),
            new Vector3(1633.82f, 3606.05f, 38.06f),
            new Vector3(565.41f, -594.35f, 46.41f),
            new Vector3(451.16f, -1377.18f, 45.55f),
            new Vector3(-422.29f, -1556.47f, 27.06f),
            new Vector3(-964.45f, -2777.81f, 16.97f),
            new Vector3(-2005.18f, -312.82f, 49.55f),
            new Vector3(1676.43f, 3152.86f, 47.8f),
            new Vector3(-523.2f, -1483.91f, 14.82f),
            new Vector3(787.86f, -2911.2f, 8.11f),
            new Vector3(1979.79f, 1934.31f, 91.86f),
            new Vector3(672.13f, -3005.24f, 8.46f),
            new Vector3(109.22f, -2814.94f, 11.71f),
            new Vector3(110.06f, -3209.12f, 10.19f),
            new Vector3(123.16f, -2954.92f, 11.76f),
            new Vector3(178.91f, -2778.61f, 10.38f),
            new Vector3(164.71f, -2961.16f, 10.24f),
            new Vector3(287.03f, -3013.74f, 11.3f),
            new Vector3(371.55f, -2636.29f, 11.87f),
            new Vector3(-852.46f, -2552.9f, 23.93f),
            new Vector3(-988.51f, -2506.03f, 23.92f),
            new Vector3(-588.44f, -1529.17f, 4.67f),
            new Vector3(-625.81f, -1072.96f, 23.56f),
            new Vector3(-451.64f, -1397.27f, 32.82f),
            new Vector3(-443.88f, -542.03f, 27.01f),
            new Vector3(-597.81f, -108.51f, 43.48f),
            new Vector3(-727.59f, -56.68f, 41.2f),
            new Vector3(1479.2f, -2217.04f, 79.19f),
            new Vector3(367.56f, -2523.86f, 8.56f),
            new Vector3(308.62f, -620.02f, 43.89f),
            new Vector3(369.78f, -1158.22f, 31.5f),
            new Vector3(89.69f, -2191.28f, 5.75f),
        };

        public static readonly Landmark[] Landmarks =
        {
            new Landmark("MountChiliad", "Mount Chiliad Summit", new Vector3(501.9f, 5604.4f, 797.9f), 40f, 770f),
            new Landmark("MazeBankTower", "Maze Bank Tower Roof", new Vector3(-75.1f, -819.1f, 326.2f), 35f, 315f),
            new Landmark("FibBuilding", "FIB Building Roof", new Vector3(135.7f, -749.2f, 258.2f), 40f, 245f),
            new Landmark("IaaBuilding", "IAA Building Roof", new Vector3(117.2f, -620.9f, 206.1f), 40f, 150f),
            new Landmark("VinewoodSign", "Vinewood Sign", new Vector3(711.4f, 1198.1f, 348.5f), 70f, 0f),
            new Landmark("Observatory", "Galileo Observatory", new Vector3(-425f, 1115f, 326f), 70f, 0f),
            new Landmark("LandActDam", "Land Act Dam", new Vector3(1662f, 0f, 170f), 100f, 0f),
            new Landmark("DelPerroPier", "Del Perro Pier", new Vector3(-1850f, -1231f, 13f), 70f, 0f),
            new Landmark("Airport", "Los Santos International Airport", new Vector3(-1336f, -3044f, 14f), 200f, 0f),
            new Landmark("SandyShoresAirfield", "Sandy Shores Airfield", new Vector3(1747f, 3273f, 41f), 150f, 0f),
            new Landmark("Prison", "Bolingbroke Penitentiary", new Vector3(1855f, 2602f, 45f), 200f, 0f),
            new Landmark("HumaneLabs", "Humane Labs", new Vector3(3530f, 3730f, 30f), 200f, 0f),
            new Landmark("FortZancudo", "Fort Zancudo", new Vector3(-2047f, 3132f, 32f), 250f, 0f),
            new Landmark("PaletoSheriff", "Paleto Bay Sheriff's Office", new Vector3(-448f, 6012f, 31f), 60f, 0f),
            new Landmark("Grapeseed", "Grapeseed", new Vector3(1700f, 4800f, 42f), 150f, 0f),
            new Landmark("StabCity", "Stab City", new Vector3(70f, 3705f, 40f), 120f, 0f),
            new Landmark("TrevorsTrailer", "Trevor's Trailer", new Vector3(1975f, 3818f, 33f), 40f, 0f),
            new Landmark("AltruistCamp", "Altruist Camp", new Vector3(-1150f, 4925f, 222f), 120f, 0f),
            new Landmark("KortzCenter", "Kortz Center", new Vector3(-2243f, 264f, 174f), 90f, 0f),
            new Landmark("MichaelsHouse", "Michael's House", new Vector3(-812f, 179f, 76f), 40f, 0f),
            new Landmark("FranklinsHouse", "Franklin's House", new Vector3(7.5f, 528f, 174f), 40f, 0f),
            new Landmark("GroveStreet", "Grove Street", new Vector3(105f, -1940f, 20f), 80f, 0f),
            new Landmark("LestersHouse", "Lester's House", new Vector3(1273.9f, -1719.3f, 54.8f), 35f, 0f),
            new Landmark("FloydsApartment", "Floyd's Apartment", new Vector3(-1150.7f, -1520.7f, 10.6f), 35f, 0f),
            new Landmark("MazeBankArena", "Maze Bank Arena", new Vector3(-255f, -2026f, 30f), 180f, 0f),
            new Landmark("PillboxMedical", "Pillbox Hill Medical Center", new Vector3(298f, -584f, 43f), 60f, 0f),
            new Landmark("VespucciBeach", "Vespucci Beach", new Vector3(-1200f, -1570f, 4f), 120f, 0f),
            new Landmark("ChumashPier", "Chumash Pier", new Vector3(-3426f, 967f, 8f), 80f, 0f),
            new Landmark("ElGordoLighthouse", "El Gordo Lighthouse", new Vector3(3426f, 5174f, 7f), 120f, 0f),
            new Landmark("WindFarm", "RON Alternates Wind Farm", new Vector3(2300f, 1900f, 110f), 300f, 0f),
            new Landmark("SatelliteArray", "Satellite Dishes", new Vector3(2040f, 2950f, 47f), 200f, 0f),
            new Landmark("Sawmill", "Paleto Forest Sawmill", new Vector3(-570f, 5300f, 70f), 150f, 0f),
            new Landmark("AlamoSea", "Alamo Sea", new Vector3(1300f, 4100f, 30f), 350f, 0f),
            new Landmark("PacificStandard", "Pacific Standard Bank", new Vector3(235f, 216f, 106f), 50f, 0f),
            new Landmark("GolfClub", "Los Santos Golf Club", new Vector3(-1336f, 59f, 55f), 150f, 0f),
            new Landmark("BahamaMamas", "Bahama Mamas", new Vector3(-1388f, -586f, 30f), 50f, 0f),
            new Landmark("TequiLaLa", "Tequi-la-la", new Vector3(-560f, 286f, 82f), 45f, 0f),
            new Landmark("CluckinBell", "Cluckin' Bell Farms", new Vector3(-96f, 6200f, 31f), 150f, 0f),
            new Landmark("Hookies", "Hookies", new Vector3(-2190f, 4280f, 49f), 70f, 0f),
            new Landmark("MirrorPark", "Mirror Park", new Vector3(1100f, -640f, 56f), 180f, 0f),
            new Landmark("Port", "Port of Los Santos", new Vector3(1000f, -3000f, 5f), 300f, 0f),
            new Landmark("PaletoBank", "Blaine County Savings Bank", new Vector3(-110f, 6460f, 31f), 50f, 0f),
            new Landmark("LosSantosCustoms", "Los Santos Customs", new Vector3(-365f, -131f, 38f), 50f, 0f),
            new Landmark("MadrazoRanch", "Madrazo's Ranch", new Vector3(1395f, 1141f, 114f), 100f, 0f),
        };

        public static readonly KeyValuePair<int, string>[] Activities =
        {
            new KeyValuePair<int, string>(112, "Off-Road Race 1"),
            new KeyValuePair<int, string>(113, "Off-Road Race 2"),
            new KeyValuePair<int, string>(114, "Off-Road Race 3"),
            new KeyValuePair<int, string>(115, "Off-Road Race 4"),
            new KeyValuePair<int, string>(116, "Off-Road Race 5"),
            new KeyValuePair<int, string>(117, "Off-Road Race 6"),
            new KeyValuePair<int, string>(119, "Shooting Range: Handguns"),
            new KeyValuePair<int, string>(120, "Shooting Range: SMGs"),
            new KeyValuePair<int, string>(121, "Shooting Range: Rifles"),
            new KeyValuePair<int, string>(122, "Shooting Range: Machine Guns"),
            new KeyValuePair<int, string>(123, "Shooting Range: Heavy Weapons"),
            new KeyValuePair<int, string>(124, "Shooting Range: Shotguns"),
            new KeyValuePair<int, string>(125, "Stunt Plane Time Trials"),
            new KeyValuePair<int, string>(126, "Tennis"),
            new KeyValuePair<int, string>(127, "Golf"),
            new KeyValuePair<int, string>(130, "Darts"),
            new KeyValuePair<int, string>(300, "Yoga"),
            new KeyValuePair<int, string>(128, "Base Jumping"),
            new KeyValuePair<int, string>(129, "Skydiving"),
            new KeyValuePair<int, string>(141, "Triathlon 1"),
            new KeyValuePair<int, string>(142, "Triathlon 2"),
            new KeyValuePair<int, string>(143, "Triathlon 3"),
            new KeyValuePair<int, string>(132, "Sea Race 1"),
            new KeyValuePair<int, string>(133, "Sea Race 2"),
            new KeyValuePair<int, string>(134, "Sea Race 3"),
            new KeyValuePair<int, string>(135, "Sea Race 4"),
            new KeyValuePair<int, string>(136, "Street Race: City Circuit"),
            new KeyValuePair<int, string>(137, "Street Race: Airport"),
            new KeyValuePair<int, string>(138, "Street Race: Freeway"),
            new KeyValuePair<int, string>(139, "Street Race: Vespucci Canals"),
            new KeyValuePair<int, string>(140, "Street Race: South Los Santos"),
            new KeyValuePair<int, string>(181, "Flight School Lesson 1"),
            new KeyValuePair<int, string>(182, "Flight School Lesson 2"),
            new KeyValuePair<int, string>(183, "Flight School Lesson 3"),
            new KeyValuePair<int, string>(184, "Flight School Lesson 4"),
            new KeyValuePair<int, string>(185, "Flight School Lesson 5"),
            new KeyValuePair<int, string>(186, "Flight School Lesson 6"),
            new KeyValuePair<int, string>(187, "Flight School Lesson 7"),
            new KeyValuePair<int, string>(188, "Flight School Lesson 8"),
            new KeyValuePair<int, string>(189, "Flight School Lesson 9"),
            new KeyValuePair<int, string>(190, "Flight School Lesson 10"),
            new KeyValuePair<int, string>(191, "Flight School Lesson 11"),
            new KeyValuePair<int, string>(192, "Flight School Lesson 12"),
            new KeyValuePair<int, string>(288, "Watch a Movie"),
            new KeyValuePair<int, string>(301, "Ride the Cable Car"),
            new KeyValuePair<int, string>(302, "Car Wash"),
            new KeyValuePair<int, string>(304, "Buy Clothes"),
            new KeyValuePair<int, string>(305, "Modify a Car"),
            new KeyValuePair<int, string>(306, "Get a Haircut"),
            new KeyValuePair<int, string>(307, "Get a Tattoo"),
            new KeyValuePair<int, string>(309, "Watch TV"),
            new KeyValuePair<int, string>(297, "Hold Up a Store"),
            new KeyValuePair<int, string>(149, "Bail Bond 1"),
            new KeyValuePair<int, string>(150, "Bail Bond 2"),
            new KeyValuePair<int, string>(151, "Bail Bond 3"),
            new KeyValuePair<int, string>(152, "Bail Bond 4"),
            new KeyValuePair<int, string>(153, "Air Trafficking 1"),
            new KeyValuePair<int, string>(154, "Air Trafficking 2"),
            new KeyValuePair<int, string>(155, "Air Trafficking 3"),
            new KeyValuePair<int, string>(156, "Air Trafficking 4"),
            new KeyValuePair<int, string>(157, "Air Trafficking 5"),
            new KeyValuePair<int, string>(158, "Ground Trafficking 1"),
            new KeyValuePair<int, string>(159, "Ground Trafficking 2"),
            new KeyValuePair<int, string>(160, "Ground Trafficking 3"),
            new KeyValuePair<int, string>(161, "Ground Trafficking 4"),
            new KeyValuePair<int, string>(162, "Ground Trafficking 5"),
            new KeyValuePair<int, string>(180, "Hunting"),
        };

        public static readonly Skill[] Skills =
        {
            new Skill("Stamina", "STAMINA"),
            new Skill("Shooting", "SHOOTING_ABILITY"),
            new Skill("Strength", "STRENGTH"),
            new Skill("Stealth", "STEALTH_ABILITY"),
            new Skill("Flying", "FLYING_ABILITY"),
            new Skill("Driving", "WHEELIE_ABILITY"),
            new Skill("Lung Capacity", "LUNG_CAPACITY"),
        };
    }
}
