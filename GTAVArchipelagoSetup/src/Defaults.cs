namespace GTAVArchipelagoSetup
{
    internal static class Defaults
    {
        public const string Sources = @"{
  ""Components"": [
    {
      ""Name"": ""ScriptHookVDotNet"",
      ""Description"": ""Runs the .NET scripts (nightly build, needed for game version 1.0.3725)."",
      ""Repo"": ""scripthookvdotnet/scripthookvdotnet-nightly"",
      ""Asset"": ""^ScriptHookVDotNet.*\\.zip$"",
      ""Prerelease"": true,
      ""Target"": ""Game"",
      ""Files"": [ ""ScriptHookVDotNet.asi"", ""ScriptHookVDotNet2.dll"", ""ScriptHookVDotNet3.dll"", ""ScriptHookVDotNet.ini"" ]
    },
    {
      ""Name"": ""LemonUI"",
      ""Description"": ""Menu library used by every mod."",
      ""Repo"": ""LemonUIbyLemon/LemonUI"",
      ""Asset"": ""^LemonUI.*\\.zip$"",
      ""Target"": ""Mods"",
      ""Files"": [ ""LemonUI.SHVDN3.dll"" ]
    },
    {
      ""Name"": ""MissionLauncher"",
      ""Description"": ""Mission replays and start markers."",
      ""Repo"": ""dudleydehenau/GTAV-Archipelago"",
      ""Asset"": ""^MissionLauncher-[0-9].*\\.zip$"",
      ""Target"": ""Mods""
    },
    {
      ""Name"": ""GameEnvironment"",
      ""Description"": ""Sterile open world, characters, money, weapons and vehicles."",
      ""Repo"": ""dudleydehenau/GTAV-Archipelago"",
      ""Asset"": ""^GameEnvironment-[0-9].*\\.zip$"",
      ""Target"": ""Mods""
    },
    {
      ""Name"": ""GameTraps"",
      ""Description"": ""The traps."",
      ""Repo"": ""dudleydehenau/GTAV-Archipelago"",
      ""Asset"": ""^GameTraps-[0-9].*\\.zip$"",
      ""Target"": ""Mods""
    },
    {
      ""Name"": ""GameCollectibles"",
      ""Description"": ""The 160 collectibles."",
      ""Repo"": ""dudleydehenau/GTAV-Archipelago"",
      ""Asset"": ""^GameCollectibles-[0-9].*\\.zip$"",
      ""Target"": ""Mods""
    },
    {
      ""Name"": ""GameArchipelago"",
      ""Description"": ""The Archipelago client."",
      ""Repo"": ""dudleydehenau/GTAV-Archipelago"",
      ""Asset"": ""^GameArchipelago-[0-9].*\\.zip$"",
      ""Target"": ""Mods""
    },
    {
      ""Name"": ""APWorld"",
      ""Description"": ""gtav.apworld for the Archipelago launcher (to generate games)."",
      ""Repo"": ""dudleydehenau/GTAV-Archipelago"",
      ""Asset"": ""\\.apworld$"",
      ""Target"": ""World""
    }
  ]
}
";
    }
}
